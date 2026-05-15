using System.Globalization;
using System.Text;
using Api.Contracts;
using Api.Contracts.Analytics;

namespace Api.Application.AnalyticsAdvanced;

public sealed class AnalyticsCsvExportService : IAnalyticsCsvExportService
{
    public string ExportDashboardOverviewCsv(DashboardOverviewResponse response)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Metric,Value");
        builder.AppendLine($"TotalLeads,{Format(response.TotalLeads)}");
        builder.AppendLine($"TotalOpportunities,{Format(response.TotalOpportunities)}");
        builder.AppendLine($"WonOpportunities,{Format(response.WonOpportunities)}");
        builder.AppendLine($"ConversionRate,{Format(response.ConversionRate)}");
        builder.AppendLine($"PipelineValue,{Format(response.PipelineValue)}");
        builder.AppendLine();
        builder.AppendLine("LeadsPerDayDate,LeadsPerDayCount");

        foreach (var point in response.LeadsPerDay)
        {
            builder.AppendLine($"{Escape(point.Date)},{Format(point.Count)}");
        }

        return builder.ToString();
    }

    public string ExportAdvancedOverviewCsv(AnalyticsAdvancedOverviewResponse response)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Section,Metric,Value");

        Append(builder, "Funnel", "NewCount", response.Funnel.NewCount);
        Append(builder, "Funnel", "QualifiedCount", response.Funnel.QualifiedCount);
        Append(builder, "Funnel", "ProposalCount", response.Funnel.ProposalCount);
        Append(builder, "Funnel", "WonCount", response.Funnel.WonCount);
        Append(builder, "Funnel", "NewToQualifiedRate", response.Funnel.NewToQualifiedRate);
        Append(builder, "Funnel", "QualifiedToProposalRate", response.Funnel.QualifiedToProposalRate);
        Append(builder, "Funnel", "ProposalToWonRate", response.Funnel.ProposalToWonRate);

        Append(builder, "Revenue", "WonRevenue", response.Revenue.WonRevenue);
        Append(builder, "Revenue", "PipelineRevenue", response.Revenue.PipelineRevenue);
        Append(builder, "Revenue", "AverageDealSize", response.Revenue.AverageDealSize);
        Append(builder, "Revenue", "Currency", response.Revenue.Currency);

        Append(builder, "Velocity", "AverageHoursToQualified", response.Velocity.AverageHoursToQualified);
        Append(builder, "Velocity", "AverageHoursToProposal", response.Velocity.AverageHoursToProposal);
        Append(builder, "Velocity", "AverageHoursToWon", response.Velocity.AverageHoursToWon);

        Append(builder, "Sla", "AssignmentWithinSlaRate", response.Sla.AssignmentWithinSlaRate);
        Append(builder, "Sla", "FirstResponseWithinSlaRate", response.Sla.FirstResponseWithinSlaRate);
        Append(builder, "Sla", "SlaBreaches", response.Sla.SlaBreaches);

        Append(builder, "OnboardingActivation", "NewCustomers", response.OnboardingActivation.NewCustomers);
        Append(builder, "OnboardingActivation", "ActivatedCustomers", response.OnboardingActivation.ActivatedCustomers);
        Append(builder, "OnboardingActivation", "ActivationRate", response.OnboardingActivation.ActivationRate);
        Append(builder, "OnboardingActivation", "AverageHoursToFirstActivation", response.OnboardingActivation.AverageHoursToFirstActivation);

        return builder.ToString();
    }

    private static void Append(StringBuilder builder, string section, string metric, object? value)
    {
        builder.Append(Escape(section));
        builder.Append(',');
        builder.Append(Escape(metric));
        builder.Append(',');
        builder.AppendLine(Escape(Format(value)));
    }

    private static string Format(object? value)
    {
        return value switch
        {
            null => string.Empty,
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };
    }

    private static string Escape(string value)
    {
        if (value.Contains('"'))
        {
            value = value.Replace("\"", "\"\"");
        }

        if (value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value}\"";
        }

        return value;
    }
}
