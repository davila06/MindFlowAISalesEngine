import { NextRequest, NextResponse } from 'next/server';

export const dynamic = 'force-dynamic';

export async function GET(req: NextRequest) {
  const isStaticBuild =
    process.env.NEXT_EXPORT === 'true' ||
    process.env.NEXT_PHASE === 'phase-production-build';

  if (isStaticBuild) {
    return NextResponse.json([]);
  }

  try {
    const res = await fetch(`${process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5000'}/api/channels/messages`);
    const data = await res.json();
    return NextResponse.json(data);
  } catch {
    return NextResponse.json([]);
  }
}
