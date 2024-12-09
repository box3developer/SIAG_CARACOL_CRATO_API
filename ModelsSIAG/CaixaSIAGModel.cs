
namespace dotnet_api.ModelsSIAG
{
    public class CaixaSIAGModel {
        public string? IdCaixa { get; set; }
        public Guid? IdAgrupador { get; set; }
        public string? IdPallet { get; set; }
        public string? IdPedido { get; set; }
        public int? FgStatus { get; set; }
        public DateTime? DtExpedicao { get; set; }
        public DateTime? DtEstufamento { get; set; }
        public DateTime DtLeitura { get; internal set; }
        public DateTime? DtSorter { get; internal set; }
    }   
}