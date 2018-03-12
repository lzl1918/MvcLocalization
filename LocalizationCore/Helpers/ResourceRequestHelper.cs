using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

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
            IDirectoryContents contents;
            IFileProvider provider = env.ContentRootFileProvider;
            if (requestedCulture.IsAllRegion)
            {
                // find by name
                // View.en.cshtml
                culture = requestedCulture.Language;
                fileName = $"{name}.{culture}.{extension}";
                filePath = $"{parentPath}/{fileName}";
                file = provider.GetFileInfo(filePath);
                if (file.Exists && !file.IsDirectory)
                {
                    matchedCulture = requestedCulture;
                    matchedName = fileName;
                    return true;
                }

                // try find directory named with language
                // en/View.cshtml
                contents = provider.GetDirectoryContents(parentPath);
                IFileInfo dir = contents.FirstOrDefault(x => x.IsDirectory && x.Name.Equals(culture));
                if (dir != null)
                {
                    file = provider.GetFileInfo($"{parentPath}/{dir.Name}/{name}.{extension}");
                    if (file.Exists && file.IsDirectory)
                    {
                        string cultureName = culture;
                        matchedCulture = cultureName.ParseCultureExpression();
                        matchedName = $"{dir.Name}/{file.Name}";
                        return true;
                    }
                }

                // find the first region having the same language
                // View.en-XX.cshtml
                string startsWithFilter = $"{name}.{culture}-";
                file = contents.FirstOrDefault(x => !x.IsDirectory && x.Name.StartsWith(startsWithFilter) && x.Name.EndsWith(extension));
                if (file != null)
                {
                    string cultureName = file.Name.Substring(name.Length + 1);
                    cultureName = Path.GetFileNameWithoutExtension(cultureName);
                    matchedCulture = cultureName.ParseCultureExpression();
                    matchedName = file.Name;
                    return true;
                }

                // try find directory named with the first region having the same language
                // en-XX/View.cshtml
                startsWithFilter = $"{culture}-";
                dir = contents.FirstOrDefault(x => x.IsDirectory && x.Name.StartsWith(startsWithFilter));
                if (dir != null)
                {
                    file = provider.GetFileInfo($"{parentPath}/{dir.Name}/{name}.{extension}");
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
                filePath = $"{parentPath}/{fileName}";
                file = provider.GetFileInfo(filePath);
                if (file.Exists && !file.IsDirectory)
                {
                    matchedCulture = requestedCulture;
                    matchedName = fileName;
                    return true;
                }

                // try find directory named with name
                // en-US/View.cshtml
                contents = provider.GetDirectoryContents(parentPath);
                IFileInfo dir = contents.FirstOrDefault(x => x.IsDirectory && x.Name.Equals(culture));
                if (dir != null)
                {
                    file = provider.GetFileInfo($"{parentPath}/{dir.Name}/{name}.{extension}");
                    if (file.Exists && !file.IsDirectory)
                    {
                        string cultureName = culture;
                        matchedCulture = cultureName.ParseCultureExpression();
                        matchedName = $"{dir.Name}/{file.Name}";
                        return true;
                    }
                }

                // find by language
                // View.en.cshtml
                culture = requestedCulture.Language;
                fileName = $"{name}.{culture}.{extension}";
                filePath = $"{parentPath}/{fileName}";
                file = provider.GetFileInfo(filePath);
                if (file.Exists && !file.IsDirectory)
                {
                    matchedCulture = requestedCulture.RemoveRegion();
                    matchedName = fileName;
                    return true;
                }

                // try find directory named with the specific language
                // en/View.cshtml
                dir = contents.FirstOrDefault(x => x.IsDirectory && x.Name.Equals(culture));
                if (dir != null)
                {
                    file = provider.GetFileInfo($"{parentPath}/{culture}/{name}.{extension}");
                    if (file.Exists && !file.IsDirectory)
                    {
                        string cultureName = dir.Name;
                        matchedCulture = cultureName.ParseCultureExpression();
                        matchedName = $"{culture}/{file.Name}";
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
            if (file.Exists && !file.IsDirectory)
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

        public static IList<IFileCultureInfo> FindFiles(
            string parentPath,
            string extension,
            ICultureExpression requestedCulture,
            HttpContext httpContext)
        {
            IHostingEnvironment env = httpContext.RequestServices.GetService<IHostingEnvironment>();
            string filePath;
            string fileName;
            string modelName;
            string nameNoExt;
            string culture;
            string filter;
            string relatedParent;
            int sub;
            int lastIndex;
            IEnumerable<IFileInfo> files;
            IEnumerable<IFileInfo> dirs;

            IFileProvider provider = env.ContentRootFileProvider;
            IDirectoryContents contents = provider.GetDirectoryContents(parentPath);
            List<IFileCultureInfo> result = new List<IFileCultureInfo>();
            SortedSet<string> models = new SortedSet<string>();
            if (requestedCulture.IsAllRegion)
            {
                // find by name
                // **.en.ext
                culture = requestedCulture.Language;
                filter = $".{culture}.{extension}";
                files = contents.Where(x => !x.IsDirectory && x.Name.EndsWith(filter));
                sub = filter.Length;
                foreach (IFileInfo file in files)
                {
                    fileName = file.Name;
                    filePath = $"{parentPath}/{fileName}"; // Path.Combine(parentPath, fileName)
                    modelName = fileName.Substring(0, fileName.Length - sub);
                    result.Add(new FileCultureInfo(filePath, fileName, modelName, extension, culture));
                    models.Add(modelName);
                }

                // try find directory named with language
                // en/**.ext
                IFileInfo dir = contents.FirstOrDefault(x => x.IsDirectory && x.Name.Equals(culture));
                if (dir != null)
                {
                    relatedParent = $"{parentPath}/{culture}";
                    IDirectoryContents directoryContents = provider.GetDirectoryContents(relatedParent);
                    files = directoryContents.Where(x => !x.IsDirectory && x.Name.EndsWith(extension));
                    foreach (IFileInfo file in files)
                    {
                        fileName = file.Name;
                        filePath = $"{relatedParent}/{fileName}";// Path.Combine(relatedParent, fileName)
                        modelName = Path.GetFileNameWithoutExtension(fileName);
                        if (!models.Contains(modelName))
                        {
                            result.Add(new FileCultureInfo(filePath, fileName, modelName, extension, culture));
                            models.Add(modelName);
                        }
                    }
                }

                // find the regions having the same language
                // **.en-**.ext
                filter = $@"\.{culture}-\w\w\.{extension}$";
                Regex reg = new Regex(filter);
                files = contents.Where(x => !x.IsDirectory && reg.IsMatch(x.Name));
                foreach (IFileInfo file in files)
                {
                    fileName = file.Name;
                    nameNoExt = Path.GetFileNameWithoutExtension(fileName);
                    lastIndex = nameNoExt.LastIndexOf('.');
                    modelName = fileName.Substring(0, lastIndex);
                    filePath = $"{parentPath}/{fileName}"; //Path.Combine(parentPath, fileName)
                    culture = nameNoExt.Substring(lastIndex + 1);
                    if (!models.Contains(modelName))
                    {
                        result.Add(new FileCultureInfo(filePath, fileName, modelName, extension, culture));
                        models.Add(modelName);
                    }
                }

                // try find directory named with regions having the same language
                // en-**/**.ext
                filter = $@"^{culture}-\w\w$";
                reg = new Regex(filter);
                dirs = contents.Where(x => x.IsDirectory && reg.IsMatch(x.Name));
                foreach (IFileInfo langDir in dirs)
                {
                    culture = langDir.Name;
                    relatedParent = $"{parentPath}/{culture}"; //Path.Combine(parentPath, culture)

                    IDirectoryContents directoryContents = provider.GetDirectoryContents(relatedParent);
                    files = directoryContents.Where(x => !x.IsDirectory && x.Name.EndsWith(extension));
                    foreach (IFileInfo file in files)
                    {
                        fileName = file.Name;
                        filePath = $"{relatedParent}/{fileName}"; //Path.Combine(relatedParent, fileName)
                        modelName = Path.GetFileNameWithoutExtension(fileName);
                        if (!models.Contains(modelName))
                        {
                            result.Add(new FileCultureInfo(filePath, fileName, modelName, extension, culture));
                            models.Add(modelName);
                        }
                    }
                }
            }
            else
            {
                // find by name
                // **.en-US.ext
                culture = requestedCulture.DisplayName;
                filter = $".{culture}.{extension}";
                files = contents.Where(x => !x.IsDirectory && x.Name.EndsWith(filter));
                sub = filter.Length;
                foreach (IFileInfo file in files)
                {
                    fileName = file.Name;
                    filePath = $"{parentPath}/{fileName}"; //Path.Combine(parentPath, fileName)
                    modelName = fileName.Substring(0, fileName.Length - sub);
                    result.Add(new FileCultureInfo(filePath, fileName, modelName, extension, culture));
                    models.Add(modelName);
                }

                // try find directory named with culture name
                // en-US/**.ext
                dirs = contents.Where(x => x.IsDirectory && x.Name.Equals(culture));
                foreach (IFileInfo langDir in dirs)
                {
                    relatedParent = $"{parentPath}/{culture}"; //Path.Combine(parentPath, culture)

                    IDirectoryContents directoryContents = provider.GetDirectoryContents(relatedParent);
                    files = directoryContents.Where(x => !x.IsDirectory && x.Name.EndsWith(extension));
                    foreach (IFileInfo file in files)
                    {
                        fileName = file.Name;
                        filePath = $"{relatedParent}/{fileName}"; //Path.Combine(relatedParent, fileName)
                        modelName = Path.GetFileNameWithoutExtension(fileName);
                        if (!models.Contains(modelName))
                        {
                            result.Add(new FileCultureInfo(filePath, fileName, modelName, extension, culture));
                            models.Add(modelName);
                        }
                    }
                }

                // find the regions having the same language
                // **.en.ext
                culture = requestedCulture.Language;
                filter = $".{culture}.{extension}";
                files = contents.Where(x => !x.IsDirectory && x.Name.EndsWith(filter));
                sub = filter.Length;
                foreach (IFileInfo file in files)
                {
                    fileName = file.Name;
                    filePath = $"{parentPath}/{fileName}"; //Path.Combine(parentPath, fileName)
                    modelName = fileName.Substring(0, fileName.Length - sub);
                    if (!models.Contains(modelName))
                    {
                        result.Add(new FileCultureInfo(filePath, fileName, modelName, extension, culture));
                        models.Add(modelName);
                    }
                }

                // try find directory named with the same language
                // en/**.ext
                relatedParent = $"{parentPath}/{culture}"; //Path.Combine(parentPath, culture)
                IFileInfo dir = contents.FirstOrDefault(x => x.IsDirectory && x.Name.Equals(culture));
                if (dir != null)
                {
                    IDirectoryContents directoryContents = provider.GetDirectoryContents(relatedParent);
                    files = directoryContents.Where(x => !x.IsDirectory && x.Name.EndsWith(extension));
                    foreach (IFileInfo file in files)
                    {
                        fileName = file.Name;
                        filePath = $"{relatedParent}/{fileName}";  //Path.Combine(relatedParent, fileName)
                        modelName = Path.GetFileNameWithoutExtension(fileName);
                        if (!models.Contains(modelName))
                        {
                            result.Add(new FileCultureInfo(filePath, fileName, modelName, extension, culture));
                            models.Add(modelName);
                        }
                    }
                }
            }
            return result;
        }
    }
}
