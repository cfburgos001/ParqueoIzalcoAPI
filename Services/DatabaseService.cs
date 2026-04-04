using System.Data;
using Microsoft.Data.SqlClient;

namespace ParqueoIzalcoAPI.Services
{
    public interface IDatabaseService
    {
        Task<SqlConnection> GetConnectionAsync();
        Task<T?> ExecuteScalarAsync<T>(string sql, SqlParameter[]? parameters = null);
        Task<int> ExecuteNonQueryAsync(string sql, SqlParameter[]? parameters = null);
        Task<DataTable> ExecuteQueryAsync(string sql, SqlParameter[]? parameters = null);
        Task<bool> TestConnectionAsync();
    }

    public class DatabaseService : IDatabaseService
    {
        private readonly string _connectionString;
        private readonly ILogger<DatabaseService> _logger;

        public DatabaseService(IConfiguration configuration, ILogger<DatabaseService> logger)
        {
            _connectionString = configuration.GetConnectionString("DataparkConnection")
                ?? throw new InvalidOperationException("Connection string not found");
            _logger = logger;
        }

        public async Task<SqlConnection> GetConnectionAsync()
        {
            var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            return connection;
        }

        public async Task<T?> ExecuteScalarAsync<T>(string sql, SqlParameter[]? parameters = null)
        {
            try
            {
                using var connection = await GetConnectionAsync();
                using var command = new SqlCommand(sql, connection);

                if (parameters != null)
                {
                    command.Parameters.AddRange(parameters);
                }

                var result = await command.ExecuteScalarAsync();
                return result == null || result == DBNull.Value ? default : (T)result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing scalar query: {Query}", sql);
                throw;
            }
        }

        public async Task<int> ExecuteNonQueryAsync(string sql, SqlParameter[]? parameters = null)
        {
            try
            {
                using var connection = await GetConnectionAsync();
                using var command = new SqlCommand(sql, connection);

                if (parameters != null)
                {
                    command.Parameters.AddRange(parameters);
                }

                return await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing non-query: {Query}", sql);
                throw;
            }
        }

        public async Task<DataTable> ExecuteQueryAsync(string sql, SqlParameter[]? parameters = null)
        {
            try
            {
                using var connection = await GetConnectionAsync();
                using var command = new SqlCommand(sql, connection);

                if (parameters != null)
                {
                    command.Parameters.AddRange(parameters);
                }

                using var adapter = new SqlDataAdapter(command);
                var dataTable = new DataTable();
                adapter.Fill(dataTable);
                return dataTable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing query: {Query}", sql);
                throw;
            }
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                using var connection = await GetConnectionAsync();
                return connection.State == ConnectionState.Open;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database connection test failed");
                return false;
            }
        }
    }
}