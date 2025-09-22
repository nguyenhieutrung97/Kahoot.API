using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace BDKahoot.Application.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static string GetUserUpn(this ClaimsPrincipal user) => user.FindFirstValue(ClaimTypes.Upn) ?? string.Empty;

        public static string GetEmailAddress(this ClaimsPrincipal user) => user.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

        public static string GetUserNTID(this ClaimsPrincipal user) => user.FindFirstValue(ClaimTypes.Upn) ?? "gur4hc@bosch.com";

        public static string GetUserName(this ClaimsPrincipal user) => user.FindFirstValue(ClaimTypes.Surname) + ' ' + user.FindFirstValue(ClaimTypes.GivenName);
    }
}
