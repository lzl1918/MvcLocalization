using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Text;

namespace LocalizationCore.Helpers
{
    internal static class ContextHelper
    {
        public static PageContext CreateCopy(this PageContext context)
        {
            CompiledPageActionDescriptor actionDescriptor = (CompiledPageActionDescriptor)context.ActionDescriptor;
            CompiledPageActionDescriptor newActionDescriptor = new CompiledPageActionDescriptor(actionDescriptor);
            PageContext result = new PageContext()
            {
                ActionDescriptor = newActionDescriptor,
                HttpContext = context.HttpContext,
                RouteData = context.RouteData,
                ValueProviderFactories = context.ValueProviderFactories,
                ViewData = context.ViewData,
                ViewStartFactories = context.ViewStartFactories
            };
            return result;
        }
        public static ViewContext CreateCopy(this ViewContext context)
        {
            ViewContext result = new ViewContext()
            {
                ActionDescriptor = context.ActionDescriptor,
                ClientValidationEnabled = context.ClientValidationEnabled,
                ExecutingFilePath = context.ExecutingFilePath,
                FormContext = context.FormContext,
                Html5DateRenderingMode = context.Html5DateRenderingMode,
                HttpContext = context.HttpContext,
                RouteData = context.RouteData,
                TempData = context.TempData,
                ValidationMessageElement = context.ValidationMessageElement,
                ValidationSummaryMessageElement = context.ValidationSummaryMessageElement,
                View = context.View,
                ViewData = context.ViewData,
                Writer = context.Writer
            };
            return result;
        }
    }
}
