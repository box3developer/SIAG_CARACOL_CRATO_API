namespace dotnet_api.Models
{
    public class StatusLeitorModel
    {
        public string Equipamento { get; set; } = string.Empty;
        public string Leitor { get; set; } = string.Empty;
        public bool Configurado { get; set; }
        public bool Conectado { get; set; }
        public bool Executando { get; set; }
        public DateTime dt_status { get; set; }
    }
}