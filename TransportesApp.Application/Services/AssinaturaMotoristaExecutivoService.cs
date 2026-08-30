using TransportesApp.Application.DTOs;
using TransportesApp.Domain.Entities;
using TransportesApp.Domain.Enums;
using TransportesApp.Domain.Interfaces;

namespace TransportesApp.Application.Services
{
    // Assinatura da categoria Executivo do motorista — mesmo fluxo de pagamento do PlanoService
    // (Checkout Pro único via PagamentoService), mas sem catálogo: preço fixo único.
    public class AssinaturaMotoristaExecutivoService
    {
        public const decimal PrecoMensal = 49.90m;

        private readonly IAssinaturaMotoristaExecutivoRepository _assinaturaRepository;
        private readonly IMotoristaRepository _motoristaRepository;
        private readonly PagamentoService _pagamentoService;

        public AssinaturaMotoristaExecutivoService(
            IAssinaturaMotoristaExecutivoRepository assinaturaRepository,
            IMotoristaRepository motoristaRepository,
            PagamentoService pagamentoService)
        {
            _assinaturaRepository = assinaturaRepository;
            _motoristaRepository = motoristaRepository;
            _pagamentoService = pagamentoService;
        }

        public async Task<AssinaturaMotoristaExecutivoResponse?> ObterAtualAsync(Guid motoristaId)
        {
            var assinatura = await _assinaturaRepository.ObterAtivaPorMotoristaAsync(motoristaId)
                ?? await _assinaturaRepository.ObterPendentePorMotoristaAsync(motoristaId);

            return assinatura is null ? null : MapearParaResponse(assinatura);
        }

        // Cria a assinatura como PendentePagamento e devolve a URL de checkout do Mercado Pago — só
        // vira Ativa quando o PagamentoService confirmar o pagamento (webhook, ver PagamentosController).
        public async Task<AssinarExecutivoResponse> AssinarAsync(Guid motoristaId, int anoVeiculo, string emailPagador)
        {
            var motorista = await _motoristaRepository.ObterPorIdAsync(motoristaId)
                ?? throw new InvalidOperationException("Motorista não encontrado.");

            if (DateTime.UtcNow.Year - anoVeiculo > 3)
                throw new InvalidOperationException("A categoria Executivo exige veículo com até 3 anos de fabricação.");

            motorista.DefinirAnoVeiculo(anoVeiculo);
            await _motoristaRepository.AtualizarAsync(motorista);

            var ativa = await _assinaturaRepository.ObterAtivaPorMotoristaAsync(motoristaId);
            if (ativa is not null)
                return new AssinarExecutivoResponse(MapearParaResponse(ativa), null);

            // Mesma ideia do PlanoService: uma tentativa anterior não concluída é cancelada antes de
            // criar outra, pra manter só uma preference "viva" por vez.
            var pendente = await _assinaturaRepository.ObterPendentePorMotoristaAsync(motoristaId);
            if (pendente is not null)
            {
                pendente.Cancelar();
                await _assinaturaRepository.AtualizarAsync(pendente);
            }

            var nova = new AssinaturaMotoristaExecutivo(motoristaId);
            await _assinaturaRepository.AdicionarAsync(nova);

            var pagamento = await _pagamentoService.IniciarPagamentoAsync(
                motoristaId,
                TipoReferenciaPagamento.AssinaturaMotoristaExecutivo,
                nova.Id,
                PrecoMensal,
                "Assinatura Executivo — Vai na Boa",
                emailPagador);

            return new AssinarExecutivoResponse(MapearParaResponse(nova), pagamento.CheckoutUrl);
        }

        public async Task<bool> CancelarAsync(Guid motoristaId)
        {
            var atual = await _assinaturaRepository.ObterAtivaPorMotoristaAsync(motoristaId)
                ?? await _assinaturaRepository.ObterPendentePorMotoristaAsync(motoristaId);

            if (atual is null)
                return false;

            atual.Cancelar();
            await _assinaturaRepository.AtualizarAsync(atual);

            return true;
        }

        // Usado pelo CorridaService pra decidir se um motorista pode ver/aceitar corridas Executivo:
        // precisa ter assinatura ativa E veículo dentro do limite de idade.
        public async Task<bool> EstaElegivelAsync(Guid motoristaId)
        {
            var motorista = await _motoristaRepository.ObterPorIdAsync(motoristaId);
            if (motorista is null || !motorista.VeiculoElegivelParaExecutivo())
                return false;

            var ativa = await _assinaturaRepository.ObterAtivaPorMotoristaAsync(motoristaId);
            return ativa is not null;
        }

        private static AssinaturaMotoristaExecutivoResponse MapearParaResponse(AssinaturaMotoristaExecutivo assinatura)
            => new(assinatura.Id, PrecoMensal, assinatura.DataInicio, assinatura.Status);
    }
}
