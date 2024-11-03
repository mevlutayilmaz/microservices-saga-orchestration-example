using MassTransit;
using MongoDB.Driver;
using Shared.Messages;
using Stock.API.Services;

namespace Stock.API.Consumers
{
    public class StockRollbackMessageConsumer(MongoDBService mongoDBService) : IConsumer<StockRollbackMessage>
    {
        public async Task Consume(ConsumeContext<StockRollbackMessage> context)
        {
            IMongoCollection<Entities.Stock> stockCollection = mongoDBService.GetCollection<Entities.Stock>();

            foreach (var orderItem in context.Message.OrderItems)
            {
                Entities.Stock stock = await ((await stockCollection.FindAsync(s => s.ProductId == orderItem.ProductId.ToString())).FirstOrDefaultAsync());
                stock.Count += orderItem.Count;

                await stockCollection.FindOneAndReplaceAsync(s => s.ProductId == orderItem.ProductId.ToString(), stock);
            }
        }
    }
}
