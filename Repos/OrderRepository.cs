using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using AutoMapper;
using QueenOfDreamer.API.Const;
using QueenOfDreamer.API.Context;
using QueenOfDreamer.API.Dtos.OrderDto;
using QueenOfDreamer.API.Interfaces.Repos;
using QueenOfDreamer.API.Interfaces.Services;
using QueenOfDreamer.API.Models;
using log4net;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using QueenOfDreamer.API.Dtos;
using QueenOfDreamer.API.Helpers;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using QueenOfDreamer.API.Dtos.MembershipDto;
using QueenOfDreamer.API.Dtos.GatewayDto;
using QueenOfDreamer.Dtos.GatewayDto;
using QueenOfDreamer.API.Dtos.MiscellanceousDto;

namespace QueenOfDreamer.API.Repos
{
    public class OrderRepository : IOrderRepository
    {
        private readonly QueenOfDreamerContext _context;

        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly IMapper _mapper;

        private readonly IUserServices _userServices;

        private readonly IPaymentGatewayServices _paymentservices;
        private readonly IDeliveryService _deliServices;

        private readonly IQueenOfDreamerServices _services;
        
        private readonly IMiscellaneousRepository _miscellaneousRepo;
        private readonly IMemberPointRepository _memberPointRepo;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public OrderRepository(QueenOfDreamerContext context, IMapper mapper
            , IUserServices userServices, IQueenOfDreamerServices service, 
            IPaymentGatewayServices paymentservices,
            IDeliveryService deliService,
            IMiscellaneousRepository miscellaneousRepo,
            IMemberPointRepository memberPointRepo,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _mapper = mapper;
            _userServices = userServices;
            _services = service;
            _paymentservices = paymentservices;
            _deliServices= deliService;
            _miscellaneousRepo=miscellaneousRepo;
            _memberPointRepo=memberPointRepo;
            _httpContextAccessor=httpContextAccessor;
        }

        public async Task<ResponseStatus> AddToCart(AddToCartRequest request, int userId,int platform)
        {            
            int qtyInHand = await _context.ProductSku
                        .Where(x => x.ProductId == request.ProductId && x.SkuId == request.SkuId)
                        .Select(s => s.Qty)
                        .SingleOrDefaultAsync();

            var checkTrnCart = await _context.TrnCart
                            .Where(x => x.ProductId == request.ProductId 
                            && x.SkuId == request.SkuId 
                            && x.UserId == userId)
                            .FirstOrDefaultAsync();

            if (qtyInHand == 0)
            {
                return new ResponseStatus()
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message= "ပစ္စည်းကုန်နေပါသည်။"
                };
            }

            if (qtyInHand >= request.Qty)
            {
                if (checkTrnCart != null)
                {
                    checkTrnCart.Qty = checkTrnCart.Qty + request.Qty;
                  
                }
                else
                {
                    var trnCart = new TrnCart()
                    {
                        ProductId = request.ProductId,
                        SkuId = request.SkuId,
                        UserId = userId,
                        Qty = request.Qty,
                        CreatedDate = DateTime.Now,                        
                    };

                    await _context.TrnCart.AddAsync(trnCart);                   
                }

                #region  Activity Log
                var data=new ActivityLog(){
                        UserId=userId,
                        ActivityTypeId=QueenOfDreamerConst.ACTIVITY_TYPE_ADD_TO_CART,
                        Value=request.Qty>1? request.Qty+" items to cart" :" 1 item to cart" ,
                        CreatedBy=userId,
                        CreatedDate=DateTime.Now,
                        PlatformId=platform,
                    };
                _context.ActivityLog.Add(data);
                #endregion      

                await _context.SaveChangesAsync();

                return new ResponseStatus()
                {
                StatusCode = StatusCodes.Status200OK,
                Message= "အောင်မြင်သည်။"
                };           
            }
            else
            {
                return new ResponseStatus()
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message= string.Format("ပစ္စည်းအရေအတွက် {0} ခုသာမှာလို့ရပါမည်။", qtyInHand)
                };
            }
        }

        public async Task<RemoveFromCartResponse> RemoveFromCart(RemoveFromCartRequest request, int userId,int platform)
        {

            var checkTrnCart = await _context.TrnCart
                            .Where(x => x.ProductId == request.ProductId 
                            &&x.SkuId == request.SkuId && x.UserId == userId)
                            .FirstOrDefaultAsync();

            if (checkTrnCart == null)
            {
                return new RemoveFromCartResponse()
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message="မအောင်မြင်ပါ။"
                };
            }
            else
            {
                _context.TrnCart.Remove(checkTrnCart);

                #region  Activity Log
                var productName=await _context.Product.Where(x=>x.Id==checkTrnCart.ProductId).Select(x=>x.Name).SingleOrDefaultAsync();
                var data=new ActivityLog(){
                        UserId=userId,
                        ActivityTypeId=QueenOfDreamerConst.ACTIVITY_TYPE_REMOVE_FROM_CART,
                        Value=productName,
                        CreatedBy=userId,
                        CreatedDate=DateTime.Now,
                        PlatformId=platform,
                    };
                _context.ActivityLog.Add(data);
                #endregion     

                await _context.SaveChangesAsync();

                return new RemoveFromCartResponse(){
                     StatusCode = StatusCodes.Status200OK,
                    Message="အောင်မြင်သည်။"
                };
            }
        }

        public async Task<GetCartDetailResponse> GetCartDetail_Old(int userId, string token)
        {
            bool isZawgyi=Rabbit.IsZawgyi(_httpContextAccessor);

            var response = new GetCartDetailResponse(){
                DeliveryInfo = new GetCartDetailDeliveryInfo(),
                NewPaymentService = new List<GetPaymentServiceForBuyerResponse>(),
                PaymentService = new List<GetCartDetailPaymentService>(),
                ProductIssues = new List<ProductIssues>(),
                TotalAmt = 0,
                StatusCode=StatusCodes.Status200OK
            };

            #region  Product info

             var itemFromCart = await _context.TrnCart.Where(x => x.UserId == userId)
                .Select(s => new GetCartDetailProductInfo
                {
                    ProductId = s.ProductId,
                    SkuId = s.SkuId,
                    Price=_context.ProductSku.Where(x => x.ProductId == s.ProductId && x.SkuId==s.SkuId)
                         .Select(x => x.Price).FirstOrDefault(),
                    // Price = _context.ProductPrice.Where(x => x.ProductId == s.ProductId)
                    //         .Select(s => s.Price).FirstOrDefault(),
                    ProductUrl = _context.ProductImage.Where(x => x.ProductId == s.ProductId && x.isMain == true)
                            .Select(s => s.Url).FirstOrDefault(),
                    Qty = s.Qty,
                    Name = _context.Product.Where(x => x.Id == s.ProductId).Select(s =>isZawgyi?Rabbit.Uni2Zg(s.Name):s.Name)
                            .FirstOrDefault(),
                    Variation = null,
                    AvailableQty = _context.ProductSku.Where(x => x.ProductId == s.ProductId &&
                          x.SkuId == s.SkuId).Select(s => s.Qty).FirstOrDefault(),
                    PromotePrice=_context.ProductPromotion.Where(x=>x.ProductId==s.ProductId).Select(x=>x.TotalAmt).FirstOrDefault()
                }).ToListAsync();

            foreach (var item in itemFromCart)
            {
                var skuValue = await (from psku in _context.ProductSkuValue
                                      from pvopt in _context.ProductVariantOption
                                      where psku.ProductId == item.ProductId
                                      && psku.SkuId == item.SkuId
                                      && psku.ProductId == pvopt.ProductId
                                      && psku.VariantId == pvopt.VariantId
                                      && psku.ValueId == pvopt.ValueId
                                      select isZawgyi?Rabbit.Uni2Zg(pvopt.ValueName):pvopt.ValueName).ToListAsync();

                item.Variation = string.Join(",", skuValue);

                #region GetPromotion
                var productPromote=_context.ProductPromotion.Where(x=>x.ProductId==item.ProductId).FirstOrDefault();
                
                double promotePrice=0;
                if(productPromote!=null && productPromote.Percent>0)
                {
                    double discountPrice= double.Parse((((double)productPromote.Percent/(double)100)*(double)item.Price).ToString("0.00"));
                    promotePrice=item.Price-discountPrice;
                }
                item.PromotePrice=promotePrice;
                
                #endregion
                item.ProductTypeId = (int) _context.Product.Where(p => p.Id == item.ProductId).Select(p => p.ProductTypeId).FirstOrDefault();

            }

            response.ProductInfo = itemFromCart;


            #endregion
            if (response.ProductInfo.Count > 0)
            {
                #region  Payment info
                response.PaymentService = await _context.PaymentService.Where(x => x.Id != QueenOfDreamerConst.PAYMENT_SERVICE_COD && x.IsActive == true)
                                        .Select(s => new GetCartDetailPaymentService
                                        {
                                            Id = s.Id,
                                            ImgUrl = s.ImgPath,
                                            Name =isZawgyi?Rabbit.Uni2Zg(s.Name):s.Name
                                        }).ToListAsync();                    
                
                response.NewPaymentService=await _miscellaneousRepo.GetPaymentServiceForBuyer();

                #endregion
                
                #region  Delivery info
            
                var ProductTypeId = response.ProductInfo.Max(p => p.ProductTypeId);
                var userInfo = await _userServices.GetUserInfo(userId, token);
                var userDeliAddress = await _context.UserDeliveryAddress.Where(a => a.UserId == userId && a.IsSelected == true).FirstOrDefaultAsync();
                if (userDeliAddress != null)
                {
                    string cityName=await _deliServices.GetCityName(token,userDeliAddress.CityId);
                    string townshipName=await _deliServices.GetTownshipName(token,userDeliAddress.TownshipId);

                    cityName=isZawgyi?Rabbit.Uni2Zg(cityName):cityName;
                    townshipName=isZawgyi?Rabbit.Uni2Zg(townshipName):townshipName;

                    if(userInfo.UpdatedDate != null && userInfo.UpdatedDate > userDeliAddress.UpdatedDate)
                    {
                        userDeliAddress.Address = userInfo.Address==null?" ":userInfo.Address;
                        userDeliAddress.TownshipId = userInfo.TownshipId;
                        userDeliAddress.CityId = userInfo.CityId;
                        userDeliAddress.UpdatedDate = DateTime.Now;
                        await _context.SaveChangesAsync();
                    }
                    #region GetDeliveryServiceRate
                    var deliveryServiceList= await _deliServices.GetDeliveryFee(ProductTypeId,
                                    int.Parse(userDeliAddress.CityId.ToString()),
                                    int.Parse(userDeliAddress.TownshipId.ToString()),
                                    token);      

                    var otherOptions = await _deliServices.GetOtherOptionServiceRate(ProductTypeId, int.Parse(userDeliAddress.CityId.ToString()),token);       
                    #endregion

                    GetCartDetailDeliveryInfo cartDetailDeliveryInfo = new GetCartDetailDeliveryInfo();
                    cartDetailDeliveryInfo.UserId = userDeliAddress.UserId;
                    cartDetailDeliveryInfo.CityId = userDeliAddress.CityId;
                    cartDetailDeliveryInfo.TownshipId = userDeliAddress.TownshipId;
                    cartDetailDeliveryInfo.AreaInfo = townshipName + " ၊ " +cityName;
                    cartDetailDeliveryInfo.Address = userDeliAddress.Address==null?" ":(isZawgyi?Rabbit.Uni2Zg(userDeliAddress.Address):userDeliAddress.Address);
                    // cartDetailDeliveryInfo.DeliveryAmt = deliveryRate.ServiceAmount;
                    // cartDetailDeliveryInfo.DeliveryServiceId = deliveryRate.DeliveryServiceId;
                    // cartDetailDeliveryInfo.FromEstDeliveryDay = deliveryRate.FromEstDeliveryDay;
                    // cartDetailDeliveryInfo.ToEstDeliveryDay = deliveryRate.ToEstDeliveryDay;
                    cartDetailDeliveryInfo.CityName = cityName;
                    cartDetailDeliveryInfo.TownshipName = townshipName;
                    cartDetailDeliveryInfo.Name = userInfo.Name;
                    cartDetailDeliveryInfo.PhoNo = userInfo.PhoneNo;
                    cartDetailDeliveryInfo.Remark = String.Empty;
                    cartDetailDeliveryInfo.DeliveryService = deliveryServiceList;
                    cartDetailDeliveryInfo.OtherOptionService = otherOptions;
                    // cartDetailDeliveryInfo.DeliveryDate = DateTime.Now.Date.AddDays(deliveryRate.ToEstDeliveryDay).ToString("dd MMM yyyy") + "(3PM - 5PM)";
                    // cartDetailDeliveryInfo.DeliveryFromTime = String.Empty;
                    // cartDetailDeliveryInfo.DeliveryToTime = String.Empty;

                    response.DeliveryInfo = cartDetailDeliveryInfo;
                }
                else
                {
                    string cityName=await _deliServices.GetCityName(token,userInfo.CityId);
                    string townshipName=await _deliServices.GetTownshipName(token,userInfo.TownshipId);

                    cityName=isZawgyi?Rabbit.Uni2Zg(cityName):cityName;
                    townshipName=isZawgyi?Rabbit.Uni2Zg(townshipName):townshipName;

                    var defaultAddress = new UserDeliveryAddress{
                        UserId = userId,
                        CityId = userInfo.CityId,
                        TownshipId = userInfo.TownshipId,
                        Address = userInfo.Address,
                        IsSelected = true,
                        LabelName = "Home",
                        Landmark = ""
                    };
                    await _context.UserDeliveryAddress.AddAsync(defaultAddress);
                    await _context.SaveChangesAsync();
                    
                    #region GetDeliveryServiceRate
                    var deliveryRate= await _deliServices.GetDeliveryFee(ProductTypeId,
                                    int.Parse(userInfo.CityId.ToString()),
                                    int.Parse(userInfo.TownshipId.ToString()),
                                    token);    

                    var otherOptions = await _deliServices.GetOtherOptionServiceRate(ProductTypeId, int.Parse(userInfo.CityId.ToString()),token);           
                    #endregion

                    var deliveryInfo= new GetCartDetailDeliveryInfo()
                    {
                        CityId = defaultAddress.CityId,
                        TownshipId = defaultAddress.TownshipId,
                        AreaInfo =  townshipName + " ၊ " +cityName,
                        CityName =  cityName,
                        TownshipName =  townshipName,
                        Address = String.IsNullOrEmpty(defaultAddress.Address)?" ":(isZawgyi?Rabbit.Uni2Zg(defaultAddress.Address):defaultAddress.Address),
                        // DeliveryAmt = deliveryRate.ServiceAmount,
                        // DeliveryServiceId = QueenOfDreamerConst.CUSTOM_DELIVERY_SERVICE_ID,
                        // FromEstDeliveryDay = deliveryRate.FromEstDeliveryDay,
                        // ToEstDeliveryDay = deliveryRate.ToEstDeliveryDay,
                        UserId = userId,
                        Name = userInfo.Name,
                        PhoNo = userInfo.PhoneNo,
                        Remark = String.Empty,
                        DeliveryService = deliveryRate,
                        OtherOptionService = otherOptions,
                        // DeliveryDate = DateTime.Now.Date.AddDays(deliveryRate.ToEstDeliveryDay).ToString("dd MMM yyyy") + "(3PM - 5PM)",
                        // DeliveryFromTime = String.Empty,
                        // DeliveryToTime = String.Empty,
                    };
                    response.DeliveryInfo = deliveryInfo;
                }
            
                #endregion
            
                #region  Total amount info
                foreach (var item in response.ProductInfo)
                        {
                            if(item.PromotePrice>0)
                            {
                                response.TotalAmt += item.PromotePrice * item.Qty;
                            }
                            else{
                                response.TotalAmt += item.Price * item.Qty;
                            }
                            
                        }

                        if (response.DeliveryInfo.DeliveryService != null)
                        {
                            
                            response.DeliveryFee=response.DeliveryInfo.DeliveryService.Where(d => d.Default == true).Select(d => d.DeliveryFee).FirstOrDefault();
                        
                        }else{
                            response.DeliveryFee = 0;
                        }
                        response.NetAmt = response.TotalAmt + response.DeliveryFee;
                #endregion

                #region Check qty before order

                var issueList=new List<ProductIssues>();
                foreach (var item in response.ProductInfo)
                {
                    var product=await _context.Product.Where(x=>x.Id==item.ProductId).SingleOrDefaultAsync();
                    var skuProductQty = await _context.ProductSku.Where(x => x.ProductId == item.ProductId && x.SkuId == item.SkuId).FirstOrDefaultAsync();
                    if(!product.IsActive)
                    {
                            var issue=new ProductIssues(){
                                ProductId=item.ProductId,
                                ProductName=isZawgyi?Rabbit.Uni2Zg(product.Name):product.Name,
                                Action="Delete",
                                Qty=skuProductQty.Qty,
                                Reason=string.Format("Your order item - {0} has been deleted by seller.",product.Name)
                            };
                            issueList.Add(issue);
                    }
                    
                    else if (skuProductQty != null)
                    {
                        if(item.Qty>skuProductQty.Qty){//Check if add to cart qty > stock qty. Can't make order
                            
                            var issue=new ProductIssues(){
                                ProductId=item.ProductId,
                                ProductName=isZawgyi?Rabbit.Uni2Zg(product.Name):product.Name,
                                    Action="OutOfStock",
                                Qty=skuProductQty.Qty,
                                Reason=string.Format("You'er order {0} of {1}, but we only have {2} left.",(item.Qty>1?item.Qty+" quantities" : item.Qty+" quantity" ),(product.Name),(skuProductQty.Qty>1?skuProductQty.Qty+" quantities" : skuProductQty.Qty+" quantity" ))
                            };
                            issueList.Add(issue);                                
                        }            
                    }                         
                }
                if(issueList.Count()>0)
                {              
                    response.ProductIssues=issueList;
                }  
                #endregion

            }
           
            return response;
           
        }
    
         public async Task<GetCartDetailResponse> GetCartDetail(int userId, string token)
        {
            bool isZawgyi=Rabbit.IsZawgyi(_httpContextAccessor);

            var response = new GetCartDetailResponse(){
                StatusCode=StatusCodes.Status200OK
            };

            #region  Product info

             var itemFromCart = await _context.TrnCart.Where(x => x.UserId == userId)
                .Select(s => new GetCartDetailProductInfo
                {
                    ProductId = s.ProductId,
                    SkuId = s.SkuId,
                    Price=_context.ProductSku.Where(x => x.ProductId == s.ProductId && x.SkuId==s.SkuId)
                         .Select(x => x.Price).FirstOrDefault(),
                    // Price = _context.ProductPrice.Where(x => x.ProductId == s.ProductId)
                    //         .Select(s => s.Price).FirstOrDefault(),
                    ProductUrl = _context.ProductImage.Where(x => x.ProductId == s.ProductId && x.isMain == true)
                            .Select(s => s.Url).FirstOrDefault(),
                    Qty = s.Qty,
                    Name = _context.Product.Where(x => x.Id == s.ProductId).Select(s =>isZawgyi?Rabbit.Uni2Zg(s.Name):s.Name)
                            .FirstOrDefault(),
                    Variation = null,
                    AvailableQty = _context.ProductSku.Where(x => x.ProductId == s.ProductId &&
                          x.SkuId == s.SkuId).Select(s => s.Qty).FirstOrDefault(),
                    PromotePrice=_context.ProductPromotion.Where(x=>x.ProductId==s.ProductId).Select(x=>x.TotalAmt).FirstOrDefault()
                }).ToListAsync();

            foreach (var item in itemFromCart)
            {
                var skuValue = await (from psku in _context.ProductSkuValue
                                      from pvopt in _context.ProductVariantOption
                                      where psku.ProductId == item.ProductId
                                      && psku.SkuId == item.SkuId
                                      && psku.ProductId == pvopt.ProductId
                                      && psku.VariantId == pvopt.VariantId
                                      && psku.ValueId == pvopt.ValueId
                                      select isZawgyi?Rabbit.Uni2Zg(pvopt.ValueName):pvopt.ValueName).ToListAsync();

                item.Variation = string.Join(",", skuValue);

                #region GetPromotion
                var productPromote=_context.ProductPromotion.Where(x=>x.ProductId==item.ProductId).FirstOrDefault();
                
                double promotePrice=0;
                if(productPromote!=null && productPromote.Percent>0)
                {
                    double discountPrice= double.Parse((((double)productPromote.Percent/(double)100)*(double)item.Price).ToString("0.00"));
                    promotePrice=item.Price-discountPrice;
                }
                item.PromotePrice=promotePrice;
                
                #endregion

            }

            response.ProductInfo = itemFromCart;

            #endregion
           
            #region  Payment info
             response.PaymentService = await _context.PaymentService.Where(x => x.Id != QueenOfDreamerConst.PAYMENT_SERVICE_COD && x.IsActive == true)
                                    .Select(s => new GetCartDetailPaymentService
                                    {
                                        Id = s.Id,
                                        ImgUrl = s.ImgPath,
                                        Name =isZawgyi?Rabbit.Uni2Zg(s.Name):s.Name
                                    }).ToListAsync();                    
            
            response.NewPaymentService=await _miscellaneousRepo.GetPaymentServiceForBuyer();

            #endregion

            #region  Delivery info

            var userInfo = await _userServices.GetUserInfo(userId, token);
            
            var trnCartDeliInfo = await _context.TrnCartDeliveryInfo.Where(x => x.UserId == userId).FirstOrDefaultAsync();
            if (trnCartDeliInfo != null)
            {
                string cityName=await _deliServices.GetCityName(token,trnCartDeliInfo.CityId);
                string townshipName=await _deliServices.GetTownshipName(token,trnCartDeliInfo.TownshipId);
                
                cityName=isZawgyi?Rabbit.Uni2Zg(cityName):cityName;
                townshipName=isZawgyi?Rabbit.Uni2Zg(townshipName):townshipName;

                // var latestCartInfoUpdatedDate = await _context.TrnCart
                //                                 .Where(x => x.UserId == userId)
                //                                 .OrderByDescending(o => o.CreatedDate)
                //                                 .Select(s => s.CreatedDate)
                //                                 .FirstOrDefaultAsync();
                
                // if(userInfo.UpdatedDate != null && userInfo.UpdatedDate > latestCartInfoUpdatedDate)
                if(userInfo.UpdatedDate != null && userInfo.UpdatedDate > trnCartDeliInfo.UpdatedDate)
                {
                    trnCartDeliInfo.Name = userInfo.Name;
                    trnCartDeliInfo.Address = userInfo.Address==null?" ":userInfo.Address;
                    trnCartDeliInfo.PhNo = userInfo.PhoneNo;
                    trnCartDeliInfo.TownshipId = userInfo.TownshipId;
                    trnCartDeliInfo.CityId = userInfo.CityId;
                    await _context.SaveChangesAsync();
                }

                GetCartDetailDeliveryInfo cartDetailDeliveryInfo = new GetCartDetailDeliveryInfo();
                cartDetailDeliveryInfo.UserId = trnCartDeliInfo.UserId;
                cartDetailDeliveryInfo.CityId = trnCartDeliInfo.CityId;
                cartDetailDeliveryInfo.TownshipId = trnCartDeliInfo.TownshipId;
                cartDetailDeliveryInfo.AreaInfo = townshipName + " ၊ " +cityName;
                cartDetailDeliveryInfo.Address = trnCartDeliInfo.Address==null?" ":(isZawgyi?Rabbit.Uni2Zg(trnCartDeliInfo.Address):trnCartDeliInfo.Address);
                cartDetailDeliveryInfo.DeliveryAmt = trnCartDeliInfo.DeliveryAmt;
                cartDetailDeliveryInfo.DeliveryServiceId = trnCartDeliInfo.DeliveryServiceId;
                cartDetailDeliveryInfo.FromEstDeliveryDay = trnCartDeliInfo.FromEstDeliveryDay;
                cartDetailDeliveryInfo.ToEstDeliveryDay = trnCartDeliInfo.ToEstDeliveryDay;
                cartDetailDeliveryInfo.CityName = cityName;
                cartDetailDeliveryInfo.TownshipName = townshipName;
                cartDetailDeliveryInfo.Name =isZawgyi?Rabbit.Uni2Zg(trnCartDeliInfo.Name):trnCartDeliInfo.Name;
                cartDetailDeliveryInfo.PhoNo = trnCartDeliInfo.PhNo;
                cartDetailDeliveryInfo.Remark =isZawgyi?Rabbit.Uni2Zg(trnCartDeliInfo.Remark): trnCartDeliInfo.Remark;
                if (trnCartDeliInfo.DeliveryDate != DateTime.MinValue)
                {
                    cartDetailDeliveryInfo.DeliveryDate = trnCartDeliInfo.DeliveryDate.Date.ToString("yyyy-MM-dd") + "(" + trnCartDeliInfo.DeliveryFromTime + " - " + trnCartDeliInfo.DeliveryToTime + ")";
                }
                else
                {
                    cartDetailDeliveryInfo.DeliveryDate = String.Empty;
                }
                cartDetailDeliveryInfo.DeliveryFromTime = trnCartDeliInfo.DeliveryFromTime;
                cartDetailDeliveryInfo.DeliveryToTime = trnCartDeliInfo.DeliveryToTime;
                response.DeliveryInfo = cartDetailDeliveryInfo;
               
            }
            else
            {

                string cityName=await _deliServices.GetCityName(token,userInfo.CityId);
                string townshipName=await _deliServices.GetTownshipName(token,userInfo.TownshipId);

                cityName=isZawgyi?Rabbit.Uni2Zg(cityName):cityName;
                townshipName=isZawgyi?Rabbit.Uni2Zg(townshipName):townshipName;

                #region GetDeliveryServiceRate
                var deliveryRate= await _deliServices.GetDeliveryServiceRate(QueenOfDreamerConst.CUSTOM_DELIVERY_SERVICE_ID,
                                int.Parse(userInfo.CityId.ToString()),
                                int.Parse(userInfo.TownshipId.ToString()),
                                token);               
                #endregion

                var deliveryInfo= new GetCartDetailDeliveryInfo()
                {
                    CityId = userInfo.CityId,
                    TownshipId = userInfo.TownshipId,
                    AreaInfo =  townshipName + " ၊ " +cityName,
                    CityName =  cityName,
                    TownshipName =  townshipName,
                    Address = String.IsNullOrEmpty(userInfo.Address)?" ":(isZawgyi?Rabbit.Uni2Zg(userInfo.Address):userInfo.Address),
                    DeliveryAmt = deliveryRate.ServiceAmount,
                    DeliveryServiceId = QueenOfDreamerConst.CUSTOM_DELIVERY_SERVICE_ID,
                    FromEstDeliveryDay = deliveryRate.FromEstDeliveryDay,
                    ToEstDeliveryDay = deliveryRate.ToEstDeliveryDay,
                    UserId = userId,
                    Name = userInfo.Name,
                    PhoNo = userInfo.PhoneNo,
                    Remark = String.Empty,
                    DeliveryDate = DateTime.Now.Date.AddDays(deliveryRate.ToEstDeliveryDay).ToString("yyyy-MM-dd") + "(3PM - 5PM)",
                    DeliveryFromTime = String.Empty,
                    DeliveryToTime = String.Empty,
                };
                response.DeliveryInfo = deliveryInfo;
                }


            #endregion
            
            #region  Total amount info
            foreach (var item in response.ProductInfo)
                    {
                        if(item.PromotePrice>0)
                        {
                            response.TotalAmt += item.PromotePrice * item.Qty;
                        }
                        else{
                            response.TotalAmt += item.Price * item.Qty;
                        }
                        
                    }
                    response.DeliveryFee=response.DeliveryInfo.DeliveryAmt;
                    response.NetAmt = response.TotalAmt + response.DeliveryInfo.DeliveryAmt;
            #endregion

            #region Check qty before order

            var issueList=new List<ProductIssues>();
            foreach (var item in response.ProductInfo)
            {
                var product=await _context.Product.Where(x=>x.Id==item.ProductId).SingleOrDefaultAsync();
                var skuProductQty = await _context.ProductSku.Where(x => x.ProductId == item.ProductId && x.SkuId == item.SkuId).FirstOrDefaultAsync();
                if(!product.IsActive)
                {
                        var issue=new ProductIssues(){
                            ProductId=item.ProductId,
                            SkuId=item.SkuId,
                            ProductName=isZawgyi?Rabbit.Uni2Zg(product.Name):product.Name,
                            Action="Delete",
                            Qty=skuProductQty.Qty,
                            Reason=string.Format("Your order item - {0} has been deleted by seller.",product.Name)
                        };
                        issueList.Add(issue);
                }
                
                else if (skuProductQty != null)
                {
                    if(item.Qty>skuProductQty.Qty){//Check if add to cart qty > stock qty. Can't make order
                        
                        var issue=new ProductIssues(){
                            ProductId=item.ProductId,
                            SkuId=item.SkuId,
                            ProductName=isZawgyi?Rabbit.Uni2Zg(product.Name):product.Name,
                                Action="OutOfStock",
                            Qty=skuProductQty.Qty,
                            Reason=string.Format("You'er order {0} of {1}, but we only have {2} left.",(item.Qty>1?item.Qty+" quantities" : item.Qty+" quantity" ),(product.Name),(skuProductQty.Qty>1?skuProductQty.Qty+" quantities" : skuProductQty.Qty+" quantity" ))
                        };
                        issueList.Add(issue);                                
                    }            
                }                         
            }
            if(issueList.Count()>0)
            {              
                response.ProductIssues=issueList;
            }  
            #endregion

            return response;
           
        }
    
        public async Task<ResponseStatus> UpdateDeliveryInfo(UpdateDeliveryInfoRequest request, int userId, string token)
        { 
            bool isZawgyi=Rabbit.IsZawgyi(_httpContextAccessor);

            foreach (var trnCart in request.ProductCarts)
            {
                var trnCartRes = await _context.TrnCart.Where(x => x.UserId == request.UserId && x.SkuId == trnCart.SkuId && x.ProductId == trnCart.ProductId).FirstOrDefaultAsync();
                if (trnCartRes != null)
                {
                    trnCartRes.Qty = trnCart.Qty;
                    await _context.SaveChangesAsync();
                }
            }
            
            string cityName=await _deliServices.GetCityName(token,request.CityId);
            string townshipName=await _deliServices.GetTownshipName(token,request.TownshipId);

            var userInfo = await _userServices.GetUserInfo(userId, token);

            #region GetDeliveryServiceRate
            var deliveryServiceRate=await _deliServices.GetDeliveryServiceRate(
                                    QueenOfDreamerConst.CUSTOM_DELIVERY_SERVICE_ID,
                                    request.CityId,
                                    request.TownshipId,
                                    token
                                    );
            #endregion

            var deliveryInfo= new GetCartDetailDeliveryInfo()
               {
                   CityId = request.CityId,
                   TownshipId = request.TownshipId,
                   AreaInfo = townshipName + " ၊ " + cityName,
                   CityName = cityName,
                   TownshipName = townshipName,
                   Address =isZawgyi?Rabbit.Zg2Uni(request.Address):request.Address,
                   DeliveryAmt = deliveryServiceRate.ServiceAmount,
                //    DeliveryServiceId = QueenOfDreamerConst.CUSTOM_DELIVERY_SERVICE_ID,
                //    FromEstDeliveryDay = deliveryServiceRate.FromEstDeliveryDay,
                //    ToEstDeliveryDay = deliveryServiceRate.ToEstDeliveryDay,
                   UserId = userId,
                   Name = userInfo.Name,
                   PhoNo = userInfo.PhoneNo,
                   Remark =isZawgyi?Rabbit.Zg2Uni(request.Remark):request.Remark,
                //    DeliveryDate = String.Empty
               };   
             

            var trnCartDeliInfoRes = await _context.TrnCartDeliveryInfo.Where(x => x.UserId == userId).FirstOrDefaultAsync();
            if (trnCartDeliInfoRes != null)
            {
                trnCartDeliInfoRes.Address = deliveryInfo.Address;
                trnCartDeliInfoRes.Remark = deliveryInfo.Remark;
                // trnCartDeliInfoRes.DeliveryServiceId = deliveryInfo.DeliveryServiceId;
                trnCartDeliInfoRes.CityId = deliveryInfo.CityId;
                trnCartDeliInfoRes.TownshipId = deliveryInfo.TownshipId;
                // trnCartDeliInfoRes.FromEstDeliveryDay = deliveryInfo.FromEstDeliveryDay;
                // trnCartDeliInfoRes.ToEstDeliveryDay = deliveryInfo.ToEstDeliveryDay;
                trnCartDeliInfoRes.DeliveryAmt = deliveryInfo.DeliveryAmt;
            }
            else
            {
                var orderDeliveryInfoToAdd = new TrnCartDeliveryInfo
                {
                    UserId = userId,
                    Name = deliveryInfo.Name,
                    Address = deliveryInfo.Address,
                    PhNo = deliveryInfo.PhoNo,
                    Remark = deliveryInfo.Remark,
                    DeliveryDate = DateTime.Now,
                    DeliveryFromTime = "",
                    DeliveryToTime = "",
                    // DeliveryServiceId = deliveryInfo.DeliveryServiceId,
                    CityId = deliveryInfo.CityId,
                    TownshipId = deliveryInfo.TownshipId,
                    // FromEstDeliveryDay = deliveryInfo.FromEstDeliveryDay,
                    // ToEstDeliveryDay = deliveryInfo.ToEstDeliveryDay,
                    DeliveryAmt = deliveryInfo.DeliveryAmt,
                    UpdatedDate = DateTime.Now
                };
                await _context.TrnCartDeliveryInfo.AddAsync(orderDeliveryInfoToAdd);
            }
            await _context.SaveChangesAsync();
            return new ResponseStatus(){StatusCode=StatusCodes.Status200OK, Message="အောင်မြင်သည်။"};
        }

        public async Task<ResponseStatus> UpdateDeliveryDateAndTime(UpdateDeliveryDateAndTimeRequest request, int userId, string token)
        {
            foreach (var trnCart in request.ProductCarts)
            {
                var trnCartRes = await _context.TrnCart.Where(x => x.UserId == userId && x.SkuId == trnCart.SkuId && x.ProductId == trnCart.ProductId).FirstOrDefaultAsync();
                if (trnCartRes != null)
                {
                    trnCartRes.Qty = trnCart.Qty;
                    await _context.SaveChangesAsync();
                }
            }

            var userInfo = await _userServices.GetUserInfo(userId, token);

            string cityName=await _deliServices.GetCityName(token,userInfo.CityId);
            string townshipName=await _deliServices.GetTownshipName(token,userInfo.TownshipId);

            #region GetDeliveryServiceRate
            var deliveryServiceRate=await _deliServices.GetDeliveryServiceRate(
                                    QueenOfDreamerConst.CUSTOM_DELIVERY_SERVICE_ID,
                                    userInfo.CityId,
                                    userInfo.TownshipId,
                                    token
                                    );
            #endregion


            var deliveryInfo =  new GetCartDetailDeliveryInfo()
               {
                   CityId = userInfo.CityId,
                   TownshipId = userInfo.TownshipId,
                   AreaInfo = townshipName + '၊' + cityName,
                   CityName = cityName,
                   TownshipName = townshipName,
                   Address = userInfo.Address,
                   DeliveryAmt = deliveryServiceRate.ServiceAmount,
                //    DeliveryServiceId = QueenOfDreamerConst.CUSTOM_DELIVERY_SERVICE_ID,
                //    FromEstDeliveryDay = deliveryServiceRate.FromEstDeliveryDay,
                //    ToEstDeliveryDay = deliveryServiceRate.ToEstDeliveryDay,
                   UserId = userId,
                   Name = userInfo.Name,
                   PhoNo = userInfo.PhoneNo,
                   Remark = String.Empty,
                //    DeliveryDate = String.Empty

               };

            var trnCartDeliInfoRes = await _context.TrnCartDeliveryInfo.Where(x => x.UserId == userId).FirstOrDefaultAsync();
            if (trnCartDeliInfoRes != null)
            {
                trnCartDeliInfoRes.DeliveryDate = request.DeliveryDate;
                trnCartDeliInfoRes.DeliveryFromTime = request.DeliveryFromTime;
                trnCartDeliInfoRes.DeliveryToTime = request.DeliveryToTime;
            }
            else
            {
                var orderDeliveryInfoToAdd = new TrnCartDeliveryInfo
                {
                    UserId = userId,
                    Name = deliveryInfo.Name,
                    Address = deliveryInfo.Address,
                    PhNo = deliveryInfo.PhoNo,
                    Remark = String.Empty,
                    DeliveryDate = request.DeliveryDate,
                    DeliveryFromTime = request.DeliveryFromTime,
                    DeliveryToTime = request.DeliveryToTime,
                    // DeliveryServiceId = deliveryInfo.DeliveryServiceId,
                    CityId = deliveryInfo.CityId,
                    TownshipId = deliveryInfo.TownshipId,
                    // FromEstDeliveryDay = deliveryInfo.FromEstDeliveryDay,
                    // ToEstDeliveryDay = deliveryInfo.ToEstDeliveryDay,
                    DeliveryAmt = deliveryInfo.DeliveryAmt
                };
                await _context.TrnCartDeliveryInfo.AddAsync(orderDeliveryInfoToAdd);
            }

            var cartItemToUpdate = await _context.TrnCart.Where(x => x.UserId == userId)
                .OrderByDescending(o => o.CreatedDate).FirstOrDefaultAsync();
            if(cartItemToUpdate!=null)
            {
                cartItemToUpdate.CreatedDate = DateTime.Now;
            }           

            await _context.SaveChangesAsync();
             return new ResponseStatus(){StatusCode=StatusCodes.Status200OK, Message="အောင်မြင်သည်။"};
        }

        public async Task<GetDeliverySlotResponse> GetDeliverySlot(GetDeliverySlotRequest request,int userId)
        {
            GetDeliverySlotResponse response = new GetDeliverySlotResponse();
            WeekOne weekOne = new WeekOne();
            WeekTwo weekTwo = new WeekTwo();
            List<Occupied> occupiedList = new List<Occupied>();
            List<DeliverySerivceTime> DeliverySerivceTimeList = new List<DeliverySerivceTime>();
            DateTime deliDate = DateTime.Now.Date.AddDays(request.ToEstDeliveryDay - 1);
            var monday = deliDate.AddDays(-(int)deliDate.DayOfWeek + (int)DayOfWeek.Monday);
            var sunday = monday.AddDays(6);
            weekOne.StartDate = monday;
            weekOne.EndDate = sunday;
            response.WeekOne = weekOne;

            var nextMonday = monday.AddDays(7);
            var nextSunday = sunday.AddDays(7);
            weekTwo.StartDate = nextMonday;
            weekTwo.EndDate = nextSunday;
            response.WeekTwo = weekTwo;

            if (monday.Date > deliDate.Date)
            {
                response.SelectedDate = monday.ToString("dd MMM yyyy") + "(3PM - 5PM)";
            }
            else
            {
                response.SelectedDate = deliDate.ToString("dd MMM yyyy") + "(3PM - 5PM)";
            }


            var orderDeliList = from p in _context.OrderDeliveryInfo
                                where (p.DeliveryDate).Date >= monday.Date && (p.DeliveryDate).Date <= nextSunday.Date
                                group new { p } by new { p.FromTime, p.ToTime, p.DeliveryDate.Date }
                                                into grp
                                select new
                                {
                                    DeliveryDate = grp.Key.Date,
                                    OrderCount = grp.Count(),
                                    FromTime = grp.Key.FromTime,
                                    ToTime = grp.Key.ToTime
                                };
            if (orderDeliList != null)
            {
                foreach (var orderDeli in orderDeliList)
                {
                    Occupied occupied = new Occupied();
                    if (orderDeli.OrderCount >= QueenOfDreamerConst.DELIVERY_COUNT)
                    {
                        occupied.DeliveryDate = orderDeli.DeliveryDate;
                        occupied.FromTime = orderDeli.FromTime;
                        occupied.ToTime = orderDeli.ToTime;
                        occupiedList.Add(occupied);
                    }
                }
                response.OccupiedList = occupiedList;
            }
            var deliveryServiceList = await _context.Setting.Where(x => x.Name.Contains("DeliveryPeriod") && x.IsActive == true).ToListAsync();
            if (deliveryServiceList.Count > 0)
            {
                foreach (var deliveryServiceTime in deliveryServiceList)
                {
                    DeliverySerivceTime deliverySerivce = new DeliverySerivceTime();
                    var path = deliveryServiceTime.Value;
                    string[] stp = path.Split(" - ");
                    deliverySerivce.FromTime = stp[0];
                    deliverySerivce.ToTime = stp[1];
                    DeliverySerivceTimeList.Add(deliverySerivce);
                }
                response.DeliverySerivceTimeList = DeliverySerivceTimeList;
            }
            
            var trnDelivery =await _context.TrnCartDeliveryInfo
            .Where(x=>x.UserId==userId).SingleOrDefaultAsync();

            if(trnDelivery==null)
            {
                 response.UserSelectedDeliveryDate=response.SelectedDate;
            }
            else{
                response.UserSelectedDeliveryDate=trnDelivery.DeliveryDate.ToString("dd MMM yyyy") + "("+trnDelivery.DeliveryFromTime+" - "+trnDelivery.DeliveryToTime+")";
            }

            return response;
        }
    
        public async Task<PostOrderResponse> PostOrder(PostOrderRequest req, int userId,string token,int platform)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    bool isZawgyi=Rabbit.IsZawgyi(_httpContextAccessor);

                    #region Check qty before order

                    var issueList=new List<ProductIssues>();
                    PostOrderResponse response = new PostOrderResponse();
                    if(req.ProductInfo.Count==0)
                    {
                         var issue=new ProductIssues(){
                                    ProductId=0,
                                    ProductName="",
                                    Action="ItemNotFound",
                                    Qty=0,
                                    Reason="There's no item to make an order"
                                };
                                issueList.Add(issue);
                    }
                    foreach (var item in req.ProductInfo)
                    {
                        var product=await _context.Product.Where(x=>x.Id==item.ProductId).SingleOrDefaultAsync();
                        var skuProductQty = await _context.ProductSku.Where(x => x.ProductId == item.ProductId && x.SkuId == item.SkuId).FirstOrDefaultAsync();
                        if(!product.IsActive)
                        {
                             var issue=new ProductIssues(){
                                    ProductId=item.ProductId,
                                    ProductName=isZawgyi?Rabbit.Uni2Zg(product.Name):product.Name,
                                    Action="Delete",
                                    Qty=skuProductQty.Qty,
                                    Reason=string.Format("Your order item - {0} has been deleted by seller.",product.Name)
                                };
                                issueList.Add(issue);
                        }
                        
                        else if (skuProductQty != null)
                        {
                            if(item.Qty>skuProductQty.Qty){//Check if add to cart qty > stock qty. Can't make order
                               
                                var issue=new ProductIssues(){
                                    ProductId=item.ProductId,
                                    ProductName=isZawgyi?Rabbit.Uni2Zg(product.Name):product.Name,
                                     Action="OutOfStock",
                                    Qty=skuProductQty.Qty,
                                    Reason=string.Format("You'er order {0} of {1}, but we only have {2} left.",(item.Qty>1?item.Qty+" quantities" : item.Qty+" quantity" ),(product.Name),(skuProductQty.Qty>1?skuProductQty.Qty+" quantities" : skuProductQty.Qty+" quantity" ))
                                };
                                issueList.Add(issue);                                
                            }            
                        }                         
                    }
                    if(issueList.Count()>0)
                    {
                        response.OrderId = 0;
                        response.StatusCode=StatusCodes.Status400BadRequest;
                        response.ProductIssues=issueList;
                        return response;
                    }  

                    #endregion
                    
                    #region Save Order

                    var voucherNo = await _context.Order
                                    .OrderByDescending(o => o.Id)
                                    .Select(x => x.VoucherNo)
                                    .FirstOrDefaultAsync();
                    if(voucherNo==null){
                            voucherNo="0";
                    }
                    voucherNo = (int.Parse(voucherNo) + 1).ToString();
                    voucherNo = voucherNo.PadLeft(9, '0');
                        
                    var orderToAdd = new Order
                    {
                        OrderDate = DateTime.Now,
                        VoucherNo = voucherNo,
                        TotalAmt = req.TotalAmt,
                        NetAmt=req.NetAmt,
                        DeliveryFee=req.DeliveryFee,
                        CreatedDate = DateTime.Now,
                        CreatedBy = userId,
                        OrderStatusId = QueenOfDreamerConst.ORDER_STATUS_ORDER, // orderd
                        OrderUserId = userId,
                        PlatformId=platform
                    };             

                    await _context.Order.AddAsync(orderToAdd);
                    await _context.SaveChangesAsync();

                    #region  Activity Log
                    // try{
                    //     ActivityLog data=new ActivityLog(){
                    //         UserId=userId,
                    //         ActivityTypeId=QueenOfDreamerConst.ACTIVITY_TYPE_ORDER,
                    //         Value=voucherNo,
                    //         CreatedBy=userId,
                    //         CreatedDate=DateTime.Now,
                    //         PlatformId=platform
                    //     };
                    //     _context.ActivityLog.Add(data);
                    //     await _context.SaveChangesAsync();
                    // }
                    // catch(Exception ex)
                    // {
                    //     log.Error(string.Format("erorr=> {0}, inner exception=> {1}",ex.Message,ex.InnerException.Message));
                    // }
                    #endregion

                    //List<ReceivedMemberPointProductCategory> cateListPM=new List<ReceivedMemberPointProductCategory>();

                    foreach (var item in req.ProductInfo)
                    {
                        // var cID=await _context.Product.Where(x=>x.Id==item.ProductId).Select(x=>x.ProductCategoryId).SingleOrDefaultAsync();
                        
                        // var catePM= cateListPM.Where(x=>x.ProductCategoryId==cID).SingleOrDefault();

                        // if(catePM==null)
                        // {
                        //     var newCatePM=new ReceivedMemberPointProductCategory(){
                        //         ProductCategoryId=cID,
                        //         TotalAmount=item.Price * item.Qty
                        //     };
                        //     cateListPM.Add(newCatePM);
                        // }
                        // else{
                        //     catePM.TotalAmount+=item.Price * item.Qty;
                        // }

                        var productSku=await _context.ProductSku
                        .Where(x=>x.ProductId==item.ProductId
                        && x.SkuId==item.SkuId)
                        .SingleOrDefaultAsync();
                        
                         #region GetPromotion
                        var productPromote=_context.ProductPromotion.Where(x=>x.ProductId==item.ProductId).FirstOrDefault();
                        
                        double productPrice=0;
                        double productDiscount=0;
                        int promotePercent=0;
                        if(productPromote!=null && productPromote.Percent>0)
                        {
                            double discountPrice= double.Parse((((double)productPromote.Percent/(double)100)*(double)productSku.Price).ToString("0.00"));
                            productPrice=item.Price-discountPrice;
                            productDiscount=discountPrice;
                            promotePercent=productPromote.Percent;
                        }
                        else{
                            productPrice=productSku.Price;
                        }
                                        
                        #endregion

                        var orderDetailToAdd = new OrderDetail
                        {
                            OrderId = orderToAdd.Id,
                            ProductId = item.ProductId,
                            SkuId = item.SkuId,
                            Qty = item.Qty,
                            Price = productPrice,
                            OriginalPrice=productSku.Price,
                            Discount=productDiscount,
                            PromotePercent=promotePercent,
                            PromoteFixedPrice=0,
                        };
                        var skuProductQty = await _context.ProductSku.Where(x => x.ProductId == item.ProductId && x.SkuId == item.SkuId).FirstOrDefaultAsync();
                        if (skuProductQty != null)
                        {
                            skuProductQty.Qty = skuProductQty.Qty - item.Qty;
                            // await _context.SaveChangesAsync();
                        }
                        await _context.OrderDetail.AddAsync(orderDetailToAdd);
                    }

                    // save changes for order detail and product sku
                    await _context.SaveChangesAsync();

                //     ReceivedMemberPointRequest recPMReq=new ReceivedMemberPointRequest(){
                //     UserId=userId,
                //     VoucherNo=voucherNo,
                //     ProductCategory=cateListPM,
                //     ApplicationConfigId=QueenOfDreamerConst.APPLICATION_CONFIG_ID
                // };

                // await _memberPointRepo.ReceivedMemberPoint(recPMReq,token);

                    #endregion
                
                    #region Delivery Info
                    var orderDeliveryInfoToAdd = new OrderDeliveryInfo
                    {
                        OrderId = orderToAdd.Id,
                        Name =isZawgyi?Rabbit.Zg2Uni(req.DeliveryInfo.Name):req.DeliveryInfo.Name,
                        DeliveryServiceId = req.DeliveryInfo.SelectedDeliveryInfo.DeliveryServiceId,
                        DeliveryServiceName = req.DeliveryInfo.SelectedDeliveryInfo.DeliveryServiceName,
                        DeliveryServiceEmail = req.DeliveryInfo.SelectedDeliveryInfo.DeliveryServiceEmail,
                        DeliveryServicePhno = req.DeliveryInfo.SelectedDeliveryInfo.DeliveryServicePhno,
                        LogoPath = req.DeliveryInfo.SelectedDeliveryInfo.LogoPath,
                        DeliveryFee = req.DeliveryInfo.SelectedDeliveryInfo.DeliveryFee,
                        Address =isZawgyi?Rabbit.Zg2Uni(req.DeliveryInfo.Address):req.DeliveryInfo.Address,
                        PhNo = req.DeliveryInfo.PhoNo,
                        Remark =isZawgyi?Rabbit.Zg2Uni(req.DeliveryInfo.Remark):req.DeliveryInfo.Remark,
                        CityId = req.DeliveryInfo.CityId,
                        TownshipId = req.DeliveryInfo.TownshipId,
                        DeliveryDate = req.DeliveryInfo.DeliveryDate,
                        FromTime=req.DeliveryInfo.FromTime,
                        ToTime=req.DeliveryInfo.ToTime
                    };
                    await _context.OrderDeliveryInfo.AddAsync(orderDeliveryInfoToAdd);
                    #endregion

                    #region Remove TrnCartInfo 

                    var cartDataToRemove = await _context.TrnCart.Where(x => x.UserId == userId).ToListAsync();

                    _context.TrnCart.RemoveRange(cartDataToRemove);

                    var cartDeliveryInfoDataToRemove = await _context.TrnCartDeliveryInfo.Where(x => x.UserId == userId).ToListAsync();

                    _context.TrnCartDeliveryInfo.RemoveRange(cartDeliveryInfoDataToRemove);

                    await _context.SaveChangesAsync();

                    #endregion

                    #region Payment Info

                    if (req.PaymentInfo != null)
                    {
                        var path = "";
                        if (!String.IsNullOrEmpty(req.PaymentInfo.ApprovalImage.ApprovalImage))
                        {
                            var res =(await _services.UploadToS3(req.PaymentInfo.ApprovalImage.ApprovalImage
                            , req.PaymentInfo.ApprovalImage.ApprovalImageExtension, "order"));   
                            path = res.ImgPath;
                        }

                        var orderPaymentInfoToAdd = new OrderPaymentInfo
                        {
                            OrderId = orderToAdd.Id,
                            PaymentServiceId = req.PaymentInfo.PaymentServiceId,
                            TransactionDate = DateTime.Now,
                            PhoneNo = req.PaymentInfo.PhoNo,
                            Remark =isZawgyi?Rabbit.Zg2Uni(req.PaymentInfo.Remark):req.PaymentInfo.Remark,
                            ApprovalImgUrl = path,
                            PaymentStatusId = 0
                        };

                        // if payment is bank
                        if(req.PaymentInfo.PaymentServiceId==QueenOfDreamerConst.PAYMENT_SERVICE_BANK)  //if pay by bank, we will add bankID in payment service info
                        {
                            orderPaymentInfoToAdd.BankId=req.PaymentInfo.BankId;
                            orderPaymentInfoToAdd.PaymentStatusId=QueenOfDreamerConst.PAYMENT_STATUS_CHECK;
                        }

                        //if payment is COD
                        else if (req.PaymentInfo.PaymentServiceId == QueenOfDreamerConst.PAYMENT_SERVICE_COD)
                        {
                            orderPaymentInfoToAdd.PaymentStatusId=QueenOfDreamerConst.PAYMENT_STATUS_SUCCESS;                            
                        }
                        
                        //if payment is PaymentGate way
                        else if (req.PaymentInfo.PaymentServiceId == QueenOfDreamerConst.PAYMENT_SERVICE_WAVE_MONEY // Wave Money
                        || req.PaymentInfo.PaymentServiceId == QueenOfDreamerConst.PAYMENT_SERVICE_KPAY // KBZPay
                        || req.PaymentInfo.PaymentServiceId == QueenOfDreamerConst.PAYMENT_SERVICE_OK_DOLLAR // OK Dollar
                        || req.PaymentInfo.PaymentServiceId == QueenOfDreamerConst.PAYMENT_SERVICE_MASTER // Master
                        || req.PaymentInfo.PaymentServiceId == QueenOfDreamerConst.PAYMENT_SERVICE_VISA // Visa
                        || req.PaymentInfo.PaymentServiceId == QueenOfDreamerConst.PAYMENT_SERVICE_MYTEL_PAY // My Tel
                        || req.PaymentInfo.PaymentServiceId == QueenOfDreamerConst.PAYMENT_SERVICE_SAISAI_PAY // Sai Sai
                        || req.PaymentInfo.PaymentServiceId == QueenOfDreamerConst.PAYMENT_SERVICE_CB_PAY // CB
                        || req.PaymentInfo.PaymentServiceId == QueenOfDreamerConst.PAYMENT_SERVICE_AYA_PAY // AYA
                        || req.PaymentInfo.PaymentServiceId == QueenOfDreamerConst.PAYMENT_SERVICE_ONE_PAY // ONE Pay                        
                        )
                        {
                            orderPaymentInfoToAdd.PaymentStatusId=QueenOfDreamerConst.PAYMENT_STATUS_SUCCESS;                            
                        }

                        //if payment is PaySlip
                        else if(req.PaymentInfo.PaymentServiceId == QueenOfDreamerConst.PAYMENT_SERVICE_WAVE_MONEY_MANUAL // Wave Money
                        || req.PaymentInfo.PaymentServiceId == QueenOfDreamerConst.PAYMENT_SERVICE_KPAY_MANUAL // KBZPay
                        || req.PaymentInfo.PaymentServiceId == QueenOfDreamerConst.PAYMENT_SERVICE_OK_DOLLAR_MANUAL // OK Dollar
                        || req.PaymentInfo.PaymentServiceId == QueenOfDreamerConst.PAYMENT_SERVICE_MASTER_MANUAL // Master
                        || req.PaymentInfo.PaymentServiceId == QueenOfDreamerConst.PAYMENT_SERVICE_VISA_MANUAL // Visa
                        || req.PaymentInfo.PaymentServiceId == QueenOfDreamerConst.PAYMENT_SERVICE_MYTEL_PAY_MANUAL // My Tel
                        || req.PaymentInfo.PaymentServiceId == QueenOfDreamerConst.PAYMENT_SERVICE_SAISAI_PAY_MANUAL // Sai Sai
                        || req.PaymentInfo.PaymentServiceId == QueenOfDreamerConst.PAYMENT_SERVICE_CB_PAY_MANUAL // CB
                        || req.PaymentInfo.PaymentServiceId == QueenOfDreamerConst.PAYMENT_SERVICE_AYA_PAY_MANUAL // AYA
                        || req.PaymentInfo.PaymentServiceId == QueenOfDreamerConst.PAYMENT_SERVICE_ONE_PAY_MANUAL // ONE Pay
                        )
                        {
                            orderPaymentInfoToAdd.PaymentStatusId=QueenOfDreamerConst.PAYMENT_STATUS_CHECK;
                        }

                        await _context.OrderPaymentInfo.AddAsync(orderPaymentInfoToAdd);                       
                        await _context.SaveChangesAsync();
                    }
                    #endregion

                    #region Noti

                    // var sellerList = await _userServices.GetAllSellerUserId(token);
                    // NotificationTemplate notiTemplate = await _context.NotificationTemplate
                    // .Where(a => a.ActionName == "Order").SingleOrDefaultAsync(); 
                    // foreach(var seller in sellerList)
                    // { 
                    //     var body = notiTemplate.Body.Replace("{userName}", orderDeliveryInfoToAdd.Name);
                        
                    //     Models.Notification notification = new Models.Notification();
                    //     notification.Title = notiTemplate.Title;
                    //     notification.Body = userId.ToString() + " မှ အော်ဒါမှာယူခဲ့သည်";
                    //     notification.UserId = seller.Id; //userId;
                    //     notification.ImgUrl = QueenOfDreamerConst.AWS_USER_PROFILE_PATH + userId + ".png";
                    //     notification.RedirectAction = QueenOfDreamerConst.NOTI_REDIRECT_ACTION_ORDER_DETAIL;
                    //     notification.ReferenceAttribute = orderToAdd.Id.ToString();
                    //     notification.CreatedDate = DateTime.Now;
                    //     notification.CreatedBy = orderToAdd.OrderUserId;
                    //     await _context.Notification.AddAsync(notification);
                    //     await _context.SaveChangesAsync();                            
                    //     var test = Helpers.Notification.SendFCMNotification(seller.Id.ToString(),
                    //                                                     notiTemplate.Title,
                    //                                                     body, seller.Id,
                    //                                                     QueenOfDreamerConst.NOTI_REDIRECT_ACTION_ORDER_DETAIL,
                    //                                                     orderToAdd.Id,
                    //                                                     notification.Id,true);  // true for sending noti to seller
                        
                    // }  
                                 
                #endregion

                    await transaction.CommitAsync();  
                    response.OrderId = orderToAdd.Id;
                    response.StatusCode = StatusCodes.Status200OK;
                    return response;  
                
                }
                
                catch (Exception e)
                {
                    log.Error(e.Message);
                    transaction.Rollback();
                    return null;
                }           
            }
        }
        public async Task<ResponseStatus> PostOrderActivity(int orderId, int userId,string token,int platform)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                   var response=new ResponseStatus();  

                    var order=await _context.Order
                    .Where(x=>x.Id==orderId)
                    .SingleOrDefaultAsync();

                    if(order==null)
                    {
                        response.StatusCode=StatusCodes.Status400BadRequest;
                        response.Message="Order is not found!";
                        return response;
                    }
                   
                    #region  Activity Log
                    try{
                        ActivityLog data=new ActivityLog(){
                            UserId=userId,
                            ActivityTypeId=QueenOfDreamerConst.ACTIVITY_TYPE_ORDER,
                            Value=order.VoucherNo,
                            CreatedBy=userId,
                            CreatedDate=DateTime.Now,
                            PlatformId=platform
                        };
                        _context.ActivityLog.Add(data);
                        await _context.SaveChangesAsync();
                    }
                    catch(Exception ex)
                    {
                        log.Error(string.Format("erorr=> {0}, inner exception=> {1}",ex.Message,ex.InnerException.Message));
                    }
                    #endregion

                    #region  MemberPoint
                    var orderDetail=await _context.OrderDetail
                    .Where(x=>x.OrderId==orderId)
                    .ToListAsync();

                    List<ReceivedMemberPointProductCategory> cateListPM=new List<ReceivedMemberPointProductCategory>();

                    foreach (var item in orderDetail)
                    {
                        var cID=await _context.Product.Where(x=>x.Id==item.ProductId).Select(x=>x.ProductCategoryId).SingleOrDefaultAsync();
                        
                        var catePM= cateListPM.Where(x=>x.ProductCategoryId==cID).SingleOrDefault();

                        if(catePM==null)
                        {
                            var newCatePM=new ReceivedMemberPointProductCategory(){
                                ProductCategoryId=cID,
                                TotalAmount=item.Price * item.Qty
                            };
                            cateListPM.Add(newCatePM);
                        }
                        else{
                            catePM.TotalAmount+=item.Price * item.Qty;
                        }                       
                    }

                    ReceivedMemberPointRequest recPMReq=new ReceivedMemberPointRequest(){
                    UserId=userId,
                    VoucherNo=order.VoucherNo,
                    ProductCategory=cateListPM,
                    ApplicationConfigId=QueenOfDreamerConst.APPLICATION_CONFIG_ID
                    };

                    await _memberPointRepo.ReceivedMemberPoint(recPMReq,token);
                    #endregion
                   
                    #region Noti

                    var orderDeliveryInfo=await _context.OrderDeliveryInfo
                    .Where(x=>x.OrderId==orderId)
                    .SingleOrDefaultAsync();

                    var sellerList = await _userServices.GetAllSellerUserId(token);
                    NotificationTemplate notiTemplate = await _context.NotificationTemplate
                    .Where(a => a.ActionName == "Order").SingleOrDefaultAsync(); 
                    foreach(var seller in sellerList)
                    { 
                        var body = notiTemplate.Body.Replace("{userName}", orderDeliveryInfo==null?"":orderDeliveryInfo.Name);
                        
                        Models.Notification notification = new Models.Notification();
                        notification.Title = notiTemplate.Title;
                        notification.Body = userId.ToString() + " မှ အော်ဒါမှာယူခဲ့သည်";
                        notification.UserId = seller.Id; //userId;
                        notification.ImgUrl = QueenOfDreamerConst.AWS_USER_PROFILE_PATH + userId + ".png";
                        notification.RedirectAction = QueenOfDreamerConst.NOTI_REDIRECT_ACTION_ORDER_DETAIL;
                        notification.ReferenceAttribute = orderId.ToString();
                        notification.CreatedDate = DateTime.Now;
                        notification.CreatedBy = userId;
                        await _context.Notification.AddAsync(notification);
                        await _context.SaveChangesAsync();                            
                        var test = Helpers.Notification.SendFCMNotification(seller.Id.ToString(),
                                                                        notiTemplate.Title,
                                                                        body, seller.Id,
                                                                        QueenOfDreamerConst.NOTI_REDIRECT_ACTION_ORDER_DETAIL,
                                                                        orderId,
                                                                        notification.Id,true);  // true for sending noti to seller
                        
                    }  
                            
                #endregion
                 await transaction.CommitAsync(); 
                    response.StatusCode = StatusCodes.Status200OK;
                    response.Message="Success";
                    return response;      
                
                }
                
                catch (Exception e)
                {
                    log.Error(e.Message);
                    transaction.Rollback();
                    return null;
                }           
            }
        }
        
        public async Task<ResponseStatus> UpdateProductCart(UpdateProductCartRequest request, int userId)
        {
            foreach (var trnCart in request.ProductCarts)
            {
                var trnCartRes = await _context.TrnCart.Where(x => x.UserId == userId && x.SkuId == trnCart.SkuId && x.ProductId == trnCart.ProductId).FirstOrDefaultAsync();
                if (trnCartRes != null)
                {
                    trnCartRes.Qty = trnCart.Qty;
                    await _context.SaveChangesAsync();
                }
            }
            return new ResponseStatus(){StatusCode=StatusCodes.Status200OK,Message="အောင်မြင်သည်။"};
        }

        public async Task<List<GetOrderHistoryResponse>> GetOrderHistory(GetOrderHistoryRequest request)
        {
            bool isZawgyi=Rabbit.IsZawgyi(_httpContextAccessor);

            var orderDate=Convert.ToDateTime(request.OrderDate).Date;
            var paymentDate=Convert.ToDateTime(request.PaymentDate).Date;

            List<Order> orderList= await (  
                                            (   from ord in _context.Order
                                                join pay in _context.OrderPaymentInfo  on ord.Id equals pay.OrderId
                                                where ord.OrderUserId == request.UserId
                                                && (string.IsNullOrEmpty(request.VoucherNo) || ord.VoucherNo.Contains(request.VoucherNo))
                                                && (request.PaymentStatusId==0 || ord.OrderPaymentInfo.Where(p=>p.OrderId==ord.Id).OrderByDescending(p=>p.TransactionDate).FirstOrDefault().PaymentStatusId==request.PaymentStatusId)
                                                && (request.OrderStatusId==0 || ord.OrderStatusId == request.OrderStatusId)
                                                && ((orderDate.ToString("dd-MMM-yy") == "01-Jan-01") || ord.OrderDate.Date == orderDate.Date)
                                                && ((paymentDate.ToString("dd-MMM-yy") == "01-Jan-01") || pay.TransactionDate.Date == paymentDate.Date)
                                                orderby ord.OrderDate descending
                                                select ord
                                            ).Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize)   
                                        ).ToListAsync();
                                        
            orderList =  orderList.GroupBy(x =>x.Id).Select(g => g.First()).ToList();

            List<GetOrderHistoryResponse> response = new List<GetOrderHistoryResponse>();           

            if (orderList.Count > 0)
            {
                foreach (var orderDetails in orderList)
                {
                    var orderDetailList = await _context.OrderDetail.Where(x => x.OrderId == orderDetails.Id).ToListAsync();
                    if (orderDetailList.Count > 0)
                    {
                        GetOrderHistoryResponse orderHistory = new GetOrderHistoryResponse();
                        orderHistory.OrderId = orderDetails.Id;
                        orderHistory.OrderDate = orderDetails.OrderDate;
                        orderHistory.CreatedDate = orderDetails.CreatedDate;
                        orderHistory.VoucherNo = orderDetails.VoucherNo;

                        var paymentInfo = await _context.OrderPaymentInfo.Where(p => p.OrderId == orderDetails.Id)
                                    .Select(itm => new
                                    {
                                        TransactionDate = itm.TransactionDate,
                                        PaymentStatusId = itm.PaymentStatus.Id,
                                        PaymentStatus = itm.PaymentStatus.Name,
                                        PaymentServiceName =isZawgyi?Rabbit.Uni2Zg(itm.PaymentService.Name):itm.PaymentService.Name,
                                        PaymentServiceImgPath = itm.PaymentService.ImgPath
                                    }).OrderByDescending(p => p.TransactionDate).FirstOrDefaultAsync();
                        if (paymentInfo != null)
                        {
                            orderHistory.PaymentDate = paymentInfo.TransactionDate;
                            orderHistory.PaymentStatusId = paymentInfo.PaymentStatusId;
                            orderHistory.PaymentStatusName = paymentInfo.PaymentStatus;
                            orderHistory.PaymentServiceImgPath = paymentInfo.PaymentServiceImgPath;
                        }
                        else
                        {
                            orderHistory.PaymentStatusName = "";
                            orderHistory.PaymentServiceImgPath = "";
                        }
                        
                        var orderStatus = await _context.OrderStatus.Where(x => x.Id == orderDetails.OrderStatusId).FirstOrDefaultAsync();
                        if (orderStatus != null)
                        {
                            orderHistory.OrderStatus =isZawgyi?Rabbit.Uni2Zg(orderStatus.Name):orderStatus.Name;
                        }
                        else
                        {
                            orderHistory.OrderStatus = "";
                        }
                        orderHistory.OrderStatusId = orderDetails.OrderStatusId;
                        int items = 0;
                      
                        foreach (var orderDetailRes in orderDetailList)
                        {
                            items += orderDetailRes.Qty;
                            var productRes = await _context.Product.Where(x => x.Id == orderDetailRes.ProductId).FirstOrDefaultAsync();
                            if (productRes != null)
                            {
                                var productImageRes = await _context.ProductImage.Where(x => x.isMain == true && x.ProductId == productRes.Id).FirstOrDefaultAsync();
                                if (productImageRes != null)
                                {
                                    orderHistory.ProductUrl = productImageRes.Url;
                                }
                                else
                                {
                                    orderHistory.ProductUrl = "";
                                }
                            }
                        }
                        orderHistory.Qty = items;
                        orderHistory.Price = orderDetails.NetAmt;
                        response.Add(orderHistory);
                    }
                }
            }     
            return response;
        }

        public async Task<List<GetOrderHistoryResponse>> GetOrderHistorySeller(GetOrderHistorySellerRequest request)
        {
            bool isZawgyi=Rabbit.IsZawgyi(_httpContextAccessor);

            var orderDate=Convert.ToDateTime(request.OrderDate).Date;
            var paymentDate=Convert.ToDateTime(request.PaymentDate).Date;

             List<Order> orderList=await _context.Order.Include(a=>a.OrderPaymentInfo)
                    .Where(a=>(string.IsNullOrEmpty(request.VoucherNo) || a.VoucherNo.Contains(request.VoucherNo))
                    && (request.PaymentStatusId==0 || a.OrderPaymentInfo.Where(p=>p.OrderId==a.Id).OrderByDescending(p=>p.TransactionDate).FirstOrDefault().PaymentStatusId==request.PaymentStatusId)
                    && (request.OrderStatusId==0 || a.OrderStatusId==request.OrderStatusId)
                    && ((orderDate.ToString("dd-MMM-yy") == "01-Jan-01")|| a.OrderDate.Date == orderDate)
                    && ((paymentDate.ToString("dd-MMM-yy") == "01-Jan-01") || a.OrderPaymentInfo.Where(p=>p.OrderId==a.Id).OrderByDescending(p=>p.TransactionDate).FirstOrDefault().TransactionDate.Date == paymentDate))
                    .OrderByDescending(a=>a.OrderDate).Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize)                    
                    .ToListAsync();

           
            List<GetOrderHistoryResponse> response = new List<GetOrderHistoryResponse>();           
            if (orderList.Count > 0)
            {
                foreach (var orders in orderList)
                {
                    var orderDetailList = await _context.OrderDetail.Where(x => x.OrderId == orders.Id).ToListAsync();
                    if (orderDetailList.Count > 0)
                    {
                        GetOrderHistoryResponse orderHistory = new GetOrderHistoryResponse();
                        orderHistory.OrderId = orders.Id;
                        orderHistory.OrderDate = orders.OrderDate;
                        orderHistory.VoucherNo = orders.VoucherNo;
                        orderHistory.CreatedDate = orders.CreatedDate;                        
                        var paymentInfo = await _context.OrderPaymentInfo.Where(p => p.OrderId == orders.Id)
                                    .Select(itm => new
                                    {
                                        TransactionDate = itm.TransactionDate,
                                        PaymentStatusId = itm.PaymentStatus.Id,
                                        PaymentStatus = itm.PaymentStatus.Name,
                                        PaymentServiceName =isZawgyi?Rabbit.Uni2Zg(itm.PaymentService.Name):itm.PaymentService.Name,
                                        PaymentServiceImgPath = itm.PaymentService.ImgPath
                                    }).OrderByDescending(p => p.TransactionDate).FirstOrDefaultAsync();
                        if (paymentInfo != null)
                        {
                            orderHistory.PaymentDate = paymentInfo.TransactionDate;
                            orderHistory.PaymentStatusId = paymentInfo.PaymentStatusId;
                            orderHistory.PaymentStatusName = paymentInfo.PaymentStatus;
                            orderHistory.PaymentServiceImgPath = paymentInfo.PaymentServiceImgPath;
                        }
                        else
                        {
                            orderHistory.PaymentStatusName = "";
                            orderHistory.PaymentServiceImgPath = "";
                        }

                        var orderStatus = await _context.OrderStatus.Where(x => x.Id == orders.OrderStatusId).FirstOrDefaultAsync();
                        if (orderStatus != null)
                        {
                            orderHistory.OrderStatus =isZawgyi?Rabbit.Uni2Zg(orderStatus.Name):orderStatus.Name;
                        }
                        else
                        {
                            orderHistory.OrderStatus = "";
                        }
                        orderHistory.OrderStatusId = orders.OrderStatusId;
                        int items = 0;

                        foreach (var orderDetailRes in orderDetailList)
                        {
                            items += orderDetailRes.Qty;
                            var productRes = await _context.Product.Where(x => x.Id == orderDetailRes.ProductId).FirstOrDefaultAsync();
                            if (productRes != null)
                            {
                                var productImageRes = await _context.ProductImage.Where(x => x.isMain == true && x.ProductId == productRes.Id).FirstOrDefaultAsync();
                                if (productImageRes != null)
                                {
                                    orderHistory.ProductUrl = productImageRes.Url;
                                }
                                else
                                {
                                    orderHistory.ProductUrl = "";
                                }
                            }
                        }
                        orderHistory.Qty = items;
                        orderHistory.Price = orders.NetAmt;
                        response.Add(orderHistory);
                    }
                }
            }
            return response;
        }

        public async Task<List<GetNotificationResponse>> GetNotificationBuyer(GetNotificationRequest request, int userId, string token)
        {
            bool isZawgyi=Rabbit.IsZawgyi(_httpContextAccessor);

             List<GetNotificationResponse> response = new List<GetNotificationResponse>();
            var notificationList = await _context.Notification.Where(x => x.UserId == userId).ToListAsync();
            int notificationCount = 0;
            if (request.Date == null)
            {
                var notificationRes = await _context.Notification.Where(x => x.UserId == userId && x.IsSeen == false).ToListAsync();
                notificationCount = notificationRes.Count;
            }
            else
            {
                var notificationRes = await _context.Notification.Where(x => x.UserId == userId && x.CreatedDate >= request.Date).ToListAsync();
                notificationCount = notificationRes.Count;
            }

            if (notificationList.Count > 0)
            {
                foreach (var notificationDetails in notificationList)
                {
                    GetNotificationResponse notification = new GetNotificationResponse();
                    notification.Id = notificationDetails.Id;
                    notification.Body =isZawgyi?Rabbit.Uni2Zg(notificationDetails.Body):notificationDetails.Body;
                    notification.Url = notificationDetails.ImgUrl;
                    notification.RedirectAction = notificationDetails.RedirectAction;
                    notification.ReferenceAttribute = notificationDetails.ReferenceAttribute;
                    notification.NotificationDate = notificationDetails.CreatedDate;
                    notification.Count = notificationCount;
                    notification.IsSeen = notificationDetails.IsSeen;
                    response.Add(notification);
                }
            }
            response = response.OrderByDescending(x => x.NotificationDate).ToList();
            PagedList<GetNotificationResponse> orderPageList = await PagedList<GetNotificationResponse>.Create(response, request.PageNumber, request.PageSize);
            response = _mapper.Map<List<GetNotificationResponse>>(orderPageList);
            return response;
        }

        public async Task<List<GetNotificationResponse>> GetNotificationSeller(GetNotificationRequest request, int userId, string token)
        {
            bool isZawgyi=Rabbit.IsZawgyi(_httpContextAccessor);

             List<GetNotificationResponse> response = new List<GetNotificationResponse>();
            var notificationList = await _context.Notification.Where(x => x.UserId == 0 || x.UserId == userId).ToListAsync();
           
            int notificationCount = 0;
            if (request.Date == null)
            {
                var notificationRes = await _context.Notification.Where(x => x.UserId == userId && x.IsSeen == false).ToListAsync();
                notificationCount = notificationRes.Count;
            }
            else
            {
                var notificationRes = await _context.Notification.Where(x => x.UserId == userId && x.CreatedDate >= request.Date).ToListAsync();
                notificationCount = notificationRes.Count;
            }

            if (notificationList.Count > 0)
            {
                foreach (var notificationDetails in notificationList)
                {

                    if (notificationDetails.Body.Contains("မှ အော်ဒါမှာယူခဲ့သည်"))
                    {                

                        string userStr = new String(notificationDetails.Body.Where(Char.IsDigit).ToArray());

                        int userIdInt = Int32.Parse(userStr);
                        var userInfo = await _userServices.GetUserInfo(userIdInt, token);
                        GetNotificationResponse notification = new GetNotificationResponse();
                        notification.Id = notificationDetails.Id;
                        if(userInfo!=null)
                        {  
                             notification.Body =userInfo.Name + " မှ အော်ဒါမှာယူခဲ့သည်";
                             notification.Body=isZawgyi?Rabbit.Uni2Zg(notification.Body):notification.Body;
                        }
                        notification.Url = notificationDetails.ImgUrl;
                        notification.RedirectAction = notificationDetails.RedirectAction;
                        notification.ReferenceAttribute = notificationDetails.ReferenceAttribute;
                        notification.NotificationDate = notificationDetails.CreatedDate;
                        notification.Count = notificationCount;
                        notification.IsSeen = notificationDetails.IsSeen;
                        response.Add(notification);
                    }
                    else if(notificationDetails.Title=="PaymentAgain")
                    {
                        string userStr =notificationDetails.Body.Split(" ")[0];

                        int userIdInt = Int32.Parse(userStr);
                        var userInfo = await _userServices.GetUserInfo(userIdInt, token);
                        GetNotificationResponse notification = new GetNotificationResponse();
                        notification.Id = notificationDetails.Id;
                        if(userInfo!=null)
                        {  
                             notification.Body =notificationDetails.Body.Replace(userStr,userInfo.Name);
                             notification.Body=isZawgyi?Rabbit.Uni2Zg(notification.Body):notification.Body;
                        }
                        notification.Url = notificationDetails.ImgUrl;
                        notification.RedirectAction = notificationDetails.RedirectAction;
                        notification.ReferenceAttribute = notificationDetails.ReferenceAttribute;
                        notification.NotificationDate = notificationDetails.CreatedDate;
                        notification.Count = notificationCount;
                        notification.IsSeen = notificationDetails.IsSeen;
                        response.Add(notification);
                    }
                    else
                    {
                        GetNotificationResponse notification = new GetNotificationResponse();
                        notification.Id = notificationDetails.Id;
                        notification.Body =isZawgyi?Rabbit.Uni2Zg(notificationDetails.Body):notificationDetails.Body;
                        notification.Url = notificationDetails.ImgUrl;
                        notification.RedirectAction = notificationDetails.RedirectAction;
                        notification.ReferenceAttribute = notificationDetails.ReferenceAttribute;
                        notification.NotificationDate = notificationDetails.CreatedDate;
                        notification.Count = notificationCount;
                        notification.IsSeen = notificationDetails.IsSeen;
                        response.Add(notification);
                    }
                }
            }
            response = response.OrderByDescending(x => x.NotificationDate).ToList();
            PagedList<GetNotificationResponse> orderPageList = await PagedList<GetNotificationResponse>.Create(response, request.PageNumber, request.PageSize);
            response = _mapper.Map<List<GetNotificationResponse>>(orderPageList);
            return response;
        }

        public async Task<ResponseStatus> SeenNotification(SeenNotificationRequest request, int userId)
        {
            ResponseStatus response = new ResponseStatus();
            var order = await _context.Notification.Where(x => x.Id == request.Id).FirstOrDefaultAsync();
            if (order != null)
            {
                if (order.IsSeen == true)
                {
                    response.StatusCode = StatusCodes.Status200OK;
                    response.Message = "မြင်ပြီးပါပြီ။";
                }
                else
                {
                    order.IsSeen = true;
                    await _context.SaveChangesAsync();
                    response.StatusCode = StatusCodes.Status200OK;
                    response.Message = "အောင်မြင်သည်။";
                }
            }
            
            return response;
        }

        public async Task<List<GetOrderListByProductResponse>> GetOrderListByProduct(GetOrderListByProductRequest request)
        {
            bool isZawgyi=Rabbit.IsZawgyi(_httpContextAccessor);

            request.ProductName=isZawgyi?Rabbit.Zg2Uni(request.ProductName):request.ProductName;

            List<GetOrderListByProductResponse> response = new List<GetOrderListByProductResponse>();
            List<OrderProductResponse> orderProductResponses = new List<OrderProductResponse>();
            var productAndOrder = (from p in _context.Product
                                  join o in _context.OrderDetail
                                  on p.Id equals o.ProductId
                                  where (string.IsNullOrEmpty(request.ProductName) 
                                  || p.Name.Contains(request.ProductName))
                                  group new { p, o } by new { p.Id, o.ProductId, p.Name }
                                    into grp
                                  select new
                                  {
                                      ProductId = grp.Key.Id,
                                      OrderCount = grp.Count(),
                                      TotalQty = grp.Sum(f => f.o.Qty),
                                      Name = grp.Key.Name
                                  }).Skip((request.PageNumber-1)*request.PageSize).Take(request.PageSize);
            if (productAndOrder != null)
            {
                foreach (var product in productAndOrder)
                {
                    OrderProductResponse OrderProduct = new OrderProductResponse();
                    OrderProduct.ProductName =isZawgyi?Rabbit.Uni2Zg(product.Name):product.Name;
                    OrderProduct.OrderCount = product.OrderCount;
                    OrderProduct.TotalQty = product.TotalQty;
                    OrderProduct.ProductId = product.ProductId;
                    orderProductResponses.Add(OrderProduct);
                }
            }
            
            foreach (var orderDetails in orderProductResponses)
            {
                GetOrderListByProductResponse orderListProduct = new GetOrderListByProductResponse();
                orderListProduct.ProductName = orderDetails.ProductName;
                // orderListProduct.OrderCount = _context.OrderDetail
                //                         .Where(x=>x.ProductId==orderDetails.ProductId)
                //                         .Sum(x=>x.Qty);
                orderListProduct.OrderCount=orderDetails.OrderCount;
                orderListProduct.TotalQty = _context.ProductSku
                                        .Where(x=>x.ProductId==orderDetails.ProductId)
                                        .Sum(x=>x.Qty);
                orderListProduct.ProductId = orderDetails.ProductId;
                var ordDetails = await _context.OrderDetail.Where(x => x.ProductId == orderDetails.ProductId).ToListAsync();

                var resp = await _context.Order.Where(x => ordDetails.Select(y => y.OrderId).Contains(x.Id)).ToListAsync();
                
                List<string> userImg=new List<string>();
                orderListProduct.UserImage= resp.Select(x=>QueenOfDreamerConst.AWS_USER_PROFILE_PATH + x.OrderUserId + ".png").ToList<string>();

                var productImage = await _context.ProductImage.Where(x => x.ProductId == orderDetails.ProductId && x.isMain == true).FirstOrDefaultAsync();
                if (productImage != null)
                {
                    orderListProduct.ProductUrl = productImage.Url;
                }
                else
                {
                    orderListProduct.ProductUrl = "";
                }
                var productPrice = await _context.ProductPrice.Where(x => x.ProductId == orderDetails.ProductId).FirstOrDefaultAsync();
                if (productPrice != null)
                {
                    orderListProduct.OriginalPrice = productPrice.Price;
                }
                else
                {
                    orderListProduct.OriginalPrice = 0;
                }
                var promotePrice = await _context.ProductPromotion.Where(x => x.ProductId == orderDetails.ProductId).FirstOrDefaultAsync();
                if (promotePrice != null)
                {
                    orderListProduct.PromotePrice = promotePrice.TotalAmt;
                    orderListProduct.PromotePercent=promotePrice.Percent;
                }
                else
                {
                    orderListProduct.PromotePrice = 0;
                    orderListProduct.PromotePercent=0;
                }
                // var orderDetailSkuList = await _context.OrderDetail.Where(x => x.ProductId == orderDetails.ProductId).ToListAsync();
                // if (orderDetailSkuList.Count > 0)
                // {
                //     var firstOrderDate = DateTime.MinValue;
                //     var secondOrderDate = DateTime.MinValue;
                //     foreach (var sku in orderDetailSkuList)
                //     {
                //         var order = await _context.Order.Where(x => x.Id == sku.OrderId).FirstOrDefaultAsync();
                //         firstOrderDate = order.OrderDate;
                //         if (secondOrderDate < firstOrderDate)
                //         {
                //             secondOrderDate = firstOrderDate;
                //             firstOrderDate = DateTime.MinValue;
                //         }
                //         orderListProduct.OrderDate = secondOrderDate;
                //         var skuValue = await (from psku in _context.ProductSkuValue
                //                               from pvopt in _context.ProductVariantOption
                //                               where psku.ProductId == sku.ProductId
                //                               && psku.SkuId == sku.SkuId
                //                               && psku.ProductId == pvopt.ProductId
                //                               && psku.VariantId == pvopt.VariantId
                //                               && psku.ValueId == pvopt.ValueId
                //                               select pvopt.ValueName).ToListAsync();
                //         orderListProduct.SkuValue += string.Join(",", skuValue) + " ";                        
                //     }
                // }
                response.Add(orderListProduct);
            }
            response = response.OrderByDescending(x => x.OrderDate).ToList();
            
            return response;
        }

        public async Task<GetOrderListByProductIdResponse> GetOrderListByProductId(GetOrderListByProductIdRequest request)
        {
            // var productDetails = await _context.OrderDetail
            //                       .Join(_context.Product, ord => ord.ProductId, pro => pro.Id, (ord, pro) => new { ord, pro })
            //                       .Join(_context.ProductImage, pImg => pImg.pro.Id, bb => bb.ProductId, (pImg, bb) => new { pImg, bb })
            //                       .Where(x => x.pImg.ord.ProductId == request.ProductId && x.bb.isMain == true)
            //                       .Select(z => new
            //                       {
            //                           ProductId = z.pImg.pro.Id,
            //                           ProductName = z.pImg.pro.Name,
            //                           OrderId = z.pImg.ord.OrderId,
            //                           Price = z.pImg.ord.Price,
            //                           SkuId = z.pImg.ord.SkuId,
            //                           TotalQty = z.pImg.ord.Qty,
            //                           ProductUrl = z.pImg.pro.ProductImage.Where(x => x.isMain == true).FirstOrDefault()

            //                       }).ToListAsync();

            bool isZawgyi=Rabbit.IsZawgyi(_httpContextAccessor);

            var productDetails = await (from ordDtl in _context.OrderDetail
                                        join prd in _context.Product on ordDtl.ProductId equals prd.Id
                                        where ordDtl.ProductId == request.ProductId
                                        select new {
                                            ProductId = prd.Id,
                                            ProductName =isZawgyi?Rabbit.Uni2Zg(prd.Name):prd.Name,
                                            OrderId = ordDtl.OrderId,
                                            Price = ordDtl.Price,
                                            SkuId = ordDtl.SkuId,
                                            TotalQty = ordDtl.Qty,
                                            ProductUrl = _context.ProductImage.Where(p => p.ProductId == prd.Id && p.isMain == true).FirstOrDefault()
                                        }
                                        ).ToListAsync();

            if (productDetails.Count > 0)
            {
                GetOrderListByProductIdResponse orderListByProduct = new GetOrderListByProductIdResponse();
                List<UserResponse> userOrderList = new List<UserResponse>();
                var productResponse = productDetails.FirstOrDefault();
                orderListByProduct.ProductId = productResponse.ProductId;
                orderListByProduct.ProductName = productResponse.ProductName;
                orderListByProduct.Price = productResponse.Price;
                orderListByProduct.ProductUrl = productResponse.ProductUrl.Url;
                orderListByProduct.OrderCount = productDetails.Count;
                orderListByProduct.TotalQty = _context.ProductSku
                                        .Where(x=>x.ProductId==request.ProductId)
                                        .Sum(x=>x.Qty);

                var sku = await (from psku in _context.ProductSkuValue
                                 from pvopt in _context.ProductVariantOption
                                 where psku.ProductId == productResponse.ProductId
                                 && psku.SkuId == productResponse.SkuId
                                 && psku.ProductId == pvopt.ProductId
                                 && psku.VariantId == pvopt.VariantId
                                 && psku.ValueId == pvopt.ValueId
                                 select isZawgyi?Rabbit.Uni2Zg(pvopt.ValueName):pvopt.ValueName).ToListAsync();
                orderListByProduct.SkuValue += string.Join(",", sku) + " ";

                foreach (var orderDetail in productDetails)
                {
                    // orderListByProduct.TotalQty += orderDetail.TotalQty;
                    UserResponse userOrder = new UserResponse();
                    var orderRes = await _context.Order
                                    .Join(_context.OrderDeliveryInfo, ord => ord.Id, ordDeli => ordDeli.OrderId, (ord, ordDeli) => new { ord, ordDeli })
                                    .Where(x => x.ord.Id == orderDetail.OrderId)
                                    .Select(z => new
                                    {
                                        OrderStatusId = z.ord.OrderStatusId,
                                        UserName =isZawgyi?Rabbit.Uni2Zg(z.ordDeli.Name):z.ordDeli.Name,
                                        UserId = z.ord.OrderUserId,
                                        OrderDate = z.ord.OrderDate,
                                        CreatedDate = z.ord.CreatedDate
                                    }).FirstOrDefaultAsync();
                    var skuValue = await (from psku in _context.ProductSkuValue
                                          from pvopt in _context.ProductVariantOption
                                          where psku.ProductId == orderDetail.ProductId
                                          && psku.SkuId == orderDetail.SkuId
                                          && psku.ProductId == pvopt.ProductId
                                          && psku.VariantId == pvopt.VariantId
                                          && psku.ValueId == pvopt.ValueId
                                          select isZawgyi?Rabbit.Uni2Zg(pvopt.ValueName):pvopt.ValueName).ToListAsync();

                    userOrder.Sku += string.Join(",", skuValue) + " ";
                    if (orderRes != null)
                    {
                        userOrder.OrderStatusId = orderRes.OrderStatusId;
                        var orderStautsRes = await _context.OrderStatus.Where(x => x.Id == orderRes.OrderStatusId).FirstOrDefaultAsync();
                        if (orderStautsRes != null)
                        {
                            userOrder.OrderStatus =isZawgyi?Rabbit.Uni2Zg(orderStautsRes.Name): orderStautsRes.Name;
                        }
                        userOrder.UserId = orderRes.UserId;
                        userOrder.UserUrl = QueenOfDreamerConst.AWS_USER_PROFILE_PATH + orderRes.UserId + ".png";
                        userOrder.UserName = orderRes.UserName;
                        userOrder.OrderCreatedDate = orderRes.OrderDate;
                        userOrder.CreatedDate = orderRes.CreatedDate;

                    }
                    else
                    {
                        userOrder.OrderStatus = "";
                        userOrder.UserId = 0;
                        userOrder.UserName = "";
                    }
                    userOrder.OrderId = orderDetail.OrderId;
                    userOrder.Qty = orderDetail.TotalQty;

                    var paymentInfo = await _context.OrderPaymentInfo.Where(p => p.OrderId == orderDetail.OrderId)
                                    .Select(itm => new
                                    {
                                        Id = itm.Id,
                                        TransactionDate = itm.TransactionDate,
                                        PaymentStatusId = itm.PaymentStatus.Id,
                                        PaymentStatus =isZawgyi?Rabbit.Uni2Zg(itm.PaymentStatus.Name): itm.PaymentStatus.Name,
                                        PaymentServiceName =isZawgyi?Rabbit.Uni2Zg(itm.PaymentService.Name): itm.PaymentService.Name,
                                        PaymentServiceImgPath = itm.PaymentService.ImgPath
                                    }).OrderByDescending(p => p.TransactionDate).FirstOrDefaultAsync();
                    if (paymentInfo != null)
                    {
                        userOrder.PaymentInfoId = paymentInfo.Id;
                        userOrder.PaymentStatusId = paymentInfo.PaymentStatusId;
                        userOrder.PaymentStatus = paymentInfo.PaymentStatus;
                        userOrder.PaymentServiceName = paymentInfo.PaymentServiceName;
                        userOrder.PaymentServiceImgPath = paymentInfo.PaymentServiceImgPath;
                    }
                    else
                    {
                        userOrder.PaymentStatusId = 0;
                        userOrder.PaymentStatus = "";
                        userOrder.PaymentServiceName = "";
                        userOrder.PaymentServiceImgPath = "";
                    }
                    userOrderList.Add(userOrder);
                }
                userOrderList = userOrderList.OrderByDescending(x => x.CreatedDate).ToList();
                orderListByProduct.UserList = userOrderList;
                return orderListByProduct;
            }
            return null;
        }

        public async Task<ResponseStatus> UpdateOrderStatus(UpdateOrderStatusRequest request, int currentUserLogin,int platform)
        {
            ResponseStatus response = new ResponseStatus();
            Order orderDetail = await _context.Order.Where(x => x.Id == request.OrderId).FirstOrDefaultAsync();
            if (orderDetail == null)
            {
                response.StatusCode = StatusCodes.Status401Unauthorized;
                response.Message = "Invalid order Id.";
                return response;
            }
            else
            {
                if (orderDetail.OrderStatusId == QueenOfDreamerConst.ORDER_STATUS_ORDER && request.OrderStatusId == QueenOfDreamerConst.ORDER_STATUS_TAKE)
                {
                    orderDetail.OrderStatusId = QueenOfDreamerConst.ORDER_STATUS_TAKE; //ထုတ်ပိုးပြီး

                    #region  Activity Log
                    try{
                        ActivityLog data=new ActivityLog(){
                            UserId=currentUserLogin,
                            ActivityTypeId=QueenOfDreamerConst.ACTIVITY_TYPE_ORDER_STATUS,
                            Value=QueenOfDreamerConst.ORDER_STATUS_TAKE.ToString()+"#"+orderDetail.VoucherNo,
                            CreatedBy=currentUserLogin,
                            CreatedDate=DateTime.Now,
                            PlatformId=platform
                        };
                        _context.ActivityLog.Add(data);
                        await _context.SaveChangesAsync();
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                    #endregion

                    string voucherNo = orderDetail.VoucherNo;
                    int userId = orderDetail.OrderUserId;
                    try
                    {
                        await _context.SaveChangesAsync();

                        var notiTemplateBuyer = await _context.NotificationTemplate.
                            Where(a => a.ActionName == "OrderStatusBuyer").SingleOrDefaultAsync();
                        var bodyBuyer = notiTemplateBuyer.Body.Replace("{orderId}", voucherNo.ToString());
                        var bodyBuyers = bodyBuyer.Replace("{orderStatus}", "အောင်မြင်စွာ ထုပ်ပိုးပြီး ဖြစ်သည်");

                        Models.Notification notificationBuyer = new Models.Notification();
                        notificationBuyer.Title = notiTemplateBuyer.Title;
                        notificationBuyer.Body = bodyBuyers;
                        notificationBuyer.UserId = userId;
                        notificationBuyer.ImgUrl = QueenOfDreamerConst.AWS_USER_PROFILE_PATH + userId + ".png";
                        notificationBuyer.RedirectAction = QueenOfDreamerConst.NOTI_REDIRECT_ACTION_ORDER_DETAIL;
                        notificationBuyer.ReferenceAttribute = request.OrderId.ToString();
                        notificationBuyer.CreatedDate = DateTime.Now;
                        notificationBuyer.CreatedBy = 1;
                        await _context.Notification.AddAsync(notificationBuyer);
                        await _context.SaveChangesAsync();

                        var test = Helpers.Notification.SendFCMNotification(userId.ToString(),
                                                                            notiTemplateBuyer.Title,
                                                                            bodyBuyers, userId,
                                                                            QueenOfDreamerConst.NOTI_REDIRECT_ACTION_ORDER_DETAIL,
                                                                            orderDetail.Id,
                                                                            notificationBuyer.Id,false);  // true for sending noti to seller

                        response.Message = "ထုပ်ပိုးပြီးအဆင့်သို့ အောင်မြင်စွာ ပြောင်းပြီးပါပြီ။";
                        response.StatusCode = StatusCodes.Status200OK;
                        return response;
                    }
                    catch (Exception ex)
                    {
                        response.StatusCode = StatusCodes.Status500InternalServerError;
                        response.Message = ex.Message;
                        return response;
                    }
                }
                else if (orderDetail.OrderStatusId == QueenOfDreamerConst.ORDER_STATUS_ORDER && request.OrderStatusId == QueenOfDreamerConst.ORDER_STATUS_SENDING)
                {
                    orderDetail.OrderStatusId = QueenOfDreamerConst.ORDER_STATUS_SENDING; //ပို့နေသည်
                    string voucherNo = orderDetail.VoucherNo;
                    int userId = orderDetail.OrderUserId;

                    #region  Activity Log
                    try{
                        ActivityLog data=new ActivityLog(){
                            UserId=currentUserLogin,
                            ActivityTypeId=QueenOfDreamerConst.ACTIVITY_TYPE_ORDER_STATUS,
                            Value=QueenOfDreamerConst.ORDER_STATUS_SENDING.ToString()+"#"+orderDetail.VoucherNo,
                            CreatedBy=currentUserLogin,
                            CreatedDate=DateTime.Now,
                            PlatformId=platform
                        };
                        _context.ActivityLog.Add(data);
                        await _context.SaveChangesAsync();
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                    #endregion

                    try
                    {
                        NotificationTemplate notiTemplateBuyer = await _context.NotificationTemplate.
                            Where(a => a.ActionName == "OrderStatusBuyer").SingleOrDefaultAsync();
                        var bodyBuyer = notiTemplateBuyer.Body.Replace("{orderId}", voucherNo.ToString());
                        var bodyBuyers = bodyBuyer.Replace("{orderStatus}", "ပို့ဆောင်ရန်ညွှန်ကြားပြီးဖြစ်သည်");

                        Models.Notification notificationBuyer = new Models.Notification();
                        notificationBuyer.Title = notiTemplateBuyer.Title;
                        notificationBuyer.Body = bodyBuyers;
                        notificationBuyer.UserId = userId;
                        notificationBuyer.ImgUrl = QueenOfDreamerConst.AWS_USER_PROFILE_PATH + userId + ".png";
                        notificationBuyer.RedirectAction = QueenOfDreamerConst.NOTI_REDIRECT_ACTION_ORDER_DETAIL;
                        notificationBuyer.ReferenceAttribute = request.OrderId.ToString();
                        notificationBuyer.CreatedDate = DateTime.Now;
                        notificationBuyer.CreatedBy = currentUserLogin;
                        await _context.Notification.AddAsync(notificationBuyer);
                        await _context.SaveChangesAsync();

                        var test = Helpers.Notification.SendFCMNotification(userId.ToString(),
                                                                            notiTemplateBuyer.Title,
                                                                            bodyBuyers, 
                                                                            userId,
                                                                            QueenOfDreamerConst.NOTI_REDIRECT_ACTION_ORDER_DETAIL,
                                                                            orderDetail.Id,
                                                                            notificationBuyer.Id,false);  // true for sending noti to seller

                        response.Message = "ပို့နေသည်အဆင့်သို့ အောင်မြင်စွာ ပြောင်းပြီးပါပြီ။";
                        response.StatusCode = StatusCodes.Status200OK;
                        return response;
                    }
                    catch (Exception ex)
                    {
                        response.StatusCode = StatusCodes.Status500InternalServerError;
                        response.Message = ex.Message;
                        return response;
                    }
                }
                else if (orderDetail.OrderStatusId == QueenOfDreamerConst.ORDER_STATUS_ORDER && request.OrderStatusId == QueenOfDreamerConst.ORDER_STATUS_SENT)
                {
                    orderDetail.OrderStatusId = QueenOfDreamerConst.ORDER_STATUS_SENT; //ပို့ပြီး
                    string voucherNo = orderDetail.VoucherNo;
                    int userId = orderDetail.OrderUserId;

                    #region  Activity Log
                    try{
                        ActivityLog data=new ActivityLog(){
                            UserId=currentUserLogin,
                            ActivityTypeId=QueenOfDreamerConst.ACTIVITY_TYPE_ORDER_STATUS,
                            Value=QueenOfDreamerConst.ORDER_STATUS_SENT.ToString()+"#"+orderDetail.VoucherNo,
                            CreatedBy=currentUserLogin,
                            CreatedDate=DateTime.Now,
                            PlatformId=platform
                        };
                        _context.ActivityLog.Add(data);
                        await _context.SaveChangesAsync();
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                    #endregion

                    try
                    {
                        NotificationTemplate notiTemplateBuyer = await _context.NotificationTemplate.
                            Where(a => a.ActionName == "OrderStatusBuyer").SingleOrDefaultAsync();
                        var bodyBuyer = notiTemplateBuyer.Body.Replace("{orderId}", voucherNo.ToString());
                        var bodyBuyers = bodyBuyer.Replace("{orderStatus}", "အောင်မြင်စွာ ပို့ဆောင်ပြီးဖြစ်သည်");

                        Models.Notification notificationBuyer = new Models.Notification();
                        notificationBuyer.Title = notiTemplateBuyer.Title;
                        notificationBuyer.Body = bodyBuyers;
                        notificationBuyer.UserId = userId;
                        notificationBuyer.ImgUrl = QueenOfDreamerConst.AWS_USER_PROFILE_PATH + userId + ".png";
                        notificationBuyer.RedirectAction = QueenOfDreamerConst.NOTI_REDIRECT_ACTION_ORDER_DETAIL;
                        notificationBuyer.ReferenceAttribute = request.OrderId.ToString();
                        notificationBuyer.CreatedDate = DateTime.Now;
                        notificationBuyer.CreatedBy = currentUserLogin;
                        await _context.Notification.AddAsync(notificationBuyer);
                        await _context.SaveChangesAsync();

                        var test = Helpers.Notification.SendFCMNotification(userId.ToString(),
                                                                            notiTemplateBuyer.Title,
                                                                            bodyBuyers,
                                                                            userId,
                                                                            QueenOfDreamerConst.NOTI_REDIRECT_ACTION_ORDER_DETAIL,
                                                                            orderDetail.Id,
                                                                            notificationBuyer.Id,false);// true for sending noti to seller

                        response.Message = "ပို့ပြီးအဆင့်သို့ အောင်မြင်စွာ ပြောင်းပြီးပါပြီ။";
                        response.StatusCode = StatusCodes.Status200OK;
                        return response;
                    }
                    catch (Exception ex)
                    {
                        response.StatusCode = StatusCodes.Status500InternalServerError;
                        response.Message = ex.Message;
                        return response;
                    }
                }
                else if (orderDetail.OrderStatusId == QueenOfDreamerConst.ORDER_STATUS_TAKE && request.OrderStatusId == QueenOfDreamerConst.ORDER_STATUS_SENDING)
                {
                    orderDetail.OrderStatusId = QueenOfDreamerConst.ORDER_STATUS_SENDING; // -ပို့နေသည်
                    string voucherNo = orderDetail.VoucherNo;
                    int userId = orderDetail.OrderUserId;

                    #region  Activity Log
                    try{
                        ActivityLog data=new ActivityLog(){
                            UserId=currentUserLogin,
                            ActivityTypeId=QueenOfDreamerConst.ACTIVITY_TYPE_ORDER_STATUS,
                            Value=QueenOfDreamerConst.ORDER_STATUS_SENDING.ToString()+"#"+orderDetail.VoucherNo,
                            CreatedBy=currentUserLogin,
                            CreatedDate=DateTime.Now,
                            PlatformId=platform
                        };
                        _context.ActivityLog.Add(data);
                        await _context.SaveChangesAsync();
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                    #endregion

                    try
                    {
                        var notiTemplateBuyer = await _context.NotificationTemplate.
                            Where(a => a.ActionName == "OrderStatusBuyer").FirstOrDefaultAsync();
                        var bodyBuyer = notiTemplateBuyer.Body.Replace("{orderId}", voucherNo.ToString());
                        var bodyBuyers = bodyBuyer.Replace("{orderStatus}", "ပို့ဆောင်ရန်ညွှန်ကြားပြီးဖြစ်သည်");

                        Models.Notification notificationBuyer = new Models.Notification();
                        notificationBuyer.Title = notiTemplateBuyer.Title;
                        notificationBuyer.Body = bodyBuyers;
                        notificationBuyer.UserId = userId;
                        notificationBuyer.ImgUrl = QueenOfDreamerConst.AWS_USER_PROFILE_PATH + userId + ".png";
                        notificationBuyer.RedirectAction = QueenOfDreamerConst.NOTI_REDIRECT_ACTION_ORDER_DETAIL;
                        notificationBuyer.ReferenceAttribute = request.OrderId.ToString();
                        notificationBuyer.CreatedDate = DateTime.Now;
                        notificationBuyer.CreatedBy = currentUserLogin;
                        await _context.Notification.AddAsync(notificationBuyer);
                        await _context.SaveChangesAsync();

                        var test = Helpers.Notification.SendFCMNotification(userId.ToString(),
                                                                            notiTemplateBuyer.Title,
                                                                            bodyBuyers,
                                                                            userId,
                                                                            QueenOfDreamerConst.NOTI_REDIRECT_ACTION_ORDER_DETAIL,
                                                                            orderDetail.Id,
                                                                            notificationBuyer.Id,false);// true for sending noti to seller

                        response.Message = "ပို့နေသည်အဆင့်သို့ အောင်မြင်စွာ ပြောင်းပြီးပါပြီ။";
                        response.StatusCode = StatusCodes.Status200OK;
                        return response;
                    }
                    catch (Exception ex)
                    {
                        response.StatusCode = StatusCodes.Status500InternalServerError;
                        response.Message = ex.Message;
                        return response;
                    }
                }
                else if (orderDetail.OrderStatusId == QueenOfDreamerConst.ORDER_STATUS_TAKE && request.OrderStatusId == QueenOfDreamerConst.ORDER_STATUS_SENT)
                {
                    orderDetail.OrderStatusId = QueenOfDreamerConst.ORDER_STATUS_SENT; //ပို့ပြီး
                    string voucherNo = orderDetail.VoucherNo;
                    int userId = orderDetail.OrderUserId;

                    #region  Activity Log
                    try{
                        ActivityLog data=new ActivityLog(){
                            UserId=currentUserLogin,
                            ActivityTypeId=QueenOfDreamerConst.ACTIVITY_TYPE_ORDER_STATUS,
                            Value=QueenOfDreamerConst.ORDER_STATUS_SENT.ToString()+"#"+orderDetail.VoucherNo,
                            CreatedBy=currentUserLogin,
                            CreatedDate=DateTime.Now,
                            PlatformId=platform
                        };
                        _context.ActivityLog.Add(data);
                        await _context.SaveChangesAsync();
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                    #endregion

                    try
                    {
                        NotificationTemplate notiTemplateBuyer = await _context.NotificationTemplate.
                            Where(a => a.ActionName == "OrderStatusBuyer").SingleOrDefaultAsync();
                        var bodyBuyer = notiTemplateBuyer.Body.Replace("{orderId}", voucherNo.ToString());
                        var bodyBuyers = bodyBuyer.Replace("{orderStatus}", "အောင်မြင်စွာ ပို့ဆောင်ပြီးဖြစ်သည်");

                        Models.Notification notificationBuyer = new Models.Notification();
                        notificationBuyer.Title = notiTemplateBuyer.Title;
                        notificationBuyer.Body = bodyBuyers;
                        notificationBuyer.UserId = userId;
                        notificationBuyer.ImgUrl = QueenOfDreamerConst.AWS_USER_PROFILE_PATH + userId + ".png";
                        notificationBuyer.RedirectAction = QueenOfDreamerConst.NOTI_REDIRECT_ACTION_ORDER_DETAIL;
                        notificationBuyer.ReferenceAttribute = request.OrderId.ToString();
                        notificationBuyer.CreatedDate = DateTime.Now;
                        notificationBuyer.CreatedBy = currentUserLogin;
                        await _context.Notification.AddAsync(notificationBuyer);
                        await _context.SaveChangesAsync();

                        var test = Helpers.Notification.SendFCMNotification(userId.ToString(),
                                                                            notiTemplateBuyer.Title,
                                                                            bodyBuyers,
                                                                            userId,
                                                                            QueenOfDreamerConst.NOTI_REDIRECT_ACTION_ORDER_DETAIL,
                                                                            orderDetail.Id,
                                                                            notificationBuyer.Id,false);// true for sending noti to seller

                        response.Message = "ပို့ပြီးအဆင့်သို့ အောင်မြင်စွာ ပြောင်းပြီးပါပြီ။";
                        response.StatusCode = StatusCodes.Status200OK;
                        return response;
                    }
                    catch (Exception ex)
                    {
                        response.StatusCode = StatusCodes.Status500InternalServerError;
                        response.Message = ex.Message;
                        return response;
                    }
                }
                else if (orderDetail.OrderStatusId == QueenOfDreamerConst.ORDER_STATUS_SENDING && request.OrderStatusId == QueenOfDreamerConst.ORDER_STATUS_SENT)
                {
                    orderDetail.OrderStatusId = QueenOfDreamerConst.ORDER_STATUS_SENT; //ပို့ပြီး
                    string voucherNo = orderDetail.VoucherNo;
                    int userId = orderDetail.OrderUserId;

                    #region  Activity Log
                    try{
                        ActivityLog data=new ActivityLog(){
                            UserId=currentUserLogin,
                            ActivityTypeId=QueenOfDreamerConst.ACTIVITY_TYPE_ORDER_STATUS,
                            Value=QueenOfDreamerConst.ORDER_STATUS_SENT.ToString()+"#"+orderDetail.VoucherNo,
                            CreatedBy=currentUserLogin,
                            CreatedDate=DateTime.Now,
                            PlatformId=platform
                        };
                        _context.ActivityLog.Add(data);
                        await _context.SaveChangesAsync();
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                    #endregion

                    try
                    {
                        NotificationTemplate notiTemplateBuyer = await _context.NotificationTemplate.
                            Where(a => a.ActionName == "OrderStatusBuyer").SingleOrDefaultAsync();
                        var bodyBuyer = notiTemplateBuyer.Body.Replace("{orderId}", voucherNo.ToString());
                        var bodyBuyers = bodyBuyer.Replace("{orderStatus}", "အောင်မြင်စွာ ပို့ဆောင်ပြီးဖြစ်သည်");

                        Models.Notification notificationBuyer = new Models.Notification();
                        notificationBuyer.Title = notiTemplateBuyer.Title;
                        notificationBuyer.Body = bodyBuyers;
                        notificationBuyer.UserId = userId;
                        notificationBuyer.ImgUrl = QueenOfDreamerConst.AWS_USER_PROFILE_PATH + userId + ".png";
                        notificationBuyer.RedirectAction = QueenOfDreamerConst.NOTI_REDIRECT_ACTION_ORDER_DETAIL;
                        notificationBuyer.ReferenceAttribute = request.OrderId.ToString();
                        notificationBuyer.CreatedDate = DateTime.Now;
                        notificationBuyer.CreatedBy = currentUserLogin;
                        await _context.Notification.AddAsync(notificationBuyer);
                        await _context.SaveChangesAsync();

                        var test = Helpers.Notification.SendFCMNotification(userId.ToString(),
                                                                            notiTemplateBuyer.Title,
                                                                            bodyBuyers,
                                                                            userId,
                                                                            QueenOfDreamerConst.NOTI_REDIRECT_ACTION_ORDER_DETAIL,
                                                                            orderDetail.Id,
                                                                            notificationBuyer.Id,false);// true for sending noti to seller

                        response.Message = "ပို့ပြီးအဆင့်သို့ အောင်မြင်စွာ ပြောင်းပြီးပါပြီ။";
                        response.StatusCode = StatusCodes.Status200OK;
                        return response;
                    }
                    catch (Exception ex)
                    {
                        response.StatusCode = StatusCodes.Status500InternalServerError;
                        response.Message = ex.Message;
                        return response;
                    }
                }
                else
                {
                    response.Message = "Invalid Order Status Id";
                    response.StatusCode = StatusCodes.Status400BadRequest;
                    return response;
                }
                
                
           }
        }

        public async Task<ResponseStatus> UpdatePaymentStatus(UpdatePaymentStatusRequest request, int currentUserLogin,string token,int platform)
        {
            bool isZawgyi=Rabbit.IsZawgyi(_httpContextAccessor);

            ResponseStatus response = new ResponseStatus();

            OrderPaymentInfo orderPaymentInfoDetail = await _context.OrderPaymentInfo.Where(x => x.Id == request.PaymentInfoId).FirstOrDefaultAsync();
            if (orderPaymentInfoDetail == null)
            {
                response.StatusCode = StatusCodes.Status401Unauthorized;
                response.Message = "Invalid PaymentInfo Id.";
                return response;
            }
            else
            {
                orderPaymentInfoDetail.SellerRemark=isZawgyi?Rabbit.Zg2Uni(request.SellerRemark):request.SellerRemark;
                
                int orderId = orderPaymentInfoDetail.OrderId;
                int userId = 0;
                string paymentServiceName = "";
                string voucherNo = "";
                var orderDetail = await _context.Order.Where(x => x.Id == orderId).FirstOrDefaultAsync();
                if (orderDetail != null)
                {
                    userId = orderDetail.OrderUserId;
                    voucherNo = orderDetail.VoucherNo;
                    orderDetail.CreatedDate = DateTime.Now;
                    orderDetail.CreatedBy = 1;
                    await _context.SaveChangesAsync();
                }
                var paymentServiceDetail = await _context.PaymentService.Where(x => x.Id == orderPaymentInfoDetail.PaymentServiceId).FirstOrDefaultAsync();
                if (paymentServiceDetail != null)
                {
                    paymentServiceName = paymentServiceDetail.Name;
                }

                // if (orderPaymentInfoDetail.PaymentStatusId == QueenOfDreamerConst.PAYMENT_STATUS_CHECK && request.PaymentStatusId == QueenOfDreamerConst.PAYMENT_STATUS_SUCCESS)
                if (request.PaymentStatusId == QueenOfDreamerConst.PAYMENT_STATUS_SUCCESS)
                {

                    #region  Activity Log
                    try{
                        ActivityLog data=new ActivityLog(){
                            UserId=currentUserLogin,
                            ActivityTypeId=QueenOfDreamerConst.ACTIVITY_TYPE_PAYMENT_STATUS,
                            Value=QueenOfDreamerConst.PAYMENT_STATUS_SUCCESS.ToString()+"#"+orderDetail.VoucherNo,
                            CreatedBy=currentUserLogin,
                            CreatedDate=DateTime.Now,
                            PlatformId=platform
                        };
                        _context.ActivityLog.Add(data);
                        await _context.SaveChangesAsync();
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                    #endregion

                    try
                    {
                        // var sellerList = await _userServices.GetAllSellerUserId(token);
                        // foreach (var seller in sellerList)
                        // {
                            orderPaymentInfoDetail.PaymentStatusId = QueenOfDreamerConst.PAYMENT_STATUS_SUCCESS; 

                            NotificationTemplate notiTemplate = await _context.NotificationTemplate.
                            Where(a => a.ActionName == "PaymentStatus").SingleOrDefaultAsync();
                            var body = notiTemplate.Body.Replace("{paymentServiceName}", paymentServiceName);
                            var bodyPayment = body.Replace("{orderId}", voucherNo);
                            var bodyPayments = bodyPayment.Replace("{paymentStatus}", "အောင်မြင်ပါသည်");

                            Models.Notification notification = new Models.Notification();
                            notification.Title = notiTemplate.Title;
                            notification.Body = bodyPayments;
                            notification.UserId = userId;
                            notification.ImgUrl = QueenOfDreamerConst.AWS_USER_PROFILE_PATH + userId + ".png";
                            notification.RedirectAction = QueenOfDreamerConst.NOTI_REDIRECT_ACTION_ORDER_DETAIL;
                            notification.ReferenceAttribute = orderId.ToString();
                            notification.CreatedDate = DateTime.Now;
                            notification.CreatedBy = currentUserLogin;
                            await _context.Notification.AddAsync(notification);
                            await _context.SaveChangesAsync();

                            var aa = Helpers.Notification.SendFCMNotification(userId.ToString(),
                                                                            notiTemplate.Title,
                                                                            bodyPayments,
                                                                            userId,
                                                                            QueenOfDreamerConst.NOTI_REDIRECT_ACTION_ORDER_DETAIL,
                                                                            orderDetail.Id,
                                                                            notification.Id,false);// true for sending noti to seller
                        // }
                        response.Message = "ယခုအော်ဒါအတွက် ငွေပေးချေမှု အောင်မြင်ပါသည်";
                        response.StatusCode = StatusCodes.Status200OK;
                        return response;
                    }
                    catch (Exception ex)
                    {
                        response.StatusCode = StatusCodes.Status500InternalServerError;
                        response.Message = ex.Message;
                        return response;
                    }
                }
                // else if (orderPaymentInfoDetail.PaymentStatusId == QueenOfDreamerConst.PAYMENT_STATUS_CHECK && request.PaymentStatusId == QueenOfDreamerConst.PAYMENT_STATUS_FAIL)
                else if (request.PaymentStatusId == QueenOfDreamerConst.PAYMENT_STATUS_FAIL)
                {
                    #region  Activity Log
                    try{
                        ActivityLog data=new ActivityLog(){
                            UserId=currentUserLogin,
                            ActivityTypeId=QueenOfDreamerConst.ACTIVITY_TYPE_PAYMENT_STATUS,
                            Value=QueenOfDreamerConst.PAYMENT_STATUS_FAIL.ToString()+"#"+orderDetail.VoucherNo,
                            CreatedBy=currentUserLogin,
                            CreatedDate=DateTime.Now,
                            PlatformId=platform
                        };
                        _context.ActivityLog.Add(data);
                        await _context.SaveChangesAsync();
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                    #endregion
                    
                    try
                    {
                        orderPaymentInfoDetail.PaymentStatusId = QueenOfDreamerConst.PAYMENT_STATUS_FAIL;

                        NotificationTemplate notiTemplate = await _context.NotificationTemplate.
                        Where(a => a.ActionName == "PaymentStatus").SingleOrDefaultAsync();
                        var body = notiTemplate.Body.Replace("{paymentServiceName}", paymentServiceName);
                        var bodyPayment = body.Replace("{orderId}", voucherNo.ToString());
                        var bodyPayments = bodyPayment.Replace("{paymentStatus}", "မအောင်မြင်ပါ");

                        Models.Notification notification = new Models.Notification();
                        notification.Title = notiTemplate.Title;
                        notification.Body = bodyPayments;
                        notification.UserId = userId;
                        notification.ImgUrl = QueenOfDreamerConst.AWS_USER_PROFILE_PATH + userId + ".png";
                        notification.RedirectAction = QueenOfDreamerConst.NOTI_REDIRECT_ACTION_ORDER_DETAIL;
                        notification.ReferenceAttribute = orderId.ToString();
                        notification.CreatedDate = DateTime.Now;
                        notification.CreatedBy = currentUserLogin;
                        await _context.Notification.AddAsync(notification);
                        await _context.SaveChangesAsync();

                        var aa = Helpers.Notification.SendFCMNotification(userId.ToString(),
                                                                        notiTemplate.Title,
                                                                        bodyPayments,
                                                                        userId,
                                                                        QueenOfDreamerConst.NOTI_REDIRECT_ACTION_ORDER_DETAIL,
                                                                        orderDetail.Id,
                                                                        notification.Id,false);// true for sending noti to seller

                        response.Message = "ယခုအော်ဒါအတွက် ငွေပေးချေမှု မအောင်မြင်ပါ။ \r\n \r\n  နောက်တစ်ကြိမ် ထပ်မံပေးချေပါ။";
                        response.StatusCode = StatusCodes.Status200OK;
                        return response;
                    }
                    catch (Exception ex)
                    {
                        response.StatusCode = StatusCodes.Status500InternalServerError;
                        response.Message = ex.Message;
                        return response;
                    }
                }
                response.Message = "Invalid Payment Info Id";
                response.StatusCode = StatusCodes.Status400BadRequest;
                return response;
            }
        }

        public async Task<ResponseStatus> UpdateDeliveryServiceStatus(UpdateDeliveryServiceStatusRequest request, int currentUserLogin,string token)
        {
            ResponseStatus response = new ResponseStatus();

            OrderDeliveryInfo orderDeliveryInfoDetail = await _context.OrderDeliveryInfo.Where(x => x.OrderId == request.OrderId).FirstOrDefaultAsync();
            if (orderDeliveryInfoDetail == null)
            {
                response.StatusCode = StatusCodes.Status401Unauthorized;
                response.Message = "Invalid Order Id.";
                return response;
            }
            else
            {
                int? cityId = orderDeliveryInfoDetail.CityId;
                int? townshipId = orderDeliveryInfoDetail.TownshipId;

                #region GetDeliveryServiceRate
                var deliveryRateOld= await _deliServices.GetDeliveryServiceRate(orderDeliveryInfoDetail.DeliveryServiceId,
                                int.Parse(cityId.ToString()),
                                int.Parse(townshipId.ToString()),
                                token);
                var deliveryRateNew= await _deliServices.GetDeliveryServiceRate(request.DeliveryServiceStatusId,
                int.Parse(cityId.ToString()),
                int.Parse(townshipId.ToString()),
                token);
                #endregion
               

                var oldDeliverServiceRate =deliveryRateOld; //await _context.DeliveryServiceRate.Where(x => x.DeliveryServiceId == orderDeliveryInfoDetail.DeliveryServiceId && x.CityId == cityId && x.TownshipId == townshipId).FirstOrDefaultAsync();
                double oldServiceAmount = 0.0;
                var newDeliverServiceRate = deliveryRateNew; //await _context.DeliveryServiceRate.Where(x => x.DeliveryServiceId == request.DeliveryServiceStatusId && x.CityId == cityId && x.TownshipId == townshipId).FirstOrDefaultAsync();
                double newServiceAmount = 0.0;
                var orderDetail = await _context.Order.Where(x => x.Id == request.OrderId).FirstOrDefaultAsync();
                double totalAmount = 0.0;
                if (orderDetail != null)
                {
                    totalAmount = orderDetail.TotalAmt;
                }
                if (oldDeliverServiceRate != null)
                {
                    oldServiceAmount = oldDeliverServiceRate.ServiceAmount;
                    totalAmount = totalAmount - oldServiceAmount;
                }
                if (newDeliverServiceRate != null)
                {
                    newServiceAmount = newDeliverServiceRate.ServiceAmount;
                    totalAmount = totalAmount + newServiceAmount;
                }
                orderDetail.TotalAmt = totalAmount;
                orderDetail.CreatedDate = DateTime.Now;
                orderDetail.CreatedBy = currentUserLogin;
                orderDeliveryInfoDetail.DeliveryServiceId = request.DeliveryServiceStatusId;
                await _context.SaveChangesAsync();

                orderDetail.OrderStatusId = QueenOfDreamerConst.ORDER_STATUS_SENDING;
                string voucherNo = orderDetail.VoucherNo;
                int userId = orderDetail.OrderUserId;
                try
                {
                    var notiTemplateBuyer = await _context.NotificationTemplate.
                        Where(a => a.ActionName == "OrderStatusBuyer").FirstOrDefaultAsync();
                    var bodyBuyer = notiTemplateBuyer.Body.Replace("{orderId}", voucherNo.ToString());
                    var bodyBuyers = bodyBuyer.Replace("{orderStatus}", "ပို့ဆောင်ရန်ညွှန်ကြားပြီးဖြစ်သည်");

                    Models.Notification notificationBuyer = new Models.Notification();
                    notificationBuyer.Title = notiTemplateBuyer.Title;
                    notificationBuyer.Body = bodyBuyers;
                    notificationBuyer.UserId = userId;
                    notificationBuyer.ImgUrl = QueenOfDreamerConst.AWS_USER_PROFILE_PATH + userId + ".png";
                    notificationBuyer.RedirectAction = QueenOfDreamerConst.NOTI_REDIRECT_ACTION_ORDER_DETAIL;
                    notificationBuyer.ReferenceAttribute = request.OrderId.ToString();
                    notificationBuyer.CreatedDate = DateTime.Now;
                    notificationBuyer.CreatedBy = currentUserLogin;
                    await _context.Notification.AddAsync(notificationBuyer);
                    await _context.SaveChangesAsync();

                    var test = Helpers.Notification.SendFCMNotification(userId.ToString(),
                                                                       notiTemplateBuyer.Title,
                                                                       bodyBuyers,
                                                                       userId,
                                                                       QueenOfDreamerConst.NOTI_REDIRECT_ACTION_ORDER_DETAIL,
                                                                       orderDetail.Id,
                                                                       notificationBuyer.Id,false);// true for sending noti to seller
                   
                }
                catch (Exception ex)
                {
                    response.StatusCode = StatusCodes.Status500InternalServerError;
                    response.Message = ex.Message;
                    return response;
                }
                response.Message = "အော်ဒါပို့ဆောင်ရန် Delivery ကို အောင်မြင်စွာ ပြောင်းပြီးပါပြီ";
                response.StatusCode = StatusCodes.Status200OK;
                return response;
            }
        }

        public async Task<ResponseStatus> SellerOrderCancel(OrderCancelRequest request, int currentUserLogin,int platform)
        {
            ResponseStatus response = new ResponseStatus();

            Order orderDetail = await _context.Order.Where(x => x.Id == request.OrderId).FirstOrDefaultAsync();
            if (orderDetail == null)
            {
                response.StatusCode = StatusCodes.Status401Unauthorized;
                response.Message = "Invalid Order Id.";
                return response;
            }
            else
            {
                var orderDetailList = await _context.OrderDetail.Where(x => x.OrderId == orderDetail.Id).ToListAsync();
                if (orderDetailList.Count > 0)
                {
                    foreach (var item in orderDetailList)
                    {
                        var skuProductQty = await _context.ProductSku.Where(x => x.ProductId == item.ProductId && x.SkuId == item.SkuId).FirstOrDefaultAsync();
                        if (skuProductQty != null)
                        {
                            skuProductQty.Qty = skuProductQty.Qty + item.Qty;
                            await _context.SaveChangesAsync();
                        }
                    }
                }
                string voucherNo = orderDetail.VoucherNo;
                int userId = orderDetail.OrderUserId;
                string date = DateTime.Now.ToString();
                orderDetail.OrderStatusId = QueenOfDreamerConst.ORDER_STATUS_CANCEL;
                orderDetail.IsDeleted = true;
                orderDetail.UpdatedDate = DateTime.Now;
                orderDetail.UpdatedBy = currentUserLogin;

                #region  Activity Log
                try{
                    ActivityLog data=new ActivityLog(){
                        UserId=currentUserLogin,
                        ActivityTypeId=QueenOfDreamerConst.ACTIVITY_TYPE_ORDER_CANCEL,
                        Value=voucherNo,
                        CreatedBy=currentUserLogin,
                        CreatedDate=DateTime.Now,
                        PlatformId=platform
                    };
                    _context.ActivityLog.Add(data);
                    await _context.SaveChangesAsync();
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                #endregion

                try
                {
                    NotificationTemplate notiTemplate = await _context.NotificationTemplate.
                        Where(a => a.ActionName == "CancelOrder").SingleOrDefaultAsync();
                    var body = notiTemplate.Body.Replace("{orderId}", voucherNo.ToString());
                    var bodyCancel = body.Replace("{userType}", "ရောင်းသူမှ");
                    var bodyCancels = bodyCancel.Replace("{date}", date);

                    Models.Notification notification = new Models.Notification();
                    notification.Title = notiTemplate.Title;
                    notification.Body = bodyCancels;
                    notification.UserId = userId;
                    notification.ImgUrl = QueenOfDreamerConst.AWS_USER_PROFILE_PATH + userId + ".png";
                    notification.RedirectAction = QueenOfDreamerConst.NOTI_REDIRECT_ACTION_ORDER_DETAIL;
                    notification.ReferenceAttribute = request.OrderId.ToString();
                    notification.CreatedDate = DateTime.Now;
                    notification.CreatedBy = currentUserLogin;
                    await _context.Notification.AddAsync(notification);
                    await _context.SaveChangesAsync();

                    var aa = Helpers.Notification.SendFCMNotification(userId.ToString(),
                                                                     notiTemplate.Title,
                                                                     bodyCancels,
                                                                     userId,
                                                                     QueenOfDreamerConst.NOTI_REDIRECT_ACTION_ORDER_DETAIL,
                                                                     orderDetail.Id,
                                                                     notification.Id,false);// true for sending noti to seller

                    response.Message = "အော်ဒါ ဖျက်သိမ်းခြင်း အောင်မြင်ပါသည်";
                    response.StatusCode = StatusCodes.Status200OK;

                    return response;
                }
                catch (Exception ex)
                {
                    response.StatusCode = StatusCodes.Status500InternalServerError;
                    response.Message = ex.Message;
                    return response;
                }
                
            }
        }

        public async Task<ResponseStatus> BuyerOrderCancel(OrderCancelRequest request, int currentUserLogin,string token,int platform)
        {
            ResponseStatus response = new ResponseStatus();

            Order orderDetail = await _context.Order.Where(x => x.Id == request.OrderId).FirstOrDefaultAsync();
            if (orderDetail == null)
            {
                response.StatusCode = StatusCodes.Status401Unauthorized;
                response.Message = "Invalid Order Id.";
                return response;
            }
            else
            {
                var orderDetailList = await _context.OrderDetail.Where(x => x.OrderId == orderDetail.Id).ToListAsync();
                if (orderDetailList.Count > 0)
                {
                    foreach (var item in orderDetailList)
                    {
                        var skuProductQty = await _context.ProductSku.Where(x => x.ProductId == item.ProductId && x.SkuId == item.SkuId).FirstOrDefaultAsync();
                        if (skuProductQty != null)
                        {
                            skuProductQty.Qty = skuProductQty.Qty + item.Qty;
                            await _context.SaveChangesAsync();
                        }
                    }
                }
                string voucherNo = orderDetail.VoucherNo;                
                string date = DateTime.Now.ToString();
                orderDetail.OrderStatusId = QueenOfDreamerConst.ORDER_STATUS_CANCEL;
                string userName = "";
                var orderDeliveryInfo = await _context.OrderDeliveryInfo.Where(x => x.OrderId == orderDetail.Id).FirstOrDefaultAsync();
                if (orderDeliveryInfo != null)
                {
                    userName = orderDeliveryInfo.Name;
                }
                orderDetail.IsDeleted = true;
                orderDetail.UpdatedDate = DateTime.Now;
                orderDetail.UpdatedBy = currentUserLogin;

                #region  Activity Log
                try{
                    ActivityLog data=new ActivityLog(){
                        UserId=currentUserLogin,
                        ActivityTypeId=QueenOfDreamerConst.ACTIVITY_TYPE_ORDER_CANCEL,
                        Value=voucherNo,
                        CreatedBy=currentUserLogin,
                        CreatedDate=DateTime.Now,
                        PlatformId=platform
                    };
                    _context.ActivityLog.Add(data);
                    await _context.SaveChangesAsync();
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                #endregion

                try
                {
                    var sellerList = await _userServices.GetAllSellerUserId(token);
                    foreach (var seller in sellerList)
                    {

                        NotificationTemplate notiTemplate = await _context.NotificationTemplate.
                            Where(a => a.ActionName == "CancelOrder").SingleOrDefaultAsync();
                        var body = notiTemplate.Body.Replace("{orderId}", voucherNo.ToString());
                        var bodyCancel = body.Replace("{userType}", "ဝယ်သူမှ");
                        var bodyCancels = bodyCancel.Replace("{date}", date);

                        Models.Notification notification = new Models.Notification();
                        notification.Title = notiTemplate.Title;
                        notification.Body = bodyCancels;
                        notification.UserId = seller.Id; 
                        notification.ImgUrl = QueenOfDreamerConst.AWS_USER_PROFILE_PATH + seller.Id + ".png";
                        notification.RedirectAction = QueenOfDreamerConst.NOTI_REDIRECT_ACTION_ORDER_DETAIL;
                        notification.ReferenceAttribute = request.OrderId.ToString();
                        notification.CreatedDate = DateTime.Now;
                        notification.CreatedBy = currentUserLogin;
                        await _context.Notification.AddAsync(notification);
                        await _context.SaveChangesAsync();

                        var aa = Helpers.Notification.SendFCMNotification(seller.Id.ToString(),
                                                                        notiTemplate.Title,
                                                                        bodyCancels,
                                                                        seller.Id,
                                                                        QueenOfDreamerConst.NOTI_REDIRECT_ACTION_ORDER_DETAIL,
                                                                        orderDetail.Id,
                                                                        notification.Id,true);  // true for sending noti to seller                  
                                                                        }

                    response.Message = "အော်ဒါ ဖျက်သိမ်းခြင်း အောင်မြင်ပါသည်";
                    response.StatusCode = StatusCodes.Status200OK;
                    return response;

                }
                catch (Exception ex)
                {
                    response.StatusCode = StatusCodes.Status500InternalServerError;
                    response.Message = ex.Message;
                    return response;
                }
            }
        }

        public async Task<ResponseStatus> ChangeDeliveryAddress(ChangeDeliveryAddressRequest request)
        {
            bool isZawgyi=Rabbit.IsZawgyi(_httpContextAccessor);

            ResponseStatus response = new ResponseStatus();
            var order = await _context.Order.Where(x => x.Id == request.OrderId && x.OrderUserId == request.UserId).FirstOrDefaultAsync();
            if (order != null)
            {
                var orderDelivery = await _context.OrderDeliveryInfo.Where(x => x.OrderId == order.Id).FirstOrDefaultAsync();
                if (orderDelivery != null)
                {
                    if (!String.IsNullOrEmpty(request.PhoneNumber) && request.PhoneNumber != "string")
                    {
                        orderDelivery.PhNo = request.PhoneNumber;
                    }
                    if (!String.IsNullOrEmpty(request.Remark) && request.Remark != "string")
                    {
                        orderDelivery.Remark =isZawgyi?Rabbit.Zg2Uni(request.Remark): request.Remark;
                    }
                    orderDelivery.Address =isZawgyi?Rabbit.Zg2Uni(request.Address):request.Address;
                    orderDelivery.TownshipId = request.TownshipId;
                    orderDelivery.CityId = request.CityId;
                }
                await _context.SaveChangesAsync();
                response.StatusCode = StatusCodes.Status200OK;
                response.Message = "Success";
            }
            else
            {
                response.Message = "Invalid Order Id";
            }
            return response;
        }

        public async Task<ResponseStatus> PaymentAgain(PaymentAgainRequest request, int currentUserLogin,int platform,string token)
        {
            bool isZawgyi=Rabbit.IsZawgyi(_httpContextAccessor);

            ResponseStatus response = new ResponseStatus();

            if (request.PaymentServiceId == QueenOfDreamerConst.PAYMENT_SERVICE_COD)
            {
                var order = await _context.Order.Where(x => x.Id == request.OrderId && x.OrderUserId == currentUserLogin).FirstOrDefaultAsync();
                if (order != null)
                {
                    var paymentServiceName="";
                    var orderPaymentInfoToAdd = new OrderPaymentInfo
                    {
                        OrderId = order.Id,
                        PaymentServiceId = request.PaymentServiceId,
                        TransactionDate = DateTime.Now,
                        PhoneNo = null,
                        Remark = null,
                        ApprovalImgUrl = null,
                        PaymentStatusId = QueenOfDreamerConst.PAYMENT_STATUS_CHECK
                    };

                    if(request.PaymentServiceId==QueenOfDreamerConst.PAYMENT_SERVICE_BANK)  //if pay by bank, we will add bankID in payment service info
                    {
                        orderPaymentInfoToAdd.BankId=request.BankId;
                        paymentServiceName=await _context.Bank.Where(x=>x.Id==request.BankId).Select(x=>x.Name).SingleOrDefaultAsync();
                    }
                    else{
                        paymentServiceName=await _context.PaymentService.Where(x=>x.Id==request.PaymentServiceId).Select(x=>x.Name).SingleOrDefaultAsync();
                    }

                    await _context.OrderPaymentInfo.AddAsync(orderPaymentInfoToAdd);
                    await _context.SaveChangesAsync();
                    response.StatusCode = StatusCodes.Status200OK;
                    response.Message = "Success";

                    #region  Activity Log
                    try{
                        ActivityLog data=new ActivityLog(){
                            UserId=currentUserLogin,
                            ActivityTypeId=QueenOfDreamerConst.ACTIVITY_TYPE_MAKE_PAYMENT,
                            Value=order.VoucherNo,
                            CreatedBy=currentUserLogin,
                            CreatedDate=DateTime.Now,
                            PlatformId=platform
                        };
                        _context.ActivityLog.Add(data);
                        await _context.SaveChangesAsync();
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                    #endregion

                    #region Noti
                    var sellerList = await _userServices.GetAllSellerUserId(token); 
                    var orderUserInfo=await _userServices.GetUserInfo(order.OrderUserId,token);
                    foreach(var seller in sellerList)
                    {                            
                        NotificationTemplate notiTemplate = await _context.NotificationTemplate.
                            Where(a => a.ActionName == "PaymentAgain").SingleOrDefaultAsync();
                        var body = notiTemplate.Body.Replace("{userName}", orderUserInfo.Name);
                        body = body.Replace("{orderId}", order.VoucherNo);
                        body = body.Replace("{paymentServiceName}", paymentServiceName);
                        
                        Models.Notification notification = new Models.Notification();
                        notification.Title = notiTemplate.Title;
                        notification.Body =string.Format("{0} မှ အော်ဒါနံပါတ် {1} အတွက် {2} ဖြင့် ငွေပေးချေခဲ့သည်",order.OrderUserId,order.VoucherNo,paymentServiceName);
                        notification.UserId = seller.Id; //userId;
                        notification.ImgUrl = QueenOfDreamerConst.AWS_USER_PROFILE_PATH + order.OrderUserId + ".png";
                        notification.RedirectAction = QueenOfDreamerConst.NOTI_REDIRECT_ACTION_ORDER_DETAIL;
                        notification.ReferenceAttribute = order.Id.ToString();
                        notification.CreatedDate = DateTime.Now;
                        notification.CreatedBy = currentUserLogin;
                        await _context.Notification.AddAsync(notification);
                        await _context.SaveChangesAsync();                            
                        var test = Helpers.Notification.SendFCMNotification(seller.Id.ToString(),
                                                                        notiTemplate.Title,
                                                                        body, seller.Id,
                                                                        QueenOfDreamerConst.NOTI_REDIRECT_ACTION_ORDER_DETAIL,
                                                                        order.Id,
                                                                        notification.Id,true);  // true for sending noti to seller
                        test.GetAwaiter().GetResult();

                    }  
                    #endregion
                }
                else
                {
                    response.Message = "Invalid UserId or OrderId";
                }
            }
            else
            {
                var order = await _context.Order.Where(x => x.Id == request.OrderId && x.OrderUserId == currentUserLogin).FirstOrDefaultAsync();
                if (order != null)
                {                   
                    var path = await _services.UploadToS3(request.ApprovalImage.ApprovalImage
                       , request.ApprovalImage.ApprovalImageExtension, QueenOfDreamerConst.AWS_ORDER_PATH);

                    var paymentServiceName="";
                    var orderPaymentInfoToAdd = new OrderPaymentInfo
                    {
                        OrderId = order.Id,
                        PaymentServiceId = request.PaymentServiceId,
                        TransactionDate = DateTime.Now,
                        PhoneNo = request.PhoNo,
                        Remark =isZawgyi?Rabbit.Zg2Uni(request.Remark):request.Remark,
                        ApprovalImgUrl = path.ImgPath,
                        PaymentStatusId = QueenOfDreamerConst.PAYMENT_STATUS_CHECK
                    };

                    if(request.PaymentServiceId==QueenOfDreamerConst.PAYMENT_SERVICE_BANK)  //if pay by bank, we will add bankID in payment service info
                    {
                        orderPaymentInfoToAdd.BankId=request.BankId;
                         paymentServiceName=await _context.Bank.Where(x=>x.Id==request.BankId).Select(x=>x.Name).SingleOrDefaultAsync();
                    }
                    else{
                        paymentServiceName=await _context.PaymentService.Where(x=>x.Id==request.PaymentServiceId).Select(x=>x.Name).SingleOrDefaultAsync();
                    }

                    await _context.OrderPaymentInfo.AddAsync(orderPaymentInfoToAdd);
                    await _context.SaveChangesAsync();
                    response.StatusCode =StatusCodes.Status200OK;
                    response.Message = "Success";

                    #region  Activity Log
                    try{
                        ActivityLog data=new ActivityLog(){
                            UserId=currentUserLogin,
                            ActivityTypeId=QueenOfDreamerConst.ACTIVITY_TYPE_MAKE_PAYMENT,
                            Value=order.VoucherNo,
                            CreatedBy=currentUserLogin,
                            CreatedDate=DateTime.Now,
                            PlatformId=platform
                        };
                        _context.ActivityLog.Add(data);
                        await _context.SaveChangesAsync();
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                    #endregion

                    #region Noti
                    var sellerList = await _userServices.GetAllSellerUserId(token); 
                    var orderUserInfo=await _userServices.GetUserInfo(order.OrderUserId,token);
                    foreach(var seller in sellerList)
                    {                            
                        NotificationTemplate notiTemplate = await _context.NotificationTemplate.
                            Where(a => a.ActionName == "PaymentAgain").SingleOrDefaultAsync();
                        var body = notiTemplate.Body.Replace("{userName}", orderUserInfo.Name);
                        body = body.Replace("{orderId}", order.VoucherNo);
                        body = body.Replace("{paymentServiceName}", paymentServiceName);
                        
                        Models.Notification notification = new Models.Notification();
                        notification.Title = notiTemplate.Title;
                        notification.Body =string.Format("{0} မှ အော်ဒါနံပါတ် {1} အတွက် {2} ဖြင့် ငွေပေးချေခဲ့သည်",order.OrderUserId,order.VoucherNo,paymentServiceName);
                        notification.UserId = seller.Id; //userId;
                        notification.ImgUrl = QueenOfDreamerConst.AWS_USER_PROFILE_PATH + order.OrderUserId + ".png";
                        notification.RedirectAction = QueenOfDreamerConst.NOTI_REDIRECT_ACTION_ORDER_DETAIL;
                        notification.ReferenceAttribute = order.Id.ToString();
                        notification.CreatedDate = DateTime.Now;
                        notification.CreatedBy = currentUserLogin;
                        await _context.Notification.AddAsync(notification);
                        await _context.SaveChangesAsync();                            
                        var test = Helpers.Notification.SendFCMNotification(seller.Id.ToString(),
                                                                        notiTemplate.Title,
                                                                        body, seller.Id,
                                                                        QueenOfDreamerConst.NOTI_REDIRECT_ACTION_ORDER_DETAIL,
                                                                        order.Id,
                                                                        notification.Id,true);  // true for sending noti to seller
                        test.GetAwaiter().GetResult();

                    }  
                    #endregion              

                }
                else
                {
                    response.Message = "Invalid UserId or OrderId";
                }
            }
            return response;
        }
        public async Task<ResponseStatus> PaymentApprove(PaymentApproveRequest request, int currentUserLogin,int platform)
        {
            ResponseStatus response = new ResponseStatus();
            var order = await _context.Order.Where(x => x.Id == request.OrderId && x.OrderUserId == currentUserLogin).FirstOrDefaultAsync();
            if (order != null)
            {
                var orderPaymentInfo = await _context.OrderPaymentInfo.Where(x => x.OrderId == order.Id && x.Id == request.OrderPaymentInfoId && x.IsApproved == false).FirstOrDefaultAsync();
                if (orderPaymentInfo != null)
                {
                    orderPaymentInfo.IsApproved = true;
                    orderPaymentInfo.PaymentStatusId = QueenOfDreamerConst.PAYMENT_STATUS_SUCCESS;
                    order.OrderStatusId = QueenOfDreamerConst.ORDER_STATUS_ORDER;
                    await _context.SaveChangesAsync();
                    response.StatusCode = StatusCodes.Status200OK;
                    response.Message = "Success";
                }
                else
                {
                    response.Message = "Not found";
                }
            }
            return response;
        }
        public async Task<GetOrderDetailResponse> GetOrderDetail(int orderId,string token)
        {
            bool isZawgyi=Rabbit.IsZawgyi(_httpContextAccessor);

            List<OrderIdList> orderIdList = new List<OrderIdList>();
            var resp = await _context.Order.Include(a=>a.OrderDeliveryInfo).Where(x => x.Id == orderId).Select(ord => new GetOrderDetailResponse
            {
                OrderId=ord.Id,
                VoucherNo = ord.VoucherNo,
                OrderDate = ord.OrderDate,
                TotalAmt = ord.TotalAmt,
                NetAmt=ord.NetAmt,
                DeliveryFee=ord.DeliveryFee,
                UserId = ord.OrderUserId,
                IsDeleted = ord.IsDeleted,
                OrderCancelDate = ord.UpdatedDate,
                OrderCancelBy = ord.UpdatedBy.ToString(),
                OrderStatus = new GetOrderDetailOrderStatus
                {
                    Id = ord.OrderStatusId,
                    Name =isZawgyi?Rabbit.Uni2Zg(ord.OrderStatus.Name):ord.OrderStatus.Name
                },
                OrderItem = null,
                DeliveryInfo = new GetOrderDetailDeliveryInfo
                {
                    Name =isZawgyi?Rabbit.Uni2Zg(ord.OrderDeliveryInfo.Name):ord.OrderDeliveryInfo.Name,
                    Address =isZawgyi?Rabbit.Uni2Zg(ord.OrderDeliveryInfo.Address):ord.OrderDeliveryInfo.Address,
                    PhNo = ord.OrderDeliveryInfo.PhNo,
                    Remark =isZawgyi?Rabbit.Uni2Zg(ord.OrderDeliveryInfo.Remark):ord.OrderDeliveryInfo.Remark,
                    CityId = ord.OrderDeliveryInfo.CityId,
                    TownshipId = ord.OrderDeliveryInfo.TownshipId,
                    DeliveryDate=ord.OrderDeliveryInfo.DeliveryDate.ToString("dd MMM yyyy") + "(" + ord.OrderDeliveryInfo.FromTime + " - " + ord.OrderDeliveryInfo.ToTime + ")",
                    DeliveryServiceId = ord.OrderDeliveryInfo.DeliveryServiceId,
                    DeliveryService = new GetOrderDeailDeliveryService(){
                                    Id = ord.OrderDeliveryInfo.DeliveryServiceId,
                                    Name =isZawgyi?Rabbit.Uni2Zg(ord.OrderDeliveryInfo.DeliveryServiceName): ord.OrderDeliveryInfo.DeliveryServiceName,
                                    FromEstDeliveryDay = ord.OrderDeliveryInfo.FromEstDeliveryDay,
                                    ToEstDeliveryDay = ord.OrderDeliveryInfo.ToEstDeliveryDay,
                                    ImgPath = ord.OrderDeliveryInfo.LogoPath,
                                    ServiceAmount = ord.OrderDeliveryInfo.DeliveryFee
                                    }
                },
                PaymentInfo = null
            }).FirstOrDefaultAsync();


            if (resp.OrderCancelBy == "0")
            {
                resp.OrderCancelBy = "buyer";
            }
            else if (resp.OrderCancelBy == "1")
            {
                resp.OrderCancelBy = "seller";
            }
            // resp.OrderIdList = orderIdList;
            // resp.OrderCount = orderCount;
            resp.UserUrl =QueenOfDreamerConst.AWS_USER_PROFILE_PATH + resp.UserId + ".png";
            if (resp.DeliveryInfo != null)
            {
                // var deliveryInfo = await _deliServices.GetDeliveryServiceInfo(token,resp.DeliveryInfo.DeliveryServiceId);
                // resp.DeliveryInfo.DeliveryService.Name = deliveryInfo.Name;
                // resp.DeliveryInfo.DeliveryService.ImgPath = 
                resp.DeliveryInfo.CityId = resp.DeliveryInfo.CityId;
                resp.DeliveryInfo.TownshipId = resp.DeliveryInfo.TownshipId;
                resp.DeliveryInfo.CityName =await _deliServices.GetCityName(token,resp.DeliveryInfo.CityId);
                resp.DeliveryInfo.TownshipName =await _deliServices.GetTownshipName(token,resp.DeliveryInfo.TownshipId);

                resp.DeliveryInfo.CityName=isZawgyi?Rabbit.Uni2Zg(resp.DeliveryInfo.CityName):resp.DeliveryInfo.CityName;
                resp.DeliveryInfo.TownshipName=isZawgyi?Rabbit.Uni2Zg(resp.DeliveryInfo.TownshipName):resp.DeliveryInfo.TownshipName;
            }

            resp.PaymentInfo = await _context.OrderPaymentInfo.Where(p => p.OrderId == orderId)
                .Select(itm => new GetOrderDeailOrderPaymentInfo
                {
                    Id = itm.Id,
                    TransactionDate = itm.TransactionDate,
                    PhoneNo = itm.PhoneNo,
                    IsApproved = itm.IsApproved,
                    ApproveImg =itm.ApprovalImgUrl, //_context.PaySlip.Where(a=>a.OrderPaymentInfoId==itm.Id).Select(a=>a.Url).ToArray(),
                    Remark =isZawgyi?Rabbit.Uni2Zg(itm.Remark):itm.Remark,  
                    SellerRemark=isZawgyi?Rabbit.Uni2Zg(itm.SellerRemark):itm.SellerRemark,
                    BankId=itm.BankId==null?0:itm.BankId,
                    BankName=itm.Bank.Name==null?" ":itm.Bank.Name,
                    BankLogo=itm.Bank.Url==null?" ":itm.Bank.Url,                  
                    PaymentStatus = new GetOrderDetailPaymentStatus
                    {
                        Id = itm.PaymentStatus.Id,                        
                        Name =isZawgyi?Rabbit.Uni2Zg(itm.PaymentStatus.Name): itm.PaymentStatus.Name
                    },
                    PaymentService = new GetOrderDeailPaymentService
                    {
                        Name =isZawgyi?Rabbit.Uni2Zg(itm.PaymentService.Name): itm.PaymentService.Name,
                        BankName= _context.Bank.Where(x=>x.Id==itm.BankId).Select(x=>x.Name).FirstOrDefault(),
                        ImgPath = itm.PaymentService.ImgPath
                    }
                }).ToListAsync();

            if (resp.PaymentInfo.Count == 0)
            {
                GetOrderDeailOrderPaymentInfo orderPaymentInfoRes = new GetOrderDeailOrderPaymentInfo();
                GetOrderDeailPaymentService paymentService1 = new GetOrderDeailPaymentService();
                paymentService1.Name =isZawgyi?Rabbit.Uni2Zg("အိမ်အရောက် ငွေရှင်းစနစ်ဖြင့် ဝယ်ယူထားပါသည်"): "အိမ်အရောက် ငွေရှင်းစနစ်ဖြင့် ဝယ်ယူထားပါသည်";
                orderPaymentInfoRes.PaymentService = paymentService1;
                GetOrderDetailPaymentStatus PaymentStatus1 = new GetOrderDetailPaymentStatus();
                PaymentStatus1.Id = 0;
                PaymentStatus1.Name = "";
                orderPaymentInfoRes.PaymentStatus=PaymentStatus1;
                resp.PaymentInfo.Add(orderPaymentInfoRes);
            }

            resp.OrderItem = await _context.OrderDetail.Where(ordd => ordd.OrderId == orderId)
                .Select(itm => new GetOrderDetailOrderItem
                {
                    Id = itm.ProductId,
                    SkuId = itm.SkuId,
                    Url = _context.ProductImage.Where(prdImg => prdImg.ProductId == itm.ProductId && prdImg.isMain == true).Select(x => x.Url).FirstOrDefault(),
                    Name = _context.Product.Where(prd => prd.Id == itm.ProductId).Select(x =>isZawgyi?Rabbit.Uni2Zg(x.Name):x.Name).FirstOrDefault(),
                    Qty = itm.Qty,
                    OriginalPrice = itm.OriginalPrice,
                    // PromotePrice=_context.ProductPromotion.Where(p => p.ProductId == itm.ProductId).Select(p => p.TotalAmt).FirstOrDefault(),
                    // PromotePercent=_context.ProductPromotion.Where(p => p.ProductId == itm.ProductId).Select(p => p.Percent).FirstOrDefault(),
                    PromotePrice=itm.Price,
                    PromotePercent=itm.PromotePercent,
                    
                }).ToListAsync();

            foreach (var item in resp.OrderItem)
            {
                var skuValue = await (from psku in _context.ProductSkuValue
                                      from pvopt in _context.ProductVariantOption
                                      where psku.ProductId == item.Id
                                      && psku.SkuId == item.SkuId
                                      && psku.ProductId == pvopt.ProductId
                                      && psku.VariantId == pvopt.VariantId
                                      && psku.ValueId == pvopt.ValueId
                                      select isZawgyi?Rabbit.Uni2Zg(pvopt.ValueName):pvopt.ValueName).ToListAsync();

                item.SkuValue = string.Join(",", skuValue);

                // #region GetPromotion
                // var productPromote = await _context.ProductPromotion
                //                                 .Where(x => x.ProductId == item.Id)
                //                                 .FirstOrDefaultAsync();
                               
                // int promotePercent=0;
                // double promotePrice=0;
                // if(productPromote!=null && productPromote.Percent>0)
                // {
                //     promotePercent=productPromote.Percent;
                //     double discountPrice= double.Parse((((double)productPromote.Percent/(double)100)*(double)item.OriginalPrice).ToString("0.00"));
                //     promotePrice=item.OriginalPrice-discountPrice;
                //     item.PromotePercent=promotePercent;
                //     item.PromotePrice=promotePrice;
                // }
                
                // #endregion
            }

            resp.PaymentService = await _context.PaymentService.Where(x => x.Id != QueenOfDreamerConst.PAYMENT_SERVICE_COD && x.IsActive == true)
                .Select(s => new GetCartDetailPaymentService
                {
                    Id = s.Id,
                    ImgUrl = s.ImgPath,
                    Name =isZawgyi?Rabbit.Uni2Zg(s.Name): s.Name
                }).ToListAsync();
            
            resp.NewPaymentService=await _miscellaneousRepo.GetPaymentServiceForBuyer();

            return resp;            
        }
        public async Task<PostOrderByKBZPayResponse> PostOrderByKBZPay(PostOrderRequest req, int userId, string token)
        {
            bool isZawgyi=Rabbit.IsZawgyi(_httpContextAccessor);

            PostOrderByKBZPayResponse response = new PostOrderByKBZPayResponse(){
                        OrderId=0,
                        Timestamp=0,
                        NonceStr="",
                        TransactionId="",
                        Precreate=new KBZPrecreateResponse(){
                            Response=new Response(){
                                result="",
                                code="",
                                msg="",
                                merch_order_id="",
                                prepay_id="",
                                nonce_str="",
                                sign_type="",
                                sign=""
                            }
                        }
                    };

            #region Check qty before order

                    var issueList=new List<ProductIssues>();
                    if(req.ProductInfo.Count==0)
                    {
                         var issue=new ProductIssues(){
                                    ProductId=0,
                                    ProductName="",
                                    Action="ItemNotFound",
                                    Qty=0,
                                    Reason="There's no item to make an order"
                                };
                                issueList.Add(issue);
                    }
                    foreach (var item in req.ProductInfo)
                    {
                        var product=await _context.Product.Where(x=>x.Id==item.ProductId).SingleOrDefaultAsync();
                        var skuProductQty = await _context.ProductSku.Where(x => x.ProductId == item.ProductId && x.SkuId == item.SkuId).FirstOrDefaultAsync();
                        if(!product.IsActive)
                        {
                             var issue=new ProductIssues(){
                                    ProductId=item.ProductId,
                                    ProductName=isZawgyi?Rabbit.Uni2Zg(product.Name):product.Name,
                                    Action="Delete",
                                    Qty=skuProductQty.Qty,
                                    Reason=string.Format("Your order item - {0} has been deleted by seller.",product.Name)
                                };
                                issueList.Add(issue);
                        }
                        
                        else if (skuProductQty != null)
                        {
                            if(item.Qty>skuProductQty.Qty){//Check if add to cart qty > stock qty. Can't make order
                               
                                var issue=new ProductIssues(){
                                    ProductId=item.ProductId,
                                    ProductName=isZawgyi?Rabbit.Uni2Zg(product.Name):product.Name,
                                     Action="OutOfStock",
                                    Qty=skuProductQty.Qty,
                                    Reason=string.Format("You'er order {0} of {1}, but we only have {2} left.",(item.Qty>1?item.Qty+" quantities" : item.Qty+" quantity" ),(product.Name),(skuProductQty.Qty>1?skuProductQty.Qty+" quantities" : skuProductQty.Qty+" quantity" ))
                                };
                                issueList.Add(issue);                                
                            }            
                        }                         
                    }
                    if(issueList.Count()>0)
                    {
                        response.OrderId = 0;
                        response.StatusCode=StatusCodes.Status400BadRequest;
                        response.ProductIssues=issueList;
                        return response;
                    }  

                    #endregion
                    

            var transactionID= System.Guid.NewGuid().ToString()+QueenOfDreamerConst.APPLICATION_CONFIG_ID;
            OrderTransaction transaction = new OrderTransaction(){
                Id= transactionID,
                TransactionData = JsonConvert.SerializeObject(req),
                CreatedBy = userId,
                CreatedDate = DateTime.Now
            };
            _context.OrderTransaction.Add(transaction);
            await _context.SaveChangesAsync();

            response = await _paymentservices.KBZPrecreate(transactionID,req.NetAmt,req.Platform);
            response.TransactionId = transaction.Id;
            response.ProductIssues=new List<ProductIssues>();
            return response;
        }
        public async Task<PostOrderResponse> CheckKPayStatus(string transactionId, int userId, string token,int platform)
        {  

            var transaction = await _context.OrderTransaction.Where(x => x.Id == transactionId).SingleOrDefaultAsync();
            if (transaction != null)
            {
                var kbzQueryOrder = await _paymentservices.KBZQueryOrder(transactionId); 
                if (kbzQueryOrder != null)
                {
                    if (kbzQueryOrder.Response.result == "SUCCESS" && kbzQueryOrder.Response.trade_status == "PAY_SUCCESS")
                    {
                        PostOrderRequest  request= Newtonsoft.Json.JsonConvert.DeserializeObject<PostOrderRequest>(transaction.TransactionData);
                        var imgUrlResponseList = new List<ImageUrlResponse>();
                        request.PaymentInfo.ApprovalImage =new PostOrderPaymentImgage(){ApprovalImage="",ApprovalImageExtension="png"}; //new List<PostOrderPaymentImgage>();
                        PostOrderResponse resp =await PostOrder(request,userId,token,platform) ;
                        if(resp.StatusCode==StatusCodes.Status200OK)
                        {
                            transaction.OrderId=resp.OrderId;
                            transaction.mm_order_id=kbzQueryOrder.Response.mm_order_id;
                            await _context.SaveChangesAsync();
                            
                            resp.OrderId=resp.OrderId;
                            resp.StatusCode=StatusCodes.Status200OK;
                            resp.Message="အောင်မြင်သည်။";
                            return resp;
                        }
                        else{
                            transaction.mm_order_id=kbzQueryOrder.Response.mm_order_id;
                            await _context.SaveChangesAsync();
                            resp.StatusCode=StatusCodes.Status400BadRequest;
                            resp.Message="မအောင်မြင်ပါ။";
                            return resp;
                        }
                    }else
                    {
                        // response.StatusCode=StatusCodes.Status400BadRequest;
                        // response.Message="မအောင်မြင်ပါ။";
                         return null;
                    }
                }
            }
            else{
                // response.StatusCode=StatusCodes.Status400BadRequest;
                // response.Message="မအောင်မြင်ပါ။";
            }
            return null;
        }
        public async Task<List<string>> GetVoucherNoSuggestion(GetVoucherNoSuggestionRequest request)
        {
            return await _context.Order.Where(x=>x.VoucherNo.Contains(request.SearchText) && x.OrderUserId==request.UserId).OrderBy(x=>x.VoucherNo).Select(x=>x.VoucherNo)
            .Skip((request.PageNumber-1)).Take(request.PageSize).ToListAsync();
        }
        public async Task<List<string>> GetVoucherNoSuggestionSeller(GetVoucherNoSuggestionSellerRequest request)
        {
            return await _context.Order.Where(x=>x.VoucherNo.Contains(request.SearchText)).OrderBy(x=>x.VoucherNo).Select(x=>x.VoucherNo)
            .Skip((request.PageNumber-1)).Take(request.PageSize).ToListAsync();
        }
        public async Task<GetVoucherResponse> GetVoucher(int OrderId,string token)
        {
           bool isZawgyi=Rabbit.IsZawgyi(_httpContextAccessor);

            GetVoucherResponse response = new GetVoucherResponse();

            //Order
            var order=await _context.Order
            .Where(x=>x.Id==OrderId)
            .SingleOrDefaultAsync();

            //User
            var userInfo=await _userServices.GetUserInfo(order.OrderUserId,token);

            //Payment Info
            var paymentInfo=await _context.OrderPaymentInfo
                                .Where(x=>x.OrderId==OrderId)
                                .OrderByDescending(x=>x.TransactionDate)                                
                                .FirstOrDefaultAsync();

            var paymentServices=await _context.PaymentService
                            .Where(x=>x.Id==paymentInfo.PaymentServiceId)
                            .SingleOrDefaultAsync();
            var bank="";
            if(paymentInfo.BankId!=null && paymentInfo.BankId>0)
            {
                bank=await _context.Bank
                    .Where(x=>x.Id==paymentInfo.BankId)
                    .Select(x=>x.Name)
                    .SingleOrDefaultAsync();
            }

            //Order detail
            var orderDetail=await _context.OrderDetail
                            .Where(x=>x.OrderId==OrderId)
                            .Select(x=>new GetVoucherItem{
                             Qty=x.Qty,
                             Price=x.Price,
                             ProductId=x.ProductId,
                             SkuId=x.SkuId,
                             OriginalPrice=(x.Qty * x.OriginalPrice),
                             PromotePrice=(x.Qty * x.Price),
                             PromotePercent=int.Parse(x.PromotePercent.ToString()),
                            })
                            .ToListAsync();

            double totalAmount=0;
            
            foreach (var item in orderDetail)
            {
                var skuValue = await (from psku in _context.ProductSkuValue
                                      from pvopt in _context.ProductVariantOption
                                      where psku.ProductId == item.ProductId
                                      && psku.SkuId == item.SkuId
                                      && psku.ProductId == pvopt.ProductId
                                      && psku.VariantId == pvopt.VariantId
                                      && psku.ValueId == pvopt.ValueId
                                      select isZawgyi?Rabbit.Uni2Zg(pvopt.ValueName):pvopt.ValueName).ToListAsync();

                item.Sku = string.Join(",", skuValue);
                item.Name=await _context.Product
                        .Where(x=>x.Id==item.ProductId)
                        .Select(x=>isZawgyi?Rabbit.Uni2Zg(x.Name):x.Name)
                        .SingleOrDefaultAsync();  

                totalAmount=totalAmount+item.OriginalPrice;                 
            }

            //Commercial tax
            double commercialTax=0;
            if(QueenOfDreamerConst.TAX_COMMERCIAL_TAX>0)
            {
                commercialTax= double.Parse((((double)QueenOfDreamerConst.TAX_COMMERCIAL_TAX/(double)100)*(double)order.NetAmt).ToString("0.00"));
            }  

            response.ShopUrl=QueenOfDreamerConst.COMPANY_SHOP_URL;
            response.ShopName=QueenOfDreamerConst.COMPANY_SHOP_NAME;
            response.Address=QueenOfDreamerConst.COMPANY_SHOP_ADDRESS;
            response.PhoneNo=QueenOfDreamerConst.COMPANY_PHONE_NO;
            response.BuyerName=userInfo==null?"": (isZawgyi?Rabbit.Uni2Zg(userInfo.Name):userInfo.Name);
            response.BuyerPhoneNo=userInfo==null?"":userInfo.PhoneNo;
            response.BuyerAddress=userInfo==null?"":(isZawgyi?Rabbit.Uni2Zg(userInfo.Address):userInfo.Address);
            response.BuyerRemark=isZawgyi?Rabbit.Uni2Zg(paymentInfo.Remark):paymentInfo.Remark;
            response.VoucherNo=order.VoucherNo;
            response.OrderDate=order.OrderDate;
            response.TotalAmount=totalAmount;
            response.DeliveryAmount=order.DeliveryFee;
            response.CommercialTax=commercialTax;
            response.NetAmount=order.NetAmt + commercialTax;
            response.Discount=_context.OrderDetail.Where(x=>x.OrderId==OrderId).Sum(x=>x.Discount*x.Qty);
            response.PaymentType=isZawgyi?Rabbit.Uni2Zg(paymentServices.Name):paymentServices.Name;
            response.BankName=bank;
            response.QRCode=QRCodeHelper.GenerateQRCode(QueenOfDreamerConst.COMPANY_ORDER_DETAIL_URL+OrderId);
            response.ItemList=orderDetail;

            return response;
        }
        public async Task<GetPOSVoucherResponse> GetPOSVoucher(int OrderId,int userId,string token)
        {
              bool isZawgyi=Rabbit.IsZawgyi(_httpContextAccessor);

            GetPOSVoucherResponse response = new GetPOSVoucherResponse();

            //Order
            var order=await _context.Order
            .Where(x=>x.Id==OrderId)
            .SingleOrDefaultAsync();

            //User
            var userInfo=await _userServices.GetUserInfo(userId,token);

            //Payment Info
            var paymentInfo=await _context.OrderPaymentInfo
                                .Where(x=>x.OrderId==OrderId)
                                .OrderByDescending(x=>x.TransactionDate)                                
                                .FirstOrDefaultAsync();

            var paymentServices=await _context.PaymentService
                            .Where(x=>x.Id==paymentInfo.PaymentServiceId)
                            .SingleOrDefaultAsync();
            var bank="";
            if(paymentInfo.BankId!=null && paymentInfo.BankId>0)
            {
                bank=await _context.Bank
                    .Where(x=>x.Id==paymentInfo.BankId)
                    .Select(x=>x.Name)
                    .SingleOrDefaultAsync();
            }

            //Order detail
            var orderDetail=await _context.OrderDetail
                            .Where(x=>x.OrderId==OrderId)
                            .Select(x=>new GetPOSVoucherItem{
                             Qty=x.Qty,
                             Price=x.Price,
                             ProductId=x.ProductId,
                             SkuId=x.SkuId,
                             OriginalPrice=x.OriginalPrice * x.Qty,
                             PromotePrice=x.Price * x.Qty,
                             PromotePercent=int.Parse(x.PromotePercent.ToString()),
                            })
                            .ToListAsync();
            double totalAmount=0;
            foreach (var item in orderDetail)
            {
                var skuValue = await (from psku in _context.ProductSkuValue
                                      from pvopt in _context.ProductVariantOption
                                      where psku.ProductId == item.ProductId
                                      && psku.SkuId == item.SkuId
                                      && psku.ProductId == pvopt.ProductId
                                      && psku.VariantId == pvopt.VariantId
                                      && psku.ValueId == pvopt.ValueId
                                      select isZawgyi?Rabbit.Uni2Zg(pvopt.ValueName):pvopt.ValueName).ToListAsync();

                item.Sku = string.Join(",", skuValue);
                item.Name=await _context.Product
                        .Where(x=>x.Id==item.ProductId)
                        .Select(x=>isZawgyi?Rabbit.Uni2Zg(x.Name):x.Name)
                        .SingleOrDefaultAsync();  
                totalAmount=totalAmount+item.OriginalPrice;
            }

            //Commercial tax
            double commercialTax=0;
            if(QueenOfDreamerConst.TAX_COMMERCIAL_TAX>0)
            {
                commercialTax= double.Parse((((double)QueenOfDreamerConst.TAX_COMMERCIAL_TAX/(double)100)*(double)order.NetAmt).ToString("0.00"));
            }            

            response.ShopName=QueenOfDreamerConst.COMPANY_SHOP_NAME;
            response.Address=QueenOfDreamerConst.COMPANY_SHOP_ADDRESS;
            response.PhoneNo=QueenOfDreamerConst.COMPANY_PHONE_NO;
            response.VoucherNo=order.VoucherNo;
            response.OrderId=order.Id;
            response.OrderDate=order.OrderDate;
            response.TotalAmount=totalAmount;
            response.DeliveryAmount=order.DeliveryFee;
            response.NetAmount=order.NetAmt + commercialTax;
            response.Discount=_context.OrderDetail.Where(x=>x.OrderId==OrderId).Sum(x=>x.Discount * x.Qty);
            response.PaymentType=isZawgyi?Rabbit.Uni2Zg(paymentServices.Name):paymentServices.Name;
            response.BankName=bank;
            response.TaxplayerId=QueenOfDreamerConst.TAX_TAXPAYER_ID;
            response.Cashier=userInfo==null?"":(isZawgyi?Rabbit.Uni2Zg(userInfo.Name):userInfo.Name);
            response.Changed=0;
            response.CommercialTax=commercialTax;
            response.ItemList=orderDetail;
            response.QRCode=QRCodeHelper.GenerateQRCode(QueenOfDreamerConst.COMPANY_ORDER_DETAIL_URL+OrderId);           

            return response;
        }
        public async Task<bool> CallBackKPayNotify(KBZNotifyRequest request)
        {
            var trans=await _context.OrderTransaction
            .Where(x=>x.Id==request.Request.merch_order_id)
            .SingleOrDefaultAsync();
            if(trans!=null && trans.OrderId!=null && trans.OrderId>0)
            {
                return await _context.Order
                .AnyAsync(x=>x.Id==trans.OrderId);
            }
            else{
                return false;
            }
        }

        //------wave-------//
        public async Task<PostOrderByWavePayResponse> PostOrderByWavePay(PostOrderRequest req, int userId, string token,int platform)
        {
            var transactionID = System.Guid.NewGuid().ToString()+QueenOfDreamerConst.APPLICATION_CONFIG_ID;
            OrderTransaction transaction = new OrderTransaction(){
                Id= transactionID,
                TransactionData = JsonConvert.SerializeObject(req),
                CreatedBy = userId,
                CreatedDate = DateTime.Now
            };
            _context.OrderTransaction.Add(transaction);
            await _context.SaveChangesAsync();
            List<ProductItem> items = new List<ProductItem>();
            foreach (var item in req.ProductInfo)
            {
                 var productSku=await _context.ProductSku
                .Where(x=>x.ProductId==item.ProductId
                && x.SkuId==item.SkuId)
                .SingleOrDefaultAsync();
                        
                #region GetPromotion
                var productPromote=_context.ProductPromotion.Where(x=>x.ProductId==item.ProductId).FirstOrDefault();
                
                if(productPromote!=null && productPromote.Percent>0)
                {
                    double discountPrice= double.Parse((((double)productPromote.Percent/(double)100)*(double)productSku.Price).ToString("0.00"));
                    item.Price=item.Price-discountPrice;                    
                }
                else{
                    item.Price=productSku.Price;
                }
                                
                #endregion

                string productName = "";
                int amt = 0;
                var product = new ProductItem();
                if (item.Qty > 1)
                {
                    productName = _context.Product.Where(x => x.Id == item.ProductId).Select(x => x.Name).SingleOrDefault();
                    productName = productName+ "  x  "+item.Qty;
                    amt = Convert.ToInt32(item.Price * item.Qty);
                    product.name = productName;
                    product.amount = amt;
                }else{
                    productName = _context.Product.Where(x => x.Id == item.ProductId).Select(x => x.Name).SingleOrDefault();
                    amt = Convert.ToInt32(item.Price);
                    product.name = productName;
                    product.amount = amt;
                }
                items.Add(product);
            }
            if (req.DeliveryFee > 0)
            {
                items.Add(new ProductItem{name = "Delivery Fee", amount = Convert.ToInt32(req.DeliveryFee)});
            }
            var payment_description =string.IsNullOrEmpty(req.PaymentInfo.Remark)?"-":req.PaymentInfo.Remark;

            PostOrderByWavePayResponse response = await _paymentservices.WavePayPrecreate(transactionID,req.NetAmt,items,payment_description,platform);
            return response;
        }
        public async Task<PostOrderResponse> CheckWaveTransactionStatus(CheckWaveTransactionStatusRequest request,int platform)
        {
            var resp = new PostOrderResponse();
            if (request.status == "PAYMENT_CONFIRMED")
            {
                var transaction = await _context.OrderTransaction.Where(x => x.Id == request.merchantReferenceId).SingleOrDefaultAsync();
                var has = _paymentservices.GenerateSHA256Hash_WaveTransaction(request);
                if (request.hashValue == has)
                {
                    PostOrderRequest req= Newtonsoft.Json.JsonConvert.DeserializeObject<PostOrderRequest>(transaction.TransactionData);
                        var imgUrlResponseList = new List<ImageUrlResponse>();
                        req.PaymentInfo.ApprovalImage =new PostOrderPaymentImgage(){ApprovalImage="",ApprovalImageExtension="png"}; //new List<PostOrderPaymentImgage>();

                        resp =await PostOrder(req,transaction.CreatedBy,null,platform) ;
                        if(resp.StatusCode==StatusCodes.Status200OK)
                        {
                            transaction.OrderId=resp.OrderId;
                            transaction.mm_order_id=request.paymentRequestId;
                            await _context.SaveChangesAsync();
                            
                            // transaction.OrderId=resp.OrderId;
                            // response.StatusCode=StatusCodes.Status200OK;
                            // response.Message="အောင်မြင်သည်။";
                            return resp;
                        }
                        else{
                            transaction.mm_order_id=request.paymentRequestId;
                            await _context.SaveChangesAsync();
                            // response.StatusCode=StatusCodes.Status400BadRequest;
                            // response.Message="မအောင်မြင်ပါ။";
                            return resp;
                        }
                }
            }else{
                var removetransaction = await _context.OrderTransaction.Where(x => x.Id == request.merchantReferenceId).FirstOrDefaultAsync();
                _context.OrderTransaction.Remove(removetransaction);
                resp.StatusCode=StatusCodes.Status400BadRequest;
                resp.Message="မအောင်မြင်ပါ။";
            }
            return resp;
        }
        
        public async Task<GetOrderIdByTransactionIdResponse> GetOrderIdByTransactionId(string transactionId, string token)
        {
            var resp = new GetOrderIdByTransactionIdResponse();
            var orderId =await _context.OrderTransaction.Where(x => x.Id == transactionId).Select(x => x.OrderId).FirstOrDefaultAsync();
            if (orderId != null)
            {
                resp.OrderId = orderId;
                resp.Message = "Success";
                resp.StatusCode = StatusCodes.Status200OK;
            }else{
                resp.OrderId = null;
                resp.Message = "Failed";
                resp.StatusCode = StatusCodes.Status400BadRequest;
            }
            return resp;
        }
        
        public async Task<GetOrderDetailResponse> GetOrderDetailByTransactionId(string transactionId, string token)
        {
            var resp = new GetOrderDetailResponse();
            var orderId = _context.OrderTransaction.Where(x => x.Id == transactionId).Select(x => x.OrderId).FirstOrDefault();
            if (orderId != null)
            {
                resp = await GetOrderDetail(Convert.ToInt32(orderId),token);
            }
            return resp;
        }

        public async Task<List<GetDeliveryAddressResponse>> GetDeliveryAddress(int userId,string token)
        {
            bool isZawgyi=Rabbit.IsZawgyi(_httpContextAccessor);
            var deliAdd = await _context.UserDeliveryAddress.Where(a => a.UserId == userId)
                                                    .Select(a => new GetDeliveryAddressResponse{
                                                        Id = a.Id,
                                                        CityId = a.CityId,
                                                        TownshipId = a.TownshipId,
                                                        LabelName = a.LabelName,
                                                        Landmark = a.Landmark,
                                                        Address = a.Address
                                                    }).ToListAsync();
            if (deliAdd.Count > 0)
            {
                foreach (var item in deliAdd)
                {
                    string cityName=await _deliServices.GetCityName(token,item.CityId);
                    string townshipName=await _deliServices.GetTownshipName(token,item.TownshipId);

                    cityName=isZawgyi?Rabbit.Uni2Zg(cityName):cityName;
                    townshipName=isZawgyi?Rabbit.Uni2Zg(townshipName):townshipName;

                    item.CityName = cityName;
                    item.TownshipName = townshipName;
                }
            }

            return deliAdd;
        }

        public async Task<GetDeliveryAddressResponse> GetDeliveryAddressDetail(int DeliveryAddressId ,int userId,string token)
        {
            bool isZawgyi=Rabbit.IsZawgyi(_httpContextAccessor);
            var deliAdd = await _context.UserDeliveryAddress.Where(a => a.UserId == userId && a.Id == DeliveryAddressId)
                                                    .Select(a => new GetDeliveryAddressResponse{
                                                        Id = a.Id,
                                                        CityId = a.CityId,
                                                        TownshipId = a.TownshipId,
                                                        LabelName = a.LabelName,
                                                        Landmark = a.Landmark,
                                                        Address = a.Address
                                                    }).SingleOrDefaultAsync();

            if (deliAdd != null)
            {
                string cityName=await _deliServices.GetCityName(token,deliAdd.CityId);
                string townshipName=await _deliServices.GetTownshipName(token,deliAdd.TownshipId);

                cityName=isZawgyi?Rabbit.Uni2Zg(cityName):cityName;
                townshipName=isZawgyi?Rabbit.Uni2Zg(townshipName):townshipName;

                deliAdd.CityName = cityName;
                deliAdd.TownshipName = townshipName;
            }

            return deliAdd;
        }

        public async Task<ResponseStatus> CreateUserDeliveryAddress(CreateUserDeliveryAddressRequest req,int userId)
        {
            bool isZawgyi=Rabbit.IsZawgyi(_httpContextAccessor);
            if (!string.IsNullOrEmpty(req.Address))
            {   
                req.Address = isZawgyi?Rabbit.Uni2Zg(req.Address):req.Address;
            }
            if (!string.IsNullOrEmpty(req.Landmark))
            {   
                req.Landmark = isZawgyi?Rabbit.Uni2Zg(req.Landmark):req.Landmark;
            }

            var add = new UserDeliveryAddress{
                CityId = req.CityId,
                TownshipId = req.TownshipId,
                Address = req.Address,
                Landmark = req.Landmark,
                UserId = userId,
                LabelName = req.LabelName,
                IsSelected = false,
                CreatedBy = userId,
                CreatedDate = DateTime.Now
            };

            await _context.UserDeliveryAddress.AddAsync(add);
            await _context.SaveChangesAsync();

            return new ResponseStatus{
                Message = "Success",
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResponseStatus> UpdateUserDeliveryAddress(UpdateUserDeliveryAddressRequest req,int userId)
        {
            bool isZawgyi=Rabbit.IsZawgyi(_httpContextAccessor);
            if (!string.IsNullOrEmpty(req.Address))
            {   
                req.Address = isZawgyi?Rabbit.Uni2Zg(req.Address):req.Address;
            }
            if (!string.IsNullOrEmpty(req.Landmark))
            {   
                req.Landmark = isZawgyi?Rabbit.Uni2Zg(req.Landmark):req.Landmark;
            }

            var existingAdd = await _context.UserDeliveryAddress.Where(a => a.Id == req.Id && a.UserId == userId).FirstOrDefaultAsync();
            if (existingAdd != null)
            {
                if (req.isSelected)
                {
                    var selectedAdd = await _context.UserDeliveryAddress.Where(u => u.UserId == userId && u.IsSelected == true).FirstOrDefaultAsync();
                    if (selectedAdd != null)
                    {
                        selectedAdd.IsSelected = false;
                        await _context.SaveChangesAsync();
                    }
                }
                existingAdd.CityId = req.CityId;
                existingAdd.TownshipId = req.TownshipId;
                existingAdd.Address = req.Address;
                existingAdd.Landmark = req.Landmark;
                existingAdd.UserId = userId;
                existingAdd.LabelName = req.LabelName;
                existingAdd.IsSelected = req.isSelected;
                existingAdd.UpdatedDate = DateTime.Now;
                existingAdd.UpdatedBy = userId;

                await _context.SaveChangesAsync();
            }
            return new ResponseStatus{
                Message = "Success",
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResponseStatus> ConfirmSelectedAddress(GetDeliveryAddressRequest req,int userId)
        {   
            foreach (var trnCart in req.ProductCarts)
            {
                var trnCartRes = await _context.TrnCart.Where(x => x.UserId == userId && x.SkuId == trnCart.SkuId && x.ProductId == trnCart.ProductId).FirstOrDefaultAsync();
                if (trnCartRes != null)
                {
                    trnCartRes.Qty = trnCart.Qty;
                    await _context.SaveChangesAsync();
                }
            }
            var existingAdd = await _context.UserDeliveryAddress.Where(a => a.Id == req.DeliveryAddressId && a.UserId == userId).FirstOrDefaultAsync();
            if (existingAdd != null)
            {
                var selectedAdd = await _context.UserDeliveryAddress.Where(u => u.UserId == userId && u.IsSelected == true).FirstOrDefaultAsync();
                if (selectedAdd != null)
                {
                    selectedAdd.IsSelected = false;
                    await _context.SaveChangesAsync();
                }
                existingAdd.IsSelected = true;
                await _context.SaveChangesAsync();
            }
            return new ResponseStatus{
                Message = "Success",
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResponseStatus> DeleteUserDeliveryAddress(int DeliveryAddressId,int userId)
        {
            var existingAdd = await _context.UserDeliveryAddress.Where(a => a.Id == DeliveryAddressId && a.UserId == userId).FirstOrDefaultAsync();
            if (existingAdd != null)
            {
                _context.UserDeliveryAddress.Remove(existingAdd);
                await _context.SaveChangesAsync();
            }
            return new ResponseStatus{
                Message = "Success",
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<List<GetDeliveryAddressLabelResponse>> GetDeliveryAddressLabel()
        {
            var res = await _context.DeliveryAddressLabel.Select(a => new GetDeliveryAddressLabelResponse{
                Id = a.Id,
                Name = a.Name
            }).ToListAsync();

            return res;
        }
    }
} 