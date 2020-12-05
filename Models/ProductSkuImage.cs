namespace QueenOfDreamer.API.Models
{
    public class ProductSkuImage
    {
        public int Id {get;set;}
        public int SkuId {get;set;}
        public int ProductId {get;set;}
        public string Url {get;set;}
        public string Thumbnail {get;set;}
    }
}