namespace SuccessfulMessagesService.Services.Interfaces;

public interface ISenderResponseSuccessReservations
{
    Task SendForward(byte[] message);

}
