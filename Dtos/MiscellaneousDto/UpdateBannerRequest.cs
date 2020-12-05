using QueenOfDreamer.API.Dtos.ProductDto;

namespace QueenOfDreamer.API.Dtos.MiscellaneousDto
{
    public class UpdateBannerRequest
    {
        public int Id {get;set;}
        public string Name {get;set;}
        public ImageRequest ImageRequest{get;set;}
        public int BannerLinkId {get;set;}
    }
}