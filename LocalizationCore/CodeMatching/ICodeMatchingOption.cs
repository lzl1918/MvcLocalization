namespace LocalizationCore.CodeMatching
{
    public interface ICodeMatchingOption
    {
        string ResourceDirectory { get; }
        bool IsCaseSensitive { get; }
    }

    internal sealed class CodeMatchingOption : ICodeMatchingOption
    {
        public string ResourceDirectory { get; }

        public bool IsCaseSensitive { get; }

        public CodeMatchingOption(string resourceDirectory, bool isCaseSensitive)
        {
            ResourceDirectory = resourceDirectory;
            IsCaseSensitive = isCaseSensitive;
        }

    }
}
