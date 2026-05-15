namespace Api.Domain.Pipeline;

public static class DefaultPipelineStages
{
    public static readonly PipelineStageDefinition New = new(
        Guid.Parse("11111111-1111-1111-1111-111111111111"),
        "new",
        1,
        "#2563eb");

    public static readonly PipelineStageDefinition Qualified = new(
        Guid.Parse("22222222-2222-2222-2222-222222222222"),
        "qualified",
        2,
        "#7c3aed");

    public static readonly PipelineStageDefinition Proposal = new(
        Guid.Parse("33333333-3333-3333-3333-333333333333"),
        "proposal",
        3,
        "#d97706");

    public static readonly PipelineStageDefinition Won = new(
        Guid.Parse("44444444-4444-4444-4444-444444444444"),
        "won",
        4,
        "#16a34a");

    public static IReadOnlyList<PipelineStageDefinition> All => [New, Qualified, Proposal, Won];
}

public sealed record PipelineStageDefinition(Guid Id, string Name, int Order, string Color);