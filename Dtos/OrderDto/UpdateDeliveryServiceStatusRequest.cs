﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QueenOfDreamer.API.Dtos.OrderDto
{
    public class UpdateDeliveryServiceStatusRequest
    {
        public int OrderId { get; set; }
        public int DeliveryServiceStatusId { get; set; }
    }
}
