namespace MainService.Services;

using MainService.Services.Interfaces;
using MainService.Models;
using System.Text;

public class ValidatorService : IValidatorService
{
    public string ValidateReservation(ReservationRequest reservation)
    {
        var errors = new StringBuilder();

        if (string.IsNullOrWhiteSpace(reservation.ClientTelephone) || !IsValidTelephone(reservation.ClientTelephone))
        {
            errors.AppendLine("ClientTelephone is missing, empty or invalid.");
        }

        if (reservation.DateOfReservation == default)
        {
            errors.AppendLine("DateOfReservation is missing or invalid.");
        }

        return errors.ToString();
    }

    private static bool IsValidTelephone(string telephone)
    {
        return long.TryParse(telephone, out _);
    }
}
