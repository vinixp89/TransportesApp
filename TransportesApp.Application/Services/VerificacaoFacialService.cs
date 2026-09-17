using TransportesApp.Application.DTOs;
using TransportesApp.Domain.Entities;
using TransportesApp.Domain.Enums;
using TransportesApp.Domain.Interfaces;

namespace TransportesApp.Application.Services
{
    // Auditoria de reconhecimento facial — NUNCA lança exceção pro chamador (ver comentário em
    // VerificacaoFacial e IFaceMatchGateway): mesmo se a comparação falhar, o registro fica salvo
    // como "não verificado" pro Admin revisar depois. Quem lida com os arquivos em disco (leitura
    // dos bytes, salvar a foto capturada) é o CorridasController, mesmo padrão de
    // MotoristasController.EnviarFotos — aqui só existe a regra de persistência + comparação.
    public class VerificacaoFacialService
    {
        private readonly IVerificacaoFacialRepository _verificacaoFacialRepository;
        private readonly IMotoristaRepository _motoristaRepository;
        private readonly IFaceMatchGateway _faceMatchGateway;

        public VerificacaoFacialService(
            IVerificacaoFacialRepository verificacaoFacialRepository,
            IMotoristaRepository motoristaRepository,
            IFaceMatchGateway faceMatchGateway)
        {
            _verificacaoFacialRepository = verificacaoFacialRepository;
            _motoristaRepository = motoristaRepository;
            _faceMatchGateway = faceMatchGateway;
        }

        public async Task RegistrarAsync(
            Guid corridaId,
            Guid motoristaId,
            MomentoVerificacaoFacial momento,
            string fotoUrlRelativa,
            byte[]? fotoReferenciaBytes,
            byte[] fotoCapturadaBytes)
        {
            var verificacao = new VerificacaoFacial(corridaId, motoristaId, momento, fotoUrlRelativa);
            await _verificacaoFacialRepository.AdicionarAsync(verificacao);

            if (fotoReferenciaBytes is null)
            {
                verificacao.RegistrarErro("Motorista sem selfie de cadastro pra comparar.");
                await _verificacaoFacialRepository.AtualizarAsync(verificacao);
                return;
            }

            var resultado = await _faceMatchGateway.CompararAsync(fotoReferenciaBytes, fotoCapturadaBytes);

            if (resultado.Sucesso)
                verificacao.RegistrarResultado(resultado.Similaridade);
            else
                verificacao.RegistrarErro(resultado.MensagemErro ?? "Falha desconhecida na comparação facial.");

            await _verificacaoFacialRepository.AtualizarAsync(verificacao);
        }

        public async Task<IEnumerable<VerificacaoFacialAdminResponse>> ListarRecentesAsync(int quantidade)
        {
            var verificacoes = await _verificacaoFacialRepository.ListarRecentesAsync(quantidade);

            var respostas = new List<VerificacaoFacialAdminResponse>();

            foreach (var verificacao in verificacoes)
            {
                var motorista = await _motoristaRepository.ObterPorIdAsync(verificacao.MotoristaId);

                respostas.Add(new VerificacaoFacialAdminResponse(
                    verificacao.Id,
                    verificacao.CorridaId,
                    verificacao.MotoristaId,
                    motorista?.Nome ?? "(motorista removido)",
                    verificacao.Momento,
                    verificacao.Processada,
                    verificacao.Similaridade,
                    verificacao.Confere,
                    verificacao.ErroProcessamento,
                    verificacao.DataHora));
            }

            return respostas;
        }

        // Caminho relativo da foto capturada numa verificação específica — usado só internamente
        // pelo endpoint de streaming autenticado (ver AdminVerificacaoFacialController), igual
        // MotoristaService.ObterCaminhoFotoSelfieAsync.
        public async Task<string?> ObterCaminhoFotoAsync(Guid verificacaoId)
        {
            var verificacao = await _verificacaoFacialRepository.ObterPorIdAsync(verificacaoId);
            return verificacao?.FotoUrl;
        }
    }
}
