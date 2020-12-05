using System.ComponentModel.DataAnnotations;

namespace QueenOfDreamer.Dtos.MiscellaneousDto
{
    public class GetTownshipRequest
    {
        [Required]
        public int CityId { get; set; }
    }
}