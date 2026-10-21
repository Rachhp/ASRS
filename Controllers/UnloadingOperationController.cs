using Newtonsoft.Json;
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
        //public JsonResult GetPalletDetails(string fgCode, string description, string model, int qty)
        //{
        //    using (var db = new S1947Entities())
        //    {
        //        var pallets = db.GoodsExisitings
        //            .Where(x =>
        //                x.FGModel == model &&
        //                x.FGCode == fgCode &&
        //                x.FGDesc == description &&
        //                x.LocaStatus == "LOADED" &&
        //                x.Quantity > 0)
        //            .OrderBy(x => x.CreatedOn)
        //            .ThenBy(x => x.Layer)
        //            .ThenBy(x => x.Row)
        //            .ThenBy(x => x.Col)
        //            //.OrderBy(x => x.ModifiedOn)
        //            //.ThenBy(x => x.Row)
        //            .ToList();

        //        int allocatedQty = 0;

        //        List<object> selected = new List<object>();

        //        foreach (var pallet in pallets)
        //        {
        //            selected.Add(new
        //            {
        //                PalletNo = pallet.PalletNo,
        //                QR = pallet.PalletQr,
        //                Qty = pallet.Quantity,
        //                Location = pallet.LocationName,
        //                LocationId = pallet.MTransNo
        //            });

        //            allocatedQty += Convert.ToInt32(pallet.Quantity);
        //            // Stop as soon as requested quantity is satisfied
        //            if (allocatedQty >= qty)
        //            {
        //                return Json(new
        //                {
        //                    Success = true,
        //                    FLCCount = selected.Count,
        //                    AllocatedQty = allocatedQty,
        //                    Pallets = selected
        //                }, JsonRequestBehavior.AllowGet);
        //            }
        //        }
        //        return Json(new
        //        {
        //            Success = false,
        //            Message = "Insufficient stock available."
        //        }, JsonRequestBehavior.AllowGet);
        //    }
        //}


        //public JsonResult GetPalletDetails( string fgCode,string description,string model,int qty)
        //{
        //    using (var db = new S1947Entities())
        //    {
        //        var pallets = db.GoodsExisitings
        //            .Where(x => x.FGModel == model &&
        //                x.FGCode == fgCode &&
        //                x.FGDesc == description &&
        //                x.LocaStatus == "LOADED" &&
        //                x.Quantity > 0).ToList();

        //        // STEP 1: Check available quantity
        //        int availableQty = pallets.Sum(x => Convert.ToInt32(x.Quantity));
        //        if (qty <= 0 || availableQty < qty)
        //        {
        //            return Json(new
        //            {
        //                Success = false,
        //                Message = "Insufficient stock available.",
        //                AvailableQty = availableQty
        //            }, JsonRequestBehavior.AllowGet);
        //        }

        //        // STEP 3: Sort by actual loading sequence
        //        var sequencePallets = pallets.OrderBy(x => x.sequence).ToList();

        //        // STEP 4: Identify the loading pattern
        //        bool normalPattern = IsNormalLoadingPattern(sequencePallets);
        //        List<dynamic> selectedPallets;

        //        if (normalPattern)
        //        {
        //            // IF: Normal alternating-layer pattern
        //            selectedPallets = SelectUntilQuantity(sequencePallets, qty);
        //        }
        //        else
        //        {
        //            // NON-NORMAL PATTERN:
        //            // 1. Oldest loading date
        //            // 2. Shift priority
        //            // 3. Loading time within the shift


        //            // ELSE: Same-layer / non-normal pattern
        //            // Oldest loading date first
        //            var dateWisePallets = pallets.OrderBy(x => x.CreatedOn.HasValue ? x.CreatedOn.Value.Date : DateTime.MinValue).ToList();

        //            // Group by loading date. Shift priority must be
        //            // applied within the relevant date group using
        //            // your actual Shift Master fields.
        //            var priorityPallets = ApplyShiftPriority(dateWisePallets);

        //            // Choose from both ends of the row order.
        //            var alternatingPallets =GetAlternatingEnds(priorityPallets);

        //            selectedPallets = SelectUntilQuantity(alternatingPallets, qty);
        //        }


        //        // STEP 5: Return selected pallets
        //        var result = selectedPallets.Select(p => new
        //        {
        //            PalletNo = p.PalletNo,
        //            QR = p.PalletQr,
        //            Qty = p.Quantity,
        //            Location = p.LocationName,
        //            LocationId = p.MTransNo,
        //            Layer = p.Layer,
        //            Row = p.Row,
        //            Col = p.Col
        //        }).ToList();

        //        return Json(new
        //        {
        //            Success = true,
        //            FLCCount = result.Count,
        //            AllocatedQty = result.Sum(x => Convert.ToInt32(x.Qty)),
        //            Pallets = result,
        //            LoadingPattern = normalPattern? "Normal": "SameLayer"
        //        }, JsonRequestBehavior.AllowGet);
        //    }
        //}


        public JsonResult GetPalletDetails(string fgCode, string description, string model, int qty)
        {
            using (var db = new S1947Entities())
            {
                var pallets = db.GoodsExisitings
                    .Where(x =>
                        x.FGModel == model &&
                        x.FGCode == fgCode &&
                        x.FGDesc == description &&
                        x.LocaStatus == "LOADED" &&
                        x.Quantity > 0)
                    .ToList();

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

                // STEP 2: Check the existing loading sequence.

                var selectedPallets = SelectPalletsForUnloading(pallets, qty);

                // STEP 4: Return the selected pallet details.
                var result = selectedPallets.Select(p => new
                {
                    PalletNo = p.PalletNo,
                    QR = p.PalletQr,
                    Qty = p.Quantity,
                    Location = p.LocationName,
                    LocationId = p.MTransNo,
                    Layer = p.Layer,
                    Row = p.Row,
                    Col = p.Col,
                    Sequence = p.sequence,
                    CreatedOn = p.CreatedOn
                }).ToList();

                return Json(new
                {
                    Success = true,
                    FLCCount = result.Count,
                    AllocatedQty = result.Sum(x => Convert.ToInt32(x.Qty)),
                    RequestedQty = qty,
                    Pallets = result,
                }, JsonRequestBehavior.AllowGet);
            }
        }

        //from here it check is normal loading patternis there 
        private bool IsNormalLoadingPattern(List<GoodsExisiting> pallets)
        {
            if (pallets == null || pallets.Count <= 1)
                return true;

            var ordered = pallets.OrderBy(x => x.sequence).ToList();
            // Check consecutive pallets in sequence order.
            for (int i = 1; i < ordered.Count; i++)
            {
                var current = ordered[i];
                var previous = ordered[i - 1];

                int previousLayer = Convert.ToInt32(previous.Layer);
                int currentLayer = Convert.ToInt32(current.Layer);
                int previousRow = Convert.ToInt32(previous.Row);
                int currentRow = Convert.ToInt32(current.Row);

                // Different layers: normal pattern
                if (currentLayer != previousLayer)
                    continue;

                // Same layer, different categories: normal pattern
                if (current.Cat != previous.Cat)
                    continue;

                // Same layer and category, but different rows: normal
                if (previousRow != currentRow)
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

                // Whole pallets: stop when requested quantity is met
                // or exceeded.
                if (allocatedQty >= requestedQty)
                    break;
            }

            return selected;
        }



        private List<GoodsExisiting> SelectPalletsForUnloading(List<GoodsExisiting> pallets, int requestedQty)
        {
            var sequencePallets = pallets.OrderBy(x => x.sequence).ToList();
            bool normalPattern = IsNormalLoadingPattern(sequencePallets);
            if (normalPattern)
            {
                return SelectUntilQuantity(sequencePallets, requestedQty);
            }
            else
            {
                //var priorityPallets = ApplyShiftPriority(pallets);

                //return SelectUntilQuantity(priorityPallets, requestedQty);

                // NON-NORMAL PATTERN: Apply shift priority and alternating ends.
                var shiftPriority = ApplyShiftPriority(pallets);

                var shiftAlternating = GetAlternatingEnds(shiftPriority);

                var selected = SelectUntilQuantity(
                    shiftAlternating, requestedQty);

                if (selected.Sum(x => Convert.ToInt32(x.Quantity)) >= requestedQty)
                {
                    return selected;
                }

                // FALLBACK: Relax shift priority to date-wise priority.
                var datePriority = pallets
                    .OrderBy(x => x.CreatedOn.HasValue
                        ? x.CreatedOn.Value.Date
                        : DateTime.MaxValue)
                    .ThenBy(x => x.sequence)
                    .ToList();

                var dateAlternating = GetAlternatingEnds(datePriority);

                return SelectUntilQuantity(dateAlternating, requestedQty);

             }
        }

        private List<GoodsExisiting> ApplyDatePriority( List<GoodsExisiting> pallets)
        {
            return pallets
                .OrderBy(x => x.CreatedOn.HasValue
                    ? x.CreatedOn.Value.Date
                    : DateTime.MaxValue)
                .ThenBy(x => x.sequence)
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


        //private List<GoodsExisiting> GetAlternatingEnds(List<GoodsExisiting> pallets)
        //{
        //    var result = new List<GoodsExisiting>();

        //    if (pallets == null || pallets.Count == 0)
        //        return result;


        //    // First row, last row, second row, second-last row, and so on. This is a basic alternating-ends helper.
        //    //var ordered = pallets
        //    //    .OrderBy(x => x.Row)
        //    //    .ThenBy(x => x.sequence)
        //    //    .ToList();
        //    var groups = pallets.GroupBy(x => new { x.Layer, x.Col }).ToList();
        //    foreach (var group in groups)
        //    {
        //        var rows = group
        //            .OrderBy(x => x.Row)
        //            .ToList();

        //        int left = 0;
        //        int right = rows.Count - 1;

        //        while (left <= right)
        //        {
        //            result.Add(rows[left]);
        //            left++;

        //            if (left <= right)
        //            {
        //                result.Add(rows[right]);
        //                right--;
        //            }
        //        }
        //    }

        //    return result;
        //}



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

                int activeOperationCount = db.UnloadMaterialTemps
                                            .Where(x => x.status == "Pending")
                                            .GroupBy(x => x.DockNo)
                                            .Count();

                bool currentDockExists = db.UnloadMaterialTemps
                                        .Any(x =>
                                            x.DockNo == DockNo &&
                                            x.status == "Pending");


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

                        var pallets = db.GoodsExisitings
                            .Where(x =>
                                x.LocaStatus == "LOADED" &&
                                x.FGModel == Model &&
                                x.FGCode == FGCode &&
                                x.FGDesc == Description &&
                                x.Quantity > 0)
                            .ToList();

                        if (Quantity <= 0)
                        {
                            transaction.Rollback();

                            return Json(new
                            {
                                Success = false,
                                Message = "Requested quantity must be greater than zero."
                            });
                        }


                        List<GoodsExisiting> selectedPallets =
                            SelectPalletsForUnloading(pallets, Quantity);

                        int allocatedQty = selectedPallets.Sum(
                            x => Convert.ToInt32(x.Quantity));

                        if (allocatedQty < Quantity)
                        {
                            transaction.Rollback();

                            return Json(new
                            {
                                Success = false,
                                Message = "Insufficient stock available."
                            });
                        }


                        string firstSerialCode = null;
                        foreach (var pallet in selectedPallets)
                        {
                            // Generate a UNIQUE SerialCode for EACH pallet/task
                            string serialCode = GenerateOutboundSerialCode(db);
                            // Keep first SerialCode for UnloadMaterialTemp
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

                            //// 2.INSERT TASK DTL USING STORED PROCEDURE
                            //int nextPriority = db.TaskDtls
                            //                .Where(x => x.TaskType == "UNLOADOPERATION")
                            //                .Select(x => (int?)x.Priority)
                            //                .Max() ?? 0;
                            //nextPriority++;
                            //int nextTaskNo = db.TaskDtls
                            //                .Select(x => (int?)x.TaskNo)
                            //                .Max() ?? 0;

                            //nextTaskNo++;
                            //// Save the first task number
                            //if (firstTaskNo == 0)
                            //{
                            //    firstTaskNo = nextTaskNo;
                            //}
                            ////stored procedure
                            //db.InsertTaskDtl(nextTaskNo, "UNLOADOPERATION", nextPriority, pallet.PalletQr, "OUTBOUND", pallet.LocationName, "Pending", stationNo.ToString(), pallet.MTransNo.ToString(), nextPriority, DateTime.Now, DateTime.Now, null, null, serialCode);


                            //db.TaskReports.Add(new TaskReport
                            //{
                            //    TaskID = nextTaskNo,
                            //    SerialCode = serialCode,
                            //    StatusCode = "100",
                            //    StautusMessage = "Task Created",
                            //    OperationType = "OUTBOUND"
                            //});
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
                            FLCQty = selectedPallets.Count,
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
                            // First pallet's SerialCode
                            SerialCode = firstSerialCode,

                            status = "Pending",
                            Date = DateTime.Now.ToString("dd/MM/yyyy"),
                            Time = DateTime.Now.ToString("HH:mm"),
                            Shift = shift
                        };

                        db.UnloadMaterialTemps.Add(material);
                        db.SaveChanges();

                        //4. UPDATE LOCATION  in Goods Exisiting
                        foreach (var pallet in selectedPallets)
                        {
                            pallet.Quantity = 0;
                            pallet.LocaStatus = "OCCUPIED";
                            pallet.ModifiedOn = DateTime.Now;
                            db.Entry(pallet).State = EntityState.Modified;
                        }
                        db.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return Json(new
                        {
                            Success = false,
                            Message = ex.Message
                        });
                    }
                    transaction.Commit();
                    return Json(new
                    {
                        Success = true,
                        Message = "Loading saved successfully"
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


        //serial code generation here
        private string GenerateOutboundSerialCode(S1947Entities db)
        {
            string today = DateTime.Now.ToString("yyyyMMdd");
            string serialPrefix = "OUT" + today;

            // Check TaskDtls
            string lastTaskSerial = db.TaskDtls
                .Where(x =>
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
                    var tempData = db.UnloadTemps
                        .Where(x =>
                            x.Status == "Pending" &&
                            x.DockNo == model.DockNo)
                        .ToList();

                    var tempData1 = db.UnloadMaterialTemps
                        .Where(x =>
                            x.status == "Pending" &&
                            x.DockNo == model.DockNo)
                        .ToList();

                    if (!tempData.Any())
                    {
                        return Json(new
                        {
                            Success = false,
                            Message = "No pending records found."
                        });
                    }

                    foreach (var itemq in tempData1)
                    {
                        itemq.status = "Submit";
                        db.Entry(itemq).State = EntityState.Modified;
                    }

                    foreach (var item in tempData)
                    {
                        item.Status = "Submit";
                        item.DockNo = model.DockNo;
                        item.VehicleSize = model.VehicleSize;
                        item.VehicleNumber = model.VehicleNumber;
                        item.TransporterName = model.TransporterName;
                        item.VehicleSealNo = model.VehicleSealNo;
                        item.TotalQty = model.TotalQty;
                        item.CreatedOn = DateTime.Now;

                        db.Entry(item).State = EntityState.Modified;
                    }

                    db.SaveChanges();

                    submittedUnloadTemps = db.UnloadTemps
                        .Where(x =>
                            x.Status == "Submit" &&
                            x.DockNo == model.DockNo)
                        .OrderBy(x => x.MTransNo)
                        .ToList();

                    //new logic new part
                    var palletLocationIds = submittedUnloadTemps
                                            .Select(x => x.LocationId)
                                            .Where(x => !string.IsNullOrEmpty(x))
                                            .ToList();

                    var goodsByLocationId = db.GoodsExisitings
                        .Where(x => x.DeleteStatus == "N")
                        .ToList()
                        .Where(x => palletLocationIds.Contains(x.MTransNo.ToString()))
                        .GroupBy(x => x.MTransNo)
                        .ToDictionary(g => g.Key, g => g.First());

                    submittedUnloadTemps = submittedUnloadTemps.OrderBy(x =>
                        {
                            int locationId;

                            if (int.TryParse(x.LocationId, out locationId) &&
                                goodsByLocationId.ContainsKey(locationId))
                            {
                                return goodsByLocationId[locationId].CreatedOn;
                            }

                            return DateTime.MaxValue;
                        })
                        .ThenBy(x => x.MTransNo)
                        .ToList();


                    // INSERT TASK DTL USING STORED PROCEDURE
                    int firstTaskNo = 0;

                    //task priority
                    int nextPriority = db.TaskDtls
                                    .Where(x => x.TaskType == "UNLOADOPERATION")
                                    .Select(x => (int?)x.Priority)
                                    .Max() ?? 0;
                    nextPriority++;

                    ///task no 
                    int nextTaskNo = db.TaskDtls
                                    .Select(x => (int?)x.TaskNo)
                                    .Max() ?? 0;

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
                        //stored procedure
                        db.InsertTaskDtl(nextTaskNo, "UNLOADOPERATION", nextPriority, pallet.PlalletQR, "OUTBOUND", pallet.LocationName, "Pending", pallet.Station, pallet.MTransNo.ToString(), nextPriority, DateTime.Now, DateTime.Now, null, null, pallet.SerialCode);

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
                }

                // Build WCS task list
                List<WcsTaskRequest> wcsTasks = new List<WcsTaskRequest>();
                using (S1947Entities db = new S1947Entities())
                {
                    // Sequence for tasks in this API batch
                    int taskSequence = 1;
                    // Serial number for each column
                    Dictionary<int, int> columnSerialNumbers = new Dictionary<int, int>();
                    foreach (var item in submittedUnloadTemps)
                    {
                        int locationId;
                        if (!int.TryParse(item.LocationId, out locationId))
                        {
                            return Json(new
                            {
                                Success = false,
                                Message = "Invalid LocationId for UnloadTemp MTransNo: " + item.MTransNo
                            });
                        }

                        var goods = db.GoodsExisitings
                            .FirstOrDefault(x =>
                                x.MTransNo == locationId &&
                                x.LocaStatus == "OCCUPIED" &&
                                x.DeleteStatus == "N");

                        if (goods == null)
                        {
                            return Json(new
                            {
                                Success = false,
                                Message = "GoodsExisiting record not found for LocationId: " + item.LocationId
                            });
                        }

                        int stationNo = Convert.ToInt32(item.Station);

                        // Create ONE WCS task for ONE UnloadTemp row
                        // ----------------------------------------------------
                        int column = Convert.ToInt32(goods.Col);

                        if (!columnSerialNumbers.ContainsKey(column))
                        {
                            // First time this column appears
                            columnSerialNumbers[column] = 1;
                        }
                        else
                        {
                            // Column appeared again
                            columnSerialNumbers[column]++;
                        }

                        int serialNum = columnSerialNumbers[column];

                        WcsTaskRequest wcsTask = new WcsTaskRequest
                        {
                            SerialCode = item.SerialCode,
                            TrayCode = item.PlalletQR,
                            method = "outbound",
                            Layer = goods.Layer,// x-level
                            //From_Layer = goods.XPos,//level rmeoved on 3-09-2026
                            From_Column = goods.Col,//y-column
                            From_Row = goods.Row,//z-row
                            From_OrderRow = 0,  // that 0 means dynamic.
                            SortCode = taskSequence,// Normal unloading priority
                            SerialNum = serialNum,
                            StockSize = 0,
                            TraySize = 0, // Standard sizes
                            from_warehouse = "WH01",
                            to_channel = stationNo == 1 ? "lift06c90" : "lift06c91",
                        };
                        wcsTasks.Add(wcsTask);
                        taskSequence++;

                    }
                }
                if (!wcsTasks.Any())
                {
                    return Json(new
                    {
                        Success = false,
                        Message = "No records available to send to WCS."
                    });
                }
                //Send COMPLETE LIST to WCS commneted for testing purpose

                //============================================================
                WcsApiResponse wcsResponse = await SendTasksToWcs(wcsTasks);

                if (wcsResponse == null ||
                    wcsResponse.code != 200 ||
                    wcsResponse.status != true)
                {
                    return Json(new
                    {
                        Success = false,
                        Message = "WCS did not accept the task.",
                        WcsResponse = wcsResponse
                    });
                }

                // STEP 6
                // WCS SUCCESS
                using (S1947Entities db = new S1947Entities())
                {
                    foreach (var item in submittedUnloadTemps)
                    {
                        // Update UnloadTemp
                        var unloadTemp = db.UnloadTemps
                            .FirstOrDefault(x =>
                                x.MTransNo == item.MTransNo);

                        if (unloadTemp != null)
                        {
                            unloadTemp.Status = "PassedToAPI";

                            db.Entry(unloadTemp).State =
                                EntityState.Modified;
                        }
                        db.SaveChanges();
                    }
                }


                // Final response
                // ============================================================
                return Json(new
                {
                    Success = true,
                    Message = "Loading submitted and WCS tasks sent successfully.",
                    TotalTasksSent = wcsTasks.Count,
                    // WcsResponse = wcsResponse
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
        private async Task<WcsApiResponse> SendTasksToWcs(List<WcsTaskRequest> tasks)
        {
            string wcsUrl = ConfigurationManager.AppSettings["WcsInsertTaskUrl"];//fetching url

            string requestJson = JsonConvert.SerializeObject(tasks);
            const int maxAttempts = 3;
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    using (HttpClient client = new HttpClient())
                    {
                        client.Timeout = TimeSpan.FromSeconds(30);

                        client.DefaultRequestHeaders.Accept.Clear();

                        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                        using (var content = new StringContent(
                            requestJson,
                            Encoding.UTF8,
                            "application/json"))
                        {
                            HttpResponseMessage response = await client.PostAsync(wcsUrl, content);
                            string responseJson = await response.Content.ReadAsStringAsync();

                            // HTTP error
                            if (!response.IsSuccessStatusCode)
                            {
                                // Retry only HTTP 5xx
                                if ((int)response.StatusCode >= 500 &&
                                    attempt < maxAttempts)
                                {
                                    await Task.Delay(1000);
                                    continue;
                                }

                                SaveWcsLog(requestJson, responseJson, (int)response.StatusCode, null,
                                false, "FAILED", "HTTP error from WCS");

                                return new WcsApiResponse
                                {
                                    code = (int)response.StatusCode,
                                    status = false,
                                    message = "WCS HTTP error.",
                                    data = false
                                };
                            }

                            // Deserialize WCS response
                            WcsApiResponse result = JsonConvert.DeserializeObject<WcsApiResponse>(
                                    responseJson);

                            if (result == null)
                            {
                                SaveWcsLog(
                                    requestJson,
                                    responseJson,
                                    (int)response.StatusCode,
                                    null,
                                    false,
                                    "FAILED",
                                    "Invalid response from WCS");

                                return new WcsApiResponse
                                {
                                    code = 500,
                                    status = false,
                                    message = "Invalid response from WCS.",
                                    data = false
                                };
                            }

                            // WCS business failure
                            if (result.code != 200 || result.status != true)
                            {
                                SaveWcsLog(
                                    requestJson,
                                    responseJson,
                                    (int)response.StatusCode,
                                    result.code,
                                    result.status,
                                    "FAILED",
                                    result.message);

                                return result;
                            }

                            // WCS success
                            SaveWcsLog(
                                requestJson,
                                responseJson,
                                (int)response.StatusCode,
                                result.code,
                                result.status,
                                "SUCCESS",
                                null);

                            return result;
                        }
                    }
                }
                catch (TaskCanceledException ex)
                {
                    if (attempt < maxAttempts)
                    {
                        await Task.Delay(1000);
                        continue;
                    }

                    SaveWcsLog(
                    requestJson,
                    null,
                    null,
                    null,
                    false,
                    "EXCEPTION",
                    "WCS API timeout: " + ex.Message);

                    return new WcsApiResponse
                    {
                        code = 408,
                        status = false,
                        message = "WCS API timeout after 3 attempts.",
                        data = false
                    };
                }
                catch (HttpRequestException ex)
                {
                    // CONNECTION / NETWORK ERROR
                    SaveWcsLog(
                    requestJson,
                    null,
                    null,
                    null,
                    false,
                    "EXCEPTION",
                    "WCS connection error: " + ex.Message);

                    return new WcsApiResponse
                    {
                        code = 503,
                        status = false,
                        message = "Unable to connect to WCS after 3 atempts.",
                        data = false
                    };
                }
                catch (Exception ex)
                {
                    SaveWcsLog(
                        requestJson,
                        null,
                        null,
                        null,
                        false,
                        "EXCEPTION",
                        ex.ToString());

                    return new WcsApiResponse
                    {
                        code = 500,
                        status = false,
                        message = "Unexpected WCS API error.",
                        data = false
                    };
                }
            }
            return new WcsApiResponse
            {
                code = 500,
                status = false,
                message = "WCS request failed after 3 attempts.",
                data = false
            };
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




        /// for move task caling 

        private async Task<WcsApiResponse> SendMoveTaskToApi(int mtransno)
        {
            string apiUrl =
                System.Configuration.ConfigurationManager
                    .AppSettings["MoveTaskApiUrl"];

            if (string.IsNullOrWhiteSpace(apiUrl))
            {
                return new WcsApiResponse
                {
                    code = 500,
                    status = false,
                    message = "MoveTaskApiUrl is not configured.",
                    data = false
                };
            }

            // TEMPORARY REQUEST TEMPLATE.
            // Confirm the actual MOVE API request format before production use.
            var requestData = new
            {
                MTransNo = mtransno
            };

            using (var client = new HttpClient())
            {
                try
                {
                    string json = Newtonsoft.Json.JsonConvert
                        .SerializeObject(requestData);

                    var content = new StringContent(
                        json,
                        System.Text.Encoding.UTF8,
                        "application/json");

                    HttpResponseMessage response =
                        await client.PostAsync(apiUrl, content);

                    string responseJson =
                        await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        return new WcsApiResponse
                        {
                            code = (int)response.StatusCode,
                            status = false,
                            message = "MOVE API request failed: " + responseJson,
                            data = false
                        };
                    }

                    var result =
                        Newtonsoft.Json.JsonConvert
                            .DeserializeObject<WcsApiResponse>(responseJson);

                    if (result == null)
                    {
                        return new WcsApiResponse
                        {
                            code = 500,
                            status = false,
                            message = "Invalid response from MOVE API.",
                            data = false
                        };
                    }

                    return result;
                }
                catch (Exception ex)
                {
                    return new WcsApiResponse
                    {
                        code = 500,
                        status = false,
                        message = "MOVE API error: " + ex.Message,
                        data = false
                    };
                }
            }
        }


    }
}


