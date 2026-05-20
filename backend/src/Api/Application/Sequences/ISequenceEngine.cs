using Api.Domain.Sequences;

namespace Api.Application.Sequences;

/// <summary>Processes due enrollment steps in batch (called by background service).</summary>
public interface ISequenceEngine
{
    Task RunDueBatchAsync(CancellationToken cancellationToken);
}
