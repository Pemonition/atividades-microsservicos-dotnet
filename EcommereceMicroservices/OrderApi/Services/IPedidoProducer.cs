using Shared;

namespace OrderApi.Services;

public interface IPedidoProducer
{
    Task PublicarPedidoAsync(PedidoCriadoEvent evento);
    Task PublicarAtualizacaoAsync(PedidoAtualizadoEvent evento);
}
