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
        Verde,
        Vermelha,
        Rosa,
        
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

    public enum TipoConsumo 
    { 
        Pacote,
        Avulsa
    
    }
}
