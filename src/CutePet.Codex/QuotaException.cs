namespace CutePet.Codex;

public enum QuotaFailure
{
    MissingDependency, NeedsLogin, UnsupportedAuth, Timeout,
    ConnectionClosed, IncompatibleProtocol, ServiceError, AccountChanged
}

public sealed class QuotaException(QuotaFailure failure, string message, int? rpcCode = null)
    : Exception(message)
{
    public QuotaFailure Failure { get; } = failure;
    public int? RpcCode { get; } = rpcCode;
}
