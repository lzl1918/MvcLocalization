using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Internal;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Abstractions;
using System.IO;
using LocalizationCore.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using LocalizationCore.Services;

namespace LocalizationCore
{
    internal sealed class CultureMatchingViewResultExecutor : ViewResultExecutor
    {
        private const string ActionNameKey = "action";
        private const string ControllerNameKey = "controller";
        private readonly IHostingEnvironment env;

        public CultureMatchingViewResultExecutor(
            IHostingEnvironment env,
            IOptions<MvcViewOptions> viewOptions,
            IHttpResponseStreamWriterFactory writerFactory,
            ICompositeViewEngine viewEngine,
            ITempDataDictionaryFactory tempDataFactory,
            DiagnosticSource diagnosticSource,
            ILoggerFactory loggerFactory,
            IModelMetadataProvider modelMetadataProvider) : base(viewOptions, writerFactory, viewEngine, tempDataFactory, diagnosticSource, loggerFactory, modelMetadataProvider)
        {
            this.env = env;
        }

        internal async Task ExecuteAsync(ActionContext context, ViewResult result)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (result == null)
                throw new ArgumentNullException(nameof(result));

            ViewEngineResult viewEngineResult = FindView(context, result);
            viewEngineResult.EnsureSuccessful(originalLocations: null);

            IView view = viewEngineResult.View;
            using (view as IDisposable)
            {

                await ExecuteAsync(
                    context,
                    view,
                    result.ViewData,
                    result.TempData,
                    result.ContentType,
                    result.StatusCode);
            }
        }

        public override ViewEngineResult FindView(ActionContext actionContext, ViewResult viewResult)
        {
            string viewName = viewResult.ViewName;
            if (viewName == null)
                viewName = GetActionName(actionContext);
            if (viewName == null)
                return base.FindView(actionContext, viewResult);

            string controllerName;
            if (!actionContext.ActionDescriptor.RouteValues.TryGetValue(ControllerNameKey, out controllerName) ||
                string.IsNullOrEmpty(controllerName))
                controllerName = "";
            string directory = Path.Combine("Views", controllerName);

            HttpContext httpContext = actionContext.HttpContext;
            IServiceProvider provider = httpContext.RequestServices;
            ICultureOption cultureOption = provider.GetRequiredService<ICultureOption>();
            ICultureContext cultureContext = provider.GetRequiredService<ICultureContext>();
            CultureMatchingViewResult viewFindResult = (CultureMatchingViewResult)viewResult;
            ICultureFileCache fileCache = provider.GetRequiredService<ICultureFileCache>();
            IFileCultureInfo fileCultureInfo = fileCache.Get(viewFindResult.RequestedCulture, directory, viewName, "cshtml");
            if (fileCultureInfo != null)
            {
                httpContext.RequestServices.GetService<ILocalizedViewRenderContextAccessor>().Context = new LocalizedViewRenderContext(viewFindResult.RequestedCulture, fileCultureInfo.Culture, cultureContext.UrlCultureSpecifier);
                string relativePath = fileCultureInfo.RelativePath.Substring(0, fileCultureInfo.RelativePath.Length - 7); // 7 == ".cshtml".Length
                relativePath = relativePath.Substring(directory.Length + 1);
                viewResult.ViewName = relativePath;
                return base.FindView(actionContext, viewResult);
            }
            else
            {
                httpContext.RequestServices.GetService<ILocalizedViewRenderContextAccessor>().Context = new LocalizedViewRenderContext(viewFindResult.RequestedCulture, null, cultureContext.UrlCultureSpecifier);
                return base.FindView(actionContext, viewResult);
            }
        }

        // copied from asp.net source code
        private static string GetActionName(ActionContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (!context.RouteData.Values.TryGetValue(ActionNameKey, out var routeValue))
                return null;

            var actionDescriptor = context.ActionDescriptor;
            string normalizedValue = null;
            if (actionDescriptor.RouteValues.TryGetValue(ActionNameKey, out var value) &&
                !string.IsNullOrEmpty(value))
            {
                normalizedValue = value;
            }

            var stringRouteValue = routeValue?.ToString();
            if (string.Equals(normalizedValue, stringRouteValue, StringComparison.OrdinalIgnoreCase))
            {
                return normalizedValue;
            }

            return stringRouteValue;
        }
    }

    internal sealed class CultureMatchingViewResult : ViewResult
    {
        public ICultureExpression RequestedCulture { get; }

        public CultureMatchingViewResult(ICultureExpression requestedCulture)
        {
            RequestedCulture = requestedCulture;
        }

        public async override Task ExecuteResultAsync(ActionContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            IServiceProvider services = context.HttpContext.RequestServices;
            CultureMatchingViewResultExecutor executor = services.GetRequiredService<CultureMatchingViewResultExecutor>();
            await executor.ExecuteAsync(context, this);
        }
    }

    public abstract class CultureMatchingController : Controller
    {
        public override ViewResult View(string viewName, object model)
        {
            ViewData.Model = model;
            ICultureContext cultureContext = HttpContext.RequestServices.GetService<ICultureContext>();
            return new CultureMatchingViewResult(cultureContext.CurrentCulture)
            {
                ViewData = ViewData,
                TempData = TempData,
                ViewName = viewName
            };
        }
    }
}
