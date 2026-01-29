using System;
using System.Diagnostics;

namespace Infocus.Essentials
{
    public sealed class PdfFilePrinter
    {
        public void PrintFile(string fileName)
        {
            ProcessStartInfo info = new ProcessStartInfo();
            info.Verb = @"print";
            info.FileName = fileName;
            info.CreateNoWindow = true;
            info.WindowStyle = ProcessWindowStyle.Hidden;

            Process p = new Process();
            p.StartInfo = info;
            p.Start();

            p.WaitForInputIdle();
            System.Threading.Thread.Sleep(3000);
            if(false == p.CloseMainWindow())
                p.Kill();
        }
    }
}
