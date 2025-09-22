using BDKahoot.API.Hubs.Models.Players;

namespace BDKahoot.API.Hubs.Models.Responses
{
    /// <summary>
    /// Personalized final results response for individual players
    /// </summary>
    public class PersonalizedFinalResultsResponse : FinalResultsResponse
    {
        /// <summary>
        /// Player's individual rank in the game
        /// </summary>
        public int YourRank { get; set; }
        
        /// <summary>
        /// Player's final score
        /// </summary>
        public int YourScore { get; set; }
        
        /// <summary>
        /// Player's progress as "correct/total" format
        /// </summary>
        public string YourProgress { get; set; } = string.Empty;
        
        /// <summary>
        /// Whether the player finished in top 3
        /// </summary>
        public bool IsInTopThree { get; set; }
    }
}