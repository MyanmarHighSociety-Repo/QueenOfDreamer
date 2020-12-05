using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using QueenOfDreamer.API.Const;
using QueenOfDreamer.API.Dtos;
using QueenOfDreamer.API.Dtos.MembershipDto;
using QueenOfDreamer.API.Dtos.UserDto;
using QueenOfDreamer.API.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace QueenOfDreamer.API.Services
{
    public class MemberPointServices : IMemberPointServices
    {
        static HttpClient client = new HttpClient();
        public async Task<List<GetConfigMemberPointResponse>> GetConfigMemberPoint(string token)
        {
            token = token.Remove(0,7);
            client.DefaultRequestHeaders.Authorization 
                         = new AuthenticationHeaderValue("Bearer", token);

            HttpResponseMessage response = await client
                .GetAsync(QueenOfDreamerConst.MEMBERPOINT_SERVICE_PATH + "GetConfigMemberPoint/?applicationConfigId="+QueenOfDreamerConst.APPLICATION_CONFIG_ID);
            
            if(response.IsSuccessStatusCode)
            {
                var data = JsonConvert.DeserializeObject<List<GetConfigMemberPointResponse>>(
                    await response.Content.ReadAsStringAsync());
                return data;
            }
            return new List<GetConfigMemberPointResponse>();
        }
        public async Task<GetConfigMemberPointResponse> GetConfigMemberPointById(int id, string token)
        {
            token = token.Remove(0,7);
            client.DefaultRequestHeaders.Authorization 
                         = new AuthenticationHeaderValue("Bearer", token);

            HttpResponseMessage response = await client
                .GetAsync(QueenOfDreamerConst.MEMBERPOINT_SERVICE_PATH + "GetConfigMemberPointById/?id="+id+"&applicationConfigId="+QueenOfDreamerConst.APPLICATION_CONFIG_ID);
            
            if(response.IsSuccessStatusCode)
            {
                var data = JsonConvert.DeserializeObject<GetConfigMemberPointResponse>(
                    await response.Content.ReadAsStringAsync());
                return data;
            }
            return new GetConfigMemberPointResponse();
        }
        public async Task<ResponseStatus> ReceivedMemberPoint(ReceivedMemberPointRequest request, string token)
        {
            token = token.Remove(0,7);
            client.DefaultRequestHeaders.Authorization 
                         = new AuthenticationHeaderValue("Bearer", token);  

            var json = JsonConvert.SerializeObject(request);
            var dataToSend = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await client.PostAsync(QueenOfDreamerConst.MEMBERPOINT_SERVICE_PATH + "ReceivedMemberPoint/", dataToSend);

            if(response.IsSuccessStatusCode)
            {
                var data = JsonConvert.DeserializeObject<ResponseStatus>(
                    await response.Content.ReadAsStringAsync());
                return data;
            }
            return new ResponseStatus(){StatusCode=StatusCodes.Status404NotFound};
        }
        public async Task<GetMyOwnPointResponse> GetMyOwnPoint(GetMyOwnPointRequest request,string token)
        {
            var data=new GetMyOwnPointResponse(){
                UserId=request.UserId,
                TotalPoint=0
            };
             token = token.Remove(0,7);
            client.DefaultRequestHeaders.Authorization 
                         = new AuthenticationHeaderValue("Bearer", token);

            HttpResponseMessage response = await client
                .GetAsync(QueenOfDreamerConst.MEMBERPOINT_SERVICE_PATH + "GetMyOwnPoint/?userId="+request.UserId+"&applicationConfigId="+QueenOfDreamerConst.APPLICATION_CONFIG_ID);
            
            if(response.IsSuccessStatusCode)
            {
                var res = JsonConvert.DeserializeObject<GetMyOwnPointResponse>(
                    await response.Content.ReadAsStringAsync());
                    if(res!=null)
                    {
                        data=res;
                    }
                return data;
            }
            return data;
        }
        public async Task<ResponseStatus> RedemptionMemberPoint(RedemptionMemberPointRequest request, string token)
        {
            token = token.Remove(0,7);
            client.DefaultRequestHeaders.Authorization 
                         = new AuthenticationHeaderValue("Bearer", token);  

            var json = JsonConvert.SerializeObject(request);
            var dataToSend = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await client.PostAsync(QueenOfDreamerConst.MEMBERPOINT_SERVICE_PATH + "RedemptionMemberPoint/", dataToSend);

            if(response.IsSuccessStatusCode)
            {
                var data = JsonConvert.DeserializeObject<ResponseStatus>(
                    await response.Content.ReadAsStringAsync());
                return data;
            }
            return new ResponseStatus(){StatusCode=StatusCodes.Status404NotFound};
        }

        public async Task<List<GetConfigMemberPointProductCategory>> GetProductCategoryForCreateConfigMemberPoint(string token)
        {
            token = token.Remove(0,7);
            client.DefaultRequestHeaders.Authorization 
                         = new AuthenticationHeaderValue("Bearer", token);

            HttpResponseMessage response = await client
                .GetAsync(QueenOfDreamerConst.MEMBERPOINT_SERVICE_PATH + "GetProductCategory/?applicationConfigId="+QueenOfDreamerConst.APPLICATION_CONFIG_ID);
            
            if(response.IsSuccessStatusCode)
            {
                var data = JsonConvert.DeserializeObject<List<GetConfigMemberPointProductCategory>>(
                    await response.Content.ReadAsStringAsync());
                return data;
            }
            return new List<GetConfigMemberPointProductCategory>();
        }
    }
}
