using Dymatrix.Notifications.Domain.Models;
using Dymatrix.Notifications.Infrastructure.Models;

namespace Dymatrix.Notifications.Infrastructure.Abstractions;

public interface IMessageGenerator
{
    Task<GeneratedMessage> GenerateAsync(
        Notification notification,
        CancellationToken cancellationToken);
}
