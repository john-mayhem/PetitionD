// File: Core/Models/PetitionHistory.cs
namespace PetitionD.Core.Models;

public class PetitionHistory
{
    public string Actor { get; set; } = string.Empty;
    public DateTime Time { get; set; }
    public NC.PetitionLib.State ActionCode { get; set; }  // Use fully qualified name
}