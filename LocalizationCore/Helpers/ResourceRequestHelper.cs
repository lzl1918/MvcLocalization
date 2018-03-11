﻿using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LocalizationCore.Helpers
{
    public static class ResourceRequestHelper
    {
        private static bool TryFindFile(
            string parentPath,
            string name,
            string extension,
            IHostingEnvironment env,
            ICultureExpression requestedCulture,
            out string matchedName,
            out ICultureExpression matchedCulture)
        {
            string filePath;
            string fileName;
            string culture;

            if (requestedCulture.IsAllRegion)
            {
                // find by name
                // View.en.cshtml
                culture = requestedCulture.Language;
                fileName = $"{name}.{culture}.{extension}";
                filePath = Path.Combine(parentPath, fileName);
                if (env.ContentRootFileProvider.GetFileInfo(filePath).Exists)
                {
                    matchedCulture = requestedCulture;
                    matchedName = fileName;
                    return true;
                }

                // find the first region having the same language
                IDirectoryContents directoryContents = env.ContentRootFileProvider.GetDirectoryContents(parentPath);
                string startsWithFilter = $"{name}.{culture}";
                IFileInfo file = directoryContents.FirstOrDefault(x => x.Name.StartsWith(startsWithFilter) && x.Name.EndsWith(extension));
                if (file != null)
                {
                    string cultureName = file.Name.Substring(name.Length + 1);
                    cultureName = cultureName.Substring(0, cultureName.Length - extension.Length - 1);
                    matchedCulture = cultureName.ParseCultureExpression();
                    matchedName = file.Name;
                    return true;
                }
            }
            else
            {
                culture = requestedCulture.DisplayName;
                fileName = $"{name}.{culture}.{extension}";
                filePath = Path.Combine(parentPath, fileName);
                if (env.ContentRootFileProvider.GetFileInfo(filePath).Exists)
                {
                    matchedCulture = requestedCulture;
                    matchedName = fileName;
                    return true;
                }

                culture = requestedCulture.Language;
                fileName = $"{name}.{culture}.{extension}";
                filePath = Path.Combine(parentPath, fileName);
                if (env.ContentRootFileProvider.GetFileInfo(filePath).Exists)
                {
                    matchedCulture = requestedCulture.RemoveRegion();
                    matchedName = fileName;
                    return true;
                }
            }

            matchedCulture = null;
            matchedName = null;
            return false;
        }

        public static bool TryFindFile(
            string parentPath,
            string name,
            string extension,
            ICultureExpression requestedCulture,
            HttpContext httpContext,
            out string matchedName,
            out ICultureExpression matchedCulture)
        {
            IHostingEnvironment env = httpContext.RequestServices.GetService<IHostingEnvironment>();
            ICultureOption option = httpContext.RequestServices.GetService<ICultureOption>();

            // match requested
            if (TryFindFile(parentPath, name, extension, env, requestedCulture, out matchedName, out matchedCulture))
                return true;

            // match default
            if (TryFindFile(parentPath, name, extension, env, option.DefaultCulture, out matchedName, out matchedCulture))
                return true;

            // match no language
            string fileName = $"{name}.{extension}";
            IFileInfo file = env.ContentRootFileProvider.GetFileInfo(Path.Combine(parentPath, fileName));
            if (file.Exists)
            {
                matchedCulture = null;
                matchedName = fileName;
                return true;
            }

            // match failed
            matchedCulture = null;
            matchedName = null;
            return false;
        }
    }
}