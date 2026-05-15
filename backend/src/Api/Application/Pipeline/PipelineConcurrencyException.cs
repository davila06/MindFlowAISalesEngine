namespace Api.Application.Pipeline;

public sealed class PipelineConcurrencyException : Exception
{
    public PipelineConcurrencyException(string message) : base(message)
    {
    }
}
