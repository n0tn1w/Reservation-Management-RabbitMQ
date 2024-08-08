namespace SuccessfulMessagesService.Services.Interfaces;

public interface IDBManagement
{
    Task SaveSuccessfulMessageAsync(byte[] message);
}

