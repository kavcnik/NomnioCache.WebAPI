using Microsoft.AspNetCore.Mvc;
using Nomnio.WebAPI.Contracts;
using Nomnio.WebAPI.Contracts.Grains;
using Nomnio.WebAPI.Contracts.Requests;
using Nomnio.WebAPI.Services;

namespace Nomnio.WebAPI.Controllers
{
    [ApiController]
    [Route("emails")]
    public class EmailsController : ControllerBase
    {
        private readonly IGrainFactory _grains;

        public EmailsController(IGrainFactory grains)
        {
            _grains = grains;
        }

        [HttpGet("{email}")]
        [ProducesResponseType(typeof(CacheResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Get([FromRoute] GetBreachedEmailRequest request, CancellationToken ct)
        {
            request.Email = EmailService.NormalizeEmail(request.Email);
            var domain = EmailService.ExtractDomain(request.Email);

            var grain = _grains.GetGrain<ICacheGrain>(domain);
            var result = await grain.GetAsync(request.Email, ct);

            if (!result.Found)
                return NotFound();

            return Ok(result);
        }

        [HttpPost("{email}")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Post(
            [FromRoute] string email,
            [FromBody] AddBreachedEmailDetails body,
            CancellationToken ct)
        {
            email = EmailService.NormalizeEmail(email);
            var domain = EmailService.ExtractDomain(email);

            var grain = _grains.GetGrain<ICacheGrain>(domain);

            var added = await grain.AddAsync(email, body.Details ?? string.Empty, ct);

            if (!added)
            {
                return Conflict(new
                {
                    email,
                    message = "Email already exists in breached list"
                });
            }

            return CreatedAtAction(
                nameof(Get),
                new { email },
                new { email, body.Details });
        }
    }
}
