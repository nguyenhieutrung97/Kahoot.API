using AutoMapper;
using BDKahoot.Application.Games.Commands.CreateGame;
using BDKahoot.Application.Games.Commands.UpdateGame;
using BDKahoot.Domain.Models;

namespace BDKahoot.Application.Games.Dtos
{
    public class GameProfile : Profile
    {
        public GameProfile()
        {
            CreateMap<Game, GameDto>()
                .ForMember(dest => dest.Deleted, opt => opt.MapFrom(src => src.Deleted))
                .ForMember(dest => dest.CreatedOn, opt => opt.MapFrom(src => src.CreatedOn))
                .ForMember(dest => dest.DeletedOn, opt => opt.MapFrom(src => src.DeletedOn));

            CreateMap<CreateGameCommand, Game>()
                .ForMember(dest => dest.HostUserNTID, opt => opt.MapFrom(src => src.UserNTID));

            CreateMap<UpdateGameCommand, Game>()
                .ForMember(dest => dest.UpdatedOn, opt => opt.MapFrom(_ => DateTime.UtcNow));
        }
    }
}
