namespace QueenOfDreamer.API.Dtos.ProductDto
{
    public class GetProductByRelatedCategryResponse
    {
        public int ProductId {get;set;}
        public string Url {get;set;}
        public string Name {get;set;}        
        public double OriginalPrice {get;set;}
        public double PromotePrice {get;set;}
        public int PromotePercent { get; set; }
        public int ProductCategoryId {get;set;}
    }
}