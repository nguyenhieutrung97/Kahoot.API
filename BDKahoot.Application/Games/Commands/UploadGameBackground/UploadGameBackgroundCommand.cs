using MediatR;
using Microsoft.AspNetCore.Http;

namespace BDKahoot.Application.Games.Commands.UploadGameBackground
{
    public class UploadGameBackgroundCommand : IRequest
    {
        public string Id { get; set; } = string.Empty;
        public string UserNTID { get; set; } = string.Empty;
        public required IFormFile File { get; set; }
    }
}
