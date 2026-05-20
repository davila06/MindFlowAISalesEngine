import { NextRequest, NextResponse } from "next/server";

export const dynamic = "force-dynamic";

const defaultSmtpSettings = {
  providerType: "smtp",
  providerBaseUrl: "",
  apiKey: "",
  host: "",
  port: 587,
  username: "",
  password: "",
  fromEmail: "",
  fromName: "",
  enableSsl: true
};

function getBackendBaseUrl() {
  return process.env.NEXT_PUBLIC_API_URL ?? process.env.API_BASE_URL ?? "http://localhost:5156";
}

export async function GET() {
  const isStaticBuild =
    process.env.NEXT_EXPORT === "true" ||
    process.env.NEXT_PHASE === "phase-production-build";

  if (isStaticBuild) {
    return NextResponse.json(defaultSmtpSettings);
  }

  try {
    const response = await fetch(`${getBackendBaseUrl()}/api/email/smtp-settings`, {
      cache: "no-store"
    });

    if (response.status === 404) {
      return NextResponse.json(defaultSmtpSettings);
    }

    if (!response.ok) {
      const body = await response.text();
      return new NextResponse(body || "Upstream request failed", {
        status: response.status
      });
    }

    const data = await response.json();
    return NextResponse.json({ ...defaultSmtpSettings, ...data, password: "", apiKey: "" });
  } catch {
    return NextResponse.json(defaultSmtpSettings);
  }
}

export async function PUT(request: NextRequest) {
  try {
    const payload = await request.json();
    const response = await fetch(`${getBackendBaseUrl()}/api/email/smtp-settings`, {
      method: "PUT",
      headers: {
        "Content-Type": "application/json"
      },
      body: JSON.stringify(payload)
    });

    const bodyText = await response.text();
    if (!response.ok) {
      return new NextResponse(bodyText || "Upstream request failed", {
        status: response.status
      });
    }

    if (!bodyText.trim()) {
      return NextResponse.json({ ...defaultSmtpSettings, ...payload, password: "", apiKey: "" });
    }

    const data = JSON.parse(bodyText);
    return NextResponse.json({ ...defaultSmtpSettings, ...data, password: "", apiKey: "" });
  } catch {
    return NextResponse.json({ message: "Invalid request payload" }, { status: 400 });
  }
}
