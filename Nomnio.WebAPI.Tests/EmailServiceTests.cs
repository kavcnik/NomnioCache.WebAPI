using Nomnio.WebAPI.Services;

namespace Nomnio.WebAPI.Tests
{
    public class EmailServiceTests
    {
        [Theory]
        [InlineData("Test@Example.COM", "test@example.com")]
        [InlineData("  user@domain.com  ", "user@domain.com")]
        [InlineData("  UPPER@CASE.COM  ", "upper@case.com")]
        [InlineData("already@normalized.com", "already@normalized.com")]
        public void NormalizeEmail_TrimsAndLowercases(string input, string expected)
        {
            var result = EmailService.NormalizeEmail(input);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("user@example.com", "example.com")]
        [InlineData("user@domain.com", "domain.com")]
        [InlineData("user@sub.domain.com", "sub.domain.com")]
        public void ExtractDomain_ReturnsDomainPart(string input, string expected)
        {
            var result = EmailService.ExtractDomain(input);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ExtractDomain_AfterNormalize_ReturnsLowercaseDomain()
        {
            var normalized = EmailService.NormalizeEmail("USER@DOMAIN.COM");
            var domain = EmailService.ExtractDomain(normalized);
            Assert.Equal("domain.com", domain);
        }

        [Theory]
        [InlineData("noatsign")]
        [InlineData("trailing@")]
        public void ExtractDomain_InvalidEmail_ThrowsArgumentException(string input)
        {
            Assert.Throws<ArgumentException>(() => EmailService.ExtractDomain(input));
        }
    }
}
