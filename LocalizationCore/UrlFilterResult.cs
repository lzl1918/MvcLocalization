namespace LocalizationCore
{
    public abstract class UrlFilterResult
    {
        internal UrlFilterResult() { }
    }

    public sealed class InvokeMiddlewareFilterResult : UrlFilterResult
    {

    }
    
    internal sealed class SetCultureFilterResult : UrlFilterResult
    {
        public string CultureSpecifier { get; set; }
        public ICultureExpression Culture { get; set; }
        public string PathBase { get; set; }
        public string Action { get; set; }

        public SetCultureFilterResult(string cultureSpecifier, ICultureExpression culture, string pathBase, string action)
        {
            CultureSpecifier = cultureSpecifier;
            PathBase = pathBase;
            Action = action;
            Culture = culture;
        }
    }
    public sealed class CheckHeaderFilterResult : UrlFilterResult
    {
    }
}
