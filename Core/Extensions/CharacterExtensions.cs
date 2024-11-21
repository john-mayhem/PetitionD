// File: Core/Extensions/CharacterExtensions.cs
using PetitionD.Core.Models;

namespace PetitionD.Core.Extensions;

public static class CharacterExtensions
{
    public static GameCharacter ToGameCharacter(this GmCharacter gmChar)
    {
        return new GameCharacter
        {
            WorldId = gmChar.WorldId,
            AccountName = gmChar.AccountName,
            AccountUid = gmChar.AccountUid,
            CharName = gmChar.CharName,
            CharUid = gmChar.CharUid
        };
    }
}