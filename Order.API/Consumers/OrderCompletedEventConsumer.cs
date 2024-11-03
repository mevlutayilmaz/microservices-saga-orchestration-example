using MassTransit;
using Order.API.Contexts;
using Order.API.Enums;
using Shared.OrderEvents;

namespace Order.API.Consumers
{
    public class OrderCompletedEventConsumer(OrderAPIDbContext _context) : IConsumer<OrderCompletedEvent>
    {
        public async Task Consume(ConsumeContext<OrderCompletedEvent> context)
        {
            Entities.Order order = await _context.Orders.FindAsync(context.Message.OrderId);
            if (order is null)
                throw new NullReferenceException();
            order.OrderStatus = OrderStatus.Completed;
            await _context.SaveChangesAsync();
        }
    }
}
