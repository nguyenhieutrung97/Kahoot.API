using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace BDKahoot.IntegrationTests.Helpers
{
    internal class AzureAdAuthHandlerTest : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public AzureAdAuthHandlerTest(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // Check for test headers from SignalR connections
            var userId = Request.Headers["test-user-id"].FirstOrDefault() ?? Guid.NewGuid().ToString();
            var userName = Request.Headers["test-user-name"].FirstOrDefault() ?? "Test User";
            var isHost = Request.Headers["test-is-host"].FirstOrDefault() == "True";

            // Simulate AzureAD claims with test data
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Surname, "Team3"),
                new Claim(ClaimTypes.GivenName, userName),
                new Claim(ClaimTypes.Upn, "tut3hc@bosch.com"),
                new Claim(ClaimTypes.Email, "team3.testuser@vn.bosch.com"),
            };

            // Add role claim for hosts
            if (isHost)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Host"));
            }

            var identity = new ClaimsIdentity(claims, "TestAzureAd");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "TestAzureAd");

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
