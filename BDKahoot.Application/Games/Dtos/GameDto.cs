using BDKahoot.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace BDKahoot.Application.Games.Dtos
{
    public class GameDto
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string HostUserNTID { get; set; } = default!;
        public bool Deleted { get; set; }
        public DateTime? DeletedOn { get; set; }
        public DateTime? CreatedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }

        [Required, Range((int)GameState.Draft, (int)GameState.InActive)]
        public GameState State { get; set; }
    }
}
