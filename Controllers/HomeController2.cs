using S1947.Models;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web.Mvc;

namespace S1947.Controllers
{
    public class HomeController2 : Controller
    {
        public ActionResult Login()
        {
            return View();
        }
        public ActionResult Init()
        {
            return View();
        }

        public static string Encrypt(string input, string key)
        {
            byte[] inputArray = UTF8Encoding.UTF8.GetBytes(input);
            TripleDESCryptoServiceProvider tripleDES = new TripleDESCryptoServiceProvider();
            tripleDES.Key = UTF8Encoding.UTF8.GetBytes(key);
            tripleDES.Mode = CipherMode.ECB;
            tripleDES.Padding = PaddingMode.PKCS7;
            ICryptoTransform cTransform = tripleDES.CreateEncryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(inputArray, 0, inputArray.Length);
            tripleDES.Clear();
            return Convert.ToBase64String(resultArray, 0, resultArray.Length);
        }
        public static string Decrypt(string input, string key)
        {
            byte[] inputArray = Convert.FromBase64String(input);
            TripleDESCryptoServiceProvider tripleDES = new TripleDESCryptoServiceProvider();
            tripleDES.Key = UTF8Encoding.UTF8.GetBytes(key);
            tripleDES.Mode = CipherMode.ECB;
            tripleDES.Padding = PaddingMode.PKCS7;
            ICryptoTransform cTransform = tripleDES.CreateDecryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(inputArray, 0, inputArray.Length);
            tripleDES.Clear();
            return UTF8Encoding.UTF8.GetString(resultArray);
        }
        [HttpPost]
        public ActionResult Login1(string UserId, string PW)
        {
            using (S1947Entities db = new S1947Entities())
            {
                // Retrieve user by UserId and PW
                var user = db.UserMasters.FirstOrDefault(u => u.UserId == UserId.Trim() && u.LockStatus == "Y");

                if (user != null && user.DeleteStatus == "N")
                {
                    string pw = user.PW.Trim();
                    string encVa = Encrypt(PW, "mskl-1sv4-xklq23");
                    string dec = Decrypt(encVa, "mskl-1sv4-xklq23");
                    if (pw.Equals(encVa))
                    {
                        db.SaveChanges();
                        // Store user information in the session
                        Session["UserId"] = user.UserId;
                        Session["UserType"] = user.UserType;
                        Session["UserName"] = user.UserName;
                        return RedirectToAction("Index", "Dashboard");

                    }
                    else if (user != null && user.DeleteStatus == "Y")
                    {

                        TempData["wrongLogin"] = "Please Contact Admin.";
                        return RedirectToAction("Login");
                    }
                    TempData["wrongLogin"] = "Login id / Password does not match";
                    return RedirectToAction("Login");
                }
                else
                {
                    TempData["wrongLogin"] = "Login id / Password does not match";
                    return RedirectToAction("Login");
                }
            }
        }

        public ActionResult Index()
        {
            if (Session["UserId"] != null)
            {
                ViewBag.UserName = Session["UserName"];
                return View();
            }
            else
            {
                return RedirectToAction("Login", "Home");
            }
        }
        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();

            return RedirectToAction("Login", "Home");
        }

    }
}