namespace TransportesApp.Application.DTOs
{
    public record CriarClienteRequest(
        string Nome,
        string Cpf,
        string Telefone,
        string Logradouro,
        string Numero,
        string Bairro,
        string Cidade,
        string Estado,
        string? Complemento = null,
        double? Latitude = null,
        double? Longitude = null
    );

    public record ClienteResponse(
      Guid Id,
      Guid UsuarioId,
      string Nome,
      string Cpf,
      string Telefone,
      string Email,
      double? AvaliacaoMedia,
      DateTime DataCadastro,
      bool TelefoneVerificado,
      bool TermosAceitos,
      // Só indica se a selfie foi enviada, não a URL de verdade — mesmo padrão do
      // MotoristaResponse.FotosEnviadas: quem precisa exibir a imagem passa por um endpoint
      // autenticado dedicado, nunca pelo caminho cru do arquivo.
      bool TemFotoSelfie
  );
}
