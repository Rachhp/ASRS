using System;
using System.Collections.Generic;

namespace S1947.Models
{
    public class AndonViewModel
    {
        public string Date { get; set; }
        public string Time { get; set; }
        public string Shift { get; set; }

        // Header Details
        public int? DockNo { get; set; }
        public int? VehicleSize { get; set; }
        public string VehicleNumber { get; set; }
        public string TransporterName { get; set; }
        public string VehicleSealNo { get; set; }

        // Status
        public string VehicleLoadingStatus { get; set; }
        public List<int> GreenFLCSlots { get; set; }

        // Bottom Details
        public string SupervisiorName { get; set; }
        public string MobileNoSupervisor { get; set; }
        public string DriverName { get; set; }
        public string MobileNoDriver { get; set; }
        public string SecurityName { get; set; }
        public string MobileNoSecurity { get; set; }

        // Dropdown Lists
        public List<string> CustomerList { get; set; }
        public List<string> ModelList { get; set; }

        // Loading Table
        public List<UnloadMaterialTemp> LoadingHistory { get; set; }

        // ASRS Status
        public List<UnloadTemp> ASRSLoadingStatus { get; set; }
        // Row Details

        public string InvoiceNo { get; set; }
        public string CustomerName { get; set; }
        public string LocationOfDelivery { get; set; }
        public string PartNo { get; set; }
        public string PartDescription { get; set; }
        public string SAPFGCode { get; set; }
        public int PartQuantity { get; set; }
        public int FLCQty { get; set; }
        public int AvailableQty { get; set; }

        public DateTime? LoadingStartTime { get; set; }
        public DateTime? LoadingEndTime { get; set; }
        public class LoadingTableViewModel
        {
            public List<UnloadMaterialTemp> LoadingHistory { get; set; }

            public List<string> CustomerList { get; set; }

            public List<string> ModelList { get; set; }
        }

        public class SubmitLoadingModel
        {
            public string InvoiceNo { get; set; }
            public int DockNo { get; set; }
            public int VehicleSize { get; set; }
            public string VehicleNumber { get; set; }
            public string TransporterName { get; set; }
            public string VehicleSealNo { get; set; }
            public int TotalQty { get; set; }
            public int TotalFLC { get; set; }
        }

    }
}