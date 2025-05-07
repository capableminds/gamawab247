using Newtonsoft.Json;

namespace TransactionMonitoring.Service
{
    public record SimpleFlutterwaveVerificationRes
    {
        [JsonProperty("id")]
        public int id { get; set; }
        [JsonProperty("amount")]
        public decimal amount { get; set; }
        [JsonProperty("currency")]
        public string currency { get; set; }
        [JsonProperty("status")]
        public string status { get; set; }
    }

}
