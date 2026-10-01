using System.Text.Json;

namespace CutePet.Core;

public sealed record QuotaWindow(string Kind, double? UsedPercent,
    double? RemainingPercent, int? WindowDurationMinutes, DateTimeOffset? ResetsAtUtc);

public sealed record QuotaBucket(string LimitId, string? DisplayName,
    string? PlanType, string? LimitReachedType, IReadOnlyList<QuotaWindow> Windows);

// This public model intentionally excludes email, account IDs, tokens and raw RPC payloads.
public sealed record QuotaSnapshot(DateTimeOffset LastSuccessfulSyncUtc,
    string? PlanType, bool? OrdinaryUsageAllowed, IReadOnlyList<QuotaBucket> Buckets);

public static class QuotaParser
{
    public static QuotaSnapshot Parse(JsonElement result, string? planType = null)
    {
        if (result.ValueKind != JsonValueKind.Object)
            throw new FormatException("额度响应不是对象。");
        var buckets = new List<QuotaBucket>();
        if (result.TryGetProperty("rateLimitsByLimitId", out var multiple)
            && multiple.ValueKind == JsonValueKind.Object)
        {
            foreach (var bucket in multiple.EnumerateObject())
                buckets.Add(ParseBucket(bucket.Value, bucket.Name, planType));
        }
        // Empty / unavailable multi-bucket views may still have a valid legacy bucket.
        if (buckets.Count == 0 && result.TryGetProperty("rateLimits", out var legacy)
            && legacy.ValueKind == JsonValueKind.Object)
            buckets.Add(ParseBucket(legacy, Text(legacy, "limitId") ?? "codex", planType));
        if (buckets.Count == 0)
            throw new FormatException("额度响应缺少可识别的额度桶。");

        bool? allowed = null;
        if (result.TryGetProperty("ordinaryUsageAllowed", out var permission)
            && permission.ValueKind is JsonValueKind.True or JsonValueKind.False)
            allowed = permission.GetBoolean();
        return new(DateTimeOffset.UtcNow, planType, allowed, buckets);
    }

    private static QuotaBucket ParseBucket(JsonElement value, string id, string? plan)
    {
        if (value.ValueKind != JsonValueKind.Object)
            throw new FormatException("额度桶不是对象。");
        var windows = new List<QuotaWindow>();
        foreach (var kind in new[] { "primary", "secondary" })
        {
            if (!value.TryGetProperty(kind, out var window) || window.ValueKind == JsonValueKind.Null)
                continue;
            if (window.ValueKind != JsonValueKind.Object)
                throw new FormatException("额度窗口不是对象。");
            double? used = null;
            if (window.TryGetProperty("usedPercent", out var percent)
                && percent.ValueKind == JsonValueKind.Number && percent.TryGetDouble(out var number)
                && double.IsFinite(number)) used = number;
            int? duration = null;
            if (window.TryGetProperty("windowDurationMins", out var minutes)
                && minutes.ValueKind == JsonValueKind.Number && minutes.TryGetInt32(out var length)
                && length > 0) duration = length;
            DateTimeOffset? reset = null;
            if (window.TryGetProperty("resetsAt", out var timestamp)
                && timestamp.ValueKind == JsonValueKind.Number && timestamp.TryGetInt64(out var seconds))
            {
                try { reset = DateTimeOffset.FromUnixTimeSeconds(seconds); }
                catch (ArgumentOutOfRangeException) { /* Unknown timestamps remain unknown. */ }
            }
            windows.Add(new(kind, used, used is null ? null : Math.Clamp(100 - used.Value, 0, 100),
                duration, reset));
        }
        return new(id, Text(value, "limitName"), Text(value, "planType") ?? plan,
            Text(value, "rateLimitReachedType"), windows);
    }

    public static string? Text(JsonElement value, string property) =>
        value.ValueKind == JsonValueKind.Object && value.TryGetProperty(property, out var field)
        && field.ValueKind == JsonValueKind.String ? field.GetString() : null;
}
