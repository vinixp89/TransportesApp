using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace TransportesApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConfigController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public ConfigController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // Feature flag remota pro app Motorista: a tela de saldo/saque (recurso financeiro) só fica
        // visível quando isso vier true. Existe porque o Google Play exige conta tipo Organização
        // pra apps com recursos financeiros (ver Financial Features Declaration) — enquanto essa
        // verificação não sai, publicamos o app sem esse pedaço, e liberamos depois trocando só a
        // variável de ambiente no servidor (Features__CarteiraMotoristaLiberada), sem precisar de
        // novo build nem passar pela análise da loja de novo. Público de propósito, é só um booleano.
        [AllowAnonymous]
        [HttpGet("app")]
        public IActionResult App()
        {
            var valorConfig = _configuration["Features:CarteiraMotoristaLiberada"];
            var carteiraMotoristaLiberada = bool.TryParse(valorConfig, out var liberada) && liberada;

            return Ok(new { carteiraMotoristaLiberada });
        }

        // Texto dos termos de uso/contrato exibido na tela de aceite do cadastro (Cliente e
        // Motorista). RASCUNHO GENÉRICO — ainda NÃO revisado por advogado, não usar como termo
        // definitivo em produção sem essa revisão. "versao" existe pra permitir, no futuro, detectar
        // quando o texto mudou e pedir um novo aceite de quem já aceitou uma versão anterior (ainda
        // não implementado — Cliente/Motorista só guardam TermosAceitos/DataAceiteTermos, sem qual
        // versão foi aceita).
        [AllowAnonymous]
        [HttpGet("termos")]
        public IActionResult Termos()
        {
            return Ok(new { versao = "rascunho-1", texto = TextoTermos });
        }

        private const string TextoTermos = """
            TERMOS DE USO E CONTRATO DE PRESTAÇÃO DE SERVIÇOS — VAI NA BOA (RASCUNHO)

            Este é um texto provisório, gerado automaticamente como ponto de partida, e AINDA NÃO
            passou por revisão jurídica. Não deve ser considerado válido como termo definitivo até
            que um advogado o revise e aprove.

            1. OBJETO
            A plataforma Vai na Boa conecta clientes que desejam contratar corridas de transporte
            individual a motoristas parceiros cadastrados, atuando como intermediária tecnológica
            entre as partes.

            2. CADASTRO
            2.1. O usuário declara que as informações fornecidas no cadastro (incluindo documentos,
            fotos e dados de contato) são verdadeiras, completas e atualizadas.
            2.2. O cadastro pode ser suspenso ou cancelado em caso de informações falsas, uso
            indevido da plataforma ou violação destes termos.

            3. RESPONSABILIDADES DO MOTORISTA
            3.1. O motorista é responsável por possuir habilitação válida, documentação do veículo em
            dia e seguro conforme exigido pela legislação aplicável.
            3.2. O motorista atua como prestador de serviço autônomo, não havendo vínculo
            empregatício com a Vai na Boa.

            4. RESPONSABILIDADES DO CLIENTE
            4.1. O cliente se compromete a fornecer endereços corretos e a tratar o motorista com
            respeito durante a prestação do serviço.

            5. PAGAMENTOS
            5.1. Os valores das corridas, planos e pacotes são exibidos no aplicativo antes da
            confirmação e podem ser alterados a qualquer momento para novas contratações.

            6. PRIVACIDADE
            6.1. O tratamento de dados pessoais segue a Política de Privacidade da plataforma,
            disponível em vainaboamobilidade.com.br/privacidade.

            7. CANCELAMENTO E RESCISÃO
            7.1. Qualquer das partes pode encerrar sua conta a qualquer momento pelas configurações
            do aplicativo, observadas as obrigações já assumidas até o encerramento.

            8. DISPOSIÇÕES GERAIS
            8.1. Este documento pode ser atualizado periodicamente. Alterações relevantes serão
            comunicadas dentro do aplicativo.

            Ao aceitar estes termos, você declara ter lido e concordado com as condições acima.
            """;
    }
}
