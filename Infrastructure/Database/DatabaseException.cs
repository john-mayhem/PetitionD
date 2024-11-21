// File: Infrastructure/Database/DatabaseException.cs
namespace PetitionD.Infrastructure.Database;

public class DatabaseException : Exception
{
    public DatabaseException(string message) : base(message)
    {
    }

    public DatabaseException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
