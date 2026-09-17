using TransportesApp.Domain.Enums;

namespace TransportesApp.Domain.Entities
{
    // Registro de auditoria de reconhecimento facial do motorista — uma selfie tirada no início e
    // outra no fim de cada corrida, comparada contra a selfie do cadastro (ver
    // Motorista.FotoSelfieUrl). NUNCA bloqueia a viagem: mesmo se a comparação falhar (serviço fora,
    // sem crédito configurado, nenhum rosto detectado) o registro fica salvo com Processada=false e
    // ErroProcessamento preenchido, só pra revisão manual do Admin depois.
    public class VerificacaoFacial
    {
        public Guid Id { get; private set; }
        public Guid CorridaId { get; private set; }
        public Guid MotoristaId { get; private set; }
        public MomentoVerificacaoFacial Momento { get; private set; }
        public string FotoUrl { get; private set; } = default!;
        public bool Processada { get; private set; }
        public double? Similaridade { get; private set; }
        public bool? Confere { get; private set; }
        public string? ErroProcessamento { get; private set; }
        public DateTime DataHora { get; private set; }

        // Abaixo desse score (0-100) de similaridade, Confere fica false — mesmo limiar recomendado
        // pela AWS Rekognition pra comparação de rosto único (CompareFaces).
        private const double LimiarSimilaridade = 80;

        protected VerificacaoFacial() { }

        public VerificacaoFacial(Guid corridaId, Guid motoristaId, MomentoVerificacaoFacial momento, string fotoUrl)
        {
            Id = Guid.NewGuid();
            CorridaId = corridaId;
            MotoristaId = motoristaId;
            Momento = momento;
            FotoUrl = fotoUrl;
            Processada = false;
            DataHora = DateTime.UtcNow;
        }

        public void RegistrarResultado(double similaridade)
        {
            Processada = true;
            Similaridade = similaridade;
            Confere = similaridade >= LimiarSimilaridade;
            ErroProcessamento = null;
        }

        public void RegistrarErro(string motivo)
        {
            Processada = false;
            Similaridade = null;
            Confere = null;
            ErroProcessamento = motivo;
        }
    }
}
