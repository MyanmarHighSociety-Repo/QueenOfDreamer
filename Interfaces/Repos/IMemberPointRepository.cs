using System.Collections.Generic;
using System.Threading.Tasks;
using QueenOfDreamer.API.Dtos;
using QueenOfDreamer.API.Dtos.MembershipDto;
using QueenOfDreamer.API.Dtos.OrderDto;

namespace QueenOfDreamer.API.Interfaces.Repos
{
    public interface IMemberPointRepository
    {
        Task<List<GetConfigMemberPointResponse>> GetConfigMemberPoint(string token);
        Task<GetConfigMemberPointResponse> GetConfigMemberPointById(int id, string token);        
        Task<ResponseStatus> ReceivedMemberPoint(ReceivedMemberPointRequest request,string token);
        Task<ResponseStatus> CreateProductReward(CreateProductRewardRequest request);
        Task<ResponseStatus> UpdateProductReward(UpdateProductRewardRequest request);
        Task<List<GetRewardProductResponse>> GetRewardProduct(GetRewardProductRequest request);
        Task<GetRewardProductByIdResponse> GetRewardProductById(GetRewardProductByIdRequest request);        
        Task<GetRewardProductDetailResponse> GetRewardProductDetail(GetRewardProductDetailRequest request,int currentUserLogin,string token);
        Task<PostOrderResponse> RedeemOrder(RedeemOrderRequest request,int currentUserLogin,string token);
        Task<GetCartDetailForRewardResponse> GetCartDetailForReward(int productId,int skuId,int currentUserLogin,string token);
        Task<ResponseStatus> DeleteProductReward(int id);
        Task<PostOrderByKBZPayResponse> RedeemOrderByKBZPay(RedeemOrderRequest request,int currentUserLogin,string token);
        Task<List<GetConfigMemberPointProductCategory>> GetProductCategoryForCreateConfigMemberPoint(string token);
        Task<GetOrderDetailForMemberPoint_MS_Response> GetOrderDetailForMemberPoint_MS(string voucherNo);
        Task<List<GetProductListForAddProductRewardResponse>> GetProductListForAddProductReward(GetProductListForAddProductRewardRequest request);
    }
}
