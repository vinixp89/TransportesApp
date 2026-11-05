using TransportesApp.Domain.Enums;

namespace TransportesApp.Domain.ValueObjects
{
    public sealed class FaixaDistancia
    {

        public CorFaixa Cor { get; }
        public double KmMinimo { get; }

        public double? KmMaximo { get; }

        public decimal PrecoAvulso { get; }

        // Preço da mesma faixa de distância na categoria Executivo (veículo até 3 anos, sedan médio ou
        // SUV — ver AssinaturaMotoristaExecutivo) — só corrida avulsa por enquanto, pacotes e benefício de
        // plano continuam exclusivos da categoria Normal (ver CorridaService.CriarAsync).
        public decimal PrecoAvulsoExecutivo { get; }

        // Preço da moto (sempre categoria Normal) — null nas faixas acima de DistanciaMaximaMotoKm,
        // onde moto não é atendida (hoje só a Roxa, 40,1–50 km).
        public decimal? PrecoAvulsoMoto { get; }

        // Corrida de moto vai até esse limite (inclusive) — acima disso o cliente só pode pedir carro.
        public const double DistanciaMaximaMotoKm = 40;

        // Tamanhos de pacote de corridas disponíveis pra compra.
        public static readonly int[] TamanhosPacoteDisponiveis = { 3, 5, 10 };

        public FaixaDistancia(CorFaixa cor, double kmMinimo, double? kmMaximo, decimal precoAvulso, decimal precoAvulsoExecutivo, decimal? precoAvulsoMoto)
        {
            Cor = cor;
            KmMinimo = kmMinimo;
            KmMaximo = kmMaximo;
            PrecoAvulso = precoAvulso;
            PrecoAvulsoExecutivo = precoAvulsoExecutivo;
            PrecoAvulsoMoto = precoAvulsoMoto;
        }

        public bool Contem(double km) => km >= KmMinimo && (KmMaximo is null || km <= KmMaximo);

        public bool AtendeMoto => PrecoAvulsoMoto is not null;

        public decimal ObterPreco(CategoriaCorrida categoria, TipoVeiculo tipoVeiculo = TipoVeiculo.Carro)
        {
            if (tipoVeiculo == TipoVeiculo.Moto)
            {
                if (categoria == CategoriaCorrida.Executivo)
                    throw new InvalidOperationException("A categoria Executivo é só para carro.");

                return PrecoAvulsoMoto
                    ?? throw new InvalidOperationException($"A faixa {Cor} não é atendida por moto (limite de {DistanciaMaximaMotoKm:0}km).");
            }

            return categoria == CategoriaCorrida.Executivo ? PrecoAvulsoExecutivo : PrecoAvulso;
        }

        // Preço do pacote é proporcional ao avulso da categoria (sem desconto) — a margem por corrida
        // já é enxuta (15%), então dar desconto em pacote cortaria direto na margem.
        public decimal ObterPrecoPacote(int quantidade, CategoriaCorrida categoria = CategoriaCorrida.Normal, TipoVeiculo tipoVeiculo = TipoVeiculo.Carro)
        {
            if (!TamanhosPacoteDisponiveis.Contains(quantidade))
                throw new ArgumentException(
                    $"Pacote de {quantidade} corridas não é um tamanho disponível. Tamanhos válidos: {string.Join(", ", TamanhosPacoteDisponiveis)}.");

            return ObterPreco(categoria, tipoVeiculo) * quantidade;
        }

        private static readonly List<FaixaDistancia> Faixas = new()
        {
            // KmMinimo do Azul é 0 (não 1) de propósito: cobre corridas bem curtas (< 1km) que a
            // tabela não previa explicitamente, evitando um buraco sem faixa nenhuma.
            //
            // Colunas: carro Normal, carro Executivo, moto. A moto passou a custar o que o carro Normal
            // custava antes do reajuste de novembro/2026 (que subiu o carro em todas as faixas).
            new FaixaDistancia(CorFaixa.Azul,     0,    5,   10.90m,  17.90m,   8.90m),
            new FaixaDistancia(CorFaixa.Amarela,  5.1, 10,   16.90m,  28.90m,  14.90m),
            new FaixaDistancia(CorFaixa.Laranja, 10.1, 15,   25.90m,  43.90m,  22.90m),
            new FaixaDistancia(CorFaixa.Vermelha,15.1, 20,   34.90m,  54.90m,  29.90m),
            new FaixaDistancia(CorFaixa.Rosa,    20.1, 30,   57.90m,  91.90m,  49.90m),
            new FaixaDistancia(CorFaixa.Verde,   30.1, 40,   74.90m, 120.90m,  64.90m),
            new FaixaDistancia(CorFaixa.Roxa,    40.1, 50,   91.90m, 149.90m,  null)
        };
        public static FaixaDistancia ClassificarPorDistancia(double km, TipoVeiculo tipoVeiculo = TipoVeiculo.Carro)
        {
            if (tipoVeiculo == TipoVeiculo.Moto && km > DistanciaMaximaMotoKm)
                throw new InvalidOperationException(
                    $"Corridas de moto vão até {DistanciaMaximaMotoKm:0}km (distância informada: {km:0.#}km). Escolha carro pra essa viagem.");

            var faixa = Faixas.FirstOrDefault(f => f.Contem(km));
            return faixa ?? throw new InvalidOperationException($"Corridas acima de 50km não são permitidas nesta plataforma (distância informada: {km}km).");

        }

        public static FaixaDistancia ObterPorCor(CorFaixa cor) => Faixas.First(f => f.Cor == cor);

        // Exposto só como leitura — usado pra montar o catálogo de pacotes disponíveis pra compra.
        public static IReadOnlyList<FaixaDistancia> ListarTodas() => Faixas.AsReadOnly();
    }
}
