using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BDKahoot.Application.Games.Commands.DeleteGame
{
    public class DeleteGameCommand() : IRequest
    {
        public string Id { get; set; } = default!;
        public string UserNTID { get; set; } = default!;
    }
}
