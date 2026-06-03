namespace Snap.Nicole.Services.Git;

internal sealed record SettingsGitOperationResult
{
    public bool Succeeded { get; init; }

    public SettingsGitFailureKind FailureKind { get; init; }

    public SettingsGitOperationDetailKind DetailKind { get; init; }

    public string Detail { get; init; } = string.Empty;

    public static SettingsGitOperationResult Success()
    {
        return new()
        {
            Succeeded = true,
            FailureKind = SettingsGitFailureKind.None,
        };
    }

    public static SettingsGitOperationResult Success(SettingsGitOperationDetailKind detailKind)
    {
        return new()
        {
            Succeeded = true,
            FailureKind = SettingsGitFailureKind.None,
            DetailKind = detailKind,
        };
    }

    public static SettingsGitOperationResult Failure(SettingsGitFailureKind failureKind)
    {
        return Failure(failureKind, string.Empty);
    }

    public static SettingsGitOperationResult Failure(SettingsGitFailureKind failureKind, string detail)
    {
        return new()
        {
            Succeeded = false,
            FailureKind = failureKind,
            Detail = detail,
        };
    }

    public static SettingsGitOperationResult Failure(SettingsGitFailureKind failureKind, SettingsGitOperationDetailKind detailKind)
    {
        return new()
        {
            Succeeded = false,
            FailureKind = failureKind,
            DetailKind = detailKind,
        };
    }

    public static SettingsGitOperationResult Command(SettingsGitCommandResult result)
    {
        return result.Succeeded ? Success() : Failure(result.FailureKind, result.NormalizedCombinedOutput);
    }

    public static SettingsGitOperationResult Command(SettingsGitCommandResult result, SettingsGitOperationDetailKind detailKind)
    {
        return result.Succeeded ? Success(detailKind) : Failure(result.FailureKind, result.NormalizedCombinedOutput);
    }

    public static SettingsGitOperationResult CommandFailure(SettingsGitCommandResult result)
    {
        return Failure(result.FailureKind, result.NormalizedCombinedOutput);
    }
}
