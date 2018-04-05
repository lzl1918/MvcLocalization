using Hake.Extension.Cache;
using LocalizationCore.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LocalizationCore.Services
{
    internal interface ICultureFileCache
    {
        IFileCultureInfo Get(ICultureExpression requestedCulture, string directory, string name, string extension);
    }

    internal sealed class CultureFileCache : ICultureFileCache
    {
        private sealed class CultureFilePage
        {
            private readonly ICultureExpression culture;
            private ICache<string, IFileCultureInfo> cache;

            internal CultureFilePage(ICultureExpression culture, int capacity)
            {
                cache = new Cache<string, IFileCultureInfo>(capacity);
                this.culture = culture;
            }

            internal IFileCultureInfo Get(string directory, string name, string extension, IHostingEnvironment env, ICultureOption cultureOption)
            {
                string resourceKey = $"{directory}/{name}.?.{extension}";
                return cache.Get(resourceKey, key =>
                {
                    if (ResourceRequestHelper.TryFindFile(directory, name, extension, culture, env, cultureOption, out IFileCultureInfo result))
                        return RetrivationResult<IFileCultureInfo>.Create(result);
                    return RetrivationResult<IFileCultureInfo>.Create(null);
                });
            }
        }


        private ICache<string, CultureFilePage> cultureCache;
        private int perCultureCacheSize;
        private readonly IHostingEnvironment env;
        private readonly ICultureOption cultureOption;


        internal CultureFileCache(IHostingEnvironment env, ICultureOption cultureOption, int cultureCacheSize, int perCultureCacheSize)
        {
            this.env = env;
            this.cultureOption = cultureOption;
            this.perCultureCacheSize = perCultureCacheSize;
            this.cultureCache = new Cache<string, CultureFilePage>(cultureCacheSize);
        }

        public IFileCultureInfo Get(ICultureExpression requestedCulture, string directory, string name, string extension)
        {
            string culture = requestedCulture.DisplayName;
            CultureFilePage culturePage = cultureCache.Get(culture, key => RetrivationResult<CultureFilePage>.Create(new CultureFilePage(requestedCulture, perCultureCacheSize)));
            return culturePage.Get(directory, name, extension, env, cultureOption);
        }
    }
}
