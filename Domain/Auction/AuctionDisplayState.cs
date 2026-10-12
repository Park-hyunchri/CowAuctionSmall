using static CowAuctionSmall.Domain.Auction.AuctionStatus;

namespace CowAuctionSmall.Domain.Auction
{
    public enum AuctionDisplayPhase
    {
        Unknown,
        Waiting,
        Running,
        Finished
    }

    /// <summary>수신으로 확인된 상태바 정보. 화면 전환이나 경매 업무 상태를 변경하지 않는다.</summary>
    public sealed record AuctionDisplayState(int Method, AuctionDisplayPhase Phase, int? SipNumber)
    {
        public static AuctionDisplayState Empty { get; } = new(0, AuctionDisplayPhase.Unknown, null);

        public AuctionDisplayState WithMethod(int method)
        {
            if (method is not (10 or 20)) return this;
            return Method == method ? this : new(method, AuctionDisplayPhase.Unknown, null);
        }

        public AuctionDisplayState ApplyState(AS state, int? sipNumber)
        {
            if (Method is not (10 or 20)) return Empty;
            switch (state)
            {
                case AS.NONE:
                    return new(Method, AuctionDisplayPhase.Unknown, null);
                case AS.READY:
                    return new(Method, AuctionDisplayPhase.Waiting, null);
                case AS.START:
                    // 8003은 예정가 변경에도 수신된다. 진행 중인 경매를 새로 시작하거나 종료하지 않는다.
                    return Phase == AuctionDisplayPhase.Running ? this : new(Method, AuctionDisplayPhase.Waiting, null);
                case AS.PROGRESS:
                    if (Method == 20 && (!sipNumber.HasValue || sipNumber.Value < 0)) return this;
                    return new(Method, AuctionDisplayPhase.Running, Method == 20 && sipNumber > 0 ? sipNumber : null);
                case AS.COMPLETED:
                    return Method == 20 ? CompleteSingle(sipNumber) : new(Method, AuctionDisplayPhase.Finished, null);
                case AS.FINISH:
                    return new(Method, AuctionDisplayPhase.Finished, null);
                default:
                    // 8005와 SD F 등 의미가 확정되지 않은 신호로 진행 상태를 추측하지 않는다.
                    return this;
            }
        }

        public AuctionDisplayState CompleteSingle(int? sipNumber)
        {
            if (Method != 20 || !sipNumber.HasValue || sipNumber.Value < 0) return this;
            if (Phase == AuctionDisplayPhase.Running && SipNumber.HasValue && SipNumber != sipNumber) return this;
            return new(Method, AuctionDisplayPhase.Finished, sipNumber > 0 ? sipNumber : null);
        }
    }
}
