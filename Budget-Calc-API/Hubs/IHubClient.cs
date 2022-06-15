namespace Budget;

public interface IHubClient
{
    Task BroadcastMessage();
}