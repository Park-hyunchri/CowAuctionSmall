namespace CowAuctionSmall.Presentation.Display
{
    public enum HistoryBatchStage { Empty, Preparing, Loading, Applying, Complete, Failed }

    public sealed record HistoryBatchProgress(long Version, string Epoch, HistoryBatchStage Stage,
        int Total = 0, int Cached = 0, int Succeeded = 0, int Failed = 0, int Stopped = 0, int Notes = 0)
    {
        public int Processed => Cached + Succeeded + Failed + Stopped;
        public string Text => Stage switch
        {
            HistoryBatchStage.Empty => "이력 조회 대상 없음",
            HistoryBatchStage.Preparing => $"이력 캐시 확인 · 대상 {Total}건 · 캐시부터 표시",
            HistoryBatchStage.Applying => $"이력 {Processed}/{Total}건 처리 · 화면 반영 대기",
            HistoryBatchStage.Complete => $"이력 처리 완료 {Processed}/{Total}건 · 소유주 변경 비고 {Notes}건 · 캐시 {Cached} · 성공 {Succeeded} · 실패 {Failed} · 중단 {Stopped}",
            HistoryBatchStage.Failed => "이력 처리 실패 · 기존 표시 유지 · 이력 새로고침으로 재시도",
            _ => $"이력 조회 {Processed}/{Total}건 처리 · 캐시 {Cached} · 성공 {Succeeded} · 실패 {Failed} · 중단 {Stopped} · 조회된 정보부터 반영"
        };
    }
}
