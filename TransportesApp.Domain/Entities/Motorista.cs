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

        // Ano de fabricação do veículo — só é preenchido quando o motorista assina a categoria Executivo
        // (ver AssinaturaMotoristaExecutivoService.AssinarAsync), que exige veículo com até 3 anos.
        // Null pra quem nunca assinou Executivo (não é coletado no cadastro comum).
        public int? AnoVeiculo { get; private set; }

        public DateTime DataCadastro { get; private set; }

        protected Motorista() { }



        public Motorista(Guid usuarioId, string cnh, string cpf, string placaVeiculo, string modeloVeiculo, Endereco endereco)
        {

            if (string.IsNullOrWhiteSpace(cnh))
                throw new ArgumentException("CNH é Obrigatória. ");

            if (!CpfValidator.EhValido(cpf))
                throw new ArgumentException("CPF inválido.");

            if (endereco is null)
                throw new ArgumentException("Endereço é obrigatório.");

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

        // Categoria Executivo exige veículo com até 3 anos de fabricação (contando a partir do ano atual).
        public bool VeiculoElegivelParaExecutivo() => AnoVeiculo is not null && DateTime.UtcNow.Year - AnoVeiculo.Value <= 3;









    }
}
