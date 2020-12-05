using System;

namespace QueenOfDreamer.API.Models
{
    public class TrnProductSku
    {        
        public Guid ProductId { get; set; }

        public int SkuId { get; set; }

        public int Qty { get; set; }
    }
}