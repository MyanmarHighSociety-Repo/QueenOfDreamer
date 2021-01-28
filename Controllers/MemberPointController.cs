using System;
using System.Security.Claims;
using System.Threading.Tasks;
using QueenOfDreamer.API.Dtos;
using QueenOfDreamer.API.Dtos.MembershipDto;
using QueenOfDreamer.API.Dtos.OrderDto;
using QueenOfDreamer.API.Helpers;
using QueenOfDreamer.API.Interfaces.Repos;
using QueenOfDreamer.API.Interfaces.Services;
using log4net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace QueenOfDreamer.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    public class MemberPointController : ControllerBase
    {
        private readonly IMemberPointRepository _memberPointRepo;
        
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public MemberPointController(IMemberPointRepository memberPointRepo)
        {
            _memberPointRepo = memberPointRepo;
        }

        [HttpGet("GetConfigMemberPoint")]
        [Authorize]
        [ServiceFilter(typeof(ActionActivity))]
        [ServiceFilter(typeof(ActionActivityLog))]
        public async Task<IActionResult> GetConfigMemberPoint()
        {
            try
            {
                string token = Request.Headers["Authorization"];
                var response = await _memberPointRepo.GetConfigMemberPoint(token);
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

        [HttpGet("GetConfigMemberPointById")]
        [Authorize]
        [ServiceFilter(typeof(ActionActivity))]
        [ServiceFilter(typeof(ActionActivityLog))]
        public async Task<IActionResult> GetConfigMemberPointById(int id)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                var response = await _memberPointRepo.GetConfigMemberPointById(id,token);
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

        [HttpPost("CreateProductReward")]
        [Authorize]
        [ServiceFilter(typeof(ActionActivity))]
        [ServiceFilter(typeof(ActionActivityLog))]
        public async Task<IActionResult> CreateProductReward(CreateProductRewardRequest request)
        {
            try
            {
                var response = await _memberPointRepo.CreateProductReward(request);
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

        [HttpPost("UpdateProductReward")]
        [Authorize]
        [ServiceFilter(typeof(ActionActivity))]
        [ServiceFilter(typeof(ActionActivityLog))]
        public async Task<IActionResult> UpdateProductReward(UpdateProductRewardRequest request)
        {
            try
            {
                var response = await _memberPointRepo.UpdateProductReward(request);
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

        [HttpGet("GetRewardProduct")]
        public async Task<IActionResult> GetRewardProduct([FromQuery]GetRewardProductRequest request)
        {
            try
            {
                var response = await _memberPointRepo.GetRewardProduct(request);
                // if (response == null || response.Count == 0)
                // {
                //     return BadRequest(new {status = StatusCodes.Status400BadRequest,message = "No Result Found!"});
                // }               
                return Ok(response);
            }
            catch (Exception e)
            {
                log.Error(e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError,e.Message);
            }
        }
        [HttpGet("GetRewardProductById")]
        [Authorize]
        [ServiceFilter(typeof(ActionActivity))]
        [ServiceFilter(typeof(ActionActivityLog))]
        public async Task<IActionResult> GetRewardProductById([FromQuery]GetRewardProductByIdRequest request)
        {
            try
            {                
                var response = await _memberPointRepo.GetRewardProductById(request);
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

        [HttpGet("GetRewardProductDetail")]
        [Authorize]
        [ServiceFilter(typeof(ActionActivity))]
        [ServiceFilter(typeof(ActionActivityLog))]
        public async Task<IActionResult> GetRewardProductDetail([FromQuery]GetRewardProductDetailRequest request)
        {
            try
            {       
                string token = Request.Headers["Authorization"];
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);         
                var response = await _memberPointRepo.GetRewardProductDetail(request,userId,token);
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
        
        [HttpGet("GetCartDetailForReward")]
        [Authorize]
        [ServiceFilter(typeof(ActionActivity))]
        [ServiceFilter(typeof(ActionActivityLog))]
        public async Task<IActionResult> GetCartDetailForReward(int productId,int skuId)
        {
            try
            {       
                string token = Request.Headers["Authorization"];
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);         
                var response = await _memberPointRepo.GetCartDetailForReward(productId,skuId,userId,token);
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

        [HttpDelete("DeleteProductReward")]
        [Authorize]
        [ServiceFilter(typeof(ActionActivity))]
        [ServiceFilter(typeof(ActionActivityLog))]
        public async Task<IActionResult> DeleteProductReward(int id)
        {
            try
            {          
                var response = await _memberPointRepo.DeleteProductReward(id);
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


        [HttpPost("RedeemOrder")]
        [Authorize]
        [ServiceFilter(typeof(ActionActivity))]
        [ServiceFilter(typeof(ActionActivityLog))]
        public async Task<IActionResult> RedeemOrder(RedeemOrderRequest request)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);      
                var response = await _memberPointRepo.RedeemOrder(request,userId,token);
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
        
        [HttpPost("RedeemOrderByKBZPay")]
        [Authorize]
        [ServiceFilter(typeof(ActionActivity))]
        [ServiceFilter(typeof(ActionActivityLog))]
        public async Task<IActionResult> RedeemOrderByKBZPay(RedeemOrderRequest request)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);      
                var response = await _memberPointRepo.RedeemOrderByKBZPay(request,userId,token);
                return Ok(response);
            }
            catch (Exception e)
            {
                log.Error(e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError,e.Message);
            }
        }

        [HttpGet("GetProductCategoryForCreateConfigMemberPoint")]
        [Authorize]
        [ServiceFilter(typeof(ActionActivity))]
        [ServiceFilter(typeof(ActionActivityLog))]
        public async Task<IActionResult> GetProductCategoryForCreateConfigMemberPoint()
        {
            try
            {       
                string token = Request.Headers["Authorization"];                        
                var response = await _memberPointRepo.GetProductCategoryForCreateConfigMemberPoint(token);
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

        [HttpGet("GetOrderDetailForMemberPoint_MS")]
        [Authorize]
        [ServiceFilter(typeof(ActionActivity))]
        [ServiceFilter(typeof(ActionActivityLog))]
        public async Task<IActionResult> GetOrderDetailForMemberPoint_MS(string voucherNo)
        {
            try
            {                         
                var response = await _memberPointRepo.GetOrderDetailForMemberPoint_MS(voucherNo);
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

        [HttpGet("GetProductListForAddProductReward")]
        [Authorize]
        [ServiceFilter(typeof(ActionActivity))]
        [ServiceFilter(typeof(ActionActivityLog))]
        public async Task<IActionResult> GetProductListForAddProductReward([FromQuery]GetProductListForAddProductRewardRequest request)
        {
            try
            {                         
                var response = await _memberPointRepo.GetProductListForAddProductReward(request);
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

    }
}