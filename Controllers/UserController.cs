using S1947.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace S1947.Controllers
{
    public class UserController : Controller
    {
        S1947Entities db = new S1947Entities();
        public ActionResult Index()
        {
            return View();
        }

        // Fetch Username Based on UserID
        public JsonResult GetUserName(string userId)
        {
            var user = db.UserMasters.FirstOrDefault(u => u.UserId == userId);
            if (user != null)
            {
                return Json(new { success = true, userName = user.UserName }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new { success = false, message = "User Not Found." }, JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult FingerPrint()
        {
            ViewBag.UserId = new SelectList(db.UserMasters.Where(x => x.DeleteStatus == "N" && x.LockStatus == "Y").OrderBy(x => x.MTransNo).Select(x => new
            {
                code = x.UserId,
                name = x.UserId + " : - " + x.UserName
            }), "code", "name").Distinct().ToList();
            return View();
        }
        public ActionResult AddEdit(int MTransNo = 0)
        {
            ViewBag.UserType = new List<SelectListItem>(){
                new SelectListItem {Text="ADMIN", Value="ADMIN", Selected=false},
                //new SelectListItem {Text="SUPERVISOR", Value="SUPERVISOR", Selected=false},
                new SelectListItem {Text="OPERATOR", Value="OPERATOR", Selected=false},
                };
            ViewBag.LockStatus = new List<SelectListItem>(){
                new SelectListItem {Text="Active", Value="Y", Selected=false},
                new SelectListItem {Text="Inactive", Value="N", Selected=false},
                };
            UserMasterValidation user = new UserMasterValidation();
            var data = db.UserMasters.Where(s => s.MTransNo == MTransNo).FirstOrDefault();
            if (MTransNo > 0)
            {
                string dec = data.PW;
                if (dec != "")
                {
                    dec = HomeController.Decrypt(data.PW, "mskl-1sv4-xklq23");
                }

                user.MTransNo = MTransNo;
                user.UserType = data.UserType;
                user.UserName = data.UserName;
                user.ContactNo = data.ContactNo;
                user.LockStatus = data.LockStatus;
                user.UserId = data.UserId;
                user.EmailId = data.EmailId;
                user.PW = dec;
                return View(user);
            }
            else
            {
                string userid = "";
                user.UserId = userid;
                return View(user);
            }
            //return View();
        }

        [HttpPost]
        public ActionResult AddEdit(UserMasterValidation data)
        {
            UserMasterValidation b = new UserMasterValidation();

            try
            {
                // ==========================================
                // EDIT EXISTING USER
                // ==========================================
                if (data.MTransNo > 0)
                {
                    UserMaster user = db.UserMasters
                        .Where(s => s.MTransNo == data.MTransNo)
                        .FirstOrDefault();

                    string encVa = HomeController.Encrypt(
                        data.PW,
                        "mskl-1sv4-xklq23"
                    );

                    var ModifiedBy = Session["MTransNo"];

                    if (user != null)
                    {
                        user.UserName = data.UserName;
                        user.UserId = data.UserId;
                        user.ContactNo = data.ContactNo;
                        user.UserType = data.UserType;
                        user.LockStatus = data.LockStatus;
                        user.EmailId = data.EmailId;   
                        user.PW = encVa;
                        user.ModifiedBy = Convert.ToInt16(ModifiedBy);
                        user.ModifiedOn = DateTime.UtcNow.AddHours(5).AddMinutes(30);

                        db.Entry(user).State =System.Data.Entity.EntityState.Modified;
                        db.SaveChanges();

                        return Json(new
                        {
                            success = true,
                            message = "✅ User Updated Successfully!"
                        });
                    }
                    else
                    {
                        ModelState.AddModelError(
                            "",
                            "The Specified MTransNo Does Not Exist."
                        );

                        return View(b);
                    }
                }

                // ==========================================
                // ADD NEW USER
                // ==========================================
                else
                {
                    var createdBy = Session["MTransNo"];

                    string encVa = HomeController.Encrypt(
                        data.PW,
                        "mskl-1sv4-xklq23"
                    );

                    UserMaster UserMaster = new UserMaster();

                    UserMaster.UserType = data.UserType;
                    UserMaster.UserName = data.UserName;
                    UserMaster.ContactNo = data.ContactNo;
                    UserMaster.LockStatus = data.LockStatus;
                    UserMaster.DeleteStatus = "N";
                    UserMaster.UserId = data.UserId;
                    UserMaster.PW = encVa;
                    UserMaster.EmailId = data.EmailId;
                    UserMaster.CreatedBy = Convert.ToInt16(createdBy);
                    UserMaster.CreatedOn = DateTime.UtcNow
                        .AddHours(5)
                        .AddMinutes(30);

                    db.UserMasters.Add(UserMaster);

                    // First save UserMaster so MTransNo is generated
                    db.SaveChanges();


                    // ==========================================
                    // CREATE DEFAULT USER AUTHORIZATIONS
                    // ==========================================

                    int userNo = UserMaster.MTransNo;

                    // ------------------------------------------
                    // ADMIN
                    // Give ADMIN access to ALL modules
                    // ------------------------------------------
                    if (data.UserType.Equals(
                        "ADMIN",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        var allModules = db.Menulists
                            .Select(x => new
                            {
                                x.MTransNo,
                                x.ModuleType
                            })
                            .ToList();

                        foreach (var module in allModules)
                        {
                            UserAuthorization objD =
                                new UserAuthorization
                                {
                                    UserNo = userNo,
                                    ModuleType = module.ModuleType,
                                    ModuleNo = module.MTransNo,
                                    AuthType = "F"
                                };

                            db.UserAuthorizations.Add(objD);
                        }
                    }

                    // ------------------------------------------
                    // OPERATOR
                    // Give OPERATOR:
                    // OPERATIONS + REPORTS
                    // ------------------------------------------
                    else if (data.UserType.Equals(
                        "OPERATOR",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        var operatorModules = db.Menulists
                            .Where(x =>
                                x.ModuleType == "OPERATIONS" ||
                                x.ModuleType == "REPORTS")
                            .Select(x => new
                            {
                                x.MTransNo,
                                x.ModuleType
                            })
                            .ToList();

                        foreach (var module in operatorModules)
                        {
                            UserAuthorization objD =
                                new UserAuthorization
                                {
                                    UserNo = userNo,
                                    ModuleType = module.ModuleType,
                                    ModuleNo = module.MTransNo,
                                    AuthType = "F"
                                };

                            db.UserAuthorizations.Add(objD);
                        }
                    }

                    // Save authorizations
                    db.SaveChanges();


                    return Json(new
                    {
                        success = true,
                        message = "✅ User Added Successfully!"
                    });
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(
                    "",
                    "An Error Occurred While Saving The Data: " +
                    ex.Message
                );

                return View(b);
            }
        }

        public ActionResult Delete(int MTransNo)
        {
            S1947Entities db = new S1947Entities();
            UserMasterValidation q = new UserMasterValidation();
            try
            {
                var item = db.UserMasters.FirstOrDefault(a => a.MTransNo == MTransNo);
                var modifiedby = Session["Userid"];
                if (item != null && item.LockStatus == "N")
                {
                    item.DeleteStatus = "Y";
                    item.ModifiedBy = Convert.ToInt16(modifiedby);
                    item.ModifiedOn = DateTime.UtcNow.AddHours(5).AddMinutes(30);
                    db.Entry(item).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges();
                    //db.InsertAdminLog(item.UserName, item.UserId, item.UserType, 0, Convert.ToString(Session["UserId"]), 0, 0, 0, "USER_DELETED", DateTime.UtcNow.AddHours(5).AddMinutes(30), 0, 0,0);
                    return Json(new { success = true });
                }
                return Json(new { success = false });
            }
            catch
            {
                return Json(new { success = false });
            }
        }
        //public JsonResult isExists(string UserId)
        //{
        //    var exists = db.UserMasters.Any(x => x.UserId == UserId  && x.DeleteStatus == "N");
        //    return Json(!exists, JsonRequestBehavior.AllowGet);
        //}
        public JsonResult GetDtlFinger(string userCode)
        {
            var check = db.UserMasters.Where(x => x.UserId == userCode && x.LockStatus == "Y" && x.DeleteStatus == "N").FirstOrDefault();
            return Json(check, JsonRequestBehavior.AllowGet);
        }
        public JsonResult isCodeExists(string UserId)
        {
            return Json(!db.UserMasters.Any(x => x.UserId == UserId), JsonRequestBehavior.AllowGet);
        }
    }
}