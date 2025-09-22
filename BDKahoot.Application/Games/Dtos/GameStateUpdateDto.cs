using BDKahoot.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BDKahoot.Application.Games.Dtos
{
    public class GameStateUpdateDto
    {
        public GameState CurrentState { get; set; }
        public GameState TargetState { get; set; }
    }
}
