using TransportesApp.Application.DTOs;
using TransportesApp.Domain.Entities;
using TransportesApp.Domain.Interfaces;
using TransportesApp.Domain.ValueObjects;

namespace TransportesApp.Application.Services
{
    public class PacoteCorridasService
    {
        private readonly IPacoteCorridasRepository _pacoteCorridasRepository;

        public PacoteCorridasService(IPacoteCorridasRepository pacoteCorridasRepository)
        {
            _pacoteCorridasRepository = pacoteCorridasRepository;
        }

        // Não depende do repositório — é só a tabela de preços do domínio, montada pra exibição.
        public IEnumerable<CatalogoPacoteResponse> ObterCatalogo()
        {
            return FaixaDistancia.ListarTodas().Select(faixa => new CatalogoPacoteResponse(
                faixa.Cor,
                faixa.PrecoAvulso,
                FaixaDistancia.TamanhosPacoteDisponiveis
                    .Select(quantidade => new TamanhoPacoteResponse(quantidade, faixa.ObterPrecoPacote(quantidade)))
                    .ToList()
            ));
        }

        public async Task<PacoteCorridasResponse> CriarAsync(CriarPacoteCorridasRequest request, Guid clienteId)
        {
            var pacote = new PacoteCorridas(clienteId, request.Faixa, request.Quantidade);

            await _pacoteCorridasRepository.AdicionarAsync(pacote);

            return MapearParaResponse(pacote);
        }

        public async Task<PacoteCorridasResponse?> ObterPorIdAsync(Guid id)
        {
            var pacote = await _pacoteCorridasRepository.ObterPorIdAsync(id);

            return pacote is null ? null : MapearParaResponse(pacote);
        }

        public async Task<IEnumerable<PacoteCorridasResponse>> ListarPorClienteAsync(Guid clienteId)
        {
            var pacotes = await _pacoteCorridasRepository.ListarPorClienteAsync(clienteId);

            return pacotes.Select(MapearParaResponse);
        }

        private static PacoteCorridasResponse MapearParaResponse(PacoteCorridas pacote)
        {
            return new PacoteCorridasResponse(
                pacote.Id,
                pacote.ClienteId,
                pacote.Faixa,
                pacote.QuantidadeTotal,
                pacote.QuantidadeUsada,
                pacote.QuantidadeRestante,
                pacote.PrecoPago,
                pacote.DataCompra
            );
        }
    }
}
