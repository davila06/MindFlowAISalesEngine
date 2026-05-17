import type { NextApiRequest, NextApiResponse } from "next";

export default function handler(req: NextApiRequest, res: NextApiResponse) {
  if (req.method !== "POST") {
    res.status(405).json({ error: "Method not allowed" });
    return;
  }

  // In real prod: persist to analytics store or forward to DataDog/AppInsights
  // For demo: just log
  try {
    const event = req.body;
    // eslint-disable-next-line no-console
    console.log("[API UX TELEMETRY]", event);
    res.status(204).end();
  } catch (err) {
    res.status(400).json({ error: "Invalid payload" });
  }
}
