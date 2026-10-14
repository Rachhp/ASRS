using S1947.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace S1834.Controllers
{
    public class UserAuthController : Controller
    {
        public ActionResult Index()
        {           
            if (Session["Userid"] == null)
            {
                return RedirectToAction("Login", "Home");
            }

            using (S1947Entities db = new S1947Entities())
            {
                var userList = (
                    from U in db.UserMasters
                    where U.DeleteStatus == "N"
                       && U.LockStatus == "Y"
                       && U.UserType != "ADMIN"
                       && db.UserAuthorizations.Any(A => A.UserNo == U.MTransNo)
                    orderby U.UserName
                    select new UserAuthorizationValidation
                    {
                        UserNo = U.MTransNo,
                        UserName = U.UserName,
                        UserId = U.UserId
                    }
                ).ToList();

                return View(userList);
            }
        }


        // ADD / EDIT
        [HttpGet]
        public ActionResult AddEdit(int? UserNo)
        {
            if (Session["Userid"] == null)
            {
                return RedirectToAction("Login", "Home");
            }

            using (S1947Entities db = new S1947Entities())
            {
               
                var users = db.UserMasters
                    .Where(x => x.DeleteStatus == "N"
                             && x.LockStatus == "Y"
                             && x.UserType != "ADMIN")
                    .OrderBy(x => x.UserName)
                    .ToList();

                ViewBag.UserNo = new SelectList
                    (users, "MTransNo","UserName", UserNo
                );

                UserAuthorizationValidation model =new UserAuthorizationValidation();

                if (UserNo.HasValue && UserNo.Value > 0)
                {
                    model.UserNo = UserNo.Value;
                }

                var modules = db.Menulists
                    .OrderBy(x => x.ModuleType)
                    .ThenBy(x => x.MSLNo)
                    .ToList();

                ViewBag.Modules = modules;
                var existingAuth = db.UserAuthorizations
                    .Where(x => x.UserNo == UserNo)
                    .ToList();

                ViewBag.ExistingAuth = existingAuth;


                return View(model);
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult SaveAuthorization(int UserNo,string AuthorizationData)
        {
            if (Session["Userid"] == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Session expired. Please login again."
                });
            }

            try
            {
                using (S1947Entities db = new S1947Entities())
                {
                    // Check user
                    var user = db.UserMasters.FirstOrDefault(x =>
                        x.MTransNo == UserNo &&
                        x.DeleteStatus == "N" &&
                        x.LockStatus == "Y");

                    if (user == null)
                    {
                        return Json(new
                        {
                            success = false,
                            message = "Invalid user selected."
                        });
                    }

                    var oldAuth = db.UserAuthorizations.Where(x => x.UserNo == UserNo).ToList();
                    if (oldAuth.Any())
                    {
                        db.UserAuthorizations.RemoveRange(oldAuth);
                    }

                    if (!string.IsNullOrEmpty(AuthorizationData))
                    {
                        string[] rows = AuthorizationData.Split('~');
                        foreach (string row in rows)
                        {
                            if (string.IsNullOrWhiteSpace(row))
                                continue;

                            string[] values = row.Split('^');
                            if (values.Length < 3)
                                continue;

                            int moduleNo;

                            if (!int.TryParse(values[0], out moduleNo))
                                continue;

                            string moduleType = values[1];
                            string authType = values[2];

                            // Only save Full Authorization
                            if (authType == "F")
                            {
                                UserAuthorization obj =new UserAuthorization();
                                obj.UserNo = UserNo;
                                obj.ModuleNo = moduleNo;
                                obj.ModuleType = moduleType;
                                obj.AuthType = "F";
                                db.UserAuthorizations.Add(obj);
                            }
                        }
                    }

                    db.SaveChanges();
                    return Json(new
                    {
                        success = true,
                        message = "Authorization saved successfully."
                    });
                }
            }
            catch (Exception ex)
            {
                string message = ex.Message;
                if (ex.InnerException != null)
                {
                    message += " | " + ex.InnerException.Message;
                }
                return Json(new
                {
                    success = false,
                    message = message
                });
            }
        }

    
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Delete(int UserNo)
        {
            if (Session["Userid"] == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Session expired. Please login again."
                });
            }
            try
            {
                using (S1947Entities db = new S1947Entities())
                {
                    var authList = db.UserAuthorizations
                        .Where(x => x.UserNo == UserNo)
                        .ToList();

                    if (authList.Any())
                    {
                        db.UserAuthorizations.RemoveRange(authList);
                        db.SaveChanges();
                    }
                    return Json(new
                    {
                        success = true,
                        message = "Authorization deleted successfully."
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
