namespace BDKahoot.API.Hubs.Models.Players
{
    /// <summary>
    /// Result of player join operation
    /// </summary>
    public class PlayerJoinResult
    {
        public bool IsSuccess { get; private set; }
        public bool IsReconnection { get; private set; }

        private PlayerJoinResult(bool isSuccess, bool isReconnection = false)
        {
            IsSuccess = isSuccess;
            IsReconnection = isReconnection;
        }

        public static PlayerJoinResult Success(bool isReconnection = false) => new(true, isReconnection);
        public static PlayerJoinResult Failed() => new(false);
    }
}
