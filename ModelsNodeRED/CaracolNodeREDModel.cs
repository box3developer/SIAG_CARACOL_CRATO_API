using System.Text.Json.Serialization;

namespace dotnet_api.ModelsNodeRED
{
    public class CaracolNodeREDModel
    {
        [JsonPropertyName("caracol")]
        public string? Caracol { get; set; }

        [JsonPropertyName("cheio")]
        public int? Cheio { get; set; }

        [JsonPropertyName("luzVD")]
        public int? LuzVD { get; set; }

        [JsonPropertyName("luzesVM")]
        public List<int>? LuzesVM { get; set; }
    }
}