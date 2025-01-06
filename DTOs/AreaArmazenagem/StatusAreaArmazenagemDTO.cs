namespace dotnet_api.DTOs.AreaArmazenagem
{
    public class StatusAreaArmazenagemDTO
    {
        public long Codigo { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public string Cor { get; set; } = string.Empty;
        public bool SemPallet { get; set; }
        public int Pallet { get; set; }
        public int Caracol { get; set; }
        public int Gaiola { get; set; }
        public int Quantidade { get; set; }
    }
}
