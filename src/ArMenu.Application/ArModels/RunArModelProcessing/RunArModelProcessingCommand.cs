using ArMenu.Domain.ArModels;
using ArMenu.Domain.Common;
using Mediator;

namespace ArMenu.Application.ArModels.RunArModelProcessing;

/// <summary>
/// Runs one attempt of a queued processing. Sent by the processing worker, in a scope bound to the processing's tenant.
/// </summary>
public sealed record RunArModelProcessingCommand(ArModelProcessingId ProcessingId) : ICommand<Result>;
