namespace QueenOfDreamer.API.Dtos.OrderDto
{
    public class GetDeliveryAddressResponse
    {
        public int Id {get;set;}
        public string LabelName {get;set;}
        public int CityId {get;set;}
        public string CityName {get;set;}
        public int TownshipId {get;set;}
        public string TownshipName {get;set;}
        public string Address {get;set;}
        public string Landmark {get;set;}
    }
}