using MassTransit;
using Order.API.Contexts;
using Order.API.Enums;
using Shared.OrderEvents;

namespace Order.API.Consumers
{
    public class OrderFailedEventConsumer(OrderAPIDbContext _context) : IConsumer<OrderFailedEvent>
    {
        public async Task Consume(ConsumeContext<OrderFailedEvent> context)
        {
            Entities.Order order = await _context.Orders.FindAsync(context.Message.OrderId);
            if (order is null)
                throw new NullReferenceException();
            order.OrderStatus = OrderStatus.Fail;
            await _context.SaveChangesAsync();
        }
    }
}
