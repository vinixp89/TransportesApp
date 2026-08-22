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

    public enum TipoTransacaoCarteira
    {
        Recarga,
        Debito
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
}
