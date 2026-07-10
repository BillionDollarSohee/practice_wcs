using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EisSocketService.Models
{
    // CART_MASTER 테이블 매핑
    public class CartMaster
    {
        public string CartId { get; set; }
        public string CartBarcode { get; set; }
        public string LineType { get; set; }
        public DateTime CreateDttm { get; set; }
    }
}