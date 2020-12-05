namespace QueenOfDreamer.API.Dtos.DeliveryDto
{
    public class GetDeliveryServiceDetailResponse
    {
        public int Id {get;set;}
        public string Name {get;set;}
        public string ImgPath {get;set;}
        public int CityId {get;set;}
        public string CityName {get;set;}
        public int TownshipId {get;set;}
        public string TownshipName {get;set;}
        public string Address {get;set;}
        public string PhNo {get;set;}
        public string ContactPerson {get;set;}
        public int AppConfigId {get;set;}
        public string Email {get;set;}
    }
}