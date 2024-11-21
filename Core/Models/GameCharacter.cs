namespace PetidionD.Core.Models;

public class GameCharacter
{
    public int WorldId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public int AccountUid { get; set; }
    public string CharName { get; set; } = string.Empty;
    public int CharUid { get; set; }
}