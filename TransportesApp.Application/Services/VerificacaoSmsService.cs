using Microsoft.Extensions.Caching.Memory;
using TransportesApp.Domain.Enums;
using TransportesApp.Domain.Interfaces;

namespace TransportesApp.Application.Services
{
    // Verificação de telefone por SMS, comum a Cliente e Motorista — mesmo princípio do código de
    // redefinição de senha em AuthController (6 dígitos, guardado em memória por tempo limitado, sem
    // tabela nova no banco pra algo tão efêmero). Centralizado aqui (em vez de duplicado dentro de
    // ClienteService/MotoristaService) porque a lógica é idêntica pros dois lados, só muda o
    // repositório consultado.
    public class VerificacaoSmsService
    {
        private readonly IClienteRepository _clienteRepository;
        private readonly IMotoristaRepository _motoristaRepository;
        private readonly ISmsService _smsService;
        private readonly IMemoryCache _cache;

        private static readonly MemoryCacheEntryOptions CodigoOptions = new()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        };

        public VerificacaoSmsService(
            IClienteRepository clienteRepository,
            IMotoristaRepository motoristaRepository,
            ISmsService smsService,
            IMemoryCache cache)
        {
            _clienteRepository = clienteRepository;
            _motoristaRepository = motoristaRepository;
            _smsService = smsService;
            _cache = cache;
        }

        public async Task EnviarCodigoAsync(TipoUsuario tipo, Guid usuarioId)
        {
            var telefone = await ObterTelefoneAsync(tipo, usuarioId)
                ?? throw new InvalidOperationException("Cadastro não encontrado pra essa conta.");

            var codigo = Random.Shared.Next(0, 1_000_000).ToString("D6");
            _cache.Set(ChaveCache(tipo, usuarioId), codigo, CodigoOptions);

            await _smsService.EnviarAsync(telefone, $"Seu código de confirmação Vai na Boa é {codigo}. Válido por 10 minutos.");
        }

        // Devolve false pra código inválido/expirado ou cadastro não encontrado — quem chama decide
        // a mensagem de erro (ver AuthController), aqui só a regra de negócio.
        public async Task<bool> ConfirmarCodigoAsync(TipoUsuario tipo, Guid usuarioId, string codigoInformado)
        {
            if (!_cache.TryGetValue(ChaveCache(tipo, usuarioId), out string? codigoValido) || codigoValido != codigoInformado)
                return false;

            var confirmou = tipo == TipoUsuario.Cliente
                ? await ConfirmarClienteAsync(usuarioId)
                : await ConfirmarMotoristaAsync(usuarioId);

            if (!confirmou)
                return false;

            _cache.Remove(ChaveCache(tipo, usuarioId));
            return true;
        }

        private async Task<bool> ConfirmarClienteAsync(Guid usuarioId)
        {
            var cliente = await _clienteRepository.ObterPorUsuarioIdAsync(usuarioId);
            if (cliente is null)
                return false;

            cliente.VerificarTelefone();
            await _clienteRepository.AtualizarAsync(cliente);
            return true;
        }

        private async Task<bool> ConfirmarMotoristaAsync(Guid usuarioId)
        {
            var motorista = await _motoristaRepository.ObterPorUsuarioIdAsync(usuarioId);
            if (motorista is null)
                return false;

            motorista.VerificarTelefone();
            await _motoristaRepository.AtualizarAsync(motorista);
            return true;
        }

        private async Task<string?> ObterTelefoneAsync(TipoUsuario tipo, Guid usuarioId)
        {
            if (tipo == TipoUsuario.Cliente)
                return (await _clienteRepository.ObterPorUsuarioIdAsync(usuarioId))?.Telefone;

            return (await _motoristaRepository.ObterPorUsuarioIdAsync(usuarioId))?.Telefone;
        }

        private static string ChaveCache(TipoUsuario tipo, Guid usuarioId) => $"verificar-sms:{tipo}:{usuarioId}";
    }
}
