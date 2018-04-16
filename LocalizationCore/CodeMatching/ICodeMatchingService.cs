using Hake.Extension.ValueRecord;
using LocalizationCore.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace LocalizationCore.CodeMatching
{
    public interface ICodeMatchingService
    {
        HttpContext HttpContext { get; }
        void Match<T>(ICultureExpression requestedCulture, ICultureExpression defaultCulture, IEnumerable<T> codedItems) where T : ICodedItem;
        void Match<T>(ICultureExpression requestedCulture, ICultureExpression defaultCulture, T codedItem) where T : ICodedItem;
        string Match(ICultureExpression requestedCulture, ICultureExpression defaultCulture, string code, string defaultName);
    }

    internal sealed class CodeMatchingService : ICodeMatchingService
    {
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly ICodeMatchingCache matchingCache;
        private readonly ICodeMatchingOption matchingOption;
        private readonly IHostingEnvironment env;

        public HttpContext HttpContext => httpContextAccessor.HttpContext;

        public CodeMatchingService(
            IHttpContextAccessor httpContextAccessor,
            ICodeMatchingCache matchingCache,
            ICodeMatchingOption matchingOption,
            IHostingEnvironment env)
        {
            this.httpContextAccessor = httpContextAccessor;
            this.matchingCache = matchingCache;
            this.matchingOption = matchingOption;
            this.env = env;
        }

        public void Match<T>(ICultureExpression requestedCulture, ICultureExpression defaultCulture, IEnumerable<T> codedItems) where T : ICodedItem
        {
            foreach (T codedItem in codedItems)
            {
                codedItem.DisplayName = Match(requestedCulture, defaultCulture, codedItem.Code, codedItem.DefaultName);
            }
        }

        public void Match<T>(ICultureExpression requestedCulture, ICultureExpression defaultCulture, T codedItem) where T : ICodedItem
        {
            codedItem.DisplayName = Match(requestedCulture, defaultCulture, codedItem.Code, codedItem.DefaultName);
        }

        public string Match(ICultureExpression requestedCulture, ICultureExpression defaultCulture, string code, string defaultName)
        {
            return matchingCache.Get(HttpContext, code, defaultName, requestedCulture);
        }
    }

    public static class CodeMatchingServiceExtensions
    {
        public static void Match<T>(this ICodeMatchingService service, T codedItem) where T : ICodedItem
        {
            HttpContext httpContext = service.HttpContext;
            IServiceProvider services = httpContext.RequestServices;
            ICultureOption cultureOption = services.GetRequiredService<ICultureOption>();
            ICultureContext cultureContext = services.GetRequiredService<ICultureContext>();
            service.Match(cultureContext.CurrentCulture, cultureOption.DefaultCulture, codedItem);
        }
        public static void Match<T>(this ICodeMatchingService service, IEnumerable<T> codedItems) where T : ICodedItem
        {
            HttpContext httpContext = service.HttpContext;
            IServiceProvider services = httpContext.RequestServices;
            ICultureOption cultureOption = services.GetRequiredService<ICultureOption>();
            ICultureContext cultureContext = services.GetRequiredService<ICultureContext>();
            service.Match(cultureContext.CurrentCulture, cultureOption.DefaultCulture, codedItems);
        }
        public static string Match(this ICodeMatchingService service, string code, string defaultName)
        {
            HttpContext httpContext = service.HttpContext;
            IServiceProvider services = httpContext.RequestServices;
            ICultureOption cultureOption = services.GetRequiredService<ICultureOption>();
            ICultureContext cultureContext = services.GetRequiredService<ICultureContext>();
            return service.Match(cultureContext.CurrentCulture, cultureOption.DefaultCulture, code, defaultName);
        }

        public static void Match<T>(this ICodeMatchingService service, ICultureExpression requestedCulture, T codedItem) where T : ICodedItem
        {
            HttpContext httpContext = service.HttpContext;
            IServiceProvider services = httpContext.RequestServices;
            ICultureOption cultureOption = services.GetRequiredService<ICultureOption>();
            service.Match(requestedCulture, cultureOption.DefaultCulture, codedItem);
        }
        public static void Match<T>(this ICodeMatchingService service, ICultureExpression requestedCulture, IEnumerable<T> codedItems) where T : ICodedItem
        {
            HttpContext httpContext = service.HttpContext;
            IServiceProvider services = httpContext.RequestServices;
            ICultureOption cultureOption = services.GetRequiredService<ICultureOption>();
            service.Match(requestedCulture, cultureOption.DefaultCulture, codedItems);
        }
        public static string Match(this ICodeMatchingService service, ICultureExpression requestedCulture, string code, string defaultName)
        {
            HttpContext httpContext = service.HttpContext;
            IServiceProvider services = httpContext.RequestServices;
            ICultureOption cultureOption = services.GetRequiredService<ICultureOption>();
            return service.Match(requestedCulture, cultureOption.DefaultCulture, code, defaultName);
        }
    }
}
