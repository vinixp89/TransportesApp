namespace TransportesApp.Application.Email
{
    public record EmailMensagem(string Assunto, string CorpoHtml);

    public static class EmailTemplates
    {
        public static EmailMensagem BoasVindasCliente(string nome)
        {
            var assunto = "Bem-vindo(a) ao Vai na Boa!";

            var corpoHtml = $@"
                <div style=""font-family: Arial, sans-serif; max-width: 480px; margin: 0 auto; color: #222;"">
                    <h2>Olá, {nome}!</h2>
                    <p>Seu cadastro no <strong>Vai na Boa</strong> foi concluído com sucesso.</p>
                    <p>A partir de agora você já pode solicitar corridas direto pelo app, com praticidade e segurança.</p>
                    <p>Qualquer dúvida, é só entrar em contato com a gente.</p>
                    <p>Boa viagem!<br/>Equipe Vai na Boa</p>
                </div>";

            return new EmailMensagem(assunto, corpoHtml);
        }

        public static EmailMensagem BoasVindasMotorista()
        {
            var assunto = "Bem-vindo(a) à frota Vai na Boa Motorista!";

            var corpoHtml = @"
                <div style=""font-family: Arial, sans-serif; max-width: 480px; margin: 0 auto; color: #222;"">
                    <h2>Bem-vindo(a) à frota!</h2>
                    <p>Seu cadastro como motorista no <strong>Vai na Boa</strong> foi concluído com sucesso.</p>
                    <p>Fique de olho no app: assim que estiver disponível, você já pode começar a aceitar corridas.</p>
                    <p>Boas corridas!<br/>Equipe Vai na Boa</p>
                </div>";

            return new EmailMensagem(assunto, corpoHtml);
        }
    }
}
