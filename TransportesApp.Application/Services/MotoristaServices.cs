using TransportesApp.Application.DTOs;
using TransportesApp.Domain.Entities;
using TransportesApp.Domain.Enums;
using TransportesApp.Domain.Interfaces;
using TransportesApp.Domain.ValueObjects;

namespace TransportesApp.Application.Services
{
    public class MotoristaService
    {
        private readonly IMotoristaRepository _motoristaRepository;

        public MotoristaService(IMotoristaRepository motoristaRepository)
        {
            _motoristaRepository = motoristaRepository;
        }

        public async Task<MotoristaResponse> CriarAsync(CriarMotoristaRequest request, Guid usuarioId)
        {
            var endereco = new Endereco(
                logradouro: request.Logradouro,
                numero: request.Numero,
                bairro: request.Bairro,
                cidade: request.Cidade,
                estado: request.Estado,
                latitude: request.Latitude ?? 0,
                longitude: request.Longitude ?? 0,
                complemento: request.Complemento
            );

            var motorista = new Motorista(
                usuarioId: usuarioId,
                nome: request.Nome,
                cnh: request.Cnh,
                cpf: request.Cpf,
                telefone: request.Telefone,
                placaVeiculo: request.PlacaVeiculo,
                modeloVeiculo: request.ModeloVeiculo,
                endereco: endereco,
                anoVeiculo: request.AnoVeiculo
            );

            await _motoristaRepository.AdicionarAsync(motorista);

            return MapearParaResponse(motorista);
        }

        public async Task<MotoristaResponse?> ObterPorIdAsync(Guid id)
        {
            var motorista = await _motoristaRepository.ObterPorIdAsync(id);

            return motorista is null ? null : MapearParaResponse(motorista);
        }

        public async Task<MotoristaResponse?> ObterPorUsuarioIdAsync(Guid usuarioId)
        {
            var motorista = await _motoristaRepository.ObterPorUsuarioIdAsync(usuarioId);

            return motorista is null ? null : MapearParaResponse(motorista);
        }

        // Salva os caminhos das 3 fotos de verificação — quem já salvou os arquivos em disco e gerou
        // essas URLs é o controller (MotoristasController.EnviarFotos), que lida com IFormFile/disco;
        // aqui só persiste as strings na entidade.
        public async Task<MotoristaResponse?> DefinirFotosAsync(
            Guid usuarioId, string fotoSelfieUrl, string fotoVeiculoUrl, string fotoPlacaUrl)
        {
            var motorista = await _motoristaRepository.ObterPorUsuarioIdAsync(usuarioId);

            if (motorista is null)
                return null;

            motorista.DefinirFotos(fotoSelfieUrl, fotoVeiculoUrl, fotoPlacaUrl);
            await _motoristaRepository.AtualizarAsync(motorista);

            return MapearParaResponse(motorista);
        }

        // Aceite dos termos/contrato do motorista (ver Motorista.AceitarTermos) — chamado pela tela
        // dedicada no cadastro (ver MotoristasController.AceitarTermos).
        public async Task<MotoristaResponse?> AceitarTermosAsync(Guid usuarioId)
        {
            var motorista = await _motoristaRepository.ObterPorUsuarioIdAsync(usuarioId);

            if (motorista is null)
                return null;

            motorista.AceitarTermos();
            await _motoristaRepository.AtualizarAsync(motorista);

            return MapearParaResponse(motorista);
        }

        // Caminho relativo cru da selfie, de propósito fora do MotoristaResponse (que só expõe o
        // booleano FotosEnviadas) — usado só internamente por CorridasController.ObterFotoMotorista
        // pra localizar o arquivo em disco e devolvê-lo como binário, atrás de autorização de que o
        // cliente pedindo é dono de uma corrida em andamento com esse motorista.
        public async Task<string?> ObterCaminhoFotoSelfieAsync(Guid motoristaId)
        {
            var motorista = await _motoristaRepository.ObterPorIdAsync(motoristaId);
            return motorista?.FotoSelfieUrl;
        }

        // Mesma ideia de ObterCaminhoFotoSelfieAsync, mas pras fotos de veículo/placa — usado pelo
        // AdminExecutivoController pra deixar o Admin conferir a placa declarada contra a foto de
        // verdade antes de aprovar a categoria Executivo (ver AssinaturaMotoristaExecutivoService).
        public async Task<string?> ObterCaminhoFotoVeiculoAsync(Guid motoristaId)
        {
            var motorista = await _motoristaRepository.ObterPorIdAsync(motoristaId);
            return motorista?.FotoVeiculoUrl;
        }

        public async Task<string?> ObterCaminhoFotoPlacaAsync(Guid motoristaId)
        {
            var motorista = await _motoristaRepository.ObterPorIdAsync(motoristaId);
            return motorista?.FotoPlacaUrl;
        }

        // Suspende ou exclui a conta por violação de termos — usado pelo Admin (ver
        // MotoristasController). Lança ArgumentException se o motivo vier vazio (ver Motorista.Suspender).
        public async Task<MotoristaResponse?> SuspenderAsync(Guid motoristaId, string motivo, int? dias)
        {
            var motorista = await _motoristaRepository.ObterPorIdAsync(motoristaId);

            if (motorista is null)
                return null;

            var suspensoAte = dias is null ? (DateTime?)null : DateTime.UtcNow.AddDays(dias.Value);
            motorista.Suspender(motivo, suspensoAte);
            await _motoristaRepository.AtualizarAsync(motorista);

            return MapearParaResponse(motorista);
        }

        public async Task<MotoristaResponse?> BanirAsync(Guid motoristaId, string motivo)
        {
            var motorista = await _motoristaRepository.ObterPorIdAsync(motoristaId);

            if (motorista is null)
                return null;

            motorista.Banir(motivo);
            await _motoristaRepository.AtualizarAsync(motorista);

            return MapearParaResponse(motorista);
        }

        public async Task<MotoristaResponse?> ReativarAsync(Guid motoristaId)
        {
            var motorista = await _motoristaRepository.ObterPorIdAsync(motoristaId);

            if (motorista is null)
                return null;

            motorista.Reativar();
            await _motoristaRepository.AtualizarAsync(motorista);

            return MapearParaResponse(motorista);
        }

        // Checado no login (AuthController) e a cada requisição autenticada (ver OnTokenValidated no
        // Program.cs) — garante que uma suspensão/exclusão feita pelo Admin já bloqueia o motorista na
        // hora, mesmo que ele já estivesse logado. Suspensão temporária vencida se auto-reativa aqui
        // (sem o Admin precisar fazer nada), então quem chama nem precisa saber que isso aconteceu.
        public async Task<(bool Bloqueado, string? Mensagem)> VerificarBloqueioAsync(Guid usuarioId)
        {
            var motorista = await _motoristaRepository.ObterPorUsuarioIdAsync(usuarioId);

            if (motorista is null)
                return (false, null);

            if (motorista.StatusConta == StatusContaMotorista.Suspensa
                && motorista.BloqueadoAte is not null && motorista.BloqueadoAte <= DateTime.UtcNow)
            {
                motorista.Reativar();
                await _motoristaRepository.AtualizarAsync(motorista);
                return (false, null);
            }

            if (motorista.StatusConta == StatusContaMotorista.Banida)
                return (true, $"Sua conta foi excluída do Vai na Boa. Motivo: {motorista.MotivoBloqueio}");

            if (motorista.StatusConta == StatusContaMotorista.Suspensa)
            {
                var ate = motorista.BloqueadoAte is null
                    ? "definitivamente"
                    : $"até {motorista.BloqueadoAte:dd/MM/yyyy}";
                return (true, $"Sua conta está suspensa {ate}. Motivo: {motorista.MotivoBloqueio}");
            }

            return (false, null);
        }

        public async Task<IEnumerable<MotoristaResponse>> ListarAsync()
        {
            var motoristas = await _motoristaRepository.ListarAsync();

            return motoristas.Select(MapearParaResponse);
        }

        public async Task<IEnumerable<MotoristaResponse>> ListarDisponiveisAsync()
        {
            var motoristas = await _motoristaRepository.ListarDisponiveisAsync();

            return motoristas.Select(MapearParaResponse);
        }

        public async Task<MotoristaResponse?> FicarDisponivelAsync(Guid usuarioId)
        {
            var motorista = await _motoristaRepository.ObterPorUsuarioIdAsync(usuarioId);

            if (motorista is null)
                return null;

            motorista.FicarDisponivel();

            await _motoristaRepository.AtualizarAsync(motorista);

            return MapearParaResponse(motorista);
        }

        public async Task<MotoristaResponse?> FicarOfflineAsync(Guid usuarioId)
        {
            var motorista = await _motoristaRepository.ObterPorUsuarioIdAsync(usuarioId);

            if (motorista is null)
                return null;

            motorista.FicarOffline();

            await _motoristaRepository.AtualizarAsync(motorista);

            return MapearParaResponse(motorista);
        }

        public async Task<bool> ExcluirContaAsync(Guid usuarioId)
        {
            var motorista = await _motoristaRepository.ObterPorUsuarioIdAsync(usuarioId);

            if (motorista is null)
                return false;

            motorista.Excluir();
            await _motoristaRepository.AtualizarAsync(motorista);

            return true;
        }

        public async Task<MotoristaResponse?> AtualizarLocalizacaoAsync(Guid usuarioId, double latitude, double longitude)
        {
            var motorista = await _motoristaRepository.ObterPorUsuarioIdAsync(usuarioId);

            if (motorista is null)
                return null;

            motorista.AtualizarLocalizacao(latitude, longitude);

            await _motoristaRepository.AtualizarAsync(motorista);

            return MapearParaResponse(motorista);
        }

        // Versão pra cliente: sem CPF/CNH, e com distância calculada se a posição do cliente for informada.
        public async Task<IEnumerable<MotoristaDisponivelResponse>> ListarDisponiveisResumoAsync(
            double? latitude, double? longitude, double? raioKm)
        {
            var motoristas = await _motoristaRepository.ListarDisponiveisAsync();

            var resultado = motoristas.Select(m =>
            {
                double? distanciaKm = null;

                if (latitude is not null && longitude is not null
                    && m.LatitudeAtual is not null && m.LongitudeAtual is not null)
                {
                    distanciaKm = CalcularDistanciaKm(
                        latitude.Value, longitude.Value,
                        m.LatitudeAtual.Value, m.LongitudeAtual.Value);
                }

                return new MotoristaDisponivelResponse(
                    m.Id,
                    m.PlacaVeiculo,
                    m.ModeloVeiculo,
                    m.AvaliacaoMedia,
                    m.LatitudeAtual,
                    m.LongitudeAtual,
                    distanciaKm
                );
            });

            if (latitude is not null && longitude is not null)
            {
                // Sem localização atual registrada, não dá pra saber a distância — não entra no resultado.
                resultado = resultado.Where(m => m.DistanciaKm is not null);

                if (raioKm is not null)
                    resultado = resultado.Where(m => m.DistanciaKm <= raioKm);

                resultado = resultado.OrderBy(m => m.DistanciaKm);
            }

            return resultado;
        }

        private static double CalcularDistanciaKm(double lat1, double lon1, double lat2, double lon2)
        {
            const double raioTerraKm = 6371;

            var dLat = ParaRadianos(lat2 - lat1);
            var dLon = ParaRadianos(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ParaRadianos(lat1)) * Math.Cos(ParaRadianos(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return raioTerraKm * c;
        }

        private static double ParaRadianos(double graus) => graus * Math.PI / 180;

        private static MotoristaResponse MapearParaResponse(Motorista motorista)
        {
            return new MotoristaResponse(
                motorista.Id,
                motorista.UsuarioId,
                motorista.Nome,
                motorista.CNH,
                motorista.Cpf,
                motorista.Telefone,
                motorista.PlacaVeiculo,
                motorista.ModeloVeiculo,
                motorista.AnoVeiculo,
                motorista.AvaliacaoMedia,
                motorista.DataCadastro,
                new EnderecoResponse(
                    motorista.Endereco.Logradouro,
                    motorista.Endereco.Numero,
                    motorista.Endereco.Bairro,
                    motorista.Endereco.Cidade,
                    motorista.Endereco.Estado,
                    motorista.Endereco.Latitude,
                    motorista.Endereco.Longitude,
                    motorista.Endereco.Complemento
                ),
                motorista.Status,
                motorista.LatitudeAtual,
                motorista.LongitudeAtual,
                motorista.FotoSelfieUrl is not null && motorista.FotoVeiculoUrl is not null && motorista.FotoPlacaUrl is not null,
                motorista.TelefoneVerificado,
                motorista.TermosAceitos,
                motorista.StatusConta,
                motorista.BloqueadoAte,
                motorista.MotivoBloqueio
            );
        }
    }
}