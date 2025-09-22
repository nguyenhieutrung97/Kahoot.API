using BDKahoot.Domain.Models;
using MediatR;
using System.ComponentModel.DataAnnotations;

namespace BDKahoot.Application.Games.Commands.CreateGame
{
    public class CreateGameCommand : IRequest<string>
    {
        public string Title { get; set; } = default!;
        public string Description { get; set; } = default!;
        public string UserNTID { get; set; } = string.Empty;
    }
}