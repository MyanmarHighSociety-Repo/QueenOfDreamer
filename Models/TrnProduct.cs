using System;

namespace QueenOfDreamer.API.Models
{
    public class TrnProduct
    {
        public Guid Id { get; set; }

        public int ProductCategoryId { get; set; }

        public DateTime CreatedDate { get; set; }
    }
}