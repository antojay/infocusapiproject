using System;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Web;
using System.Web.Hosting;

namespace Infocus.Common
{
    public static class SystemUtility
    {
        public static String GetMacAddress()
        {
            String macAddress = String.Empty;

            IPGlobalProperties computerProperties = IPGlobalProperties.GetIPGlobalProperties();
            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
            if(nics != null && nics.Length > 0)
            {
                long greatestSpeed = 0;
                foreach(NetworkInterface adapter in nics)
                {
                    //IPInterfaceProperties properties = adapter.GetIPProperties(); //  .GetIPInterfaceProperties();
                    if((adapter.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                        adapter.NetworkInterfaceType == NetworkInterfaceType.FastEthernetFx ||
                        adapter.NetworkInterfaceType == NetworkInterfaceType.FastEthernetT) && adapter.Speed > greatestSpeed && adapter.OperationalStatus == OperationalStatus.Up)
                    {
                        macAddress = String.Empty;
                        PhysicalAddress address = adapter.GetPhysicalAddress();
                        byte[] bytes = address.GetAddressBytes();
                        for(int i = 0; i < bytes.Length; i++)
                        {
                            // Display the physical address in hexadecimal.
                            macAddress += bytes[i].ToString("X2");
                            // Insert a hyphen after each byte, unless we are at the end of the 
                            // address.
                            if(i != bytes.Length - 1)
                            {
                                macAddress += "-";
                            }
                        }
                    }
                }
            }

            return macAddress;
        }

        public static String GetComputerName()
        {
            return Environment.MachineName;
        }

        public static Version GetOsVersion()
        {
            return Environment.OSVersion.Version;
        }

        public static String GetOsName()
        {
            return Environment.OSVersion.Platform.ToString("g");
        }

        public static Boolean GetIs64Bit()
        {
            return Environment.Is64BitOperatingSystem;
        }

        public static String GetWorkingDirectory()
        {
            ExecutionEnvironment environment = GetExecutionEnvironment();
            if(environment == ExecutionEnvironment.WebApplicationEnvironment)
            {
                return HttpContext.Current.Server.MapPath("~/");
            }
            else if(environment == ExecutionEnvironment.WebServiceEnvironment)
            {
                return HostingEnvironment.ApplicationPhysicalPath;
            }
            return Environment.CurrentDirectory;
        }

        public static ExecutionEnvironment GetExecutionEnvironment()
        {
            if(HttpContext.Current != null)
            {
                return ExecutionEnvironment.WebApplicationEnvironment;
            }
            else if(HostingEnvironment.IsHosted)
            {
                return ExecutionEnvironment.WebServiceEnvironment;
            }
            return ExecutionEnvironment.WindowsEnvironment;
        }

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool GlobalMemoryStatusEx([In, Out] MemoryStatusEx stat);

        public static void ReduceMemoryConsumption()
        {
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            EmptyWorkingSet(Process.GetCurrentProcess().Handle);
        }

        [DllImport("psapi.dll")]
        private static extern Int32 EmptyWorkingSet(IntPtr hwProc);
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public class MemoryStatusEx
    {
        public uint Length;
        public uint MemoryLoad;
        public ulong TotalPhysical;
        public ulong AvailablePhysical;
        public ulong TotalPageFile;
        public ulong AvailablePageFile;
        public ulong TotalVirtual;
        public ulong AvailableVirtual;
        public ulong AvailableExtendedVirtual;

        public MemoryStatusEx()
        {
            Length = (uint)Marshal.SizeOf(typeof(MemoryStatusEx));
        }
    }

    public enum ExecutionEnvironment
    {
        WindowsEnvironment,
        WebApplicationEnvironment,
        WebServiceEnvironment

    }
}
