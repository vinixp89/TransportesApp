using Amazon;
using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using Microsoft.Extensions.Configuration;
using TransportesApp.Domain.Interfaces;

namespace TransportesApp.Infrastructure.Gateways
{
    // Compara duas selfies via AWS Rekognition (CompareFaces). Credenciais vêm da cadeia padrão do
    // SDK (variáveis de ambiente AWS_ACCESS_KEY_ID/AWS_SECRET_ACCESS_KEY/AWS_REGION, ou perfil/role
    // — nunca hardcoded aqui). Enquanto essas credenciais não estiverem configuradas no servidor,
    // toda chamada cai no catch abaixo e volta Sucesso=false — o registro fica salvo como "não
    // verificado", sem quebrar o cadastro/viagem (ver VerificacaoFacial.RegistrarErro).
    public class AwsRekognitionFaceMatchGateway : IFaceMatchGateway
    {
        // Mesmo limiar mínimo aceito pela API do Rekognition pra considerar a comparação — abaixo
        // disso a própria AWS já teria descartado o match, então isso só evita chamar a API com um
        // threshold menor que o necessário.
        private const float SimilarityThreshold = 1f;

        private readonly IConfiguration _configuration;

        public AwsRekognitionFaceMatchGateway(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<ResultadoComparacaoFacial> CompararAsync(byte[] fotoReferencia, byte[] fotoCapturada)
        {
            try
            {
                var regiao = _configuration["Aws:Region"] ?? "us-east-1";
                using var client = new AmazonRekognitionClient(RegionEndpoint.GetBySystemName(regiao));

                using var streamReferencia = new MemoryStream(fotoReferencia);
                using var streamCapturada = new MemoryStream(fotoCapturada);

                var request = new CompareFacesRequest
                {
                    SourceImage = new Image { Bytes = streamReferencia },
                    TargetImage = new Image { Bytes = streamCapturada },
                    SimilarityThreshold = SimilarityThreshold
                };

                var response = await client.CompareFacesAsync(request);

                if (response.FaceMatches.Count == 0)
                    return new ResultadoComparacaoFacial(true, 0, null);

                var melhorMatch = response.FaceMatches.Max(m => m.Similarity);
                return new ResultadoComparacaoFacial(true, melhorMatch, null);
            }
            catch (Exception ex)
            {
                // Qualquer falha (sem credencial configurada, nenhum rosto na foto, serviço fora do
                // ar) é auditoria, nunca deve propagar — quem chama sempre recebe uma resposta.
                return new ResultadoComparacaoFacial(false, 0, ex.Message);
            }
        }
    }
}
