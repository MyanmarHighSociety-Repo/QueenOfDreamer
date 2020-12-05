using System.Collections.Generic;

namespace QueenOfDreamer.API.Dtos.OrderDto
{
    public class UpdateProductCartRequest
    {
        public List<ProductCart> ProductCarts { get; set; }
    }
    public class ProductCart
    {
        public int ProductId { get; set; }
        public int SkuId { get; set; }
        public int Qty { get; set; }
    }
}