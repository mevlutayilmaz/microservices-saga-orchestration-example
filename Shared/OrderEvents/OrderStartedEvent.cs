using Shared.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.OrderEvents
{
    public class OrderStartedEvent
    {
        public Guid OrderId { get; set; }
        public Guid BuyerId { get; set; }
        public decimal TotalPrice { get; set; }
        public List<OrderItemMessage> OrderItems { get; set; }
    }
}
