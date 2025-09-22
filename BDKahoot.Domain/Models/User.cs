using System.ComponentModel.DataAnnotations;

namespace BDKahoot.Domain.Models
{
    public class User : BaseModel
    {
        [MaxLength(16, ErrorMessage = "Please input Upn field with hint: ntid@bosch.com")]
        public string Upn { get; set; } = string.Empty;
        [EmailAddress]
        public string EmailAddress { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string Lastname { get; set; } = string.Empty;
    }
}
