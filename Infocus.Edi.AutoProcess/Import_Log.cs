
namespace Infocus.Edi.AutoProcess
{
    using System;

    public class Import_Log
    {
        public static string sMsg;
        public static string sLogFile;

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
            Import_Log.oStreamWriter.WriteLine(sEntry);
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

        public Import_Log()
        {
            if (System.IO.Directory.Exists(InfocusEdiAutoProcess.sLogPath) == false)
            {
                System.IO.Directory.CreateDirectory(InfocusEdiAutoProcess.sLogPath);
            }
            DateTime dDtTime = DateTime.Now;
            string sCurDt = dDtTime.Year.ToString();
            sCurDt += dDtTime.Month.ToString().PadLeft(2, '0');
            sCurDt += dDtTime.Day.ToString().PadLeft(2, '0');
            sCurDt += dDtTime.Hour.ToString().PadLeft(2, '0');
            sCurDt += dDtTime.Minute.ToString().PadLeft(2, '0');
            sCurDt += dDtTime.Second.ToString().PadLeft(2, '0');
            sLogFile = InfocusEdiAutoProcess.sLogPath + "\\850_AutoImport_" + sCurDt + ".log";
            oFileStream = System.IO.File.Open(sLogFile, System.IO.FileMode.Create, System.IO.FileAccess.ReadWrite);
            oStreamWriter = new System.IO.StreamWriter(oFileStream);
            oStreamWriter.NewLine = "\r\n";
            oStreamWriter.AutoFlush = true;
        }
    }
}
