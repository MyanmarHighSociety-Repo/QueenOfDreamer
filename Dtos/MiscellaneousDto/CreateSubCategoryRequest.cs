using System.Collections.Generic;

namespace QueenOfDreamer.API.Dtos.MiscellaneousDto
{
    public class CreateSubCategoryRequest
    {
        public int MainCategoryId {get;set;}
        public string Name {get;set;}
        public string Url {get;set;}
        public List<CreateSubCategoryVariant> VariantList{get;set;}
    }
    public class CreateSubCategoryVariant{
        public string Name {get;set;}
    }
}