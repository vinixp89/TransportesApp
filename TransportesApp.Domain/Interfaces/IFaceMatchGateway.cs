namespace TransportesApp.Domain.Interfaces
{
    // Comparação de rosto entre duas fotos (selfie de cadastro vs selfie capturada na corrida).
    // Implementação de verdade usa a AWS Rekognition (ver AwsRekognitionFaceMatchGateway); nunca
    // lança exceção — qualquer falha (sem credencial configurada, nenhum rosto detectado, serviço
    // fora) volta como Sucesso=false + MensagemErro, porque isso é um recurso de auditoria e nunca
    // deve travar o fluxo de quem chamou.
    public interface IFaceMatchGateway
    {
        Task<ResultadoComparacaoFacial> CompararAsync(byte[] fotoReferencia, byte[] fotoCapturada);
    }

    public sealed record ResultadoComparacaoFacial(bool Sucesso, double Similaridade, string? MensagemErro);
}
