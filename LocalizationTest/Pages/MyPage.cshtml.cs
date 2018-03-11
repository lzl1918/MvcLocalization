using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LocalizationCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LocalizationTest.Pages
{
    public class MyPageModel : CultureMatchingPageModel
    {
        public string Title { get; } = "1234";
        public IActionResult OnGet()
        {
            return Page();
        }
    }
}