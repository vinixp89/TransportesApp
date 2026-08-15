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

        public async Task<ClienteResponse> CriarAsync(CriarClienteRequest request)
        {
            var endereco = new Endereco(
                logradouro: request.Logradouro,
                numero: request.Numero,
                bairro: request.Bairro,
                cidade: request.Cidade,
                estado: request.Estado,
                latitude: request.Latitude,
                longitude: request.Longitude,
                complemento: request.Complemento
            );

            var cliente = new Cliente(
                nome: request.Nome,
                telefone: request.Telefone,
                email: request.Email,
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

        private static ClienteResponse MapearParaResponse(Cliente cliente)
        {
            return new ClienteResponse(
                cliente.Id,
                cliente.Nome,
                cliente.Telefone,
                cliente.Email,
                cliente.AvaliacaoMedia,
                cliente.DataCadastro
            );
        }
    }
}