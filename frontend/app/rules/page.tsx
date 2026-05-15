import { RuleBuilderPanel } from "@/components/rules/RuleBuilderPanel";
import { RuleTable } from "@/components/rules/RuleTable";

export default function RulesPage() {
  return (
    <section className="grid">
      <RuleBuilderPanel />
      <RuleTable />
    </section>
  );
}