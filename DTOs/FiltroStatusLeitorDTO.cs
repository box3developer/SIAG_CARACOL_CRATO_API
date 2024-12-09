namespace dotnet_api.DTOs
{
    public class FiltroStatusLeitorDTO
    {
        public string? Equipamento { get; set; }
        public string? Leitor { get; set; }
        public bool? Configurado { get; set; }
        public bool? Conectado { get; set; }
        public bool? Executando { get; set; }
        public DateTime? InicioPeriodo { get; set; }
        public DateTime? FimPeriodo { get; set; }
    }
}
