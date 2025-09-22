using FluentValidation;
using BDKahoot.Application.Games.Commands.UploadGameBackground;
using System.IO;
using System.Linq;

namespace BDKahoot.Application.Games.Commands.UploadGameBackground
{
    public class UploadGameBackgroundCommandValidator : AbstractValidator<UploadGameBackgroundCommand>
    {
        private readonly string[] allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };

        public UploadGameBackgroundCommandValidator()
        {
            RuleFor(x => x.File)
                .NotNull().WithMessage("File cannot be null.")
                .DependentRules(() =>
                {
                    RuleFor(x => x.File.Length)
                        .GreaterThan(0).WithMessage("File is empty.");
                    RuleFor(x => x.File.FileName)
                        .Must(fileName =>
                        {
                            var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
                            return allowedExtensions.Contains(extension);
                        })
                        .WithMessage("Invalid file format. Only image files are allowed.");
                });
        }
    }
}