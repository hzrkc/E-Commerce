using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;
using Xunit;
using ECommerce.Api;

namespace ECommerce.Tests
{
    public class OrdersControllerAuthorizedTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public OrdersControllerAuthorizedTests(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        private async Task<string> GetJwtTokenAsync()
        {
            var loginRequest = new
            {
                Username = "testuser",
                Password = "123123"
            };

            var content = new StringContent(JsonConvert.SerializeObject(loginRequest), Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("/api/auth/login", content);

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            dynamic? result = JsonConvert.DeserializeObject(json);
            if (result?.data?.token == null)
                throw new Exception("Token alınamadı.");

            return (string)result.data.token;
        }

        [Fact]
        public async Task CreateOrder_ShouldSucceed_WithValidToken()
        {
            var token = await GetJwtTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var requestBody = new
            {
                userId = "934b6e75-5c76-4a7b-8f02-a4d0c0970807", // test kullanıcının id'si
                productId = "8b49f635-f0c7-43fa-b50e-4a025545d013", // mevcut bir ürün id'si
                quantity = 1,
                paymentMethod = 1
            };

            var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("/api/orders", content);

            Assert.True(response.IsSuccessStatusCode);
        }
    }
}
