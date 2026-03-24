namespace EcoData.Common.Messaging.Handlers;

/// <summary>
/// Handler interface for processing commands and returning results.
/// </summary>
/// <typeparam name="TCommand">The type of command to handle.</typeparam>
/// <typeparam name="TResult">The type of result to return.</typeparam>
public interface ICommandHandler<in TCommand, TResult>
{
    /// <summary>
    /// Handles a command and returns a result.
    /// </summary>
    /// <param name="command">The command to handle.</param>
    /// <param name="context">Context information about the command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of handling the command.</returns>
    Task<TResult> HandleAsync(TCommand command, CommandContext context, CancellationToken cancellationToken = default);
}
