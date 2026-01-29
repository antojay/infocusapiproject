using System;
using System.Web.Mvc;

namespace Infocus.WebApi.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.Title = "Home Page";

            return View();
        }

        public ActionResult Process()
        {
            ViewBag.Title = "Process 856 Records";
            return View();
        }
    }
}
