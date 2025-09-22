using BDKahoot.Application.Questions.Commands.CreateQuestion;
using BDKahoot.Domain.Enums;
using FluentValidation.TestHelper;

namespace BDKahoot.UnitTests.BDKahoot.Application.Questions.Commands.CreateQuestion
{
    public class CreateQuestionCommandValidatorTests
    {
        private readonly CreateQuestionCommandValidator _validator;

        public CreateQuestionCommandValidatorTests()
        {
            _validator = new CreateQuestionCommandValidator();
        }

        [Fact]
        public void Validate_ValidCommand_ShouldNotHaveValidationErrors()
        {
            // Arrange
            var command = new CreateQuestionCommand
            {
                GameId = "valid-game-id",
                UserNTID = "test@bosch.com",
                Title = "Valid Question Title",
                TimeLimitSeconds = 30,
                Type = QuestionType.SingleChoice
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void Validate_InvalidTitle_ShouldHaveValidationError(string title)
        {
            // Arrange
            var command = new CreateQuestionCommand
            {
                GameId = "valid-game-id",
                UserNTID = "test@bosch.com",
                Title = title,
                TimeLimitSeconds = 30,
                Type = QuestionType.SingleChoice
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Title);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-10)]
        public void Validate_InvalidTimeLimitSeconds_ShouldHaveValidationError(int timeLimitSeconds)
        {
            // Arrange
            var command = new CreateQuestionCommand
            {
                GameId = "valid-game-id",
                UserNTID = "test@bosch.com",
                Title = "Valid Question Title",
                TimeLimitSeconds = timeLimitSeconds,
                Type = QuestionType.SingleChoice
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.TimeLimitSeconds);
        }

        [Fact]
        public void Validate_TitleTooLong_ShouldHaveValidationError()
        {
            // Arrange
            var longTitle = new string('A', 201); // Assuming max length is 200
            var command = new CreateQuestionCommand
            {
                GameId = "valid-game-id",
                UserNTID = "test@bosch.com",
                Title = longTitle,
                TimeLimitSeconds = 30,
                Type = QuestionType.SingleChoice
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Title);
        }

        [Theory]
        [InlineData(QuestionType.SingleChoice)]
        [InlineData(QuestionType.MultipleChoice)]
        [InlineData(QuestionType.TrueFalse)]
        public void Validate_ValidQuestionTypes_ShouldNotHaveValidationError(QuestionType questionType)
        {
            // Arrange
            var command = new CreateQuestionCommand
            {
                GameId = "valid-game-id",
                UserNTID = "test@bosch.com",
                Title = "Valid Question Title",
                TimeLimitSeconds = 30,
                Type = questionType
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Type);
        }
    }
}
