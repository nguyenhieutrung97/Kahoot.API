namespace BDKahoot.Domain.Enums
{
    public enum GameSessionState
    {
        Lobby = 0,          // Players can join, waiting for host to start
        InProgress = 1,     // Game is running, no new players can join
        WaitingForHost = 2, // All players answered, waiting for host to proceed
        Completed = 3,      // Game finished
        Aborted = 4       // Game was cancelled by host disconnected
    }
}
