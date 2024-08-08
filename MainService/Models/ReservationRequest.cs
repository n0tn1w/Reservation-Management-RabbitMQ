namespace MainService.Models;

public class ReservationRequest
{
    public string ClientName { get; set; }
    public string ClientTelephone { get; set; }
    public int NumberOfReservedTable { get; set; }
    public DateTime DateOfReservation { get; set; }
}
