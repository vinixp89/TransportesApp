using TransportesApp.Domain.Enums;

namespace TransportesApp.Domain.ValueObjects
{
    public sealed class FaixaDistancia
    {

        public CorFaixa Cor { get; }
        public double KmMinimo { get; }

        public double? KmMaximo { get; }

        public decimal PrecoAvulso { get; }

        // Preço da mesma faixa de distância na categoria Black (veículo até 3 anos, sedan médio ou
        // SUV — ver AssinaturaMotoristaBlack) — só corrida avulsa por enquanto, pacotes e benefício de
        // plano continuam exclusivos da categoria Normal (ver CorridaService.CriarAsync).
        public decimal PrecoAvulsoBlack { get; }

        // Tamanhos de pacote de corridas disponíveis pra compra.
        public static readonly int[] TamanhosPacoteDisponiveis = { 3, 5, 10 };

        public FaixaDistancia(CorFaixa cor, double kmMinimo, double? kmMaximo, decimal precoAvulso, decimal precoAvulsoBlack)
        {
            Cor = cor;
            KmMinimo = kmMinimo;
            KmMaximo = kmMaximo;
            PrecoAvulso = precoAvulso;
            PrecoAvulsoBlack = precoAvulsoBlack;
        }

        public bool Contem(double km) => km >= KmMinimo && (KmMaximo is null || km <= KmMaximo);

        public decimal ObterPreco(CategoriaCorrida categoria) => categoria == CategoriaCorrida.Black ? PrecoAvulsoBlack : PrecoAvulso;

        // Preço do pacote é proporcional ao avulso (sem desconto) — a margem por corrida já é enxuta (15%),
        // então dar desconto em pacote cortaria direto na margem.
        public decimal ObterPrecoPacote(int quantidade)
        {
            if (!TamanhosPacoteDisponiveis.Contains(quantidade))
                throw new ArgumentException(
                    $"Pacote de {quantidade} corridas não é um tamanho disponível. Tamanhos válidos: {string.Join(", ", TamanhosPacoteDisponiveis)}.");

            return PrecoAvulso * quantidade;
        }

        private static readonly List<FaixaDistancia> Faixas = new()
        {
            // KmMinimo do Azul é 0 (não 1) de propósito: cobre corridas bem curtas (< 1km) que a
            // tabela não previa explicitamente, evitando um buraco sem faixa nenhuma.
            new FaixaDistancia(CorFaixa.Azul,     0,    5,   8.90m,  15.90m),
            new FaixaDistancia(CorFaixa.Amarela,  5.1, 10,  14.90m,  24.90m),
            new FaixaDistancia(CorFaixa.Laranja, 10.1, 15,  22.90m,  37.90m),
            new FaixaDistancia(CorFaixa.Vermelha,15.1, 20,  29.90m,  47.90m),
            new FaixaDistancia(CorFaixa.Rosa,    20.1, 30,  49.90m,  79.90m),
            new FaixaDistancia(CorFaixa.Verde,   30.1, 40,  64.90m, 104.90m),
            new FaixaDistancia(CorFaixa.Roxa,    40.1, 50,  79.90m, 129.90m)
        };
        public static FaixaDistancia ClassificarPorDistancia( double km)
        {

            var faixa = Faixas.FirstOrDefault(f => f.Contem(km));
            return faixa ?? throw new InvalidOperationException($"Corridas acima de 50km não são permitidas nesta plataforma (distância informada: {km}km).");

        }

        public static FaixaDistancia ObterPorCor(CorFaixa cor) => Faixas.First(f => f.Cor == cor);

        // Exposto só como leitura — usado pra montar o catálogo de pacotes disponíveis pra compra.
        public static IReadOnlyList<FaixaDistancia> ListarTodas() => Faixas.AsReadOnly();
    }
}
