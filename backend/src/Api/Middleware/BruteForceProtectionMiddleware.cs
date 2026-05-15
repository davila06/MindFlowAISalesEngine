using System.Collections.Concurrent;
using Api.Contracts;

namespace Api.Middleware;

public sealed class BruteForceProtectionMiddleware
{
    private const int Threshold = 5;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(10);

    private readonly RequestDelegate _next;
    private static readonly ConcurrentDictionary<string, AttemptCounter> Counters = new();

    public BruteForceProtectionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var method = context.Request.Method;

        if (!HttpMethods.IsPut(method)
            || !path.Equals("/api/email/smtp-settings", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var tenant = context.Request.Headers["X-Tenant-Id"].ToString();
        if (string.IsNullOrWhiteSpace(tenant))
        {
            tenant = "default";
        }

        var key = tenant.Trim().ToLowerInvariant();
        var counter = Counters.GetOrAdd(key, _ => new AttemptCounter());
        var blocked = false;

        lock (counter)
        {
            if (DateTime.UtcNow - counter.WindowStartUtc > Window)
            {
                counter.WindowStartUtc = DateTime.UtcNow;
                counter.FailedAttempts = 0;
            }

            if (counter.FailedAttempts >= Threshold)
            {
                blocked = true;
            }
        }

        if (blocked)
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await context.Response.WriteAsJsonAsync(new ApiErrorResponse
            {
                Code = "brute_force_blocked",
                Message = "Too many failed attempts for SMTP settings. Try again later.",
                TraceId = context.TraceIdentifier
            });
            return;
        }

        await _next(context);

        if (context.Response.StatusCode is StatusCodes.Status400BadRequest
            or StatusCodes.Status401Unauthorized
            or StatusCodes.Status403Forbidden)
        {
            lock (counter)
            {
                counter.FailedAttempts++;
            }
        }
    }

    private sealed class AttemptCounter
    {
        public int FailedAttempts { get; set; }
        public DateTime WindowStartUtc { get; set; } = DateTime.UtcNow;
    }
}
