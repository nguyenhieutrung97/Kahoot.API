using AutoMapper;
using BDKahoot.Application.Games.Commands.UpdateGame;
using BDKahoot.Application.Questions.Commands.CreateQuestion;
using BDKahoot.Application.Questions.Commands.UpdateQuestion;
using BDKahoot.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BDKahoot.Application.Questions.Dtos
{
    public class QuestionsProfile : Profile
    {
        public QuestionsProfile()
        {
            CreateMap<Question, QuestionDto>()
                .ForMember(dest => dest.Deleted, opt => opt.MapFrom(src => src.Deleted))
                .ForMember(dest => dest.CreatedOn, opt => opt.MapFrom(src => src.CreatedOn))
                .ForMember(dest => dest.DeletedOn, opt => opt.MapFrom(src => src.DeletedOn));

            CreateMap<CreateQuestionCommand, Question>();
            CreateMap<UpdateQuestionCommand, Question>();
        }
    }
}
