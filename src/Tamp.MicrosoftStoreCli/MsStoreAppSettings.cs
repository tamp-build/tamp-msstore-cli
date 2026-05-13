namespace Tamp.MicrosoftStoreCli;

// ────────────────────────────────────────────────────────────────────────────
//  apps list / apps get
// ────────────────────────────────────────────────────────────────────────────

/// <summary>Settings for <c>msstore apps list</c> — list all applications in your Partner Center account.</summary>
public sealed class MsStoreAppsListSettings : MsStoreSettingsBase
{
    public MsStoreAppsListSettings SetVerbose(bool v = true) { Verbose = v; return this; }
    public MsStoreAppsListSettings SetWorkingDirectory(string? cwd) { WorkingDirectory = cwd; return this; }

    protected override IEnumerable<string> Verb => new[] { "apps", "list" };
    protected override void AppendArguments(List<string> args) { /* no flags */ }
}

/// <summary>Settings for <c>msstore apps get &lt;productId&gt;</c>.</summary>
public sealed class MsStoreAppsGetSettings : MsStoreSettingsBase
{
    /// <summary>The Store product ID.</summary>
    public string? ProductId { get; set; }

    public MsStoreAppsGetSettings SetProductId(string productId) { ProductId = productId; return this; }
    public MsStoreAppsGetSettings SetVerbose(bool v = true) { Verbose = v; return this; }

    protected override IEnumerable<string> Verb => new[] { "apps", "get" };

    protected override void AppendArguments(List<string> args)
    {
        if (string.IsNullOrEmpty(ProductId))
            throw new InvalidOperationException("ProductId is required for `msstore apps get`.");
        args.Add(ProductId!);
    }
}

// ────────────────────────────────────────────────────────────────────────────
//  init / package — project-scaffolding verbs (less load-bearing for DasBook
//  since DasBook handles packaging via Tamp.Tauri.V2 + Tamp.Msix, but typed
//  for completeness)
// ────────────────────────────────────────────────────────────────────────────

/// <summary>Settings for <c>msstore init &lt;pathOrUrl&gt;</c>.</summary>
public sealed class MsStoreInitSettings : MsStoreSettingsBase
{
    /// <summary>Project path or PWA URL (positional argument).</summary>
    public string? PathOrUrl { get; set; }

    /// <summary>Publisher Display Name (<c>--publisherDisplayName</c>).</summary>
    public string? PublisherDisplayName { get; set; }

    /// <summary>Auto-pack the project (<c>--package</c>).</summary>
    public bool Package { get; set; }

    /// <summary>Auto-publish after package (<c>--publish</c>). Implies <see cref="Package"/>.</summary>
    public bool Publish { get; set; }

    /// <summary>Flight ID for the published package (<c>--flightId</c>).</summary>
    public string? FlightId { get; set; }

    /// <summary>Rollout percentage 0-100 (<c>--packageRolloutPercentage</c>).</summary>
    public int? PackageRolloutPercentage { get; set; }

    /// <summary>Architectures (<c>--arch</c>): x86, x64, arm64.</summary>
    public List<string> Architectures { get; } = new();

    /// <summary>Output directory (<c>--output</c>).</summary>
    public string? Output { get; set; }

    /// <summary>Version override (<c>--version</c>).</summary>
    public string? Version { get; set; }

    public MsStoreInitSettings SetPathOrUrl(string p) { PathOrUrl = p; return this; }
    public MsStoreInitSettings SetPublisherDisplayName(string n) { PublisherDisplayName = n; return this; }
    public MsStoreInitSettings SetPackage(bool v = true) { Package = v; return this; }
    public MsStoreInitSettings SetPublish(bool v = true) { Publish = v; return this; }
    public MsStoreInitSettings SetFlightId(string id) { FlightId = id; return this; }
    public MsStoreInitSettings SetPackageRolloutPercentage(int pct) { PackageRolloutPercentage = pct; return this; }
    public MsStoreInitSettings AddArchitecture(string arch) { Architectures.Add(arch); return this; }
    public MsStoreInitSettings AddArchitectures(params string[] arches) { Architectures.AddRange(arches); return this; }
    public MsStoreInitSettings SetOutput(string path) { Output = path; return this; }
    public MsStoreInitSettings SetVersion(string version) { Version = version; return this; }
    public MsStoreInitSettings SetVerbose(bool v = true) { Verbose = v; return this; }
    public MsStoreInitSettings SetWorkingDirectory(string? cwd) { WorkingDirectory = cwd; return this; }

    protected override IEnumerable<string> Verb => new[] { "init" };

    protected override void AppendArguments(List<string> args)
    {
        if (string.IsNullOrEmpty(PathOrUrl))
            throw new InvalidOperationException("PathOrUrl is required for `msstore init` (project path or PWA URL).");
        if (PackageRolloutPercentage is < 0 or > 100)
            throw new InvalidOperationException(
                $"PackageRolloutPercentage must be between 0 and 100; got {PackageRolloutPercentage}.");

        args.Add(PathOrUrl!);
        if (!string.IsNullOrEmpty(PublisherDisplayName)) { args.Add("--publisherDisplayName"); args.Add(PublisherDisplayName!); }
        if (Package) args.Add("--package");
        if (Publish) args.Add("--publish");
        if (!string.IsNullOrEmpty(FlightId)) { args.Add("--flightId"); args.Add(FlightId!); }
        if (PackageRolloutPercentage is int pct) { args.Add("--packageRolloutPercentage"); args.Add(pct.ToString()); }
        foreach (var a in Architectures) { args.Add("--arch"); args.Add(a); }
        if (!string.IsNullOrEmpty(Output)) { args.Add("--output"); args.Add(Output!); }
        if (!string.IsNullOrEmpty(Version)) { args.Add("--version"); args.Add(Version!); }
    }
}

/// <summary>Settings for <c>msstore package &lt;pathOrUrl&gt;</c> — produce an MSIX from a recognized project shape.</summary>
public sealed class MsStorePackageSettings : MsStoreSettingsBase
{
    public string? PathOrUrl { get; set; }
    public string? Output { get; set; }
    public List<string> Architectures { get; } = new();
    public string? Version { get; set; }

    public MsStorePackageSettings SetPathOrUrl(string p) { PathOrUrl = p; return this; }
    public MsStorePackageSettings SetOutput(string path) { Output = path; return this; }
    public MsStorePackageSettings AddArchitecture(string arch) { Architectures.Add(arch); return this; }
    public MsStorePackageSettings AddArchitectures(params string[] arches) { Architectures.AddRange(arches); return this; }
    public MsStorePackageSettings SetVersion(string version) { Version = version; return this; }
    public MsStorePackageSettings SetVerbose(bool v = true) { Verbose = v; return this; }
    public MsStorePackageSettings SetWorkingDirectory(string? cwd) { WorkingDirectory = cwd; return this; }

    protected override IEnumerable<string> Verb => new[] { "package" };

    protected override void AppendArguments(List<string> args)
    {
        if (string.IsNullOrEmpty(PathOrUrl))
            throw new InvalidOperationException("PathOrUrl is required for `msstore package`.");
        args.Add(PathOrUrl!);
        if (!string.IsNullOrEmpty(Output)) { args.Add("--output"); args.Add(Output!); }
        foreach (var a in Architectures) { args.Add("--arch"); args.Add(a); }
        if (!string.IsNullOrEmpty(Version)) { args.Add("--version"); args.Add(Version!); }
    }
}
