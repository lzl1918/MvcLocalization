using System;
using System.Text;

namespace LocalizationCore.CodeMatching
{
    public interface ICodedItem
    {
        string Code { get; }
        string DefaultName { get; }
        string DisplayName { get; set; }
    }
}
