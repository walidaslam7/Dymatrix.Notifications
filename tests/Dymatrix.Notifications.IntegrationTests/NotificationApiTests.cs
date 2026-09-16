using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Dymatrix.Notifications.IntegrationTests.Factories;

namespace Dymatrix.Notifications.IntegrationTests;

public sealed class NotificationApiTests(NotificationApiFactory factory) : IClassFixture<NotificationApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GivenApplication_WhenHealthIsRequested_ThenReturnsOk()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("info", HttpStatusCode.OK, false)]
    [InlineData("warning", HttpStatusCode.Accepted, true)]
    [InlineData("error", HttpStatusCode.Accepted, true)]
    [InlineData("critical", HttpStatusCode.Accepted, true)]
    [InlineData("WaRnInG", HttpStatusCode.Accepted, true)]
    public async Task GivenSupportedLevel_WhenNotificationIsSubmitted_ThenReturnsExpectedStatus(
        string level,
        HttpStatusCode expectedStatus,
        bool queued)
    {
        var response = await _client.PostAsJsonAsync("/api/notifications", new
        {
            level,
            message = "Database connection pool exhausted"
        });

        Assert.Equal(expectedStatus, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.NotEqual(Guid.Empty, body.RootElement.GetProperty("notificationId").GetGuid());
        Assert.Equal(queued, body.RootElement.GetProperty("queued").GetBoolean());
    }

    [Theory]
    [InlineData("{\"message\":\"Missing level\"}")]
    [InlineData("{\"level\":null,\"message\":\"Null level\"}")]
    [InlineData("{\"level\":\"warning\"}")]
    [InlineData("{\"level\":\"warning\",\"message\":null}")]
    [InlineData("{\"level\":\"warning\",\"message\":\"   \"}")]
    [InlineData("{\"level\":\"banana\",\"message\":\"Invalid level\"}")]
    [InlineData("{\"level\":\"warning\",}")]
    public async Task GivenInvalidRequest_WhenNotificationIsSubmitted_ThenReturnsValidationProblem(string json)
    {
        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/notifications", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.StartsWith("application/problem+json", response.Content.Headers.ContentType?.ToString());
    }

    [Fact]
    public async Task GivenNumericLevel_WhenNotificationIsSubmitted_ThenReturnsValidationProblem()
    {
        using var content = new StringContent(
            "{\"level\":999,\"message\":\"Invalid level\"}",
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await _client.PostAsync("/api/notifications", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.StartsWith("application/problem+json", response.Content.Headers.ContentType?.ToString());
    }
    [Fact]
    public async Task GivenForwardingNotification_WhenSubmitted_ThenItIsProcessedFromTheQueue()
    {
        var response = await _client.PostAsJsonAsync("/api/notifications", new
        {
            level = "warning",
            message = "Database connection pool exhausted"
        });

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var notificationId = body.RootElement.GetProperty("notificationId").GetGuid();

        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var processed = await factory.WaitForProcessedAsync(notificationId, cancellationTokenSource.Token);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal(notificationId, processed.Id);
    }
}
