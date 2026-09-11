using System.Text.RegularExpressions;
using TransportesApp.Domain.Common;
using TransportesApp.Domain.ValueObjects;

namespace TransportesApp.Domain.Entities
{
    public class Cliente
    {
        private static readonly Regex EmailRegex = new(
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            RegexOptions.Compiled);

        public Guid Id { get; private set; }
        public Guid UsuarioId { get; private set; }
        public string Nome { get; private set; }
        public string Cpf { get; private set; }
        public string Telefone { get; private set; }
        public string Email { get; private set; }
        public Endereco Endereco { get; private set; }
        public double? AvaliacaoMedia { get; private set; }
        public DateTime DataCadastro { get; private set; }

        // Selfie pedida no cadastro (ver ClienteService.DefinirFotoSelfieAsync) — mesmo padrão do
        // Motorista.FotoSelfieUrl: guarda o caminho relativo salvo em disco, não o binário, e é
        // nullable porque quem se cadastrou antes dessa exigência existir não tem essa foto ainda.
        public string? FotoSelfieUrl { get; private set; }

        // Verificação de telefone por SMS e aceite dos termos de uso — mesmo padrão do Motorista
        // (ver Motorista.VerificarTelefone/AceitarTermos).
        public bool TelefoneVerificado { get; private set; }
        public bool TermosAceitos { get; private set; }
        public DateTime? DataAceiteTermos { get; private set; }

        protected Cliente() { }

        public Cliente(Guid usuarioId, string nome, string cpf, string telefone, string email, Endereco endereco)
        {
            if (string.IsNullOrWhiteSpace(nome))
                throw new ArgumentException("Nome é obrigatório.");

            if (!CpfValidator.EhValido(cpf))
                throw new ArgumentException("CPF inválido.");

            if (string.IsNullOrWhiteSpace(telefone))
                throw new ArgumentException("Telefone é obrigatório.");

            if (string.IsNullOrWhiteSpace(email) || !EmailRegex.IsMatch(email))
                throw new ArgumentException("Email inválido.");

            if (endereco is null)
                throw new ArgumentException("Endereço é obrigatório.");

            Id = Guid.NewGuid();
            UsuarioId = usuarioId;
            Nome = nome;
            Cpf = CpfValidator.Normalizar(cpf);
            Telefone = telefone;
            Email = email;
            Endereco = endereco;
            AvaliacaoMedia = null;
            DataCadastro = DateTime.UtcNow;
        }

        public void DefinirFotoSelfie(string fotoSelfieUrl) => FotoSelfieUrl = fotoSelfieUrl;

        public void VerificarTelefone() => TelefoneVerificado = true;

        public void AceitarTermos()
        {
            TermosAceitos = true;
            DataAceiteTermos = DateTime.UtcNow;
        }

        // Exclusão de conta (ver AuthController.ExcluirConta) — não é um DELETE de verdade, porque
        // Corridas/PacoteCorridas/TransacaoCarteira têm FK Restrict pra Cliente (histórico financeiro
        // precisa ser preservado por obrigação legal, ver seção 7 da política de privacidade). Em vez
        // disso, anonimiza os dados pessoais aqui e quem chama isso também bloqueia o login da conta
        // (ver UserManager no AuthController) — na prática a conta fica inacessível e sem dados
        // identificáveis, que é o resultado que importa pro usuário.
        public void Excluir()
        {
            Nome = "Conta excluída";
            Cpf = "00000000000";
            Telefone = "";
            Email = $"excluido-{Id:N}@vainaboamobilidade.com.br";
            Endereco = new Endereco("Removido", "0", "Removido", "Removido", "SP", 0, 0);
            FotoSelfieUrl = null;
        }
    }
}