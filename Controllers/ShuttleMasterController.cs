using S1947.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace S1947.Controllers
{
    public class ShuttleMasterController : Controller
    {
        // GET: MaterialMaster
        //For common use
        S1947Entities conn = new S1947Entities();
        // TABLE SCREEN
        public ActionResult Index()
        {
            var data = conn.ShuttleDtls.ToList();
            return View(data);
        }

        public ActionResult AddEdit(int MTransNo = 0)
        {
            Shuttlemastervalidationcs model = new Shuttlemastervalidationcs();

            var dropdown = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "-- Select Status --" },
                new SelectListItem { Value = "Y", Text = "Active" },
                new SelectListItem { Value = "N", Text = "Inactive" }
            };

            if (MTransNo > 0)
            {
                var data = conn.ShuttleDtls.FirstOrDefault(x => x.MTransNo == MTransNo);

                if (data != null)
                {
                    model.MTransNo = data.MTransNo;
                    model.ShuttleNo = data.ShuttleNo;
                    model.LockStatus = data.LockStatus?.Trim(); // important
                }
            }
            else
            {
                model.ShuttleNo = conn.ShuttleDtls
                    .OrderBy(x => x.MTransNo)
                    .Select(x => x.ShuttleNo)
                    .FirstOrDefault();

                model.LockStatus = "";
            }

            ViewBag.lockstatus = new SelectList(
                dropdown,
                "Value",
                "Text",
                model.LockStatus
            );

            ViewBag.ShuttleList = conn.ShuttleDtls.ToList();

            // 🔥 MUST BE LAST LINE BEFORE RETURN
            ModelState.Clear();

            return View(model);
        }
        //     public ActionResult AddEdit(int MTransNo = 0)
        //     {
        //         Shuttlemastervalidationcs model = new Shuttlemastervalidationcs();

        //         // STATUS DROPDOWN
        //         var dropdown = new List<SelectListItem>
        //         {
        //         new SelectListItem { Value = "", Text = "-- Select Status --" },
        //         new SelectListItem { Value = "Y", Text = "Active" },
        //         new SelectListItem { Value = "N", Text = "Inactive" }
        //         };


        //         //if (MTransNo == 0)
        //         //{
        //         //    model.ShuttleNo = conn.ShuttleDtls
        //         //                          .OrderBy(x => x.MTransNo)
        //         //                          .Select(x => x.ShuttleNo)
        //         //                          .FirstOrDefault();
        //         //}

        //         //ViewBag.lockstatus = new SelectList(
        //         //    new[]
        //         //    {
        //         //        new { Value = "Y", Text = "Active" },
        //         //        new { Value = "N", Text = "Inactive", }
        //         //    },
        //         //    "Value",
        //         //    "Text",
        //         //    true

        //         //);

        //         //// TABLE DATA
        //         //ViewBag.ShuttleList = conn.ShuttleDtls.ToList();

        //         if (MTransNo > 0)
        //         {
        //             var data = conn.ShuttleDtls.FirstOrDefault(x => x.MTransNo == MTransNo);

        //             if (data != null)
        //             {
        //                 model.MTransNo = data.MTransNo;
        //                 model.ShuttleNo = data.ShuttleNo;
        //                 model.LockStatus = data.LockStatus; // ✅ key fix
        //             }
        //         }
        //         else
        //         {
        //             model.ShuttleNo = conn.ShuttleDtls
        //                 .OrderBy(x => x.MTransNo)
        //                 .Select(x => x.ShuttleNo)
        //                 .FirstOrDefault();

        //             model.LockStatus = ""; // ✅ force empty on create
        //         }


        //         ViewBag.lockstatus = new SelectList(
        //    dropdown,
        //    "Value",
        //    "Text",
        //    model.LockStatus // ✅ correct selected value
        //);

        //         ViewBag.ShuttleList = conn.ShuttleDtls.ToList();

        //         return View(model);
        //     }

        public ActionResult SaveData(int MTransNo, string status)
        {
            // oonly update 
            var data = conn.ShuttleDtls.Where(x => x.MTransNo == MTransNo).FirstOrDefault();
            if (data != null)
            {
                //update the status 
                data.LockStatus = status;
                data.CreatedBy = Convert.ToInt16(Session["MTransNo"]);
                data.CreatedOn = DateTime.Now;
                data.DeleteStatus = "N";
                conn.Entry(data).State = System.Data.Entity.EntityState.Modified;
                conn.SaveChanges();
                return Json(new { success = true, message = "✅ Data Updated Successfully!" });

            }
            return Json(new
            {
                success = false,
                message = "Record Not Found"
            });

        }

    }
}
