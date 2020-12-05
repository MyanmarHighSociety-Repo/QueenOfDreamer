namespace QueenOfDreamer.API.Dtos.OrderDto
{
    public class UpdateUserDeliveryAddressRequest
    {
        public int Id {get;set;}
        public int CityId {get;set;}

        public int TownshipId {get;set;}

        public string LabelName {get;set;}

        public string Address {get;set;}

        public string Landmark {get;set;}

        public bool isSelected {get;set;}

    }
}