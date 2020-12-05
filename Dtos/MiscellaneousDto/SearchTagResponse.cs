using QueenOfDreamer.API.Dtos;

namespace QueenOfDreamer.Dtos.MiscellaneousDto
{
    public class SearchTagResponse : ResponseStatus
    {
        public int Id {get;set;}
        public string Name {get;set;}
    }
}