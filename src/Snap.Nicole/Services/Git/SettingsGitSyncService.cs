using Snap.Nicole.Core.IO;
using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Snap.Nicole.Services.Git;

internal sealed class SettingsGitSyncService : ISettingsGitSyncService
{
    private const string GitFileName = "git";
    private const string RemoteName = "origin";
    private const string DefaultBranchName = "main";
    private const string CommitAuthorName = "Snap Nicole";
    private const string CommitAuthorEmail = "snap-nicole@local";

    private static readonly string[] BlockingFiles = ["CHERRY_PICK_HEAD", "MERGE_HEAD", "REBASE_HEAD", "REVERT_HEAD", "index.lock"];

    public string RepositoryPath { get => WellKnownLocations.Settings; }

    public async Task<SettingsGitRepositoryState> GetRepositoryStateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            Directory.CreateDirectory(RepositoryPath);

            SettingsGitCommandResult version = await RunGitAsync(["--version"], null, cancellationToken);
            if (version.GitUnavailable)
            {
                return new()
                {
                    FailureKind = SettingsGitFailureKind.GitUnavailable,
                    Detail = version.NormalizedCombinedOutput,
                };
            }

            if (!version.Succeeded)
            {
                return new()
                {
                    FailureKind = version.FailureKind,
                    Detail = version.NormalizedCombinedOutput,
                };
            }

            SettingsGitCommandResult topLevel = await RunGitAsync(["rev-parse", "--show-toplevel"], RepositoryPath, cancellationToken);
            if (!topLevel.Succeeded)
            {
                string dotGit = Path.Combine(RepositoryPath, ".git");
                SettingsGitFailureKind failureKind = (Directory.Exists(dotGit) || File.Exists(dotGit)) ? topLevel.FailureKind : SettingsGitFailureKind.None;

                return new()
                {
                    IsGitAvailable = true,
                    GitVersion = version.NormalizedOutput,
                    FailureKind = failureKind,
                    Detail = topLevel.NormalizedCombinedOutput,
                };
            }

            if (!string.Equals(
                Path.GetFullPath(RepositoryPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                Path.GetFullPath(topLevel.Output.Trim()).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                StringComparison.OrdinalIgnoreCase))
            {
                return new()
                {
                    IsGitAvailable = true,
                    GitVersion = version.NormalizedOutput,
                };
            }

            SettingsGitCommandResult remote = await RunGitAsync(["remote", "get-url", RemoteName], RepositoryPath, cancellationToken);
            return new()
            {
                IsGitAvailable = true,
                IsRepository = true,
                GitVersion = version.NormalizedOutput,
                RemoteUrl = remote.Succeeded ? remote.NormalizedOutput : null,
                FailureKind = remote.Succeeded ? SettingsGitFailureKind.None : SettingsGitFailureKind.Remote,
                Detail = remote.NormalizedCombinedOutput,
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new()
            {
                FailureKind = SettingsGitFailureKind.Permission,
                Detail = ex.Message,
            };
        }
    }

    public async Task<SettingsGitOperationResult> InitializeRepositoryAsync(string remoteUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(remoteUrl))
        {
            return SettingsGitOperationResult.Failure(SettingsGitFailureKind.Remote);
        }

        try
        {
            Directory.CreateDirectory(RepositoryPath);

            SettingsGitRepositoryState state = await GetRepositoryStateAsync(cancellationToken);
            if (!state.IsGitAvailable)
            {
                return SettingsGitOperationResult.Failure(SettingsGitFailureKind.GitUnavailable, state.Detail);
            }

            if (!state.IsRepository)
            {
                if (await RunGitAsync(["init"], RepositoryPath, cancellationToken) is { Succeeded: false } init)
                {
                    return SettingsGitOperationResult.CommandFailure(init);
                }

                if (await RunGitAsync(["symbolic-ref", "HEAD", $"refs/heads/{DefaultBranchName}"], RepositoryPath, cancellationToken) is { Succeeded: false} setDefaultBranch)
                {
                    return SettingsGitOperationResult.CommandFailure(setDefaultBranch);
                }
            }

            SettingsGitCommandResult existingRemote = await RunGitAsync(["remote", "get-url", RemoteName], RepositoryPath, cancellationToken);
            SettingsGitCommandResult configureRemote = existingRemote.Succeeded
                ? await RunGitAsync(["remote", "set-url", RemoteName, remoteUrl.Trim()], RepositoryPath, cancellationToken)
                : await RunGitAsync(["remote", "add", RemoteName, remoteUrl.Trim()], RepositoryPath, cancellationToken);

            return SettingsGitOperationResult.Command(configureRemote);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return SettingsGitOperationResult.Failure(SettingsGitFailureKind.Permission, ex.Message);
        }
    }

    public async Task<SettingsGitOperationResult> PullAsync(CancellationToken cancellationToken = default)
    {
        if (await EnsureRepositoryReadyAsync(cancellationToken) is { Succeeded: false } ready)
        {
            return ready;
        }

        if (EnsureNoRepositoryOperationInProgress() is { Succeeded: false } repositoryOperation)
        {
            return repositoryOperation;
        }

        if (await CommitSettingsChangesAsync("Save settings before pull", cancellationToken) is { Succeeded: false } commit)
        {
            return commit;
        }

        SettingsGitCommandResult remoteHeads = await RunGitAsync(["ls-remote", "--heads", RemoteName], RepositoryPath, cancellationToken);
        if (!remoteHeads.Succeeded)
        {
            return SettingsGitOperationResult.CommandFailure(remoteHeads);
        }

        if (string.IsNullOrWhiteSpace(remoteHeads.Output))
        {
            return SettingsGitOperationResult.Success("RemoteEmpty");
        }

        SettingsGitCommandResult upstream = await RunGitAsync(["rev-parse", "--abbrev-ref", "--symbolic-full-name", "@{u}"], RepositoryPath, cancellationToken);
        SettingsGitCommandResult pull = upstream.Succeeded
            ? await RunGitAsync(["pull", "--rebase", "--autostash"], RepositoryPath, cancellationToken)
            : await PullFromDefaultRemoteBranchAsync(cancellationToken);

        return SettingsGitOperationResult.Command(pull);
    }

    public async Task<SettingsGitOperationResult> PushAsync(CancellationToken cancellationToken = default)
    {
        if (await EnsureRepositoryReadyAsync(cancellationToken) is { Succeeded: false } ready)
        {
            return ready;
        }

        if (EnsureNoRepositoryOperationInProgress() is { Succeeded: false } repositoryOperation)
        {
            return repositoryOperation;
        }

        if (await CommitSettingsChangesAsync("Sync settings", cancellationToken) is { Succeeded: false} commit)
        {
            return commit;
        }

        if (await RunGitAsync(["rev-parse", "--verify", "HEAD"], RepositoryPath, cancellationToken) is { Succeeded: false})
        {
            return SettingsGitOperationResult.Success("NoLocalCommits");
        }

        SettingsGitCommandResult upstream = await RunGitAsync(["rev-parse", "--abbrev-ref", "--symbolic-full-name", "@{u}"], RepositoryPath, cancellationToken);
        SettingsGitCommandResult push = upstream.Succeeded
            ? await RunGitAsync(["push"], RepositoryPath, cancellationToken)
            : await RunGitAsync(["push", "-u", RemoteName, "HEAD"], RepositoryPath, cancellationToken);

        return SettingsGitOperationResult.Command(push);
    }

    private async Task<SettingsGitOperationResult> EnsureRepositoryReadyAsync(CancellationToken cancellationToken)
    {
        SettingsGitRepositoryState state = await GetRepositoryStateAsync(cancellationToken);
        if (!state.IsGitAvailable)
        {
            return SettingsGitOperationResult.Failure(SettingsGitFailureKind.GitUnavailable, state.Detail);
        }

        if (!state.IsRepository)
        {
            return SettingsGitOperationResult.Failure(SettingsGitFailureKind.Repository, state.Detail);
        }

        if (string.IsNullOrWhiteSpace(state.RemoteUrl))
        {
            return SettingsGitOperationResult.Failure(SettingsGitFailureKind.Remote, state.Detail);
        }

        return SettingsGitOperationResult.Success();
    }

    private SettingsGitOperationResult EnsureNoRepositoryOperationInProgress()
    {
        if (!TryGetGitDirectory(out string gitDirectory))
        {
            return SettingsGitOperationResult.Failure(SettingsGitFailureKind.Repository);
        }

        if (BlockingFiles.Any(fileName => File.Exists(Path.Combine(gitDirectory, fileName))) ||
            Directory.Exists(Path.Combine(gitDirectory, "rebase-apply")) ||
            Directory.Exists(Path.Combine(gitDirectory, "rebase-merge")))
        {
            return SettingsGitOperationResult.Failure(SettingsGitFailureKind.Repository, "RepositoryOperationInProgress");
        }

        return SettingsGitOperationResult.Success();
    }

    private async Task<SettingsGitOperationResult> CommitSettingsChangesAsync(string message, CancellationToken cancellationToken)
    {
        if (await RunGitAsync(["add", "--all", "--force"], RepositoryPath, cancellationToken) is { Succeeded: false} add)
        {
            return SettingsGitOperationResult.CommandFailure(add);
        }

        SettingsGitCommandResult diff = await RunGitAsync(["diff", "--cached", "--quiet"], RepositoryPath, cancellationToken);
        if (diff.ExitCode is 0)
        {
            return SettingsGitOperationResult.Success("NoChanges");
        }

        if (diff.ExitCode is not 1)
        {
            return SettingsGitOperationResult.CommandFailure(diff);
        }

        SettingsGitCommandResult commit = await RunGitAsync(["-c", $"user.name={CommitAuthorName}", "-c", $"user.email={CommitAuthorEmail}", "commit", "-m", message], RepositoryPath, cancellationToken);
        return SettingsGitOperationResult.Command(commit, "Committed");
    }

    private async Task<SettingsGitCommandResult> PullFromDefaultRemoteBranchAsync(CancellationToken cancellationToken)
    {
        string remoteBranch = await GetDefaultRemoteBranchAsync(cancellationToken);
        SettingsGitCommandResult pull = await RunGitAsync(["pull", "--rebase", "--autostash", RemoteName, remoteBranch], RepositoryPath, cancellationToken);
        if (!pull.Succeeded)
        {
            return pull;
        }

        SettingsGitCommandResult currentBranch = await RunGitAsync(["branch", "--show-current"], RepositoryPath, cancellationToken);
        if (currentBranch.Succeeded && string.Equals(currentBranch.Output.Trim(), remoteBranch, StringComparison.Ordinal))
        {
            _ = await RunGitAsync(["branch", "--set-upstream-to", $"{RemoteName}/{remoteBranch}"], RepositoryPath, cancellationToken);
        }

        return pull;
    }

    private async Task<string> GetDefaultRemoteBranchAsync(CancellationToken cancellationToken)
    {
        SettingsGitCommandResult remoteHead = await RunGitAsync(["ls-remote", "--symref", RemoteName, "HEAD"], RepositoryPath, cancellationToken);
        if (remoteHead.Succeeded)
        {
            foreach (string line in remoteHead.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                const string Prefix = "ref: refs/heads/";
                if (line.StartsWith(Prefix, StringComparison.Ordinal))
                {
                    int branchNameEnd = line.IndexOf('\t', Prefix.Length);
                    return branchNameEnd >= 0 ? line[Prefix.Length..branchNameEnd] : line[Prefix.Length..];
                }
            }
        }

        SettingsGitCommandResult currentBranch = await RunGitAsync(["branch", "--show-current"], RepositoryPath, cancellationToken);
        string branchName = currentBranch.Output.Trim();
        return string.IsNullOrWhiteSpace(branchName) ? DefaultBranchName : branchName;
    }

    private async Task<SettingsGitCommandResult> RunGitAsync(string[] arguments, string? workingDirectory, CancellationToken cancellationToken)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = GitFileName,
            WorkingDirectory = workingDirectory ?? RepositoryPath,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        try
        {
            using (Process process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start git."))
            {
                try
                {
                    await process.WaitForExitAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    try
                    {
                        if (!process.HasExited)
                        {
                            process.Kill(true);
                        }
                    }
                    catch (InvalidOperationException)
                    {
                    }
                    catch (Win32Exception)
                    {
                    }

                    await process.WaitForExitAsync(CancellationToken.None);
                    throw;
                }

                return new()
                {
                    ExitCode = process.ExitCode,
                    Output = await process.StandardOutput.ReadToEndAsync(cancellationToken),
                    Error = await process.StandardError.ReadToEndAsync(cancellationToken),
                };
            }
        }
        catch (Win32Exception ex)
        {
            return new()
            {
                ExitCode = -1,
                Output = string.Empty,
                Error = ex.Message,
                GitUnavailable = true,
            };
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            return new()
            {
                ExitCode = -1,
                Output = string.Empty,
                Error = ex.Message,
            };
        }
    }

    private bool TryGetGitDirectory(out string gitDirectory)
    {
        string dotGit = Path.Combine(RepositoryPath, ".git");
        if (Directory.Exists(dotGit))
        {
            gitDirectory = dotGit;
            return true;
        }

        if (!File.Exists(dotGit))
        {
            gitDirectory = string.Empty;
            return false;
        }

        try
        {
            string content = File.ReadAllText(dotGit).Trim();
            const string Prefix = "gitdir:";
            if (!content.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
            {
                gitDirectory = string.Empty;
                return false;
            }

            string path = content[Prefix.Length..].Trim();
            gitDirectory = Path.GetFullPath(path, RepositoryPath);
            return Directory.Exists(gitDirectory);
        }
        catch (IOException)
        {
            gitDirectory = string.Empty;
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            gitDirectory = string.Empty;
            return false;
        }
    }
}
