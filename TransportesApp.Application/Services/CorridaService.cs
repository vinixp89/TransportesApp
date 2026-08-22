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

        public CorridaService(
            ICorridaRepository corridaRepository,
            IMapsService mapsService,
            IPacoteCorridasRepository pacoteCorridasRepository,
            IMotoristaRepository motoristaRepository,
            IAssinaturaPlanoRepository assinaturaPlanoRepository)
        {
            _corridaRepository = corridaRepository;
            _mapsService = mapsService;
            _pacoteCorridasRepository = pacoteCorridasRepository;
            _motoristaRepository = motoristaRepository;
            _assinaturaPlanoRepository = assinaturaPlanoRepository;
        }

        public async Task<CorridaResponse> CriarAsync(CriarCorridasRequest request, Guid clienteId)
        {
            var rota = await CalcularRotaAsync(request.Origem, request.Destino);

            // Se a corrida é por pacote, valida que o pacote existe, é do cliente, é da faixa certa
            // e ainda tem corridas disponíveis — só depois disso ele é efetivamente consumido.
            var pacote = await ValidarPacoteAsync(request, clienteId, rota.Faixa);

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

        public async Task<CorridaResponse?> CancelarAsync(Guid corridaId)
        {
            var corrida = await _corridaRepository.ObterPorIdAsync(corridaId);

            if (corrida is null)
                return null;

            corrida.Cancelar();

            await _corridaRepository.AtualizarAsync(corrida);

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