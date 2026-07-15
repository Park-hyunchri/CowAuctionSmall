using CowAuctionSmall.NetProto.interfaces;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace CowAuctionSmall.NetProto.netty
{
    class AuctionDelegate
    {
        private static readonly Lazy<AuctionDelegate> instance = new Lazy<AuctionDelegate>(() => new AuctionDelegate());
        private AuctionShareNettyClient? mClient; // Netty 클라이언트 객체
        private readonly object _clientLock = new object();
        private readonly SemaphoreSlim _disposeGate = new SemaphoreSlim(1, 1);

        private AuctionDelegate() { }

        /// <summary>
        /// 싱글톤 인스턴스를 반환하는 메서드
        /// </summary>
        public static AuctionDelegate getInstance()
        {
            return instance.Value;
        }


        /// <summary>
        /// Netty 클라이언트를 생성하고 실행하는 메서드.
        /// </summary>
        public void createClients(string host_, int port_, iNettyControllable controllable)
        {
            lock (_clientLock)
            {
                if (mClient != null && mClient.isActive())
                {
                    Debug.WriteLine("✅ Netty 클라이언트가 이미 실행 중입니다.");
                    return;
                }

                Debug.WriteLine($"🔄 Netty 클라이언트 생성: {host_}:{port_}");
                mClient = new AuctionShareNettyClient.Builder(host_, port_)
                            .setController(controllable)
                            .buildAndRun();
            }
        }

        /// <summary>
        /// Netty 클라이언트를 안전하게 종료하는 메서드.
        /// </summary>
        public async Task disposeClients()
        {
            await _disposeGate.WaitAsync().ConfigureAwait(false);
            try
            {
                AuctionShareNettyClient? clientToDispose;
                lock (_clientLock)
                {
                    clientToDispose = mClient;
                    mClient = null;
                }

                if (clientToDispose == null)
                {
                    Debug.WriteLine("⚠️ 종료할 Netty 클라이언트가 없습니다.");
                    return;
                }

                Debug.WriteLine("🛑 Netty 클라이언트 종료 중...");
                await clientToDispose.stopClient().ConfigureAwait(false);
            }
            finally
            {
                _disposeGate.Release();
            }
        }




        /// <summary>
        /// 현재 Netty 클라이언트가 활성 상태인지 확인.
        /// </summary>
        public bool isActive()
        {
            lock (_clientLock)
            {
                return mClient != null && mClient.isActive();
            }
        }

        public String sendMessage(String msg)
        {
            AuctionShareNettyClient? currentClient;
            lock (_clientLock)
            {
                currentClient = mClient;
            }

            if (currentClient != null && currentClient.isActive())
            {
                currentClient.sendMessage(msg);
                return msg;
            }
            else
            {
                return "IS NULL or NOT ACTIVE";
            }
        }
    }
}
