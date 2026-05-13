namespace Tamp.MicrosoftStoreCli;

/// <summary>
/// Common knobs shared across every <c>msstore</c> subcommand: working directory,
/// environment variables, the global <c>-v / --verbose</c> flag.
/// </summary>
public abstract class MsStoreSettingsBase
{
    /// <summary>Working directory for the spawned msstore process.</summary>
    public string? WorkingDirectory { get; set; }

    /// <summary>Per-invocation environment variables.</summary>
    public Dictionary<string, string> EnvironmentVariables { get; } = new();

    /// <summary>Emit verbose output (<c>-v</c>). Default off; flip on for CI logs.</summary>
    public bool Verbose { get; set; }

    /// <summary>Subclasses override to return their verb tokens (e.g. <c>["publish"]</c>, <c>["submission", "get"]</c>).</summary>
    protected abstract IEnumerable<string> Verb { get; }

    /// <summary>Subclasses override to append their flag-specific args.</summary>
    protected abstract void AppendArguments(List<string> args);

    /// <summary>Subclasses may override to provide Secrets that are revealed at exec time
    /// and masked in the printed CommandPlan trace.</summary>
    protected virtual IReadOnlyList<Secret> CollectSecrets() => Array.Empty<Secret>();

    internal CommandPlan ToCommandPlan(Tool tool)
    {
        if (tool is null) throw new ArgumentNullException(nameof(tool));

        var args = new List<string>(Verb);
        AppendArguments(args);
        if (Verbose) args.Add("-v");

        return new CommandPlan
        {
            Executable = tool.Executable.Value,
            Arguments = args,
            Environment = new Dictionary<string, string>(EnvironmentVariables),
            WorkingDirectory = WorkingDirectory ?? tool.WorkingDirectory,
            Secrets = CollectSecrets(),
        };
    }
}

// ────────────────────────────────────────────────────────────────────────────
//  Reconfigure / Info / Settings — global / one-shot configuration verbs
// ────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Settings for <c>msstore reconfigure</c> — configure the CLI with Partner Center credentials
/// for non-interactive (CI) use.
/// </summary>
/// <remarks>
/// Authenticate against Partner Center via Entra ID service principal. The standard CI shape
/// is <c>TenantId</c> + <c>SellerId</c> + <c>ClientId</c> + <c>ClientSecret</c>; alternatively
/// a certificate (thumbprint OR file path + password) can be supplied.
/// </remarks>
public sealed class MsStoreReconfigureSettings : MsStoreSettingsBase
{
    /// <summary>Entra ID tenant ID (<c>--tenantId</c>).</summary>
    public string? TenantId { get; set; }

    /// <summary>Partner Center seller ID (<c>--sellerId</c>).</summary>
    public string? SellerId { get; set; }

    /// <summary>Service-principal client ID (<c>--clientId</c>).</summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// Service-principal client secret (<c>--clientSecret</c>). Modeled as a <see cref="Secret"/>
    /// so the value is masked in the printed CommandPlan trace and is not logged.
    /// Mutually exclusive with the certificate-based selectors.
    /// </summary>
    public Secret? ClientSecret { get; set; }

    /// <summary>Certificate thumbprint (<c>--certificateThumbprint</c>). Mutually exclusive with <see cref="ClientSecret"/> and <see cref="CertificateFilePath"/>.</summary>
    public string? CertificateThumbprint { get; set; }

    /// <summary>Certificate file path (<c>--certificateFilePath</c>). Mutually exclusive with <see cref="ClientSecret"/> and <see cref="CertificateThumbprint"/>.</summary>
    public string? CertificateFilePath { get; set; }

    /// <summary>Certificate password (<c>--certificatePassword</c>), used only with <see cref="CertificateFilePath"/>. <see cref="Secret"/>-typed so it's masked.</summary>
    public Secret? CertificatePassword { get; set; }

    /// <summary>Reset only the credentials, without re-running the configuration wizard (<c>--reset</c>).</summary>
    public bool Reset { get; set; }

    public MsStoreReconfigureSettings SetTenantId(string id) { TenantId = id; return this; }
    public MsStoreReconfigureSettings SetSellerId(string id) { SellerId = id; return this; }
    public MsStoreReconfigureSettings SetClientId(string id) { ClientId = id; return this; }
    public MsStoreReconfigureSettings SetClientSecret(Secret secret) { ClientSecret = secret; return this; }
    public MsStoreReconfigureSettings SetCertificateThumbprint(string thumbprint) { CertificateThumbprint = thumbprint; return this; }
    public MsStoreReconfigureSettings SetCertificateFilePath(string path) { CertificateFilePath = path; return this; }
    public MsStoreReconfigureSettings SetCertificatePassword(Secret pwd) { CertificatePassword = pwd; return this; }
    public MsStoreReconfigureSettings SetReset(bool v = true) { Reset = v; return this; }
    public MsStoreReconfigureSettings SetVerbose(bool v = true) { Verbose = v; return this; }
    public MsStoreReconfigureSettings SetWorkingDirectory(string? cwd) { WorkingDirectory = cwd; return this; }
    public MsStoreReconfigureSettings SetEnvironmentVariable(string name, string value) { EnvironmentVariables[name] = value; return this; }

    protected override IEnumerable<string> Verb => new[] { "reconfigure" };

    protected override IReadOnlyList<Secret> CollectSecrets()
    {
        var list = new List<Secret>();
        if (ClientSecret is not null) list.Add(ClientSecret);
        if (CertificatePassword is not null) list.Add(CertificatePassword);
        return list;
    }

    protected override void AppendArguments(List<string> args)
    {
        var hasSecret = ClientSecret is not null;
        var hasThumbprint = !string.IsNullOrEmpty(CertificateThumbprint);
        var hasCertFile = !string.IsNullOrEmpty(CertificateFilePath);
        var authPathsSet = (hasSecret ? 1 : 0) + (hasThumbprint ? 1 : 0) + (hasCertFile ? 1 : 0);
        if (authPathsSet > 1)
            throw new InvalidOperationException(
                "ClientSecret, CertificateThumbprint, and CertificateFilePath are mutually exclusive — pick exactly one auth path.");
        if (!string.IsNullOrEmpty(CertificateThumbprint) && CertificatePassword is not null)
            throw new InvalidOperationException(
                "CertificatePassword applies only to CertificateFilePath; remove it or switch to file-based selection.");

        if (!string.IsNullOrEmpty(TenantId)) { args.Add("--tenantId"); args.Add(TenantId!); }
        if (!string.IsNullOrEmpty(SellerId)) { args.Add("--sellerId"); args.Add(SellerId!); }
        if (!string.IsNullOrEmpty(ClientId)) { args.Add("--clientId"); args.Add(ClientId!); }
        if (ClientSecret is not null) { args.Add("--clientSecret"); args.Add(ClientSecret.Reveal()); }
        if (!string.IsNullOrEmpty(CertificateThumbprint)) { args.Add("--certificateThumbprint"); args.Add(CertificateThumbprint!); }
        if (!string.IsNullOrEmpty(CertificateFilePath)) { args.Add("--certificateFilePath"); args.Add(CertificateFilePath!); }
        if (CertificatePassword is not null) { args.Add("--certificatePassword"); args.Add(CertificatePassword.Reveal()); }
        if (Reset) args.Add("--reset");
    }
}

/// <summary>Settings for <c>msstore info</c> — print the active CLI configuration.</summary>
public sealed class MsStoreInfoSettings : MsStoreSettingsBase
{
    public MsStoreInfoSettings SetVerbose(bool v = true) { Verbose = v; return this; }
    public MsStoreInfoSettings SetWorkingDirectory(string? cwd) { WorkingDirectory = cwd; return this; }

    protected override IEnumerable<string> Verb => new[] { "info" };
    protected override void AppendArguments(List<string> args) { /* no extra flags */ }
}

/// <summary>Settings for <c>msstore settings setpdn &lt;publisherDisplayName&gt;</c> — set the global Publisher Display Name used by <c>init</c>.</summary>
public sealed class MsStoreSetPdnSettings : MsStoreSettingsBase
{
    public string? PublisherDisplayName { get; set; }

    public MsStoreSetPdnSettings SetPublisherDisplayName(string name) { PublisherDisplayName = name; return this; }
    public MsStoreSetPdnSettings SetVerbose(bool v = true) { Verbose = v; return this; }

    protected override IEnumerable<string> Verb => new[] { "settings", "setpdn" };

    protected override void AppendArguments(List<string> args)
    {
        if (string.IsNullOrEmpty(PublisherDisplayName))
            throw new InvalidOperationException("PublisherDisplayName is required for `msstore settings setpdn`.");
        args.Add(PublisherDisplayName!);
    }
}
