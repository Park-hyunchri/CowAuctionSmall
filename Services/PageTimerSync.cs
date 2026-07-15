using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace CowAuctionSmall.Services
{
    public sealed class PageSyncState
    {
        public int PageIndex { get; }
        public int TotalPages { get; }
        public int SecondsLeft { get; }
        public long UnixSeconds { get; }

        public PageSyncState(int pageIndex, int totalPages, int secondsLeft, long unixSeconds)
        {
            PageIndex = pageIndex;
            TotalPages = totalPages;
            SecondsLeft = secondsLeft;
            UnixSeconds = unixSeconds;
        }
    }

    public sealed class PageTimerSync : IDisposable
    {
        private const string MessagePrefix = "PAGESYNC";
        private const int MessageVersion = 1;

        private readonly Dispatcher _dispatcher;
        private readonly Action<PageSyncState> _onPageSync;
        private readonly Action<bool> _onMasterChanged;
        private readonly Action<string>? _log;
        private readonly int _port;
        private readonly int _heartbeatMs;
        private readonly TimeSpan _timeout;
        private readonly object _stateLock = new();
        private readonly object _lifecycleLock = new();

        private UdpClient? _client;
        private CancellationTokenSource? _cts;
        private Task? _receiveTask;
        private System.Timers.Timer? _heartbeatTimer;
        private System.Timers.Timer? _timeoutTimer;
        private PageSyncState? _state;
        private DateTime _lastHeartbeatUtc = DateTime.MinValue;
        private bool _isMaster;
        private bool _isStarted;
        private bool _isDisposed;
        private readonly string _localIp;
        private readonly uint _localIpValue;
        private IPEndPoint _broadcastEndpoint;

        public bool IsMaster => _isMaster;
        public string LocalIp => _localIp;

        public PageTimerSync(
            Dispatcher dispatcher,
            Action<PageSyncState> onPageSync,
            Action<bool> onMasterChanged,
            Action<string>? log = null,
            int port = 45123,
            int heartbeatMs = 1000,
            int timeoutMs = 5000)
        {
            _dispatcher = dispatcher;
            _onPageSync = onPageSync;
            _onMasterChanged = onMasterChanged;
            _log = log;
            _port = port;
            _heartbeatMs = heartbeatMs;
            _timeout = TimeSpan.FromMilliseconds(timeoutMs);

            var ip = NetworkInterfaceHelper.GetPreferredIPv4();
            _localIp = ip.ToString();
            _localIpValue = IpToUInt(ip);
            _broadcastEndpoint = new IPEndPoint(IPAddress.Broadcast, _port);
        }

        public void Start()
        {
            lock (_lifecycleLock)
            {
                if (_isDisposed || _client != null)
                    return;

                _client = new UdpClient(_port)
                {
                    EnableBroadcast = true
                };

                _cts = new CancellationTokenSource();
                _receiveTask = Task.Run(() => ReceiveLoop(_cts.Token));

                _heartbeatTimer = new System.Timers.Timer(_heartbeatMs)
                {
                    AutoReset = true
                };
                _heartbeatTimer.Elapsed += HeartbeatTimerElapsed;

                _timeoutTimer = new System.Timers.Timer(500)
                {
                    AutoReset = true
                };
                _timeoutTimer.Elapsed += TimeoutTimerElapsed;
                _timeoutTimer.Start();

                _lastHeartbeatUtc = DateTime.MinValue;
                _isMaster = false;
                _isStarted = true;
            }

            Log($"[PageSync] started (ip={_localIp}, port={_port})");
        }

        public void Stop()
        {
            System.Timers.Timer? timeoutTimer;
            System.Timers.Timer? heartbeatTimer;
            CancellationTokenSource? cts;
            UdpClient? client;

            lock (_lifecycleLock)
            {
                if (_client == null && _heartbeatTimer == null && _timeoutTimer == null && _cts == null)
                    return;

                _isStarted = false;
                _isMaster = false;
                _lastHeartbeatUtc = DateTime.MinValue;

                timeoutTimer = _timeoutTimer;
                heartbeatTimer = _heartbeatTimer;
                cts = _cts;
                client = _client;

                _timeoutTimer = null;
                _heartbeatTimer = null;
                _client = null;
                _cts = null;
                _receiveTask = null;
            }

            if (timeoutTimer != null)
            {
                timeoutTimer.Elapsed -= TimeoutTimerElapsed;
                timeoutTimer.Stop();
                timeoutTimer.Dispose();
            }

            if (heartbeatTimer != null)
            {
                heartbeatTimer.Elapsed -= HeartbeatTimerElapsed;
                heartbeatTimer.Stop();
                heartbeatTimer.Dispose();
            }

            cts?.Cancel();
            cts?.Dispose();
            client?.Close();
            client?.Dispose();

            Log("[PageSync] stopped");
        }

        public void UpdateState(PageSyncState state)
        {
            lock (_stateLock)
            {
                _state = state;
            }
        }

        public void Dispose()
        {
            lock (_lifecycleLock)
            {
                if (_isDisposed)
                    return;

                _isDisposed = true;
            }

            Stop();
            GC.SuppressFinalize(this);
        }

        private async Task ReceiveLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                UdpReceiveResult result;
                try
                {
                    result = await _client!.ReceiveAsync(token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Log($"[PageSync] receive error: {ex.Message}");
                    continue;
                }

                var message = Encoding.UTF8.GetString(result.Buffer);
                if (!TryParse(message, out var state, out var senderIpValue))
                    continue;

                if (senderIpValue == _localIpValue)
                    continue;

                HandleHeartbeat(state, senderIpValue);
            }
        }

        private void HandleHeartbeat(PageSyncState state, uint senderIpValue)
        {
            lock (_lifecycleLock)
            {
                if (!_isStarted)
                    return;
            }

            if (senderIpValue < _localIpValue)
            {
                _lastHeartbeatUtc = DateTime.UtcNow;
                SetMaster(false, $"master={UIntToIp(senderIpValue)}");
                DispatchPage(state);
            }
            else if (senderIpValue > _localIpValue)
            {
                if (!_isMaster)
                {
                    SetMaster(true, $"preempt (remote={UIntToIp(senderIpValue)})");
                }
            }
        }

        private void CheckTimeout()
        {
            lock (_lifecycleLock)
            {
                if (!_isStarted || _isMaster)
                    return;
            }

            if (_lastHeartbeatUtc == DateTime.MinValue || (DateTime.UtcNow - _lastHeartbeatUtc) > _timeout)
            {
                SetMaster(true, "timeout");
            }
        }

        private void SendHeartbeat()
        {
            if (!_isMaster || _client == null)
                return;

            PageSyncState? state;
            lock (_stateLock)
            {
                state = _state;
            }

            if (state == null)
                return;

            var payload = BuildMessage(state);
            var bytes = Encoding.UTF8.GetBytes(payload);
            try
            {
                _client.Send(bytes, bytes.Length, _broadcastEndpoint);
            }
            catch (Exception ex)
            {
                Log($"[PageSync] send error: {ex.Message}");
            }
        }

        private void SetMaster(bool value, string reason)
        {
            lock (_lifecycleLock)
            {
                if (!_isStarted || _isMaster == value)
                    return;

                _isMaster = value;

                try
                {
                    if (_isMaster)
                    {
                        _heartbeatTimer?.Start();
                    }
                    else
                    {
                        _heartbeatTimer?.Stop();
                    }
                }
                catch (ObjectDisposedException)
                {
                    Log($"[PageSync] timer state change skipped after dispose ({reason})");
                    return;
                }
                catch (InvalidOperationException ex)
                {
                    Log($"[PageSync] timer state change failed: {ex.Message}");
                    return;
                }
                catch (NullReferenceException)
                {
                    Log($"[PageSync] timer state change skipped during shutdown ({reason})");
                    return;
                }
            }

            _dispatcher.BeginInvoke(() => _onMasterChanged(value));
            Log($"[PageSync] {(value ? "MASTER" : "SUB")} ({reason})");
        }

        private void HeartbeatTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            SendHeartbeat();
        }

        private void TimeoutTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            CheckTimeout();
        }

        private void DispatchPage(PageSyncState state)
        {
            _dispatcher.BeginInvoke(() => _onPageSync(state));
        }

        private string BuildMessage(PageSyncState state)
        {
            return string.Join('|',
                MessagePrefix,
                MessageVersion,
                _localIp,
                state.PageIndex,
                state.TotalPages,
                state.SecondsLeft,
                state.UnixSeconds);
        }

        private static bool TryParse(string payload, out PageSyncState state, out uint senderIpValue)
        {
            state = null!;
            senderIpValue = 0;

            var parts = payload.Split('|');
            if (parts.Length < 7)
                return false;

            if (!string.Equals(parts[0], MessagePrefix, StringComparison.Ordinal))
                return false;

            if (!int.TryParse(parts[1], out var version) || version != MessageVersion)
                return false;

            if (!IPAddress.TryParse(parts[2], out var ip))
                return false;

            if (!int.TryParse(parts[3], out var pageIndex))
                return false;

            if (!int.TryParse(parts[4], out var totalPages))
                return false;

            if (!int.TryParse(parts[5], out var secondsLeft))
                return false;

            if (!long.TryParse(parts[6], out var unixSeconds))
                return false;

            senderIpValue = IpToUInt(ip);
            state = new PageSyncState(pageIndex, totalPages, secondsLeft, unixSeconds);
            return true;
        }

        private static uint IpToUInt(IPAddress ip)
        {
            var bytes = ip.GetAddressBytes();
            if (bytes.Length != 4)
                return 0;

            return ((uint)bytes[0] << 24) | ((uint)bytes[1] << 16) | ((uint)bytes[2] << 8) | bytes[3];
        }

        private static string UIntToIp(uint ip)
        {
            var b0 = (ip >> 24) & 0xFF;
            var b1 = (ip >> 16) & 0xFF;
            var b2 = (ip >> 8) & 0xFF;
            var b3 = ip & 0xFF;
            return $"{b0}.{b1}.{b2}.{b3}";
        }

        private void Log(string message)
        {
            if (_log == null)
                return;

            _dispatcher.BeginInvoke(() => _log(message));
        }
    }

    internal static class NetworkInterfaceHelper
    {
        public static IPAddress GetPreferredIPv4()
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(nic => nic.OperationalStatus == OperationalStatus.Up)
                .Where(nic => nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .Where(nic => nic.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                .ToList();

            foreach (var nic in interfaces)
            {
                var props = nic.GetIPProperties();
                var hasGateway = props.GatewayAddresses.Any(g =>
                    g.Address.AddressFamily == AddressFamily.InterNetwork &&
                    !g.Address.Equals(IPAddress.Any) &&
                    !g.Address.Equals(IPAddress.None));

                if (!hasGateway)
                    continue;

                var ip = props.UnicastAddresses.FirstOrDefault(u => u.Address.AddressFamily == AddressFamily.InterNetwork);
                if (ip != null)
                    return ip.Address;
            }

            foreach (var nic in interfaces)
            {
                var props = nic.GetIPProperties();
                var ip = props.UnicastAddresses.FirstOrDefault(u => u.Address.AddressFamily == AddressFamily.InterNetwork);
                if (ip != null)
                    return ip.Address;
            }

            return IPAddress.Loopback;
        }
    }
}
