using System.Collections.Generic;
using System.Web.Mvc;

namespace S1947.Models
{
    public class LoadingSheetModel
    {

        public string InvoiceNo { get; set; }

        public string CustomerName { get; set; }

        public string DockNo { get; set; }

        public string VehicleNumber { get; set; }

        public string TransporterName { get; set; }

        public int FLCQty { get; set; }


        public int LocationId { get; set; }
        public int LocationId1 { get; set; }

        public List<SelectListItem> LocationList { get; set; }
        public List<SelectListItem> LocationList1 { get; set; }

    }

}