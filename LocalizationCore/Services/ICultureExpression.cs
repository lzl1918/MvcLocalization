using System;

namespace LocalizationCore
{
    public interface ICultureExpression : IEquatable<ICultureExpression>, IComparable<ICultureExpression>
    {
        string DisplayName { get; }
        string Language { get; }
        string Region { get; }
        bool IsAllRegion { get; }
    }

    internal sealed class CultureExpression : ICultureExpression
    {
        private string displayName;
        private readonly string language;
        private readonly string region;
        private readonly bool isAllRegion;

        public string DisplayName => displayName ?? (displayName = GetDisplayName());

        public string Language => language;

        public string Region => region;

        public bool IsAllRegion => isAllRegion;


        internal CultureExpression(string language, string region)
        {
            this.language = language;
            this.region = region;
            isAllRegion = region == "*";
        }
        private string GetDisplayName()
        {
            if (isAllRegion)
                return language;
            return $"{language}-{region}";
        }

        public override string ToString() => DisplayName;
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (obj is ICultureExpression other)
                return this.Equals(other);
            return false;
        }
        public override int GetHashCode()
        {
            return displayName.GetHashCode();
        }

        public bool Equals(ICultureExpression other)
        {
            if (other == null)
                return false;
            return DisplayName.Equals(other.DisplayName);
        }

        public int CompareTo(ICultureExpression other)
        {
            if (other == null)
                return -1;
            return DisplayName.CompareTo(other.DisplayName);
        }
    }

    public static class CultureExpressionExtensions
    {
        public static bool TryParseCultureExpression(this string language, out ICultureExpression expression)
        {
            if (language.Length == 2)
            {
                expression = new CultureExpression(language.ToLower(), "*");
                return true;
            }
            else if (language.Length == 4 && language.EndsWith("-*"))
            {
                expression = new CultureExpression(language.Substring(0, 2).ToLower(), "*");
                return true;
            }
            else if (language.Length == 5 && (language[2] == '-' || language[2] == '_'))
            {
                expression = new CultureExpression(language.Substring(0, 2).ToLower(), language.Substring(3, 2).ToUpper());
                return true;
            }
            expression = null;
            return false;
        }

        public static ICultureExpression ParseCultureExpression(this string language)
        {
            if (language.Length == 2)
            {
                return new CultureExpression(language.ToLower(), "*");
            }
            else if (language.Length == 4 && language.EndsWith("-*"))
            {
                return new CultureExpression(language.Substring(0, 2).ToLower(), "*");
            }
            else if (language.Length == 5 && language[2] == '-')
            {
                return new CultureExpression(language.Substring(0, 2).ToLower(), language.Substring(3, 2).ToUpper());
            }
            throw new FormatException($"cannot parse {language} as a vaild language pattern");
        }

        public static ICultureExpression RemoveRegion(this ICultureExpression culture)
        {
            if (culture.IsAllRegion)
                return culture;
            else
                return new CultureExpression(culture.Language, "*");
        }
    }
}
