namespace MainService.Services;

using MainService.Services.Interfaces;
using System.Data.SqlClient;
using MainService.Models;
using System.Data;

public class DBManagement : IDBManagement
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<DBManagement> _logger;

    public DBManagement(IConfiguration configuration, ILogger<DBManagement> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }
    public async Task<long> SaveValidationResultAsync(ReservationValidationDTO validationResult)
    {
        try
        {

            using (var connection = new SqlConnection(_configuration.GetConnectionString("myPosMSSQL")))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand("SaveValidationResultReturingId", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.Add(new SqlParameter("@RawRequest", SqlDbType.VarBinary) { Value = validationResult.RawRequest });
                    command.Parameters.Add(new SqlParameter("@DT", SqlDbType.DateTime2) { Value = validationResult.DT });
                    command.Parameters.Add(new SqlParameter("@ValidationResultCode", SqlDbType.Int) { Value = validationResult.ValidationResultCode });

                    SqlParameter outputIdParam = new SqlParameter("@InsertedID", SqlDbType.BigInt)
                    {
                        Direction = ParameterDirection.Output
                    };
                    command.Parameters.Add(outputIdParam);

                    await command.ExecuteNonQueryAsync();
                    long insertedId = (long)outputIdParam.Value;

                    return insertedId;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"An error occurred while ex SaveResponseResult to the database: {ex.Message}\nStack Trace: {ex.StackTrace}");
            return -1;
        }
    }

    public async Task SaveValidationResultAsync(long insertedId)
    {
        try
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("myPosMSSQL")))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand("SaveResponseResult", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.Add(new SqlParameter("@InsertedId", SqlDbType.BigInt) { Value = insertedId });

                    await command.ExecuteNonQueryAsync();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"An error occurred while ex SaveResponseResult to the database: {ex.Message}\nStack Trace: {ex.StackTrace}");
        }
    }
}
