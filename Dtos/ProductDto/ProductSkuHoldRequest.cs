using System;
using System.Collections.Generic;

namespace QueenOfDreamer.API.Dtos.ProductDto
{
    public class ProductSkuHoldRequest
    {
        public Guid ProductId { get; set; }
        public List<Sku> Sku { get; set; }
    }
}