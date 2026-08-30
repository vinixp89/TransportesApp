using TransportesApp.Domain.Enums;

namespace TransportesApp.Application.DTOs
{
    // Resultado da busca de destinatário por e-mail exato (ver ClientesController.Buscar) — só o
    // essencial pra confirmar "é essa pessoa mesmo" antes de doar, sem expor CPF/telefone/endereço.
    public record BuscarClienteResponse(Guid Id, string Nome, string Email);

    // PacoteCorridasId nulo (padrão) = doa debitando da carteira. Informado = doa consumindo 1 corrida
    // de um pacote que o próprio doador já tem (precisa ser da mesma Faixa e ter corrida disponível).
    public record DoarCorridaRequest(string EmailDestinatario, CorFaixa Faixa, Guid? PacoteCorridasId = null);

    // QuantidadeRestantePacote só vem preenchida quando a doação saiu de um pacote (PacoteCorridasId
    // informado) — indica quantas corridas sobraram nesse pacote depois da doação.
    public record DoarCorridaResponse(string NomeDestinatario, CorFaixa Faixa, decimal Valor, decimal SaldoRestante, int? QuantidadeRestantePacote);
}
