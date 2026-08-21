using TransportesApp.Domain.Enums;
using TransportesApp.Domain.ValueObjects;

namespace TransportesApp.Domain.Entities
{
    public class Corrida
    {
        public Guid Id { get; private set; }
        public Guid ClienteId { get; private set; }
        public Guid? MotoristaId { get; private set; }
        public Endereco Origem { get; private set; } = default!;

        public Endereco Destino { get; private set; } = default!;
        public double DistanciaEstimadaKm { get; private set; }

        public double? DistanciaRealKm { get; private set; }
        public CorFaixa FaixaContratada { get; private set; }
        public TipoConsumo TipoConsumo { get; private set; }

        public Guid? PacoteCorridasId { get; private set; }
        public decimal? ValorExcedente { get;private set; }    
        public StatusCorrida Status { get; private set; }
        public DateTime DataSolicitacao { get; private set; }
        public DateTime DataFinalizacao { get; private set; }

        protected Corrida() { }


        public Corrida(Guid clienteId,Endereco origem,Endereco destino,double distanciaEstimadaKm, FaixaDistancia faixa, TipoConsumo tipoConsumo,
             Guid? pacoteCorridasId)
        {

            if (tipoConsumo == TipoConsumo.Pacote && pacoteCorridasId is null)
                throw new ArgumentException("Corrida por pacote  precisa informa  o PacoteCorridasId");

            Id = Guid.NewGuid();
            ClienteId = clienteId;
            Origem = origem;
            Destino = destino;
            DistanciaEstimadaKm = distanciaEstimadaKm;
            FaixaContratada = faixa.Cor;
            TipoConsumo = tipoConsumo;
            // Em corrida avulsa, ignora qualquer pacoteCorridasId que venha no request (ex: o Guid de
            // exemplo que o Swagger preenche sozinho) — só grava o Id do pacote quando o consumo é por Pacote.
            // Sem isso, um Guid "lixo" nesse campo tenta ser salvo e quebra a FK com PacotesCorridas.
            PacoteCorridasId = tipoConsumo == TipoConsumo.Pacote ? pacoteCorridasId : null;
            Status = StatusCorrida.Solicitada;
            DataSolicitacao = DateTime.UtcNow;
        
        
        }

        public void AtribuirMotorista(Guid motoristaId) 
        {

            if (Status != StatusCorrida.Solicitada)
                throw new InvalidOperationException("Só é possível atribuir  motorista a uma corrida solicitada");
            MotoristaId = motoristaId;
            Status = StatusCorrida.Confirmada;
        
        
        }

        public void IniciarViagem() 
        {

            if (Status != StatusCorrida.Confirmada)
                throw new InvalidOperationException("Corrida precisa estar confirmada para iniiciar a viagem");
            Status = StatusCorrida.EmAndamento;
        
        
        
        }
        public bool FinalizarCorrida(double distanciaReal)
        {
            if (Status != StatusCorrida.EmAndamento)
                throw new InvalidOperationException("Corrida precisa está em andamento para ser finalizada");

            DistanciaRealKm = distanciaReal;
            var faixaReal = FaixaDistancia.ClassificarPorDistancia(distanciaReal);
            var estouroFaixa = faixaReal.Cor != FaixaContratada;

            Status = StatusCorrida.Finalizada;
            DataFinalizacao = DateTime.UtcNow;

            return estouroFaixa;

        }

        public void RegistrarExcedente(decimal valor)  => ValorExcedente =valor;

        public void Cancelar()
        {

            if (Status is StatusCorrida.Finalizada or StatusCorrida.Cancelada)
                throw new InvalidOperationException("Corrida finalizada ou cancelada não pode ser cancelada");
            Status = StatusCorrida.Cancelada;
        
        
        }






    }
}
