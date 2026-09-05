namespace TransportesApp.Domain.ValueObjects
{
    public sealed class Endereco
    {

        public string Logradouro { get; }
        public string Numero { get; }

        public string Bairro { get; }

        public string Cidade { get; }
        public string Estado { get; }
        public string? Complemento { get; }
        public double Latitude { get; }
        public double Longitude { get; }

        private Endereco() { }

        public Endereco(string logradouro, string numero,string bairro, string cidade,string estado,double latitude,double longitude, string? complemento =null)
        
        {

            if (string.IsNullOrWhiteSpace(logradouro))
                throw new ArgumentException("logradouro é obrigatorio");

            if (string.IsNullOrWhiteSpace(numero))
                throw new ArgumentException("o numero é obrigatorio");

            if (string.IsNullOrWhiteSpace(cidade))
                throw new ArgumentException("A cidade é obrigatoria");

            // Coluna do banco é varchar(2) (sigla de UF, ex: "RJ") — o Google Maps normalmente devolve
            // a sigla certinha, mas já apareceu caso de vir o nome do estado por extenso. Trunca em vez
            // de deixar o INSERT inteiro quebrar por causa de um campo que é só informativo.
            var estadoSaneado = string.IsNullOrWhiteSpace(estado)
                ? ""
                : estado.Trim()[..Math.Min(2, estado.Trim().Length)];

                    Logradouro = logradouro;
                    Numero = numero;
                    Bairro = bairro;
                    Cidade = cidade;
                    Estado = estadoSaneado;
                    Complemento = complemento;
                    Latitude = latitude;
                    Longitude = longitude;



                    
        
        }
        public override bool Equals(object? obj)
        {
            if (obj is not Endereco outro) return false;
            return Latitude.Equals(outro.Latitude) && Longitude.Equals(outro.Longitude);
        }
        public override int GetHashCode() => HashCode.Combine(Latitude, Longitude);

        public override string ToString() => $"{Logradouro},{Numero} - {Bairro}, {Cidade}/ {Estado}";
       
      
    }
}
