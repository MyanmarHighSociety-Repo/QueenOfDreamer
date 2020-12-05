using System.Collections.Generic;

namespace QueenOfDreamer.API.Dtos.ProductDto
{
    public class UploadProductResponse : ResponseStatus
    {
        public List<UploadProductIssues> IssuesList{get;set;}
    }
    public class UploadProductIssues{
        public string ProductName {get;set;}
        public string Reason {get;set;}
    }
}