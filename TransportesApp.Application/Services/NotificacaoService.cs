using TransportesApp.Application.DTOs;
using TransportesApp.Domain.Entities;
using TransportesApp.Domain.Enums;
using TransportesApp.Domain.Interfaces;

namespace TransportesApp.Application.Services
{
    // Caixa de entrada in-app do cliente — separada do envio de push (Expo/Firebase), que é
    // "dispare e esqueça". Aqui fica um histórico persistido que o cliente pode voltar a ver,
    // marcar como lida etc. Outros services (ex: DoacaoService) chamam CriarAsync quando algo
    // relevante acontece pro cliente.
    public class NotificacaoService
    {
        private readonly INotificacaoRepository _notificacaoRepository;

        public NotificacaoService(INotificacaoRepository notificacaoRepository)
        {
            _notificacaoRepository = notificacaoRepository;
        }

        public async Task CriarAsync(Guid clienteId, string titulo, string mensagem, TipoNotificacao tipo)
        {
            var notificacao = new Notificacao(clienteId, titulo, mensagem, tipo);
            await _notificacaoRepository.AdicionarAsync(notificacao);
        }

        public async Task<IEnumerable<NotificacaoResponse>> ListarAsync(Guid clienteId)
        {
            var notificacoes = await _notificacaoRepository.ListarPorClienteAsync(clienteId);
            return notificacoes.Select(MapearParaResponse);
        }

        public async Task<ContagemNaoLidasResponse> ContarNaoLidasAsync(Guid clienteId)
        {
            var quantidade = await _notificacaoRepository.ContarNaoLidasPorClienteAsync(clienteId);
            return new ContagemNaoLidasResponse(quantidade);
        }

        // Retorna false se a notificação não existe ou não pertence a esse cliente — quem chama
        // (controller) trata isso como 404, sem revelar se o Id existe pra outra pessoa.
        public async Task<bool> MarcarComoLidaAsync(Guid id, Guid clienteId)
        {
            var notificacao = await _notificacaoRepository.ObterPorIdAsync(id);

            if (notificacao is null || notificacao.ClienteId != clienteId)
                return false;

            notificacao.MarcarComoLida();
            await _notificacaoRepository.AtualizarAsync(notificacao);

            return true;
        }

        public async Task MarcarTodasComoLidasAsync(Guid clienteId)
        {
            var notificacoes = await _notificacaoRepository.ListarPorClienteAsync(clienteId);

            foreach (var notificacao in notificacoes.Where(n => !n.Lida))
            {
                notificacao.MarcarComoLida();
                await _notificacaoRepository.AtualizarAsync(notificacao);
            }
        }

        private static NotificacaoResponse MapearParaResponse(Notificacao notificacao)
        {
            return new NotificacaoResponse(
                notificacao.Id,
                notificacao.Titulo,
                notificacao.Mensagem,
                notificacao.Tipo,
                notificacao.Lida,
                notificacao.DataCriacao
            );
        }
    }
}
