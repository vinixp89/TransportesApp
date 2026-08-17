using System;
using System.Collections.Generic;
using System.Text;

namespace TransportesApp.Application.DTOs
{
    public record CriarMotoristaRequest
        (
                Guid UsuarioId,
                string Cnh,
                string PlacaVeiculo,
                string ModeloVeiculo






        );
    public record MotoristaResponse
        (
            Guid Id,
            Guid UsuarioId,
            string Cnh,
            string PlacaVeiculo,
            string ModeloVeiculo,
            double AvaliacaoMeida,
            DateTime DataCadastro

        
        
        
        
        
        );
    



    
}
