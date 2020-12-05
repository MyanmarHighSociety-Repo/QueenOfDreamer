using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using QueenOfDreamer.API.Const;
using QueenOfDreamer.API.Dtos.MiscellaneousDto;
using QueenOfDreamer.API.Interfaces.Services;
using Newtonsoft.Json;
using QueenOfDreamer.API.Dtos.OrderDto;
using QueenOfDreamer.Dtos.MiscellaneousDto;
using QueenOfDreamer.API.Dtos.DeliveryDto;

namespace QueenOfDreamer.API.Services
{
    public class DeliveryService : IDeliveryService
    {
        static HttpClient client = new HttpClient();
        public async Task<GetCityResponse> GetCity(string token)
        {
            token = token.Remove(0,7);
            client.DefaultRequestHeaders.Authorization 
                         = new AuthenticationHeaderValue("Bearer", token);

            HttpResponseMessage response = await client
                .GetAsync(QueenOfDreamerConst.DELIVERY_SERVICE_PATH + "GetCity");
            
            if(response.IsSuccessStatusCode)
            {
                var data = JsonConvert.DeserializeObject<List<GetCityResponseArry>>(
                                await response.Content.ReadAsStringAsync());
                return new GetCityResponse() { CityList = data };
            }
            return null;
        }
        public async Task<GetTownResponse> GetTownship(int cityId,string token)
        {
            token = token.Remove(0,7);
            client.DefaultRequestHeaders.Authorization 
                         = new AuthenticationHeaderValue("Bearer", token);

            HttpResponseMessage response = await client
                                        .GetAsync(QueenOfDreamerConst.DELIVERY_SERVICE_PATH + "GetTownship?cityId="+cityId);
            
            if(response.IsSuccessStatusCode)
            {
                var data = JsonConvert.DeserializeObject<List<GetTownResponseArry>>(
                                await response.Content.ReadAsStringAsync());
                return new GetTownResponse() {TownList = data};
            }
            return null;
        }
         public async Task<string> GetCityName(string token,int? id=0)
        {
            token = token.Remove(0,7);
            client.DefaultRequestHeaders.Authorization 
                         = new AuthenticationHeaderValue("Bearer", token);

            HttpResponseMessage response = await client
                                        .GetAsync(QueenOfDreamerConst.DELIVERY_SERVICE_PATH + "GetCityName?id="+id);
            
            if(response.IsSuccessStatusCode)
            {
                // var data = JsonConvert.DeserializeObject<string>(
                //                 await response.Content.ReadAsStringAsync());
                var data = await response.Content.ReadAsStringAsync();
                return data;
            }
            return null;
        }
        public async Task<string> GetTownshipName(string token,int? id=0)
        {
            token = token.Remove(0,7);
            client.DefaultRequestHeaders.Authorization 
                         = new AuthenticationHeaderValue("Bearer", token);

            HttpResponseMessage response = await client
                                        .GetAsync(QueenOfDreamerConst.DELIVERY_SERVICE_PATH + "GetTownshipName?id="+id);
            
            if(response.IsSuccessStatusCode)
            {
                // var data = JsonConvert.DeserializeObject<string>(
                //                 await response.Content.ReadAsStringAsync());

                var data = await response.Content.ReadAsStringAsync();
                
                return data;
            }
            return null;
        }
        

        public async Task<GetDeliveryServiceRateResponse> GetDeliveryServiceRate(int deliveryServiceId,int cityId,int townshipId,string token)
        {
            token = token.Remove(0,7);
            client.DefaultRequestHeaders.Authorization 
                         = new AuthenticationHeaderValue("Bearer", token);

            HttpResponseMessage response = await client
                                        .GetAsync(QueenOfDreamerConst.DELIVERY_SERVICE_PATH + "GetDeliveryServiceRate?deliveryServiceId="+deliveryServiceId+"&cityId="+cityId+"&townshipId="+townshipId);
            
            if(response.IsSuccessStatusCode)
            {
                var data = JsonConvert.DeserializeObject<GetDeliveryServiceRateResponse>(
                                await response.Content.ReadAsStringAsync());
                return data;
            }
            return null;
        }

        public async Task<List<GetDeliveryServiceResponse>> GetDeliveryService(string token)
        {
            token = token.Remove(0,7);
            client.DefaultRequestHeaders.Authorization 
                         = new AuthenticationHeaderValue("Bearer", token);

            HttpResponseMessage response = await client
                                        .GetAsync(QueenOfDreamerConst.DELIVERY_SERVICE_PATH + "GetDeliveryService?AppConfigId="+QueenOfDreamerConst.APPLICATION_CONFIG_ID);
            
            if(response.IsSuccessStatusCode)
            {
                var data = JsonConvert.DeserializeObject<List<GetDeliveryServiceResponse>>(
                                await response.Content.ReadAsStringAsync());
                return data;
            }
            return null;
        }

        public async Task<GetDeliveryServiceDetailResponse> GetDeliveryServiceInfo(string token, int DeliveryServiceId)
        {
            token = token.Remove(0,7);
            client.DefaultRequestHeaders.Authorization 
                         = new AuthenticationHeaderValue("Bearer", token);

            HttpResponseMessage response = await client
                                        .GetAsync(QueenOfDreamerConst.DELIVERY_SERVICE_PATH + "GetDeliveryServiceDetail?DeliveryServiceId="+DeliveryServiceId+"&AppConfigId="+QueenOfDreamerConst.APPLICATION_CONFIG_ID);
            
            if(response.IsSuccessStatusCode)
            {
                var data = JsonConvert.DeserializeObject<GetDeliveryServiceDetailResponse>(
                                await response.Content.ReadAsStringAsync());
                return data;
            }
            return null;
        }

        public async Task<List<GetDeliveryServiceResponse>> GetDefaultDeliveryService(string token)
        {
            token = token.Remove(0,7);
            client.DefaultRequestHeaders.Authorization 
                         = new AuthenticationHeaderValue("Bearer", token);

            HttpResponseMessage response = await client
                                        .GetAsync(QueenOfDreamerConst.DELIVERY_SERVICE_PATH + "GetDefaultDeliveryService?AppConfigId="+QueenOfDreamerConst.APPLICATION_CONFIG_ID);
            
            if(response.IsSuccessStatusCode)
            {
                var data = JsonConvert.DeserializeObject<List<GetDeliveryServiceResponse>>(
                                await response.Content.ReadAsStringAsync());
                return data;
            }
            return null;
        }
        public async Task<List<GetDeliveryFeeResponse>> GetDeliveryFee(int ProductTypeId, int CityId, int TownshipId, string token)
        {
            token = token.Remove(0,7);
            client.DefaultRequestHeaders.Authorization 
                         = new AuthenticationHeaderValue("Bearer", token);

            HttpResponseMessage response = await client
                                        .GetAsync(QueenOfDreamerConst.DELIVERY_SERVICE_PATH + "GetDeliveryFee?AppConfigId="+QueenOfDreamerConst.APPLICATION_CONFIG_ID+"&ProductTypeId="+ProductTypeId+"&CityId="+CityId+"&TownshipId="+TownshipId);
            
            if(response.IsSuccessStatusCode)
            {
                var data = JsonConvert.DeserializeObject<List<GetDeliveryFeeResponse>>(
                                await response.Content.ReadAsStringAsync());
                return data;
            }
            return null;
        }

        public async Task<List<GetOtherCityDeliveryServiceRateResponse>> GetOtherOptionServiceRate(int ProductTypeId, int CityId, string token)
        {
            token = token.Remove(0,7);
            client.DefaultRequestHeaders.Authorization 
                         = new AuthenticationHeaderValue("Bearer", token);

            HttpResponseMessage response = await client
                                        .GetAsync(QueenOfDreamerConst.DELIVERY_SERVICE_PATH + "GetOtherOptionServiceRate?AppConfigId="+QueenOfDreamerConst.APPLICATION_CONFIG_ID+"&ProductTypeId="+ProductTypeId+"&CityId="+CityId);
            
            if(response.IsSuccessStatusCode)
            {
                var data = JsonConvert.DeserializeObject<List<GetOtherCityDeliveryServiceRateResponse>>(
                                await response.Content.ReadAsStringAsync());
                return data;
            }
            return null;
        }
    }
}