using System;
using System.Collections.Generic;
using System.Text;

namespace LocalizationCore
{
    public interface IFileCultureInfo
    {
        string RelativePath { get; }
        string FileName { get; }
        string FileNameWithoutCulture { get; }
        string Extension { get; }
        ICultureExpression Culture { get; }
        bool HasCulture { get; }
    }

    internal sealed class FileCultureInfo : IFileCultureInfo
    {
        public string RelativePath { get; }
        public string FileName { get; }
        public string FileNameWithoutCulture { get; }
        public string Extension { get; }
        public ICultureExpression Culture { get; }
        public bool HasCulture => Culture != null;

        public FileCultureInfo(string relativePath, string fileName, string fileNameWithoutCulture, string extension, ICultureExpression culture)
        {
            RelativePath = relativePath;
            FileName = fileName;
            FileNameWithoutCulture = fileNameWithoutCulture;
            Extension = extension;
            Culture = culture;
        }
    }
}
