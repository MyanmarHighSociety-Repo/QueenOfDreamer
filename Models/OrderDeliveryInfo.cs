using System;
using System.ComponentModel.DataAnnotations;

namespace QueenOfDreamer.API.Models
{
    public class OrderDeliveryInfo
    {
        public int Id { get; set; }

        public int OrderId { get; set; }

        public virtual Order Order { get; set; }

        [StringLength(255)]
        public string Name { get; set; }
        public int? CityId { get; set; }
        public int? TownshipId { get; set; }
        public string Address { get; set; }
        public string PhNo { get; set; }
        public string Remark { get; set; }     
        public DateTime DeliveryDate{get;set;}  

        [StringLength(10)] 
        public string FromTime { get; set; }

        [StringLength(10)] 
        public string ToTime { get; set; }
        public int DeliveryServiceId { get; set; }   
        public string DeliveryServiceEmail {get;set;}
        public string DeliveryServicePhno {get;set;}
        public string DeliveryServiceName {get;set;}
        public string LogoPath {get;set;}
        public double DeliveryFee {get;set;}
        public int FromEstDeliveryDay { get; set; }
        public int ToEstDeliveryDay { get; set; }
    }
}