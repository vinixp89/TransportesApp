using TransportesApp.Application.DTOs;
using TransportesApp.Domain.Entities;
using TransportesApp.Domain.Enums;
using TransportesApp.Domain.Interfaces;
using TransportesApp.Domain.ValueObjects;

namespace TransportesApp.Application.Services
{
    public class CorridaService
    {
        private readonly ICorridaRepository _corridaRepository;
        private readonly IMapsService _mapsService;
        private readonly IPacoteCorridasRepository _pacoteCorridasRepository;
        private readonly IMotoristaRepository _motoristaRepository;
        private readonly IAssinaturaPlanoRepository _assinaturaPlanoRepository;
        private readonly ICarteiraRepository _carteiraRepository;
        private readonly ITransacaoCarteiraRepository _transacaoCarteiraRepository;

        public CorridaService(
            ICorridaRepository corridaRepository,
            IMapsService mapsService,
            IPacoteCorridasRepository pacoteCorridasRepository,
            IMotoristaRepository motoristaRepository,
            IAssinaturaPlanoRepository assinaturaPlanoRepository,
            ICarteiraRepository carteiraRepository,
            ITransacaoCarteiraRepository transacaoCarteiraRepository)
        {
            _corridaRepository = corridaRepository;
            _mapsService = mapsService;
            _pacoteCorridasRepository = pacoteCorridasRepository;
            _motoristaRepository = motoristaRepository;
            _assinaturaPlanoRepository = assinaturaPlanoRepository;
            _carteiraRepository = carteiraRepository;
            _transacaoCarteiraRepository = transacaoCarteiraRepository;
        }

        public async Task<CorridaResponse> CriarAsync(CriarCorridasRequest request, Guid clienteId)
        {
            var rota = await CalcularRotaAsync(request.Origem, request.Destino);

            // Se a corrida é por pacote, valida que o pacote existe, é do cliente, é da faixa certa
            // e ainda tem corridas disponíveis — só depois disso ele é efetivamente consumido.
            var pacote = await ValidarPacoteAsync(request, clienteId, rota.Faixa);

            // Se a corrida é avulsa, valida que a carteira do cliente tem saldo suficiente pro valor
            // da faixa — só depois disso ela é efetivamente debitada (mesma ideia do pacote: nunca
            // debita antes de a corrida já estar persistida com sucesso).
            var carteiraAvulsa = await ValidarSaldoAvulsaAsync(request.TipoConsumo, clienteId, rota.Faixa.PrecoAvulso);

            // Assinatura ativa do cliente (se tiver) — usada tanto pra validar/consumir o benefício de
            // corrida grátis (TipoConsumo.BeneficioPlano) quanto pra registrar que uma corrida paga foi
            // feita no mês (o que libera esse benefício pra próxima vez). Busca uma vez só e reaproveita.
            var assinatura = await _assinaturaPlanoRepository.ObterAtivaPorClienteAsync(clienteId);

            if (request.TipoConsumo == TipoConsumo.BeneficioPlano)
                ValidarBeneficioPlano(assinatura, rota.Faixa);

            var corrida = new Corrida(
                clienteId: clienteId,
                origem: rota.Origem,
                destino: rota.Destino,
                distanciaEstimadaKm: rota.DistanciaKm,
                faixa: rota.Faixa,
                tipoConsumo: request.TipoConsumo,
                pacoteCorridasId: request.PacoteCorridasId
            );

            await _corridaRepository.AdicionarAsync(corrida);

            if (pacote is not null)
            {
                pacote.UsarCorrida();
                await _pacoteCorridasRepository.AtualizarAsync(pacote);
            }

            if (carteiraAvulsa is not null)
            {
                carteiraAvulsa.Debitar(rota.Faixa.PrecoAvulso);
                await _carteiraRepository.AtualizarAsync(carteiraAvulsa);

                var transacaoDebito = new TransacaoCarteira(
                    carteiraAvulsa.Id, TipoTransacaoCarteira.Debito, rota.Faixa.PrecoAvulso, $"Corrida avulsa — faixa {rota.Faixa.Cor}");
                await _transacaoCarteiraRepository.AdicionarAsync(transacaoDebito);
            }

            if (assinatura is not null)
            {
                var agora = DateTime.UtcNow;

                // Corrida grátis consome o benefício; qualquer outra (avulsa ou por pacote) conta como
                // "corrida paga" e libera o benefício pro resto do mês (ver AssinaturaPlano).
                if (request.TipoConsumo == TipoConsumo.BeneficioPlano)
                    assinatura.UsarBeneficioGratis(agora);
                else
                    assinatura.RegistrarCorridaPaga(agora);

                await _assinaturaPlanoRepository.AtualizarAsync(assinatura);
            }

            return MapearParaResponse(corrida, rota.Avisos.Count > 0 ? rota.Avisos : null);
        }

        // Valida que o cliente pode mesmo usar a corrida grátis do plano: precisa ter assinatura ativa,
        // o plano precisa ter esse benefício, a corrida solicitada precisa ser exatamente da cor que o
        // plano libera de graça, e o benefício precisa estar disponível (já fez 1 corrida paga no mês e
        // ainda não usou a grátis). Lançado antes de criar a corrida, igual ValidarPacoteAsync faz.
        private static void ValidarBeneficioPlano(AssinaturaPlano? assinatura, FaixaDistancia faixa)
        {
            if (assinatura is null)
                throw new InvalidOperationException("Você não tem uma assinatura ativa com esse benefício.");

            var plano = PlanoAssinatura.ObterPorTipo(assinatura.Tipo);

            if (plano.CorGratisPorMes is null)
                throw new InvalidOperationException($"O plano {plano.Nome} não inclui corrida grátis mensal.");

            if (plano.CorGratisPorMes != faixa.Cor)
                throw new InvalidOperationException(
                    $"O benefício do seu plano vale só pra corridas da faixa {plano.CorGratisPorMes}, mas essa corrida caiu na faixa {faixa.Cor}.");

            var agora = DateTime.UtcNow;

            if (!assinatura.CorridaPagaLiberouBeneficioNoMes(agora))
                throw new InvalidOperationException("Faça uma corrida paga (avulsa ou por pacote) neste mês pra liberar sua corrida grátis.");

            if (assinatura.BeneficioJaUsadoNoMes(agora))
                throw new InvalidOperationException("Você já usou sua corrida grátis deste mês — ela não é acumulável.");
        }

        // Calcula rota/faixa/valor sem persistir nada — pra tela de confirmação mostrar preço e faixa
        // antes do cliente efetivamente solicitar a corrida.
        public async Task<EstimarCorridaResponse> EstimarAsync(EstimarCorridaRequest request)
        {
            var rota = await CalcularRotaAsync(request.Origem, request.Destino);

            return new EstimarCorridaResponse(
                MapearEnderecoResponse(rota.Origem),
                MapearEnderecoResponse(rota.Destino),
                rota.DistanciaKm,
                rota.DuracaoMinutos,
                rota.Faixa.Cor,
                rota.Faixa.PrecoAvulso,
                rota.Avisos.Count > 0 ? rota.Avisos : null
            );
        }

        // Geocodifica origem/destino, calcula distância+duração via API de mapas e classifica a faixa.
        // Usado tanto por CriarAsync (que persiste) quanto por EstimarAsync (que só devolve o cálculo).
        private async Task<RotaCalculada> CalcularRotaAsync(EnderecoRequest origemRequest, EnderecoRequest destinoRequest)
        {
            var avisos = new List<string>();

            var (origem, origemParcial) = await ResolverEnderecoAsync(origemRequest);
            if (origemParcial)
                avisos.Add("O endereço de origem foi localizado com correspondência parcial pelo Google Maps — confira se está correto.");

            var (destino, destinoParcial) = await ResolverEnderecoAsync(destinoRequest);
            if (destinoParcial)
                avisos.Add("O endereço de destino foi localizado com correspondência parcial pelo Google Maps — confira se está correto.");

            var (distanciaKm, duracaoMinutos) = await _mapsService.CalcularRotaAsync(
                origem.Latitude, origem.Longitude, destino.Latitude, destino.Longitude);

            var faixa = FaixaDistancia.ClassificarPorDistancia(distanciaKm);

            return new RotaCalculada(origem, destino, distanciaKm, duracaoMinutos, faixa, avisos);
        }

        private sealed record RotaCalculada(
            Endereco Origem,
            Endereco Destino,
            double DistanciaKm,
            double? DuracaoMinutos,
            FaixaDistancia Faixa,
            List<string> Avisos
        );

        // Não consome o pacote aqui — só valida e devolve, pra só debitar depois que a corrida
        // já tiver sido persistida com sucesso (evita descontar corrida do pacote e a criação falhar depois).
        private async Task<PacoteCorridas?> ValidarPacoteAsync(CriarCorridasRequest request, Guid clienteId, FaixaDistancia faixa)
        {
            if (request.TipoConsumo != TipoConsumo.Pacote)
                return null;

            if (request.PacoteCorridasId is null)
                throw new InvalidOperationException("Informe o pacoteCorridasId para corridas com tipoConsumo Pacote.");

            var pacote = await _pacoteCorridasRepository.ObterPorIdAsync(request.PacoteCorridasId.Value);

            if (pacote is null)
                throw new InvalidOperationException("Pacote de corridas não encontrado.");

            if (pacote.ClienteId != clienteId)
                throw new InvalidOperationException("Este pacote de corridas não pertence a você.");

            if (pacote.Faixa != faixa.Cor)
                throw new InvalidOperationException(
                    $"Este pacote é da faixa {pacote.Faixa}, mas a corrida solicitada caiu na faixa {faixa.Cor}. " +
                    "Use um pacote da faixa correta ou solicite como corrida avulsa.");

            if (!pacote.TemCorridaDisponivel)
                throw new InvalidOperationException("Este pacote não tem corridas disponíveis. Compre um novo pacote ou solicite como corrida avulsa.");

            return pacote;
        }

        // Não debita aqui — só valida e devolve a carteira, pra só debitar depois que a corrida já
        // tiver sido persistida com sucesso (mesma ideia de ValidarPacoteAsync). Cliente sem carteira
        // ainda (nunca recarregou) cai no mesmo erro de saldo insuficiente, sem precisar de um caso
        // especial: saldo implícito é zero.
        private async Task<Carteira?> ValidarSaldoAvulsaAsync(TipoConsumo tipoConsumo, Guid clienteId, decimal valor)
        {
            if (tipoConsumo != TipoConsumo.Avulsa)
                return null;

            var carteira = await _carteiraRepository.ObterPorClienteIdAsync(clienteId);

            if (carteira is null || carteira.Saldo < valor)
                throw new InvalidOperationException(
                    "Saldo insuficiente na carteira para essa corrida avulsa. Recarregue sua carteira antes de pedir.");

            return carteira;
        }

        public async Task<CorridaResponse?> ObterPorIdAsync(Guid id)
        {
            var corrida = await _corridaRepository.ObterPorIdAsync(id);

            return corrida is null ? null : MapearParaResponse(corrida);
        }

        public async Task<IEnumerable<CorridaResponse>> ListarAsync()
        {
            var corridas = await _corridaRepository.ListarAsync();

            return corridas.Select(c => MapearParaResponse(c));
        }

        // Extrato de corridas do próprio motorista (mais recentes primeiro, ver CorridaRepository).
        public async Task<IEnumerable<CorridaResponse>> ListarPorMotoristaAsync(Guid motoristaId)
        {
            var corridas = await _corridaRepository.ListarPorMotoristaIdAsync(motoristaId);

            return corridas.Select(c => MapearParaResponse(c));
        }

        public async Task<CorridaResponse?> AtribuirMotoristaAsync(Guid corridaId, Guid motoristaId)
        {
            var corrida = await _corridaRepository.ObterPorIdAsync(corridaId);

            if (corrida is null)
                return null;

            var motorista = await _motoristaRepository.ObterPorIdAsync(motoristaId);

            if (motorista is null)
                throw new InvalidOperationException("Motorista não encontrado.");

            // Já valida internamente que o motorista está Disponivel antes de mudar pra EmCorrida.
            motorista.IniciarCorrida();

            corrida.AtribuirMotorista(motoristaId);

            await _corridaRepository.AtualizarAsync(corrida);
            await _motoristaRepository.AtualizarAsync(motorista);

            return MapearParaResponse(corrida);
        }

        public async Task<CorridaResponse?> IniciarViagemAsync(Guid corridaId)
        {
            var corrida = await _corridaRepository.ObterPorIdAsync(corridaId);

            if (corrida is null)
                return null;

            corrida.IniciarViagem();

            await _corridaRepository.AtualizarAsync(corrida);

            return MapearParaResponse(corrida);
        }

        public async Task<FinalizarCorridaResponse?> FinalizarAsync(Guid corridaId, FinalizarCorridaRequest request)
        {
            var corrida = await _corridaRepository.ObterPorIdAsync(corridaId);

            if (corrida is null)
                return null;

            var estourouFaixa = corrida.FinalizarCorrida(request.DistanciaReal);

            await _corridaRepository.AtualizarAsync(corrida);

            if (corrida.MotoristaId is not null)
            {
                var motorista = await _motoristaRepository.ObterPorIdAsync(corrida.MotoristaId.Value);

                if (motorista is not null)
                {
                    motorista.FinalizarCorrida();
                    await _motoristaRepository.AtualizarAsync(motorista);
                }
            }

            return new FinalizarCorridaResponse(estourouFaixa, MapearParaResponse(corrida));
        }

        // sempreReembolsar vem do controller (ver CorridasController.Cancelar) e indica se quem está
        // cancelando NÃO é o cliente dono da corrida (motorista atribuído ou Admin) — nesse caso o
        // cancelamento sempre reverte o que já foi consumido, já que não foi o cliente quem desistiu.
        public async Task<CorridaResponse?> CancelarAsync(Guid corridaId, bool sempreReembolsar)
        {
            var corrida = await _corridaRepository.ObterPorIdAsync(corridaId);

            if (corrida is null)
                return null;

            var deveReverterConsumo = corrida.Cancelar(sempreReembolsar);

            await _corridaRepository.AtualizarAsync(corrida);

            if (deveReverterConsumo)
                await ReverterConsumoAsync(corrida);

            // Se já tinha motorista atribuído (e ele estava em corrida por causa dela), libera de volta.
            if (corrida.MotoristaId is not null)
            {
                var motorista = await _motoristaRepository.ObterPorIdAsync(corrida.MotoristaId.Value);

                if (motorista is not null && motorista.Status == StatusMotorista.EmCorrida)
                {
                    motorista.FinalizarCorrida();
                    await _motoristaRepository.AtualizarAsync(motorista);
                }
            }

            return MapearParaResponse(corrida);
        }

        // Desfaz o que foi consumido/debitado na criação da corrida (ver CriarAsync) — só chamado quando
        // Corrida.Cancelar() sinaliza que o cancelamento dá direito a reversão. Cada TipoConsumo sabe
        // devolver o que é seu; o valor usado pro estorno da carteira é o preço ATUAL da faixa (mesma
        // lógica de MapearParaResponse.valorReferencia) — como a tabela de preços é fixa em código
        // (ver FaixaDistancia), na prática é sempre o mesmo valor que foi debitado na criação.
        private async Task ReverterConsumoAsync(Corrida corrida)
        {
            switch (corrida.TipoConsumo)
            {
                case TipoConsumo.Pacote when corrida.PacoteCorridasId is not null:
                    var pacote = await _pacoteCorridasRepository.ObterPorIdAsync(corrida.PacoteCorridasId.Value);
                    if (pacote is not null)
                    {
                        pacote.DevolverCorrida();
                        await _pacoteCorridasRepository.AtualizarAsync(pacote);
                    }
                    break;

                case TipoConsumo.Avulsa:
                    var carteira = await _carteiraRepository.ObterPorClienteIdAsync(corrida.ClienteId);
                    if (carteira is not null)
                    {
                        var valor = FaixaDistancia.ObterPorCor(corrida.FaixaContratada).PrecoAvulso;
                        carteira.Estornar(valor);
                        await _carteiraRepository.AtualizarAsync(carteira);

                        var transacaoEstorno = new TransacaoCarteira(
                            carteira.Id, TipoTransacaoCarteira.Estorno, valor, $"Estorno — cancelamento de corrida avulsa ({corrida.FaixaContratada})");
                        await _transacaoCarteiraRepository.AdicionarAsync(transacaoEstorno);
                    }
                    break;

                case TipoConsumo.BeneficioPlano:
                    var assinatura = await _assinaturaPlanoRepository.ObterAtivaPorClienteAsync(corrida.ClienteId);
                    if (assinatura is not null)
                    {
                        assinatura.DevolverBeneficioGratis(DateTime.UtcNow);
                        await _assinaturaPlanoRepository.AtualizarAsync(assinatura);
                    }
                    break;
            }
        }

        // Sempre geocodifica o endereço em texto via Google Maps — o cliente não informa lat/long.
        // Devolve também se a correspondência foi parcial, pra sinalizar um possível endereço errado.
        private async Task<(Endereco Endereco, bool CorrespondenciaParcial)> ResolverEnderecoAsync(EnderecoRequest request)
        {
            var enderecoTexto = FormatarEnderecoParaGeocodificacao(request);
            var (latitude, longitude, correspondenciaParcial) = await _mapsService.GeocodificarAsync(enderecoTexto);

            var endereco = new Endereco(
                logradouro: request.Logradouro,
                numero: request.Numero,
                bairro: request.Bairro,
                cidade: request.Cidade,
                estado: request.Estado,
                latitude: latitude,
                longitude: longitude,
                complemento: request.Complemento
            );

            return (endereco, correspondenciaParcial);
        }

        private static string FormatarEnderecoParaGeocodificacao(EnderecoRequest request)
            => $"{request.Logradouro}, {request.Numero}, {request.Bairro}, {request.Cidade}, {request.Estado}, Brasil";

        private static EnderecoResponse MapearEnderecoResponse(Endereco endereco)
        {
            return new EnderecoResponse(
                endereco.Logradouro,
                endereco.Numero,
                endereco.Bairro,
                endereco.Cidade,
                endereco.Estado,
                endereco.Latitude,
                endereco.Longitude,
                endereco.Complemento
            );
        }

        private static CorridaResponse MapearParaResponse(Corrida corrida, IReadOnlyList<string>? avisosEndereco = null)
        {
            // ValorReferencia não é gravado na corrida — é derivado da faixa contratada na hora de responder,
            // pra sempre refletir a tabela de preços atual em vez de um valor que poderia ficar desatualizado.
            var valorReferencia = FaixaDistancia.ObterPorCor(corrida.FaixaContratada).PrecoAvulso;

            return new CorridaResponse(
                corrida.Id,
                corrida.ClienteId,
                corrida.MotoristaId,
                MapearEnderecoResponse(corrida.Origem),
                MapearEnderecoResponse(corrida.Destino),
                corrida.DistanciaEstimadaKm,
                corrida.DistanciaRealKm,
                corrida.FaixaContratada,
                valorReferencia,
                corrida.TipoConsumo,
                corrida.Status,
                corrida.DataSolicitacao,
                avisosEndereco
            );
        }
    }
}