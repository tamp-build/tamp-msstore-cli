namespace Tamp.MicrosoftStoreCli;

/// <summary>
/// Settings for <c>msstore publish &lt;pathOrUrl&gt;</c> — the load-bearing verb. Submits an MSIX
/// (or any recognized project shape's bundle) to the Microsoft Store as a new submission, optionally
/// gated by rollout percentage and flight ring.
/// </summary>
/// <remarks>
/// <para>
/// The typical DasBook-style flow is: <c>SetPathOrUrl(projectRoot).SetInputFile(msix)</c>. The
/// <c>InputFile</c> path is required when the surrounding project shape is not auto-discoverable,
/// or when the adopter is producing the MSIX out-of-band (e.g. via Tamp.Msix's <c>Pack</c> verb
/// rather than through msstore-cli's own packaging).
/// </para>
/// <para>
/// <c>NoCommit</c> leaves the submission in Partner Center as a draft for human review before
/// publishing. <c>PackageRolloutPercentage</c> phases the rollout to a percentage of users (0-100);
/// can be ramped up incrementally via the <c>Submission.Rollout</c> verbs afterwards.
/// </para>
/// </remarks>
public sealed class MsStorePublishSettings : MsStoreSettingsBase
{
    /// <summary>Project root directory or PWA URL (positional argument).</summary>
    public string? PathOrUrl { get; set; }

    /// <summary>Path to the existing <c>.msix</c> or <c>.msixupload</c> (<c>--inputFile</c>). When set, msstore-cli skips the auto-packaging step and submits this artifact directly.</summary>
    public string? InputFile { get; set; }

    /// <summary>Override the application ID (<c>--appId</c>). Required only when the project was never initialized via <c>msstore init</c>.</summary>
    public string? AppId { get; set; }

    /// <summary>Leave the submission in draft state — don't commit (<c>--noCommit</c>). Adopters use this for Partner Center UI review before publishing.</summary>
    public bool NoCommit { get; set; }

    /// <summary>Specifies a flight ID for ring/insider rollouts (<c>--flightId</c>).</summary>
    public string? FlightId { get; set; }

    /// <summary>Rollout percentage 0-100 (<c>--packageRolloutPercentage</c>). Validated at <c>ToCommandPlan</c> time.</summary>
    public int? PackageRolloutPercentage { get; set; }

    public MsStorePublishSettings SetPathOrUrl(string p) { PathOrUrl = p; return this; }
    public MsStorePublishSettings SetInputFile(string path) { InputFile = path; return this; }
    public MsStorePublishSettings SetAppId(string id) { AppId = id; return this; }
    public MsStorePublishSettings SetNoCommit(bool v = true) { NoCommit = v; return this; }
    public MsStorePublishSettings SetFlightId(string id) { FlightId = id; return this; }
    public MsStorePublishSettings SetPackageRolloutPercentage(int pct) { PackageRolloutPercentage = pct; return this; }
    public MsStorePublishSettings SetVerbose(bool v = true) { Verbose = v; return this; }
    public MsStorePublishSettings SetWorkingDirectory(string? cwd) { WorkingDirectory = cwd; return this; }
    public MsStorePublishSettings SetEnvironmentVariable(string name, string value) { EnvironmentVariables[name] = value; return this; }

    protected override IEnumerable<string> Verb => new[] { "publish" };

    protected override void AppendArguments(List<string> args)
    {
        if (string.IsNullOrEmpty(PathOrUrl))
            throw new InvalidOperationException(
                "PathOrUrl is required for `msstore publish` — point at the project root, or at a PWA URL.");
        if (PackageRolloutPercentage is < 0 or > 100)
            throw new InvalidOperationException(
                $"PackageRolloutPercentage must be between 0 and 100; got {PackageRolloutPercentage}.");

        args.Add(PathOrUrl!);
        if (!string.IsNullOrEmpty(InputFile)) { args.Add("--inputFile"); args.Add(InputFile!); }
        if (!string.IsNullOrEmpty(AppId)) { args.Add("--appId"); args.Add(AppId!); }
        if (NoCommit) args.Add("--noCommit");
        if (!string.IsNullOrEmpty(FlightId)) { args.Add("--flightId"); args.Add(FlightId!); }
        if (PackageRolloutPercentage is int pct) { args.Add("--packageRolloutPercentage"); args.Add(pct.ToString()); }
    }
}
