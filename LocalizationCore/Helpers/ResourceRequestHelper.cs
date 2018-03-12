using Microsoft.AspNetCore.Hosting;
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
            IFileInfo file;
            if (requestedCulture.IsAllRegion)
            {
                // find by name
                // View.en.cshtml

                culture = requestedCulture.Language;
                fileName = $"{name}.{culture}.{extension}";
                filePath = Path.Combine(parentPath, fileName);
                file = env.ContentRootFileProvider.GetFileInfo(filePath);
                if (file.Exists && !file.IsDirectory)
                {
                    matchedCulture = requestedCulture;
                    matchedName = fileName;
                    return true;
                }

                // find the first region having the same language
                // View.en-XX.cshtml
                IDirectoryContents directoryContents = env.ContentRootFileProvider.GetDirectoryContents(parentPath);
                string startsWithFilter = $"{name}.{culture}";
                file = directoryContents.FirstOrDefault(x => !x.IsDirectory && x.Name.StartsWith(startsWithFilter) && x.Name.EndsWith(extension));
                if (file != null)
                {
                    string cultureName = file.Name.Substring(name.Length + 1);
                    cultureName = Path.GetFileNameWithoutExtension(cultureName);
                    matchedCulture = cultureName.ParseCultureExpression();
                    matchedName = file.Name;
                    return true;
                }

                // try find directory named with language
                // en/View.cshtml
                IFileInfo dir = env.ContentRootFileProvider.GetFileInfo(Path.Combine(parentPath, culture));
                if (dir.Exists && dir.IsDirectory)
                {
                    file = env.ContentRootFileProvider.GetFileInfo(Path.Combine(parentPath, dir.Name, $"{name}.{extension}"));
                    if (file.Exists && file.IsDirectory)
                    {
                        string cultureName = culture;
                        matchedCulture = cultureName.ParseCultureExpression();
                        matchedName = $"{dir.Name}/{file.Name}";
                        return true;
                    }
                }

                // try find directory named with the first region having the same language
                // en-XX/View.cshtml
                dir = directoryContents.FirstOrDefault(x => x.IsDirectory && x.Name.StartsWith(culture));
                if (dir != null)
                {
                    file = env.ContentRootFileProvider.GetFileInfo(Path.Combine(parentPath, dir.Name, $"{name}.{extension}"));
                    if (file.Exists && !file.IsDirectory)
                    {
                        string cultureName = dir.Name;
                        matchedCulture = cultureName.ParseCultureExpression();
                        matchedName = $"{dir.Name}/{file.Name}";
                        return true;
                    }
                }
            }
            else
            {
                // find by name
                // View.en-US.cshtml
                culture = requestedCulture.DisplayName;
                fileName = $"{name}.{culture}.{extension}";
                filePath = Path.Combine(parentPath, fileName);
                file = env.ContentRootFileProvider.GetFileInfo(filePath);
                if (file.Exists && !file.IsDirectory)
                {
                    matchedCulture = requestedCulture;
                    matchedName = fileName;
                    return true;
                }

                // find by language
                // View.en.cshtml
                culture = requestedCulture.Language;
                fileName = $"{name}.{culture}.{extension}";
                filePath = Path.Combine(parentPath, fileName);
                file = env.ContentRootFileProvider.GetFileInfo(filePath);
                if (file.Exists && !file.IsDirectory)
                {
                    matchedCulture = requestedCulture.RemoveRegion();
                    matchedName = fileName;
                    return true;
                }

                // try find directory named with name
                // en-US/View.cshtml
                IFileInfo dir = env.ContentRootFileProvider.GetFileInfo(Path.Combine(parentPath, requestedCulture.DisplayName));
                if (dir.Exists && dir.IsDirectory)
                {
                    file = env.ContentRootFileProvider.GetFileInfo(Path.Combine(parentPath, dir.Name, $"{name}.{extension}"));
                    if (file.Exists && !file.IsDirectory)
                    {
                        string cultureName = culture;
                        matchedCulture = cultureName.ParseCultureExpression();
                        matchedName = $"{dir.Name}/{file.Name}";
                        return true;
                    }
                }

                // try find directory named with the specific language
                // en/View.cshtml
                dir = env.ContentRootFileProvider.GetFileInfo(Path.Combine(parentPath, requestedCulture.Language));
                if (dir.Exists && dir.IsDirectory)
                {
                    file = env.ContentRootFileProvider.GetFileInfo(Path.Combine(parentPath, dir.Name, $"{name}.{extension}"));
                    if (file.Exists && !file.IsDirectory)
                    {
                        string cultureName = dir.Name;
                        matchedCulture = cultureName.ParseCultureExpression();
                        matchedName = $"{dir.Name}/{file.Name}";
                        return true;
                    }
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
