using Newtonsoft.Json.Linq;
using S1947.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;

namespace S1947.Controllers
{
    public class AndonController : Controller
    {
        UtilityController utility = new UtilityController();

        public ActionResult AndonScreen()
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

                model.LoadingHistory = db.UnloadMaterialTemps
                          .OrderBy(x => x.id)
                          .ToList();

                var latestLoading = db.UnloadMaterialTemps
                                      .OrderByDescending(x => x.id)
                                      .FirstOrDefault();

                if (latestLoading != null)
                {
                    model.DockNo = latestLoading.DockNo;
                    model.VehicleSize =latestLoading.VehicleSize;
                    model.VehicleNumber = latestLoading.VehicleRegNo;
                    model.VehicleSealNo = latestLoading.VehicleSealNo;
                    model.TransporterName = latestLoading.TransporterName;
                    model.DriverName = latestLoading.DriverName;
                    model.MobileNoDriver = latestLoading.MobileNoDriver;
                    model.SecurityName = latestLoading.SecurityName;
                    model.MobileNoSecurity = latestLoading.MobileNoSecurity;
                    model.SupervisiorName = latestLoading.SupervisiorName;
                    model.MobileNoSupervisor = latestLoading.MobileNoSupervisor;
                    //add her eloading star and end time 
                }

                model.ASRSLoadingStatus = db.UnloadTemps
                            .OrderByDescending(x => x.MTransNo)
                            .ToList();

                return View(model);
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


        [HttpPost]
    public JsonResult SaveLoading(int DockNo,
                                    int VehicleSize,
                                    string VehicleNumber,
                                    string TransporterName,
                                    string VehicleSealNo,

                                    string InvoiceNo,
                                    string CustomerName,
                                    string Location,
                                    string Model,
                                    string FGCode,
                                    string Description,
                                    int Quantity,
                                    int AvailableQty,
                                    string DriverName,
                                    string DriverMobile,
                                    string SecurityName,
                                    string SecurityMobile,
                                    string SupervisorName,
                                    string SupervisorMobile)
        {
            string shift = utility.GetCurrentShiftName();
            using (var db = new S1947Entities())
            {
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
                            .OrderBy(x => x.Row)
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

                        foreach (var pallet in selectedPallets)
                        {

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
                                FLCLocation = selectedPallets.Count,
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
                                DockNo = DockNo,
                                PalletNo = pallet.PalletNo,
                                PlalletQR = pallet.PalletQr,
                                LocationId =
                                pallet.MTransNo.ToString(),
                                LocationName =
                                pallet.LocationName,
                                Status = "Pending",
                                CreatedBy = "System",
                                CreatedOn = DateTime.Now
                            };
                            db.UnloadTemps.Add(temp);

                        }
                        db.SaveChanges();

                        //unload material temp table insert for printing the label
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
                            DriverName = DriverName,
                            MobileNoDriver = DriverMobile,
                            SecurityName = SecurityName,
                            MobileNoSecurity = SecurityMobile,
                            SupervisiorName = SupervisorName,
                            MobileNoSupervisor = SupervisorMobile,
                            DockNo = DockNo,
                            Date = DateTime.Now.ToString("dd/MM/yyyy"),
                            Time = DateTime.Now.ToString("HH:mm"),
                            Shift = shift
                        };

                        db.UnloadMaterialTemps.Add(material);
                        db.SaveChanges();


                        //=========================================
                        // UPDATE LOCATION MASTER
                        //=========================================


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

        
    }
}


