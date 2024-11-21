using NC.PetitionLib;

namespace PetidionD.Core.Models;

public class GmCharacter
{
    public int WorldId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public int AccountUid { get; set; }
    public string CharName { get; set; } = string.Empty;
    public int CharUid { get; set; }
    public Grade Grade { get; set; }
}