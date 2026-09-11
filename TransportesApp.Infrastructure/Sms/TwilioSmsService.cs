using Microsoft.Extensions.Configuration;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using TransportesApp.Domain.Interfaces;

namespace TransportesApp.Infrastructure.Sms
{
    // Implementação de ISmsService usando o SDK oficial do Twilio (pacote NuGet "Twilio"). Mesmo
    // princípio do MercadoPagoGateway: TwilioClient.Init é chamado uma vez só (aqui, de forma
    // preguiçosa na primeira chamada, porque diferente do Mercado Pago não temos um hook de startup
    // dedicado pra isso) e o SDK guarda a credencial como estado estático global.
    public class TwilioSmsService : ISmsService
    {
        private readonly IConfiguration _configuration;
        private static bool _inicializado;
        private static readonly object TravaInicializacao = new();

        public TwilioSmsService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task EnviarAsync(string numeroDestino, string mensagem)
        {
            GarantirInicializado();

            var numeroRemetente = _configuration["Twilio:NumeroRemetente"];
            if (string.IsNullOrWhiteSpace(numeroRemetente))
                throw new InvalidOperationException("Número remetente do Twilio não configurado (Twilio:NumeroRemetente).");

            await MessageResource.CreateAsync(
                body: mensagem,
                from: new PhoneNumber(numeroRemetente),
                to: new PhoneNumber(NormalizarParaE164(numeroDestino))
            );
        }

        // Aceita número já em E.164 (+55...) ou em formato comum brasileiro (com/sem DDI 55, com/sem
        // formatação) — sempre devolve E.164, que é o único formato que a API do Twilio aceita.
        private static string NormalizarParaE164(string numero)
        {
            var apenasDigitos = new string(numero.Where(char.IsDigit).ToArray());

            if (numero.TrimStart().StartsWith('+'))
                return $"+{apenasDigitos}";

            // Número local (DDD + telefone) tem 10 ou 11 dígitos no Brasil — sem esses dígitos, já
            // deve ter vindo com o DDI 55 embutido.
            if (apenasDigitos.Length is 10 or 11)
                return $"+55{apenasDigitos}";

            return $"+{apenasDigitos}";
        }

        private void GarantirInicializado()
        {
            if (_inicializado)
                return;

            lock (TravaInicializacao)
            {
                if (_inicializado)
                    return;

                var accountSid = _configuration["Twilio:AccountSid"];
                var authToken = _configuration["Twilio:AuthToken"];

                if (string.IsNullOrWhiteSpace(accountSid) || string.IsNullOrWhiteSpace(authToken))
                    throw new InvalidOperationException(
                        "Credenciais do Twilio não configuradas (Twilio:AccountSid / Twilio:AuthToken nos User Secrets — ver Program.cs).");

                TwilioClient.Init(accountSid, authToken);
                _inicializado = true;
            }
        }
    }
}
