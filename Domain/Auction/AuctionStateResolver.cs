
namespace CowAuctionSmall.Domain.Auction
{
    public static class AuctionStateResolver
    {
        public static DisplayAuctionState Resolve(gValues? item)
        {
            if (item == null || !int.TryParse(item.AuctionResultStatus, out int status))
                return DisplayAuctionState.Logo;

            // 결과 코드를 우선한다. 취소는 기존과 같이 로고 화면에 연결한다.
            return status switch
            {
                11 => item.IsRunning ? DisplayAuctionState.Running : DisplayAuctionState.Ready,
                22 => DisplayAuctionState.Sold,
                23 => DisplayAuctionState.Unsold,
                24 => DisplayAuctionState.Canceled,
                _ => DisplayAuctionState.Logo
            };
        }
    }
}
