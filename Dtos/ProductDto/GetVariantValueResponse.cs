namespace QueenOfDreamer.API.Dtos.ProductDto
{
    public class GetVariantValueResponse : ResponseStatus
    {
        public int ValueId  { get; set; }
        public string ValueName { get; set; }
    }
}