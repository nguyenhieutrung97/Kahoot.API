using BDKahoot.Domain.Models;
using BDKahoot.Domain.Repositories;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace BDKahoot.API.Middlewares
{
    public class PostLoginClaimsTransformation(IUnitOfWork unitOfWork) : IClaimsTransformation
    {
        public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            var identity = (ClaimsIdentity)principal.Identity!;
            var userId = identity.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userLastName = identity.FindFirst(ClaimTypes.Surname)?.Value;
            var userFirstName = identity.FindFirst(ClaimTypes.GivenName)?.Value;
            var userEmailAddress = identity.FindFirst(ClaimTypes.Email)?.Value;
            var userNTIDEmailAddress = identity.FindFirst(ClaimTypes.Upn)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Task.FromResult(principal);

            bool userExistInDb = unitOfWork.Users.GetByUpnAsync(userNTIDEmailAddress).Result == null ? false : true;

            if (!userExistInDb)
            {
                // Add new user with default role and claims.
                User user = new User() { CreatedOn = DateTime.UtcNow, UpdatedOn = DateTime.UtcNow, FirstName = userFirstName, Lastname = userLastName, Upn = userNTIDEmailAddress, EmailAddress = userEmailAddress };
                unitOfWork.Users.AddAsync(user);
            }

            return Task.FromResult(principal);
        }
    }
}
