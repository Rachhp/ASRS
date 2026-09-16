using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace S1947
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        protected void Application_BeginRequest()
        {
            var culture = new System.Globalization.CultureInfo("en-US");
            System.Threading.Thread.CurrentThread.CurrentCulture = culture;
            System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
        }


        //protected void Application_Error(object sender, EventArgs e)
        //{
        //    Exception ex = Server.GetLastError();
        //    //var error = ex;

        //    Session["LastError"] = ex.Message;

        //    //Server.ClearError();


        //    S1947Entities db = new S1947Entities();

        //    string stc = ex.StackTrace;
        //    string msg = ex.Message;
        //    if (stc.Length >= 500)
        //    {
        //        stc = stc.Substring(0, 495);
        //    }

        //    if (msg.Length >= 500)
        //    {
        //        msg = msg.Substring(0, 495);
        //    }
        //    ErrorLogData obj = new ErrorLogData();
        //    obj.StackTrace = stc;
        //    obj.Message = msg;
        //    try
        //    {
        //        obj.CreatedBy = Convert.ToByte(Session["Userid"]);
        //        obj.UserType = Convert.ToString(Session["UserType"]);
        //    }
        //    catch
        //    {
        //        obj.CreatedBy = 0;
        //    }

        //    obj.DeleteStatus = "N";
        //    obj.CreatedOn = DateTime.Now;
        //    db.ErrorLogDatas.Add(obj);
        //    db.SaveChanges();
        //    Server.ClearError();
        //    Response.Redirect("~/Error/ServerError");
        //}
        //protected void Application_Error(object sender, EventArgs e)
        //{
        //    Exception ex = Server.GetLastError();
        //    //var error = ex;

        //    GlobalVeriables.Exception = ex;

        //    //Server.ClearError();


        //    S1947Entities db = new S1947Entities();

        //    string stc = ex.StackTrace;
        //    string msg = ex.Message;
        //    if (stc.Length >= 500)
        //    {
        //        stc = stc.Substring(0, 495);
        //    }

        //    if (msg.Length >= 500)
        //    {
        //        msg = msg.Substring(0, 495);
        //    }
        //    ErrorLogData obj = new ErrorLogData();
        //    obj.StackTrace = stc;
        //    obj.Message = msg;
        //    try
        //    {
        //        obj.CreatedBy = Convert.ToByte(Session["Userid"]);
        //        obj.UserType = Convert.ToString(Session["UserType"]);
        //    }
        //    catch
        //    {
        //        obj.CreatedBy = 0;
        //    }
        //    obj.DeleteStatus = "N";
        //    obj.CreatedOn = DateTime.Now;
        //    db.ErrorLogDatas.Add(obj);
        //    db.SaveChanges();
        //    Server.ClearError();
        //    Response.Redirect("~/Error/ServerError");
        //}

    }
}
