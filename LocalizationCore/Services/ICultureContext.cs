using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace LocalizationCore
{
    public interface ICultureContext
    {
        ICultureOption CultureOption { get; }
        ICultureExpression CurrentCulture { get; set; }
        string Action { get; set; }
        string UrlCultureSpecifier { get; set; }
    }

    internal sealed class CultureContext : ICultureContext
    {
        private readonly ICultureOption cultureOption;
        private ICultureExpression currentCulture;
        private string action;
        private string urlCultureSpecifier;

        public ICultureOption CultureOption => cultureOption;

        public ICultureExpression CurrentCulture { get => currentCulture; set => currentCulture = value; }
        public string Action { get => action; set => action = value; }
        public string UrlCultureSpecifier { get => urlCultureSpecifier; set => urlCultureSpecifier = value; }

        public CultureContext(ICultureOption cultureOption)
        {
            this.cultureOption = cultureOption;
        }
    }
}
