using Microsoft.AspNetCore.Mvc;
using QueenOfDreamer.API.Interfaces.Repos;
using AutoMapper;
using QueenOfDreamer.API.Interfaces.Services;
using log4net;
using Microsoft.AspNetCore.Authorization;
using QueenOfDreamer.API.Helpers;
using QueenOfDreamer.API.Dtos.OrderDto;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Net.Http;
using System.Net;
using QueenOfDreamer.API.Const;
using System.Linq;
using DeviceDetectorNET.Parser;
using QueenOfDreamer.Dtos.GatewayDto;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Hosting;

namespace QueenOfDreamer.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]    
    public class OrderController : ControllerBase
    {
        private readonly IOrderRepository _repo;

        private readonly IMapper _mapper;

        private readonly IQueenOfDreamerServices _services;
        private readonly IWebHostEnvironment _env;

        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public OrderController(IOrderRepository repo, IMapper mapper, IQueenOfDreamerServices services,IWebHostEnvironment env)
        {
            _repo = repo;
            _mapper = mapper;
            _services = services;
            _env = env;
        }
   
        [HttpPost("AddToCart")]
        [Authorize]
        [ServiceFilter(typeof(ActionActivity))]
        [ServiceFilter(typeof(ActionActivityLog))]
        public async Task<IActionResult> AddToCart(AddToCartRequest request)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
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
                    userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                }
                catch{
                }
                var response = await _repo.AddToCart(request,userId,platform);
                if (response.StatusCode == StatusCodes.Status200OK)
                {
                    return Ok(response);
                }
                else{
                     return StatusCode(response.StatusCode,response);
                }               

            }
            catch(Exception e)
            {
                log.Error(e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError,e.Message);
            }
        }
        [HttpPost("RemoveFromCart")]
        [Authorize]
        [ServiceFilter(typeof(ActionActivity))]
        [ServiceFilter(typeof(ActionActivityLog))]
        public async Task<IActionResult> RemoveFromCart(RemoveFromCartRequest request)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
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
                var response = await _repo.RemoveFromCart(request,userId,platform);
                if (response.StatusCode == StatusCodes.Status200OK)
                {
                    return Ok(response);
                }
                else{
                     return StatusCode(response.StatusCode,response);
                }               

            }
            catch(Exception e)
            {
                log.Error(e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError,e.Message);
            }
        }

        [HttpGet("GetCartDetail")]
        [Authorize]
        [ServiceFilter(typeof(ActionActivity))]
        [ServiceFilter(typeof(ActionActivityLog))]
        public async Task<IActionResult> GetCartDetail()
        {
            try
            {
                string token = Request.Headers["Authorization"];
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

                var response = await _repo.GetCartDetail(userId,token);
                if (response.StatusCode == StatusCodes.Status200OK)
                {
                    return Ok(response);
                }
                else{
                     return StatusCode(response.StatusCode,response);
                }               

            }
            catch(Exception e)
            {
                log.Error(e.Message+", inner => "+e.InnerException.Message);
                return StatusCode(StatusCodes.Status500InternalServerError,e.Message);
            }
        }

        [HttpPost("UpdateDeliveryinfo")]
        [Authorize]
        [ServiceFilter(typeof(ActionActivity))]
        [ServiceFilter(typeof(ActionActivityLog))]
        public async Task<IActionResult> UpdateDeliveryinfo(UpdateDeliveryInfoRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                string token = Request.Headers["Authorization"];
                var response =await _repo.UpdateDeliveryInfo(request, userId,token);
                 
                if (response.StatusCode != StatusCodes.Status200OK)
                {
                    return StatusCode(response.StatusCode);
                }
                return Ok(response);
            }
            catch (Exception e)
            {
                log.Error(e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError,e.Message);
            }
        }

        [HttpPost("UpdateDeliveryDateAndTime")]
        [Authorize]
        [ServiceFilter(typeof(ActionActivity))]
        [ServiceFilter(typeof(ActionActivityLog))]
        public async Task<IActionResult> UpdateDeliveryDateAndTime(UpdateDeliveryDateAndTimeRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                string token = Request.Headers["Authorization"];
                var response=await _repo.UpdateDeliveryDateAndTime(request, userId,token);
                
                if (response.StatusCode != StatusCodes.Status200OK)
                {
                    return StatusCode(response.StatusCode);
                }
                return Ok(response);
                
            }
            catch (Exception e)
            {
                log.Error(e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError,e.Message);
            }
        }
        
        [HttpGet("GetDeliverySlot")]
        [Authorize]
        [ServiceFilter(typeof(ActionActivity))]
        [ServiceFilter(typeof(ActionActivityLog))]
        public async Task<IActionResult> GetDeliverySlot([FromQuery]GetDeliverySlotRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                var response = await _repo.GetDeliverySlot(request,userId);
                return Ok(response);
            }
            catch (Exception e)
            {
                log.Error(e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError,e.Message);
            }
        }
       
        [HttpPost("PostOrder")]
        [Authorize]
        [ServiceFilter(typeof(ActionActivity))]
        [ServiceFilter(typeof(ActionActivityLog))]
        public async Task<IActionResult> PostOrder(PostOrderRequest request)
        {
            try
            {
                // var startTime=DateTime.Now;
                // log.Info(string.Format("PostOrder start time => {0}",startTime));
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                string token = Request.Headers["Authorization"];

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

                var response= await _repo.PostOrder(request,userId,token,platform);
                if (response.StatusCode != StatusCodes.Status200OK)
                {
                    return StatusCode(response.StatusCode,response);
                }
                // log.Info(string.Format("PostOrder end time => {0}",DateTime.Now));
                // log.Info(string.Format("PostOrder diff time => {0}",DateTime.Now.Subtract(startTime).TotalSeconds));
                return Ok(response);
            }
            catch (Exception e)
            {
                log.Error(e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError,e.Message);
            }
        }
        
        [HttpPost("PostOrderActivity")]
        [Authorize]
        [ServiceFilter(typeof(ActionActivity))]
        [ServiceFilter(typeof(ActionActivityLog))]
        public async Task<IActionResult> PostOrderActivity(int orderId)
        {
            try
            {                
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                string token = Request.Headers["Authorization"];

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

                var response= await _repo.PostOrderActivity(orderId,userId,token,platform);
                if (response.StatusCode != StatusCodes.Status200OK)
                {
                    return StatusCode(response.StatusCode,response);
                }

                return Ok(response);
            }
            catch (Exception e)
            {
                log.Error(e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError,e.Message);
            }
        }
       

        [HttpPost("PostOrderByKBZPay")]
        [Authorize]
        [ServiceFilter(typeof(ActionActivity))]
        [ServiceFilter(typeof(ActionActivityLog))]
        public async Task<IActionResult> PostOrderByKBZPay(PostOrderRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                string token = Request.Headers["Authorization"];
                var response= await _repo.PostOrderByKBZPay(request,userId,token);
                return Ok(response);
            }
            catch (Exception e)
            {
                log.Error(e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError,e.Message);
            }
        }

        [HttpPost("CheckKPayStatus")]
        [Authorize]
        [ServiceFilter(typeof(ActionActivity))]
        [ServiceFilter(typeof(ActionActivityLog))]
        public async Task<IActionResult> CheckKPayStatus(string transactionId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                string token = Request.Headers["Authorization"];

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
                    userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                }
                catch{
                }
                var response = await _repo.CheckKPayStatus(transactionId, userId,token,platform);
                if (response.StatusCode != StatusCodes.Status200OK)
                {
                    return StatusCode(response.StatusCode,response);
                }
                return Ok(response);
            }
            catch (Exception e)
            {
                log.Error(e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError,e.Message);
            }
        }

        [HttpPost("PostOrderByWavePay")]
        [Authorize]
        [ServiceFilter(typeof(ActionActivity))]
        [ServiceFilter(typeof(ActionActivityLog))]
        public async Task<IActionResult> PostOrderByWavePay(PostOrderRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                string token = Request.Headers["Authorization"];

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

                var response= await _repo.PostOrderByWavePay(request,userId,token,platform);
                return Ok(response);
            }
            catch (Exception e)
            {
                log.Error(e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError,e.Message);
            }
        }

        [HttpPost("CheckWaveTransactionStatus")]
        // [Authorize]
        // [ServiceFilter(typeof(ActionActivity))]
        // [ServiceFilter(typeof(ActionActivityLog))]
        public async Task<IActionResult> CheckWaveTransactionStatus(CheckWaveTransactionStatusRequest request)
        {
            try
            {
                // var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                // string token = Request.Headers["Authorization"];

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
                var response = await _repo.CheckWaveTransactionStatus(request,platform);
                if (response.StatusCode != StatusCodes.Status200OK)
                {
                    return StatusCode(response.StatusCode,response);
                }
                return Ok(response);
            }
            catch (Exception e)
            {
                log.Error(e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError,e.Message);
            }
        }

        [HttpGet("GetOrderIdByTransactionId")]
        [Authorize]
        [ServiceFilter(typeof(ActionActivity))]
        [ServiceFilter(typeof(ActionActivityLog))]
        public async Task<IActionResult> GetOrderIdByTransactionId(string transactionId)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                var response = await _repo.GetOrderIdByTransactionId(transactionId,token);
                return Ok(response);
            }
            catch (Exception e)
            {
                log.Error(e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError,e.Message);
            }
        }


        [HttpPost("UpdateProductCart")]
        [Authorize]
        [ServiceFilter(typeof(ActionActivity))]
        [ServiceFilter(typeof(ActionActivityLog))]
        public async Task<IActionResult> UpdateProductCart(UpdateProductCartRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                string token = Request.Headers["Authorization"];
                var response= await _repo.UpdateProductCart(request,userId);
                 if (response.StatusCode != StatusCodes.Status200OK)
                {
                    return StatusCode(response.StatusCode);
                }
                return Ok(response);
            }
            catch (Exception e)
            {
                log.Error(e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError,e.Message);
            }
        }

        [HttpGet("GetOrderHistory")]
        [Authorize]
        [ServiceFilter(typeof(ActionActivity))]
        [ServiceFilter(typeof(ActionActivityLog))]
        public async Task<IActionResult> GetOrderHistory([FromQuery] GetOrderHistoryRequest request)
        {
            try
            {
                var response = await _repo.GetOrderHistory(request);
                if (response == null || response.Count == 0)
                {
                    return BadRequest(new {status = StatusCodes.Status400BadRequest,message = "No Result Found!"});
                }               
                return Ok(response);
            }
            catch (Exception e)
            {
                log.Error(e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError,e.Message);
            }
        }

        [HttpGet("GetOrderHistorySeller")]
        [Authorize]
        [ServiceFilter(typeof(ActionActivity))]
        [ServiceFilter(typeof(ActionActivityLog))]
        public async Task<IActionResult> GetOrderHistorySeller([FromQuery] GetOrderHistorySellerRequest request)
        {
            try
            {              
                var response = await _repo.GetOrderHistorySeller(request);
                if (response == null || response.Count == 0)
                {
                    return BadRequest(new {status = StatusCodes.Status400BadRequest,message = "No Result Found!"});
                }
                return Ok(response);
            }
            catch (Exception e)
            {
                log.Error(e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError,e.Message);
            }
        }
        [HttpGet("GetNotificationBuyer")]
        [Authorize]
        [ServiceFilter(typeof(ActionActivity))]
        [ServiceFilter(typeof(ActionActivityLog))]
        public async Task<IActionResult> GetNotificationBuyer([FromQuery] GetNotificationRequest request)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

                var response = await _repo.GetNotificationBuyer(request,userId, token);
                if (response == null || response.Count == 0)
                {
                    return BadRequest(new {status = StatusCodes.Status400BadRequest,message = "No Result Found!"});
                }
                return Ok(response);
            }
            catch (Exception e)
            {
                log.Error(e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError,e.Message);
            }
        }
        [HttpGet("GetNotificationSeller")]
        [Authorize]
        [ServiceFilter(typeof(ActionActivity))]
        [ServiceFilter(typeof(ActionActivityLog))]
        public async Task<IActionResult> GetNotificationSeller([FromQuery] GetNotificationRequest request)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

                var response = await _repo.GetNotificationSeller(request,userId, token);
                if (response == null || response.Count == 0)
                {
                    return BadRequest(new {status = StatusCodes.Status400BadRequest,message = "No Result Found!"});
                }
                return Ok(response);
            }
            catch (Exception e)
            {
                log.Error(e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError,e.Message);
            }
        }
        [HttpGet("SeenNotification")]
        [Authorize]
        [ServiceFilter(typeof(ActionActivity))]
        [ServiceFilter(typeof(ActionActivityLog))]
        public async Task<IActionResult> SeenNotification([FromQuery] SeenNotificationRequest request)
        {
            try
            {               
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

                var response = await _repo.SeenNotification(request,userId);
                if (response.StatusCode !=StatusCodes.Status200OK)
                {
                    return BadRequest(new {status = StatusCodes.Status400BadRequest,message = "No Result Found!"});
                }
                return Ok(response);
            }
            catch (Exception e)
            {
                log.Error(e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError,e.Message);
            }
        }

        [HttpPost("UpdateOrderStatus")]
        [Authorize]
        [ServiceFilter(typeof(ActionActivity))]
        [ServiceFilter(typeof(ActionActivityLog))]
        public async Task<IActionResult> UpdateOrderStatus(UpdateOrderStatusRequest request)
        {
            try
            {
                var currentUserLogin = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
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
                
                var path = _env.WebRootPath;
                var pathToFile = path
                    + Path.DirectorySeparatorChar.ToString()  
                    + "EmailTemplate.html";
                var response = await _repo.UpdateOrderStatus(request, currentUserLogin,platform);
                
                if (response.StatusCode != StatusCodes.Status200OK)
                {
                    return StatusCode(response.StatusCode, response);
                }
                return Ok(response);
            }
            catch (Exception e)
            {
                log.Error(e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError,e.Message);
            }
        }

        [HttpPost("UpdatePaymentStatus")]
        [Authorize]
        [ServiceFilter(typeof(ActionActivity))]
        [ServiceFilter(typeof(ActionActivityLog))]
        public async Task<IActionResult> UpdatePaymentStatus(UpdatePaymentStatusRequest request)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                var currentUserLogin = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

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

                var response = await _repo.UpdatePaymentStatus(request, currentUserLogin, token,platform);
                if (response.StatusCode != StatusCodes.Status200OK)
                {
                    return StatusCode(response.StatusCode, response);
                }
                return Ok(response);
            }
            catch (Exception e)
            {
                log.Error(e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError,e.Message);
            }
        }
        [HttpPost("UpdateDeliveryServiceStatus")]
        [Authorize]
        [ServiceFilter(typeof(ActionActivity))]
        [ServiceFilter(typeof(ActionActivityLog))]
        public async Task<IActionResult> UpdateDeliveryServiceStatus(UpdateDeliveryServiceStatusRequest request)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                var currentUserLogin = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                var response = await _repo.UpdateDeliveryServiceStatus(request, currentUserLogin,token);
                if (response.StatusCode != StatusCodes.Status200OK)
                {
                    return StatusCode(response.StatusCode, response);
                }
                return Ok(response);
            }
            catch (Exception e)
            {
                log.Error(e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError,e.Message);
            }
        }

        [HttpPost("SellerOrderCancel")]
        [Authorize]
        [ServiceFilter(typeof(ActionActivity))]
        [ServiceFilter(typeof(ActionActivityLog))]
        public async Task<IActionResult> SellerOrderCancel(OrderCancelRequest request)
        {
            try
            {
                var currentUserLogin = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
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
                var response = await _repo.SellerOrderCancel(request, currentUserLogin,platform);
                if (response.StatusCode != StatusCodes.Status200OK)
                {
                    return StatusCode(response.StatusCode, response);
                }
                return Ok(response);
            }
            catch (Exception e)
            {
                log.Error(e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError,e.Message);
            }
        }

        [HttpPost("BuyerOrderCancel")]
        [Authorize]
        [ServiceFilter(typeof(ActionActivity))]
        [ServiceFilter(typeof(ActionActivityLog))]
        public async Task<IActionResult> BuyerOrderCancel(OrderCancelRequest request)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                var currentUserLogin = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
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
                var response = await _repo.BuyerOrderCancel(request, currentUserLogin, token,platform);
                if (response.StatusCode != StatusCodes.Status200OK)
                {
                    return StatusCode(response.StatusCode, response);
                }
                return Ok(response);
            }
            catch (Exception e)
            {
                log.Error(e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError,e.Message);
            }
        }

        [HttpPost("PaymentAgain")]
        [Authorize]
        [ServiceFilter(typeof(ActionActivity))]
        [ServiceFilter(typeof(ActionActivityLog))]
        public async Task<IActionResult> PaymentAgain(PaymentAgainRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                string token = Request.Headers["Authorization"];
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

                var response = await _repo.PaymentAgain(request, userId,platform,token);
                if (response.StatusCode != StatusCodes.Status200OK)
                {
                    return StatusCode(response.StatusCode, response);
                }
                return Ok(response);
            }
            catch (Exception e)
            {
                log.Error(e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError,e.Message);
            }
        }

        [HttpPost("ChangeDeliveryAddress")]
        [Authorize]
        [ServiceFilter(typeof(ActionActivity))]
        [ServiceFilter(typeof(ActionActivityLog))]
        public async Task<IActionResult> ChangeDeliveryAddress(ChangeDeliveryAddressRequest request)
        {
            try
            {
                var response = await _repo.ChangeDeliveryAddress(request);
                if (response.StatusCode != StatusCodes.Status200OK)
                {
                    return StatusCode(response.StatusCode, response);
                }
                return Ok(response);
            }
            catch (Exception e)
            {
                log.Error(e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError,e.Message);
            }
        }
        [HttpPost("PaymentApprove")]
        [Authorize]
        [ServiceFilter(typeof(ActionActivity))]
        [ServiceFilter(typeof(ActionActivityLog))]
        public async Task<IActionResult> PaymentApprove(PaymentApproveRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

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

                var response = await _repo.PaymentApprove(request, userId,platform);
                if (response.StatusCode != StatusCodes.Status200OK)
                {
                    return StatusCode(response.StatusCode, response);
                }
                return Ok(response);
            }
            catch (Exception e)
            {
                log.Error(e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError,e.Message);
            }
        }
       
        [HttpGet("GetOrderDetail")]
        [Authorize]
        [ServiceFilter(typeof(ActionActivity))]
        [ServiceFilter(typeof(ActionActivityLog))]
        public async Task<IActionResult> GetOrderDetail(int orderId)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

                var response = await _repo.GetOrderDetail(orderId, token);
                if (response == null)
                {
                    return BadRequest(new {status = StatusCodes.Status400BadRequest,message = "No Result Found!"});
                }
                return Ok(response);
            }
            catch (Exception e)
            {
                log.Error(e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError,e.Message);
            }
        }
        
        [HttpGet("GetOrderListByProduct")]
        [Authorize]
        [ServiceFilter(typeof(ActionActivity))]
        [ServiceFilter(typeof(ActionActivityLog))]
        public async Task<IActionResult> GetOrderListByProduct([FromQuery]GetOrderListByProductRequest request)
        {
            try
            {                
                var response = await _repo.GetOrderListByProduct(request);
                if (response == null)
                {
                    return BadRequest(new {status = StatusCodes.Status400BadRequest,message = "No Result Found!"});
                }
                return Ok(response);
            }
            catch (Exception e)
            {
                log.Error(e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError,e.Message);
            }
        }
        [HttpGet("GetOrderListByProductId")]
        [Authorize]
        [ServiceFilter(typeof(ActionActivity))]
        [ServiceFilter(typeof(ActionActivityLog))]
        public async Task<IActionResult> GetOrderListByProductId([FromQuery]GetOrderListByProductIdRequest request)
        {
            try
            {                
                var response = await _repo.GetOrderListByProductId(request);
                if (response == null)
                {
                    return BadRequest(new {status = StatusCodes.Status400BadRequest,message = "No Result Found!"});
                }
                return Ok(response);
            }
            catch (Exception e)
            {
                log.Error(e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError,e.Message);
            }
        }
        [HttpGet("GetVoucherNoSuggestion")]
        [Authorize]
        [ServiceFilter(typeof(ActionActivity))]
        [ServiceFilter(typeof(ActionActivityLog))]
        public async Task<IActionResult> GetVoucherNoSuggestion([FromQuery] GetVoucherNoSuggestionRequest request)
        {
            try
            {
                var response = await _repo.GetVoucherNoSuggestion(request);
                return Ok(response);
            }
            catch (Exception e)
            {
                log.Error(e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError,e.Message);
            }
        }
        [HttpGet("GetVoucherNoSuggestionSeller")]
        [Authorize]
        [ServiceFilter(typeof(ActionActivity))]
        [ServiceFilter(typeof(ActionActivityLog))]
        public async Task<IActionResult> GetVoucherNoSuggestionSeller([FromQuery]GetVoucherNoSuggestionSellerRequest request)
        {
            try
            {
                var response = await _repo.GetVoucherNoSuggestionSeller(request);
                return Ok(response);
            }
            catch (Exception e)
            {
                log.Error(e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError,e.Message);
            }
        }

        [HttpGet("GetVoucher")]
        [Authorize]
        [ServiceFilter(typeof(ActionActivity))]
        [ServiceFilter(typeof(ActionActivityLog))]
        public async Task<IActionResult> GetVoucher(int orderId)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                var response = await _repo.GetVoucher(orderId,token);
                return Ok(response);
            }
            catch (Exception e)
            {
                log.Error(e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError,e.Message);
            }
        }
        [HttpGet("GetPOSVoucher")]
        [Authorize]
        [ServiceFilter(typeof(ActionActivity))]
        [ServiceFilter(typeof(ActionActivityLog))]
        public async Task<IActionResult> GetPOSVoucher(int orderId)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                var response = await _repo.GetPOSVoucher(orderId,userId,token);
                return Ok(response);
            }
            catch (Exception e)
            {
                log.Error(e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError,e.Message);
            }
        }
    
        [HttpPost("CallBackKPayNotify")]
        [AllowAnonymous]
        public async Task<IActionResult> CallBackKPayNotify(KBZNotifyRequest request)
        {
            log.Info(string.Format("KPay Notify Request - {0}",JsonConvert.SerializeObject(request)));
            
            try
            {
                var response = await _repo.CallBackKPayNotify(request);

                log.Info(string.Format("KPay Notify Response - {0}",response));

                return Ok(response);
            }
            catch (Exception e)
            {
                log.Error(e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError,e.Message);
            }
        }

        [HttpGet("GetDeliveryAddress")]
        [Authorize]
        [ServiceFilter(typeof(ActionActivity))]
        [ServiceFilter(typeof(ActionActivityLog))]
        public async Task<IActionResult> GetDeliveryAddress()
        {
            try
            {
                string token = Request.Headers["Authorization"];
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                var response = await _repo.GetDeliveryAddress(userId,token);
                return Ok(response);
            }
            catch (Exception e)
            {
                log.Error(e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError,e.Message);
            }
        }

        [HttpGet("GetDeliveryAddressDetail")]
        [Authorize]
        [ServiceFilter(typeof(ActionActivity))]
        [ServiceFilter(typeof(ActionActivityLog))]
        public async Task<IActionResult> GetDeliveryAddressDetail(int DeliveryAddressId)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                var response = await _repo.GetDeliveryAddressDetail(DeliveryAddressId,userId,token);
                return Ok(response);
            }
            catch (Exception e)
            {
                log.Error(e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError,e.Message);
            }
        }

        [HttpPost("CreateUserDeliveryAddress")]
        [Authorize]
        [ServiceFilter(typeof(ActionActivity))]
        [ServiceFilter(typeof(ActionActivityLog))]
        public async Task<IActionResult> CreateUserDeliveryAddress(CreateUserDeliveryAddressRequest request)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                if (request.CityId != 0 && request.TownshipId != 0)
                {
                    var response = await _repo.CreateUserDeliveryAddress(request,userId);
                    return Ok(response);
                }else{
                    return StatusCode(StatusCodes.Status500InternalServerError);
                }
                return Ok();
            }
            catch (Exception e)
            {
                log.Error(e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError,e.Message);
            }
        }

        [HttpPost("UpdateUserDeliveryAddress")]
        [Authorize]
        [ServiceFilter(typeof(ActionActivity))]
        [ServiceFilter(typeof(ActionActivityLog))]
        public async Task<IActionResult> UpdateUserDeliveryAddress(UpdateUserDeliveryAddressRequest request)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                var response = await _repo.UpdateUserDeliveryAddress(request,userId);
                return Ok(response);
            }
            catch (Exception e)
            {
                log.Error(e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError,e.Message);
            }
        }

        [HttpPost("ConfirmSelectedAddress")]
        [Authorize]
        [ServiceFilter(typeof(ActionActivity))]
        [ServiceFilter(typeof(ActionActivityLog))]
        public async Task<IActionResult> ConfirmSelectedAddress(GetDeliveryAddressRequest request)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                var response = await _repo.ConfirmSelectedAddress(request,userId);
                return Ok(response);
            }
            catch (Exception e)
            {
                log.Error(e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError,e.Message);
            }
        }

        [HttpGet("DeleteUserDeliveryAddress")]
        [Authorize]
        [ServiceFilter(typeof(ActionActivity))]
        [ServiceFilter(typeof(ActionActivityLog))]
        public async Task<IActionResult> DeleteUserDeliveryAddress(int DeliveryAddressId)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                var response = await _repo.DeleteUserDeliveryAddress(DeliveryAddressId,userId);
                return Ok(response);
            }
            catch (Exception e)
            {
                log.Error(e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError,e.Message);
            }
        }

        [HttpGet("GetDeliveryAddressLabel")]
        // [Authorize]
        // [ServiceFilter(typeof(ActionActivity))]
        // [ServiceFilter(typeof(ActionActivityLog))]
        public async Task<IActionResult> GetDeliveryAddressLabel(int DeliveryAddressId)
        {
            try
            {
                // string token = Request.Headers["Authorization"];
                // var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                var response = await _repo.GetDeliveryAddressLabel();
                return Ok(response);
            }
            catch (Exception e)
            {
                log.Error(e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError,e.Message);
            }
        }

    }
}
