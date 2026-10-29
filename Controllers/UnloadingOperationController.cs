using DocumentFormat.OpenXml.Spreadsheet;
using Newtonsoft.Json;
using Org.BouncyCastle.Bcpg;
using S1947.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using static S1947.Models.AndonViewModel;
namespace S1947.Controllers
{
    public class UnloadingOperationController : Controller
    {
        UtilityController utility = new UtilityController();
        public ActionResult Index()
        {
            AndonViewModel model = new AndonViewModel();

            using (S1947Entities db = new S1947Entities())
            {
                //ned to pass model list  and the qty and customer list from controller to view 
                model.CustomerList = db.CustomerMasters.Where(x => x.DeleteStatus == "N" && x.LockStatus == "Y")
                .Select(x => x.CustomerName)
                .ToList();

                model.ModelList = db.GoodsExisitings
                 .Where(x => x.LocaStatus == "LOADED")
                 .Select(x => x.FGModel)
                 .Distinct()
                 .ToList();

                ///intial empty
                model.LoadingHistory = new List<UnloadMaterialTemp>();

                model.ASRSLoadingStatus = db.UnloadTemps
                    .OrderBy(x => x.MTransNo)
                    .ToList();


                // Get Dock Max Range from master table
                int dockMaxRange = (int)db.Othermasters
                    .Select(x => x.MaxDockRange)
                    .FirstOrDefault();

                // Generate Dock No list: 1, 2, 3... up to DockMaxRange
                ViewBag.DockNo = Enumerable.Range(1, dockMaxRange)
                    .Select(x => new SelectListItem
                    {
                        Text = x.ToString(),
                        Value = x.ToString()
                    })
                    .ToList();

                return View(model);
            }
        }


        [HttpGet]
        public JsonResult GetASRSLoadingStatus()
        {
            using (var db = new S1947Entities())
            {
                var data = db.UnloadTemps
                    .OrderBy(x => x.MTransNo)
                    .Select(x => new
                    {
                        InvoiceNo = x.InvoiceNo,
                        DockNo = x.DockNo,
                        CustomerName = x.CustomerName,
                        LocationName = x.LocationofDelivery,
                        VehicleNumber = x.VehicleNumber,
                        PalletNo = x.PalletNo,
                        FGModel = x.FGModel,
                        PartQuantity = x.PartQuantity,
                        Status = db.TaskReports
                    .Where(t => t.SerialCode == x.SerialCode)
                    .OrderByDescending(t => t.ID)
                    .Select(t => t.StautusMessage)
                    .FirstOrDefault()
                    }).ToList();

                return Json(data, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult GetCustomerLocation(string customer)
        {
            // Fetch location from database using customer value
            var location = "";
            using (var db = new S1947Entities())
            {
                location = db.CustomerMasters
                             .Where(x => x.CustomerName == customer)
                             .Select(x => x.Location)
                             .FirstOrDefault();
            }
            return Json(new { location = location }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetModelDtl(string modelName)
        {
            using (var db = new S1947Entities())
            {
                //scenario 1 
                var result = db.GoodsExisitings
                    .Where(x => x.FGModel == modelName
                             && x.LocaStatus == "LOADED" && x.Quantity > 0)
                    .GroupBy(x => new
                    {
                        x.FGCode,
                        x.FGDesc
                    })
                    .Select(g => new
                    {
                        FGCode = g.Key.FGCode,
                        Description = g.Key.FGDesc,
                        AvailableQty = g.Sum(x => x.Quantity),
                    })
                    .FirstOrDefault();

                return Json(new
                {
                    FGCode = result == null ? "" : result.FGCode,
                    Description = result == null ? "" : result.Description,
                    AvailableQty = result == null ? 0 : result.AvailableQty
                }, JsonRequestBehavior.AllowGet);
            }
        }


        [HttpGet]
    

        public JsonResult GetPalletDetails(string fgCode, string description, string model, int qty)
        
        {
            using (var db = new S1947Entities())
            {
                var pallets = db.GoodsExisitings
                    .Where(x => x.FGModel == model &&
                                x.FGCode == fgCode &&
                                x.FGDesc == description &&
                                x.LocaStatus == "LOADED" &&
                                x.Quantity > 0)
                    .OrderBy(x => x.CreatedOn).ToList();

                // STEP 1: Validate requested quantity.
                int availableQty = pallets.Sum(x => Convert.ToInt32(x.Quantity));
                if (qty <= 0 || availableQty < qty)
                {
                    return Json(new
                    {
                        Success = false,
                        Message = "Insufficient stock available.",
                        AvailableQty = availableQty
                    }, JsonRequestBehavior.AllowGet);
                }

                //2:Build a list containing only the required pallets
                var requiredPallets = new List<GoodsExisiting>();
                int totalQty = 0;

                foreach (var pallet in pallets)
                {
                    requiredPallets.Add(pallet);
                    totalQty += Convert.ToInt32(pallet.Quantity);

                    if (totalQty >= qty)
                        break;
                }

                // 3.Pass ONLY the required list
                var selectedPallets = SelectPalletsForUnloading(requiredPallets, qty);

                // STEP 4: Return only the values required by the view.
                return Json(new
                {
                    Success = true,
                    FLCCount = selectedPallets.Count,
                    AllocatedQty = selectedPallets.Sum(x => Convert.ToInt32(x.Quantity)),
                    RequestedQty = qty,
                }, JsonRequestBehavior.AllowGet);
            }
        }

        private List<GoodsExisiting> SelectPalletsForUnloading(List<GoodsExisiting> pallets, int requestedQty)
        {
            var sequencePallets = pallets.OrderBy(x => x.CreatedOn).ToList();
            //var sequencePallets = pallets.OrderBy(x => x.sequence).ToList();
            bool normalPattern = IsNormalLoadingPattern(sequencePallets);
            if (normalPattern)
            {
                return SelectUntilQuantity(sequencePallets, requestedQty);
            }
            else
            {
                //first check datewise list in that check shiftwise 
                // STEP 1: Apply datewise priority first.
                var datePriority = pallets.OrderBy(x => x.CreatedOn.HasValue
                        ? x.CreatedOn.Value.Date
                        : DateTime.MaxValue)
                        .ToList();

                // STEP 2: Check alternative ends within the datewise list.
                var dateAlternating = GetAlternatingEnds(datePriority);

                var dateSelected = SelectUntilQuantity(dateAlternating, requestedQty);

                // If datewise selection satisfies the quantity, unload directly.
                if (dateSelected.Sum(x => Convert.ToInt32(x.Quantity)) >= requestedQty)
                {
                    return dateSelected;
                }

                // STEP 3: Datewise alternative ends are insufficient.
                // Apply shiftwise priority, then check alternative ends again.
                var shiftPriority = ApplyShiftPriority(pallets);

                var shiftAlternating = GetAlternatingEnds(shiftPriority);

                return SelectUntilQuantity(shiftAlternating, requestedQty);

                ////// NON-NORMAL PATTERN: Apply shift priority and alternating ends.
                ////var shiftPriority = ApplyShiftPriority(pallets);

                ////var shiftAlternating = GetAlternatingEnds(shiftPriority);

                //var selected = SelectUntilQuantity(shiftAlternating, requestedQty);

                //if (selected.Sum(x => Convert.ToInt32(x.Quantity)) >= requestedQty)
                //{
                //    return selected;
                //}

                //// FALLBACK: Relax shift priority to date-wise priority.
                //var datePriority = pallets
                //    .OrderBy(x => x.CreatedOn.HasValue
                //        ? x.CreatedOn.Value.Date
                //        : DateTime.MaxValue)
                //    .ThenBy(x => x.sequence)
                //    .ToList();

                //var dateAlternating = GetAlternatingEnds(datePriority);

                //return SelectUntilQuantity(dateAlternating, requestedQty);

            }
        }


        //from here it check is normal loading patternis there 
        private bool IsNormalLoadingPattern(List<GoodsExisiting> pallets)
        
        {
            if (pallets == null || pallets.Count <= 1)
                return true;

          //var ordered = pallets.OrderBy(x => x.sequence).ToList();
            var ordered = pallets.OrderBy(x => x.CreatedOn).ToList();

            // Check consecutive pallets in sequence order.
            for (int i = 1; i < ordered.Count; i++)
            {
                var current = ordered[i];
                var previous = ordered[i - 1];

                int previousLayer = Convert.ToInt32(previous.Layer);
                int currentLayer = Convert.ToInt32(current.Layer);
                int previousRow = Convert.ToInt32(previous.Row);
                int currentRow = Convert.ToInt32(current.Row);
                int previouscol = Convert.ToInt32(previous.Col);
                int currentcol = Convert.ToInt32(current.Col);

                // Different layers: normal pattern
                if (currentLayer != previousLayer)
                    continue;

                // Same layer, different categories: normal pattern
                if (current.Cat != previous.Cat)
                    continue;

                //apply condtion if smae column adn sme row & smae cat then its non normal

                

                //// Same layer and category, but different rows: normal
                //if (previousRow != currentRow)
                //    continue;
                // Same layer and category, but different col: normal
                if (previouscol != currentcol)
                    continue;
                // Same layer, same category, and same row: non-normal
                return false;
            }
            return true;
        }


        //sequnecial unloading
        private List<GoodsExisiting> SelectUntilQuantity(IEnumerable<GoodsExisiting> pallets, int requestedQty)
        {
            var selected = new List<GoodsExisiting>();
            int allocatedQty = 0;

            foreach (var pallet in pallets)
            {
                selected.Add(pallet);
                allocatedQty += Convert.ToInt32(pallet.Quantity);
                // Whole pallets: stop when requested quantity is met or exceeded.
                if (allocatedQty >= requestedQty)
                    break;
            }
            return selected;
        }

      
        private List<GoodsExisiting> ApplyDatePriority( List<GoodsExisiting> pallets)
        {
            return pallets.OrderBy(x => x.CreatedOn.HasValue
                    ? x.CreatedOn.Value.Date
                    : DateTime.MaxValue)
                .ToList();
        }

        private List<GoodsExisiting> ApplyShiftPriority(List<GoodsExisiting> pallets)
        {
            using (var db = new S1947Entities())
            {
                var shifts = db.shift_master.Where(s => s.is_active).OrderBy(s => s.Id).ToList();
                var prioritized = pallets.Select(p =>
                {
                    DateTime createdOn = p.CreatedOn?? DateTime.MinValue;
                    TimeSpan palletTime = createdOn.TimeOfDay;

                    var matchingShift = shifts.FirstOrDefault(s =>
                    {
                        TimeSpan start = s.start_time;
                        TimeSpan end = s.end_time;

                        if (start < end)
                        {
                            return palletTime >= start &&
                                   palletTime < end;
                        }

                        // Overnight shift.
                        return palletTime >= start ||
                               palletTime < end;
                    });

                    return new
                    {
                        Pallet = p,
                        LoadingDate = createdOn.Date,
                        LoadingTime = createdOn.TimeOfDay,
                        ShiftId = matchingShift?.Id ?? int.MaxValue
                    };
                });

                return prioritized
                    .OrderBy(x => x.LoadingDate)
                    .ThenBy(x => x.ShiftId)
                    .ThenBy(x => x.LoadingTime)
                    .ThenBy(x => x.Pallet.sequence)
                    .Select(x => x.Pallet)                                                        
                    .ToList();
            }
        }

        private List<GoodsExisiting> GetAlternatingEnds( List<GoodsExisiting> pallets)
        {
            var result = new List<GoodsExisiting>();

            // A and D: keep row order; do not apply B/C two-ended selection.
            var sectionAD = pallets
                .Where(x => x.Cat == "A" || x.Cat == "D")
                .OrderBy(x => x.Cat)
                .ThenBy(x => x.Row)
                .ToList();

            // B and C: alternate between both ends.
            var sectionBC = pallets
                .Where(x => x.Cat == "B" || x.Cat == "C")
                .GroupBy(x => new { x.Layer, x.Col, x.Cat })
                .ToList();

            foreach (var group in sectionBC)
            {
                var positions = group
                    .OrderBy(x => x.Row)
                    .ToList();

                int left = 0;
                int right = positions.Count - 1;

                while (left <= right)
                {
                    result.Add(positions[left]);
                    left++;

                    if (left <= right)
                    {
                        result.Add(positions[right]);
                        right--;
                    }
                }
            }

            // Include any positions without A/B/C/D category afterward.
            var otherPositions = pallets
                .Where(x => x.Cat != "A" &&
                            x.Cat != "B" &&
                            x.Cat != "C" &&
                            x.Cat != "D")
                .OrderBy(x => x.sequence)
                .ToList();

            result.AddRange(sectionAD);
            result.AddRange(otherPositions);

            return result;
        }

        //toherefor unload 
        [HttpPost]
        public JsonResult SaveLoading(int DockNo, int VehicleSize, string VehicleNumber, string TransporterName, string VehicleSealNo, string InvoiceNo,
                                         string CustomerName, string Location, string Model, string FGCode, string Description, int Quantity, int AvailableQty,
                                        string DriverName, string DriverMobile, string SecurityName, string SecurityMobile, string SupervisorName, string SupervisorMobile)
        {
            string shift = utility.GetCurrentShiftName();//fetching shift from utility controller
            using (var db = new S1947Entities())
            {
                int allowedDockCount = GetAllowedUnloadingOperationCount();
                int stationNo = GetStationNo(DockNo);

                if (stationNo == 0)
                {
                    return Json(new
                    {
                        Success = false,
                        Message = "No unloading station is currently active."
                    });
                }

                int activeOperationCount = db.UnloadMaterialTemps.Where(x => x.status == "Pending").GroupBy(x => x.DockNo).Count();
                bool currentDockExists = db.UnloadMaterialTemps.Any(x => x.DockNo == DockNo && x.status == "Pending");

                if (!currentDockExists && activeOperationCount >= allowedDockCount)
                {
                    return Json(new
                    {
                        Success = false,
                        Message = "Maximum unloading operations already running."
                    });
                }

                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        //var pallets = db.GoodsExisitings
                        //    .Where(x =>
                        //        x.LocaStatus == "INPROCESS" &&
                        //        x.FGModel == Model &&
                        //        x.FGCode == FGCode &&
                        //        x.FGDesc == Description)
                        //    .OrderBy(x => x.UnloadSequence)
                        //    .ToList();

                        //int allocatedQty = pallets.Sum(x => Convert.ToInt32(x.Quantity));
                        //if (allocatedQty < Quantity)
                        //{
                        //    transaction.Rollback();
                        //    return Json(new
                        //    {
                        //        Success = false,
                        //        Message = "Insufficient stock available."
                        //    });
                        //}
                         var availablePallets = db.GoodsExisitings
                            .Where(x =>
                                x.LocaStatus == "LOADED" &&
                                x.FGModel == Model &&
                                x.FGCode == FGCode &&
                                x.FGDesc == Description &&
                                x.Quantity > 0)
                            .OrderBy(x => x.CreatedOn)
                            .ToList();

                        int availableQty = availablePallets.Sum(x => Convert.ToInt32(x.Quantity));

                        if (availableQty < Quantity)
                        {
                            transaction.Rollback();

                            return Json(new
                            {
                                Success = false,
                                Message = "Insufficient stock available."
                            });
                        }

                        var requiredPallets = new List<GoodsExisiting>();
                        int totalQty = 0;

                        foreach (var pallet in availablePallets)
                        {
                            requiredPallets.Add(pallet);
                            totalQty += Convert.ToInt32(pallet.Quantity);

                            if (totalQty >= Quantity)
                                break;
                        }

                        var pallets = SelectPalletsForUnloading(requiredPallets, Quantity);
                        int allocatedQty = pallets.Sum(x => Convert.ToInt32(x.Quantity));

                        if (allocatedQty < Quantity)
                        {
                            transaction.Rollback();

                            return Json(new
                            {
                                Success = false,
                                Message = "Unable to allocate the requested quantity."
                            });
                        }

                        // Reserve the exact pallets selected for this save.
                        for (int i = 0; i < pallets.Count; i++)
                        {
                            pallets[i].LocaStatus = "INPROCESS";
                            pallets[i].UnloadSequence = i + 1;
                        }

                        db.SaveChanges();

                        string firstSerialCode = null;
                        foreach (var pallet in pallets)
                        {
                            // Generate a UNIQUE SerialCode for EACH pallet/task
                            string serialCode = GenerateOutboundSerialCode(db);
                            // Keep first SerialCode for Temp
                            if (firstSerialCode == null)
                            {
                                firstSerialCode = serialCode;
                            }
                            //1. insert to unload temp individual data ASRS
                            UnloadTemp temp = new UnloadTemp
                            {
                                InvoiceNo = InvoiceNo,
                                CustomerName = CustomerName,
                                LocationofDelivery = Location,
                                FGModel = Model,
                                FGModelDesc = Description,
                                SAPFGCode = FGCode,
                                PartQuantity = Convert.ToInt32(pallet.Quantity),
                                TotalQty = allocatedQty,
                                FLCLocation = 1,
                                VehicleSize = VehicleSize,
                                VehicleNumber = VehicleNumber,
                                TransporterName = TransporterName,
                                VehicleSealNo = VehicleSealNo,
                                DriverName = DriverName,
                                MobileNoDriver = DriverMobile,
                                SecurityName = SecurityName,
                                MobileNoSecurity = SecurityMobile,
                                SupervisiorName = SupervisorName,
                                MobileNoSupervisor = SupervisorMobile,
                                SerialCode = serialCode,
                                DockNo = DockNo,
                                PalletNo = pallet.PalletNo,
                                PlalletQR = pallet.PalletQr,
                                LocationId = pallet.MTransNo.ToString(),
                                Station = stationNo,
                                LocationName = pallet.LocationName,
                                Status = "Pending",
                                CreatedBy = "System",
                                CreatedOn = DateTime.Now
                            };
                            db.UnloadTemps.Add(temp);
                            db.SaveChanges(); 
                        }

                        //3. unload material temp table insert for view  Table 
                        UnloadMaterialTemp material = new UnloadMaterialTemp()
                        {
                            InvoiceNo = InvoiceNo,
                            Customer = CustomerName,
                            Location = Location,
                            Model = Model,
                            SAPFGCode = FGCode,
                            Description = Description,
                            EnteredQty = Quantity,
                            FLCQty = pallets.Count,
                            TransporterName = TransporterName,
                            VehicleRegNo = VehicleNumber,
                            AvailableQty = AvailableQty,
                            VehicleSealNo = VehicleSealNo,
                            VehicleSize = VehicleSize,
                            DriverName = DriverName,
                            MobileNoDriver = DriverMobile,
                            SecurityName = SecurityName,
                            MobileNoSecurity = SecurityMobile,
                            SupervisiorName = SupervisorName,
                            MobileNoSupervisor = SupervisorMobile,
                            DockNo = DockNo,
                            Station = stationNo,
                            SerialCode = firstSerialCode,
                            status = "Pending",
                            Date = DateTime.Now.ToString("dd/MM/yyyy"),
                            Time = DateTime.Now.ToString("HH:mm"),
                            Shift = shift
                        };
                        db.UnloadMaterialTemps.Add(material);
                        db.SaveChanges();
                       
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return Json(new
                        {
                            Success = false, Message = ex.Message
                        });
                    }
                    transaction.Commit();
                    return Json(new
                    {
                        Success = true, Message = "Data saved successfully"
                    });
                }
            }
        }

        [HttpGet]
        public ActionResult GetDockLoadingDetails(int DockNo)
        {
            using (var db = new S1947Entities())
            {

                var loadingHistory = db.UnloadMaterialTemps
                .Where(x => x.DockNo == DockNo)
                //&& x.status == "Submit")
                .OrderBy(x => x.id)
                .ToList();

                var latestLoading = loadingHistory
                .OrderByDescending(x => x.id)
                .FirstOrDefault();

                var model = new Models.LoadingTableViewModel
                {
                    LoadingHistory = loadingHistory,
                    IsExistingData = loadingHistory.Any(),

                    CustomerList = db.CustomerMasters
                        .Where(x => x.DeleteStatus == "N"
                                 && x.LockStatus == "Y")
                        .Select(x => x.CustomerName)
                        .ToList(),

                    ModelList = db.GoodsExisitings
                        .Where(x => x.LocaStatus == "LOADED")
                        .Select(x => x.FGModel)
                        .Distinct()
                        .ToList(),

                    DockNo = latestLoading?.DockNo ?? 0,
                    VehicleSize = latestLoading?.VehicleSize ?? 0,
                    VehicleNumber = latestLoading?.VehicleRegNo,
                    VehicleSealNo = latestLoading?.VehicleSealNo,
                    TransporterName = latestLoading?.TransporterName,
                    DriverName = latestLoading?.DriverName,
                    MobileNoDriver = latestLoading?.MobileNoDriver,
                    SecurityName = latestLoading?.SecurityName,
                    MobileNoSecurity = latestLoading?.MobileNoSecurity,
                    SupervisiorName = latestLoading?.SupervisiorName,
                    MobileNoSupervisor = latestLoading?.MobileNoSupervisor
                };

                return PartialView("_LoadingTable", model);
            }
        }


        [HttpGet]
        public JsonResult GetActiveDock(int DockNo)
        {
            using (var db = new S1947Entities())
            {
                int allowedCount = GetAllowedUnloadingOperationCount();

                // Current active docks
                var activeDocks = db.UnloadMaterialTemps
                    .Where(x => x.status == "Submit")
                    .Select(x => x.DockNo)
                    .Distinct()
                    .ToList();

                // If selected dock already exists, allow loading
                if (activeDocks.Contains(DockNo))
                {
                    return Json(new
                    {
                        Success = true
                    }, JsonRequestBehavior.AllowGet);
                }

                // Check limit
                if (activeDocks.Count >= allowedCount)
                {
                    return Json(new
                    {
                        Success = false,
                        Message = "Maximum unloading stations are already active."
                    }, JsonRequestBehavior.AllowGet);
                }


                return Json(new
                {
                    Success = true
                }, JsonRequestBehavior.AllowGet);


            }
        }


        private int GetAllowedUnloadingOperationCount()
        {
            using (var db = new S1947Entities())
            {
                var runMode = db.LiftStatusMasters
                    .Where(x => x.IsActive == true)
                    .Select(x => x.RunMode)
                    .FirstOrDefault();

                switch (runMode)
                {
                    case 1:
                    case 2:
                        return 1;

                    case 3:
                        return 2;

                    default:
                        return 0;
                }
            }
        }


        private int GetStationNo(int dockNo)
        {
            using (var db = new S1947Entities())
            {
                var runMode = db.LiftStatusMasters
                    .Where(x => x.IsActive == true)
                    .Select(x => x.RunMode)
                    .FirstOrDefault();

                switch (runMode)
                {
                    // Only Station 1 is active
                    case 1:
                        return 1;

                    // Only Station 2 is active
                    case 2:
                        return 2;

                    // Both Station 1 and Station 2 are active
                    case 3:
                        // Odd dock  -> Station 1
                        // Even dock -> Station 2
                        return (dockNo % 2 == 1) ? 1 : 2;

                    default:
                        return 0;
                }
            }
        }


        //serial code generation helper method
        private string GenerateOutboundSerialCode(S1947Entities db)
        {
            string today = DateTime.Now.ToString("yyyyMMdd");
            string serialPrefix = "OUT" + today;

            // Check TaskDtls
            string lastTaskSerial = db.TaskDtls.Where(x =>
                    x.SerialCode != null &&
                    x.SerialCode.StartsWith(serialPrefix))
                    .OrderByDescending(x => x.SerialCode)
                    .Select(x => x.SerialCode)
                    .FirstOrDefault();

            // Check UnloadTemp
            string lastUnloadTempSerial = db.UnloadTemps
                .Where(x =>
                    x.SerialCode != null &&
                    x.SerialCode.StartsWith(serialPrefix))
                .OrderByDescending(x => x.SerialCode)
                .Select(x => x.SerialCode)
                .FirstOrDefault();

            // Find the highest serial number from both tables
            int lastNumber = 0;

            if (!string.IsNullOrWhiteSpace(lastTaskSerial))
            {
                string numberPart = lastTaskSerial.Substring(serialPrefix.Length);
                if (int.TryParse(numberPart, out int taskNumber))
                {
                    lastNumber = Math.Max(lastNumber, taskNumber);
                }
            }

            if (!string.IsNullOrWhiteSpace(lastUnloadTempSerial))
            {
                string numberPart = lastUnloadTempSerial.Substring(serialPrefix.Length);
                if (int.TryParse(numberPart, out int unloadTempNumber))
                {
                    lastNumber = Math.Max(lastNumber, unloadTempNumber);
                }
            }

            int nextSerialNumber = lastNumber + 1;

            if (nextSerialNumber > 9999)
            {
                throw new InvalidOperationException(
                    $"Daily outbound serial number exceeded 9999 for {today}.");
            }
            return $"{serialPrefix}{nextSerialNumber:D4}";
        }



    [HttpPost]
    public async Task<JsonResult> SubmitLoading(SubmitLoadingModel model)
    {
        try
        {
            List<UnloadTemp> submittedUnloadTemps;
            using (S1947Entities db = new S1947Entities())
            {
                var UnloadTempData = db.UnloadTemps.Where(x => x.Status == "Pending" && x.DockNo == model.DockNo).ToList();
                var UnloadMaterialTempData = db.UnloadMaterialTemps.Where(x =>x.status == "Pending" && x.DockNo == model.DockNo) .ToList();

                if (!UnloadTempData.Any())
                {
                    return Json(new
                    {
                        Success = false,
                        Message = "No pending records found."
                    });
                }

                foreach (var itemq in UnloadMaterialTempData)
                {
                    itemq.status = "Submit";
                    db.Entry(itemq).State = EntityState.Modified;
                    db.SaveChanges();
                }

                foreach (var item in UnloadTempData)
                {
                    item.Status = "Submit";
                    db.Entry(item).State = EntityState.Modified;
                    db.SaveChanges();
                }
        
                submittedUnloadTemps = db.UnloadTemps.Where(x => x.Status == "Submit" && x.DockNo == model.DockNo).OrderBy(x => x.MTransNo).ToList();

                // INSERT TASK DTL USING STORED PROCEDURE
                int firstTaskNo = 0;
                //task priority
                int nextPriority = db.TaskDtls.Where(x => x.TaskType == "UNLOADOPERATION").Select(x => (int?)x.Priority).Max() ?? 0;
                nextPriority++;
                //task no 
                int nextTaskNo = db.TaskDtls.Select(x => (int?)x.TaskNo).Max() ?? 0;
                nextTaskNo++;
                // Save the first task number
                if (firstTaskNo == 0)
                {
                    firstTaskNo = nextTaskNo;
                }
                // Create one task for each UnloadTemp transaction
                //foreach (var pallet in tempData)
                foreach (var pallet in submittedUnloadTemps)
                {
                        // Check whether a task already exists for this pallet
                    bool taskExists = db.TaskDtls.Any(x =>x.TaskType == "UNLOADOPERATION" &&
                        x.DestinationLocation == pallet.LocationName &&
                        x.SourceLocation == "OUTBOUND" );
                        if (taskExists)
                        {
                            // Skip already submitted tasks
                            continue;
                        }

                    //stored procedure
                    db.InsertTaskDtl(nextTaskNo, "UNLOADOPERATION", nextPriority, pallet.PlalletQR, "OUTBOUND", pallet.LocationName, "PENDING", pallet.Station, pallet.LocationId, nextPriority, DateTime.Now, DateTime.Now, null, null, pallet.SerialCode);
                    db.TaskReports.Add(new TaskReport
                    {
                        TaskID = nextTaskNo,
                        SerialCode = pallet.SerialCode,
                        StatusCode = "100",
                        StautusMessage = "Task Created",
                        OperationType = "OUTBOUND"
                    });
                    // IMPORTANT
                    nextTaskNo++;
                    nextPriority++;
                    db.SaveChanges();
                }
                    //update inprocess to occupied in goods existing table
                    var goods = db.GoodsExisitings.Where(x => x.LocaStatus == "INPROCESS").ToList();
                    foreach (var item in goods)
                    { 
                        item.LocaStatus = "OCCUPIED";
                        item.UnloadSequence = 0;
                        item.ModifiedOn = DateTime.Now;
                        db.Entry(item).State = EntityState.Modified;
                        db.SaveChanges();
                    }
                }
                //triger the unloading api here
                // SendTasksToWcs();
                //// https://localhost:44341/api/MoveAPI/MoveTaskUnload  thsi url with no parameters post method 
                // Trigger unloading API
                //bool apiSuccess = await TriggerUnloadApi();

                //if (!apiSuccess)
                //{
                //    return Json(new
                //    {
                //        Success = false,
                //        Message = "Database submission completed, but unloading API failed."
                //    });
                //}

                //return Json(new
                //{
                //    Success = true,
                //    Message = "Unloading submitted and API triggered successfully."
                //});

                return Json(new
                {
                    Success = true,
                    Message = "succes",
              
                });
            }
        catch (Exception ex)
        {
            return Json(new
            {
                Success = false,
                Message = "Error while submitting loading.",
                Error = ex.Message
            });
        }
    }



        //for outbound api method
        private async Task<bool> TriggerUnloadApi()
        {
            string apiUrl = ConfigurationManager.AppSettings["MoveTaskUnloadUrl"];

            using (HttpClient client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(30);

                // POST with no parameters and no request body
                HttpResponseMessage response = await client.PostAsync(
                    apiUrl,
                    null
                );

                return response.IsSuccessStatusCode;
            }
        }


        private void SaveWcsLog(string requestData, string responseData, int? httpStatusCode, int? wcsCode, bool? wcsStatus, string result, string errorMessage)
        {
            try
            {
                using (var db = new S1947Entities())
                {
                    db.WcsApiLogs.Add(new WcsApiLog
                    {
                        ApiName = "InsertTask",
                        RequestData = requestData,
                        ResponseData = responseData,
                        HttpStatusCode = httpStatusCode,
                        WcsCode = wcsCode,
                        WcsStatus = wcsStatus,
                        Result = result,
                        ErrorMessage = errorMessage,
                        CreatedOn = DateTime.Now
                    });

                    db.SaveChanges();
                }
            }
            catch
            {
                // VERY IMPORTANT:
                // Logging failure must never affect business logic.
            }
        }
        

    }
}


