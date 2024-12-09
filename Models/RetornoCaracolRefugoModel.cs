using dotnet_api.ModelsSIAG;

namespace dotnet_api.Models
{
    public class RetornoCaracolRefugoModel
    {
        public int? Gaiola { get; set; }
        public ParametroMensagemCaracolSIAGModel? Mensagem { get; set; }
        public PerformanceOperadorModel? PerformanceDia { get; set; }
        public PerformanceOperadorModel? PerformanceHora { get; set; }
    }
}