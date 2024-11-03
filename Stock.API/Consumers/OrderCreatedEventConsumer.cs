using MassTransit;
using MongoDB.Driver;
using Shared;
using Shared.OrderEvents;
using Shared.StockEvents;
using Stock.API.Services;

namespace Stock.API.Consumers
{
    public class OrderCreatedEventConsumer(MongoDBService mongoDBService, ISendEndpointProvider sendEndpointProvider) : IConsumer<OrderCreatedEvent>
    {
        public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
        {
            List<bool> stockResults = new();
            IMongoCollection<Entities.Stock> stockCollection = mongoDBService.GetCollection<Entities.Stock>();
            
            foreach (var orderItem in context.Message.OrderItems)
                stockResults.Add((await stockCollection.FindAsync(s => s.ProductId == orderItem.ProductId.ToString() && s.Count >= orderItem.Count)).Any());

            ISendEndpoint sendEndpoint = await sendEndpointProvider.GetSendEndpoint(new Uri($"queue:{RabbitMQSettings.StateMachineQueue}"));

            if (stockResults.TrueForAll(x => x.Equals(true)))
            {
                foreach (var orderItem in context.Message.OrderItems)
                {
                    Entities.Stock stock = await (await stockCollection.FindAsync(s => s.ProductId == orderItem.ProductId.ToString())).FirstOrDefaultAsync();
                    stock.Count -= orderItem.Count;

                    await stockCollection.FindOneAndReplaceAsync(s => s.ProductId == orderItem.ProductId.ToString(), stock);
                }

                StockReservedEvent stockReservedEvent = new(context.Message.CorrelationId)
                {
                    OrderItems = context.Message.OrderItems
                };

                await sendEndpoint.Send(stockReservedEvent);
                Console.WriteLine("Stok işlemleri başarılı...");
            }
            else
            {

                StockNotReservedEvent stockNotReservedEvent = new(context.Message.CorrelationId)
                {
                    Message = "Stok işlemleri başarısız..."
                };

                await sendEndpoint.Send(stockNotReservedEvent);
                Console.WriteLine("Stok işlemleri başarısız...");
            }
        }
    }
}
