using CowAuctionSmall.NetProto.interfaces;
using System;
using System.Runtime.CompilerServices;

namespace CowAuctionSmall.NetProto.netty
{
    class AuctionDelegate
    {
        private static AuctionDelegate? instance = null;

        public AuctionShareNettyClient? mClient; // 네티 접속 객체

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
            this.mClient = new AuctionShareNettyClient.Builder(host_, port_).setController(controllable).buildAndRun();
        }

        public void disposeClients()
        {
            if (this.isActive() == false || this.mClient == null)
                return;

            // mClient 사용하기 전에 null 여부를 확인합니다.
            // 리소스를 명시적으로 해제하기 위해 Dispose 메서드를 호출합니다.
            this.mClient.stopClient();
            //this.mClient.Dispose();

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
    }
}
