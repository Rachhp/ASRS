using S1947.Models; // Ensure this matches your namespace
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace S1947.Controllers
{
    public class UnloadController : Controller
    {
        S1947Entities conn = new S1947Entities();
        // GET: Unload/UnloadOperation
        public ActionResult UnloadOperation()
        {

            int dockMaxRange = (int)conn.Othermasters
                     .Select(x => x.MaxDockRange)
                     .FirstOrDefault();

            ViewBag.DockNo = Enumerable.Range(1, dockMaxRange)
                .Select(x => new SelectListItem
                {
                    Text = x.ToString(),
                    Value = x.ToString()
                })
                .ToList();



            //ViewBag.DockNo = new List<SelectListItem>
            //{
            //    new SelectListItem { Text = "1", Value = "1" },
            //    new SelectListItem { Text = "2", Value = "2" },
            //    new SelectListItem { Text = "3", Value = "3" },
            //    new SelectListItem { Text = "4", Value = "4" }
            //};

            //fetch the data from location table
            var models = conn.GoodsExisitings
                   .Where(x => x.LocaStatus == "LOADED")
                   .Select(x => x.FGCode)
                   .Distinct()
                   .ToList();

            ViewBag.SAPFGCode = new SelectList(models);
            return View();
        }

        //security unload screen 
        public ActionResult UnloadScan()
        {
            var model = new UnloadValidation();
            model.DockList = conn.UnloadedDatas
             .Select(x => x.DockNo)
             .Distinct()
             .OrderBy(x => x)
             .Select(x => new SelectListItem
             {
                 Text = x.ToString(),
                 Value = x.ToString()
             })
             .ToList();


            return View(model);
        }

        [HttpGet]
        public JsonResult GetInvoiceDetails(int dockNo)
        {
            // Get active invoice for the selected dock
            var invoiceNo = conn.UnloadedDatas
                                .Where(x => x.DockNo == dockNo && x.Status== "MATERIALUNLOAD")
                                .Select(x => x.InvoiceNo)
                                .FirstOrDefault();


            var data = conn.UnloadedDatas
              .Where(x => x.InvoiceNo == invoiceNo)
              .ToList();

            var nextPallet = data
                .Where(x => x.Status == "MATERIALUNLOAD")
                .OrderBy(x => x.MTransNo)
                .Select(x => new
                {
                    x.PalletNo,
                    x.PalletQR,
                    x.SAPFGCode,
                    x.TotalQty,
                    x.FLCLocation,
                    x.Status
                })
                .FirstOrDefault();

            return Json(new
            {
                invoiceNo = invoiceNo,
                customer = data.FirstOrDefault()?.CustomerName,
                vehicleNumber = data.FirstOrDefault()?.VehicleNumber,
                vehicleSize = data.FirstOrDefault()?.VehicleSize,
                expected = data.FirstOrDefault()?.FLCLocation ?? 0,
                scanned = data.Count(x => x.Status == "MATERIALSCAN"),
                pallet = nextPallet
            }, JsonRequestBehavior.AllowGet);


            //var result = new
            //{
            //    customer = data.FirstOrDefault()?.CustomerName,
            //    expected = data.FirstOrDefault()?.FLCLocation,//total incoice pallets
            //    scanned = data.Count(x => x.Status == "MATERIALSCAN"),
            //    pallet = nextPallet
            //};

            //  return Json(result, JsonRequestBehavior.AllowGet);
        }

        public JsonResult ScanPallet(string qrCode, int dockNo)
        {
            // 1. Find pallet
            // Find active invoice for this dock
            var invoiceNo = conn.UnloadedDatas
                                .Where(x => x.DockNo == dockNo)
                                .Select(x => x.InvoiceNo)
                                .FirstOrDefault();
            if (string.IsNullOrEmpty(invoiceNo))
            {
                return Json(new
                {
                    success = false,
                    message = "No active invoice found for this dock."
                });
            }

            var pallet = conn.UnloadedDatas.FirstOrDefault(x =>
        x.PalletQR == qrCode &&
        x.InvoiceNo == invoiceNo);

            if (pallet == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Invalid QR Code."
                });
            }

            if (pallet.Status == "MATERIALSCAN")
            {
                return Json(new
                {
                    success = false,
                    message = "Already scanned."
                });
            }

            var nextSeq = conn.UnloadedDatas
                .Where(x => x.InvoiceNo == invoiceNo)
                .Max(x => (int?)x.BoxLocationNo) ?? 0;

            pallet.Status = "MATERIALSCAN";
            pallet.BoxLocationNo = nextSeq + 1;
            pallet.LoadingstartTime = DateTime.Now;

            conn.SaveChanges();

            var data = conn.UnloadedDatas
                           .Where(x => x.InvoiceNo == invoiceNo)
                           .ToList();

            var nextPallet = data
                .Where(x => x.Status == "MATERIALUNLOAD")
                .OrderBy(x => x.MTransNo)
                .Select(x => new
                {
                    x.PalletQR,
                    x.PalletNo,
                    x.SAPFGCode,
                    x.TotalQty,
                    x.FLCLocation,
                    x.Status
                })
                .FirstOrDefault();

            return Json(new
            {
                success = true,
                completed = nextPallet == null,
                scanned = data.Count(x => x.Status == "MATERIALSCAN"),
                expected = data.FirstOrDefault()?.FLCLocation ?? 0,
                pallet = nextPallet
            });
        }



        /// unload screen part
        [HttpGet]
        public JsonResult GetModelDetails(string FGCode)
        {
            var data = conn.GoodsExisitings
                           .Where(x => x.FGCode == FGCode &&
                                       x.LocaStatus == "LOADED")
                           .GroupBy(x => new
                           {
                               x.FGModel,
                               x.FGDesc
                           })
                           .Select(g => new
                           {
                               PartNo = g.Key.FGModel,
                               PartDescription = g.Key.FGDesc,
                               Quantity = g.Sum(x => x.Quantity)
                           })
                           .ToList();

            return Json(data, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetShippingInfo()
        {
            var data = conn.UnloadTemps
                         .OrderBy(x => x.MTransNo)
                         .Select(x => new
                         {
                             x.InvoiceNo,
                             x.CustomerName,
                             x.LocationofDelivery,
                             x.TransporterName,
                             x.VehicleNumber,
                             x.VehicleSealNo,
                             x.FLCLocation,
                             x.DockNo,
                             x.VehicleSize,
                             x.SupervisiorName,
                             x.MobileNoSupervisor,
                             x.DriverName,
                             x.MobileNoDriver,
                             x.SecurityName,
                             x.MobileNoSecurity
                         })
                         .FirstOrDefault();

            return Json(data, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult SaveUnloadData(UnloadValidation model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    using (S1947Entities db = new S1947Entities())
                    {
                        var pallets = db.GoodsExisitings
                            .Where(x =>
                                x.LocaStatus == "LOADED"
                                && x.FGCode == model.SAPFGCode
                                && x.FGDesc == model.PartDescription
                                && x.FGModel == model.PartNo
                                && !string.IsNullOrEmpty(x.PalletNo)
                                && !string.IsNullOrEmpty(x.PalletQr)
                            )
                            .OrderBy(x => x.Row)
                            .ToList();

                        int remainingQty = model.TotalQuantity;

                        List<GoodsExisiting> usedPallets = new List<GoodsExisiting>();

                        foreach (var p in pallets)
                        {
                            if (remainingQty <= 0)
                                break;

                            // ❌ skip if not enough for full pallet
                            if (p.Quantity > remainingQty)
                                continue;

                            int unloadQty = (int)p.Quantity;
                            remainingQty -= unloadQty;

                            // insert to temp 
                            UnloadTemp ut = new UnloadTemp
                            {
                                InvoiceNo = model.InvoiceNo,
                                CustomerName = model.CustomerName,
                                LocationofDelivery = model.LocationofDelivery,
                                FGModel = model.PartNo,
                                FGModelDesc = model.PartDescription,
                                SAPFGCode = model.SAPFGCode,
                                PartQuantity = unloadQty,
                                FLCLocation = model.FLCQty,
                                VehicleSize = model.VehicleSize,
                                VehicleNumber = model.VehicleNumber,
                                TransporterName = model.TransporterName,
                                DockNo = model.DockNo,
                                VehicleSealNo = model.VehicleSealno,
                               // BoxQty = model.BoxQty,
                                TotalQty = unloadQty,
                                Status = "Added",
                                SupervisiorName = model.SupervisiorName,
                                MobileNoSupervisor = model.MobileNoSupervisor,
                                DriverName = model.DriverName,
                                MobileNoDriver = model.MobileNoDriver,
                                SecurityName = model.SecurityName,
                                MobileNoSecurity = model.MobileNoSecurity,

                                LocationId = p.MTransNo.ToString(),
                                LocationName = p.LocationName,
                                PalletNo = p.PalletNo,
                                PlalletQR = p.PalletQr
                            };

                            db.UnloadTemps.Add(ut);
                            db.SaveChanges();

                            //GoodsExisiting
                            p.Quantity = 0;
                            p.LocaStatus = "OCCUPIED";
                            db.Entry(p).State = EntityState.Modified;
                            usedPallets.Add(p);
                            db.SaveChanges();

                            ///after completion of procces 
                            // Get completed temp records
                            var tempData = db.UnloadTemps
                                             .Where(x => x.InvoiceNo == model.InvoiceNo
                                                         /* &&x.Status == "Completed"*/)
                                             .ToList();

                            foreach (var item in tempData)
                            {
                                db.UnloadingLogs.Add(new UnloadingLog
                                {
                                    InvoiceNo = item.InvoiceNo,
                                    CustomerName = item.CustomerName,
                                    LocationofDelivery = item.LocationofDelivery,
                                    FGModel = item.FGModel,
                                    FGDesc = item.FGModelDesc,
                                    FGCode = item.SAPFGCode,
                                    Quantity = item.PartQuantity,
                                    FLCLocation = item.FLCLocation,
                                    VehicleSize = item.VehicleSize,
                                    VehicleNumber = item.VehicleNumber,
                                    TransporterName = item.TransporterName,
                                    PlalletQR = item.PlalletQR,
                                    PlalletSrNo = item.PalletNo,
                                    LocationId = item.LocationId,
                                    LocationName = item.LocationName,
                                    DockNo = item.DockNo,
                                   // BoxQty = item.BoxQty,
                                    TotalQty = item.TotalQty,
                                    SupervisiorName = item.SupervisiorName,
                                    MobileNoSupervisor = item.MobileNoSupervisor,
                                    DriverName = item.DriverName,
                                    MobileNoDriver = item.MobileNoDriver,
                                    SecurityName = item.SecurityName,
                                    MobileNoSecurity = item.MobileNoSecurity,
                                    LoadingstartTime = DateTime.Now,
                                    Status = "PARTUNLOADED",
                                    Action = "UNLOADOPERATION",
                                    CreatedBy = Convert.ToInt16(Session["MTransNo"]),
                                    CreatedOn = DateTime.UtcNow.AddHours(5).AddMinutes(30)
                                });

                                db.UnloadedDatas.Add(new UnloadedData
                                {
                                    InvoiceNo = item.InvoiceNo,
                                    CustomerName = item.CustomerName,
                                    LocationofDelivery = item.LocationofDelivery,
                                    FGModel = item.FGModel,
                                    FGModelDesc = item.FGModelDesc,
                                    SAPFGCode = item.SAPFGCode,
                                    PartQuantity = item.PartQuantity,
                                    FLCLocation = item.FLCLocation,
                                    VehicleSize = item.VehicleSize,
                                    VehicleNumber = item.VehicleNumber,
                                    TransporterName = item.TransporterName,
                                    DockNo = item.DockNo,
                                   // BoxQty = item.BoxQty,
                                    TotalQty = item.TotalQty,
                                    VehicleSealNo = item.VehicleSealNo,
                                    PalletNo = item.PalletNo,
                                    PalletQR = item.PlalletQR,
                                    SupervisiorName = item.SupervisiorName,
                                    MobileNoSupervisor = item.MobileNoSupervisor,
                                    DriverName = item.DriverName,
                                    MobileNoDriver = item.MobileNoDriver,
                                    SecurityName = item.SecurityName,
                                    MobileNoSecurity = item.MobileNoSecurity,
                                    Status = "MATERIALUNLOAD",
                                    Action = "UNLOADOPERATION",
                                    CreatedBy = Convert.ToInt16(Session["MTransNo"]),
                                    CreatedOn = DateTime.UtcNow.AddHours(5).AddMinutes(30)
                                });
                            }

                            db.SaveChanges();
                        }
                        //    // insert UnloadingLog 
                        //    var unload = new UnloadingLog
                        //    {
                        //        InvoiceNo = model.InvoiceNo,
                        //        CustomerName = model.CustomerName,
                        //        LocationofDelivery = model.LocationofDelivery,
                        //        FGModel = model.PartNo,
                        //        FGModelDesc = model.PartDescription,
                        //        SAPFGCode = model.SAPFGCode,
                        //        PartQuantity = model.TotalQuantity,
                        //        FLCLocation = model.FLCQty,
                        //        VehicleSize = model.VehicleSize,
                        //        VehicleNumber = model.VehicleNumber,
                        //        TransporterName = model.TransporterName,
                        //        PlalletQR = p.ScannedQr,
                        //        PlalletNo = p.PalletNo,
                        //        LocationId = p.MTransNo.ToString(),
                        //        LocationName = p.LocationNo,
                        //        DockNo = model.DockNo,
                        //        BoxQty = model.BoxQty,
                        //        TotalQty = model.TotalQuantity,
                        //        LoadingstartTime = DateTime.Now,
                        //        Status = "PARTUNLOADED",
                        //        Action = "UNLOADOPERATION",
                        //        CreatedBy = Convert.ToInt16(Session["MTransNo"]),
                        //        CreatedOn = DateTime.UtcNow.AddHours(5).AddMinutes(30),
                        //    };

                        //    db.UnloadingLogs.Add(unload);

                        //    // insert to unload data
                        //    UnloadedData UD = new UnloadedData
                        //    {
                        //        InvoiceNo = model.InvoiceNo,
                        //        CustomerName = model.CustomerName,
                        //        LocationofDelivery = model.LocationofDelivery,
                        //        FGModel = model.PartNo,
                        //        FGModelDesc = model.PartDescription,
                        //        SAPFGCode = model.SAPFGCode,
                        //        PartQuantity = model.TotalQuantity,
                        //        FLCLocation = model.FLCQty,
                        //        VehicleSize = model.VehicleSize,
                        //        VehicleNumber = model.VehicleNumber,
                        //        TransporterName = model.TransporterName,
                        //        DockNo = model.DockNo,
                        //        BoxQty = model.BoxQty,
                        //        TotalQty = model.TotalQuantity,
                        //        VehicleSealNo = model.DockNo.ToString(),
                        //        PalletNo = p.PalletNo,
                        //        PalletQR = p.ScannedQr,
                        //        Status = "MATERIALUNLOAD",
                        //        Action = "UNLOADOPERATION",
                        //        CreatedBy = Convert.ToInt16(Session["MTransNo"]),
                        //        CreatedOn = DateTime.UtcNow.AddHours(5).AddMinutes(30)
                        //    };

                        //    db.UnloadedDatas.Add(UD);
                        //    db.SaveChanges();
                        //}

                        // ❌ If nothing matched
                        if (!usedPallets.Any())
                        {
                            return Json(new
                            {
                                success = false,
                                message = "No full pallets available for requested quantity"
                            });
                        }

                     

                        return Json(new { success = true, message = "Data Saved Successfully!" });
                    }
                }
                   

                var firstError = ModelState.Values
                                   .SelectMany(v => v.Errors)
                                   .Select(e => e.ErrorMessage)
                                   .FirstOrDefault();

                return Json(new
                {
                    success = false,
                    message = firstError
                });
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException ex)
            {
                var errors = ex.EntityValidationErrors
                    .SelectMany(x => x.ValidationErrors)
                    .Select(x => x.PropertyName + " : " + x.ErrorMessage);

                return Json(new
                {
                    success = false,
                    message = string.Join(", ", errors)
                });
            }
        }

        [HttpPost]
        public JsonResult BlockCell(int locationId)
        {
            // your logic
            return Json(new { success = true, message = "Location blocked successfully" });
        }

        [HttpPost]
        public JsonResult UnblockCell(int locationId)
        {
            // your logic
            return Json(new { success = true, message = "Location unblocked successfully" });
        }

        [HttpGet]
        public JsonResult GetUnloadList()
        {
            // Replace with your actual Database Query logic
            // Example: var list = db.UnloadTemp.OrderByDescending(x => x.MTransNo).ToList();
            // var list = new List<object>(); // Fetch your data here
            var list = conn.UnloadTemps.OrderBy(x => x.MTransNo).ToList();

            return Json(list, JsonRequestBehavior.AllowGet);
        }


        [HttpPost]
        public JsonResult DeleteUnloadEntry(int id)
        {
            try
            {
                using (var db = new S1947Entities()) // Use your actual DbContext name
                {
                    // Find the record in the UnloadTemp table using MTransNo
                    var record = db.UnloadTemps.FirstOrDefault(x => x.MTransNo == id);

                    if (record != null)
                    {
                        db.UnloadTemps.Remove(record);
                        db.SaveChanges();
                        return Json(new { success = true, message = "Entry deleted successfully!" });
                    }

                    return Json(new { success = false, message = "Record not found." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Database Error: " + ex.Message });
            }
        }



        [HttpPost]
        public JsonResult AutoUpdateStatus()
        {
            try
            {
                var record = conn.UnloadTemps
                                 .Where(x => x.Status != "Completed")
                                 .OrderBy(x => x.MTransNo)
                                 .ToList();

                if (record == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "No pending records"
                    });
                }
                else
                {
                    foreach (var item in record)
                    {
                        switch (item.Status)
                        {
                            case "Added":
                                item.Status = "Task Assigned";
                                break;
                            case "Task Assigned":
                                item.Status = "Vehicle Picked";
                                break;
                            case "Vehicle Picked":
                                item.Status = "In Process";
                                break;
                            case "In Process":
                                item.Status = "Completed";
                                break;


                        }

                        conn.SaveChanges();
                    }

                    return Json(new
                    {
                        success = true,
                    });
  
                }



            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }
    }
}