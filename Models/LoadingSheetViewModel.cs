using System.Collections.Generic;

namespace S1947.Models
{
    public class LoadingSheetViewModel
    {
        public string Date { get; set; }
        public string Time { get; set; }
        public string Shift { get; set; }
        public string Status { get; set; }

        public List<LoadingItem> Items { get; set; }

        public int TotalQuantity { get; set; }
    }

    public class LoadingItem
    {
        public int SrNo { get; set; }
        public string InvoiceNo { get; set; }
        public string CustomerName { get; set; }
        public string Location { get; set; }
        public string PartNo { get; set; }
        public string Description { get; set; }
        public string SAPCode { get; set; }
        public int Quantity { get; set; }
        public int FLCQty { get; set; }
        public string VehicleNumber { get; set; }
        public string Transporter { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public string SealNo { get; set; }
        public int vehicleSize { get; set; }
    }
}