namespace Tamp.MicrosoftStoreCli;

/// <summary>
/// Top-level facade for the Microsoft Store Developer CLI (<c>msstore-cli</c>). Wraps every
/// shipped verb so Partner Center / Microsoft Store submission becomes a typed step in the
/// Tamp build graph rather than a Partner Center web-UI ritual.
/// </summary>
/// <remarks>
/// <para>
/// <b>Tool resolution:</b>
/// <code>
/// [FromPath("msstore")] readonly Tool MsStoreCli = null!;
/// </code>
/// msstore-cli is distributed via winget / brew / .tar.gz extract. Pin the version in CI
/// (<c>winget install --version</c> / <c>brew install msstore-cli@x.y.z</c>) — the CLI is
/// officially "(preview)" and surface can shift between minor versions.
/// </para>
/// <para>
/// <b>First-time setup:</b> run <c>MsStore.Reconfigure(...)</c> once per CI runner — the CLI
/// persists configuration on disk and subsequent verbs read it. The reconfigure step is the
/// only one that takes secrets (Partner Center service-principal client secret / cert
/// password); modeled as <see cref="Secret"/> so values are masked in the CommandPlan trace.
/// </para>
/// </remarks>
public static class MsStore
{
    // ── self-bootstrap (TAM-199) ─────────────────────────────────────────

    /// <summary>
    /// Ensure <c>msstore-cli</c> is installed at <paramref name="installDir"/>,
    /// downloading + extracting the GitHub release zip if it isn't already
    /// at <paramref name="version"/>. Returns the <see cref="AbsolutePath"/>
    /// to <c>msstore.exe</c> — pipe that into <see cref="Tool.Create"/> to
    /// wire it as the tool argument of <see cref="Publish"/>, <see cref="Reconfigure"/>,
    /// etc.
    /// </summary>
    /// <param name="version">
    /// The msstore-cli release tag to install (without the leading <c>v</c>).
    /// Defaults to the version this satellite is tested against.
    /// </param>
    /// <param name="installDir">
    /// Override for the install location. Defaults to
    /// <c>%LOCALAPPDATA%\Programs\msstore-cli</c> per the upstream README convention.
    /// </param>
    /// <param name="httpClient">
    /// Optional pre-configured client (e.g. for proxy / retry policies in CI).
    /// A fresh <see cref="HttpClient"/> is used when null.
    /// </param>
    /// <remarks>
    /// <para>
    /// Idempotent — if the marker file under <paramref name="installDir"/> matches
    /// <paramref name="version"/> and the binary is present, returns immediately
    /// without any I/O.
    /// </para>
    /// <para>
    /// <b>Windows-only</b> at the runtime layer — msstore-cli does ship for
    /// macOS / Linux but the recommended install path on those platforms is
    /// <c>brew install microsoft/msstore-cli/msstore-cli</c>, which Tamp does
    /// not displace (per the "build-chain manager, not a build tool" creed —
    /// brew already owns this use case). On non-Windows, throw
    /// <see cref="PlatformNotSupportedException"/>; adopters resolve via
    /// <c>[FromPath("msstore")]</c> after brew-installing.
    /// </para>
    /// </remarks>
    public static AbsolutePath EnsureInstalled(
        string? version = null,
        AbsolutePath? installDir = null,
        System.Net.Http.HttpClient? httpClient = null)
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException(
                "MsStore.EnsureInstalled supports Windows only. On macOS / Linux use " +
                "`brew install microsoft/msstore-cli/msstore-cli` and resolve via " +
                "[FromPath(\"msstore\")].");

        version ??= MsStoreInstaller.DefaultVersion;
        installDir ??= MsStoreInstaller.DefaultWindowsInstallDir();
        var ownsClient = httpClient is null;
        httpClient ??= new System.Net.Http.HttpClient();
        try
        {
            return MsStoreInstaller.Install(version, installDir, httpClient);
        }
        finally
        {
            if (ownsClient) httpClient.Dispose();
        }
    }

    // ── one-shot / config ────────────────────────────────────────────────

    /// <summary><c>msstore reconfigure</c> — supply Partner Center credentials. Run once per CI runner.</summary>
    public static CommandPlan Reconfigure(Tool tool, Action<MsStoreReconfigureSettings> configure)
        => Run<MsStoreReconfigureSettings>(tool, configure);

    /// <summary><c>msstore info</c> — print existing configuration. Useful first step in CI logs.</summary>
    public static CommandPlan Info(Tool tool, Action<MsStoreInfoSettings>? configure = null)
        => Run<MsStoreInfoSettings>(tool, configure);

    /// <summary><c>msstore settings setpdn &lt;name&gt;</c> — set the global Publisher Display Name.</summary>
    public static CommandPlan SetPdn(Tool tool, Action<MsStoreSetPdnSettings> configure)
        => Run<MsStoreSetPdnSettings>(tool, configure);

    // ── primary verb ──────────────────────────────────────────────────────

    /// <summary>
    /// <c>msstore publish</c> — the load-bearing verb. Submit an MSIX (typically produced by
    /// <c>Tamp.Msix</c>) as a new Microsoft Store submission, optionally as a draft, optionally
    /// gated by rollout percentage or flight ID.
    /// </summary>
    public static CommandPlan Publish(Tool tool, Action<MsStorePublishSettings> configure)
        => Run<MsStorePublishSettings>(tool, configure);

    // ── project-scaffolding (less load-bearing for Tauri-based ship chains) ─

    public static CommandPlan Init(Tool tool, Action<MsStoreInitSettings> configure)
        => Run<MsStoreInitSettings>(tool, configure);

    public static CommandPlan Package(Tool tool, Action<MsStorePackageSettings> configure)
        => Run<MsStorePackageSettings>(tool, configure);

    // ---- Object-init overloads (TAM-161) for top-level verbs ----
    public static CommandPlan Reconfigure(Tool tool, MsStoreReconfigureSettings settings) => Plan(tool, settings);
    public static CommandPlan Info(Tool tool, MsStoreInfoSettings settings) => Plan(tool, settings);
    public static CommandPlan SetPdn(Tool tool, MsStoreSetPdnSettings settings) => Plan(tool, settings);
    public static CommandPlan Publish(Tool tool, MsStorePublishSettings settings) => Plan(tool, settings);
    public static CommandPlan Init(Tool tool, MsStoreInitSettings settings) => Plan(tool, settings);
    public static CommandPlan Package(Tool tool, MsStorePackageSettings settings) => Plan(tool, settings);

    // ── apps directory ─────────────────────────────────────────────────────

    /// <summary>Nested verbs under <c>msstore apps</c>.</summary>
    public static class Apps
    {
        public static CommandPlan List(Tool tool, Action<MsStoreAppsListSettings>? configure = null)
            => Run<MsStoreAppsListSettings>(tool, configure);

        public static CommandPlan Get(Tool tool, Action<MsStoreAppsGetSettings> configure)
            => Run<MsStoreAppsGetSettings>(tool, configure);

        // ---- Object-init overloads (TAM-161) ----
        public static CommandPlan List(Tool tool, MsStoreAppsListSettings settings) => Plan(tool, settings);
        public static CommandPlan Get(Tool tool, MsStoreAppsGetSettings settings) => Plan(tool, settings);
    }

    // ── submission lifecycle ────────────────────────────────────────────────

    /// <summary>Nested verbs under <c>msstore submission</c>.</summary>
    public static class Submission
    {
        public static CommandPlan Status(Tool tool, Action<MsStoreSubmissionStatusSettings> configure)
            => Run<MsStoreSubmissionStatusSettings>(tool, configure);

        public static CommandPlan Get(Tool tool, Action<MsStoreSubmissionGetSettings> configure)
            => Run<MsStoreSubmissionGetSettings>(tool, configure);

        public static CommandPlan GetListingAssets(Tool tool, Action<MsStoreSubmissionGetListingAssetsSettings> configure)
            => Run<MsStoreSubmissionGetListingAssetsSettings>(tool, configure);

        public static CommandPlan UpdateMetadata(Tool tool, Action<MsStoreSubmissionUpdateMetadataSettings> configure)
            => Run<MsStoreSubmissionUpdateMetadataSettings>(tool, configure);

        public static CommandPlan Update(Tool tool, Action<MsStoreSubmissionUpdateSettings> configure)
            => Run<MsStoreSubmissionUpdateSettings>(tool, configure);

        public static CommandPlan Poll(Tool tool, Action<MsStoreSubmissionPollSettings> configure)
            => Run<MsStoreSubmissionPollSettings>(tool, configure);

        public static CommandPlan Publish(Tool tool, Action<MsStoreSubmissionPublishSettings> configure)
            => Run<MsStoreSubmissionPublishSettings>(tool, configure);

        public static CommandPlan Delete(Tool tool, Action<MsStoreSubmissionDeleteSettings> configure)
            => Run<MsStoreSubmissionDeleteSettings>(tool, configure);

        // ---- Object-init overloads (TAM-161) ----
        public static CommandPlan Status(Tool tool, MsStoreSubmissionStatusSettings settings) => Plan(tool, settings);
        public static CommandPlan Get(Tool tool, MsStoreSubmissionGetSettings settings) => Plan(tool, settings);
        public static CommandPlan GetListingAssets(Tool tool, MsStoreSubmissionGetListingAssetsSettings settings) => Plan(tool, settings);
        public static CommandPlan UpdateMetadata(Tool tool, MsStoreSubmissionUpdateMetadataSettings settings) => Plan(tool, settings);
        public static CommandPlan Update(Tool tool, MsStoreSubmissionUpdateSettings settings) => Plan(tool, settings);
        public static CommandPlan Poll(Tool tool, MsStoreSubmissionPollSettings settings) => Plan(tool, settings);
        public static CommandPlan Publish(Tool tool, MsStoreSubmissionPublishSettings settings) => Plan(tool, settings);
        public static CommandPlan Delete(Tool tool, MsStoreSubmissionDeleteSettings settings) => Plan(tool, settings);
    }

    // ── flights (ring deploys) + flight submission lifecycle + rollout ──────

    public static class Flights
    {
        public static CommandPlan List(Tool tool, Action<MsStoreFlightsListSettings> configure)
            => Run<MsStoreFlightsListSettings>(tool, configure);

        public static CommandPlan Get(Tool tool, Action<MsStoreFlightsGetSettings> configure)
            => Run<MsStoreFlightsGetSettings>(tool, configure);

        public static CommandPlan Delete(Tool tool, Action<MsStoreFlightsDeleteSettings> configure)
            => Run<MsStoreFlightsDeleteSettings>(tool, configure);

        public static CommandPlan Create(Tool tool, Action<MsStoreFlightsCreateSettings> configure)
            => Run<MsStoreFlightsCreateSettings>(tool, configure);

        // ---- Object-init overloads (TAM-161) ----
        public static CommandPlan List(Tool tool, MsStoreFlightsListSettings settings) => Plan(tool, settings);
        public static CommandPlan Get(Tool tool, MsStoreFlightsGetSettings settings) => Plan(tool, settings);
        public static CommandPlan Delete(Tool tool, MsStoreFlightsDeleteSettings settings) => Plan(tool, settings);
        public static CommandPlan Create(Tool tool, MsStoreFlightsCreateSettings settings) => Plan(tool, settings);

        public static class Submission
        {
            public static CommandPlan Get(Tool tool, Action<MsStoreFlightSubmissionGetSettings> configure)
                => Run<MsStoreFlightSubmissionGetSettings>(tool, configure);

            public static CommandPlan Delete(Tool tool, Action<MsStoreFlightSubmissionDeleteSettings> configure)
                => Run<MsStoreFlightSubmissionDeleteSettings>(tool, configure);

            public static CommandPlan Update(Tool tool, Action<MsStoreFlightSubmissionUpdateSettings> configure)
                => Run<MsStoreFlightSubmissionUpdateSettings>(tool, configure);

            public static CommandPlan Publish(Tool tool, Action<MsStoreFlightSubmissionPublishSettings> configure)
                => Run<MsStoreFlightSubmissionPublishSettings>(tool, configure);

            public static CommandPlan Poll(Tool tool, Action<MsStoreFlightSubmissionPollSettings> configure)
                => Run<MsStoreFlightSubmissionPollSettings>(tool, configure);

            public static CommandPlan Status(Tool tool, Action<MsStoreFlightSubmissionStatusSettings> configure)
                => Run<MsStoreFlightSubmissionStatusSettings>(tool, configure);

            // ---- Object-init overloads (TAM-161) ----
            public static CommandPlan Get(Tool tool, MsStoreFlightSubmissionGetSettings settings) => Plan(tool, settings);
            public static CommandPlan Delete(Tool tool, MsStoreFlightSubmissionDeleteSettings settings) => Plan(tool, settings);
            public static CommandPlan Update(Tool tool, MsStoreFlightSubmissionUpdateSettings settings) => Plan(tool, settings);
            public static CommandPlan Publish(Tool tool, MsStoreFlightSubmissionPublishSettings settings) => Plan(tool, settings);
            public static CommandPlan Poll(Tool tool, MsStoreFlightSubmissionPollSettings settings) => Plan(tool, settings);
            public static CommandPlan Status(Tool tool, MsStoreFlightSubmissionStatusSettings settings) => Plan(tool, settings);

            public static class Rollout
            {
                public static CommandPlan Get(Tool tool, Action<MsStoreFlightRolloutGetSettings> configure)
                    => Run<MsStoreFlightRolloutGetSettings>(tool, configure);

                public static CommandPlan Update(Tool tool, Action<MsStoreFlightRolloutUpdateSettings> configure)
                    => Run<MsStoreFlightRolloutUpdateSettings>(tool, configure);

                public static CommandPlan Halt(Tool tool, Action<MsStoreFlightRolloutHaltSettings> configure)
                    => Run<MsStoreFlightRolloutHaltSettings>(tool, configure);

                public static CommandPlan Finalize(Tool tool, Action<MsStoreFlightRolloutFinalizeSettings> configure)
                    => Run<MsStoreFlightRolloutFinalizeSettings>(tool, configure);

                // ---- Object-init overloads (TAM-161) ----
                public static CommandPlan Get(Tool tool, MsStoreFlightRolloutGetSettings settings) => Plan(tool, settings);
                public static CommandPlan Update(Tool tool, MsStoreFlightRolloutUpdateSettings settings) => Plan(tool, settings);
                public static CommandPlan Halt(Tool tool, MsStoreFlightRolloutHaltSettings settings) => Plan(tool, settings);
                public static CommandPlan Finalize(Tool tool, MsStoreFlightRolloutFinalizeSettings settings) => Plan(tool, settings);
            }
        }
    }

    // ── escape hatch ────────────────────────────────────────────────────────

    /// <summary>Raw escape hatch for verbs not (yet) typed.</summary>
    public static CommandPlan Raw(Tool tool, params string[] arguments)
    {
        if (tool is null) throw new ArgumentNullException(nameof(tool));
        if (arguments is null || arguments.Length == 0)
            throw new ArgumentException("Raw requires at least one argument.", nameof(arguments));
        return new CommandPlan
        {
            Executable = tool.Executable.Value,
            Arguments = arguments.ToList(),
            Environment = new Dictionary<string, string>(),
            WorkingDirectory = tool.WorkingDirectory,
            Secrets = Array.Empty<Secret>(),
        };
    }

    private static CommandPlan Run<T>(Tool tool, Action<T>? configure) where T : MsStoreSettingsBase, new()
    {
        if (tool is null) throw new ArgumentNullException(nameof(tool));
        var s = new T();
        configure?.Invoke(s);
        return s.ToCommandPlan(tool);
    }

    // Shared object-init plan-builder used by all of the nested facades above
    // (Apps / Submission / Flights / Flights.Submission / Flights.Submission.Rollout).
    // Nested static classes can call enclosing-class private members directly.
    private static CommandPlan Plan<T>(Tool tool, T settings) where T : MsStoreSettingsBase
    {
        if (tool is null) throw new ArgumentNullException(nameof(tool));
        if (settings is null) throw new ArgumentNullException(nameof(settings));
        return settings.ToCommandPlan(tool);
    }
}
