// File: Core/Extensions/PacketExtensions.cs
namespace PetitionD.Core.Extensions;

public static class PacketExtensions
{
    public static string DecodeUnicodeString(this byte[] data, int startIndex, int length)
    {
        return System.Text.Encoding.Unicode.GetString(data, startIndex, length);
    }

    public static int GetCharacterServerIndex(int charId)
    {
        return charId >> 8; // Reverse the bit shift
    }

    public static int GetWorldCharacterId(int charId)
    {
        return charId << 8; // Apply the bit shift
    }
}