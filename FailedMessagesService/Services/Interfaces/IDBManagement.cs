namespace FailedMessagesService.Services.Interfaces;

public interface IDBManagement
{
    Task SaveFailedMessageAsync(byte[] message);
}