namespace SyncChain.API.Models;

public static class AuditResultStatuses
{
    public const string Success = "SUCCESS";
    public const string Failed = "FAILED";

    public static readonly string[] All = { Success, Failed };
}
