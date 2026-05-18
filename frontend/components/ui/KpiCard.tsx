export function KpiCard({ label, value, trend, variant }: {
  label: string;
  value: string;
  trend?: "up" | "down";
  variant?: "success" | "warning" | "error";
}) {
  const trendIcon = trend === "up"
    ? <span aria-label="Tendencia positiva" className="kpi-trend kpi-up">↑</span>
    : trend === "down"
    ? <span aria-label="Tendencia negativa" className="kpi-trend kpi-down">↓</span>
    : null;
  return (
    <article className={`kpi${variant ? ` kpi-${variant}` : ""}`}>
      <p className="label">{label}</p>
      <p className="value">{value} {trendIcon}</p>
    </article>
  );
}