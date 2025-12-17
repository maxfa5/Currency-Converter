using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Converter
{
    public class Currency
    {
        [JsonPropertyName("ID")]
        public string ID { get; set; }

        [JsonPropertyName("NumCode")]
        public string NumCode { get; set; }

        [JsonPropertyName("CharCode")]
        public string CharCode { get; set; }

        [JsonPropertyName("Nominal")]
        public int Nominal { get; set; }

        [JsonPropertyName("Name")]
        public string Name { get; set; }

        [JsonPropertyName("Value")]
        public decimal Value { get; set; }

        [JsonPropertyName("Previous")]
        public decimal Previous { get; set; }

        public string DisplayName => $"{CharCode} - {Name}";
    }
}
