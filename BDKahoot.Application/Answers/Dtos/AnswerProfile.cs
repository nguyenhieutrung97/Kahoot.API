using AutoMapper;
using BDKahoot.Application.Answers.Commands.CreateAnswer;
using BDKahoot.Application.Questions.Commands.CreateQuestion;
using BDKahoot.Application.Questions.Commands.UpdateQuestion;
using BDKahoot.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BDKahoot.Application.Answers.Dtos
{
    public class AnswerProfile : Profile
    {
        public AnswerProfile()
        {
            CreateMap<Answer, AnswerDto>()
                .ForMember(dest => dest.IsCorrect, opt => opt.MapFrom(src => src.IsCorrect))
                .ForMember(dest => dest.CreatedOn, opt => opt.MapFrom(src => src.CreatedOn))
                .ForMember(dest => dest.UpdatedOn, opt => opt.MapFrom(src => src.DeletedOn));

            CreateMap<CreateAnswerCommand, Answer>();
        }
    }
}
