using SuccessfulMessagesService.Services.Interfaces;
using System.Data;
using System.Data.SqlClient;

namespace SuccessfulMessagesService.Services;

public class DBManagement : IDBManagement
{
    private readonly IConfiguration _configuration;

    public DBManagement(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SaveSuccessfulMessageAsync(byte[] message)
    {
        try
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("myPosMSSQL")))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand("SaveSuccessfulMessage", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.Add(new SqlParameter("@RawRequest", SqlDbType.VarBinary) { Value = message });

                    await command.ExecuteNonQueryAsync();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while ex SaveSuccessfulMessage to the database: {ex.Message}\nStack Trace: {ex.StackTrace}");
        }
    }
}
