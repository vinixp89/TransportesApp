using TransportesApp.Domain.Enums;

namespace TransportesApp.Domain.Entities
{
    // Avaliação de uma corrida finalizada — cada corrida pode ter até 2 (uma do Cliente sobre o
    // Motorista, outra do Motorista sobre o Cliente), nunca as duas do mesmo lado (ver índice único em
    // AvaliacaoConfiguration). AvaliadoId guarda direto o Id de quem foi avaliado — MotoristaId quando
    // AutorTipo é Cliente, ClienteId quando é Motorista — pra recalcular a média (ver
    // AvaliacaoService.AtualizarMediaAsync) sem precisar voltar na Corrida a cada vez.
    public class Avaliacao
    {
        public const int TamanhoMaximoComentario = 500;

        public Guid Id { get; private set; }
        public Guid CorridaId { get; private set; }
        public TipoUsuario AutorTipo { get; private set; }
        public Guid AvaliadoId { get; private set; }
        public int Nota { get; private set; }
        public string? Comentario { get; private set; }
        public DateTime DataAvaliacao { get; private set; }

        protected Avaliacao() { }

        public Avaliacao(Guid corridaId, TipoUsuario autorTipo, Guid avaliadoId, int nota, string? comentario)
        {
            if (nota is < 1 or > 5)
                throw new ArgumentException("A nota precisa ser de 1 a 5.");

            if (comentario is not null && comentario.Length > TamanhoMaximoComentario)
                throw new ArgumentException($"O comentário passa do limite de {TamanhoMaximoComentario} caracteres.");

            Id = Guid.NewGuid();
            CorridaId = corridaId;
            AutorTipo = autorTipo;
            AvaliadoId = avaliadoId;
            Nota = nota;
            Comentario = string.IsNullOrWhiteSpace(comentario) ? null : comentario.Trim();
            DataAvaliacao = DateTime.UtcNow;
        }
    }
}
