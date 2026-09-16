using System;
using System.Collections.Generic;

namespace S1947.Models
{
    public class LoadingTableViewModel
    {
        public List<UnloadMaterialTemp> LoadingHistory { get; set; }
        public bool IsExistingData { get; set; }
        public List<string> CustomerList { get; set; }

        public List<string> ModelList { get; set; }

        public int DockNo { get; set; }

        public int VehicleSize { get; set; }
    
        public string VehicleNumber { get; set; }

        public string VehicleSealNo { get; set; }

        public string TransporterName { get; set; }

        public string DriverName { get; set; }

        public string MobileNoDriver { get; set; }

        public string SecurityName { get; set; }

        public string MobileNoSecurity { get; set; }

        public string SupervisiorName { get; set; }

        public string MobileNoSupervisor { get; set; }
    }
}