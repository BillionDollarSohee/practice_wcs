using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskPractice.Model
{
    public class OrderMaster
    {
        public string OrderId { get; set; }
        public string CartId { get; set; }
        public string FromId { get; set; }
        public string ToEqpId { get; set; }
        public OrderStatus Status { get; set; } = OrderStatus.WORKING;
        public List<OrderDetail> Details { get; set; } = new List<OrderDetail>();
    }

    public enum OrderStatus
    {
        INIT,
        WORKING,
        COMPLETE
    }
}
