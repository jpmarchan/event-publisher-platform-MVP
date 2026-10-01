namespace EventService.Application.Idempotency;

public sealed class IdempotencyKeyReusedException(string key)
    : Exception($"La Idempotency-Key '{key}' ya se usó con un payload distinto.");

public sealed class DuplicateIdempotencyKeyException(string key, Exception innerException)
    : Exception($"La Idempotency-Key '{key}' fue registrada por otra request concurrente.", innerException);
