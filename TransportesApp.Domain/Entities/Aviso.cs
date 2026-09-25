namespace TransportesApp.Domain.Entities
{
    // Aviso configurável pelo Admin, mostrado como pop-up ao abrir o app Cliente (ver
    // AvisoService/AvisosController). "Vigente" combina Ativo com o período (DataInicio/DataFim) —
    // fora desse período ou desativado, ObterAtivoAsync simplesmente não retorna nada, e o app não
    // mostra pop-up nenhum. TextoBotao/TelaDestino são opcionais: sem TextoBotao, o pop-up só tem o
    // botão de fechar.
    public class Aviso
    {
        public Guid Id { get; private set; }
        public string Titulo { get; private set; } = default!;
        public string Texto { get; private set; } = default!;
        public string CorFundoHex { get; private set; } = default!;
        public string? TextoBotao { get; private set; }
        public string? TelaDestino { get; private set; }
        public DateTime DataInicio { get; private set; }
        public DateTime DataFim { get; private set; }
        public bool Ativo { get; private set; }

        protected Aviso() { }

        public Aviso(
            string titulo,
            string texto,
            string corFundoHex,
            string? textoBotao,
            string? telaDestino,
            DateTime dataInicio,
            DateTime dataFim)
        {
            Id = Guid.NewGuid();
            Ativo = true;
            Atualizar(titulo, texto, corFundoHex, textoBotao, telaDestino, dataInicio, dataFim);
        }

        public void Atualizar(
            string titulo,
            string texto,
            string corFundoHex,
            string? textoBotao,
            string? telaDestino,
            DateTime dataInicio,
            DateTime dataFim)
        {
            if (string.IsNullOrWhiteSpace(titulo))
                throw new ArgumentException("Título é obrigatório.");

            if (string.IsNullOrWhiteSpace(texto))
                throw new ArgumentException("Texto é obrigatório.");

            if (dataFim <= dataInicio)
                throw new ArgumentException("A data de fim precisa ser depois da data de início.");

            Titulo = titulo.Trim();
            Texto = texto.Trim();
            CorFundoHex = string.IsNullOrWhiteSpace(corFundoHex) ? "#16a34a" : corFundoHex.Trim();
            // TelaDestino sem TextoBotao não faz sentido (não tem botão pra levar lá) — ignora
            // silenciosamente em vez de dar erro, pra não travar o Admin por um campo que ele
            // esqueceu de limpar.
            TextoBotao = string.IsNullOrWhiteSpace(textoBotao) ? null : textoBotao.Trim();
            TelaDestino = TextoBotao is null || string.IsNullOrWhiteSpace(telaDestino) ? null : telaDestino.Trim();
            DataInicio = dataInicio;
            DataFim = dataFim;
        }

        public void Ativar() => Ativo = true;

        public void Desativar() => Ativo = false;
    }
}
