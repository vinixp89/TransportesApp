using TransportesApp.Domain.Enums;

namespace TransportesApp.Application.DTOs
{
    // Resultado da busca de destinatário por e-mail exato (ver ClientesController.Buscar) — só o
    // essencial pra confirmar "é essa pessoa mesmo" antes de doar, sem expor CPF/telefone/endereço.
    public record BuscarClienteResponse(Guid Id, string Nome, string Email);

    public record DoarCorridaRequest(string EmailDestinatario, CorFaixa Faixa);

    public record DoarCorridaResponse(string NomeDestinatario, CorFaixa Faixa, decimal Valor, decimal SaldoRestante);
}
