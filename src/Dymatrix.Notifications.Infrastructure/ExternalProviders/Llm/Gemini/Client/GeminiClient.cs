using System.Net.Http.Json;
using System.Text.Json;
using Dymatrix.Notifications.Infrastructure.ExternalProviders.Llm.Gemini.Contracts;
using Dymatrix.Notifications.Infrastructure.ExternalProviders.Llm.Gemini.Serialization;
using Dymatrix.Notifications.Infrastructure.Settings;
using Microsoft.Extensions.Options;

namespace Dymatrix.Notifications.Infrastructure.ExternalProviders.Llm.Gemini.Client;

public sealed class GeminiClient(
    IHttpClientFactory httpClientFactory,
    IOptions<GeminiOptions> options)
{
    private const string ApiKeyHeaderName = "x-goog-api-key";
    internal const string ClientName = nameof(GeminiClient);

    private readonly GeminiOptions _options = options.Value;

    public async Task<GeminiGenerateContentResponse?> GenerateContentAsync(
        GeminiGenerateContentRequest requestBody,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post, RequestUri());
        request.Content = JsonContent.Create(requestBody, options: GeminiJsonSerializerOptions.ProviderJsonOptions);
        request.Headers.Add(ApiKeyHeaderName, _options.ApiKey);

        HttpResponseMessage response;
        try
        {
            response = await httpClientFactory
                .CreateClient(ClientName)
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (HttpRequestException)
        {
            throw new HttpRequestException("The Gemini provider request failed.");
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var error = await ReadProviderErrorMessageAsync(response, cancellationToken);
                throw new HttpRequestException(
                    $"The Gemini provider returned HTTP {(int)response.StatusCode} ({response.ReasonPhrase}).{error}",
                    inner: null,
                    response.StatusCode);
            }

            try
            {
                return await response.Content.ReadFromJsonAsync<GeminiGenerateContentResponse>(
                    GeminiJsonSerializerOptions.ProviderJsonOptions,
                    cancellationToken);
            }
            catch (JsonException exception)
            {
                throw new JsonException("The Gemini provider returned malformed JSON.", exception);
            }
        }
    }

    private string RequestUri() => $"models/{Uri.EscapeDataString(_options.Model)}:generateContent";

    private static async Task<string> ReadProviderErrorMessageAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            var providerError = await response.Content.ReadFromJsonAsync<GeminiErrorResponse>(
                GeminiJsonSerializerOptions.ProviderJsonOptions,
                cancellationToken);

            var message = providerError?.Error?.Message;
            return string.IsNullOrWhiteSpace(message) ? string.Empty : $" Provider error: {message}";
        }
        catch (JsonException)
        {
            return string.Empty;
        }
        catch (NotSupportedException)
        {
            return string.Empty;
        }
    }

}
