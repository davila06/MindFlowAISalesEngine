"use client";

import { useState, useCallback } from "react";
import { useQuery } from "@tanstack/react-query";
import { useI18n } from "@/i18n/I18nProvider";
import { leadsService, type LeadSearchParams } from "@/services/leads.service";
import { getCustomFieldDefinitions } from "@/services/customFields.service";
import type { CustomFieldDefinition } from "@/types/sequences";

const CORE_SORTS = ["createdAt", "score", "email", "source"] as const;
const PRIORITY_COLORS: Record<string, string> = {
  High:   "badge-error",
  Medium: "badge-warning",
  Low:    "badge-info",
};

export default function LeadsPage() {
  const { t } = useI18n();

  // ── Filter / sort state ─────────────────────────────────────────────────
  const [page, setPage]           = useState(1);
  const [sortBy, setSortBy]       = useState("createdAt");
  const [sortDir, setSortDir]     = useState<"asc" | "desc">("desc");
  const [cfSort, setCfSort]       = useState("");
  const [cfSortDir, setCfSortDir] = useState<"asc" | "desc">("asc");

  // cfFilters is an array of editable rows so the user can build multi-key filters
  const [filterRows, setFilterRows] = useState<{ key: string; value: string }[]>([]);
  // Committed (applied) filters
  const [appliedFilters, setAppliedFilters] = useState<Record<string, string>>({});

  const applyFilters = useCallback(() => {
    const map: Record<string, string> = {};
    for (const row of filterRows) {
      if (row.key.trim() && row.value.trim()) map[row.key.trim()] = row.value.trim();
    }
    setAppliedFilters(map);
    setPage(1);
  }, [filterRows]);

  const clearFilters = () => {
    setFilterRows([]);
    setAppliedFilters({});
    setPage(1);
  };

  // ── API calls ────────────────────────────────────────────────────────────
  const searchParams: LeadSearchParams = {
    page,
    pageSize: 20,
    sortBy,
    sortDir,
    cfSort: cfSort || undefined,
    cfSortDir,
    cfFilter: Object.keys(appliedFilters).length > 0 ? appliedFilters : undefined,
  };

  const { data, isLoading, isError } = useQuery({
    queryKey: ["leads", searchParams],
    queryFn: ({ signal }) => leadsService.search(searchParams, signal),
    placeholderData: (prev) => prev,
  });

  const { data: definitions = [] } = useQuery<CustomFieldDefinition[]>({
    queryKey: ["custom-fields", "Lead"],
    queryFn: ({ signal }) => getCustomFieldDefinitions("Lead", signal),
  });

  // ── Derive custom field column keys to show in table ────────────────────
  // Show up to 4 custom field columns: those present in appliedFilters first, then by order
  const cfCols = [
    ...definitions.filter(d => appliedFilters[d.key]),
    ...definitions.filter(d => !appliedFilters[d.key]),
  ].slice(0, 4);

  return (
    <main className="p-6">
      <div className="flex items-center justify-between mb-4">
        <div>
          <h1 className="text-2xl font-bold">{t("leads.title")}</h1>
          <p className="text-gray-500 text-sm">{t("leads.subtitle")}</p>
        </div>
        {data && (
          <span className="text-sm text-gray-500">
            {t("leads.total")}: <strong>{data.total}</strong>
          </span>
        )}
      </div>

      {/* ── Controls ──────────────────────────────────────────────────── */}
      <div className="border rounded p-4 bg-white mb-4 space-y-3">
        {/* Core sort */}
        <div className="flex flex-wrap gap-3 items-end">
          <div>
            <label className="text-xs font-medium text-gray-600 block mb-1">{t("leads.sortBy")}</label>
            <select
              className="input input-sm"
              value={cfSort ? "" : sortBy}
              onChange={(e) => { setSortBy(e.target.value); setCfSort(""); setPage(1); }}
            >
              {CORE_SORTS.map(s => (
                <option key={s} value={s}>{t(`leads.col.${s === "createdAt" ? "createdAt" : s}` as any)}</option>
              ))}
            </select>
          </div>
          <div>
            <label className="text-xs font-medium text-gray-600 block mb-1">{t("leads.sortDir")}</label>
            <select className="input input-sm" value={sortDir} onChange={(e) => { setSortDir(e.target.value as "asc" | "desc"); setPage(1); }}>
              <option value="asc">ASC</option>
              <option value="desc">DESC</option>
            </select>
          </div>

          {/* Custom field sort */}
          {definitions.length > 0 && (
            <>
              <div>
                <label className="text-xs font-medium text-gray-600 block mb-1">{t("leads.cfSort")}</label>
                <select
                  className="input input-sm"
                  value={cfSort}
                  onChange={(e) => { setCfSort(e.target.value); if (e.target.value) setSortBy("createdAt"); setPage(1); }}
                >
                  <option value="">— {t("leads.sortBy")} —</option>
                  {definitions.map(d => <option key={d.key} value={d.key}>{d.label}</option>)}
                </select>
              </div>
              {cfSort && (
                <div>
                  <label className="text-xs font-medium text-gray-600 block mb-1">{t("leads.cfSortDir")}</label>
                  <select className="input input-sm" value={cfSortDir} onChange={(e) => { setCfSortDir(e.target.value as "asc" | "desc"); setPage(1); }}>
                    <option value="asc">ASC</option>
                    <option value="desc">DESC</option>
                  </select>
                </div>
              )}
            </>
          )}
        </div>

        {/* Custom field filters */}
        {definitions.length > 0 && (
          <div>
            <div className="flex items-center gap-2 mb-2">
              <span className="text-xs font-medium text-gray-600">{t("leads.cfFilters")}</span>
              <button
                className="btn btn-secondary btn-sm text-xs"
                onClick={() => setFilterRows(prev => [...prev, { key: "", value: "" }])}
              >
                + {t("leads.addFilter")}
              </button>
              {(filterRows.length > 0 || Object.keys(appliedFilters).length > 0) && (
                <button className="btn btn-sm text-red-500 border-red-200 hover:bg-red-50 text-xs" onClick={clearFilters}>
                  {t("leads.clearFilters")}
                </button>
              )}
            </div>
            {filterRows.map((row, idx) => (
              <div key={idx} className="flex gap-2 mb-1 items-center">
                <select
                  className="input input-sm"
                  value={row.key}
                  onChange={(e) => {
                    const updated = [...filterRows];
                    updated[idx] = { ...updated[idx], key: e.target.value };
                    setFilterRows(updated);
                  }}
                >
                  <option value="">— field —</option>
                  {definitions.map(d => <option key={d.key} value={d.key}>{d.label}</option>)}
                </select>
                <input
                  className="input input-sm"
                  placeholder="value"
                  value={row.value}
                  onChange={(e) => {
                    const updated = [...filterRows];
                    updated[idx] = { ...updated[idx], value: e.target.value };
                    setFilterRows(updated);
                  }}
                />
                <button
                  className="btn btn-sm text-xs text-red-500 border-red-200"
                  onClick={() => setFilterRows(prev => prev.filter((_, i) => i !== idx))}
                >
                  {t("leads.removeFilter")}
                </button>
              </div>
            ))}
            {filterRows.length > 0 && (
              <button className="btn btn-primary btn-sm text-xs mt-1" onClick={applyFilters}>
                {t("leads.applyFilters")}
              </button>
            )}
          </div>
        )}
      </div>

      {/* ── Table ─────────────────────────────────────────────────────── */}
      {isLoading && <p className="text-gray-400 text-sm">{t("common.loading")}</p>}
      {isError && <p className="text-red-500 text-sm">Failed to load leads.</p>}

      {data && (
        <>
          <div className="overflow-x-auto">
            <table className="w-full text-sm border-collapse">
              <thead>
                <tr className="border-b text-left text-gray-500 text-xs uppercase">
                  <th className="py-2 pr-4">{t("leads.col.email")}</th>
                  <th className="py-2 pr-4">{t("leads.col.phone")}</th>
                  <th className="py-2 pr-4">{t("leads.col.source")}</th>
                  <th className="py-2 pr-4">{t("leads.col.score")}</th>
                  <th className="py-2 pr-4">{t("leads.col.priority")}</th>
                  <th className="py-2 pr-4">{t("leads.col.createdAt")}</th>
                  {cfCols.map(d => (
                    <th key={d.key} className="py-2 pr-4">{d.label}</th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {data.items.length === 0 && (
                  <tr>
                    <td colSpan={6 + cfCols.length} className="py-4 text-gray-400 text-sm">
                      {t("leads.empty")}
                    </td>
                  </tr>
                )}
                {data.items.map((lead) => (
                  <tr key={lead.id} className="border-b hover:bg-gray-50">
                    <td className="py-2 pr-4 font-mono text-xs">{lead.email ?? "—"}</td>
                    <td className="py-2 pr-4">{lead.phone ?? "—"}</td>
                    <td className="py-2 pr-4">{lead.source}</td>
                    <td className="py-2 pr-4 font-semibold">{lead.score}</td>
                    <td className="py-2 pr-4">
                      <span className={`badge text-xs ${PRIORITY_COLORS[lead.priority] ?? "badge-info"}`}>
                        {lead.priority}
                      </span>
                    </td>
                    <td className="py-2 pr-4 text-gray-500 text-xs">
                      {new Date(lead.createdAtUtc).toLocaleDateString()}
                    </td>
                    {cfCols.map(d => (
                      <td key={d.key} className="py-2 pr-4 text-xs">
                        {lead.customFields[d.key] ?? <span className="text-gray-300">—</span>}
                      </td>
                    ))}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {/* Pagination */}
          <div className="flex items-center gap-3 mt-3">
            <button
              className="btn btn-secondary btn-sm text-xs"
              onClick={() => setPage(p => Math.max(1, p - 1))}
              disabled={page === 1}
            >
              {t("leads.prevPage")}
            </button>
            <span className="text-sm text-gray-600">
              {page} / {Math.max(1, Math.ceil(data.total / 20))}
            </span>
            <button
              className="btn btn-secondary btn-sm text-xs"
              onClick={() => setPage(p => p + 1)}
              disabled={!data.hasMore}
            >
              {t("leads.nextPage")}
            </button>
          </div>
        </>
      )}
    </main>
  );
}
