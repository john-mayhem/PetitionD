// File: Core/Models/PetitionHistory.cs
namespace PetidionD.Core.Models;

public class PetitionHistory
{
    public string Actor { get; set; } = string.Empty;
    public DateTime Time { get; set; }
    public State ActionCode { get; set; }
}