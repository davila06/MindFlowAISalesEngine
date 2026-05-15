using System.ComponentModel.DataAnnotations;

namespace Api.Contracts;

public sealed class EmailTemplateRollbackRequest
{
    [Range(1, int.MaxValue)]
    public int TargetVersion { get; init; }
}