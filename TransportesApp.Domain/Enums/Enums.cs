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
    public enum TipoConsumo
    {
        Avulsa,
        Pacote

    }

    public enum TipoTransacaoCarteira
    {
        Recarga,
        Debito
    }
}
