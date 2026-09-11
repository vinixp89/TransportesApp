using TransportesApp.Domain.Enums;

namespace TransportesApp.Application.DTOs
{
    public record CriarMotoristaRequest(
        string Cnh,
        string Cpf,
        string Telefone,
        string PlacaVeiculo,
        string ModeloVeiculo,
        int AnoVeiculo,
        string Logradouro,
        string Numero,
        string Bairro,
        string Cidade,
        string Estado,
        string? Complemento = null,
        double? Latitude = null,
        double? Longitude = null
    );

    public record MotoristaResponse(
        Guid Id,
        Guid UsuarioId,
        string Cnh,
        string Cpf,
        string Telefone,
        string PlacaVeiculo,
        string ModeloVeiculo,
        double AvaliacaoMeida,
        DateTime DataCadastro,
        EnderecoResponse Endereco,
        StatusMotorista Status,
        double? LatitudeAtual,
        double? LongitudeAtual,
        bool FotosEnviadas,
        bool TelefoneVerificado,
        bool TermosAceitos
    );

    // Dados do motorista que o CLIENTE pode ver enquanto está com uma corrida em andamento com ele —
    // deliberadamente sem CPF/CNH/telefone (ver CorridasController.ObterMotoristaDaCorrida). A foto
    // em si não vem aqui (é binário) — TemFoto só indica se dá pra chamar o endpoint que a serve.
    public record MotoristaDaCorridaResponse(
        string PlacaVeiculo,
        string ModeloVeiculo,
        double AvaliacaoMedia,
        bool TemFoto
    );

    // Versão enxuta, sem CPF/CNH, pra um cliente ver motoristas disponíveis sem expor dados sensíveis de outra pessoa.
    public record MotoristaDisponivelResponse(
        Guid Id,
        string PlacaVeiculo,
        string ModeloVeiculo,
        double AvaliacaoMedia,
        double? LatitudeAtual,
        double? LongitudeAtual,
        double? DistanciaKm
    );

    public record AtualizarLocalizacaoRequest(double Latitude, double Longitude);
}
