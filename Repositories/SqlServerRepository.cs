using Microsoft.Data.SqlClient;
using System.Data;

namespace SunatGreApi.Repositories
{
    public class SqlServerRepository : ISqlServerRepository
    {
        private readonly string _connectionString;

        public SqlServerRepository(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public async Task<(string nombreComercial, string codigoTela, string ordenCompra, string codigoProveedor)> GetDetalleBienAsync(string partida)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand("usp_gre_detalle_bien", connection);
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@cod_ordtra", partida);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return (
                    reader["nombre_comercial"]?.ToString() ?? string.Empty,
                    reader["cod_tela"]?.ToString() ?? string.Empty,
                    reader["oc"]?.ToString() ?? string.Empty,
                    reader["cod_proveedor"]?.ToString() ?? string.Empty
                );
            }

            return (string.Empty, string.Empty, string.Empty, string.Empty);
        }

        public async Task<(string codigoClaseOrden, string codigoCentroCosto)> GetCabeceraBienAsync(string ordenCompra)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand("usp_gre_cabecera_bien", connection);
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@orden_compra", ordenCompra);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return (
                    reader["clase_orden"]?.ToString() ?? string.Empty,
                    reader["centro_costo"]?.ToString() ?? string.Empty
                );
            }

            return (string.Empty, string.Empty);
        }

        public async Task<string> GetMovimientoPorClaseAsync(string codigoClaseOrden)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand("usp_gre_mov_x_clase", connection);
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@Cod_ClaOrdComp", codigoClaseOrden);

            var result = await command.ExecuteScalarAsync();
            return result?.ToString() ?? string.Empty;
        }
    }
}
