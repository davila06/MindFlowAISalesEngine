import { useOperationsKpiQuery } from '@/hooks/queries/useOperationsKpiQuery';
import { KpiCard } from '@/components/ui/KpiCard';

export function OperationsKpiCards({ days }: { days: number }) {
  const { data, isLoading, error } = useOperationsKpiQuery(days);

  if (isLoading) return <div>Cargando KPIs operativos...</div>;
  if (error) return <div className="text-red-600">Error al cargar KPIs</div>;
  if (!data) return null;

  return (
    <section className="grid grid-cols-1 md:grid-cols-3 gap-4 my-6">
      <KpiCard label="Deploys/semana" value={data.deploymentFrequency.toString()} variant="success" />
      <KpiCard label="Change Failure Rate" value={(data.changeFailureRate * 100).toFixed(1) + '%'} variant={data.changeFailureRate > 0.1 ? 'error' : 'success'} />
      <KpiCard label="MTTR (h)" value={data.mttrHours.toFixed(1)} variant={data.mttrHours > 4 ? 'warning' : 'success'} />
      <KpiCard label="Job failures" value={data.backgroundJobFailures.toString()} variant={data.backgroundJobFailures > 0 ? 'error' : 'success'} />
      <KpiCard label="Email Delivery %" value={(data.emailDeliverySuccessRate * 100).toFixed(2) + '%'} variant={data.emailDeliverySuccessRate < 0.97 ? 'warning' : 'success'} />
    </section>
  );
}
