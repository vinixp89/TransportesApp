using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using TransportesApp.Domain.Interfaces;

namespace TransportesApp.Infrastructure.Email
{
    public class SmtpEmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public SmtpEmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task EnviarAsync(string destinatario, string assunto, string corpoHtml)
        {
            var host = _configuration["Smtp:Host"];
            var port = int.Parse(_configuration["Smtp:Port"] ?? "587");
            var usuario = _configuration["Smtp:User"];
            var senha = _configuration["Smtp:Password"];
            var remetente = _configuration["Smtp:From"];
            var enableSsl = bool.Parse(_configuration["Smtp:EnableSsl"] ?? "true");

            using var mensagem = new MailMessage
            {
                From = new MailAddress(string.IsNullOrWhiteSpace(remetente) ? usuario! : remetente, "Vai na Boa"),
                Subject = assunto,
                Body = corpoHtml,
                IsBodyHtml = true
            };
            mensagem.To.Add(destinatario);

            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(usuario, senha),
                EnableSsl = enableSsl
            };

            await client.SendMailAsync(mensagem);
        }
    }
}
