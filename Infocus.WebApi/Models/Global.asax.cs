using Infocus.WebApi.Common.Bone;
using Infocus.WebApi.Common;
using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]
namespace Infocus.WebApi
{
    public class WebApiApplication : HttpApplication
    {
        private static ILog _logger = LogManager.GetLogger(typeof(WebApiApplication));
        protected void Application_Start()
        {
            _logger.Debug("In Application_Start");
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            //ConfigureLog();
            try
            {
                BusinessOneRuntimeContext.Instance.InitializeRuntime();
               // Infocus.WebApi.Common.Bone.BusinessOneCompany _company = ( Infocus.WebApi.Common.Bone.BusinessOneCompany) BusinessOneRuntimeContext.Instance.GetCompany(); 
              SAPbobsCOM.Company _company = BusinessOneRuntimeContext.Instance.GetCompany(); // 08-04-2022
      
            }
            // 07-22-2021 begin
            catch (HttpException ht)
            {
                int ohtpCode = ht.GetHttpCode();
                string ip = "Unknown";
                try
                {
                    ip = HttpContext.Current.Request.UserHostAddress;
                }
                catch
                {
                    ip = "Unknown";
                }
                _logger.Fatal(ht.Message + " ip:" + ip, ht);
            }
            // 07-22-2021 end
            catch (Exception ex)
            {
                _logger.Fatal(ex);

            }
        }

        protected void Application_End()
        {
            BusinessOneRuntimeContext.Instance.Terminate();
        }
        //private void ConfigureLog()
        //{
        //    String rollingLogFile =
        //        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        //            "D1 Technologies", "WebApi", "log-{Date}.txt");
        //    Log.Logger = new LoggerConfiguration()
        //        .MinimumLevel.Verbose()
        //        .WriteTo.Trace(outputTemplate: "{Timestamp} [{Level}] [{SourceContext}] {Message}{NewLine}{Exception}")
        //        .WriteTo.RollingFile(rollingLogFile, Serilog.Events.LogEventLevel.Debug, "{Timestamp} [{Level}] [{SourceContext}] {Message}{NewLine}{Exception}")
        //        .CreateLogger();
        //}
        protected void Application_Error(Object sender, EventArgs e)
        {
            Exception ex = Server.GetLastError();
            if (ex is ThreadAbortException)
                return;
            _logger.Error(ex);
            // 11-18-2022 begin
            if (!String.IsNullOrWhiteSpace(ex.InnerException.Message) && ex.InnerException.Message.Length > 0)
            {
                _logger.Error(ex.InnerException.Message);
            }
            // 11-18-2022 end
            //Response.Redirect("/Shared/Error");
            Server.ClearError();
        }
    }
}
