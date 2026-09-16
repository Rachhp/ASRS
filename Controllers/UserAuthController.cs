using S1947.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace S1834.Controllers
{
    public class UserAuthController : Controller
    {
        // GET: UserAuthorization        
        public ActionResult Index()
        {
            if (Session["Userid"] != null)
            {
                return View();
            }
            else
            {
                return RedirectToAction("Login", "Home");
            }
        }
        public ActionResult AddEdit(int? MTransNo)
        {
            if (Session["Userid"] == null)
            {
                return RedirectToAction("Login", "Home");
            }
            //DateTime TodayDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(Convert.ToString(Session["TimeZone"])));
            DateTime TodayDate = DateTime.Now;

            S1947Entities conn = new S1947Entities();
            //ViewBag.BranchNo = new SelectList(conn.BranchMasters.Where(x => x.DeleteStatus == "N" && x.LockStatus == "Y").OrderBy(x => x.BranchName), "MTransNo", "BranchName");
            var existingUserNos = conn.UserAuthorizations.Select(ua => ua.UserNo).ToList();

            ViewBag.UserNo = new SelectList(conn.UserMasters.Where(x => x.DeleteStatus == "N" && x.LockStatus == "Y" && x.UserType != "ADMIN" && !existingUserNos.Contains(x.MTransNo))
                    .OrderBy(x => x.UserId), "MTransNo", "UserName");

            var ModuleType = (from CM in conn.Menulists
                              select new
                              {
                                  ModuleType = CM.ModuleType,
                              }).Distinct().OrderBy(x => x.ModuleType).ToList();
            ViewBag.ModuleType = new SelectList(ModuleType, "ModuleType", "ModuleType");
            ViewBag.AuthType = new List<SelectListItem>(){
                new SelectListItem {Text="No Authorization", Value="N", Selected=false},
                //new SelectListItem {Text="Read Only", Value="R", Selected=false},
                new SelectListItem {Text="Full Authorization", Value="F", Selected=false}
                };
            if (MTransNo > 0)
            {
                var UserAuthModule = conn.UserAuthorizations.Where(x => x.MTransNo == MTransNo).FirstOrDefault();
                UserAuthorizationValidation obj = new UserAuthorizationValidation();
                obj.UserNo = UserAuthModule.UserNo;
                obj.ModuleType = UserAuthModule.ModuleType.Trim();
                obj.MTransNo = UserAuthModule.MTransNo;

                if (obj.AuthType != null)
                {
                    obj.AuthType = "F";
                }
                return View(obj);
            }
            else
            {
                UserAuthorizationValidation UserAuthModule = new UserAuthorizationValidation();
                return View(UserAuthModule);
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(true)]
        public ActionResult AddEdit(UserAuthorizationValidation UserAuthModule, string MainSubmit, string Add)
        {

            if (Session["Userid"] == null)
            {
                return RedirectToAction("Login", "Home");
            }
            else
            {
                if (!string.IsNullOrEmpty(Add))
                {
                    S1947Entities db = new S1947Entities();
                    UserAuthorization obj = db.UserAuthorizations.Where(x => x.UserNo == UserAuthModule.UserNo && x.ModuleType == UserAuthModule.ModuleType).FirstOrDefault();
                    if (obj != null)
                    {
                        Session["MTransNo"] = obj.MTransNo;
                        return RedirectToAction("AddEdit", new { MTransNo = obj.MTransNo });
                    }
                    else
                    {
                        UserAuthorization objD = new UserAuthorization();
                        objD.UserNo = UserAuthModule.UserNo;
                        objD.ModuleType = UserAuthModule.ModuleType;
                        //objD.BranchNo = UserAuthModule.BranchNo;
                        objD.ModuleNo = 0;
                        objD.AuthType = "";
                        db.UserAuthorizations.Add(objD);
                        db.SaveChanges();
                        Session["MTransNo"] = objD.MTransNo;
                        // Suppose objD.MTransNo contains the value you want to send
                        return RedirectToAction("AddEdit", new { MTransNo = objD.MTransNo });

                    }
                }
                else
                {
                    return RedirectToAction("Index");
                }
            }
        }
        //public String SaveDtl(int UserNo,string ModuleType, String DtlString,String Status)
        //{
        //    int MTransNo = 0;
        //    S1834Entities db = new S1834Entities();
        //    string strText = DtlString;
        //    string[] arrayStock = strText.Split('~');
        //    foreach (string str in arrayStock)
        //    {
        //        string[] arrayValues = str.Split('^');
        //        try
        //        {
        //            if (arrayValues[3] == "1")
        //            {
        //                if (Convert.ToInt32(arrayValues[2]) == 0)
        //                {
        //                    UserAuthorization objD = new UserAuthorization();
        //                    objD.UserNo = UserNo;
        //                    objD.ModuleType = ModuleType;
        //                    //objD.BranchNo = BranchNo;
        //                    objD.ModuleNo = Convert.ToInt32(arrayValues[0]);
        //                    objD.AuthType = arrayValues[1];
        //                    db.UserAuthorizations.Add(objD);
        //                    db.SaveChanges();
        //                    MTransNo = objD.MTransNo;
        //                }
        //                else
        //                {
        //                    MTransNo = Convert.ToInt32(arrayValues[2]);
        //                    UserAuthorization objD = db.UserAuthorizations.Where(x => x.MTransNo == MTransNo).FirstOrDefault();
        //                    objD.AuthType = arrayValues[1];
        //                    db.Entry(objD).State = System.Data.Entity.EntityState.Modified;
        //                    db.SaveChanges();
        //                }
        //            }
        //        }
        //        catch (Exception e)
        //        {
        //            System.Diagnostics.Debug.WriteLine(e.StackTrace);
        //        }
        //    }
        //    return AddEdit(MTransNo);

        //}
        [HttpPost]
        public string SaveDtl(int UserNo, string ModuleType, string DtlString, string Status)
        {
            int MTransNo = 0;
            using (S1947Entities db = new S1947Entities())
            {
                string[] arrayStock = DtlString.Split('~');

                foreach (string str in arrayStock)
                {
                    string[] arrayValues = str.Split('^');
                    try
                    {
                        if (arrayValues[3] == "1")
                        {
                            if (Convert.ToInt32(arrayValues[2]) == 0)
                            {
                                UserAuthorization objD = new UserAuthorization
                                {
                                    UserNo = UserNo,
                                    ModuleType = ModuleType,
                                    ModuleNo = Convert.ToInt32(arrayValues[0]),
                                    AuthType = arrayValues[1]
                                };
                                db.UserAuthorizations.Add(objD);
                                db.SaveChanges();
                                MTransNo = objD.MTransNo;
                            }
                            else
                            {
                                MTransNo = Convert.ToInt32(arrayValues[2]);
                                UserAuthorization objD = db.UserAuthorizations.FirstOrDefault(x => x.MTransNo == MTransNo);
                                if (objD != null)
                                {
                                    objD.AuthType = arrayValues[1];
                                    db.Entry(objD).State = System.Data.Entity.EntityState.Modified;
                                    db.SaveChanges();
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        System.Diagnostics.Debug.WriteLine(e.StackTrace);
                    }
                }
            }

            return MTransNo.ToString(); // <-- return as string for JavaScript
        }

        public ActionResult DtlDelete(int MTransNo = 0)
        {
            if (MTransNo == 0)
                return RedirectToAction("Index");

            using (S1947Entities db = new S1947Entities())
            {
                var authList = db.UserAuthorizations
                                 .Where(x => x.UserNo == MTransNo)
                                 .ToList();

                if (authList.Any())
                {
                    db.UserAuthorizations.RemoveRange(authList);
                    db.SaveChanges();
                }

                Session["MTransNo"] = MTransNo;
            }

            return RedirectToAction("Index");
        }
        public ActionResult AuthDelete(int MTransNo = 0)
        {
            if (MTransNo == 0)
                return RedirectToAction("Index");
            using (S1947Entities db = new S1947Entities())
            {
                var authList = db.UserAuthorizations.Where(x => x.MTransNo == MTransNo).ToList();
                if (authList.Any())
                {
                    db.UserAuthorizations.RemoveRange(authList);
                    db.SaveChanges();
                }
                Session["MTransNo"] = MTransNo;
            }
            return RedirectToAction("Index");
        }
    }
}