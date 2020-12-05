using System.Collections.Generic;
using System.Threading.Tasks;
using QueenOfDreamer.API.Dtos;
using QueenOfDreamer.API.Dtos.MembershipDto;

namespace QueenOfDreamer.API.Interfaces.Services
{
    public interface IMemberPointServices
    {        
        Task<List<GetConfigMemberPointResponse>> GetConfigMemberPoint(string token);
        Task<GetConfigMemberPointResponse> GetConfigMemberPointById(int id, string token);
        Task<ResponseStatus> ReceivedMemberPoint(ReceivedMemberPointRequest request,string token);    
        Task<GetMyOwnPointResponse> GetMyOwnPoint(GetMyOwnPointRequest request,string token);
        Task<ResponseStatus> RedemptionMemberPoint(RedemptionMemberPointRequest request,string token); 
        Task<List<GetConfigMemberPointProductCategory>> GetProductCategoryForCreateConfigMemberPoint(string token);  
    }
}