using CowAuctionSmall.Application.Abstractions;
using CowAuctionSmall.Domain.Auction;
using CowAuctionSmall.Domain.Settings;
using CowAuctionSmall.Infra.XmlSettings;
using CowAuctionSmall.Infra.Netty.netty;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace CowAuctionSmall.Infra.Api
{
    public sealed class AuctionSessionRuntime : IAuctionSessionRuntime
    {
        private readonly ServerConn _connection;
        private readonly XmlParserCont _settings;

        public AuctionSessionRuntime(ServerConn connection, XmlParserCont settings)
        {
            _connection = connection;
            _settings = settings;
        }

        public UserInfo? LoadUserSettings() => _settings.XmlPaserResult().userInfo;
        public bool IsNetworkAvailable => NetworkInterface.GetIsNetworkAvailable();
        public bool IsConnected => AuctionDelegate.getInstance().isActive();
        public long TotalApiCount => ServerConn.TotalApiCount;
        public async Task<AuctionAuthenticationResult?> AuthenticateAsync(UserInfo user)
        {
            var result = await _connection.IssueToken(user).ConfigureAwait(false);
            return result == null ? null : new AuctionAuthenticationResult(result.AccessToken, result.AuctionHouseCode);
        }
        public Task<List<gValues>?> ReadCachedAuctionAsync(UserInfo user) => _connection.ReadCachedAuctionAsync(user);
        public async Task<AuctionConnectionResult> ConnectAsync(UserInfo user, string token)
        {
            var result = await _connection.NettyComm(user, token).ConfigureAwait(false);
            return result switch
            {
                NettyConnectResult.Connected => AuctionConnectionResult.Connected,
                NettyConnectResult.Duplicate => AuctionConnectionResult.Duplicate,
                NettyConnectResult.Timeout => AuctionConnectionResult.Timeout,
                _ => AuctionConnectionResult.Failed
            };
        }
        public Task DisconnectAsync() => AuctionDelegate.getInstance().disposeClients();
    }
}
