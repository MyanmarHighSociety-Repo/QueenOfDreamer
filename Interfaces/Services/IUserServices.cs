using System.Collections.Generic;
using System.Threading.Tasks;
using QueenOfDreamer.API.Dtos.UserDto;

namespace QueenOfDreamer.API.Interfaces.Services
{
    public interface IUserServices
    {
        Task<GetUserInfoResponse> GetUserInfo(int userId, string token);        
        Task<List<GetAllSellerUserIdResponse>> GetAllSellerUserId(string token);       
    }
}