using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace LocalizationCore
{
    public interface ILocalizedViewRenderContext
    {
        ICultureExpression RequestedCulture { get; }
        ICultureExpression RenderedCulture { get; }
        string UrlCultureSpecifier { get; }
    }

    internal sealed class LocalizedViewRenderContext : ILocalizedViewRenderContext
    {
        public ICultureExpression RequestedCulture { get; }

        public ICultureExpression RenderedCulture { get; }

        public string UrlCultureSpecifier { get; }

        internal LocalizedViewRenderContext(ICultureExpression requestedCulture, ICultureExpression renderedCulture, string urlCultureSpecifier)
        {
            RequestedCulture = requestedCulture;
            RenderedCulture = renderedCulture;
            UrlCultureSpecifier = urlCultureSpecifier;
        }


    }


    internal interface ILocalizedViewRenderContextAccessor
    {
        ILocalizedViewRenderContext Context { get; set; }
    }
    internal sealed class LocalizedViewRenderContextAccessor : ILocalizedViewRenderContextAccessor
    {
        public ILocalizedViewRenderContext Context { get; set; }
    }
}
