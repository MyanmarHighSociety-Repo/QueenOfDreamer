using System;

namespace QueenOfDreamer.API.Models
{
    public class UserDeliveryAddress
    {
        public int Id {get;set;}
        public string LabelName {get;set;}
        public int CityId {get;set;}
        public int TownshipId {get;set;}
        public string Address {get;set;}
        public string Landmark {get;set;}
        public int UserId {get;set;}
        public bool IsSelected {get;set;}
        public DateTime CreatedDate {get;set;}
        public int CreatedBy {get;set;}
        public DateTime UpdatedDate{get;set;} 
        public int UpdatedBy {get;set;}
    }
}