namespace MainService.Services.Interfaces;

using MainService.Models;

public interface ISenderHandledReservations
{
    Task SendForward(ReservationValidationDTO validationResult, long inserted_id);
}
