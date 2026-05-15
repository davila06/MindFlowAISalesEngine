namespace Api.Application.Pipeline;

public class PipelineValidationException : Exception
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public PipelineValidationException(Dictionary<string, string[]> errors)
        : base("Pipeline validation failed.")
    {
        Errors = errors;
    }
}