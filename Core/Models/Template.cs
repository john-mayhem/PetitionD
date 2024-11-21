// File: Core/Models/Template.cs
namespace PetitionD.Core.Models;

public class Template
{
    public int Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public TemplateType Type { get; set; }
    public string Content { get; set; } = string.Empty;
    public int Category { get; set; }
    public int SortOrder { get; set; }
    public int AccountUid { get; set; }  // Owner's account ID

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

    public void Serialize(NC.ToolNet.Net.Packer packer)
    {
        packer.AddInt32(Code);
        packer.AddString(Name);
        packer.AddUInt8((byte)Type);
        packer.AddString(Content);
        packer.AddInt32(Category);
        packer.AddInt32(SortOrder);
    }
}