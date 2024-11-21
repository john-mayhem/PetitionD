// File: Core/Models/Template.cs
namespace PetidionD.Core.Models;

public class Template
{
    public int Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public TemplateType Type { get; set; }
    public string Content { get; set; } = string.Empty;
    public byte Category { get; set; }
    public short SortOrder { get; set; }

    public enum TemplateType : byte
    {
        Header = 1,
        Footer = 2,
        Body = 3
    }

    public enum Domain : byte
    {
        Public,
        Personal
    }
}