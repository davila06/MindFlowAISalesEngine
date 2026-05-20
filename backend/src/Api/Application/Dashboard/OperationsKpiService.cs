using Api.Contracts;
using System.Collections.Generic;

namespace Api.Application.Dashboard;

public class OperationsKpiResponse
{
    public int DeploymentFrequency { get; set; }
    public decimal ChangeFailureRate { get; set; }
    public decimal MttrHours { get; set; }
    public int BackgroundJobFailures { get; set; }
    public decimal EmailDeliverySuccessRate { get; set; }
}

public interface IOperationsKpiService
{
    Task<OperationsKpiResponse> GetOperationsKpisAsync(int days, CancellationToken cancellationToken);
}

public class InMemoryOperationsKpiService : IOperationsKpiService
{
    public Task<OperationsKpiResponse> GetOperationsKpisAsync(int days, CancellationToken cancellationToken)
    {
        // Simulación de KPIs operativos
        return Task.FromResult(new OperationsKpiResponse
        {
            DeploymentFrequency = 5,
            ChangeFailureRate = 0.08m,
            MttrHours = 2.5m,
            BackgroundJobFailures = 1,
            EmailDeliverySuccessRate = 0.985m
        });
    }
}
