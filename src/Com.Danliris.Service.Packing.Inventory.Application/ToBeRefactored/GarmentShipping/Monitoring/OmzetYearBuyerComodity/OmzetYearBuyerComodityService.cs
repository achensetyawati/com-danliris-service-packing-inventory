﻿using Com.Danliris.Service.Packing.Inventory.Application.Utilities;
using Com.Danliris.Service.Packing.Inventory.Data.Models.Garmentshipping.GarmentShippingInvoice;
using Com.Danliris.Service.Packing.Inventory.Infrastructure.IdentityProvider;
using Com.Danliris.Service.Packing.Inventory.Infrastructure.Repositories.GarmentShipping.GarmentShippingInvoice;
using Com.Danliris.Service.Packing.Inventory.Infrastructure.Repositories.GarmentShipping.GarmentPackingList;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Com.Danliris.Service.Packing.Inventory.Application.ToBeRefactored.GarmentShipping.Monitoring.PackingList;
using System.IO;

namespace Com.Danliris.Service.Packing.Inventory.Application.ToBeRefactored.GarmentShipping.Monitoring.OmzetYearBuyerComodity
{
    public class OmzetYearBuyerComodityService : IOmzetYearBuyerComodityService
    {
        private readonly IGarmentShippingInvoiceRepository shippingInvoiceRepository;
        private readonly IGarmentShippingInvoiceItemRepository shippingInvoiceItemRepository;
        private readonly IGarmentPackingListRepository shippingpackinglistRepository;
        private readonly IIdentityProvider _identityProvider;

        public OmzetYearBuyerComodityService(IServiceProvider serviceProvider)
        {
            shippingInvoiceRepository = serviceProvider.GetService<IGarmentShippingInvoiceRepository>();
            shippingInvoiceItemRepository = serviceProvider.GetService<IGarmentShippingInvoiceItemRepository>();
            shippingpackinglistRepository = serviceProvider.GetService<IGarmentPackingListRepository>();

            _identityProvider = serviceProvider.GetService<IIdentityProvider>();
        }

        public IQueryable<OmzetYearBuyerComodityViewModel> GetData(int year)
        {
            DateTimeOffset dateFrom = new DateTimeOffset(year, 1, 1, 0, 0, 0, new TimeSpan(_identityProvider.TimezoneOffset, 0, 0));
            DateTimeOffset dateTo = new DateTimeOffset(year + 1, 1, 1, 0, 0, 0, new TimeSpan(_identityProvider.TimezoneOffset, 0, 0));

            var invoicequery = shippingInvoiceRepository.ReadAll();
            var invoiceitemquery = shippingInvoiceItemRepository.ReadAll();
            var packinglistquery = shippingpackinglistRepository.ReadAll()
                                   .Where(w => w.TruckingDate >= dateFrom && w.TruckingDate < dateTo);

            var newQ = (from a in packinglistquery
                        join b in invoicequery on a.Id equals b.PackingListId
                        join c in invoiceitemquery on b.Id equals c.GarmentShippingInvoiceId
                      
                        group new { Qty = c.Quantity,  Amt = c.Amount, Uom = c.UomUnit } by new
                        {
                            a.BuyerAgentName,
                            c.ComodityName,
                        } into G
                        select new OmzetYearBuyerComodityViewModel
                        {
                            buyerName = G.Key.BuyerAgentName,
                            comodityName = G.Key.ComodityName,
                            pcsQuantity = G.Where(i => i.Uom == "PCS").Sum(i => i.Qty),
                            setsQuantity = G.Where(i => i.Uom == "SETS").Sum(i => i.Qty),
                            amount = G.Sum(i => i.Amt),
                        });
            return newQ;
        }
        
        public List<OmzetYearBuyerComodityViewModel> GetReportData(int year)
        {
            var Query = GetData(year);
            Query = Query.OrderBy(b => b.buyerName).ThenBy(b => b.comodityName);
            return Query.ToList();
        }

        public MemoryStream GenerateExcel(int year)
        {
            var Query = GetData(year);
            DataTable result = new DataTable();

            result.Columns.Add(new DataColumn() { ColumnName = "NO", DataType = typeof(string) });
            result.Columns.Add(new DataColumn() { ColumnName = "B U Y E R", DataType = typeof(string) });
            result.Columns.Add(new DataColumn() { ColumnName = "KOMODITI", DataType = typeof(string) });
            result.Columns.Add(new DataColumn() { ColumnName = "QTY - PCS", DataType = typeof(double) });
            result.Columns.Add(new DataColumn() { ColumnName = "QTY - SETS", DataType = typeof(double) });
            result.Columns.Add(new DataColumn() { ColumnName = "AMOUNT", DataType = typeof(double) });

            if (Query.ToArray().Count() == 0)
                result.Rows.Add("", "", "", 0, 0, 0);
            else
            {
                int index = 0;
                foreach (var d in Query)
                {
                    index++;
                    result.Rows.Add(index, d.buyerName, d.comodityName, d.pcsQuantity, d.setsQuantity, d.amount);
                }
            }
            return Excel.CreateExcel(new List<KeyValuePair<DataTable, string>>() { new KeyValuePair<DataTable, string>(result, "Sheet1") }, true);
        }
    }
}