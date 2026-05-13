# Tamp.MicrosoftStoreCli

> Wrapper for [`msstore-cli`](https://github.com/microsoft/msstore-cli) — Microsoft's official, cross-platform Microsoft Store Developer CLI. Automates Partner Center submissions so shipping an MSIX upgrade is a typed step in the Tamp build graph rather than a Partner Center web-UI ritual. Final stage of the DasBook-style Rust → Tauri → MSIX → Microsoft Store ship chain.

| Package | Status |
|---|---|
| `Tamp.MicrosoftStoreCli` | 0.1.0 (initial) |

## Why this exists

Every Microsoft Store developer who's done it knows: the Partner Center submission UI is genuinely painful. For an MSIX upgrade you click through "remove old package → save → add new package → set rollout → submit", once per release, by hand, in a browser. The CLI exists for exactly this case. It's just been a third-party-to-install tool you had to glue into your build.

`Tamp.MicrosoftStoreCli` makes the Microsoft Store submission a typed verb in the build graph:

- `MsStore.Reconfigure(...)` to set service-principal auth (Partner Center client-secret is a `Tamp.Core.Secret` so it's masked in command-line traces).
- `MsStore.Publish(...)` to submit a built MSIX as a new submission — supports rollout %, flight ring, draft mode (`NoCommit`).
- Fine-grain `MsStore.Submission.{Get, Update, Publish, Poll, Status, Delete}` for adopters who want explicit lifecycle control.
- Full `MsStore.Flights.*` surface for ring/insider deploys.

Together with `Tamp.Cargo` + `Tamp.Tauri.V2` + `Tamp.Msix`, the entire "Rust app → Microsoft Store published submission" path becomes one `dotnet run -- Publish --version 1.0.6`. No bash. No web UI. No tribal-knowledge memo.

## Install

```bash
dotnet add package Tamp.MicrosoftStoreCli
```

Multi-targets net8 / net9 / net10. Requires `Tamp.Core` ≥ **1.5.1**. (1.5.1 originally granted `InternalsVisibleTo` for `Secret.Reveal()`; as of Tamp.Core **1.6.0** the IVT grant was made redundant by making `Reveal()` public + TAMP004-gated. Existing 1.5.1 dependency continues working unchanged.)

## Tool installation

Install `msstore-cli` on each CI runner — distribution channels:

- **Windows:** **direct GitHub release zip** — `msstore-cli` is *not* on winget (confirmed against `microsoft/msstore-cli` v0.3.9 — the upstream publishes only release archives, no winget manifest). The one-liner DasBook adoption used:
  ```powershell
  $dest = "$env:LOCALAPPDATA\Programs\msstore-cli"
  Invoke-WebRequest "https://github.com/microsoft/msstore-cli/releases/download/v0.3.9/MSStoreCLI-win-x64.zip" -OutFile "$env:TEMP\msstore.zip"
  Expand-Archive "$env:TEMP\msstore.zip" -DestinationPath $dest
  [Environment]::SetEnvironmentVariable("PATH", "$([Environment]::GetEnvironmentVariable('PATH','User'));$dest", "User")
  ```
- **macOS:** `brew install microsoft/msstore-cli/msstore-cli` or `.tar.gz` from [releases](https://github.com/microsoft/msstore-cli/releases)
- **Linux:** `brew install microsoft/msstore-cli/msstore-cli` or `.tar.gz`

> **Pin the version.** msstore-cli is officially labeled "(preview)" and the verb surface can shift between minor versions. Pin the GitHub release tag (`v0.3.9` shown above) on Windows; use `brew install msstore-cli@<x.y.z>` on macOS / Linux. CI runners should download a fixed release tag rather than `latest`. Tamp.MicrosoftStoreCli 0.1.0 is built against the msstore-cli `0.3.9` (January 2026) verb shape.

## Quick start — full DasBook-style ship pipeline

```csharp
using Tamp;
using Tamp.Cargo;
using Tamp.Tauri.V2;
using Tamp.Msix;
using Tamp.MicrosoftStoreCli;

class Build : TampBuild
{
    public static int Main(string[] args) => Execute<Build>(args);

    [Parameter] readonly string Version = "1.0.6";
    [Parameter] readonly string ProductId = "9P53PC5S0PHJ";  // your Partner Center app ID
    [Parameter] readonly int RolloutPercent = 25;            // 25% gradual rollout to start

    [FromPath("cargo")] readonly Tool CargoBin = null!;
    [FromPath("makeappx")] readonly Tool MakeAppx = null!;
    [FromPath("msstore")]  readonly Tool MsStoreCli = null!;
    [FromNodeModules("tauri")] readonly Tool TauriCli = null!;

    [Secret] readonly Secret PartnerCenterClientSecret = null!;

    AbsolutePath SrcTauri     => RootDirectory / "src-tauri";
    AbsolutePath ServiceCrate => RootDirectory / "dasbook-service";
    AbsolutePath StagingDir   => RootDirectory / "msix-package";
    AbsolutePath AppxManifest => StagingDir / "AppxManifest.xml";
    AbsolutePath Artifacts    => RootDirectory / "artifacts";
    AbsolutePath MsixOut      => Artifacts / $"DasBook_{Msix.NormalizeMsixVersion(Version)}_x64.msix";

    const string TargetTriple = "x86_64-pc-windows-msvc";

    Target ConfigureMsStore => _ => _
        .Description("[Publish] One-time Partner Center auth on this runner")
        .Executes(() => MsStore.Reconfigure(MsStoreCli, s => s
            .SetTenantId(Environment.GetEnvironmentVariable("PARTNER_CENTER_TENANT_ID"))
            .SetSellerId(Environment.GetEnvironmentVariable("PARTNER_CENTER_SELLER_ID"))
            .SetClientId(Environment.GetEnvironmentVariable("PARTNER_CENTER_CLIENT_ID"))
            .SetClientSecret(PartnerCenterClientSecret)));

    Target StampVersion => _ => _
        .Executes(() => Msix.SetAppxManifestVersion(AppxManifest, Version));

    Target BuildService => _ => _
        .Executes(() => Cargo.Build(CargoBin, s => s
            .SetWorkingDirectory(ServiceCrate)
            .SetRelease().SetTarget(TargetTriple).SetLocked()));

    Target StageSidecar => _ => _
        .DependsOn(nameof(BuildService))
        .Executes(() =>
        {
            var built = ServiceCrate / "target" / TargetTriple / "release" / "dasbook-service.exe";
            var sidecar = Tauri.ExternalBinPath(SrcTauri, "dasbook-service", TargetTriple);
            Directory.CreateDirectory(sidecar.Parent!.Value);
            File.Copy(built.Value, sidecar.Value, overwrite: true);
        });

    Target BuildDesktop => _ => _
        .DependsOn(nameof(StageSidecar))
        .Executes(() => Tauri.Build(TauriCli, s => s
            .AddBundles("msi", "nsis").SetTarget(TargetTriple)));

    Target StageDesktopExe => _ => _
        .DependsOn(nameof(BuildDesktop))
        .Executes(() => File.Copy(
            (SrcTauri / "target" / TargetTriple / "release" / "dasbook2.exe").Value,
            (StagingDir / "DasBook.exe").Value, overwrite: true));

    Target PackMsix => _ => _
        .DependsOn(nameof(StampVersion), nameof(StageDesktopExe))
        .Executes(() => { Directory.CreateDirectory(Artifacts.Value); return Msix.Pack(MakeAppx, s => s
            .SetSourceDirectory(StagingDir).SetOutputPackage(MsixOut)); });

    // ── The actual publish step. One Tamp target replaces the entire web-UI flow. ──
    Target PublishToStore => _ => _
        .DependsOn(nameof(ConfigureMsStore), nameof(PackMsix))
        .Description("[Publish] Submit MSIX as upgrade to Microsoft Store with gradual rollout")
        .Executes(() => MsStore.Publish(MsStoreCli, s => s
            .SetPathOrUrl(RootDirectory)
            .SetInputFile(MsixOut)
            .SetAppId(ProductId)
            .SetPackageRolloutPercentage(RolloutPercent)));

    // Optional: bump rollout to 100% later (after monitoring crash-free %)
    Target FinalizeRollout => _ => _
        .Description("[Publish] Bump rollout to 100% — call from a separate manual run after monitoring")
        .Executes(() => MsStore.Submission.Update(MsStoreCli, s => s
            .SetProductId(ProductId)
            .SetPackage("{\"packageRollout\":{\"packageRolloutPercentage\":100}}")));
}
```

Run `dotnet tamp PublishToStore --version 1.0.6 --rolloutPercent 25` and the entire chain executes: cargo build → stage sidecar → tauri build → stage desktop exe → pack MSIX → configure auth → submit to Store. The browser tab never opens.

## Verb surface

### Authentication / config

| Tamp method | msstore verb | Notes |
|---|---|---|
| `MsStore.Reconfigure(...)` | `msstore reconfigure` | Service-principal credentials. ClientSecret + CertificatePassword are `Secret`-typed so they don't leak in CommandPlan trace. Auth selectors (ClientSecret / CertificateThumbprint / CertificateFilePath) are mutually exclusive — validated at `ToCommandPlan` time. |
| `MsStore.Info(...)` | `msstore info` | Diagnostic. Useful early step in CI logs. |
| `MsStore.SetPdn(...)` | `msstore settings setpdn` | Set global Publisher Display Name. |

### Primary verb

| Tamp method | msstore verb | Notes |
|---|---|---|
| `MsStore.Publish(...)` | `msstore publish` | Submit MSIX. `InputFile` is the upgrade-existing-MSIX path. `NoCommit` for draft; `PackageRolloutPercentage` for gradual rollout (validated 0-100); `FlightId` for ring deploys. |

### Submission lifecycle (fine-grain)

`MsStore.Submission.{Status, Get, GetListingAssets, UpdateMetadata, Update, Poll, Publish, Delete}` — the canonical "get → mutate JSON → update → publish → poll" flow when you want explicit control over the submission record.

### Flights (ring deploys)

`MsStore.Flights.{List, Get, Create, Delete, Submission.*, Submission.Rollout.{Get, Update, Halt, Finalize}}` — ship a flight to insiders first, ramp rollout %, optionally halt and roll back.

### Apps directory

`MsStore.Apps.{List, Get}` — query Partner Center for the app catalog. Useful for "find the product ID by name" startup steps.

### Project init / package

`MsStore.Init(...)` / `MsStore.Package(...)` — for projects that haven't been initialized for the Store yet. Covered for completeness; for DasBook-style chains the packaging is handled upstream by `Tamp.Msix`.

### Escape hatch

`MsStore.Raw(tool, "anything", "you", "want")` — for verbs not yet typed.

## Secrets handling

`Reconfigure` accepts `Secret`-typed `ClientSecret` and `CertificatePassword`. Internally these are revealed at command-line emission time and the values are listed in the `CommandPlan.Secrets` collection so Tamp's process trace masks them in printed output. Same shape as `Tamp.AzureCli.V2`'s access-token handling.

`Tamp.MicrosoftStoreCli` was added to `Tamp.Core`'s `InternalsVisibleTo` list in 1.5.1 — but as of Tamp.Core **1.6.0** the IVT gate was retired in favor of the [TAMP004 analyzer](https://github.com/tamp-build/tamp/wiki/Tamp-Analyzers#tamp004). The wrapper's `*Settings` classes naturally fall under TAMP004's approved-context heuristic, so no special access grant is required for newer Tamp.Core versions. The 1.5.1 minimum dependency stays for back-compat.

## Mutually-exclusive option enforcement

`Reconfigure` rejects calls that set more than one cert selector — `InvalidOperationException` at `ToCommandPlan(...)` time, not after the slow tool launches. Catches the "I switched from password to thumbprint mid-edit and left both flags set" class of bug at build-graph compile time.

## CI auth setup (GitHub Actions / Azure DevOps)

The official patterns from msstore-cli's docs translate cleanly:

```yaml
# GitHub Actions — store the four values as encrypted secrets
- name: Setup Microsoft Store Developer CLI
  uses: microsoft/microsoft-store-apppublisher@v1.1

- name: Tamp ship-to-store
  env:
    PARTNER_CENTER_TENANT_ID: ${{ secrets.PARTNER_CENTER_TENANT_ID }}
    PARTNER_CENTER_SELLER_ID: ${{ secrets.PARTNER_CENTER_SELLER_ID }}
    PARTNER_CENTER_CLIENT_ID: ${{ secrets.PARTNER_CENTER_CLIENT_ID }}
    PARTNER_CENTER_CLIENT_SECRET: ${{ secrets.PARTNER_CENTER_CLIENT_SECRET }}
  run: dotnet run --project build -- PublishToStore --version ${{ github.ref_name }}
```

## Sibling packages

- [`Tamp.Cargo`](https://github.com/tamp-build/tamp-cargo) — Rust toolchain. Top of the DasBook chain.
- [`Tamp.Tauri.V2`](https://github.com/tamp-build/tamp-tauri) — Tauri 2.x desktop builds + externalBin sidecar path.
- [`Tamp.Msix`](https://github.com/tamp-build/tamp-msix) — `makeappx` + `signtool` + AppxManifest version helpers. Produces the MSIX that `MsStore.Publish` submits.

## Releasing

Releases follow the [Tamp dogfood pattern](MAINTAINERS.md): bump `<Version>` in `Directory.Build.props`, tag `v<X.Y.Z>`, GitHub Actions runs `dotnet tamp Ci` then `dotnet tamp Push`.

## License

MIT. See [LICENSE](LICENSE).
