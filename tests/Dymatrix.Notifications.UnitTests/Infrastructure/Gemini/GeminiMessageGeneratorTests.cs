using System.Net;
using System.Text.Json;
using Dymatrix.Notifications.Domain.Constants;
using Dymatrix.Notifications.Domain.Models;
using Dymatrix.Notifications.Infrastructure.ExternalProviders.Llm.Gemini;
using Dymatrix.Notifications.Infrastructure.ExternalProviders.Llm.Gemini.Client;
using Dymatrix.Notifications.Infrastructure.ExternalProviders.Llm.Gemini.Mapping;
using Dymatrix.Notifications.Infrastructure.Settings;
using Microsoft.Extensions.Options;

namespace Dymatrix.Notifications.UnitTests.Infrastructure.Gemini;

public sealed class GeminiMessageGeneratorTests
{
    [Fact]
    public async Task GivenNotification_WhenMessageIsGenerated_ThenBuildsRequestAndMapsStructuredOutput()
    {
        var handler = new CapturingHandler("{\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"{\\\"category\\\":\\\"database\\\",\\\"message\\\":\\\"Database unavailable\\\"}\",\"thoughtSignature\":\"provider-metadata\"}]}}]}");
        var generator = CreateGenerator(handler, "test-key");

        var generated = await generator.GenerateAsync(
            new Notification(NotificationLevel.Error, "database connection refused"),
            CancellationToken.None);

        Assert.Equal("database", generated.Category);
        Assert.Equal("Database unavailable", generated.Message);
        Assert.Equal(HttpMethod.Post, handler.Request!.Method);
        Assert.Equal("https://generativelanguage.googleapis.com/v1beta/models/gemini-3.6-flash:generateContent", handler.Request.RequestUri!.ToString());
        Assert.Equal("test-key", handler.Request.Headers.GetValues("x-goog-api-key").Single());

        using var body = JsonDocument.Parse(handler.Body!);
        var root = body.RootElement;
        Assert.Contains("Error", root.GetProperty("contents")[0].GetProperty("parts")[0].GetProperty("text").GetString());
        Assert.Contains("database connection refused", root.GetProperty("contents")[0].GetProperty("parts")[0].GetProperty("text").GetString());
        Assert.Contains("untrusted", root.GetProperty("contents")[0].GetProperty("parts")[0].GetProperty("text").GetString(), StringComparison.OrdinalIgnoreCase);
        var generationConfig = root.GetProperty("generationConfig");
        Assert.Equal("application/json", generationConfig.GetProperty("responseMimeType").GetString());
        var schema = generationConfig.GetProperty("responseSchema");
        Assert.Equal("object", schema.GetProperty("type").GetString());
        Assert.False(schema.TryGetProperty("additionalProperties", out _));
        Assert.Equal(2, schema.GetProperty("required").GetArrayLength());
        Assert.Equal("string", schema.GetProperty("properties").GetProperty("category").GetProperty("type").GetString());
        Assert.Equal("string", schema.GetProperty("properties").GetProperty("message").GetProperty("type").GetString());
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, "HTTP 400")]
    [InlineData(HttpStatusCode.InternalServerError, "HTTP 500")]
    public async Task GivenProviderFailure_WhenMessageIsGenerated_ThenThrows(HttpStatusCode status, string expected)
    {
        var generator = CreateGenerator(new CapturingHandler("{}", status), "test-key");

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
            generator.GenerateAsync(new Notification(NotificationLevel.Warning, "disk full"), CancellationToken.None));

        Assert.Contains(expected, exception.Message);
    }

    [Fact]
    public async Task GivenStructuredProviderError_WhenMessageIsGenerated_ThenIncludesSanitizedDiagnostics()
    {
        const string providerError = "{\"error\":{\"code\":400,\"message\":\"Example validation error\",\"status\":\"INVALID_ARGUMENT\"}}";
        var generator = CreateGenerator(new CapturingHandler(providerError, HttpStatusCode.BadRequest), "test-key");

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
            generator.GenerateAsync(new Notification(NotificationLevel.Warning, "disk full"), CancellationToken.None));

        Assert.Contains("HTTP 400", exception.Message);
        Assert.Contains("Example validation error", exception.Message);
        Assert.DoesNotContain("test-key", exception.Message);
        Assert.DoesNotContain("x-goog-api-key", exception.Message);
    }

    [Theory]
    [InlineData("", typeof(InvalidDataException), "no usable generated content")]
    [InlineData("not-json", typeof(JsonException), "malformed structured output")]
    [InlineData("{\"category\":\"\",\"message\":\"usable\"}", typeof(InvalidDataException), "generated category was empty")]
    [InlineData("{\"category\":\"usable\",\"message\":\"  \"}", typeof(InvalidDataException), "generated message was empty")]
    [InlineData("{\"category\":\"usable\",\"message\":\"ok\",\"extra\":true}", typeof(JsonException), "malformed structured output")]
    public async Task GivenInvalidGeneratedOutput_WhenMessageIsGenerated_ThenThrows(
        string generatedJson,
        Type expectedExceptionType,
        string expectedMessage)
    {
        var response = $"{{\"candidates\":[{{\"content\":{{\"parts\":[{{\"text\":{JsonSerializer.Serialize(generatedJson)}}}]}}}}]}}";
        var generator = CreateGenerator(new CapturingHandler(response), "test-key");

        var exception = await Record.ExceptionAsync(() =>
            generator.GenerateAsync(new Notification(NotificationLevel.Error, "request failed"), CancellationToken.None));

        Assert.NotNull(exception);
        Assert.Equal(expectedExceptionType, exception.GetType());
        Assert.Contains(expectedMessage, exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static GeminiMessageGenerator CreateGenerator(HttpMessageHandler handler, string apiKey) =>
        new(
            new GeminiRequestMapper(),
            new GeminiClient(
                new TestHttpClientFactory(
                    new HttpClient(handler) { BaseAddress = new Uri("https://generativelanguage.googleapis.com/v1beta/") }),
                Options.Create(new GeminiOptions { ApiKey = apiKey, Model = "gemini-3.6-flash" })),
            new GeminiResponseMapper());

    private sealed class TestHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        private readonly string body;
        private readonly HttpStatusCode status;
        private readonly CancellationToken cancellationToken;

        public CapturingHandler(string body, HttpStatusCode status = HttpStatusCode.OK, CancellationToken cancellationToken = default)
        {
            this.body = body;
            this.status = status;
            this.cancellationToken = cancellationToken;
        }

        public HttpRequestMessage? Request { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            this.cancellationToken.ThrowIfCancellationRequested();
            return new HttpResponseMessage(status)
            {
                Content = new StringContent(body)
            };
        }
    }
}
