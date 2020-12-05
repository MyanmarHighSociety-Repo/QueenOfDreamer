using System.Collections.Generic;

namespace QueenOfDreamer.API.Dtos.DeliveryDto
{
    public class GetOtherCityDeliveryServiceRateResponse : ResponseStatus
    {
        public int Id {get;set;}
        public int DeliveryServiceId {get;set;}
        public string DeliveryServiceName {get;set;}
        public string DeliveryCompEmail {get;set;}
        public string ImgPath {get;set;}
        public double DeliveryFee {get;set;}
        public int FromEstDeliveryDay { get; set; }
        public int ToEstDeliveryDay { get; set; }
        public string ContactPh {get;set;}
        public string GateName {get;set;}
        public List<SelectedCity> AvailableCity {get;set;}
    }

    public class SelectedCity{
        public int Id {get;set;}
        public int CityId {get;set;}
        public string CityName {get;set;}
        public string Action {get;set;}// Off New
    }

}