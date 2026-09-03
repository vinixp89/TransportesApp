using TransportesApp.Application.DTOs;
using TransportesApp.Domain.Entities;
using TransportesApp.Domain.Interfaces;
using TransportesApp.Domain.ValueObjects;

namespace TransportesApp.Application.Services
{
    public class PacoteCorridasService
    {
        private readonly IPacoteCorridasRepository _pacoteCorridasRepository;
        private readonly IAssinaturaPlanoRepository _assinaturaPlanoRepository;

        public PacoteCorridasService(
            IPacoteCorridasRepository pacoteCorridasRepository,
            IAssinaturaPlanoRepository assinaturaPlanoRepository)
        {
            _pacoteCorridasRepository = pacoteCorridasRepository;
            _assinaturaPlanoRepository = assinaturaPlanoRepository;
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
            var percentualDesconto = await ObterPercentualDescontoAsync(clienteId);

            var pacote = new PacoteCorridas(clienteId, request.Faixa, request.Quantidade, percentualDesconto);

            await _pacoteCorridasRepository.AdicionarAsync(pacote);

            return MapearParaResponse(pacote);
        }

        // Desconto do plano de assinatura ativo do cliente, se tiver (ver PlanoAssinatura.PercentualDescontoPacotes).
        // 0 pra quem não tem assinatura ativa ou cujo plano não dá desconto (ex: Básico).
        private async Task<decimal> ObterPercentualDescontoAsync(Guid clienteId)
        {
            var assinatura = await _assinaturaPlanoRepository.ObterAtivaPorClienteAsync(clienteId);

            if (assinatura is null)
                return 0m;

            return PlanoAssinatura.ObterPorTipo(assinatura.Tipo).PercentualDescontoPacotes;
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
