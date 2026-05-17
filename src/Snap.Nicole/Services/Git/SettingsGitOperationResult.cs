namespace Snap.Nicole.Services.Git;

internal sealed record SettingsGitOperationResult
{
    public bool Succeeded { get; init; }

    public SettingsGitFailureKind FailureKind { get; init; }

    public string Detail { get; init; } = string.Empty;

    public static SettingsGitOperationResult Success()
    {
        return Success(string.Empty);
    }

    public static SettingsGitOperationResult Success(string detail)
    {
        return new()
        {
            Succeeded = true,
            FailureKind = SettingsGitFailureKind.None,
            Detail = detail,
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

    public static SettingsGitOperationResult Command(SettingsGitCommandResult result)
    {
        return result.Succeeded ? Success() : Failure(result.FailureKind, result.NormalizedCombinedOutput);
    }

    public static SettingsGitOperationResult Command(SettingsGitCommandResult result, string detail)
    {
        return result.Succeeded ? Success(detail) : Failure(result.FailureKind, result.NormalizedCombinedOutput);
    }

    public static SettingsGitOperationResult CommandFailure(SettingsGitCommandResult result)
    {
        return Failure(result.FailureKind, result.NormalizedCombinedOutput);
    }
}
