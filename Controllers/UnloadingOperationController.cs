using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.EMMA;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Bcpg;
using S1947.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Data.SqlClient;
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
                var result = db.GoodsExisitings
                    .Where(x => x.FGModel == modelName
                             && x.LocaStatus == "LOADED")
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
                        LatestModifiedOn = g.Max(x => x.ModifiedOn)
                    })
                    .OrderBy(x => x.LatestModifiedOn)
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
                    .Where(x =>
                        x.FGModel == model &&
                        x.FGCode == fgCode &&
                        x.FGDesc == description &&
                        x.LocaStatus == "LOADED" &&
                        x.Quantity > 0)
                    .OrderBy(x => x.ModifiedOn)
                    .ThenBy(x => x.Row)
                    .ToList();

                int allocatedQty = 0;

                List<object> selected = new List<object>();

                foreach (var pallet in pallets)
                {
                    selected.Add(new
                    {
                        PalletNo = pallet.PalletNo,
                        QR = pallet.PalletQr,
                        Qty = pallet.Quantity,
                        Location = pallet.LocationName,
                        LocationId = pallet.MTransNo
                    });

                    allocatedQty += Convert.ToInt32(pallet.Quantity);
                    // Stop as soon as requested quantity is satisfied
                    if (allocatedQty >= qty)
                    {
                        return Json(new
                        {
                            Success = true,
                            FLCCount = selected.Count,
                            AllocatedQty = allocatedQty,
                            Pallets = selected
                        }, JsonRequestBehavior.AllowGet);
                    }
                }
                return Json(new
                {
                    Success = false,
                    Message = "Insufficient stock available."
                }, JsonRequestBehavior.AllowGet);
            }
        }


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
                                x.Quantity > 0
                            )
                            .OrderBy(x => x.ModifiedOn)
                            .ThenBy(x => x.Row)
                            .ToList();

                        int allocatedQty = 0;

                        List<GoodsExisiting> selectedPallets = new List<GoodsExisiting>();
                        foreach (var pallet in pallets)
                        {
                            selectedPallets.Add(pallet);
                            allocatedQty += Convert.ToInt32(pallet.Quantity);

                            if (allocatedQty >= Quantity)
                                break;
                        }

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
                    foreach (var pallet in tempData)
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
                            // AGV handover station  rmeoved on 3-09-2026
                            //extraInfo = new WcsExtraInfo
                            //{
                            //    station_no = "AGV_STATION_A0" + stationNo
                            //}
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
                // Send COMPLETE LIST to WCS
                // ============================================================
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
                    WcsResponse = wcsResponse
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

            string requestJson =
                JsonConvert.SerializeObject(tasks);
            const int maxAttempts = 3;
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    using (HttpClient client = new HttpClient())
                    {
                        client.Timeout = TimeSpan.FromSeconds(30);

                        client.DefaultRequestHeaders.Accept.Clear();

                        client.DefaultRequestHeaders.Accept.Add(
                            new MediaTypeWithQualityHeaderValue(
                                "application/json"));

                        using (var content = new StringContent(
                            requestJson,
                            Encoding.UTF8,
                            "application/json"))
                        {
                            HttpResponseMessage response =
                                await client.PostAsync(wcsUrl, content);

                            string responseJson =
                                await response.Content.ReadAsStringAsync();

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

                                SaveWcsLog(
                                requestJson,
                                responseJson,
                                (int)response.StatusCode,
                                null,
                                false,
                                "FAILED",
                                "HTTP error from WCS");

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



    }
}


