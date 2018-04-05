using Hake.Extension.Cache;
using LocalizationCore.Helpers;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.Text;

namespace LocalizationCore.Services
{
    internal interface ICultureFileCollectionCache
    {
        IList<IFileCultureInfo> Get(ICultureExpression requestedCulture, string directory, string extension);
    }

    internal sealed class CultureFileCollectionCache : ICultureFileCollectionCache
    {
        private sealed class CultureFileCollectionPage
        {
            private readonly ICultureExpression culture;
            private ICache<string, IList<IFileCultureInfo>> cache;

            internal CultureFileCollectionPage(ICultureExpression culture, int capacity)
            {
                cache = new Cache<string, IList<IFileCultureInfo>>(capacity);
                this.culture = culture;
            }

            internal IList<IFileCultureInfo> Get(string directory, string extension, IHostingEnvironment env)
            {
                string resourceKey = $"{directory}/?.?.{extension}";
                return cache.Get(resourceKey, key =>
                {
                    IList<IFileCultureInfo> result = ResourceRequestHelper.FindFiles(directory, extension, culture, env);
                    return RetrivationResult<IList<IFileCultureInfo>>.Create(result);
                });
            }
        }


        private ICache<string, CultureFileCollectionPage> cultureCache;
        private int perCultureCacheSize;
        private readonly IHostingEnvironment env;


        internal CultureFileCollectionCache(IHostingEnvironment env, int cultureCacheSize, int perCultureCacheSize)
        {
            this.env = env;
            this.perCultureCacheSize = perCultureCacheSize;
            this.cultureCache = new Cache<string, CultureFileCollectionPage>(cultureCacheSize);
        }

        public IList<IFileCultureInfo> Get(ICultureExpression requestedCulture, string directory, string extension)
        {
            string culture = requestedCulture.DisplayName;
            CultureFileCollectionPage culturePage = cultureCache.Get(culture, key => RetrivationResult<CultureFileCollectionPage>.Create(new CultureFileCollectionPage(requestedCulture, perCultureCacheSize)));
            return culturePage.Get(directory, extension, env);
        }
    }

}
