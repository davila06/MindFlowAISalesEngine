namespace Api.Application.Email;

public interface IEmailDispatchService
{
    Task<int> ExecuteDueAsync(CancellationToken cancellationToken);
    Task RetryAsync(Guid emailLogId, CancellationToken cancellationToken);
    Task<EmailKpiSnapshot> GetKpisAsync(CancellationToken cancellationToken);
}