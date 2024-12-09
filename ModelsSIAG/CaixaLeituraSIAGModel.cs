
namespace dotnet_api.ModelsSIAG
{
    public class CaixaLeituraSIAGModel {
        public string? IdCaixaLeitura { get; set; }
        public string? IdCaixa { get; set; }
        public DateTime DtLeitura { get; set; }
        public int? FgTipo { get; set; }
        public int? FgStatus { get; set; }
        public string? IdOperador { get; set; }
        public string? IdEquipamento { get; set; }
        public string? IdPallet { get; set; }
        public string? IdAreaArmazenagem { get; set; }
        public string? IdEndereco { get; set; }
        public int? FgCancelado { get; set; }
        public string? IdOrdem { get; set; }
    }   
}