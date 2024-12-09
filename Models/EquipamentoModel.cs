using dotnet_api.ModelsSIAG;

namespace dotnet_api.Models
{
    public class EquipamentoModel
    {
        public string? IdEquipamento { get; set; }
        public string? NmEquipamento { get; set; }
        public string? NmIdentificador { get; set; }
        public string? IdOperador { get; set; }
        public OperadorModel? Operador { get; set; }
        public string? CdUltimaLeitura { get; set; }
        public DateTime? DtUltimaLeitura { get; set; }
        public int? CaixasPendentes { get; set; }
        public bool Cheio { get; set; }
        public int? Nivel { get; set; }
        public LiderVirtualSIAGModel? LiderVirtual { get; set; }
    }
}