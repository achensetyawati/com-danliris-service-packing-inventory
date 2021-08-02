﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Com.Danliris.Service.Packing.Inventory.Application.ToBeRefactored.GarmentShipping.GarmentShippingInvoice
{
    public class ShippingPackingListViewModel
    {
        public int InvoiceId { get; set; }
        public string BuyerAgentCode { get; set; }
        public string BuyerAgentName { get; set; }
        public decimal Amount { get; set; }
    }
}
