using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CaptchaIon;

namespace MvcApplication1.Controllers
{
    public class HomeController : Controller
    {
        [HttpGet]
        public ActionResult Index()
        {
            ViewBag.Message = "Добро пожаловать в ASP.NET MVC!";

            return View();
        }
        [HttpPost]
        public ActionResult Index(Captcha cap)
        {
            var valid = cap.IsValid;
            return View();
        }

        
    }
}
