using TransportesApp.Domain.Enums;

namespace TransportesApp.Application.DTOs
{
    public record CriarMotoristaRequest(
        string Nome,
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
        string Nome,
        string Cnh,
        string Cpf,
        string Telefone,
        string PlacaVeiculo,
        string ModeloVeiculo,
        int? AnoVeiculo,
        double AvaliacaoMeida,
        DateTime DataCadastro,
        EnderecoResponse Endereco,
        StatusMotorista Status,
        double? LatitudeAtual,
        double? LongitudeAtual,
        bool FotosEnviadas,
        bool TemFotoSelfie,
        bool TemFotoVeiculo,
        bool TemFotoPlaca,
        bool TelefoneVerificado,
        bool TermosAceitos,
        StatusContaMotorista StatusConta,
        DateTime? BloqueadoAte,
        string? MotivoBloqueio
    );

    // Edição manual pelo Admin (ver AdminMotoristasPage) — mesmos campos de CriarMotoristaRequest,
    // mas AnoVeiculo é opcional aqui (o motorista pode ter sido cadastrado antes dessa exigência
    // existir, e o Admin não deveria ser obrigado a preencher um valor que não tem como confirmar).
    public record AtualizarMotoristaRequest(
        string Nome,
        string Cnh,
        string Cpf,
        string Telefone,
        string PlacaVeiculo,
        string ModeloVeiculo,
        int? AnoVeiculo,
        string Logradouro,
        string Numero,
        string Bairro,
        string Cidade,
        string Estado,
        string? Complemento = null,
        double? Latitude = null,
        double? Longitude = null
    );

    // Dias null = suspensão por tempo indeterminado ("definitivamente"); com valor, a conta volta a
    // funcionar sozinha depois desses dias (ver MotoristaService.VerificarBloqueioAsync).
    public record SuspenderMotoristaRequest(string Motivo, int? Dias);

    public record BanirMotoristaRequest(string Motivo);

    // Dados do motorista que o CLIENTE pode ver enquanto está com uma corrida em andamento com ele —
    // deliberadamente sem CPF/CNH/telefone (ver CorridasController.ObterMotoristaDaCorrida). A foto
    // em si não vem aqui (é binário) — TemFoto só indica se dá pra chamar o endpoint que a serve.
    public record MotoristaDaCorridaResponse(
        string Nome,
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
