namespace QueenOfDreamer.API.Dtos.DeliveryDto
{
    public class GetDeliveryFeeResponse
    {
        public int DeliveryServiceId {get;set;}
        public string DeliveryServiceName {get;set;}
        public string DeliveryCompEmail {get;set;}
        public string ImgPath {get;set;}
        public double DeliveryFee {get;set;}
        public int FromEstDeliveryDay { get; set; }
        public int ToEstDeliveryDay { get; set; }
        public string ContactPh {get;set;}
        public bool Default {get;set;}
    }
}