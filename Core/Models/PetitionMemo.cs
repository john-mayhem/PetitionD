// File: Core/Models/PetitionMemo.cs
namespace PetidionD.Core.Models;

public class PetitionMemo
{
    public string Writer { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime Time { get; set; }
}