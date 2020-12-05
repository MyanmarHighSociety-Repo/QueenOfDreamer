using System.Collections.Generic;
using System.Threading.Tasks;
using QueenOfDreamer.API.Dtos;
using QueenOfDreamer.API.Dtos.ProductDto;
using QueenOfDreamer.API.Dtos.ReportDto;
using QueenOfDreamer.API.Models;

namespace QueenOfDreamer.API.Interfaces.Repos
{
    public interface IReportRepository
    {
         Task<List<GetActivityLogResponse>> GetActivityLog(GetActivityLogRequest request,string token);
         Task<GetRevenueResponse> GetRevenue(GetRevenueRequest request);
         Task<GetSearchKeywordResponse> GetSearchKeyword(GetSearchKeywordRequest request);
         Task<ResponseStatus> NewRegisterCount(NewRegisterCountRequest request,int platform);
         Task<List<GetProductSearchResponse>> GetProductSearch(GetProductSearchRequest request);
         Task<GetSalesAndPerformanceResponse> GetSalesAndPerformance(GetSalesAndPerformanceRequest request);

         Task<GetVisitorsResponse> GetVisitors(GetSalesAndPerformanceRequest request);
    }
}