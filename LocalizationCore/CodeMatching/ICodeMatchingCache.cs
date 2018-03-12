//using LocalizationCore.CodeMatching.Internals;
//using System.Collections.Generic;

//namespace LocalizationCore.CodeMatching
//{
//    internal interface ICodeMatchingCache
//    {
//        bool TryGetDisplayName(ICodedItem codedItem, ICultureExpression requestedCulture, out string name);
//        void UpdateCache(string code, string displayName, ICultureExpression culture);

//        void Clear();
//        void Clear(ICultureExpression culture);
//    }

//    internal sealed class CodeMatchingCache : ICodeMatchingCache
//    {
//        private sealed class CachePage
//        {
//            private CacheDictionary<string, string> codeValues;
//            public CachePage(int cacheSize)
//            {
//                codeValues = new CacheDictionary<string, string>(cacheSize);
//            }

//            public bool TryGetName(string code, out string name) => codeValues.TryGetValue(code, out name);
//            public void UpdateName(string code, string displayName) => codeValues[code] = displayName;
//        }

//        private readonly int singlePageSize;
//        private readonly CacheDictionary<string, CachePage> pages;

//        public CodeMatchingCache(int singlePageSize, int totalPageSize)
//        {
//            pages = new CacheDictionary<string, CachePage>(totalPageSize);
//            this.singlePageSize = singlePageSize;
//        }

//        public bool TryGetDisplayName(ICodedItem codedItem, ICultureExpression requestedCulture, out string name)
//        {
//            string culture = requestedCulture.DisplayName;
//            CachePage page;
//            if (!pages.TryGetValue(culture, out page))
//            {
//                page = new CachePage(singlePageSize);
//                pages[culture] = page;
//            }
//            return page.TryGetName(codedItem.Code, out name);
//        }

//        public void UpdateCache(string code, string displayName, ICultureExpression culture)
//        {
//            string cultureName = culture.DisplayName;
//            CachePage page;
//            if (!pages.TryGetValue(cultureName, out page))
//            {
//                page = new CachePage(singlePageSize);
//                pages[cultureName] = page;
//            }
//            page.UpdateName(code, displayName);
//        }

//        public void Clear() => pages.Clear();

//        public void Clear(ICultureExpression culture)
//        {
//            string cultureName = culture.DisplayName;
//            pages.Remove(cultureName);
//        }
//    }
//}
