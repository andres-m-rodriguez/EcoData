namespace EcoData.Common.Messaging.Handlers;

/// <summary>
/// Handler interface for processing events.
/// </summary>
/// <typeparam name="TEvent">The type of event to handle.</typeparam>
public interface IEventHandler<in TEvent>
{
    /// <summary>
    /// Handles an event.
    /// </summary>
    /// <param name="event">The event to handle.</param>
    /// <param name="context">Context information about the event.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task HandleAsync(TEvent @event, EventContext context, CancellationToken cancellationToken = default);
}
