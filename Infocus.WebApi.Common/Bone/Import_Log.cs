
namespace Infocus.WebApi.Common.Bone
{
    using System;

    public class Import_Log
    {
        public static string sMsg;
        public static string sLogFile;
        public static string _850Error; // 06-29-2019
        public static string _940Error; // 07-24-2023

        public static System.IO.FileStream oFileStream = null;
        public static System.IO.StreamWriter oStreamWriter = null;

        public static string FmtLogTime()
        {
            DateTime dDtTime = DateTime.Now;
            string sCurDt = dDtTime.Hour.ToString().PadLeft(2, '0');
            sCurDt += ":" + dDtTime.Minute.ToString().PadLeft(2, '0');
            sCurDt += ":" + dDtTime.Second.ToString().PadLeft(2, '0');
            sCurDt += " on " + dDtTime.Month.ToString().PadLeft(2, '0');
            sCurDt += "/" + dDtTime.Day.ToString().PadLeft(2, '0');
            sCurDt += "/" + dDtTime.Year.ToString();
            return sCurDt;
        }

        public static void LogEntry(string sInput)
        {
            //string sEntry = FmtLogTime() + " ";
            string sEntry = null;

            sEntry = FmtLogTime() + " " + sInput;
            try
            {
                Import_Log.oStreamWriter.WriteLine(sEntry);
            }
            catch (Exception l)
            {
                string oErrMesg = l.Message;
            }
        }

        public static void CloseLog()
        {
            oFileStream.Flush();
            oStreamWriter.Flush();
            oStreamWriter.Close();
            oStreamWriter.Dispose();
            oFileStream.Close();
            oFileStream.Dispose();

        }

        public Import_Log(string sLogPath, string fileName)
        {
            if (System.IO.Directory.Exists(sLogPath) == false)
            {
                System.IO.Directory.CreateDirectory(sLogPath);
            }
            DateTime dDtTime = DateTime.Now;
            string sCurDt = dDtTime.Year.ToString();
            sCurDt += dDtTime.Month.ToString().PadLeft(2, '0');
            sCurDt += dDtTime.Day.ToString().PadLeft(2, '0');
            sCurDt += dDtTime.Hour.ToString().PadLeft(2, '0');
            sCurDt += dDtTime.Minute.ToString().PadLeft(2, '0');
            sCurDt += dDtTime.Second.ToString().PadLeft(2, '0');
            //sLogFile = sLogPath + "\\850_AutoImport_" + sCurDt + ".log";
            sLogFile = sLogPath + "\\" + fileName.Trim() + "_" + sCurDt + ".log"; // 08-04-2023
            oFileStream = System.IO.File.Open(sLogFile, System.IO.FileMode.Create, System.IO.FileAccess.ReadWrite);
            oStreamWriter = new System.IO.StreamWriter(oFileStream);
            oStreamWriter.NewLine = "\r\n";
            oStreamWriter.AutoFlush = true;
        }
    }
}
