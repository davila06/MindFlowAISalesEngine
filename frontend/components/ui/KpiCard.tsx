export function KpiCard({ label, value }: { label: string; value: string }) {
  return (
    <article className="kpi">
      <p className="label">{label}</p>
      <p className="value">{value}</p>
    </article>
  );
}