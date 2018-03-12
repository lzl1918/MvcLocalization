using System;
using System.Collections.Generic;
using System.Text;

namespace LocalizationCore
{
    public interface IFileCultureInfo
    {
        string RelativePath { get; }
        string FileName { get; }
        string ModelName { get; }
        string Extension { get; }
        ICultureExpression Culture { get; }
    }

    internal sealed class FileCultureInfo : IFileCultureInfo
    {
        public string RelativePath { get; }
        public string FileName { get; }
        public string ModelName { get; }
        public string Extension { get; }
        public ICultureExpression Culture { get; }

        public FileCultureInfo(string relativePath, string fileName, string modelName, string extension, string culture)
        {
            RelativePath = relativePath;
            FileName = fileName;
            ModelName = modelName;
            Extension = extension;
            Culture = culture.ParseCultureExpression();
        }
    }
}
