using TransportesApp.Domain.Enums;

namespace TransportesApp.Domain.Entities
{
    public class Motorista
    {
        public Guid Id { get; private set; }
        public Guid UsuarioId { get; private set; }
        public string CNH { get; private set; }
        public string PlacaVeiculo { get; private set; }
        public string ModeloVeiculo { get; private set; }
        public StatusMotorista Status { get; private set; }
        public double? LatitudeAtual { get; private set; }
        public double? LongitudeAtual { get; private set; }
        public double AvaliacaoMedia { get; private set; }

        public DateTime DataCadastro { get; private set; }

        protected Motorista() { }



        public Motorista(Guid usuarioId,string cnh , string placaVeiculo,string  modeloVeiculo) 
        {

            if (string.IsNullOrWhiteSpace(cnh))
                throw new ArgumentException("CNH é Obrigatória. ");
            Id = Guid.NewGuid();
            UsuarioId = usuarioId;
            CNH = cnh;
            PlacaVeiculo = placaVeiculo;
            ModeloVeiculo = modeloVeiculo;
            Status = StatusMotorista.Offline;
            AvaliacaoMedia = 5.0;
            DataCadastro = DateTime.UtcNow;
        
        
        
        
        }

        public void AtualizarLocalizacao(double latitude , double longitude)
        {

            LatitudeAtual = latitude;
            LongitudeAtual = longitude;       
        
        
        }
        public void FicarDisponivel() => Status = StatusMotorista.Disponivel;

        public void FicarOffline() => Status = StatusMotorista.Offline;

        public void IniciarCorrida() 
        {

            if (Status != StatusMotorista.Disponivel)
                throw new InvalidOperationException("Motorista precisa esta disponivel no momento");

            Status = StatusMotorista.EmCorrida;
        
        
        
        }
        public void FinalizarCorrida() => Status = StatusMotorista.Disponivel;      









    }
}
