using Api.Contracts;

namespace Api.Application.Proposals;

public interface IProposalService
{
    Task<ProposalResponse> CreateAsync(CreateProposalRequest request, CancellationToken cancellationToken);
    Task<ProposalTemplateResponse> CreateTemplateAsync(CreateProposalTemplateRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProposalTemplateResponse>> ListTemplatesAsync(CancellationToken cancellationToken);
    Task<ProposalResponse?> GetByIdAsync(Guid proposalId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProposalResponse>> ListAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<ProposalReminderJobResponse>> GetReminderDeadLetterAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<ProposalReminderJobResponse>> GetReminderPoisonQueueAsync(CancellationToken cancellationToken);
    Task<(byte[] PdfBytes, string FileName)?> GetPdfAsync(Guid proposalId, CancellationToken cancellationToken);
    Task<ProposalResponse?> SignAsync(Guid proposalId, ProposalSignRequest request, CancellationToken cancellationToken);
    Task<ProposalResponse?> ExpireAsync(Guid proposalId, CancellationToken cancellationToken);
    Task<ProposalResponse?> RenewAsync(Guid proposalId, ProposalRenewRequest request, CancellationToken cancellationToken);
    Task<ProposalKpiResponse> GetKpisAsync(CancellationToken cancellationToken);
    Task ForceReminderDueAsync(Guid proposalId, CancellationToken cancellationToken);
    Task RequeueReminderAsync(Guid proposalId, CancellationToken cancellationToken);
    Task ExecuteDueRemindersAsync(CancellationToken cancellationToken);
    Task TrackAsync(string trackingToken, CancellationToken cancellationToken);
}
