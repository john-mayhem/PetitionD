using NC.PetitionLib;

namespace PetitionD.Core.Models;

public class GmCharacter : GameCharacter  // Make it inherit from GameCharacter
{
    public Grade Grade { get; set; }
    public int AssignCount { get; set; }

    public static GmCharacter Empty() => new()
    {
        AccountName = string.Empty,
        AccountUid = 0,
        CharName = string.Empty,
        CharUid = 0,
        WorldId = 0,
        Grade = Grade.GMS,
        AssignCount = 0
    };

    public GameCharacter ToGameCharacter() => new()
    {
        WorldId = WorldId,
        AccountName = AccountName,
        AccountUid = AccountUid,
        CharName = CharName,
        CharUid = CharUid
    };
}