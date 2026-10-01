using TransportesApp.Domain.Common;
using TransportesApp.Domain.Enums;
using TransportesApp.Domain.ValueObjects;

namespace TransportesApp.Domain.Entities
{
    public class Motorista
    {
        public Guid Id { get; private set; }
        public Guid UsuarioId { get; private set; }
        public string Nome { get; private set; } = default!;
        public string CNH { get; private set; }
        public string Cpf { get; private set; } = default!;
        public string Telefone { get; private set; } = default!;
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

        // Verificação de telefone por SMS (ver VerificacaoSmsService) — quem envia/confere o código
        // é o service, aqui só guarda o resultado final. TermosAceitos/DataAceiteTermos registram o
        // aceite do contrato do motorista (ver AceitarTermos) — nullable porque quem se cadastrou
        // antes dessas exigências existirem não tem esse dado preenchido.
        public bool TelefoneVerificado { get; private set; }
        public bool TermosAceitos { get; private set; }
        public DateTime? DataAceiteTermos { get; private set; }

        public DateTime DataCadastro { get; private set; }

        // Situação disciplinar da conta (ver Suspender/Banir/Reativar) — BloqueadoAte só é relevante
        // quando StatusConta é Suspensa: uma data futura é suspensão temporária (expira sozinha,
        // sem ação do Admin — ver MotoristaService.VerificarBloqueioAsync), null é "definitivamente".
        // MotivoBloqueio é mostrado pro motorista na tela de login enquanto a conta estiver bloqueada.
        public StatusContaMotorista StatusConta { get; private set; }
        public DateTime? BloqueadoAte { get; private set; }
        public string? MotivoBloqueio { get; private set; }

        // Idade máxima aceita pro veículo no cadastro comum — mais permissiva que os 3 anos exigidos
        // pra assinar a categoria Executivo (ver VeiculoElegivelParaExecutivo).
        public const int IdadeMaximaVeiculoAnos = 12;

        protected Motorista() { }



        public Motorista(Guid usuarioId, string nome, string cnh, string cpf, string telefone, string placaVeiculo, string modeloVeiculo, Endereco endereco, int anoVeiculo)
        {

            if (string.IsNullOrWhiteSpace(nome))
                throw new ArgumentException("Nome é obrigatório.");

            if (string.IsNullOrWhiteSpace(cnh))
                throw new ArgumentException("CNH é Obrigatória. ");

            if (!CpfValidator.EhValido(cpf))
                throw new ArgumentException("CPF inválido.");

            if (string.IsNullOrWhiteSpace(telefone))
                throw new ArgumentException("Telefone é obrigatório.");

            if (endereco is null)
                throw new ArgumentException("Endereço é obrigatório.");

            if (anoVeiculo > DateTime.UtcNow.Year)
                throw new ArgumentException("Ano de fabricação do veículo não pode ser no futuro.");

            if (DateTime.UtcNow.Year - anoVeiculo > IdadeMaximaVeiculoAnos)
                throw new ArgumentException($"O veículo precisa ter no máximo {IdadeMaximaVeiculoAnos} anos de fabricação.");

            Id = Guid.NewGuid();
            UsuarioId = usuarioId;
            Nome = nome;
            CNH = cnh;
            Cpf = CpfValidator.Normalizar(cpf);
            Telefone = telefone;
            PlacaVeiculo = placaVeiculo;
            ModeloVeiculo = modeloVeiculo;
            Endereco = endereco;
            Status = StatusMotorista.Offline;
            StatusConta = StatusContaMotorista.Ativa;
            AvaliacaoMedia = 5.0;
            DataCadastro = DateTime.UtcNow;
            AnoVeiculo = anoVeiculo;




        }

        // Edição manual pelo Admin (ver AdminMotoristasPage) — mesma validação básica do cadastro
        // (CPF válido, campos obrigatórios), mas sem travar o ano de fabricação pelo limite de
        // IdadeMaximaVeiculoAnos: o Admin pode estar só corrigindo um erro de digitação em outro
        // campo, não necessariamente aprovando um veículo novo.
        public void AtualizarDados(
            string nome, string cnh, string cpf, string telefone,
            string placaVeiculo, string modeloVeiculo, int? anoVeiculo, Endereco endereco)
        {
            if (string.IsNullOrWhiteSpace(nome))
                throw new ArgumentException("Nome é obrigatório.");

            if (string.IsNullOrWhiteSpace(cnh))
                throw new ArgumentException("CNH é obrigatória.");

            if (!CpfValidator.EhValido(cpf))
                throw new ArgumentException("CPF inválido.");

            if (string.IsNullOrWhiteSpace(telefone))
                throw new ArgumentException("Telefone é obrigatório.");

            if (endereco is null)
                throw new ArgumentException("Endereço é obrigatório.");

            Nome = nome;
            CNH = cnh;
            Cpf = CpfValidator.Normalizar(cpf);
            Telefone = telefone;
            PlacaVeiculo = placaVeiculo;
            ModeloVeiculo = modeloVeiculo;
            AnoVeiculo = anoVeiculo;
            Endereco = endereco;
        }

        // Substituição individual de uma foto pelo Admin (ver AdminMotoristasPage) — diferente de
        // DefinirFotos acima, que é chamado pelo próprio motorista enviando as 3 de uma vez só.
        public void DefinirFotoSelfie(string url) => FotoSelfieUrl = url;
        public void DefinirFotoVeiculo(string url) => FotoVeiculoUrl = url;
        public void DefinirFotoPlaca(string url) => FotoPlacaUrl = url;

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

        // Chamado pelo VerificacaoSmsService depois que o motorista informa o código de 6 dígitos
        // recebido por SMS (ver ISmsService) — nunca é chamado direto, sempre por trás da conferência
        // do código.
        public void VerificarTelefone() => TelefoneVerificado = true;

        // Chamado quando o motorista aceita o contrato/termos de uso na tela dedicada do app — bloqueia
        // o resto do fluxo até acontecer (ver MotoristaController). Idempotente: aceitar de novo só
        // atualiza a data.
        public void AceitarTermos()
        {
            TermosAceitos = true;
            DataAceiteTermos = DateTime.UtcNow;
        }

        // Suspensão temporária (suspensoAte informado, ex: daqui 7 dias) ou por tempo indeterminado
        // (suspensoAte null — o Admin escolheu "definitivamente") — usada quando o motorista viola
        // algum termo do app. Diferente de Banir: aqui a conta pode voltar a funcionar sozinha quando
        // o prazo passar, sem o Admin precisar reativar manualmente.
        public void Suspender(string motivo, DateTime? suspensoAte)
        {
            if (string.IsNullOrWhiteSpace(motivo))
                throw new ArgumentException("Informe o motivo da suspensão.");

            StatusConta = StatusContaMotorista.Suspensa;
            BloqueadoAte = suspensoAte;
            MotivoBloqueio = motivo;
        }

        // Exclusão por violação — diferente de Excluir() acima (que é a exclusão voluntária do
        // próprio motorista, com anonimização): aqui os dados pessoais NÃO são apagados, porque o
        // Admin pode precisar deles depois (recurso, obrigação legal). Só bloqueia o login
        // permanentemente, mostrando o motivo.
        public void Banir(string motivo)
        {
            if (string.IsNullOrWhiteSpace(motivo))
                throw new ArgumentException("Informe o motivo da exclusão.");

            StatusConta = StatusContaMotorista.Banida;
            BloqueadoAte = null;
            MotivoBloqueio = motivo;
        }

        // Desfaz uma suspensão ou exclusão por engano — volta a poder logar normalmente.
        public void Reativar()
        {
            StatusConta = StatusContaMotorista.Ativa;
            BloqueadoAte = null;
            MotivoBloqueio = null;
        }

        // Categoria Executivo exige veículo com até 3 anos de fabricação (contando a partir do ano atual).
        public bool VeiculoElegivelParaExecutivo() => AnoVeiculo is not null && DateTime.UtcNow.Year - AnoVeiculo.Value <= 3;

        // Chamado pelo AvaliacaoService depois de qualquer avaliação nova de um cliente sobre esse
        // motorista — sempre a média recalculada de TODAS as avaliações recebidas, nunca incrementada.
        public void DefinirAvaliacaoMedia(double media) => AvaliacaoMedia = media;

        // Exclusão de conta (ver AuthController.ExcluirContaMotorista) — mesmo padrão do
        // Cliente.Excluir: não é DELETE de verdade (Corridas/TransacaoCarteiraMotorista têm FK
        // Restrict pra Motorista, histórico financeiro precisa ser preservado por obrigação legal),
        // então só anonimiza os dados pessoais e as fotos de documento aqui — quem chama isso
        // também bloqueia o login da conta (ver UserManager no AuthController).
        public void Excluir()
        {
            Nome = "Removido";
            CNH = "REMOVIDA";
            Cpf = "00000000000";
            Telefone = "";
            PlacaVeiculo = "Removida";
            ModeloVeiculo = "Removido";
            Endereco = new Endereco("Removido", "0", "Removido", "Removido", "SP", 0, 0);
            FotoSelfieUrl = null;
            FotoVeiculoUrl = null;
            FotoPlacaUrl = null;
        }








    }
}
