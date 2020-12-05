using System.ComponentModel.DataAnnotations;
namespace QueenOfDreamer.API.Dtos.ProductDto
{
    public class GetProductDetailRequest
    {
        [Required]
        public int ProductId { get; set; }
    }
}