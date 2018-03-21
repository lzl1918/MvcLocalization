using LocalizationCore.CodeMatching;
using LocalizationCore.Middlewares;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace LocalizationCore
{
    public static class LocalizationExtensions
    {
        public static IServiceCollection AddMvcLocalization(this IServiceCollection services, string defaultCulture, IEnumerable<string> supportedCultures)
        {
            services.AddSingleton<ICultureOption>(new CultureOption(defaultCulture, supportedCultures));
            services.AddScoped<ICultureContext, CultureContext>();
            services.AddSingleton<CultureMatchingViewResultExecutor>();
            services.AddScoped<ILocalizedViewRenderContextAccessor, LocalizedViewRenderContextAccessor>();
            services.AddScoped<ILocalizedViewRenderContext>(service =>
            {
                ILocalizedViewRenderContextAccessor accessor = service.GetService<ILocalizedViewRenderContextAccessor>();
                if (accessor.Context == null)
                {
                    ICultureContext cultureContext = service.GetService<ICultureContext>();
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

        public static IServiceCollection AddCodeMatching(this IServiceCollection services, string resourceDirectory = "/Strings", bool isCaseSensitive = false)
        {
            services.AddSingleton<ICodeMatchingOption>(new CodeMatchingOption(resourceDirectory, isCaseSensitive));
            services.AddScoped<ICodeMatchingService, CodeMatchingService>();
            return services;
        }
    }
}
