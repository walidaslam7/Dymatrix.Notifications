using System.Net;
using System.Text.Json;
using Dymatrix.Notifications.Domain.Constants;
using Dymatrix.Notifications.Domain.Models;
using Dymatrix.Notifications.Infrastructure.ExternalProviders.Webhooks.Discord.Client;
using Dymatrix.Notifications.Infrastructure.ExternalProviders.Webhooks.Discord;
using Dymatrix.Notifications.Infrastructure.ExternalProviders.Webhooks.Discord.Mapping;
using Dymatrix.Notifications.Infrastructure.Models;
using Dymatrix.Notifications.Infrastructure.Settings;
using Microsoft.Extensions.Options;

namespace Dymatrix.Notifications.UnitTests.Infrastructure.Discord;

public sealed class DiscordNotificationPublisherTests
{
    [Fact]
    public async Task GivenGeneratedMessage_WhenPublished_ThenPostsExpectedPayload()
    {
        var handler = new CapturingHandler(HttpStatusCode.NoContent);
        var publisher = CreatePublisher(handler, "https://discord.example/webhook/abc");

        await publisher.PublishAsync(
            new Notification(NotificationLevel.Warning, "raw original"),
            new GeneratedMessage("database", "Connection pool exhausted."),
            CancellationToken.None);

        Assert.Equal(HttpMethod.Post, handler.Request!.Method);
        Assert.Equal("https://discord.example/webhook/abc", handler.Request.RequestUri!.ToString());
        using var body = JsonDocument.Parse(handler.Body!);
        Assert.Equal("[WARNING] database\nConnection pool exhausted.", body.RootElement.GetProperty("content").GetString());
    }

    [Fact]
    public async Task GivenDiscordFailure_WhenPublished_ThenThrowsWithoutExposingWebhook()
    {
        const string webhook = "https://discord.example/secret-webhook";
        var publisher = CreatePublisher(new CapturingHandler(HttpStatusCode.BadGateway), webhook);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
            publisher.PublishAsync(
                new Notification(NotificationLevel.Error, "failed"),
                new GeneratedMessage("availability", "Service unavailable."),
                CancellationToken.None));

        Assert.DoesNotContain(webhook, exception.ToString());
    }

    [Fact]
    public async Task GivenTransportFailureContainingWebhook_WhenPublished_ThenExceptionIsSanitized()
    {
        const string webhook = "https://discord.example/secret-webhook";
        var publisher = CreatePublisher(new ThrowingHandler(new HttpRequestException($"Failed request to {webhook}")), webhook);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
            publisher.PublishAsync(
                new Notification(NotificationLevel.Error, "failed"),
                new GeneratedMessage("availability", "Service unavailable."),
                CancellationToken.None));

        Assert.Equal("The Discord webhook request failed.", exception.Message);
        Assert.DoesNotContain(webhook, exception.ToString());
    }

    private static DiscordNotificationPublisher CreatePublisher(HttpMessageHandler handler, string webhookUrl) =>
        new(
            new DiscordNotificationMapper(),
            new DiscordWebhookClient(
                new TestHttpClientFactory(new HttpClient(handler)),
                Options.Create(new DiscordOptions { WebhookUrl = webhookUrl })));

    private sealed class TestHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode status;
        private readonly CancellationToken configuredCancellationToken;

        public CapturingHandler(HttpStatusCode status, CancellationToken cancellationToken = default)
        {
            this.status = status;
            configuredCancellationToken = cancellationToken;
        }

        public HttpRequestMessage? Request { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            configuredCancellationToken.ThrowIfCancellationRequested();
            Request = request;
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(status);
        }
    }

    private sealed class ThrowingHandler(HttpRequestException exception) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => throw exception;
    }
}
