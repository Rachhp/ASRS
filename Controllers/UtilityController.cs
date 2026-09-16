using S1947.Models;
using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Web.Mvc;
using System.Web.SessionState;

namespace S1947.Controllers
{
    [SessionState(SessionStateBehavior.Disabled)]
    public class UtilityController : Controller
    {
        SerialPort mySerialPort;
        // GET: Utility
        public ActionResult Index()
        {
            return View();
        }
        // GET: UserMaster

        //PlcController plc = new PlcController();
        string plc_ready = "";
       

        static public void globalError(Exception exc)
        {
            S1947Entities conn = new S1947Entities();
            string stc = exc.StackTrace;
            string msg = exc.Message;

            if (stc.Length >= 500)
            {
                stc = stc.Substring(0, 480);
            }

            if (msg.Length >= 500)
            {
                msg = msg.Substring(0, 495);
            }
            //productError ER1 = new productError();
            //ER1.error = stc.Trim();
            //ER1.message = msg;
            //ER1.ModifiedOn = DateTime.Now;
            //conn.productErrors.Add(ER1);
            //conn.SaveChanges();Y:\Vertical carousal
        }
        public string BackUp()
        {
            DateTime d = DateTime.Now;
            string dd = d.ToString("dd-MM");   // safer formatting
            string dbNm = "S1947";             // must match the actual DB name you want to back up

            string connectionString = "data source=192.168.20.127,1433;initial catalog=S1947;persist security info=True;user id=cautomate;password=caspl123;trustservercertificate=True;";
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                string path = @"D:\DataBaseBackUp\";  // file Name should be change while Integration Testing
                string backupFile = path + dbNm + "_" + dd + ".bak";

                string backupSql = $@"BACKUP DATABASE [{dbNm}]TO DISK = '{backupFile}'WITH INIT, MEDIANAME = 'Z_SQLServerBackups', NAME = 'Full Backup of {dbNm}'";

                using (SqlCommand cmd = new SqlCommand(backupSql, con))
                {
                    cmd.ExecuteNonQuery();
                }

                try
                {
                    string[] files = Directory.GetFiles(path, "*.Bak");
                    foreach (string file in files)
                    {
                        FileInfo fi = new FileInfo(file);
                        if (fi.CreationTime < DateTime.Now.AddDays(-15))
                        {
                            fi.Delete();
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Optional: log or handle exceptions
                    Console.WriteLine("Error deleting old backups: " + ex.Message);
                }
            }
            return "success";
        }
        public string fullScreen()
        {
            //Process p = new Process();
            // p.StartInfo.FileName = "cmd.exe";
            // //p.StartInfo.Arguments = @"/c E:\\pdf2xml";
            // p.StartInfo.UseShellExecute = false;
            // p.StartInfo.RedirectStandardOutput = true;
            // p.StartInfo.RedirectStandardInput = true;
            // p.Start();

            // p.StandardInput.WriteLine("D:");
            // string cm = "E:\\script.bat";
            // p.StandardInput.WriteLine(cm);
            return "abcd";
        }
        public string rfId()
        {
            mySerialPort = new SerialPort("COM3");

            mySerialPort.BaudRate = 9600;
            mySerialPort.Parity = Parity.None;
            mySerialPort.StopBits = StopBits.One;
            mySerialPort.DataBits = 8;
            mySerialPort.Handshake = Handshake.None;
            mySerialPort.RtsEnable = true;

            mySerialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
            try
            {
                mySerialPort.Open();
            }
            catch (Exception e)
            {
                UtilityController.globalError(e);
                System.Diagnostics.Debug.WriteLine(e.StackTrace);
                return "Please check RFID module and try again";
            }

            while (true)
            {
                if (indata.Length == 12)
                {
                    mySerialPort.Close();

                    return indata;
                }

            }
        }
        private void DataReceivedHandler(
                        object sender,
                        SerialDataReceivedEventArgs e)
        {
            mySerialPort = (SerialPort)sender;

            indata = indata + mySerialPort.ReadExisting();
            if (data.Length == 12)
            {
                mySerialPort.Close();
            }


        }
        string data = "";
      public string getWeight()
        {
            mySerialPort = new SerialPort("COM5");

            mySerialPort.BaudRate = 9600;
            mySerialPort.Parity = Parity.None;
            mySerialPort.StopBits = StopBits.One;
            mySerialPort.DataBits = 8;
            mySerialPort.Handshake = Handshake.None;
            mySerialPort.RtsEnable = true;

            mySerialPort.DataReceived += new SerialDataReceivedEventHandler(WeightReceivedHandler);

            mySerialPort.Open();
            while (true)
            {
                if (indata.Length == 12)
                {
                    S1947Entities conn = new S1947Entities();
                }
            }
        }
        public void startvideo(string load, int v)
        {
            var a = DateTime.Now;
            string[] files = Directory.GetDirectories("E:\\video_recordings\\");

            foreach (string file in files)
            {
                string s = DateTime.Now.AddMonths(0) + "";
                DirectoryInfo fi = new DirectoryInfo(file);
                if (fi.LastAccessTime < DateTime.Now.AddMonths(-1))
                {
                    DeleteDirectory(fi.FullName, true);
                }

            }
            var path = "E:\\video_recordings\\";
            var folder = Path.Combine(path, a.Month + "");

            bool IsExists = System.IO.Directory.Exists(folder);
            if (!IsExists)
                Directory.CreateDirectory(folder);
            var folder2 = Path.Combine(folder, a.Day + "-" + a.Month + "-" + a.Year);

            bool IsExists2 = System.IO.Directory.Exists(folder2);
            if (!IsExists2)
                Directory.CreateDirectory(folder2);


            Process p = new Process();
            p.StartInfo.FileName = "cmd.exe";
            //p.StartInfo.Arguments = @"/c E:\\pdf2xml";
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardInput = true;
            p.Start();
            S1947Entities conn = new S1947Entities();
            var userExists = conn.UserMasters.Where(x => x.MTransNo == v).FirstOrDefault();
            p.StandardInput.WriteLine("D:");


            string cm = "E:\\ffmpeg\\bin\\ffmpeg -f dshow -i video=\"USB Video Device\" " + folder2 + "\\" + load + "-" + userExists.UserId + "-" + a.Day + "-" + a.Month + "-" + a.Year + "-" + a.Hour + "-" + a.Minute + "-" + a.Second + ".AVI";
            p.StandardInput.WriteLine(cm);

        }
        public static void DeleteDirectory(string directoryName, bool checkDirectiryExist)
        {
            if (Directory.Exists(directoryName))
                Directory.Delete(directoryName, true);
        }
        public void stopvideo()
        {
            Process cmd = new Process();
            cmd.StartInfo.FileName = "cmd.exe";
            cmd.StartInfo.UseShellExecute = false;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.Start();
            cmd.StandardInput.WriteLine("taskkill /f /t /im ffmpeg.exe");


        }

        string indata = "";
        private void WeightReceivedHandler(
                        object sender,
                        SerialDataReceivedEventArgs e)
        {
            mySerialPort = (SerialPort)sender;

            indata = indata + mySerialPort.ReadExisting();
            if (indata.Length == 12)
            {

                mySerialPort.Close();
            }
        }

            // Common function
            public string GetCurrentShiftName()
            {
                using (S1947Entities conn = new S1947Entities())
                {
                    var now = DateTime.Now.TimeOfDay;
                    var shifts = conn.shift_master
                                      .Where(x => x.is_active)
                                      .ToList();

                    foreach (var item in shifts)
                    {
                        var start = item.start_time;
                        var end = item.end_time;

                        if (start < end)
                        {
                            if (now >= start && now < end)
                            {
                                return item.shift_name;
                            }
                        }
                        else
                        {
                            // Overnight shift
                            if (now >= start || now < end)
                            {
                                return item.shift_name;
                            }
                        }
                    }

                    return "";
                }
            }

            // For AJAX / Dashboard
            public JsonResult GetCurrentShift()
            {
                string shift = GetCurrentShiftName();
                return Json(shift, JsonRequestBehavior.AllowGet);
            }
        }
}