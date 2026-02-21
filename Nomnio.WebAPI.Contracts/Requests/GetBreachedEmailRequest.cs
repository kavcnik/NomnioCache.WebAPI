using System.ComponentModel.DataAnnotations;

namespace Nomnio.WebAPI.Contracts.Requests
{
    public class GetBreachedEmailRequest
    {
        [Required]
        [EmailAddress]
        public required string Email { get; set; }
    }
}
