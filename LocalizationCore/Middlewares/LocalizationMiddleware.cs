using LocalizationCore.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalizationCore.Middlewares
{
    internal sealed class LocalizationMiddleware
    {
        private readonly RequestDelegate next;
        private readonly bool checkCultureSupported;

        public LocalizationMiddleware(RequestDelegate next, bool checkCultureSupported)
        {
            this.next = next;
            this.checkCultureSupported = checkCultureSupported;
        }

        public Task Invoke(HttpContext context, ICultureContext cultureContext)
        {
            string urlSpecifier;
            ICultureExpression cultureExpression = ExtractLanguageFromUrl(context, cultureContext, out urlSpecifier, out string action);
            cultureContext.UrlCultureSpecifier = urlSpecifier;
            if (cultureExpression == null)
                cultureExpression = ExtractLanguageFromHeader(context, cultureContext);
            if (cultureExpression == null)
                cultureExpression = cultureContext.CultureOption.DefaultCulture;
            cultureContext.Action = action;
            cultureContext.CurrentCulture = cultureExpression;
            if (urlSpecifier.Length <= 0)
            {
                return next(context);
            }
            else
            {
                return next(context).ContinueWith(tsk =>
                {
                    if ((context.Response.StatusCode == 301 || context.Response.StatusCode == 302)
                    && context.Response.Headers.TryGetValue("Location", out StringValues locationValue)
                    && locationValue.Count > 0)
                    {
                        string location = locationValue[0];
                        if (UrlComponents.TryParse(location, out UrlComponents urlComponents))
                        {
                            if (urlComponents.IsRelative)
                            {
                                if (!string.IsNullOrEmpty(urlComponents.CultureSpecifier))
                                {
                                    // dose not change the location
                                    // context.Response.Headers["Location"] = urlSpecifier + context.Response.Headers["Location"];
                                    return;
                                }
                                else
                                {
                                    urlComponents.TrySetCulture(urlSpecifier);
                                    context.Response.Headers["Location"] = urlComponents.FullActionWithQuery;
                                    return;
                                }
                            }
                            else
                            {
                                string host = context.Request.Host.Value;
                                string requestedHost = urlComponents.FullHost;
                                if (requestedHost != null && requestedHost.Equals(host, StringComparison.CurrentCultureIgnoreCase))
                                {
                                    if (!string.IsNullOrEmpty(urlComponents.CultureSpecifier))
                                    {
                                        // dose not change the location
                                        // context.Response.Headers["Location"] = urlSpecifier + context.Response.Headers["Location"];
                                        return;
                                    }
                                    else
                                    {
                                        urlComponents.TrySetCulture(urlSpecifier);
                                        context.Response.Headers["Location"] = urlComponents.FullActionWithQuery;
                                        return;
                                    }
                                }
                                else
                                {
                                    // dose not change the location
                                    // context.Response.Headers["Location"] = urlSpecifier + context.Response.Headers["Location"];
                                    return;
                                }
                            }

                        }
                        else
                        {
                            // dose not change the location
                            // context.Response.Headers["Location"] = urlSpecifier + context.Response.Headers["Location"];
                        }
                    }
                });
            }

        }

        private ICultureExpression ExtractLanguageFromHeader(HttpContext context, ICultureContext cultureContext)
        {
            ICultureOption cultureOption = cultureContext.CultureOption;
            StringValues languageValues;
            if (!context.Request.Headers.TryGetValue("Accept-Language", out languageValues))
                return null;
            List<string> languageCodes = new List<string>();
            foreach (string lang in languageValues)
                languageCodes.AddRange(lang.Split(';').Select(x => x.ToLower()));
            ICultureExpression expression;
            foreach (string lang in languageCodes)
            {
                if (lang.TryParseCultureExpression(out expression))
                {
                    if (checkCultureSupported && !cultureContext.CultureOption.IsCultureSupported(expression))
                    {
                        return null;
                    }
                    return expression;
                }

            }
            return null;
        }
        private ICultureExpression ExtractLanguageFromUrl(HttpContext context, ICultureContext cultureContext, out string urlSpecifier, out string action)
        {
            ICultureOption cultureOption = cultureContext.CultureOption;
            string path = context.Request.Path.Value;
            if (UrlComponents.TryParse(path, out UrlComponents urlComponents))
            {
                action = urlComponents.Action;
                context.Request.Path = new PathString(action);
                urlSpecifier = urlComponents.CultureSpecifier ?? "";
                if (urlSpecifier.Length > 0)
                    context.Request.PathBase = context.Request.PathBase.Add(new PathString(urlSpecifier));

                if (urlComponents.Culture != null && checkCultureSupported && !cultureContext.CultureOption.IsCultureSupported(urlComponents.Culture))
                    return null;
                return urlComponents.Culture;
            }
            else
            {
                urlSpecifier = "";
                action = path;
                return null;
            }
        }

    }
}
