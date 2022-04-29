using System;
using System.Text.Json.Serialization;

namespace HBARTVLScanner.Web.Data
{
    public class PricePayload
    {
        [JsonPropertyName("hedera-hashgraph")]
        public Price Payload { get; set; }
    }

    public class Price
    {
        [JsonPropertyName("usd")]
        public double USD { get; set; }

        [JsonPropertyName("usd_24h_change")]
        public double TwentyFourHourChange { get; set; }
    }
}

