namespace MainService.Services.Interfaces;

using MainService.Models;

public interface IDBManagement
{
    Task<long> SaveValidationResultAsync(ReservationValidationDTO validationResult);

    Task SaveValidationResultAsync(long insertedId);
}
