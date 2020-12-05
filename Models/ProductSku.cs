using System;

namespace QueenOfDreamer.API.Models
{
    public class ProductSku
    {        
        public int ProductId { get; set; }

        public int SkuId { get; set; }

        public string Sku { get; set; }

        public int Qty { get; set; }

        public double Price { get; set; }

        public DateTime? CreatedDate { get; set; }

        public int? CreatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }

        public int? UpdatedBy { get; set; }

        public double? OriginalPrice {get;set;}
        public double? AdditionalPrice {get;set;}
        public DateTime? FromDate {get;set;}
        public DateTime? ToDate{get;set;}
        public bool? IsAvailable {get;set;}
        public bool? AllowPreOrder{get;set;}
        public double? PreOrderPrice {get;set;}
        public DateTime? PreOrderDate {get;set;}
         
    }
}