using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Xunit;
using GLMS.Api.Data;

namespace GLMS.Tests
{
    // Custom factory that configures the API for testing
    public class GlmsApiFactory
        : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(
            IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                // Remove the real database registration
                var descriptor = services.SingleOrDefault(d =>
                    d.ServiceType ==
                    typeof(DbContextOptions<GlmsDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                // Replace with an in-memory database
                // No real SQL Server needed for tests
                services.AddDbContext<GlmsDbContext>(options =>
                    options.UseInMemoryDatabase("GlmsTestDb"));

                // Build the service provider and create the DB
                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider
                    .GetRequiredService<GlmsDbContext>();

                // Make sure the in-memory DB is created
                db.Database.EnsureCreated();
            });
        }
    }

    public class ApiIntegrationTests
        : IClassFixture<GlmsApiFactory>
    {
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public ApiIntegrationTests(GlmsApiFactory factory)
        {
            _client = factory.CreateClient();
        }

        // AUTH TESTS

        [Fact]
        public async Task Register_ValidUser_Returns200()
        {
            // ARRANGE
            var payload = JsonSerializer.Serialize(new
            {
                username = "testuser_" +
                    Guid.NewGuid().ToString("N")[..8],
                password = "TestPass123!"
            });
            var content = new StringContent(
                payload, Encoding.UTF8, "application/json");

            // ACT
            var response = await _client
                .PostAsync("/api/auth/register", content);

            // ASSERT
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Login_InvalidCredentials_Returns401()
        {
            // ARRANGE
            var payload = JsonSerializer.Serialize(new
            {
                username = "nobody",
                password = "wrongpassword"
            });
            var content = new StringContent(
                payload, Encoding.UTF8, "application/json");

            // ACT
            var response = await _client
                .PostAsync("/api/auth/login", content);

            // ASSERT
            Assert.Equal(
                HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Login_ValidCredentials_ReturnsToken()
        {
            // ARRANGE — register first
            var username = "logintest_" +
                Guid.NewGuid().ToString("N")[..8];
            var password = "TestPass123!";

            var regPayload = JsonSerializer.Serialize(
                new { username, password });
            var regContent = new StringContent(
                regPayload, Encoding.UTF8, "application/json");
            await _client.PostAsync(
                "/api/auth/register", regContent);

            // Login with same credentials
            var loginPayload = JsonSerializer.Serialize(
                new { username, password });
            var loginContent = new StringContent(
                loginPayload, Encoding.UTF8, "application/json");

            // ACT
            var response = await _client
                .PostAsync("/api/auth/login", loginContent);
            var json = await response.Content.ReadAsStringAsync();

            // ASSERT
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("token", json.ToLower());
        }

        // CONTRACTS TESTS

        [Fact]
        public async Task GetContracts_Returns200AndJson()
        {
            // ACT
            var response = await _client
                .GetAsync("/api/contracts");
            var json = await response.Content
                .ReadAsStringAsync();

            // ASSERT
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(json);
            Assert.NotEmpty(json);
        }

        [Fact]
        public async Task GetContracts_FilterByStatus_Returns200()
        {
            // ACT
            var response = await _client
                .GetAsync("/api/contracts?status=Active");

            // ASSERT
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task CreateContract_InvalidData_Returns400()
        {
            // ARRANGE — empty object missing required fields
            var payload = JsonSerializer.Serialize(new { });
            var content = new StringContent(
                payload, Encoding.UTF8, "application/json");

            // ACT
            var response = await _client
                .PostAsync("/api/contracts", content);

            // ASSERT
            Assert.Equal(
                HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetContractById_InvalidId_Returns404()
        {
            // ACT
            var response = await _client
                .GetAsync("/api/contracts/999999");

            // ASSERT
            Assert.Equal(
                HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task PatchContractStatus_InvalidId_Returns404()
        {
            // ARRANGE
            var payload = JsonSerializer.Serialize(
                new { status = "Active" });
            var content = new StringContent(
                payload, Encoding.UTF8, "application/json");

            // ACT
            var response = await _client.PatchAsync(
                "/api/contracts/999999/status", content);

            // ASSERT
            Assert.Equal(
                HttpStatusCode.NotFound, response.StatusCode);
        }

        // CLIENTS TESTS

        [Fact]
        public async Task GetClients_Returns200AndJson()
        {
            // ACT
            var response = await _client
                .GetAsync("/api/clients");
            var json = await response.Content
                .ReadAsStringAsync();

            // ASSERT
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(json);
        }

        [Fact]
        public async Task CreateClient_ValidData_Returns201()
        {
            // ARRANGE
            var payload = JsonSerializer.Serialize(new
            {
                name = "Test Client " +
                    Guid.NewGuid().ToString("N")[..6],
                contactDetails = "test@client.com",
                region = "Africa"
            });
            var content = new StringContent(
                payload, Encoding.UTF8, "application/json");

            // ACT
            var response = await _client
                .PostAsync("/api/clients", content);

            // ASSERT
            Assert.Equal(
                HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task CreateClient_InvalidData_Returns400()
        {
            // ARRANGE
            var payload = JsonSerializer.Serialize(new { });
            var content = new StringContent(
                payload, Encoding.UTF8, "application/json");

            // ACT
            var response = await _client
                .PostAsync("/api/clients", content);

            // ASSERT
            Assert.Equal(
                HttpStatusCode.BadRequest, response.StatusCode);
        }

        // SERVICE REQUESTS TESTS

        [Fact]
        public async Task GetServiceRequests_Returns200AndJson()
        {
            // ACT
            var response = await _client
                .GetAsync("/api/servicerequests");
            var json = await response.Content
                .ReadAsStringAsync();

            // ASSERT
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(json);
        }

        [Fact]
        public async Task
            CreateServiceRequest_InvalidContractId_Returns400()
        {
            // ARRANGE — contract 999999 does not exist
            var payload = JsonSerializer.Serialize(new
            {
                contractId = 999999,
                description = "Test request",
                costUSD = 100.00,
                exchangeRate = 18.50,
                status = "Pending"
            });
            var content = new StringContent(
                payload, Encoding.UTF8, "application/json");

            // ACT
            var response = await _client
                .PostAsync("/api/servicerequests", content);

            // ASSERT — workflow guard must reject this
            Assert.Equal(
                HttpStatusCode.BadRequest, response.StatusCode);
        }

        // CURRENCY TESTS

        [Fact]
        public async Task GetCurrencyRate_Returns200AndRate()
        {
            // ACT
            var response = await _client
                .GetAsync("/api/currency/rate");
            var json = await response.Content
                .ReadAsStringAsync();

            // ASSERT
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("rate", json.ToLower());
        }
    }
}