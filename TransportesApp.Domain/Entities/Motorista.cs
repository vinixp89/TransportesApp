using TransportesApp.Domain.Common;
using TransportesApp.Domain.Enums;
using TransportesApp.Domain.ValueObjects;

namespace TransportesApp.Domain.Entities
{
    public class Motorista
    {
        public Guid Id { get; private set; }
        public Guid UsuarioId { get; private set; }
        public string CNH { get; private set; }
        public string Cpf { get; private set; } = default!;
        public string PlacaVeiculo { get; private set; }
        public string ModeloVeiculo { get; private set; }
        public Endereco Endereco { get; private set; } = default!;
        public StatusMotorista Status { get; private set; }
        public double? LatitudeAtual { get; private set; }
        public double? LongitudeAtual { get; private set; }
        public double AvaliacaoMedia { get; private set; }

        // Ano de fabricação do veículo — coletado já no cadastro (ver validação abaixo, que exige no
        // máximo IdadeMaximaVeiculoAnos). Continua nullable porque motoristas cadastrados antes dessa
        // exigência existir não têm esse dado preenchido.
        public int? AnoVeiculo { get; private set; }

        // Fotos de verificação (selfie, veículo, placa) pedidas no cadastro — ver
        // MotoristaService.DefinirFotosAsync. Guarda o caminho relativo do arquivo salvo no disco do
        // servidor (ver MotoristasController), não o binário. Nullable porque quem se cadastrou antes
        // dessa exigência existir não tem essas fotos ainda.
        public string? FotoSelfieUrl { get; private set; }
        public string? FotoVeiculoUrl { get; private set; }
        public string? FotoPlacaUrl { get; private set; }

        public DateTime DataCadastro { get; private set; }

        // Idade máxima aceita pro veículo no cadastro comum — mais permissiva que os 3 anos exigidos
        // pra assinar a categoria Executivo (ver VeiculoElegivelParaExecutivo).
        public const int IdadeMaximaVeiculoAnos = 12;

        protected Motorista() { }



        public Motorista(Guid usuarioId, string cnh, string cpf, string placaVeiculo, string modeloVeiculo, Endereco endereco, int anoVeiculo)
        {

            if (string.IsNullOrWhiteSpace(cnh))
                throw new ArgumentException("CNH é Obrigatória. ");

            if (!CpfValidator.EhValido(cpf))
                throw new ArgumentException("CPF inválido.");

            if (endereco is null)
                throw new ArgumentException("Endereço é obrigatório.");

            if (anoVeiculo > DateTime.UtcNow.Year)
                throw new ArgumentException("Ano de fabricação do veículo não pode ser no futuro.");

            if (DateTime.UtcNow.Year - anoVeiculo > IdadeMaximaVeiculoAnos)
                throw new ArgumentException($"O veículo precisa ter no máximo {IdadeMaximaVeiculoAnos} anos de fabricação.");

            Id = Guid.NewGuid();
            UsuarioId = usuarioId;
            CNH = cnh;
            Cpf = CpfValidator.Normalizar(cpf);
            PlacaVeiculo = placaVeiculo;
            ModeloVeiculo = modeloVeiculo;
            Endereco = endereco;
            Status = StatusMotorista.Offline;
            AvaliacaoMedia = 5.0;
            DataCadastro = DateTime.UtcNow;
            AnoVeiculo = anoVeiculo;




        }

        public void AtualizarLocalizacao(double latitude , double longitude)
        {

            LatitudeAtual = latitude;
            LongitudeAtual = longitude;       
        
        
        }
        public void FicarDisponivel() => Status = StatusMotorista.Disponivel;

        public void FicarOffline() => Status = StatusMotorista.Offline;

        public void IniciarCorrida() 
        {

            if (Status != StatusMotorista.Disponivel)
                throw new InvalidOperationException("Motorista precisa esta disponivel no momento");

            Status = StatusMotorista.EmCorrida;
        
        
        
        }
        public void FinalizarCorrida() => Status = StatusMotorista.Disponivel;

        // Chamado ao assinar a categoria Executivo — grava o ano informado pelo motorista pra validar a
        // elegibilidade (ver VeiculoElegivelParaExecutivo). Não há verificação de foto/documento ainda,
        // é autodeclarado.
        public void DefinirAnoVeiculo(int anoVeiculo) => AnoVeiculo = anoVeiculo;

        // Chamado depois do cadastro, quando o motorista envia as 3 fotos de verificação (ver
        // MotoristasController.EnviarFotos) — os 3 argumentos vêm sempre juntos porque o app pede as
        // três de uma vez, numa única tela.
        public void DefinirFotos(string fotoSelfieUrl, string fotoVeiculoUrl, string fotoPlacaUrl)
        {
            FotoSelfieUrl = fotoSelfieUrl;
            FotoVeiculoUrl = fotoVeiculoUrl;
            FotoPlacaUrl = fotoPlacaUrl;
        }

        // Categoria Executivo exige veículo com até 3 anos de fabricação (contando a partir do ano atual).
        public bool VeiculoElegivelParaExecutivo() => AnoVeiculo is not null && DateTime.UtcNow.Year - AnoVeiculo.Value <= 3;









    }
}
