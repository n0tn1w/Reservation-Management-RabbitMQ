namespace FailedMessagesService.Services;

using FailedMessagesService.Services.Interfaces;
using System.Data;
using Npgsql;

public class DBManagement : IDBManagement
{
    private readonly IConfiguration _configuration;

    public DBManagement(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SaveFailedMessageAsync(byte[] message)
    {
        try
        {
            using (var connection = new NpgsqlConnection(_configuration.GetConnectionString("myPosPostgreSQL")))
            {
                await connection.OpenAsync();

                using (var command = new NpgsqlCommand("SELECT public.savefailedmessage(@RawRequest)", connection))
                {
                    command.CommandType = CommandType.Text;
                    command.Parameters.Add(new NpgsqlParameter("RawRequest", NpgsqlTypes.NpgsqlDbType.Text)
                    {
                        Value = message
                    });

                    await command.ExecuteNonQueryAsync();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while ex savefailedmessage to the database: {ex.Message}\nStack Trace: {ex.StackTrace}");
        }
    }
}
