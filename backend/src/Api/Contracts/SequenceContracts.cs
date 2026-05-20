namespace Api.Contracts;

// ---- Sequences ----

public record SequenceStepRequest(
    int Order,
    string ActionType,
    string ActionValue,
    int DelayDays);

public record CreateSequenceRequest(
    string Name,
    string? Description,
    List<SequenceStepRequest> Steps);

public record UpdateSequenceRequest(
    string Name,
    string? Description,
    bool IsActive,
    List<SequenceStepRequest> Steps);

public record EnrollLeadRequest(Guid LeadId);

// ---- Responses ----

public record SequenceStepResponse(
    Guid Id,
    int Order,
    string ActionType,
    string ActionValue,
    int DelayDays);

public record SequenceResponse(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    List<SequenceStepResponse> Steps);

public record SequenceEnrollmentResponse(
    Guid Id,
    Guid LeadId,
    Guid SequenceId,
    string Status,
    int NextStepOrder,
    DateTime NextStepDueAtUtc,
    DateTime EnrolledAtUtc,
    DateTime? CompletedAtUtc,
    DateTime? ExitedAtUtc,
    string? ExitReason);
