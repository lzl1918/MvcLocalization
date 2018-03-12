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
                return service.GetService<ILocalizedViewRenderContextAccessor>().Context;
            });
            return services;
        }

        public static IApplicationBuilder UseMvcLocalization(this IApplicationBuilder app, bool checkCultureSupported)
        {
            return app.UseMiddleware<LocalizationMiddleware>(checkCultureSupported);
        }

        public static IServiceCollection AddCodeMatching(this IServiceCollection services, string resourceDirectory = "/Strings")
        {
            services.AddSingleton<ICodeMatchingOption>(new CodeMatchingOption(resourceDirectory));
            services.AddScoped<ICodeMatchingService, CodeMatchingService>();
            return services;
        }
    }
}
