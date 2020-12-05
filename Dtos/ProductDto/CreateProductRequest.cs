using System;
using System.Collections.Generic;
using QueenOfDreamer.API.Models;
using Microsoft.AspNetCore.Http;

namespace QueenOfDreamer.API.Dtos.ProductDto
{
    public class CreateProductRequest
    {
        public Guid ProductId { get; set; }
        public string Name { get; set; }        
        public string Description { get; set; }
        public int ProductTypeId {get;set;}
        public int? BrandId {get;set;}
        public double Price { get; set; }
        // public ProductPriceEntry ProductPrice { get; set; }
        public string ProductStatus {get;set;}
        public List<Tag> TagsList {get;set;}
        public int Promotion {get;set;}
        public ProductClipRequest ProductClip {get;set;}
        public List<ImageRequest> ImageList { get; set; }        
        public List<Sku> Sku { get; set; }
    }
    public class ProductPriceEntry {
        public double Price{get;set;}
        public DateTime? FromDate{get;set;}
        public DateTime? ToDate{get;set;}
    }
    public class Sku
    {
        public int SkuId { get; set; }
        public int Qty { get; set; }
        public double Price {get;set;}
    }    
    public class ImageRequest{
        public int ImageId {get;set;}
        public string ImageContent { get; set; }
        public string Extension { get; set; }
        public string Action {get;set;}
        public int SeqNo {get;set;}
    }
    public class ProductClipRequest{
        public int ProductId { get; set; }

        public string Name { get; set; }

        public string ClipPath { get; set; }

        public int SeqNo { get; set; }
    }
}