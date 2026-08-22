using TransportesApp.Domain.Enums;

namespace TransportesApp.Domain.ValueObjects
{
    // Catálogo fixo dos planos de assinatura, no mesmo espírito de FaixaDistancia: os 3 planos são
    // conhecidos e não mudam a cada request, então não tem tabela pra isso — só a assinatura em si
    // (quem assinou qual plano) é que fica no banco, ver AssinaturaPlano.
    public sealed class PlanoAssinatura
    {
        public TipoPlano Tipo { get; }
        public string Nome { get; }
        public decimal PrecoMensal { get; }
        public IReadOnlyList<string> Beneficios { get; }

        // Cor da corrida que o plano dá de graça 1x por mês (não acumulável) — null quando o plano
        // não tem esse benefício. A regra de liberação (precisa de 1 corrida paga no mês antes de
        // poder usar a grátis) e o controle mensal ficam em AssinaturaPlano, não aqui: isso aqui é só
        // o catálogo fixo, sem estado.
        public CorFaixa? CorGratisPorMes { get; }

        public PlanoAssinatura(TipoPlano tipo, string nome, decimal precoMensal, IReadOnlyList<string> beneficios, CorFaixa? corGratisPorMes = null)
        {
            Tipo = tipo;
            Nome = nome;
            PrecoMensal = precoMensal;
            Beneficios = beneficios;
            CorGratisPorMes = corGratisPorMes;
        }

        private static readonly List<PlanoAssinatura> Planos = new()
        {
            new PlanoAssinatura(TipoPlano.Basico, "Básico", 0m, new[]
            {
                "Peça corridas quando precisar",
                "Pagamento avulso ou por pacote de corridas",
                "Suporte por e-mail",
            }),
            new PlanoAssinatura(TipoPlano.Plus, "Plus", 19.90m, new[]
            {
                "10% de desconto em todos os pacotes de corrida",
                "Suporte prioritário por chat",
                "Cancelamento gratuito em até 5 minutos",
            }),
            new PlanoAssinatura(TipoPlano.Premium, "Premium", 39.90m, new[]
            {
                "20% de desconto em todos os pacotes de corrida",
                "Suporte prioritário 24 horas",
                "Cancelamento gratuito em até 15 minutos",
                "Prioridade na fila de motoristas em horário de pico",
                "1 corrida azul grátis por mês (não acumulável) — faça 1 corrida paga no mês pra liberar",
            }, CorFaixa.Azul),
            new PlanoAssinatura(TipoPlano.Diamante, "Diamante", 59.90m, new[]
            {
                "30% de desconto em todos os pacotes de corrida",
                "Suporte prioritário 24 horas",
                "Cancelamento gratuito em até 20 minutos",
                "Prioridade máxima na fila de motoristas em horário de pico",
                "1 corrida vermelha grátis por mês (não acumulável) — faça 1 corrida paga no mês pra liberar",
            }, CorFaixa.Vermelha),
        };

        public static IReadOnlyList<PlanoAssinatura> ListarTodos() => Planos.AsReadOnly();

        public static PlanoAssinatura ObterPorTipo(TipoPlano tipo) => Planos.First(p => p.Tipo == tipo);
    }
}
