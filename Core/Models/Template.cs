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

    public static class Operations
    {
        public static PetitionErrorCode Delete(int accountUid, int code)
        {
            // Implementation
            return PetitionErrorCode.Success;
        }

        public static PetitionErrorCode Download(
            int gmAccountUid,
            string gmAccount,
            int code,
            out int resultCode)
        {
            resultCode = code;
            return PetitionErrorCode.Success;
        }

        public static PetitionErrorCode Update(
            int gmAccountUid,
            string gmAccount,
            int code,
            string name,
            TemplateType type,
            string content,
            int category,
            out int resultCode)
        {
            resultCode = code;
            return PetitionErrorCode.Success;
        }

        public static PetitionErrorCode UpdateOrder(
            int gmAccountUid,
            int code,
            int offset)
        {
            return PetitionErrorCode.Success;
        }
    }

    public void Serialize(NC.ToolNet.Networking.Protocol.Packer packer)
    {
        packer.AddInt32(Code);
        packer.AddString(Name);
        packer.AddUInt8((byte)Type);
        packer.AddString(Content);
        packer.AddInt32(Category);
        packer.AddInt32(SortOrder);
    }

    public static IEnumerable<Template> GetTemplateList(int gmAccountUid)
    {
        // Implementation placeholder
        return [];
    }

    public static PetitionErrorCode Delete(int gmAccountUid, int code)
    {
        // Implementation placeholder
        return PetitionErrorCode.Success;
    }

    public static PetitionErrorCode Download(
        int gmAccountUid,
        string gmAccount,
        int code,
        out int resultCode)
    {
        resultCode = code;
        // Implementation placeholder
        return PetitionErrorCode.Success;
    }

    public static PetitionErrorCode UpdateOrder(
        int gmAccountUid,
        int code,
        int offset)
    {
        // Implementation placeholder
        return PetitionErrorCode.Success;
    }
}