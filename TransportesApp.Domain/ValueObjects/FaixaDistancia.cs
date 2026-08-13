using TransportesApp.Domain.Enums;

namespace TransportesApp.Domain.ValueObjects
{
    public sealed class FaixaDistancia
    {

        public CorFaixa Cor { get; }
        public double KmMinimo { get; }

        public double? KmMaximo { get; }

        public decimal PrecoAvulso { get; }

        public decimal PrecoPacote10 { get; }

        public FaixaDistancia(CorFaixa cor, double KmMinimo ,double? KmMaximo, decimal precoAvulso, decimal precoPacote)
        {

            Cor = cor;
            KmMinimo = KmMinimo;
            KmMaximo = KmMaximo;
            PrecoAvulso = precoAvulso;
            PrecoPacote10 = PrecoPacote10;

        
        
        
        }

        public bool Contem(double km) => km >= KmMinimo && (KmMaximo is null || km <= KmMaximo);

        private static readonly List<FaixaDistancia> Faixas = new()
        {
            new FaixaDistancia(CorFaixa.Azul,0,5,8m,50m),
            new FaixaDistancia(CorFaixa.Amarela, 5,10,20m,80m),
            new FaixaDistancia(CorFaixa.Vermelha, 10,30,50m,160m),
            new FaixaDistancia(CorFaixa.Rosa, 30 ,60,70m,280m),
            new FaixaDistancia(CorFaixa.Verde,60 ,120, 120m,480m),
            new FaixaDistancia(CorFaixa.Roxa, 120,220,200m, 800m)


        };
        public static FaixaDistancia ClassificarPorDistancia( double km) 
        {

            var faixa = Faixas.FirstOrDefault(f => f.Contem(km));
            return faixa ?? throw new InvalidOperationException($"Corridas acima de 150km não são permitidas nesta plataforma(distancia informada {km}km) .");

        }

        public static FaixaDistancia ObterPorCor(CorFaixa cor) => Faixas.First(f => f.Cor == cor);
    }
}
