using OrderApi.Services;
using Shared;
using Xunit;

public class PedidoStoreTests
{
    [Fact]
    public async Task AtualizacaoPublicaDadosAtuaisEStatusAnterior()
    {
        var store = new PedidoStore();
        var producer = new ProducerFake();
        var criado = new PedidoCriadoEvent(Guid.NewGuid(), "Ana", "ana@example.com", 50, DateTime.UtcNow);
        await store.CriarAsync(criado, producer);
        var atualizado = await store.AtualizarAsync(criado.PedidoId, "Enviado", 65, producer);
        var evento = Assert.Single(producer.Eventos);
        Assert.Equal(criado.PedidoId, evento.PedidoId);
        Assert.Equal("Criado", evento.StatusAnterior);
        Assert.Equal("Enviado", evento.StatusAtual);
        Assert.Equal(65, evento.ValorTotal);
        Assert.Equal(criado.ClienteEmail, evento.ClienteEmail);
        Assert.Equal(criado.ClienteNome, evento.ClienteNome);
        Assert.NotEqual(Guid.Empty, evento.EventoId);
        Assert.Equal(atualizado!.DataAtualizacao, evento.DataAtualizacao);
        Assert.Equal(atualizado, await store.ObterAsync(criado.PedidoId));
        await store.AtualizarAsync(criado.PedidoId, "Entregue", 65, producer);
        Assert.Equal("Enviado", producer.Eventos[1].StatusAnterior);
        Assert.NotEqual(evento.EventoId, producer.Eventos[1].EventoId);
    }

    [Fact]
    public async Task PedidoInexistenteNaoPublicaEvento()
    {
        var producer = new ProducerFake();
        Assert.Null(await new PedidoStore().AtualizarAsync(Guid.NewGuid(), "Pago", 10, producer));
        Assert.Empty(producer.Eventos);
    }

    [Fact]
    public async Task FalhaNoBrokerNaoAlteraPedido()
    {
        var store = new PedidoStore();
        var producer = new ProducerFake();
        var criado = new PedidoCriadoEvent(Guid.NewGuid(), "Ana", "ana@example.com", 50, DateTime.UtcNow);
        var anterior = await store.CriarAsync(criado, producer);
        producer.Falhar = true;
        await Assert.ThrowsAsync<IOException>(() => store.AtualizarAsync(criado.PedidoId, "Pago", 99, producer));
        Assert.Equal(anterior, await store.ObterAsync(criado.PedidoId));
        Assert.Empty(producer.Eventos);
    }

    [Fact]
    public async Task AtualizacoesConcorrentesPreservamSequenciaDeStatus()
    {
        var store = new PedidoStore();
        var producer = new ProducerFake();
        var criado = new PedidoCriadoEvent(Guid.NewGuid(), "Ana", "ana@example.com", 50, DateTime.UtcNow);
        await store.CriarAsync(criado, producer);
        await Task.WhenAll(Enumerable.Range(1, 20).Select(i =>
            store.AtualizarAsync(criado.PedidoId, $"Etapa {i}", i, producer)));
        Assert.Equal(20, producer.Eventos.Count);
        Assert.Equal("Criado", producer.Eventos[0].StatusAnterior);
        for (var i = 1; i < 20; i++)
            Assert.Equal(producer.Eventos[i - 1].StatusAtual, producer.Eventos[i].StatusAnterior);
    }

    private sealed class ProducerFake : IPedidoProducer
    {
        public List<PedidoAtualizadoEvent> Eventos { get; } = new();
        public bool Falhar { get; set; }
        public Task PublicarPedidoAsync(PedidoCriadoEvent evento) => Task.CompletedTask;
        public async Task PublicarAtualizacaoAsync(PedidoAtualizadoEvent evento)
        {
            await Task.Yield();
            if (Falhar) throw new IOException("Broker indisponivel");
            Eventos.Add(evento);
        }
    }
}
