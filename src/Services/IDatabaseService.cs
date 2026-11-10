using BProMax.Configuration;

namespace BProMax.Services;

public interface IDatabaseService
{
    Task<bool> TestConnectionAsync();
    Task<int> ExecuteNonQueryAsync(string sql, params Npgsql.NpgsqlParameter[] parameters);
    Task<Npgsql.NpgsqlDataReader> ExecuteReaderAsync(string sql, params Npgsql.NpgsqlParameter[] parameters);
    Task<T?> QuerySingleAsync<T>(string sql, Func<Npgsql.NpgsqlDataReader, T> map, params Npgsql.NpgsqlParameter[] parameters);
    Task<List<T>> QueryListAsync<T>(string sql, Func<Npgsql.NpgsqlDataReader, T> map, params Npgsql.NpgsqlParameter[] parameters);
    Npgsql.NpgsqlConnection CreateConnection();
}
