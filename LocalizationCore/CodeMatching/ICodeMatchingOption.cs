namespace LocalizationCore.CodeMatching
{
    public interface ICodeMatchingOption
    {
        string ResourceDirectory { get; }
    }

    internal sealed class CodeMatchingOption : ICodeMatchingOption
    {
        public string ResourceDirectory { get; }

        public CodeMatchingOption(string resourceDirectory)
        {
            ResourceDirectory = resourceDirectory;
        }

    }
}
