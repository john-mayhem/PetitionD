namespace PetitionD.Core.Models;

public class GameCharacter
{
    public int WorldId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public int AccountUid { get; set; }
    public string CharName { get; set; } = string.Empty;
    public int CharUid { get; set; }

    public static GameCharacter Empty() => new()
    {
        AccountName = string.Empty,
        AccountUid = 0,
        CharName = string.Empty,
        CharUid = 0,
        WorldId = 0
    };
}