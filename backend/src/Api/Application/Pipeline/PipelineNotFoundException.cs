namespace Api.Application.Pipeline;

public class PipelineNotFoundException : Exception
{
    public PipelineNotFoundException(string message)
        : base(message)
    {
    }
}