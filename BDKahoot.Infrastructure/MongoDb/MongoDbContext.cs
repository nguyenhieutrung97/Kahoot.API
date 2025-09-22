using BDKahoot.Domain.Enums;
using BDKahoot.Domain.Models;
using Microsoft.Extensions.Options;
using Mongo2Go;
using MongoDB.Driver;
using MongoDB.Bson;

namespace BDKahoot.Infrastructure.MongoDb
{
    public class MongoDbContext
    {
        private readonly IMongoDatabase _database;
        private MongoDbRunner? _runner;
        private readonly MongoDbSettings _settings;

        public MongoDbContext(IOptions<MongoDbSettings> options)
        {
            _settings = options.Value;

            if (_settings.UseInMemory)
            {
                _runner = MongoDbRunner.Start();
                IMongoClient _client = new MongoClient(_runner.ConnectionString);
                _database = _client.GetDatabase(_settings.DatabaseName);

                // Seed data for in-memory database
                SeedData().Wait();
            }
            else
            {
                var client = new MongoClient(_settings.ConnectionString);
                _database = client.GetDatabase(_settings.DatabaseName);
            }
        }

        public IMongoCollection<T> GetCollection<T>(string name) => _database.GetCollection<T>(name);

        public void Dispose()
        {
            _runner?.Dispose();
        }

        private async Task SeedData()
        {
            // Seed Games
            var gamesCollection = GetCollection<Game>("Games");
            var questionsCollection = GetCollection<Question>("Questions");
            var answersCollection = GetCollection<Answer>("Answers");

            // Check if collection already has data
            if ((await gamesCollection.CountDocumentsAsync(FilterDefinition<Game>.Empty)) == 0)
            {
                // Pre-generate ObjectIds for consistent referencing
                var techGameId = "675c1f85e7b5a4f12c8d9e10";
                //var generalGameId = "675c1f85e7b5a4f12c8d9e11";
                //var legacyGameId = "675c1f85e7b5a4f12c8d9e12";

                var sampleGames = new List<Game>
                {
                    new Game
                    {
                        Id = techGameId,
                        Title = "🚀 Tech Knowledge Challenge",
                        Description = "Test your programming and technology skills with this comprehensive quiz!",
                        CreatedOn = DateTime.UtcNow.AddDays(-5),
                        UpdatedOn = DateTime.UtcNow.AddDays(-1),
                        HostUserNTID = "dto6hc@bosch.com",
                        State = GameState.Draft
                    },
                    //new Game
                    //{
                    //    Id = generalGameId,
                    //    Title = "🌍 General Knowledge Trivia",
                    //    Description = "Challenge yourself with questions from various topics including science, history, and fun facts!",
                    //    CreatedOn = DateTime.UtcNow.AddDays(-3),
                    //    UpdatedOn = DateTime.UtcNow,
                    //    HostUserNTID = "dto6hc@bosch.com",
                    //    State = GameState.Draft
                    //},
                    //// Legacy games for backward compatibility
                    //new Game
                    //{
                    //    Id = legacyGameId,
                    //    Title = "💻 C# Programming Quiz",
                    //    Description = "Test your knowledge of C# programming",
                    //    CreatedOn = DateTime.UtcNow.AddDays(-10),
                    //    UpdatedOn = DateTime.UtcNow.AddDays(-5),
                    //    HostUserNTID = "dto6hc@bosch.com",
                    //    State = GameState.Draft
                    //}
                };

                await gamesCollection.InsertManyAsync(sampleGames);

                // Pre-generate Question ObjectIds
                var techQ1Id = "675c1f85e7b5a4f12c8d9e20";
                var techQ2Id = "675c1f85e7b5a4f12c8d9e21";
                var techQ3Id = "675c1f85e7b5a4f12c8d9e22";
                //var techQ4Id = "675c1f85e7b5a4f12c8d9e23";
                //var techQ5Id = "675c1f85e7b5a4f12c8d9e24";

                //var generalQ1Id = "675c1f85e7b5a4f12c8d9e30";
                //var generalQ2Id = "675c1f85e7b5a4f12c8d9e31";
                //var generalQ3Id = "675c1f85e7b5a4f12c8d9e32";
                //var generalQ4Id = "675c1f85e7b5a4f12c8d9e33";
                //var generalQ5Id = "675c1f85e7b5a4f12c8d9e34";

                //var legacyQ1Id = "675c1f85e7b5a4f12c8d9e40";

                // Create questions with reference to actual game IDs
                var sampleQuestions = new List<Question>
                {
                    // Tech Knowledge Challenge Game Questions (5 questions)
                    new Question
                    {
                        Id = techQ1Id,
                        GameId = techGameId,
                        Title = "Which programming language is known as the 'language of the web'?",
                        Type = QuestionType.SingleChoice,
                        TimeLimitSeconds = 5,
                        CreatedOn = DateTime.UtcNow.AddDays(-2),
                        UpdatedOn = DateTime.UtcNow.AddDays(-1)
                    },
                    new Question
                    {
                        Id = techQ2Id,
                        GameId = techGameId,
                        Title = "Which of the following are cloud computing platforms? (Select all that apply)",
                        Type = QuestionType.MultipleChoice,
                        TimeLimitSeconds = 5,
                        CreatedOn = DateTime.UtcNow.AddDays(-2),
                        UpdatedOn = DateTime.UtcNow.AddDays(-1)
                    },
                    new Question
                    {
                        Id = techQ3Id,
                        GameId = techGameId,
                        Title = "Git is a distributed version control system.",
                        Type = QuestionType.TrueFalse,
                        TimeLimitSeconds = 5,
                        CreatedOn = DateTime.UtcNow.AddDays(-2),
                        UpdatedOn = DateTime.UtcNow.AddDays(-1)
                    },
                    //new Question
                    //{
                    //    Id = techQ4Id,
                    //    GameId = techGameId,
                    //    Title = "What does API stand for?",
                    //    Type = QuestionType.SingleChoice,
                    //    TimeLimitSeconds = 15,
                    //    CreatedOn = DateTime.UtcNow.AddDays(-2),
                    //    UpdatedOn = DateTime.UtcNow.AddDays(-1)
                    //},
                    //new Question
                    //{
                    //    Id = techQ5Id,
                    //    GameId = techGameId,
                    //    Title = "Which of these are NoSQL databases? (Select all that apply)",
                    //    Type = QuestionType.MultipleChoice,
                    //    TimeLimitSeconds = 25,
                    //    CreatedOn = DateTime.UtcNow.AddDays(-2),
                    //    UpdatedOn = DateTime.UtcNow.AddDays(-1)
                    //},

                    //// General Knowledge Trivia Game Questions (5 questions)
                    //new Question
                    //{
                    //    Id = generalQ1Id,
                    //    GameId = generalGameId,
                    //    Title = "What is the capital city of Australia?",
                    //    Type = QuestionType.SingleChoice,
                    //    TimeLimitSeconds = 15,
                    //    CreatedOn = DateTime.UtcNow.AddDays(-1),
                    //    UpdatedOn = DateTime.UtcNow
                    //},
                    //new Question
                    //{
                    //    Id = generalQ2Id,
                    //    GameId = generalGameId,
                    //    Title = "The Great Wall of China is visible from space with the naked eye.",
                    //    Type = QuestionType.TrueFalse,
                    //    TimeLimitSeconds = 12,
                    //    CreatedOn = DateTime.UtcNow.AddDays(-1),
                    //    UpdatedOn = DateTime.UtcNow
                    //},
                    //new Question
                    //{
                    //    Id = generalQ3Id,
                    //    GameId = generalGameId,
                    //    Title = "Which of these are renewable energy sources? (Select all that apply)",
                    //    Type = QuestionType.MultipleChoice,
                    //    TimeLimitSeconds = 20,
                    //    CreatedOn = DateTime.UtcNow.AddDays(-1),
                    //    UpdatedOn = DateTime.UtcNow
                    //},
                    //new Question
                    //{
                    //    Id = generalQ4Id,
                    //    GameId = generalGameId,
                    //    Title = "Who painted the famous artwork 'The Starry Night'?",
                    //    Type = QuestionType.SingleChoice,
                    //    TimeLimitSeconds = 18,
                    //    CreatedOn = DateTime.UtcNow.AddDays(-1),
                    //    UpdatedOn = DateTime.UtcNow
                    //},
                    //new Question
                    //{
                    //    Id = generalQ5Id,
                    //    GameId = generalGameId,
                    //    Title = "Honey never spoils and can last indefinitely if stored properly.",
                    //    Type = QuestionType.TrueFalse,
                    //    TimeLimitSeconds = 10,
                    //    CreatedOn = DateTime.UtcNow.AddDays(-1),
                    //    UpdatedOn = DateTime.UtcNow
                    //},

                    //// Legacy question for backward compatibility
                    //new Question
                    //{
                    //    Id = legacyQ1Id,
                    //    GameId = legacyGameId,
                    //    Title = "What version of .NET introduced top-level statements?",
                    //    Type = QuestionType.SingleChoice,
                    //    TimeLimitSeconds = 10
                    //}
                };

                await questionsCollection.InsertManyAsync(sampleQuestions);

                // Create answers with reference to actual question IDs
                var sampleAnswers = new List<Answer>
                {
                    // Tech Q1: Which programming language is known as the 'language of the web'? (Single Choice)
                    new Answer
                    {
                        Id = "675c1f85e7b5a4f12c8d9e50",
                        QuestionId = techQ1Id,
                        Title = "JavaScript",
                        IsCorrect = true,
                        CreatedOn = DateTime.UtcNow.AddDays(-1),
                        UpdatedOn = DateTime.UtcNow
                    },
                    new Answer
                    {
                        Id = "675c1f85e7b5a4f12c8d9e51",
                        QuestionId = techQ1Id,
                        Title = "Python",
                        IsCorrect = false,
                        CreatedOn = DateTime.UtcNow.AddDays(-1),
                        UpdatedOn = DateTime.UtcNow
                    },
                    new Answer
                    {
                        Id = "675c1f85e7b5a4f12c8d9e52",
                        QuestionId = techQ1Id,
                        Title = "Java",
                        IsCorrect = false,
                        CreatedOn = DateTime.UtcNow.AddDays(-1),
                        UpdatedOn = DateTime.UtcNow
                    },
                    new Answer
                    {
                        Id = "675c1f85e7b5a4f12c8d9e53",
                        QuestionId = techQ1Id,
                        Title = "C#",
                        IsCorrect = false,
                        CreatedOn = DateTime.UtcNow.AddDays(-1),
                        UpdatedOn = DateTime.UtcNow
                    },

                    // Tech Q2: Cloud computing platforms (Multiple Choice)
                    new Answer
                    {
                        Id = "675c1f85e7b5a4f12c8d9e54",
                        QuestionId = techQ2Id,
                        Title = "Amazon Web Services (AWS)",
                        IsCorrect = true,
                        CreatedOn = DateTime.UtcNow.AddDays(-1),
                        UpdatedOn = DateTime.UtcNow
                    },
                    new Answer
                    {
                        Id = "675c1f85e7b5a4f12c8d9e55",
                        QuestionId = techQ2Id,
                        Title = "Microsoft Azure",
                        IsCorrect = true,
                        CreatedOn = DateTime.UtcNow.AddDays(-1),
                        UpdatedOn = DateTime.UtcNow
                    },
                    new Answer
                    {
                        Id = "675c1f85e7b5a4f12c8d9e56",
                        QuestionId = techQ2Id,
                        Title = "Google Cloud Platform",
                        IsCorrect = true,
                        CreatedOn = DateTime.UtcNow.AddDays(-1),
                        UpdatedOn = DateTime.UtcNow
                    },
                    new Answer
                    {
                        Id = "675c1f85e7b5a4f12c8d9e57",
                        QuestionId = techQ2Id,
                        Title = "Adobe Photoshop",
                        IsCorrect = false,
                        CreatedOn = DateTime.UtcNow.AddDays(-1),
                        UpdatedOn = DateTime.UtcNow
                    },

                    // Tech Q3: Git is distributed (True/False)
                    new Answer
                    {
                        Id = "675c1f85e7b5a4f12c8d9e58",
                        QuestionId = techQ3Id,
                        Title = "True",
                        IsCorrect = true,
                        CreatedOn = DateTime.UtcNow.AddDays(-1),
                        UpdatedOn = DateTime.UtcNow
                    },
                    new Answer
                    {
                        Id = "675c1f85e7b5a4f12c8d9e59",
                        QuestionId = techQ3Id,
                        Title = "False",
                        IsCorrect = false,
                        CreatedOn = DateTime.UtcNow.AddDays(-1),
                        UpdatedOn = DateTime.UtcNow
                    },

                    //// Tech Q4: What does API stand for? (Single Choice)
                    //new Answer
                    //{
                    //    Id = "675c1f85e7b5a4f12c8d9e60",
                    //    QuestionId = techQ4Id,
                    //    Title = "Application Programming Interface",
                    //    IsCorrect = true,
                    //    CreatedOn = DateTime.UtcNow.AddDays(-1),
                    //    UpdatedOn = DateTime.UtcNow
                    //},
                    //new Answer
                    //{
                    //    Id = "675c1f85e7b5a4f12c8d9e61",
                    //    QuestionId = techQ4Id,
                    //    Title = "Advanced Programming Interface",
                    //    IsCorrect = false,
                    //    CreatedOn = DateTime.UtcNow.AddDays(-1),
                    //    UpdatedOn = DateTime.UtcNow
                    //},
                    //new Answer
                    //{
                    //    Id = "675c1f85e7b5a4f12c8d9e62",
                    //    QuestionId = techQ4Id,
                    //    Title = "Automated Program Integration",
                    //    IsCorrect = false,
                    //    CreatedOn = DateTime.UtcNow.AddDays(-1),
                    //    UpdatedOn = DateTime.UtcNow
                    //},
                    //new Answer
                    //{
                    //    Id = "675c1f85e7b5a4f12c8d9e63",
                    //    QuestionId = techQ4Id,
                    //    Title = "Application Process Interface",
                    //    IsCorrect = false,
                    //    CreatedOn = DateTime.UtcNow.AddDays(-1),
                    //    UpdatedOn = DateTime.UtcNow
                    //},

                    //// Tech Q5: NoSQL databases (Multiple Choice)
                    //new Answer
                    //{
                    //    Id = "675c1f85e7b5a4f12c8d9e64",
                    //    QuestionId = techQ5Id,
                    //    Title = "MongoDB",
                    //    IsCorrect = true,
                    //    CreatedOn = DateTime.UtcNow.AddDays(-1),
                    //    UpdatedOn = DateTime.UtcNow
                    //},
                    //new Answer
                    //{
                    //    Id = "675c1f85e7b5a4f12c8d9e65",
                    //    QuestionId = techQ5Id,
                    //    Title = "Redis",
                    //    IsCorrect = true,
                    //    CreatedOn = DateTime.UtcNow.AddDays(-1),
                    //    UpdatedOn = DateTime.UtcNow
                    //},
                    //new Answer
                    //{
                    //    Id = "675c1f85e7b5a4f12c8d9e66",
                    //    QuestionId = techQ5Id,
                    //    Title = "MySQL",
                    //    IsCorrect = false,
                    //    CreatedOn = DateTime.UtcNow.AddDays(-1),
                    //    UpdatedOn = DateTime.UtcNow
                    //},
                    //new Answer
                    //{
                    //    Id = "675c1f85e7b5a4f12c8d9e67",
                    //    QuestionId = techQ5Id,
                    //    Title = "Cassandra",
                    //    IsCorrect = true,
                    //    CreatedOn = DateTime.UtcNow.AddDays(-1),
                    //    UpdatedOn = DateTime.UtcNow
                    //},

                    //// General Q1: Capital of Australia (Single Choice)
                    //new Answer
                    //{
                    //    Id = "675c1f85e7b5a4f12c8d9e70",
                    //    QuestionId = generalQ1Id,
                    //    Title = "Canberra",
                    //    IsCorrect = true,
                    //    CreatedOn = DateTime.UtcNow,
                    //    UpdatedOn = DateTime.UtcNow
                    //},
                    //new Answer
                    //{
                    //    Id = "675c1f85e7b5a4f12c8d9e71",
                    //    QuestionId = generalQ1Id,
                    //    Title = "Sydney",
                    //    IsCorrect = false,
                    //    CreatedOn = DateTime.UtcNow,
                    //    UpdatedOn = DateTime.UtcNow
                    //},
                    //new Answer
                    //{
                    //    Id = "675c1f85e7b5a4f12c8d9e72",
                    //    QuestionId = generalQ1Id,
                    //    Title = "Melbourne",
                    //    IsCorrect = false,
                    //    CreatedOn = DateTime.UtcNow,
                    //    UpdatedOn = DateTime.UtcNow
                    //},
                    //new Answer
                    //{
                    //    Id = "675c1f85e7b5a4f12c8d9e73",
                    //    QuestionId = generalQ1Id,
                    //    Title = "Perth",
                    //    IsCorrect = false,
                    //    CreatedOn = DateTime.UtcNow,
                    //    UpdatedOn = DateTime.UtcNow
                    //},

                    //// General Q2: Great Wall visible from space (True/False)
                    //new Answer
                    //{
                    //    Id = "675c1f85e7b5a4f12c8d9e74",
                    //    QuestionId = generalQ2Id,
                    //    Title = "True",
                    //    IsCorrect = false,
                    //    CreatedOn = DateTime.UtcNow,
                    //    UpdatedOn = DateTime.UtcNow
                    //},
                    //new Answer
                    //{
                    //    Id = "675c1f85e7b5a4f12c8d9e75",
                    //    QuestionId = generalQ2Id,
                    //    Title = "False",
                    //    IsCorrect = true,
                    //    CreatedOn = DateTime.UtcNow,
                    //    UpdatedOn = DateTime.UtcNow
                    //},

                    //// General Q3: Renewable energy sources (Multiple Choice)
                    //new Answer
                    //{
                    //    Id = "675c1f85e7b5a4f12c8d9e76",
                    //    QuestionId = generalQ3Id,
                    //    Title = "Solar Power",
                    //    IsCorrect = true,
                    //    CreatedOn = DateTime.UtcNow,
                    //    UpdatedOn = DateTime.UtcNow
                    //},
                    //new Answer
                    //{
                    //    Id = "675c1f85e7b5a4f12c8d9e77",
                    //    QuestionId = generalQ3Id,
                    //    Title = "Wind Power",
                    //    IsCorrect = true,
                    //    CreatedOn = DateTime.UtcNow,
                    //    UpdatedOn = DateTime.UtcNow
                    //},
                    //new Answer
                    //{
                    //    Id = "675c1f85e7b5a4f12c8d9e78",
                    //    QuestionId = generalQ3Id,
                    //    Title = "Coal",
                    //    IsCorrect = false,
                    //    CreatedOn = DateTime.UtcNow,
                    //    UpdatedOn = DateTime.UtcNow
                    //},
                    //new Answer
                    //{
                    //    Id = "675c1f85e7b5a4f12c8d9e79",
                    //    QuestionId = generalQ3Id,
                    //    Title = "Hydroelectric",
                    //    IsCorrect = true,
                    //    CreatedOn = DateTime.UtcNow,
                    //    UpdatedOn = DateTime.UtcNow
                    //},

                    //// General Q4: The Starry Night painter
                    //new Answer
                    //{
                    //    Id = "675c1f85e7b5a4f12c8d9e80",
                    //    QuestionId = generalQ4Id,
                    //    Title = "Vincent van Gogh",
                    //    IsCorrect = true,
                    //    CreatedOn = DateTime.UtcNow,
                    //    UpdatedOn = DateTime.UtcNow
                    //},
                    //new Answer
                    //{
                    //    Id = "675c1f85e7b5a4f12c8d9e81",
                    //    QuestionId = generalQ4Id,
                    //    Title = "Pablo Picasso",
                    //    IsCorrect = false,
                    //    CreatedOn = DateTime.UtcNow,
                    //    UpdatedOn = DateTime.UtcNow
                    //},
                    //new Answer
                    //{
                    //    Id = "675c1f85e7b5a4f12c8d9e82",
                    //    QuestionId = generalQ4Id,
                    //    Title = "Claude Monet",
                    //    IsCorrect = false,
                    //    CreatedOn = DateTime.UtcNow,
                    //    UpdatedOn = DateTime.UtcNow
                    //},
                    //new Answer
                    //{
                    //    Id = "675c1f85e7b5a4f12c8d9e83",
                    //    QuestionId = generalQ4Id,
                    //    Title = "Leonardo da Vinci",
                    //    IsCorrect = false,
                    //    CreatedOn = DateTime.UtcNow,
                    //    UpdatedOn = DateTime.UtcNow
                    //},

                    //// General Q5: Honey preservation (True/False)
                    //new Answer
                    //{
                    //    Id = "675c1f85e7b5a4f12c8d9e84",
                    //    QuestionId = generalQ5Id,
                    //    Title = "True",
                    //    IsCorrect = true,
                    //    CreatedOn = DateTime.UtcNow,
                    //    UpdatedOn = DateTime.UtcNow
                    //},
                    //new Answer
                    //{
                    //    Id = "675c1f85e7b5a4f12c8d9e85",
                    //    QuestionId = generalQ5Id,
                    //    Title = "False",
                    //    IsCorrect = false,
                    //    CreatedOn = DateTime.UtcNow,
                    //    UpdatedOn = DateTime.UtcNow
                    //},

                    //// Legacy question answers
                    //new Answer
                    //{
                    //    Id = "675c1f85e7b5a4f12c8d9e90",
                    //    QuestionId = legacyQ1Id,
                    //    Title = ".NET 5",
                    //    IsCorrect = false
                    //},
                    //new Answer
                    //{
                    //    Id = "675c1f85e7b5a4f12c8d9e91",
                    //    QuestionId = legacyQ1Id,
                    //    Title = ".NET Core 3.1",
                    //    IsCorrect = false
                    //},
                    //new Answer
                    //{
                    //    Id = "675c1f85e7b5a4f12c8d9e92",
                    //    QuestionId = legacyQ1Id,
                    //    Title = ".NET 6",
                    //    IsCorrect = true
                    //},
                    //new Answer
                    //{
                    //    Id = "675c1f85e7b5a4f12c8d9e93",
                    //    QuestionId = legacyQ1Id,
                    //    Title = ".NET Framework 4.8",
                    //    IsCorrect = false
                    //}
                };

                await answersCollection.InsertManyAsync(sampleAnswers);
            }
        }
    }
}
