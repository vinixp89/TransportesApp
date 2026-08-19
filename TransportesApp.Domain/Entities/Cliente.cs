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
    }
}