﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Routing;
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
                    if (context.Response.StatusCode == 301 || context.Response.StatusCode == 302)
                    {
                        if (context.Response.Headers.ContainsKey("Location"))
                        {
                            string location = context.Response.Headers["Location"][0];
                            if (location.StartsWith('/'))
                                context.Response.Headers["Location"] = urlSpecifier + location;
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
            string path = context.Request.PathBase.Value;
            if (path.Length > 0)
            {
                int slashIndex = 1;
                int pathLength = path.Length;
                for (; slashIndex < pathLength; slashIndex++)
                    if (path[slashIndex] == '/') break;
                string lang = path.Substring(1, slashIndex - 1).ToLower();
                if (!lang.TryParseCultureExpression(out ICultureExpression cultureExpression))
                {
                    urlSpecifier = "";
                    action = path;
                    return null;
                }

                urlSpecifier = path.Substring(0, slashIndex);

                if (slashIndex < pathLength)
                {
                    action = path.Substring(slashIndex);
                    context.Request.PathBase = new PathString(action);
                }
                else
                {
                    action = "/";
                    context.Request.PathBase = new PathString("/");
                }

                if (checkCultureSupported && !cultureContext.CultureOption.IsCultureSupported(cultureExpression))
                {
                    return null;
                }

                return cultureExpression;
            }
            else
            {
                path = context.Request.Path;
                int slashIndex = 1;
                int pathLength = path.Length;
                for (; slashIndex < pathLength; slashIndex++)
                    if (path[slashIndex] == '/') break;
                string lang = path.Substring(1, slashIndex - 1).ToLower();
                if (!lang.TryParseCultureExpression(out ICultureExpression cultureExpression))
                {
                    urlSpecifier = "";
                    action = path;
                    return null;
                }

                urlSpecifier = path.Substring(0, slashIndex);

                if (slashIndex < pathLength)
                {
                    action = path.Substring(slashIndex);
                    context.Request.Path = new PathString(action);
                }
                else
                {
                    action = "/";
                    context.Request.Path = new PathString("/");
                }

                if (checkCultureSupported && !cultureContext.CultureOption.IsCultureSupported(cultureExpression))
                {
                    return null;
                }

                return cultureExpression;
            }
        }
    }
}
