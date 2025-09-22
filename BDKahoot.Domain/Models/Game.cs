using BDKahoot.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace BDKahoot.Domain.Models
{
    public class Game : BaseModel
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string HostUserNTID { get; set; } = default!;
        [Required, Range((int)GameState.Draft, (int)GameState.InActive)]
        public GameState State { get; set; }
    }
}
