using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using QueenOfDreamer.API.Dtos;
using QueenOfDreamer.API.Dtos.ProductDto;
using QueenOfDreamer.API.Models;

namespace QueenOfDreamer.API.Interfaces.Repos
{
    public interface IProductRepository
    {
        Task<TrnProduct> CreateDemoProduct(int categoryId);
        Task<List<PopulateSkuResponse>> PopulateSku(PopulateSkuRequest req, Guid productId);
        Task<List<PopulateSkuResponse>> PopulateSkuImage(List<PopulateSkuResponse> resp,List<SkuImage> SkuImages);
        Task<ResponseStatus> CreateProduct(CreateProductRequest req, List<ImageUrlResponse> imageUrlList,int currentLoginID);        
        Task<ResponseStatus> UpdateProduct(UpdateProductRequest req, List<ImageUrlResponse> imageUrlList,int currentLoginID);   
        Task<AddSkuForUpdateProductResponse> AddSkuForUpdateProduct(AddSkuForUpdateProductRequest request,int currentLoginID);
        Task<ResponseStatus> DeleteSku(DeleteSkuRequest request);   
        Task<ProductImage> GetProductImageById(int id);
        Task<GetProductDetailResponse> GetProductDetail(GetProductDetailRequest request,int userId,string token);
        Task<List<GetLandingProductPromotionResponse>> GetLandingProductPromotion(GetLandingProductPromotionRequest request);
        Task<List<GetLandingProductLatestResponse>> GetLandingProductLatest(GetLandingProductLatestRequest request);
        Task<List<GetProductByRelatedCategryResponse>> GetProductByRelatedCategry(GetProductByRelatedCategryRequest request);
        Task<List<GetProductByRelatedTagResponse>> GetProductByRelatedTag(GetProductByRelatedTagRequest request);
        Task<GetLandingProductCategoryResponse> GetLandingProductCategory(GetLandingProductCategoryRequest request);
        Task<List<GetProductListResponse>> GetProductList(GetProductListRequest request);
        Task<ResponseStatus> DeleteProduct(DeleteProductRequest request);
        Task<ProductSearchResponse> ProductSearch(ProductSearchRequest request,int userId,int platform);
        Task<List<GetVariantByCategoryResponse>> GetVariantByCategoryId(int categoryId);
        Task ProductSkuHold(ProductSkuHoldRequest req);
        Task<List<GetProductSkuResponse>> GetProductSku(GetProductSkuRequest req);        
        Task<GetProductVariantResponse> GetProductVariant(GetProductSkuRequest req);
        Task<List<GetVariantValueResponse>> GetVariantValue(GetVariantValueRequest request);
        Task<List<GetProductNameSuggestionResponse>> GetProductNameSuggestion(GetProductNameSuggestionRequest request);
        Task<List<GetBestSellingProductResponse>> GetBestSellingProduct(GetBestSellingProductRequest request);
        Task<byte[]> DownloadProductUploadTemplate();
        Task<UploadProductResponse> UploadProduct(UploadProductRequest request,int currentLoginID);
        Task<GetAllProductListBuyerResponse> GetAllProductListBuyer(GetAllProductListBuyerRequest request);
        Task<List<ProductImage>> GetAllProductImageByProductId(int productId);
        Task<List<GetProductTypeResponse>> GetProductType();
        Task<ResponseStatus> UpdateProductStatus(UpdateProductStatusRequest request);
    }
}
