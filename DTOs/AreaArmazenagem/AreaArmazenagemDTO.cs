using dotnet_api.Data;

namespace dotnet_api.DTOs.AreaArmazenagem
{
    public class AreaArmazenagemDTO
    {
        public long IdAreaArmazenagem { get; set; }
        public int IdTipoArea { get; set; }
        public int IdEndereco { get; set; }
        public Guid IdAgrupador { get; set; }
        public string? IdCaracol { get; set; }
        public int NrPosicaoX { get; set; }
        public int NrPosicaoY { get; set; }
        public int NrLado { get; set; }
        public StatusAreaArmazenagem FgStatus { get; set; }
        public string CdIdentificacao { get; set; } = string.Empty;
    }
}
