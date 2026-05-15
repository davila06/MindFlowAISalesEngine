namespace Api.Application.Onboarding;

public class OnboardingValidationException : Exception
{
    public Dictionary<string, string[]> Errors { get; }

    public OnboardingValidationException(Dictionary<string, string[]> errors)
        : base("One or more onboarding validation errors occurred.")
    {
        Errors = errors;
    }
}
