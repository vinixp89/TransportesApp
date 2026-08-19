using System.Linq;

namespace TransportesApp.Domain.Common
{
    public static class CpfValidator
    {
        public static bool EhValido(string cpf)
        {
            if (string.IsNullOrWhiteSpace(cpf))
                return false;

            var digitos = Normalizar(cpf);

            if (digitos.Length != 11)
                return false;

            // CPFs com todos os dígitos iguais (000.000.000-00, 111.111.111-11, etc.) não são válidos.
            if (digitos.Distinct().Count() == 1)
                return false;

            var numeros = digitos.Select(c => c - '0').ToArray();

            var primeiros9 = numeros.Take(9).ToArray();
            var digito1 = CalcularDigitoVerificador(primeiros9, 10);

            var primeiros10 = primeiros9.Append(digito1).ToArray();
            var digito2 = CalcularDigitoVerificador(primeiros10, 11);

            return numeros[9] == digito1 && numeros[10] == digito2;
        }

        public static string Normalizar(string cpf)
            => new string((cpf ?? string.Empty).Where(char.IsDigit).ToArray());

        private static int CalcularDigitoVerificador(int[] numeros, int pesoInicial)
        {
            var soma = 0;

            for (var i = 0; i < numeros.Length; i++)
                soma += numeros[i] * (pesoInicial - i);

            var resto = soma % 11;

            return resto < 2 ? 0 : 11 - resto;
        }
    }
}
