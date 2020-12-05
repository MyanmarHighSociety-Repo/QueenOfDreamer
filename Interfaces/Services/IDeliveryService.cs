using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using QueenOfDreamer.API.Dtos.DeliveryDto;
using QueenOfDreamer.API.Dtos.MiscellaneousDto;
using QueenOfDreamer.API.Dtos.OrderDto;
using QueenOfDreamer.Dtos.MiscellaneousDto;

namespace  QueenOfDreamer.API.Interfaces.Services
{
    public interface IDeliveryService
    {
         Task<GetCityResponse> GetCity(string token);
         Task<GetTownResponse> GetTownship(int cityId,string token);
         Task<string> GetCityName(string token,int? id=0);
         Task<string> GetTownshipName(string token,int? id=0);
         Task<List<GetDeliveryServiceResponse>> GetDeliveryService(string token);
         Task<GetDeliveryServiceRateResponse> GetDeliveryServiceRate(int deliveryServiceId,int cityId,int townshipId,string token);
         Task<List<GetDeliveryServiceResponse>> GetDefaultDeliveryService(string token);
         Task<List<GetDeliveryFeeResponse>> GetDeliveryFee(int ProductTypeId, int CityId, int TownshipId, string token);
         Task<List<GetOtherCityDeliveryServiceRateResponse>> GetOtherOptionServiceRate(int ProductTypeId, int CityId, string token);
         Task<GetDeliveryServiceDetailResponse> GetDeliveryServiceInfo(string token, int DeliveryServiceId);
    }
}