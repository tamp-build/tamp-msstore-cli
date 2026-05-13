using System.IO.Compression;
using System.Net.Http;

namespace Tamp.MicrosoftStoreCli;

/// <summary>
/// Self-bootstrap helper for the <c>msstore-cli</c> binary on Windows.
/// Downloads + extracts the GitHub release zip when not already installed,
/// returning the resolved <see cref="AbsolutePath"/> to <c>msstore.exe</c>.
/// </summary>
/// <remarks>
/// <para>
/// Filed under TAM-199 from DasBook canary friction batch (2026-05-13). The
/// upstream <c>microsoft/msstore-cli</c> publishes only release archives —
/// no winget manifest exists, despite hints in older docs. Every Windows
/// adopter writes the same 4-line PowerShell snippet to download + extract
/// to a known path; this typed helper makes that step part of the Tamp
/// build graph so version drift is visible to CI.
/// </para>
/// <para>
/// Internal implementation; the public-facing entry point is
/// <see cref="MsStore.EnsureInstalled"/> which adds the platform gate +
/// path defaulting before delegating here.
/// </para>
/// </remarks>
internal static class MsStoreInstaller
{
    /// <summary>Default version this satellite is tested against (matches README pin).</summary>
    public const string DefaultVersion = "0.3.9";

    /// <summary>GitHub release asset name on Windows x64.</summary>
    public const string WindowsAssetName = "MSStoreCLI-win-x64.zip";

    private const string MarkerFileName = ".tamp-msstore-version";

    /// <summary>
    /// Idempotent install: if <paramref name="installDir"/> already contains
    /// <c>msstore.exe</c> + a marker file with matching <paramref name="version"/>,
    /// returns the binary path without any I/O. Otherwise downloads the
    /// release zip via <paramref name="httpClient"/>, extracts to
    /// <paramref name="installDir"/>, writes the marker, returns the path.
    /// </summary>
    public static AbsolutePath Install(
        string version,
        AbsolutePath installDir,
        HttpClient httpClient)
    {
        if (string.IsNullOrWhiteSpace(version))
            throw new ArgumentException("Version must be non-empty.", nameof(version));
        if (installDir is null) throw new ArgumentNullException(nameof(installDir));
        if (httpClient is null) throw new ArgumentNullException(nameof(httpClient));

        var binaryPath = installDir / "msstore.exe";
        var markerPath = installDir / MarkerFileName;

        // Idempotency — second invocation at the same version is a no-op.
        if (binaryPath.FileExists() && markerPath.FileExists())
        {
            var installed = markerPath.ReadAllText().Trim();
            if (installed == version) return binaryPath;
        }

        var url = $"https://github.com/microsoft/msstore-cli/releases/download/v{version}/{WindowsAssetName}";
        var tempZip = AbsolutePath.CreateTempFile(".zip");

        try
        {
            using (var response = httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead)
                .GetAwaiter().GetResult())
            {
                if (!response.IsSuccessStatusCode)
                    throw new InvalidOperationException(
                        $"Failed to download msstore-cli {version} from {url}: " +
                        $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}. " +
                        "Verify the version tag exists at https://github.com/microsoft/msstore-cli/releases.");

                using var fs = File.Create(tempZip.Value);
                response.Content.CopyToAsync(fs).GetAwaiter().GetResult();
            }

            // Wipe + recreate install dir so a partial previous install can't
            // leave stale executables around.
            installDir.DeleteDirectory(recursive: true);
            installDir.EnsureDirectoryExists();
            ZipFile.ExtractToDirectory(tempZip.Value, installDir.Value);
            markerPath.WriteAllText(version);
        }
        finally
        {
            try { File.Delete(tempZip.Value); } catch { /* best-effort cleanup */ }
        }

        if (!binaryPath.FileExists())
            throw new InvalidOperationException(
                $"msstore-cli {version} extracted to '{installDir}', but msstore.exe was not found. " +
                "The release asset layout may have changed upstream — open a TAM ticket and " +
                "fall back to the manual install snippet in the tamp-msstore-cli README.");

        return binaryPath;
    }

    /// <summary>
    /// Default install location per the tamp-msstore-cli README:
    /// <c>%LOCALAPPDATA%\Programs\msstore-cli</c> on Windows.
    /// </summary>
    public static AbsolutePath DefaultWindowsInstallDir() =>
        AbsolutePath.Create(System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Programs", "msstore-cli"));
}
