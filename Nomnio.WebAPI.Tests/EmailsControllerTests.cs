using Microsoft.AspNetCore.Mvc;
using Nomnio.WebAPI.Contracts;
using Nomnio.WebAPI.Contracts.Grains;
using Nomnio.WebAPI.Contracts.Requests;
using Nomnio.WebAPI.Controllers;
using NSubstitute;

namespace Nomnio.WebAPI.Tests
{
    public class EmailsControllerTests
    {
        private readonly IGrainFactory _grainFactory = Substitute.For<IGrainFactory>();
        private readonly EmailsController _sut;

        public EmailsControllerTests()
        {
            _sut = new EmailsController(_grainFactory);
        }

        [Fact]
        public async Task Get_WhenNotFound_ReturnsNotFoundResult()
        {
            var grain = Substitute.For<ICacheGrain>();
            grain.GetAsync("test@example.com", Arg.Any<CancellationToken>())
                .Returns(new CacheResult { Found = false, Email = "test@example.com" });

            _grainFactory.GetGrain<ICacheGrain>("example.com", null)
                .Returns(grain);

            var request = new GetBreachedEmailRequest { Email = "test@example.com" };
            var result = await _sut.Get(request, CancellationToken.None);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Get_WhenFound_ReturnsOkWithCacheResult()
        {
            var cacheResult = new CacheResult
            {
                Found = true,
                Email = "test@example.com",
                Details = "breached"
            };
            var grain = Substitute.For<ICacheGrain>();
            grain.GetAsync("test@example.com", Arg.Any<CancellationToken>()).Returns(cacheResult);

            _grainFactory.GetGrain<ICacheGrain>("example.com", null)
                .Returns(grain);

            var request = new GetBreachedEmailRequest { Email = "test@example.com" };
            var result = await _sut.Get(request, CancellationToken.None);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(cacheResult, okResult.Value);
        }

        [Fact]
        public async Task Post_WhenAdded_ReturnsCreatedAtAction()
        {
            var grain = Substitute.For<ICacheGrain>();
            grain.AddAsync("new@example.com", "breach details", Arg.Any<CancellationToken>()).Returns(true);

            _grainFactory.GetGrain<ICacheGrain>("example.com", null)
                .Returns(grain);

            var body = new AddBreachedEmailDetails { Details = "breach details" };
            var result = await _sut.Post("new@example.com", body, CancellationToken.None);

            Assert.IsType<CreatedAtActionResult>(result);
        }

        [Fact]
        public async Task Post_WhenDuplicate_ReturnsConflict()
        {
            var grain = Substitute.For<ICacheGrain>();
            grain.AddAsync("existing@example.com", "details", Arg.Any<CancellationToken>()).Returns(false);

            _grainFactory.GetGrain<ICacheGrain>("example.com", null)
                .Returns(grain);

            var body = new AddBreachedEmailDetails { Details = "details" };
            var result = await _sut.Post("existing@example.com", body, CancellationToken.None);

            Assert.IsType<ConflictObjectResult>(result);
        }
    }
}
