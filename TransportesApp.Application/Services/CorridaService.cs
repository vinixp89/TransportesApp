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

        public CorridaService(
            ICorridaRepository corridaRepository,
            IMapsService mapsService,
            IPacoteCorridasRepository pacoteCorridasRepository)
        {
            _corridaRepository = corridaRepository;
            _mapsService = mapsService;
            _pacoteCorridasRepository = pacoteCorridasRepository;
        }

        public async Task<CorridaResponse> CriarAsync(CriarCorridasRequest request, Guid clienteId)
        {
            var avisos = new List<string>();

            var (origem, origemParcial) = await ResolverEnderecoAsync(request.Origem);
            if (origemParcial)
                avisos.Add("O endereço de origem foi localizado com correspondência parcial pelo Google Maps — confira se está correto.");

            var (destino, destinoParcial) = await ResolverEnderecoAsync(request.Destino);
            if (destinoParcial)
                avisos.Add("O endereço de destino foi localizado com correspondência parcial pelo Google Maps — confira se está correto.");

            var distanciaEstimadaKm = await _mapsService.CalcularDistanciaKmAsync(
                origem.Latitude, origem.Longitude, destino.Latitude, destino.Longitude);

            var faixa = FaixaDistancia.ClassificarPorDistancia(distanciaEstimadaKm);

            // Se a corrida é por pacote, valida que o pacote existe, é do cliente, é da faixa certa
            // e ainda tem corridas disponíveis — só depois disso ele é efetivamente consumido.
            var pacote = await ValidarPacoteAsync(request, clienteId, faixa);

            var corrida = new Corrida(
                clienteId: clienteId,
                origem: origem,
                destino: destino,
                distanciaEstimadaKm: distanciaEstimadaKm,
                faixa: faixa,
                tipoConsumo: request.TipoConsumo,
                pacoteCorridasId: request.PacoteCorridasId
            );

            await _corridaRepository.AdicionarAsync(corrida);

            if (pacote is not null)
            {
                pacote.UsarCorrida();
                await _pacoteCorridasRepository.AtualizarAsync(pacote);
            }

            return MapearParaResponse(corrida, avisos.Count > 0 ? avisos : null);
        }

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

            corrida.AtribuirMotorista(motoristaId);

            await _corridaRepository.AtualizarAsync(corrida);

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

            return new FinalizarCorridaResponse(estourouFaixa, MapearParaResponse(corrida));
        }

        public async Task<CorridaResponse?> CancelarAsync(Guid corridaId)
        {
            var corrida = await _corridaRepository.ObterPorIdAsync(corridaId);

            if (corrida is null)
                return null;

            corrida.Cancelar();

            await _corridaRepository.AtualizarAsync(corrida);

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
            return new CorridaResponse(
                corrida.Id,
                corrida.ClienteId,
                corrida.MotoristaId,
                MapearEnderecoResponse(corrida.Origem),
                MapearEnderecoResponse(corrida.Destino),
                corrida.DistanciaEstimadaKm,
                corrida.DistanciaRealKm,
                corrida.FaixaContratada,
                corrida.TipoConsumo,
                corrida.Status,
                corrida.DataSolicitacao,
                avisosEndereco
            );
        }
    }
}