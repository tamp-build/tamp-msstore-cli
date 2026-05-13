# Changelog

All notable changes to **Tamp.MicrosoftStoreCli** are recorded here.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/);
versions follow [SemVer](https://semver.org/spec/v2.0.0.html).

## [0.1.0] - 2026-05-13

### Added

- Initial release. Wraps the Microsoft Store Developer CLI
  ([microsoft/msstore-cli](https://github.com/microsoft/msstore-cli), v0.3.9
  baseline). Filed under TAM-192. Final stage of the DasBook-style Rust →
  Tauri → MSIX → Microsoft Store ship chain, slotting downstream of
  `Tamp.Msix`.

#### Authentication

- **`MsStore.Reconfigure(...)`** — `msstore reconfigure`. Three auth selectors,
  mutually exclusive:
  - `SetClientSecret(Secret)` — typical CI shape, service principal +
    client secret. `Secret`-typed so the value is masked in `CommandPlan` trace.
  - `SetCertificateThumbprint(string)` — cert from machine certificate store.
  - `SetCertificateFilePath(string)` + optional `SetCertificatePassword(Secret)` —
    PFX file path; password is `Secret`-typed.

  Auth-selector mutual exclusion is enforced at `ToCommandPlan(...)` time.

- **Requires Tamp.Core ≥ 1.5.1.** The 1.5.1 patch grants
  `InternalsVisibleTo` for `Tamp.MicrosoftStoreCli` so the wrapper can
  `Reveal()` cert/secret values into the `--clientSecret` /
  `--certificatePassword` arguments. Tamp.Core 1.5.0 will not work — the
  package depends on `[1.5.1,)`.

#### Primary verb

- **`MsStore.Publish(...)`** — `msstore publish`. The load-bearing verb:
  - `SetPathOrUrl(string)` — project root or PWA URL (required).
  - `SetInputFile(string)` — path to existing `.msix` / `.msixupload`; this is
    the "upgrade an MSIX as a new submission" path.
  - `SetAppId(string)` — Partner Center product ID override.
  - `SetNoCommit(bool)` — leave the submission in draft state.
  - `SetFlightId(string)` — ring/insider rollout.
  - `SetPackageRolloutPercentage(int)` — gradual rollout %, validated 0-100
    at `ToCommandPlan` time.

#### Submission lifecycle (fine-grain)

- **`MsStore.Submission.{Status, Get, GetListingAssets, UpdateMetadata, Update, Poll, Publish, Delete}`** —
  the canonical "get → mutate JSON → update → publish → poll" flow. `Delete`
  defaults `--no-confirm` ON for CI safety.

#### Flights (ring deploys)

- **`MsStore.Flights.{List, Get, Create, Delete}`** — flight management.
  `Create` validates that at least one group ID is supplied (Partner Center
  requires it).
- **`MsStore.Flights.Submission.{Get, Delete, Update, Publish, Poll, Status}`** —
  flight-specific submission lifecycle.
- **`MsStore.Flights.Submission.Rollout.{Get, Update, Halt, Finalize}`** —
  gradual flight rollout knobs. `Update` validates percentage 0-100.

#### Apps directory + project verbs

- **`MsStore.Apps.{List, Get}`** — Partner Center app catalog queries.
- **`MsStore.Init(...)`** / **`MsStore.Package(...)`** — project scaffolding /
  packaging for shapes msstore-cli understands natively (WinUI, .NET MAUI,
  Flutter, Electron, React Native for Desktop, PWA, UWP). DasBook-style
  Tauri ship chains do their own packaging via `Tamp.Msix`; these verbs are
  here for adopters whose project shape is auto-discoverable.

#### Info / settings / raw

- **`MsStore.Info(...)`** — diagnostic snapshot.
- **`MsStore.SetPdn(...)`** — global Publisher Display Name.
- **`MsStore.Raw(...)`** — escape hatch.

### Validation surface

- Auth selector mutual exclusion (`ClientSecret` ⊕ `CertificateThumbprint` ⊕ `CertificateFilePath`).
- `CertificatePassword` requires `CertificateFilePath` to also be set.
- `PackageRolloutPercentage` 0-100 on both `Publish` and
  `Flights.Submission.Rollout.Update`.
- Required positional arguments (`ProductId`, `FlightId`, `PathOrUrl`,
  `PublisherDisplayName`, etc.) checked at `ToCommandPlan` time with
  helpful messages that name the missing setter.

### Tests

- 43 unit tests covering positive verb-shape paths plus negative cases
  (mutual exclusion, range validation, missing-required-arg, all surfaces
  except true CLI execution).

### Notes

- **msstore-cli is officially "(preview)"** at the time of this writing.
  Adopters should pin a specific CLI version in CI (`winget install --version`
  / `brew install msstore-cli@x.y.z`) rather than tracking latest. The wrapper
  is built against and tested against the v0.3.9 verb surface (January 2026).

- Fourth non-.NET satellite, following `Tamp.Cargo`, `Tamp.Tauri.V2`,
  `Tamp.Msix`. Closes the DasBook ship chain: every stage from `cargo build`
  to "submission live in Microsoft Store" is a typed step in the build graph
  — no Partner Center web UI required.
