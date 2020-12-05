using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using QueenOfDreamer.API.Helpers;
using QueenOfDreamer.API.Interfaces.Repos;
using log4net;
using System.Threading.Tasks;
using QueenOfDreamer.API.Dtos.ProductDto;
using System;
using Microsoft.AspNetCore.Http;
using System.Linq;
using QueenOfDreamer.API.Const;
using DeviceDetectorNET.Parser;
using QueenOfDreamer.API.Dtos.ReportDto;

namespace Ecommerce.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [ServiceFilter(typeof(ActionActivity))]
    [ServiceFilter(typeof(ActionActivityLog))]
    public class ReportController : ControllerBase
    {
        private readonly IReportRepository _repo;
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public ReportController(IReportRepository repo)
        {
            _repo = repo;
        }
        [HttpGet("GetActivityLog")]
        public async Task<IActionResult> GetActivityLog([FromQuery]GetActivityLogRequest request)
        {            
            try
            {
                string token = Request.Headers["Authorization"];
                var response = await _repo.GetActivityLog(request,token);
                return Ok(response);
            }
            catch (Exception e)
            {
                string actionName = this.ControllerContext.RouteData.Values["action"].ToString();
                string controllerName = this.ControllerContext.RouteData.Values["controller"].ToString();
                log.Error(controllerName + " > " + actionName + " : " + DateTime.Now.ToString() + " => " +  e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
        [HttpGet("GetRevenue")]
        public async Task<IActionResult> GetRevenue([FromQuery]GetRevenueRequest request)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                var response = await _repo.GetRevenue(request);
                return Ok(response);
            }
            catch (Exception e)
            {
                string actionName = this.ControllerContext.RouteData.Values["action"].ToString();
                string controllerName = this.ControllerContext.RouteData.Values["controller"].ToString();
                log.Error(controllerName + " > " + actionName + " : " + DateTime.Now.ToString() + " => " +  e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
        [HttpGet("GetSearchKeyword")]
        public async Task<IActionResult> GetSearchKeyword([FromQuery]GetSearchKeywordRequest request)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                var response = await _repo.GetSearchKeyword(request);
                return Ok(response);
            }
            catch (Exception e)
            {
                string actionName = this.ControllerContext.RouteData.Values["action"].ToString();
                string controllerName = this.ControllerContext.RouteData.Values["controller"].ToString();
                log.Error(controllerName + " > " + actionName + " : " + DateTime.Now.ToString() + " => " +  e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
        [HttpPost("NewRegisterCount")]
        public async Task<IActionResult> NewRegisterCount(NewRegisterCountRequest request)
        {
            try
            {
                var platform=3;
                try{
                    #region Platform 
                    DeviceDetectorNET.DeviceDetector.SetVersionTruncation(VersionTruncation.VERSION_TRUNCATION_NONE);
                    var userAgent = Request.Headers["User-Agent"];
                    var result = DeviceDetectorNET.DeviceDetector.GetInfoFromUserAgent(userAgent);
                    var agent = result.Success ? result.ToString().Replace(Environment.NewLine, "<br/>") : "Unknown";
                    var agentArray=agent.Split("<br/>");                    
                    if(QueenOfDreamerConst.AndroidDevice.Contains(agentArray[7].Replace("Name: ","").Replace(";","").Trim()))
                    {
                        platform=1; //Android
                    }
                    else if(QueenOfDreamerConst.IosDevice.Contains(agentArray[7].Replace("Name: ","").Replace(";","").Trim()))
                    {
                        platform=2; //IOS
                    }
                    else{
                        platform=3; //Web                
                    } 
                    #endregion
                }
                catch{
                }

                var response = await _repo.NewRegisterCount(request,platform);
                return Ok(response);
            }
            catch (Exception e)
            {
                string actionName = this.ControllerContext.RouteData.Values["action"].ToString();
                string controllerName = this.ControllerContext.RouteData.Values["controller"].ToString();
                log.Error(controllerName + " > " + actionName + " : " + DateTime.Now.ToString() + " => " +  e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    
        [HttpGet("GetProductSearch")]
        public async Task<IActionResult> GetProductSearch([FromQuery]GetProductSearchRequest request)
        {
            try
            {
                var response = await _repo.GetProductSearch(request);
                return Ok(response);
            }
            catch (Exception e)
            {
                string actionName = this.ControllerContext.RouteData.Values["action"].ToString();
                string controllerName = this.ControllerContext.RouteData.Values["controller"].ToString();
                log.Error(controllerName + " > " + actionName + " : " + DateTime.Now.ToString() + " => " +  e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("GetSalesAndPerformance")]
        public async Task<IActionResult> GetSalesAndPerformance([FromQuery]GetSalesAndPerformanceRequest request)
        {
            try
            {
                if (request.FromDate.ToString("dd-MMM-yy") == "01-Jan-01")
                {
                    DateTime now = DateTime.Now;
                    request.FromDate = new DateTime(now.Year, now.Month, 1);
                    request.ToDate = request.FromDate.AddMonths(1).AddDays(-1);
                }
                var response = await _repo.GetSalesAndPerformance(request);
                return Ok(response);
            }
            catch (Exception e)
            {
                string actionName = this.ControllerContext.RouteData.Values["action"].ToString();
                string controllerName = this.ControllerContext.RouteData.Values["controller"].ToString();
                log.Error(controllerName + " > " + actionName + " : " + DateTime.Now.ToString() + " => " +  e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("GetVisitors")]
        public async Task<IActionResult> GetVisitors([FromQuery]GetSalesAndPerformanceRequest request)
        {
            try
            {
                if (request.FromDate.ToString("dd-MMM-yy") == "01-Jan-01")
                {
                    DateTime now = DateTime.Now;
                    request.FromDate = new DateTime(now.Year, now.Month, 1);
                    request.ToDate = request.FromDate.AddMonths(1).AddDays(-1);
                }
                var response = await _repo.GetVisitors(request);
                return Ok(response);
            }
            catch (Exception e)
            {
                string actionName = this.ControllerContext.RouteData.Values["action"].ToString();
                string controllerName = this.ControllerContext.RouteData.Values["controller"].ToString();
                log.Error(controllerName + " > " + actionName + " : " + DateTime.Now.ToString() + " => " +  e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}