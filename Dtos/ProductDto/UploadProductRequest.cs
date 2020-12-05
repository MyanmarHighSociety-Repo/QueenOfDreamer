using Microsoft.AspNetCore.Http;
namespace QueenOfDreamer.API.Dtos.ProductDto
{
    public class UploadProductRequest
    {
        public IFormFile File { get; set; }
    }
}