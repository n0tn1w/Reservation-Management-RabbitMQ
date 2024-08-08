namespace MainService.Services.Interfaces;

using MainService.Models;

public interface IValidatorService
{
    string ValidateReservation(ReservationRequest reservation);
}
