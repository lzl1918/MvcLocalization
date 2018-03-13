using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LocalizationTest.Pages
{
    public class RedirectModel : PageModel
    {
        public IActionResult OnGet(string location = null)
        {
            if (location != null)
                return RedirectToPage("MyPage");
            else
                return Page();
        }
    }
}