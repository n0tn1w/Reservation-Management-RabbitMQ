namespace SuccessfulMessagesService.Models;

public class ReservationValidationRequest {
    public long Id { get; set; }

    public byte[] RawRequest { get; set; }
}