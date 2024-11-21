namespace PetitionD.Core.Interfaces;

public interface IRepositoryBase
{
    Task<T?> ExecuteScalarAsync<T>(string storedProc, object parameters);
    Task<IEnumerable<T>> ExecuteListAsync<T>(string storedProc, object parameters);
    Task ExecuteNonQueryAsync(string storedProc, object parameters);
}