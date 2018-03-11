using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LocalizationCore
{

    public interface ICultureOption
    {
        ICultureExpression DefaultCulture { get; }
        IReadOnlyCollection<ICultureExpression> SupportedCultures { get; }
    }

    internal sealed class CultureExpressionComparer : IComparer<ICultureExpression>
    {
        public int Compare(ICultureExpression x, ICultureExpression y)
        {
            return x.ToString().CompareTo(y.ToString());
        }
    }
    internal sealed class CultureOption : ICultureOption
    {
        private ICultureExpression defaultCulture;
        private IReadOnlyCollection<ICultureExpression> supportedCultures;

        public ICultureExpression DefaultCulture => defaultCulture;

        public IReadOnlyCollection<ICultureExpression> SupportedCultures => supportedCultures;

        internal CultureOption(string defaultCulture, IEnumerable<string> supportedCultures)
        {
            this.defaultCulture = defaultCulture.ParseCultureExpression();
            this.supportedCultures = new SortedSet<ICultureExpression>(supportedCultures.Select(exp => exp.ParseCultureExpression()), new CultureExpressionComparer()).ToList();
        }
    }

    public static class CultureOptionExtensions
    {
        public static bool IsCultureSupported(this ICultureOption cultureOption, ICultureExpression culture)
        {
            if (culture.IsAllRegion)
                return cultureOption.SupportedCultures.Any(exp => exp.Language.Equals(culture.Language));

            return cultureOption.SupportedCultures.Any(exp =>
            {
                if (exp.IsAllRegion)
                    return exp.Language.Equals(culture.Language);
                return exp.Language.Equals(culture.Language) && exp.Region.Equals(culture.Region);
            });
        }

        public static bool IsCultureSupported(this ICultureOption cultureOption, ICultureExpression culture, out ICultureExpression supportedCulture)
        {
            ICultureExpression result;
            if (culture.IsAllRegion)
            {
                result = cultureOption.SupportedCultures.FirstOrDefault(exp => exp.Language.Equals(culture.Language));
                supportedCulture = result;
                return supportedCulture != null;
            }
            result = cultureOption.SupportedCultures.FirstOrDefault(exp => exp.Language.Equals(culture.Language) && exp.Region.Equals(culture.Region));
            supportedCulture = result;
            return supportedCulture != null;
        }
    }
}
