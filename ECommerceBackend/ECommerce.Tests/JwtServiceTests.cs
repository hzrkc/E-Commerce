using Xunit;
using Microsoft.Extensions.Configuration;
using ECommerce.Api.Services;
using ECommerce.Core.Entities;
using System.Collections.Generic;

namespace ECommerce.Tests
{
    public class JwtServiceTests
    {
        [Fact]
        public void GenerateToken_ShouldReturnValidToken()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Jwt:SecretKey", "a-very-strong-secret-key-string-1234567890abcd" },
                    { "Jwt:Issuer", "ECommerceApi" },
                    { "Jwt:Audience", "ECommerceClient" }
                })
                .Build();

            var jwtService = new JwtService(config);

            var token = jwtService.GenerateToken(new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                Email = "test@example.com"
            });

            Assert.False(string.IsNullOrWhiteSpace(token));
        }
    }
}