using System.Collections.Generic;

namespace QueenOfDreamer.API.Dtos.OrderDto
{
    public class GetDeliveryAddressRequest
    {
        public List<ProductCart> ProductCarts { get; set; }
        public int DeliveryAddressId{get;set;}
    }
}