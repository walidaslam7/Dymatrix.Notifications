using System.Text.Json;
using System.Text.Json.Serialization;

namespace Dymatrix.Notifications.Infrastructure.ExternalProviders.Llm.Gemini.Serialization;

internal static class GeminiJsonSerializerOptions
{
    public static readonly JsonSerializerOptions ProviderJsonOptions = new(JsonSerializerDefaults.Web);

    public static readonly JsonSerializerOptions GeneratedOutputJsonOptions = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };
}
