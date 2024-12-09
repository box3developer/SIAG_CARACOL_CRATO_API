
namespace dotnet_api.ModelsSIAG
{
    public class AreaArmazenagemSIAGModel {
        public string? IdAreaArmazenagem { get; set; }
        public Guid IdAgrupador { get; set; }
        public Guid IdAgrupadorReservado { get; set; }
        public string? IdentificadorCaracol { get; set; }
        public string? IdEndereco { get; set; }
        public int? FgStatus { get; set; }
        public int? PosicaoX { get; set; }
        public int? PosicaoY { get; set; }
    }   
}