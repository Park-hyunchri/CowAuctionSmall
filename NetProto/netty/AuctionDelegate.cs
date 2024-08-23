using CowAuctionSmall.Models;
using CowAuctionSmall.NetProto.interfaces;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace CowAuctionSmall.NetProto.netty
{
    class AuctionDelegate : IDisposable
    {
        private static AuctionDelegate? instance = null;

        public AuctionShareNettyClient? mClient; // 네티 접속 객체

        private NLogger logger = NLogger.Instance;

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static AuctionDelegate getInstance()
        {
            if (instance == null)
            {
                instance = new AuctionDelegate();
            }

            return instance;
        }

        public void createClients(String host_, int port_, iNettyControllable controllable)
        {
            logger.LogInfo("AuctionDelegate createClients 시작");
            this.mClient = new AuctionShareNettyClient.Builder(host_, port_).setController(controllable).buildAndRun();
        }

        public async Task disposeClientsAsync()
        {
            if (this.isActive() == false || this.mClient == null)
                return;

            // mClient 사용하기 전에 null 여부를 확인합니다.
            // 리소스를 명시적으로 해제하기 위해 Dispose 메서드를 호출합니다.
            await this.mClient.StopClientAsync(); // 비동기 메서드 호출
            this.mClient = null;
        }

        public bool isActive()
        {
            if (mClient == null)
                return false;
            return mClient.isActive();
        }

        public String sendMessage(String msg)
        {
            if (mClient != null && isActive())
            {
                mClient.sendMessage(msg);
                return msg;
            }
            else
            {
                return "IS NULL or NOT ACTIVE";
            }
        }

        // IDisposable 인터페이스 구현
        public void Dispose()
        {
            // 리소스를 해제합니다.
            DisposeAsync().GetAwaiter().GetResult();
        }

        public async Task DisposeAsync()
        {
            await disposeClientsAsync();
        }
    }
}
