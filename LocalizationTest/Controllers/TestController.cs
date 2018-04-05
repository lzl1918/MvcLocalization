using LocalizationCore;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace LocalizationTest.Controllers
{
    public class TestController : CultureMatchingController
    {
        public IActionResult Index()
        {
            return View();
        }

        public string TE()
        {
            return "STRING CONTENT";
        }
    }
}