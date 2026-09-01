using TransportesApp.Application.DTOs;
using TransportesApp.Domain.Entities;
using TransportesApp.Domain.Enums;
using TransportesApp.Domain.Interfaces;
using TransportesApp.Domain.ValueObjects;

namespace TransportesApp.Application.Services
{
    // Um cliente doa uma corrida (de uma faixa específica) pra outro cliente, achado por e-mail exato.
    // O valor sai da carteira de quem doa (mesmo preço avulso da faixa) e vira um pacote de 1 corrida
    // pronto pra usar na conta de quem recebe — não depende de nenhum dos dois já ter pacote comprado.
    public class DoacaoService
    {
        private readonly IClienteRepository _clienteRepository;
        private readonly ICarteiraRepository _carteiraRepository;
        private readonly ITransacaoCarteiraRepository _transacaoCarteiraRepository;
        private readonly IPacoteCorridasRepository _pacoteCorridasRepository;

        public DoacaoService(
            IClienteRepository clienteRepository,
            ICarteiraRepository carteiraRepository,
            ITransacaoCarteiraRepository transacaoCarteiraRepository,
            IPacoteCorridasRepository pacoteCorridasRepository)
        {
            _clienteRepository = clienteRepository;
            _carteiraRepository = carteiraRepository;
            _transacaoCarteiraRepository = transacaoCarteiraRepository;
            _pacoteCorridasRepository = pacoteCorridasRepository;
        }

        // clienteLogadoId exclui o próprio cliente do resultado — buscar o próprio e-mail não deveria
        // "achar" ninguém pra doar (ver DoarAsync, que também bloqueia doar pra si mesmo, mas aqui já
        // evita a confusão de aparecer como resultado de busca).
        public async Task<BuscarClienteResponse?> BuscarPorEmailAsync(string email, Guid clienteLogadoId)
        {
            var cliente = await _clienteRepository.ObterPorEmailAsync(email);

            if (cliente is null || cliente.Id == clienteLogadoId)
                return null;

            return new BuscarClienteResponse(cliente.Id, cliente.Nome, cliente.Email);
        }

        // Duas origens possíveis pro valor doado (ver DoarCorridaRequest.PacoteCorridasId):
        // - Carteira (padrão, PacoteCorridasId nulo): debita o preço avulso da faixa do saldo em reais.
        // - Um pacote que o doador já tem (PacoteCorridasId informado): consome 1 corrida desse pacote,
        //   sem mexer na carteira — ele já pagou por essa corrida quando comprou o pacote.
        // Em ambos os casos, o resultado final é o mesmo: um pacote de 1 corrida novo pra quem recebe.
        public async Task<DoarCorridaResponse> DoarAsync(Guid doadorId, DoarCorridaRequest request)
        {
            var destinatario = await _clienteRepository.ObterPorEmailAsync(request.EmailDestinatario)
                ?? throw new InvalidOperationException("Não encontramos nenhum cliente com esse e-mail.");

            if (destinatario.Id == doadorId)
                throw new InvalidOperationException("Você não pode doar uma corrida pra você mesmo.");

            var valor = FaixaDistancia.ObterPorCor(request.Faixa).PrecoAvulso;
            int? quantidadeRestantePacote = null;
            decimal saldoCarteira;

            if (request.PacoteCorridasId is Guid pacoteId)
            {
                var pacote = await _pacoteCorridasRepository.ObterPorIdAsync(pacoteId)
                    ?? throw new InvalidOperationException("Pacote de corridas não encontrado.");

                if (pacote.ClienteId != doadorId)
                    throw new InvalidOperationException("Este pacote de corridas não pertence a você.");

                if (pacote.Faixa != request.Faixa)
                    throw new InvalidOperationException(
                        $"Este pacote é da faixa {pacote.Faixa}, mas você escolheu doar uma corrida da faixa {request.Faixa}.");

                // Pacote promocional (ganho na promoção de lançamento) não pode ser repassado — senão
                // dava pra "vender"/transferir o brinde em vez de usá-lo você mesmo.
                if (pacote.EhPromocional)
                    throw new InvalidOperationException("Corridas ganhas em promoção não podem ser doadas.");

                if (!pacote.TemCorridaDisponivel)
                    throw new InvalidOperationException("Este pacote não tem corridas disponíveis pra doar.");

                pacote.UsarCorrida();
                await _pacoteCorridasRepository.AtualizarAsync(pacote);
                quantidadeRestantePacote = pacote.QuantidadeRestante;

                // Só pra devolver o saldo atual na resposta (não muda nada aqui) — mantém o mesmo
                // formato de resposta pras duas origens.
                var carteiraAtual = await _carteiraRepository.ObterPorClienteIdAsync(doadorId);
                saldoCarteira = carteiraAtual?.Saldo ?? 0;
            }
            else
            {
                var carteira = await _carteiraRepository.ObterPorClienteIdAsync(doadorId);

                if (carteira is null || carteira.Saldo < valor)
                    throw new InvalidOperationException(
                        "Saldo insuficiente na carteira para doar essa corrida. Recarregue sua carteira antes de doar.");

                carteira.Debitar(valor);
                await _carteiraRepository.AtualizarAsync(carteira);

                var transacao = new TransacaoCarteira(
                    carteira.Id, TipoTransacaoCarteira.Debito, valor, $"Doação de corrida ({request.Faixa}) para {destinatario.Email}");
                await _transacaoCarteiraRepository.AdicionarAsync(transacao);

                saldoCarteira = carteira.Saldo;
            }

            var pacoteDoado = PacoteCorridas.CriarDoacao(destinatario.Id, request.Faixa);
            await _pacoteCorridasRepository.AdicionarAsync(pacoteDoado);

            return new DoarCorridaResponse(destinatario.Nome, request.Faixa, valor, saldoCarteira, quantidadeRestantePacote);
        }
    }
}
