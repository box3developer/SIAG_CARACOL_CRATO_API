using System.Net;
using System.Net.Mail;
using dotnet_api.DTOs;

namespace dotnet_api.Utils
{
    public static class EmailUtil
    {
        public static async Task<string> EnviarEmail(EmailDTO emailDTO)
        {
            try
            {
                string EmailDestinatario = "";
                string EmailAssunto = "";
                string EmailConteudo = "";

                string EmailUsuario = "";
                string EmailSenha = "";

                string EmailNome = "";
                string EmailServidor = "";
                int? EmailPorta = 1;
                bool? EmailSSL = false;

                MailMessage mailMessage = new MailMessage()
                {
                    From = new MailAddress(EmailUsuario, EmailNome),
                    Subject = EmailAssunto,
                    IsBodyHtml = true,
                    Priority = MailPriority.Normal,
                    Body = EmailConteudo
                };

                mailMessage.To.Add(new MailAddress(EmailDestinatario));

                using (SmtpClient smtp = new SmtpClient(EmailServidor, EmailPorta ?? 0))
                {
                    smtp.EnableSsl = EmailSSL ?? false;
                    smtp.UseDefaultCredentials = false;
                    smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                    //smtp.Timeout = 10;
                    smtp.Credentials = new NetworkCredential(EmailUsuario, EmailSenha);
                    await smtp.SendMailAsync(mailMessage);
                }

                return "Ok";
            }
            catch (Exception)
            {
                throw new Exception("Verifique se o email informado está correto.");
            }
        }
    }
}
