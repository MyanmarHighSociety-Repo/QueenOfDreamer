
using System.Collections.Generic;
using QueenOfDreamer.API.Dtos.GatewayDto;

namespace QueenOfDreamer.API.Dtos.OrderDto
{
    public class PostOrderByKBZPayResponse : ResponseStatus
    {
        public int OrderId { get; set; }

        public int? Timestamp { get; set; }

        public string NonceStr { get; set; }
        public string TransactionId {get;set;}

        public KBZPrecreateResponse Precreate { get; set; }
        public List<ProductIssues> ProductIssues{get;set;}
    }
}
