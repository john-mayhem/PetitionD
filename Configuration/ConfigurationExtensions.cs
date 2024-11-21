namespace PetitionD.Configuration;

public static class ConfigurationExtensions
{
    public static void ValidateConfiguration(this AppSettings settings)
    {
        var errors = new List<string>();

        if (string.IsNullOrEmpty(settings.DatabaseConnString))
            errors.Add("DatabaseConnString is required");

        if (settings.DatabaseConnCount <= 0)
            errors.Add("DatabaseConnCount must be greater than 0");

        if (settings.MaxAssignmentPerGm <= 0)
            errors.Add("MaxAssignmentPerGm must be greater than 0");

        if (settings.GmServicePort <= 0)
            errors.Add("GmServicePort must be greater than 0");

        if (settings.WorldServicePort <= 0)
            errors.Add("WorldServicePort must be greater than 0");

        if (errors.Any())
            throw new InvalidOperationException(
                $"Configuration validation failed:{Environment.NewLine}" +
                string.Join(Environment.NewLine, errors));
    }
}