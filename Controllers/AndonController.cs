using Newtonsoft.Json.Linq;
using S1947.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace S1947.Controllers
{
    public class AndonController : Controller
    {
        // Change the field name from 'utility' to '_utility' to avoid ambiguity
        private readonly UtilityController _utility = new UtilityController();

        UtilityController utility = new UtilityController();

        [HttpGet]
        public ContentResult TestTVIP()
        {
            return Content(
                "Remote IP: " + Request.UserHostAddress
            );
        }


        private int GetAndonScreenId()
        {
            string clientIp = Request.UserHostAddress;

            string tv1Ip = ConfigurationManager.AppSettings["AndonTV1IP"];
            string tv2Ip = ConfigurationManager.AppSettings["AndonTV2IP"];

            if (clientIp == tv1Ip)
                return 1;

            if (clientIp == tv2Ip)
                return 2;

            return 0;
        }

        public ActionResult AndonScreen()
        {
            AndonViewModel model = new AndonViewModel();

            int screenId = GetAndonScreenId();

            using (S1947Entities db = new S1947Entities())
            {
                model.CustomerList = db.CustomerMasters
                    .Where(x => x.DeleteStatus == "N" && x.LockStatus == "Y")
                    .Select(x => x.CustomerName)
                    .ToList();

                model.ModelList = db.GoodsExisitings
                    .Where(x => x.LocaStatus == "LOADED")
                    .Select(x => x.FGModel)
                    .Distinct()
                    .ToList();

                model.LoadingHistory = db.UnloadMaterialTemps
                    .OrderBy(x => x.id)
                    .ToList();

                model.ASRSLoadingStatus = db.UnloadTemps
                    .OrderByDescending(x => x.MTransNo)
                    .ToList();

                ViewBag.ScreenId = screenId;

                return View(model);
            }
        }

        [HttpGet]
        public JsonResult GetAndonDock(int screenId)
        {
            using (S1947Entities db = new S1947Entities())
            {
                // Get all docks currently available in UnloadedDatas
                var dockList = db.UnloadedDatas
                    .Select(x => x.DockNo)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();

                // If no dock records exist yet,
                // use default Dock 1 and Dock 2.
                if (!dockList.Any())
                {
                    dockList = new List<int?> { 1, 2 };
                }

                var activeDocks = new List<int>();
                var notStartedDocks = new List<int>();

                foreach (var dockNo in dockList)
                {
                    var records = db.UnloadedDatas
                        .Where(x => x.DockNo == dockNo)
                        .ToList();

                    if (!records.Any())
                    {
                        notStartedDocks.Add((int)dockNo);
                        continue;
                    }

                    bool completed = records.All(x => x.Status == "COMPLETED");
                    bool inProcess = records.Any(x => x.Status == "MATERIALSCAN");

                    if (!completed && inProcess)
                    {
                        activeDocks.Add((int)dockNo);
                    }
                    else if (!completed)
                    {
                        notStartedDocks.Add((int)dockNo);
                    }
                }

                // Active docks get first priority
                var selectedDocks = activeDocks
                    .OrderBy(x => x)
                    .Take(2)
                    .ToList();

                // Fill remaining screen slots with not-started docks
                foreach (var dock in notStartedDocks.OrderBy(x => x))
                {
                    if (selectedDocks.Count >= 2)
                        break;

                    if (!selectedDocks.Contains(dock))
                        selectedDocks.Add(dock);
                }

                // If still no docks, default to 1 and 2
                if (!selectedDocks.Any())
                {
                    selectedDocks.Add(1);
                    selectedDocks.Add(2);
                }

                // Make sure we have two positions
                if (selectedDocks.Count == 1)
                {
                    foreach (var dock in dockList.OrderBy(x => x))
                    {
                        if (dock != selectedDocks[0])
                        {
                            selectedDocks.Add((int)dock);
                            break;
                        }
                    }
                }

                int selectedDock;

                if (screenId == 1)
                {
                    selectedDock = selectedDocks[0];
                }
                else
                {
                    selectedDock = selectedDocks.Count > 1
                        ? selectedDocks[1]
                        : selectedDocks[0];
                }

                return Json(new
                {
                    ScreenId = screenId,
                    DockNo = selectedDock
                }, JsonRequestBehavior.AllowGet);
            }
        }



        [HttpGet]
        public JsonResult GetVehicleLoadingStatus(int dockNo)
        {
            using (S1947Entities db = new S1947Entities())
            {
                var dtl = db.UnloadedDatas.Where(x => x.DockNo == dockNo).ToList();
                string status = "R";

                if (dtl.Any())
                {
                    if (dtl.All(x => x.Status == "COMPLETED"))
                        status = "G";
                    else if (dtl.Any(x => x.Status == "MATERIALSCAN"))
                        status = "Y";
                    else
                        status = "R";
                }
                int scannedCount = dtl.Count(x => x.Status == "MATERIALSCAN");
                return Json(new
                {
                    Status = status,
                    ScannedCount = scannedCount
                }, JsonRequestBehavior.AllowGet);
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
                        AvailableQty = g.Sum(x => x.Quantity)
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
                    .Where(x =>
                        x.FGModel == model &&
                        x.FGCode == fgCode &&
                        x.FGDesc == description &&
                        x.LocaStatus == "LOADED" &&
                        x.Quantity > 0)
                    .OrderBy(x => x.Row)
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



    }
}


