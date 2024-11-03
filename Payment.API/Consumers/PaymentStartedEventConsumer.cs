using MassTransit;
using Shared;
using Shared.PaymentEvents;

namespace Payment.API.Consumers
{
    public class PaymentStartedEventConsumer(ISendEndpointProvider sendEndpointProvider) : IConsumer<PaymentStartedEvent>
    {
        public async Task Consume(ConsumeContext<PaymentStartedEvent> context)
        {
            var sendEndpoint = await sendEndpointProvider.GetSendEndpoint(new Uri($"queue:{RabbitMQSettings.StateMachineQueue}"));
            if (false)
            {

                PaymentCompletedEvent paymentCompletedEvent = new(context.Message.CorrelationId)
                {
                    
                };

                await sendEndpoint.Send(paymentCompletedEvent);

                Console.WriteLine("Payment işlemleri başarılı...");
            }
            else
            {
                PaymentFailedEvent paymentFailedEvent = new(context.Message.CorrelationId)
                {
                    Message = "Payment işlemleri başarısız...",
                    OrderItems = context.Message.OrderItems
                };

                await sendEndpoint.Send(paymentFailedEvent);
                Console.WriteLine("Payment işlemleri başarısız...");
            }
        }
    }
}