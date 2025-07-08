using ECommerce.Api;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ECommerce.Tests
{
    public class OrdersControllerTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public OrdersControllerTests(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task CreateOrder_Unauthorized_WithoutToken()
        {
            var requestBody = new
            {
                userId = "11111111-1111-1111-1111-111111111111",
                productId = "22222222-2222-2222-2222-222222222222",
                quantity = 1,
                paymentMethod = 1
            };

            var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("/api/orders", content);

            Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetOrders_Unauthorized_WithoutToken()
        {
            var response = await _client.GetAsync("/api/orders/11111111-1111-1111-1111-111111111111");

            Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
