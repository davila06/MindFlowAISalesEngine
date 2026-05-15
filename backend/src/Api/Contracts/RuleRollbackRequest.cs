using System.ComponentModel.DataAnnotations;

namespace Api.Contracts;

public class RuleRollbackRequest
{
    [Range(1, int.MaxValue)]
    public int? TargetVersion { get; init; }
}
