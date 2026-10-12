namespace CowAuctionSmall.Domain.Settings
{
    public sealed class AnimalTraceSettings
    {
        public bool Enabled { get; set; }
        public string IsAnimalProductsTraceability { get; set; } = "N";
        public bool ShowOwnerChangeNote { get; set; } = true;
        public string Address { get; set; } = "http://data.ekape.or.kr/openapi-data/service/user/animalTrace/traceNoSearch";
        public string ServiceKey { get; set; } = "";
        // Zero disables the local daily cap; provider quota responses still stop requests.
        public int DailyLimit { get; set; } = 0;
        public const int DefaultRequestIntervalMs = 1000;
        public const int MinimumRequestIntervalMs = 250;
        public int RequestIntervalMs { get; set; } = DefaultRequestIntervalMs;
        public static int NormalizeRequestInterval(int value) => value <= 0
            ? DefaultRequestIntervalMs : System.Math.Max(MinimumRequestIntervalMs, value);
        // Keep production at one until the account's concurrent request allowance is verified.
        public int MaxConcurrentRequests { get; set; } = 1;
    }
}
