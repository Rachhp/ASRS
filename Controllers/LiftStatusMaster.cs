using S1947.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace S1947.Controllers
{
    public class LiftStatusMasterController : Controller
    {
        // GET: MaterialMaster
        //For common use
        S1947Entities db = new S1947Entities();
        public ActionResult AddEdit(string LiftNo = "LOAD")
        {
            var model = new Shuttlemastervalidationcs();

            var data = db.LiftStatusMasters
                         .FirstOrDefault(x => x.LiftNo == LiftNo);

            if (data != null)
            {
                model.MTransNo = data.MTransNo;
                model.LiftNo = data.LiftNo;
                model.RunMode = data.RunMode;
            }
            else
            {
                model.LiftNo = LiftNo;
                model.RunMode = 1;
            }

            return View(model);
        }


        // POST: Save Mode
        [HttpPost]
        public JsonResult SaveData(string LiftNo, int RunMode)
        {
            try
            {
                string operationMessage;
                if (IsOperationInProcess(LiftNo, out operationMessage))
                {
                    return Json(new
                    {
                        success = false,
                        operationInProcess = true,
                        message = operationMessage
                    });
                }

                var data = db.LiftStatusMasters
                             .FirstOrDefault(x => x.LiftNo == LiftNo);

                if (data == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Lift side record not found"
                    });
                }

                data.RunMode = RunMode;
                data.UpdatedDate = DateTime.Now;

                db.SaveChanges();

                ApplyLiftLogic(LiftNo, RunMode);

                return Json(new
                {
                    success = true,
                    message = LiftNo + " mode updated successfully!"
                });
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


        private bool IsOperationInProcess(string liftNo, out string message)
        {
            message = "";

            if (liftNo == "LOAD")
            {
                bool loadingInProcess = db.LoadingTemps.Any();

                if (loadingInProcess)
                {
                    message = "Loading operation is currently in process. Mode change is not allowed.";
                    return true;
                }
            }
            else if (liftNo == "UNLOAD")
            {
               
                bool unloadMaterialInProcess = db.UnloadMaterialTemps.Any(x => x.status == "submit");
                bool unloadInProcess = db.UnloadTemps
                                         .Any(x => x.Status == "PROCESSING");

                if (unloadMaterialInProcess || unloadInProcess)
                {
                    message = "Unloading operation is currently in process. Mode change is not allowed.";
                    return true;
                }
            }

            return false;
        }


        private void ApplyLiftLogic(string liftNo, int mode)
        {
            if (liftNo == "LOAD")
            {
                if (mode == 1)
                {
                    // LOAD LIFT 1 ON
                    // LOAD LIFT 2 OFF
                }
                else if (mode == 2)
                {
                    // LOAD LIFT 1 OFF
                    // LOAD LIFT 2 ON
                }
                else if (mode == 3)
                {
                    // LOAD LIFT 1 ON
                    // LOAD LIFT 2 ON
                }
            }
            else if (liftNo == "UNLOAD")
            {
                if (mode == 1)
                {
                    // UNLOAD LIFT 1 ON
                    // UNLOAD LIFT 2 OFF
                }
                else if (mode == 2)
                {
                    // UNLOAD LIFT 1 OFF
                    // UNLOAD LIFT 2 ON
                }
                else if (mode == 3)
                {
                    // UNLOAD LIFT 1 ON
                    // UNLOAD LIFT 2 ON
                }
            }
        }


    }
}

