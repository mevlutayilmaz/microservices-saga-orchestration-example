using MassTransit;
using Microsoft.EntityFrameworkCore;
using Order.API.Consumers;
using Order.API.Contexts;
using Order.API.Entities;
using Order.API.Enums;
using Order.API.ViewModels;
using Shared;
using Shared.Messages;
using Shared.OrderEvents;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMassTransit(configurator =>
{
    configurator.AddConsumer<OrderCompletedEventConsumer>();
    configurator.AddConsumer<OrderFailedEventConsumer>();

    configurator.UsingRabbitMq((context, _configurator) =>
    {
        _configurator.Host(builder.Configuration["RabbitMQ"]);

        _configurator.ReceiveEndpoint(RabbitMQSettings.Order_OrderCompletedEventQueue, e => e.ConfigureConsumer<OrderCompletedEventConsumer>(context));
        _configurator.ReceiveEndpoint(RabbitMQSettings.Order_OrderFailedEventQueue, e => e.ConfigureConsumer<OrderFailedEventConsumer>(context));
    });
});

builder.Services.AddDbContext<OrderAPIDbContext>(option => option.UseSqlServer(builder.Configuration.GetConnectionString("MSSQLServer")));

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapPost("/create-order", async (CreateOrderVM model, OrderAPIDbContext context, ISendEndpointProvider sendEndpointProvider) =>
{
    Order.API.Entities.Order order = new()
    {
        BuyerId = Guid.TryParse(model.BuyerId, out Guid result) ? result : Guid.NewGuid(),
        CreatedDate = DateTime.UtcNow,
        OrderStatus = OrderStatus.Suspend,
        TotalPrice = model.OrderItems.Sum(oi => oi.Price * oi.Count),
        OrderItems = model.OrderItems.Select(oi => new OrderItem()
        {
            Price = oi.Price,
            Count = oi.Count,
            ProductId = Guid.TryParse(oi.ProductId, out Guid result) ? result : Guid.NewGuid(),
        }).ToList()
    };

    await context.Orders.AddAsync(order);
    await context.SaveChangesAsync();


    OrderStartedEvent orderStartedEvent = new()
    {
        OrderId = order.Id,
        BuyerId= order.BuyerId,
        TotalPrice = order.TotalPrice,
        OrderItems = order.OrderItems.Select(oi => new OrderItemMessage()
        {
            Count = oi.Count,
            Price = oi.Price,
            ProductId = oi.ProductId,
        }).ToList()
    };
    ISendEndpoint sendEndpoint = await sendEndpointProvider.GetSendEndpoint(new Uri($"queue:{RabbitMQSettings.StateMachineQueue}"));
    await sendEndpoint.Send(orderStartedEvent);
});

app.Run();
