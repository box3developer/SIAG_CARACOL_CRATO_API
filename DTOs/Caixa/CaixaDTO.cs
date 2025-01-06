namespace dotnet_api.DTOs.Caixa
{
    public class CaixaDTO
    {
        public string IdCaixa { get; set; } = string.Empty;
        public Guid? IdAgrupador { get; set; }
        public int? IdPallet { get; set; }
        public int? IdPrograma { get; set; }
        public string? IdPedido { get; set; }
        public string? CdProduto { get; set; }
        public string? CdCor { get; set; }
        public string? CdGradeTamanho { get; set; }
        public int? NrCaixa { get; set; }
        public int? NrPares { get; set; }
        public bool? FgRFID { get; set; }
        public int? FgStatus { get; set; }
        public DateTime? DtEmbalagem { get; set; }
        public DateTime? DtExpedicao { get; set; }
        public DateTime? DtSorter { get; set; }
        public DateTime? DtEstufamento { get; set; }
        public DateTime DtLeitura { get; set; }
    }
}
