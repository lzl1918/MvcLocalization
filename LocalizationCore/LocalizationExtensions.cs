using LocalizationCore.CodeMatching;
using LocalizationCore.Middlewares;
using LocalizationCore.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace LocalizationCore
{
    public static class LocalizationExtensions
    {
        public static IServiceCollection AddMvcLocalization(
            this IServiceCollection services,
            string defaultCulture,
            IEnumerable<string> supportedCultures,
            int culturePageCacheSize = 100,
            int cultureCacheSize = 20)
        {
            services.AddSingleton<ICultureOption>(new CultureOption(defaultCulture, supportedCultures));
            services.AddSingleton<ICultureFileCache>(provider =>
            {
                IHostingEnvironment env = provider.GetRequiredService<IHostingEnvironment>();
                ICultureOption cultureOption = provider.GetRequiredService<ICultureOption>();
                return new CultureFileCache(env, cultureOption, cultureCacheSize, culturePageCacheSize);
            });
            services.AddSingleton<ICultureFileCollectionCache>(provider =>
            {
                IHostingEnvironment env = provider.GetRequiredService<IHostingEnvironment>();
                return new CultureFileCollectionCache(env, cultureCacheSize, culturePageCacheSize);
            });

            services.AddScoped<ICultureContext, CultureContext>();
            services.AddSingleton<CultureMatchingViewResultExecutor>();
            services.AddScoped<ILocalizedViewRenderContextAccessor, LocalizedViewRenderContextAccessor>();
            services.AddScoped<ILocalizedViewRenderContext>(service =>
            {
                ILocalizedViewRenderContextAccessor accessor = service.GetRequiredService<ILocalizedViewRenderContextAccessor>();
                if (accessor.Context == null)
                {
                    ICultureContext cultureContext = service.GetRequiredService<ICultureContext>();
                    accessor.Context = new LocalizedViewRenderContext(cultureContext.CurrentCulture, cultureContext.CurrentCulture, cultureContext.UrlCultureSpecifier);
                }
                return accessor.Context;
            });
            return services;
        }

        public static IApplicationBuilder UseMvcLocalization(this IApplicationBuilder app, bool checkCultureSupported)
        {
            return app.UseMiddleware<LocalizationMiddleware>(checkCultureSupported);
        }
        public static IApplicationBuilder UseMvcLocalization(
            this IApplicationBuilder app,
            bool checkCultureSupported,
            Func<PathString, UrlFilterResult> filter)
        {
            return app.UseMiddleware<LocalizationMiddleware>(checkCultureSupported, filter);
        }

        public static IServiceCollection AddCodeMatching(
            this IServiceCollection services,
            string resourceDirectory = "/Strings",
            bool isCaseSensitive = false,
            int culturePageCacheSize = 100,
            int cultureCacheSize = 20)
        {
            services.AddSingleton<ICodeMatchingOption>(new CodeMatchingOption(resourceDirectory, isCaseSensitive));
            services.AddSingleton<ICodeMatchingCache>(provider =>
            {
                ICodeMatchingOption option = provider.GetRequiredService<ICodeMatchingOption>();
                ICultureOption cultureOption = provider.GetRequiredService<ICultureOption>();
                ICultureFileCache cultureFileCache = provider.GetRequiredService<ICultureFileCache>();
                return new CodeMatchingCache(cultureCacheSize, culturePageCacheSize, option, cultureOption, cultureFileCache);
            });
            services.AddScoped<ICodeMatchingService, CodeMatchingService>();
            return services;
        }
    }
}
