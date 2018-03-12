using LocalizationCore.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace LocalizationCore
{
    internal sealed class CultureMatchingPageResult : PageResult
    {
        private readonly IHostingEnvironment _env;
        private readonly ICultureExpression _requestedCulture;

        public CultureMatchingPageResult(IHostingEnvironment env, ICultureExpression requestedCulture)
        {
            _env = env;
            _requestedCulture = requestedCulture;
        }
        public override async Task ExecuteResultAsync(ActionContext context)
        {
            if (!(context is PageContext pageContext))
            {
                throw new ArgumentException($"{nameof(context)} should be instance of type {typeof(PageContext).Name}");
            }


            string executingFilePath = _env.ContentRootPath;
            string basePath = this.Page.Path;
            string fileName = Path.GetFileName(basePath);
            string pageName = Path.GetFileNameWithoutExtension(basePath);
            basePath = basePath.Substring(0, basePath.Length - fileName.Length);
            while (basePath[basePath.Length - 1] == '/')
                basePath = basePath.Substring(0, basePath.Length - 1);

            HttpContext httpContext = pageContext.HttpContext;
            IRazorViewEngine engine = httpContext.RequestServices.GetRequiredService<IRazorViewEngine>();
            ICultureContext cultureContext = httpContext.RequestServices.GetService<ICultureContext>();
            if (ResourceRequestHelper.TryFindFile(basePath, pageName, "cshtml", _requestedCulture, httpContext, out string matchedName, out ICultureExpression matchedCulture))
            {
                httpContext.RequestServices.GetService<ILocalizedViewRenderContextAccessor>().Context = new LocalizedViewRenderContext(_requestedCulture, matchedCulture, cultureContext.UrlCultureSpecifier);
                string matchedPageName = Path.GetFileNameWithoutExtension(matchedName);
                string relativePath = $"{basePath}/{matchedName}";
                if (!relativePath.Equals(this.Page.Path))
                {
                    RazorPageResult pageResult = engine.GetPage(executingFilePath, relativePath);
                    Page page = (Page)pageResult.Page;
                    PageContext resultPageContext = pageContext.CreateCopy();
                    ViewContext resultViewContext = this.Page.ViewContext.CreateCopy();
                    page.ViewContext = resultViewContext;
                    resultViewContext.ExecutingFilePath = relativePath;
                    resultPageContext.ActionDescriptor.PageTypeInfo = page.GetType().GetTypeInfo();
                    resultPageContext.ActionDescriptor.RelativePath = relativePath;
                    page.PageContext = resultPageContext;
                    page.HtmlEncoder = this.Page.HtmlEncoder;
                    page.Path = relativePath;
                    this.Page = page;
                }
            }
            else
            {
                httpContext.RequestServices.GetService<ILocalizedViewRenderContextAccessor>().Context = new LocalizedViewRenderContext(_requestedCulture, null, cultureContext.UrlCultureSpecifier);
            }

            var executor = context.HttpContext.RequestServices.GetRequiredService<PageResultExecutor>();
            await executor.ExecuteAsync(pageContext, this);
        }
    }
    public abstract class CultureMatchingPageModel : PageModel
    {

        public override PageResult Page()
        {
            var env = HttpContext.RequestServices.GetService<IHostingEnvironment>();
            var cultureContext = HttpContext.RequestServices.GetService<ICultureContext>();
            return new CultureMatchingPageResult(env, cultureContext.CurrentCulture);
        }
    }
}
