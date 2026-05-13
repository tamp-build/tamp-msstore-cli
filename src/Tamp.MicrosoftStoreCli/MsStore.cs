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

    // ── apps directory ─────────────────────────────────────────────────────

    /// <summary>Nested verbs under <c>msstore apps</c>.</summary>
    public static class Apps
    {
        public static CommandPlan List(Tool tool, Action<MsStoreAppsListSettings>? configure = null)
            => Run<MsStoreAppsListSettings>(tool, configure);

        public static CommandPlan Get(Tool tool, Action<MsStoreAppsGetSettings> configure)
            => Run<MsStoreAppsGetSettings>(tool, configure);
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
}
