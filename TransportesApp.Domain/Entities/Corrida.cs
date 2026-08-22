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

        // Regra de negócio de cancelamento: corrida em andamento não pode mais ser cancelada por aqui
        // (só finalizada normalmente) — só dá pra cancelar enquanto ainda está Solicitada ou Confirmada.
        //
        // sempreReembolsar é passado por quem chama (ver CorridaService.CancelarAsync) e indica se quem
        // está cancelando NÃO é o cliente (motorista ou Admin) — nesse caso o cancelamento sempre dá
        // direito a reembolso/devolução do que já foi consumido, porque não foi o cliente quem desistiu.
        //
        // Devolve true quando o cancelamento dá direito a reverter o que já foi debitado/consumido
        // (saldo da carteira, corrida do pacote, benefício do plano) — reversão só acontece enquanto a
        // corrida ainda estava Solicitada (sem motorista atribuído) OU quando sempreReembolsar é true.
        // Cliente cancelando uma corrida já Confirmada (com motorista atribuído) não tem direito a
        // devolução: é a "taxa" de cancelamento tardio, que aqui é simplesmente não devolver o que já
        // foi gasto, em vez de cobrar algo novo.
        public bool Cancelar(bool sempreReembolsar)
        {

            if (Status is StatusCorrida.Finalizada or StatusCorrida.Cancelada or StatusCorrida.EmAndamento)
                throw new InvalidOperationException("Corrida em andamento, finalizada ou já cancelada não pode ser cancelada");

            var deveReverterConsumo = Status == StatusCorrida.Solicitada || sempreReembolsar;

            Status = StatusCorrida.Cancelada;

            return deveReverterConsumo;
        }






    }
}
