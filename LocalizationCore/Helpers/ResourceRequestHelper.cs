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
            string directory,
            string fileName,
            string extension,
            IHostingEnvironment env,
            ICultureExpression requestedCulture,
            out IFileCultureInfo result)
        {
            string filePath;
            string targetName;
            string culture;
            IFileInfo file;
            IDirectoryContents contents;
            IFileProvider provider = env.ContentRootFileProvider;
            if (requestedCulture.IsAllRegion)
            {
                // find by name
                // View.en.cshtml
                culture = requestedCulture.Language;
                targetName = $"{fileName}.{culture}.{extension}";
                filePath = $"{directory}/{targetName}";
                file = provider.GetFileInfo(filePath);
                if (file.Exists && !file.IsDirectory)
                {
                    result = new FileCultureInfo(filePath, targetName, fileName, extension, requestedCulture);
                    return true;
                }

                // try find directory named with language
                // en/View.cshtml
                contents = provider.GetDirectoryContents(directory);
                IFileInfo dir = contents.FirstOrDefault(x => x.IsDirectory && x.Name.Equals(culture));
                if (dir != null)
                {
                    targetName = $"{fileName}.{extension}";
                    filePath = $"{directory}/{dir.Name}/{targetName}";
                    file = provider.GetFileInfo(filePath);
                    if (file.Exists && !file.IsDirectory)
                    {
                        result = new FileCultureInfo(filePath, targetName, fileName, extension, requestedCulture);
                        return true;
                    }
                }

                // find the first region having the same language
                // View.en-XX.cshtml
                string startsWithFilter = $"{fileName}.{culture}-";
                string endsWithFilter = $".{extension}";
                file = contents.FirstOrDefault(x => !x.IsDirectory && x.Name.StartsWith(startsWithFilter) && x.Name.EndsWith(endsWithFilter));
                if (file != null)
                {
                    culture = file.Name.Substring(fileName.Length + 1);
                    culture = Path.GetFileNameWithoutExtension(culture);
                    filePath = $"{directory}/{file.Name}";
                    result = new FileCultureInfo(filePath, file.Name, fileName, extension, culture.ParseCultureExpression());
                    return true;
                }

                // try find directory named with the first region having the same language
                // en-XX/View.cshtml
                startsWithFilter = $"{culture}-";
                dir = contents.FirstOrDefault(x => x.IsDirectory && x.Name.StartsWith(startsWithFilter));
                if (dir != null)
                {
                    targetName = $"{fileName}.{extension}";
                    filePath = $"{directory}/{dir.Name}/{targetName}";
                    file = provider.GetFileInfo(filePath);
                    if (file.Exists && !file.IsDirectory)
                    {
                        culture = dir.Name;
                        result = new FileCultureInfo(filePath, targetName, fileName, extension, culture.ParseCultureExpression());
                        return true;
                    }
                }
            }
            else
            {
                // find by name
                // View.en-US.cshtml
                culture = requestedCulture.DisplayName;
                targetName = $"{fileName}.{culture}.{extension}";
                filePath = $"{directory}/{targetName}";
                file = provider.GetFileInfo(filePath);
                if (file.Exists && !file.IsDirectory)
                {
                    result = new FileCultureInfo(filePath, targetName, fileName, extension, requestedCulture);
                    return true;
                }

                // try find directory named with name
                // en-US/View.cshtml
                contents = provider.GetDirectoryContents(directory);
                IFileInfo dir = contents.FirstOrDefault(x => x.IsDirectory && x.Name.Equals(culture));
                if (dir != null)
                {
                    targetName = $"{fileName}.{extension}";
                    filePath = $"{directory}/{culture}/{targetName}";
                    file = provider.GetFileInfo(filePath);
                    if (file.Exists && !file.IsDirectory)
                    {
                        result = new FileCultureInfo(filePath, targetName, fileName, extension, requestedCulture);
                        return true;
                    }
                }

                // find by language
                // View.en.cshtml
                culture = requestedCulture.Language;
                targetName = $"{fileName}.{culture}.{extension}";
                filePath = $"{directory}/{targetName}";
                file = provider.GetFileInfo(filePath);
                if (file.Exists && !file.IsDirectory)
                {
                    result = new FileCultureInfo(filePath, targetName, fileName, extension, culture.ParseCultureExpression());
                    return true;
                }

                // try find directory named with the specific language
                // en/View.cshtml
                dir = contents.FirstOrDefault(x => x.IsDirectory && x.Name.Equals(culture));
                if (dir != null)
                {
                    targetName = $"{fileName}.{extension}";
                    filePath = $"{directory}/{culture}/{targetName}";
                    file = provider.GetFileInfo(filePath);
                    if (file.Exists && !file.IsDirectory)
                    {
                        result = new FileCultureInfo(filePath, targetName, fileName, extension, culture.ParseCultureExpression());
                        return true;
                    }
                }
            }

            result = null;
            return false;
        }

        public static bool TryFindFile(
            string directory,
            string fileName,
            string extension,
            ICultureExpression requestedCulture,
            IHostingEnvironment env,
            ICultureOption cultureOption,
            out IFileCultureInfo result)
        {
            // match requested
            if (TryFindFile(directory, fileName, extension, env, requestedCulture, out result))
                return true;

            // match default
            if (TryFindFile(directory, fileName, extension, env, cultureOption.DefaultCulture, out result))
                return true;

            // match no language
            string targetName = $"{fileName}.{extension}";
            IFileInfo file = env.ContentRootFileProvider.GetFileInfo(Path.Combine(directory, targetName));
            if (file.Exists && !file.IsDirectory)
            {
                result = new FileCultureInfo($"{directory}/{targetName}", targetName, fileName, extension, null);
                return true;
            }

            // match failed
            result = null;
            return false;
        }

        public static bool TryFindFile(
            string directory,
            string fileName,
            string extension,
            ICultureExpression requestedCulture,
            HttpContext httpContext,
            out IFileCultureInfo result)
        {
            IHostingEnvironment env = httpContext.RequestServices.GetRequiredService<IHostingEnvironment>();
            ICultureOption option = httpContext.RequestServices.GetRequiredService<ICultureOption>();

            return TryFindFile(directory, fileName, extension, requestedCulture, env, option, out result);
        }

        public static IList<IFileCultureInfo> FindFiles(
            string directory,
            string extension,
            ICultureExpression requestedCulture,
            IHostingEnvironment env)
        {
            string filePath;
            string targetName;
            string fileNameWithoutCulture;
            string nameNoExt;
            string culture;
            string filter;
            string relativeParent;
            int subLength;
            int lastIndex;
            string endsWithFilter = $".{extension}";
            IEnumerable<IFileInfo> files;
            IEnumerable<IFileInfo> dirs;

            IFileProvider provider = env.ContentRootFileProvider;
            IDirectoryContents contents = provider.GetDirectoryContents(directory);
            List<IFileCultureInfo> result = new List<IFileCultureInfo>();
            SortedSet<string> models = new SortedSet<string>();
            if (requestedCulture.IsAllRegion)
            {
                // find by name
                // **.en.ext
                culture = requestedCulture.Language;
                filter = $".{culture}.{extension}";
                files = contents.Where(x => !x.IsDirectory && x.Name.EndsWith(filter));
                subLength = filter.Length;
                foreach (IFileInfo file in files)
                {
                    targetName = file.Name;
                    filePath = $"{directory}/{targetName}"; // Path.Combine(parentPath, fileName)
                    fileNameWithoutCulture = targetName.Substring(0, targetName.Length - subLength);
                    result.Add(new FileCultureInfo(filePath, targetName, fileNameWithoutCulture, extension, requestedCulture));
                    models.Add(fileNameWithoutCulture);
                }

                // try find directory named with language
                // en/**.ext
                IFileInfo dir = contents.FirstOrDefault(x => x.IsDirectory && x.Name.Equals(culture));
                if (dir != null)
                {
                    relativeParent = $"{directory}/{culture}";
                    IDirectoryContents directoryContents = provider.GetDirectoryContents(relativeParent);
                    files = directoryContents.Where(x => !x.IsDirectory && x.Name.EndsWith(endsWithFilter));
                    foreach (IFileInfo file in files)
                    {
                        targetName = file.Name;
                        filePath = $"{relativeParent}/{targetName}";// Path.Combine(relatedParent, fileName)
                        fileNameWithoutCulture = Path.GetFileNameWithoutExtension(targetName);
                        if (!models.Contains(fileNameWithoutCulture))
                        {
                            result.Add(new FileCultureInfo(filePath, targetName, fileNameWithoutCulture, extension, requestedCulture));
                            models.Add(fileNameWithoutCulture);
                        }
                    }
                }

                // find the regions having the same language
                // **.en-**.ext
                filter = $@"\.{culture}-[a-zA-Z]{{2}}\.{extension}$";
                Regex reg = new Regex(filter);
                files = contents.Where(x => !x.IsDirectory && reg.IsMatch(x.Name));
                foreach (IFileInfo file in files)
                {
                    targetName = file.Name;
                    nameNoExt = Path.GetFileNameWithoutExtension(targetName);
                    lastIndex = nameNoExt.LastIndexOf('.');
                    fileNameWithoutCulture = targetName.Substring(0, lastIndex);
                    filePath = $"{directory}/{targetName}"; //Path.Combine(parentPath, fileName)
                    culture = nameNoExt.Substring(lastIndex + 1);
                    if (!models.Contains(fileNameWithoutCulture))
                    {
                        result.Add(new FileCultureInfo(filePath, targetName, fileNameWithoutCulture, extension, culture.ParseCultureExpression()));
                        models.Add(fileNameWithoutCulture);
                    }
                }

                // try find directory named with regions having the same language
                // en-**/**.ext
                filter = $"^{culture}-[a-zA-Z]{{2}}$";
                reg = new Regex(filter);
                dirs = contents.Where(x => x.IsDirectory && reg.IsMatch(x.Name));
                foreach (IFileInfo langDir in dirs)
                {
                    culture = langDir.Name;
                    ICultureExpression cultureExp = culture.ParseCultureExpression();
                    relativeParent = $"{directory}/{culture}"; //Path.Combine(parentPath, culture)

                    IDirectoryContents directoryContents = provider.GetDirectoryContents(relativeParent);
                    files = directoryContents.Where(x => !x.IsDirectory && x.Name.EndsWith(endsWithFilter));
                    foreach (IFileInfo file in files)
                    {
                        targetName = file.Name;
                        filePath = $"{relativeParent}/{targetName}"; //Path.Combine(relatedParent, fileName)
                        fileNameWithoutCulture = Path.GetFileNameWithoutExtension(targetName);
                        if (!models.Contains(fileNameWithoutCulture))
                        {
                            result.Add(new FileCultureInfo(filePath, targetName, fileNameWithoutCulture, extension, cultureExp));
                            models.Add(fileNameWithoutCulture);
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
                subLength = filter.Length;
                foreach (IFileInfo file in files)
                {
                    targetName = file.Name;
                    filePath = $"{directory}/{targetName}"; //Path.Combine(parentPath, fileName)
                    fileNameWithoutCulture = targetName.Substring(0, targetName.Length - subLength);
                    result.Add(new FileCultureInfo(filePath, targetName, fileNameWithoutCulture, extension, requestedCulture));
                    models.Add(fileNameWithoutCulture);
                }

                // try find directory named with culture name
                // en-US/**.ext
                dirs = contents.Where(x => x.IsDirectory && x.Name.Equals(culture));
                foreach (IFileInfo langDir in dirs)
                {
                    relativeParent = $"{directory}/{culture}"; //Path.Combine(parentPath, culture)

                    IDirectoryContents directoryContents = provider.GetDirectoryContents(relativeParent);
                    files = directoryContents.Where(x => !x.IsDirectory && x.Name.EndsWith(endsWithFilter));
                    foreach (IFileInfo file in files)
                    {
                        targetName = file.Name;
                        filePath = $"{relativeParent}/{targetName}"; //Path.Combine(relatedParent, fileName)
                        fileNameWithoutCulture = Path.GetFileNameWithoutExtension(targetName);
                        if (!models.Contains(fileNameWithoutCulture))
                        {
                            result.Add(new FileCultureInfo(filePath, targetName, fileNameWithoutCulture, extension, requestedCulture));
                            models.Add(fileNameWithoutCulture);
                        }
                    }
                }

                // find the regions having the same language
                // **.en.ext
                culture = requestedCulture.Language;
                ICultureExpression cultureExp = culture.ParseCultureExpression();
                filter = $".{culture}.{extension}";
                files = contents.Where(x => !x.IsDirectory && x.Name.EndsWith(filter));
                subLength = filter.Length;
                foreach (IFileInfo file in files)
                {
                    targetName = file.Name;
                    filePath = $"{directory}/{targetName}"; //Path.Combine(parentPath, fileName)
                    fileNameWithoutCulture = targetName.Substring(0, targetName.Length - subLength);
                    if (!models.Contains(fileNameWithoutCulture))
                    {
                        result.Add(new FileCultureInfo(filePath, targetName, fileNameWithoutCulture, extension, cultureExp));
                        models.Add(fileNameWithoutCulture);
                    }
                }

                // try find directory named with the same language
                // en/**.ext
                relativeParent = $"{directory}/{culture}"; //Path.Combine(parentPath, culture)
                IFileInfo dir = contents.FirstOrDefault(x => x.IsDirectory && x.Name.Equals(culture));
                if (dir != null)
                {
                    IDirectoryContents directoryContents = provider.GetDirectoryContents(relativeParent);
                    files = directoryContents.Where(x => !x.IsDirectory && x.Name.EndsWith(endsWithFilter));
                    foreach (IFileInfo file in files)
                    {
                        targetName = file.Name;
                        filePath = $"{relativeParent}/{targetName}";  //Path.Combine(relatedParent, fileName)
                        fileNameWithoutCulture = Path.GetFileNameWithoutExtension(targetName);
                        if (!models.Contains(fileNameWithoutCulture))
                        {
                            result.Add(new FileCultureInfo(filePath, targetName, fileNameWithoutCulture, extension, cultureExp));
                            models.Add(fileNameWithoutCulture);
                        }
                    }
                }
            }
            return result;
        }

        public static IList<IFileCultureInfo> FindFiles(
            string parentPath,
            string extension,
            ICultureExpression requestedCulture,
            HttpContext httpContext)
        {
            IHostingEnvironment env = httpContext.RequestServices.GetRequiredService<IHostingEnvironment>();
            return FindFiles(parentPath, extension, requestedCulture, env);
        }
    }
}
