using TransportesApp.Domain.Enums;

namespace TransportesApp.Domain.Entities
{
    // Assinatura de um cliente a um dos planos do catálogo (ver PlanoAssinatura). Um cliente tem no
    // máximo UMA assinatura "em aberto" por vez (pendente de pagamento OU ativa) — assinar outro
    // plano cancela a atual e cria uma nova (é mais simples que "trocar de plano" na mesma linha, e
    // mantém histórico de todas as assinaturas já feitas, inclusive as canceladas/recusadas).
    //
    // Ciclo de vida (ver StatusAssinatura): nasce PendentePagamento (exceto o plano Básico, que é
    // grátis e nasce direto Ativa — ver PlanoService.AssinarAsync); Ativar()/MarcarPagamentoRecusado()
    // são chamados pelo PagamentoService quando o gateway confirma ou recusa o pagamento associado.
    public class AssinaturaPlano
    {
        public Guid Id { get; private set; }
        public Guid ClienteId { get; private set; }
        public TipoPlano Tipo { get; private set; }
        public DateTime DataInicio { get; private set; }
        public DateTime? DataCancelamento { get; private set; }
        public StatusAssinatura Status { get; private set; }

        // Controle do benefício "1 corrida grátis por mês, não acumulável" (ver PlanoAssinatura.CorGratisPorMes).
        // AnoMesBeneficio guarda o mês (AAAAMM, ex: 202608) a que os dois flags abaixo se referem — quando o mês
        // atual é diferente disso, os flags são tratados como resetados (mesmo que ainda não tenham sido
        // regravados no banco), e só são realmente zerados na próxima chamada que grava algo nesse mês.
        // Isso é o que garante o "não acumulável": passou o mês sem usar, o benefício não fica pra trás.
        public int? AnoMesBeneficio { get; private set; }
        public bool CorridaPagaNoMes { get; private set; }
        public bool BeneficioUsadoNoMes { get; private set; }

        protected AssinaturaPlano() { }

        public AssinaturaPlano(Guid clienteId, TipoPlano tipo)
        {
            Id = Guid.NewGuid();
            ClienteId = clienteId;
            Tipo = tipo;
            DataInicio = DateTime.UtcNow;
            Status = StatusAssinatura.PendentePagamento;
        }

        // Chamado pelo PagamentoService quando o pagamento associado é aprovado (ou direto pelo
        // PlanoService, sem pagamento nenhum, no caso do plano Básico que é grátis).
        public void Ativar()
        {
            if (Status is not (StatusAssinatura.PendentePagamento or StatusAssinatura.PagamentoRecusado))
                throw new InvalidOperationException($"Não é possível ativar uma assinatura com status {Status}.");

            Status = StatusAssinatura.Ativa;
        }

        // Chamado pelo PagamentoService quando o pagamento associado é recusado/cancelado — fica nesse
        // estado até o cliente tentar assinar de novo (PlanoService.AssinarAsync trata isso como "sem
        // assinatura em aberto", então uma nova tentativa cria outra AssinaturaPlano do zero).
        public void MarcarPagamentoRecusado()
        {
            if (Status != StatusAssinatura.PendentePagamento)
                throw new InvalidOperationException($"Não é possível recusar uma assinatura com status {Status}.");

            Status = StatusAssinatura.PagamentoRecusado;
        }

        public void Cancelar()
        {
            if (Status is not (StatusAssinatura.Ativa or StatusAssinatura.PendentePagamento))
                throw new InvalidOperationException($"Essa assinatura não pode ser cancelada (status atual: {Status}).");

            Status = StatusAssinatura.Cancelada;
            DataCancelamento = DateTime.UtcNow;
        }

        // true só quando o cliente já fez uma corrida paga (avulsa ou por pacote) NESTE mês — é o que
        // libera a corrida grátis. Não muta estado (seguro pra usar em consultas).
        public bool CorridaPagaLiberouBeneficioNoMes(DateTime dataReferenciaUtc)
            => EhMesAtual(dataReferenciaUtc) && CorridaPagaNoMes;

        // true quando a corrida grátis deste mês já foi usada. Não muta estado.
        public bool BeneficioJaUsadoNoMes(DateTime dataReferenciaUtc)
            => EhMesAtual(dataReferenciaUtc) && BeneficioUsadoNoMes;

        // Disponível pra usar agora = já liberou (fez corrida paga no mês) e ainda não usou a grátis deste mês.
        public bool BeneficioDisponivel(DateTime dataReferenciaUtc)
            => CorridaPagaLiberouBeneficioNoMes(dataReferenciaUtc) && !BeneficioJaUsadoNoMes(dataReferenciaUtc);

        // Chamado toda vez que o cliente cria uma corrida paga (avulsa ou por pacote) — marca que o
        // benefício do mês está liberado. Idempotente: chamar de novo no mesmo mês não muda nada.
        public void RegistrarCorridaPaga(DateTime dataReferenciaUtc)
        {
            GarantirMesAtual(dataReferenciaUtc);
            CorridaPagaNoMes = true;
        }

        // Chamado ao criar uma corrida com TipoConsumo.BeneficioPlano — CorridaService já validou a cor
        // e a elegibilidade antes de chamar isso, mas valida de novo aqui pra nunca deixar o estado ir a
        // um lugar inconsistente mesmo se for chamado de outro lugar no futuro.
        public void UsarBeneficioGratis(DateTime dataReferenciaUtc)
        {
            if (!BeneficioDisponivel(dataReferenciaUtc))
                throw new InvalidOperationException("O benefício de corrida grátis não está disponível no momento.");

            BeneficioUsadoNoMes = true;
        }

        // Reverte um UsarBeneficioGratis() — chamado quando uma corrida por BeneficioPlano é cancelada
        // com direito a reembolso (ver Corrida.Cancelar e CorridaService.ReverterConsumoAsync). Só desfaz
        // se ainda estiver dentro do mesmo mês em que o benefício foi usado — se o mês já virou, o flag
        // já é tratado como resetado (ver EhMesAtual) e não há nada a desfazer.
        public void DevolverBeneficioGratis(DateTime dataReferenciaUtc)
        {
            if (EhMesAtual(dataReferenciaUtc))
                BeneficioUsadoNoMes = false;
        }

        private void GarantirMesAtual(DateTime dataReferenciaUtc)
        {
            var anoMesAtual = dataReferenciaUtc.Year * 100 + dataReferenciaUtc.Month;

            if (AnoMesBeneficio != anoMesAtual)
            {
                AnoMesBeneficio = anoMesAtual;
                CorridaPagaNoMes = false;
                BeneficioUsadoNoMes = false;
            }
        }

        private bool EhMesAtual(DateTime dataReferenciaUtc)
            => AnoMesBeneficio == dataReferenciaUtc.Year * 100 + dataReferenciaUtc.Month;
    }
}
