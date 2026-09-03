using TransportesApp.Application.DTOs;
using TransportesApp.Domain.Entities;
using TransportesApp.Domain.Interfaces;
using TransportesApp.Domain.ValueObjects;

namespace TransportesApp.Application.Services
{
    public class ClienteService
    {
        private readonly IClienteRepository _clienteRepository;

        public ClienteService(IClienteRepository clienteRepository)
        {
            _clienteRepository = clienteRepository;
        }

        public async Task<ClienteResponse> CriarAsync(CriarClienteRequest request, Guid usuarioId, string email)
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

            var cliente = new Cliente(
                usuarioId: usuarioId,
                nome: request.Nome,
                cpf: request.Cpf,
                telefone: request.Telefone,
                email: email,
                endereco: endereco
            );

            await _clienteRepository.AdicionarAsync(cliente);

            return MapearParaResponse(cliente);
        }

        public async Task<ClienteResponse?> ObterPorIdAsync(Guid id)
        {
            var cliente = await _clienteRepository.ObterPorIdAsync(id);

            return cliente is null ? null : MapearParaResponse(cliente);
        }

        public async Task<ClienteResponse?> ObterPorUsuarioIdAsync(Guid usuarioId)
        {
            var cliente = await _clienteRepository.ObterPorUsuarioIdAsync(usuarioId);

            return cliente is null ? null : MapearParaResponse(cliente);
        }

        public async Task<IEnumerable<ClienteResponse>> ListarAsync()
        {
            var clientes = await _clienteRepository.ListarAsync();

            return clientes.Select(MapearParaResponse);
        }

        // Anonimiza os dados do Cliente (ver Cliente.Excluir) — quem chama isso (AuthController)
        // também bloqueia o login da conta via UserManager, já que isso aqui não mexe em Identity.
        public async Task<bool> ExcluirContaAsync(Guid usuarioId)
        {
            var cliente = await _clienteRepository.ObterPorUsuarioIdAsync(usuarioId);

            if (cliente is null)
                return false;

            cliente.Excluir();
            await _clienteRepository.AtualizarAsync(cliente);

            return true;
        }

        private static ClienteResponse MapearParaResponse(Cliente cliente)
        {
            return new ClienteResponse(
                cliente.Id,
                cliente.UsuarioId,
                cliente.Nome,
                cliente.Cpf,
                cliente.Telefone,
                cliente.Email,
                cliente.AvaliacaoMedia,
                cliente.DataCadastro
            );
        }
    }
}