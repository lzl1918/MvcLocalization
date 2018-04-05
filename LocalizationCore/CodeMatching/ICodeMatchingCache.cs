using Hake.Extension.Cache;
using Hake.Extension.ValueRecord;
using LocalizationCore.Helpers;
using LocalizationCore.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LocalizationCore.CodeMatching
{
    
    internal interface ICodeMatchingCache
    {
        string Get(HttpContext httpContext, string code, string defaultName, ICultureExpression requestedCulture);
    }

    internal sealed class CodeMatchingCache : ICodeMatchingCache
    {
        internal class CulturePage
        {
            private readonly ICache<string, string> contentCache;
            private readonly SetRecord values;
            internal ICache<string, string> ContentCache => contentCache;
            internal SetRecord Values => values;

            public CulturePage(int capacity, SetRecord values)
            {
                contentCache = new Cache<string, string>(capacity);
                this.values = values;
            }

            internal virtual string Get(string code, string defaultName)
            {
                return ContentCache.Get(code, key =>
                {
                    if (values.TryReadAs<string>($"{key}.name", out string value) &&
                        value != null)
                        return RetrivationResult<string>.Create(value);
                    return RetrivationResult<string>.SupressResult(defaultName);
                });
            }
        }
        internal class EmptyCulturePage : CulturePage
        {
            public EmptyCulturePage() : base(1, null)
            {
            }

            internal override string Get(string code, string defaultName)
            {
                return defaultName;
            }
        }

        private readonly ICache<string, CulturePage> cultureCache;
        private readonly int perCultureCapacity;
        private readonly ICodeMatchingOption matchingOption;
        private readonly ICultureOption cultureOption;
        private readonly ICultureFileCache cultureFileCache;

        public CodeMatchingCache(
            int cultureCacheCapacity,
            int perCultureCapacity,
            ICodeMatchingOption matchingOption,
            ICultureOption cultureOption,
            ICultureFileCache cultureFileCache)
        {
            this.cultureCache = new Cache<string, CulturePage>(cultureCacheCapacity);
            this.perCultureCapacity = perCultureCapacity;
            this.matchingOption = matchingOption;
            this.cultureOption = cultureOption;
            this.cultureFileCache = cultureFileCache;
        }

        public string Get(HttpContext httpContext, string code, string defaultName, ICultureExpression requestedCulture)
        {
            string culture = requestedCulture.DisplayName;
            CulturePage culturePage = cultureCache.Get(culture, key =>
            {
                string basePath = matchingOption.ResourceDirectory;
                IList<IFileCultureInfo> files = ResourceRequestHelper.FindFiles(basePath, "json", requestedCulture, httpContext);
                if (files.Count <= 0)
                    files = ResourceRequestHelper.FindFiles(basePath, "json", cultureOption.DefaultCulture, httpContext);
                if (files.Count <= 0)
                    return RetrivationResult<CulturePage>.Create(new EmptyCulturePage());

                string filePath;
                IHostingEnvironment env = httpContext.RequestServices.GetRequiredService<IHostingEnvironment>();
                IFileProvider provider = env.ContentRootFileProvider;
                SetRecord values = new SetRecord();
                foreach (IFileCultureInfo file in files.Reverse())
                {
                    filePath = file.RelativePath;
                    IFileInfo current = provider.GetFileInfo(filePath);
                    try
                    {
                        using (Stream fileStream = current.CreateReadStream())
                        {
                            SetRecord fileContent = (SetRecord)Hake.Extension.ValueRecord.Json.Converter.ReadJson(fileStream, !matchingOption.IsCaseSensitive);
                            CombineSetRecord(values, fileContent);
                        }
                    }
                    catch
                    {

                    }
                }
                return RetrivationResult<CulturePage>.Create(new CulturePage(perCultureCapacity, values));
            });
            return culturePage.ContentCache.Get(code, key =>
            {
                if (culturePage.Values.TryReadAs<string>($"{key}.name", out string value) &&
                    value != null)
                    return RetrivationResult<string>.Create(value);
                return RetrivationResult<string>.SupressResult(defaultName);
            });
        }

        private static void CombineSetRecord(SetRecord destination, SetRecord source)
        {
            string key;
            RecordBase value;
            RecordBase destValue;
            foreach (var pair in source)
            {
                key = pair.Key;
                value = pair.Value;
                if (destination.TryGetValue(key, out destValue))
                {
                    if (value is SetRecord sourceSet && destValue is SetRecord destSet)
                    {
                        CombineSetRecord(destSet, sourceSet);
                    }
                    else if (value is ListRecord sourceList && destValue is ListRecord destList)
                    {
                        foreach (RecordBase sourceListElement in sourceList)
                            destList.Add(sourceListElement);
                    }
                    else
                    {
                        destination[key] = value;
                    }
                }
                else
                {
                    destination[key] = value;
                }
            }
        }
    }
}
