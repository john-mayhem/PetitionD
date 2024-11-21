// File: Core/Models/Category.cs
namespace PetitionD.Core.Models;

public class Category
{
    private static readonly Dictionary<int, string> Categories = new();

    public static bool IsValid(int categoryId) => Categories.ContainsKey(categoryId);

    public static void AddCategory(int id, string name)
    {
        Categories[id] = name;
    }

    public static void Clear()
    {
        Categories.Clear();
    }

    public static string GetName(int id)
    {
        return Categories.TryGetValue(id, out var name) ? name : string.Empty;
    }

    public static IReadOnlyDictionary<int, string> GetAll() => Categories;
}