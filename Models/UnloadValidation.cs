using System.Collections.Generic;
using System.Web.Mvc;

namespace S1947.Models
{
    public class UnloadValidation
    {
        public int DockNo { get; set; }
        public string InvoiceNo { get; set; }
        public string CustomerName { get; set; }
        public string LocationofDelivery { get; set; }
        public string PartNo { get; set; } // Model Nos
        public string PartDescription { get; set; } // Model Description
        public string SAPFGCode { get; set; } // SAP FG Model
        public int PartQuantity { get; set; } // Quantity Per Model
        public int AvlQuantity { get; set; } // Quantity from db
        public int BoxQty { get; set; } // Box Qty Per Model
        public int TotalQuantity { get; set; }
        public int FLCQty { get; set; } // Total FLC Count
        public int VehicleSize { get; set; }
        public string VehicleSealno { get; set; }
        public string Progress { get; set; }
        public int ShipmentId { get; set; }
        public string VehicleNumber { get; set; } // Vehicle Plate No
        public string SupervisiorName { get; set; } // Vehicle Plate No
        public string DriverName { get; set; } // Vehicle Plate No
        public string SecurityName { get; set; } // Vehicle Plate No
        public string MobileNoSupervisor { get; set; } // Vehicle Plate No
        public string MobileNoDriver { get; set; } // Vehicle Plate No
        public string MobileNoSecurity { get; set; } // Vehicle Plate No
        public string TransporterName { get; set; }


        public int? DockId { get; set; }
        public string InvoiceNo1 { get; set; }

        public string CustomerName1 { get; set; }
        public List<SelectListItem> DockList { get; set; }

        public List<SelectListItem> InvoiceList { get; set; }

        public int ExpectedFLC { get; set; }

        public int ScannedFLC { get; set; }
    }
}