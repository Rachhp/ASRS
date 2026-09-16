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
                if (data.MTransNo > 0)
                {
                    // Edit existing User Master
                    UserMaster user = db.UserMasters.Where(s => s.MTransNo == data.MTransNo).FirstOrDefault();
                    string encVa = HomeController.Encrypt(data.PW, "mskl-1sv4-xklq23");
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
                        db.Entry(user).State = System.Data.Entity.EntityState.Modified;
                        db.SaveChanges();

                        //AdminLog ad = new AdminLog();
                        //ad.MaterialCode = data.UserId;
                        //ad.MaterialName = data.UserName;
                        //ad.PackageType = data.UserType;
                        //ad.CreatedBy = Convert.ToInt16(ModifiedBy);
                        //ad.CreatedOn = DateTime.Now;
                        //ad.ReturnType= data.LockStatus;
                        //ad.Action = "User Updated";
                        //db.AdminLogs.Add(ad);
                        //db.SaveChanges();




                        return Json(new { success = true, message = "✅ User Updated Successfully!" });
                        //db.InsertAdminLog(user.UserName, user.UserId, user.UserType, 0, Convert.ToString(Session["UserId"]), 0, 0, 0, "USER_UPDATED", DateTime.UtcNow.AddHours(5).AddMinutes(30), 0, 0,0);

                    }
                    else
                    {
                        ModelState.AddModelError("", "The Specified MTransNo Does Not Exist.");
                        return View(b);
                    }
                }
                else
                {
                    var createdBy = Session["MTransNo"];
                    string encVa = HomeController.Encrypt(data.PW, "mskl-1sv4-xklq23");

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
                    UserMaster.CreatedOn = DateTime.UtcNow.AddHours(5).AddMinutes(30);

                    db.UserMasters.Add(UserMaster);
                    db.SaveChanges();


                    //AdminLog ad = new AdminLog();
                    //ad.MaterialCode = data.UserId;
                    //ad.MaterialName = data.UserName;
                    //ad.PackageType = data.UserType;
                    //ad.CreatedBy = Convert.ToInt16(createdBy);
                    //ad.CreatedOn = DateTime.Now;
                    //ad.ReturnType = data.LockStatus;
                    //ad.Action = "User Added";
                    //db.AdminLogs.Add(ad);
                    //db.SaveChanges();

                    //Added user auth table to get by default issue and return opertion
                    int userNo = UserMaster.MTransNo;
                    int[] moduleNos = { 6, 7 };
                    foreach (int moduleNo in moduleNos)
                    {
                        UserAuthorization objD = new UserAuthorization
                        {
                            UserNo = userNo,
                            ModuleType = "OPERATIONS",
                            ModuleNo = moduleNo,
                            AuthType = "F"
                        };

                        db.UserAuthorizations.Add(objD);
                    }
                    db.SaveChanges();

                    return Json(new { success = true, message = "✅ User Added Successfully!" });
                }
                //return RedirectToAction("Index", "User");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An Error Occurred While Saving The Data: " + ex.Message);
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