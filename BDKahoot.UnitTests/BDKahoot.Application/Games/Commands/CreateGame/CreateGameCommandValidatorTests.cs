using BDKahoot.Application.Games.Commands.CreateGame;
using FluentValidation.TestHelper;

namespace BDKahoot.UnitTests.BDKahoot.Application.Games.Commands.CreateGame
{
    public class CreateGameCommandValidatorTests
    {
        private readonly CreateGameCommandValidator _validator = new();

        [Fact]
        public void Should_Have_Error_When_Title_Is_Empty()
        {
            var model = new CreateGameCommand { Title = "" };

            var result = _validator.TestValidate(model);

            result.ShouldHaveValidationErrorFor(x => x.Title)
                  .WithErrorMessage("Game title is required.");
        }

        [Fact]
        public void Should_Have_Error_When_Title_Is_Too_Long()
        {
            var model = new CreateGameCommand { Title = new string('A', 101) };

            var result = _validator.TestValidate(model);

            result.ShouldHaveValidationErrorFor(x => x.Title)
                  .WithErrorMessage("Game title cannot exceed 100 characters.");
        }

        [Fact]
        public void Should_Have_Error_When_Description_Is_Too_Long()
        {
            var model = new CreateGameCommand { Description = new string('B', 501) };

            var result = _validator.TestValidate(model);

            result.ShouldHaveValidationErrorFor(x => x.Description)
                  .WithErrorMessage("Game description cannot exceed 500 characters.");
        }

        [Fact]
        public void Should_Not_Have_Errors_When_Command_Is_Valid()
        {
            var model = new CreateGameCommand
            {
                Title = "Valid Game",
                Description = "Some description",
                UserNTID = "user123"
            };

            var result = _validator.TestValidate(model);

            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}
