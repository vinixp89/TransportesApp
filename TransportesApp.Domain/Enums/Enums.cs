namespace TransportesApp.Domain.Enums
{
    public enum  TipoUsuario
    {
     
        Cliente,
        Motorista


    }

    public enum CorFaixa
    {
        Azul,
        Amarela,
        Laranja,
        Vermelha,
        Rosa,
        Verde,
        Roxa

    }

    public enum StatusCorrida 
    {
    
        Solicitada,
        Confirmada,
        MotoristaACaminho,
        EmAndamento,
        Finalizada,
        Cancelada

    }

    public enum StatusMotorista
    {
        Offline,
        Disponivel,
        EmCorrida


    }

    // Avulsa é o valor 0 (padrão) de propósito: o Swagger sempre preenche o exemplo da requisição
    // com o primeiro valor do enum e um Guid de exemplo em PacoteCorridasId. Se Pacote fosse o padrão,
    // qualquer corrida criada sem mexer nesses campos cairia num pacote inexistente e dava erro.
    // Com Avulsa em primeiro, deixar os campos como vieram = corrida avulsa (paga por corrida), sem exigir pacote.
    // BeneficioPlano foi adicionado depois (por isso fica por último, valor 2, e não mexe nos valores
    // já gravados no banco) — é a corrida grátis mensal de planos como Premium/Diamante, ver
    // PlanoAssinatura.CorGratisPorMes e AssinaturaPlano.
    public enum TipoConsumo
    {
        Avulsa,
        Pacote,
        BeneficioPlano

    }

    // Estorno foi adicionado depois (por isso fica por último, valor 2, e não mexe nos valores já
    // gravados no banco) — lançamento de devolução de saldo quando uma corrida avulsa é cancelada
    // com direito a reembolso (ver Corrida.Cancelar e CorridaService.ReverterConsumoAsync).
    public enum TipoTransacaoCarteira
    {
        Recarga,
        Debito,
        Estorno
    }

    // Básico em primeiro (valor 0) pelo mesmo motivo de TipoConsumo.Avulsa: é o padrão seguro pro
    // Swagger preencher sozinho, e representa "sem assinatura paga" de qualquer forma. Diamante foi
    // adicionado depois, por isso fica por último (valor 3) — não muda os valores já gravados no banco.
    public enum TipoPlano
    {
        Basico,
        Plus,
        Premium,
        Diamante
    }

    // PendentePagamento é o valor 0 (padrão) de propósito, igual os outros enums acima: uma assinatura
    // recém-criada some sempre nesse estado até o gateway confirmar o pagamento — ver AssinaturaPlano.
    public enum StatusAssinatura
    {
        PendentePagamento,
        Ativa,
        PagamentoRecusado,
        Cancelada
    }

    // A quem um Pagamento se refere — hoje AssinaturaPlano e RecargaCarteira de fato "escutam" a
    // confirmação (ver PagamentoService), mas o modelo já nasce genérico pra Pacotes/Corridas entrarem
    // no mesmo fluxo de pagamento mais pra frente, sem precisar de uma tabela de pagamento por feature.
    // RecargaCarteira foi adicionado depois (por isso fica por último, valor 3, e não mexe nos valores
    // já gravados no banco) — é a recarga de saldo da carteira via Mercado Pago; corridas avulsas
    // debitam desse saldo na hora (ver CorridaService), sem precisar de pagamento por corrida.
    public enum TipoReferenciaPagamento
    {
        AssinaturaPlano,
        PacoteCorridas,
        Corrida,
        RecargaCarteira
    }

    // Status Aprovado/Recusado/Cancelado/EmProcessamento mapeados a partir do "status" que o gateway
    // (Mercado Pago) devolve — ver MercadoPagoGateway.MapearStatus. Pendente é o valor 0 (padrão): um
    // Pagamento nasce sempre pendente, antes até de a preference ser criada no gateway.
    public enum StatusPagamento
    {
        Pendente,
        EmProcessamento,
        Aprovado,
        Recusado,
        Cancelado,
        Estornado
    }
}
