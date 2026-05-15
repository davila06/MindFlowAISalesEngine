export function Skeleton({ large = false }: { large?: boolean }) {
  return (
    <div
      className={large ? "skeleton skeleton-lg" : "skeleton"}
      aria-hidden="true"
    />
  );
}

export function SkeletonRows({ rows = 4 }: { rows?: number }) {
  return (
    <div className="grid" aria-hidden="true">
      {Array.from({ length: rows }).map((_, index) => (
        <Skeleton key={`skeleton-row-${index}`} />
      ))}
    </div>
  );
}
