namespace MainService.Models;

public class ReservationValidationDTO
{
    public byte[] RawRequest { get; set; }
    public DateTime DT { get; set; }
    public int ValidationResultCode { get; set; }
}