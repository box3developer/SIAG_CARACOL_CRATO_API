namespace dotnet_api.Models
{
    public class CaracolStatusModel
    {
        public int Nivel { get; set; }
        public int CaixasPendentes { get; set; }
        public bool CaracolCheio { get; set; }
        public string? Operador { get; set; }
    }
}