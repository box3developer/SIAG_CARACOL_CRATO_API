namespace dotnet_api.DTOs
{
    public class EmailDTO
    {
        public string EmailUsuario { get; set; } = string.Empty;
        public string EmailNome { get; set; } = string.Empty;
        public string EmailSenha { get; set; } = string.Empty;

        public string EmailServidor { get; set; } = string.Empty;
        public int? EmailPorta { get; set; }
        public bool? EmailSSL { get; set; }

        public string EmailDestinatario { get; set; } = string.Empty;
        public string EmailAssunto { get; set; } = string.Empty;
        public string EmailConteudo { get; set; } = string.Empty;
    }
}
