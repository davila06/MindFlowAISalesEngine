namespace Api.Contracts;

public static class DomainErrorCodes
{
    public const string ValidationError = "validation_error";
    public const string InternalError = "internal_error";
    public const string LeadDuplicate = "lead.duplicate";
    public const string LeadInvalidCountry = "lead.invalid_country";
}
