using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Tamp.MicrosoftStoreCli.Tests;

/// <summary>
/// Tests for the msstore-cli auto-installer (TAM-199). Uses an injected
/// <see cref="HttpClient"/> with a fake handler to simulate the GitHub
/// release zip download without touching the network. Covers idempotency
/// (marker-file mechanics), error wrapping, and edge cases around the
/// extracted-but-no-binary failure mode.
/// </summary>
public sealed class MsStoreInstallerTests : IDisposable
{
    private readonly string _scratch;

    public MsStoreInstallerTests()
    {
        _scratch = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
            "tamp-msstore-install-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_scratch);
    }

    public void Dispose()
    {
        try { Directory.Delete(_scratch, recursive: true); } catch { }
    }

    /// <summary>Build a tiny .zip containing the named entries (filename → bytes) and return the bytes.</summary>
    private static byte[] BuildZip(Dictionary<string, byte[]> entries)
    {
        using var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, content) in entries)
            {
                var entry = archive.CreateEntry(name);
                using var stream = entry.Open();
                stream.Write(content, 0, content.Length);
            }
        }
        return ms.ToArray();
    }

    /// <summary>Build a zip with a fake msstore.exe entry — the happy-path artifact shape.</summary>
    private static byte[] BuildFakeMsStoreZip(string fakeContent = "MZ\x00\x00fake-msstore")
        => BuildZip(new Dictionary<string, byte[]>
        {
            ["msstore.exe"] = Encoding.UTF8.GetBytes(fakeContent),
        });

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;
        public int CallCount { get; private set; }
        public List<Uri> RequestedUris { get; } = new();

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) { _responder = responder; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            if (request.RequestUri is not null) RequestedUris.Add(request.RequestUri);
            return Task.FromResult(_responder(request));
        }
    }

    // ─── Happy path ──────────────────────────────────────────────────────

    [Fact]
    public void Install_Downloads_Extracts_And_Returns_Binary_Path()
    {
        var installDir = AbsolutePath.Create(Path.Combine(_scratch, "msstore-cli"));
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(BuildFakeMsStoreZip()),
        });
        using var http = new HttpClient(handler);

        var binary = MsStoreInstaller.Install("0.3.9", installDir, http);

        Assert.Equal((installDir / "msstore.exe").Value, binary.Value);
        Assert.True(binary.FileExists());
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public void Install_Writes_Marker_File_With_Version()
    {
        var installDir = AbsolutePath.Create(Path.Combine(_scratch, "msstore-cli"));
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(BuildFakeMsStoreZip()),
        });
        using var http = new HttpClient(handler);

        MsStoreInstaller.Install("0.3.9", installDir, http);

        var marker = installDir / ".tamp-msstore-version";
        Assert.True(marker.FileExists());
        Assert.Equal("0.3.9", marker.ReadAllText().Trim());
    }

    [Fact]
    public void Install_Hits_Versioned_GitHub_Release_Url()
    {
        var installDir = AbsolutePath.Create(Path.Combine(_scratch, "msstore-cli"));
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(BuildFakeMsStoreZip()),
        });
        using var http = new HttpClient(handler);

        MsStoreInstaller.Install("0.3.9", installDir, http);

        Assert.Single(handler.RequestedUris);
        var uri = handler.RequestedUris[0].ToString();
        Assert.Contains("github.com/microsoft/msstore-cli/releases/download/v0.3.9/", uri);
        Assert.Contains("MSStoreCLI-win-x64.zip", uri);
    }

    // ─── Idempotency ─────────────────────────────────────────────────────

    [Fact]
    public void Install_Is_NoOp_When_Already_At_Target_Version()
    {
        var installDir = AbsolutePath.Create(Path.Combine(_scratch, "msstore-cli"));
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(BuildFakeMsStoreZip()),
        });
        using var http = new HttpClient(handler);

        MsStoreInstaller.Install("0.3.9", installDir, http);
        Assert.Equal(1, handler.CallCount);

        // Second call with same version → no network hit
        var binary = MsStoreInstaller.Install("0.3.9", installDir, http);
        Assert.Equal(1, handler.CallCount);
        Assert.Equal((installDir / "msstore.exe").Value, binary.Value);
    }

    [Fact]
    public void Install_Triggers_Re_Download_When_Version_Changes()
    {
        var installDir = AbsolutePath.Create(Path.Combine(_scratch, "msstore-cli"));
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(BuildFakeMsStoreZip()),
        });
        using var http = new HttpClient(handler);

        MsStoreInstaller.Install("0.3.9", installDir, http);
        Assert.Equal(1, handler.CallCount);

        // Different version → fresh download
        MsStoreInstaller.Install("0.4.0", installDir, http);
        Assert.Equal(2, handler.CallCount);
        Assert.EndsWith("v0.4.0/MSStoreCLI-win-x64.zip", handler.RequestedUris[1].ToString());
    }

    [Fact]
    public void Install_Triggers_Re_Download_When_Marker_Missing()
    {
        var installDir = AbsolutePath.Create(Path.Combine(_scratch, "msstore-cli"));
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(BuildFakeMsStoreZip()),
        });
        using var http = new HttpClient(handler);

        MsStoreInstaller.Install("0.3.9", installDir, http);
        Assert.Equal(1, handler.CallCount);

        // Adversarial: marker deleted but binary still present (e.g. partial wipe).
        (installDir / ".tamp-msstore-version").DeleteFile();

        MsStoreInstaller.Install("0.3.9", installDir, http);
        Assert.Equal(2, handler.CallCount);
    }

    // ─── Error paths ────────────────────────────────────────────────────

    [Fact]
    public void Install_Throws_With_Diagnostic_Message_On_HttpNotFound()
    {
        var installDir = AbsolutePath.Create(Path.Combine(_scratch, "msstore-cli"));
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            ReasonPhrase = "Not Found",
        });
        using var http = new HttpClient(handler);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            MsStoreInstaller.Install("999.999.999", installDir, http));
        Assert.Contains("999.999.999", ex.Message);
        Assert.Contains("404", ex.Message);
    }

    [Fact]
    public void Install_Throws_When_Zip_Missing_MsStore_Exe()
    {
        var installDir = AbsolutePath.Create(Path.Combine(_scratch, "msstore-cli"));
        // Zip with no msstore.exe inside — simulates the upstream changing
        // their asset layout and breaking our naming assumption.
        var badZip = BuildZip(new Dictionary<string, byte[]>
        {
            ["other-binary.exe"] = new byte[] { 0x00, 0x01, 0x02 },
        });
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(badZip),
        });
        using var http = new HttpClient(handler);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            MsStoreInstaller.Install("0.3.9", installDir, http));
        Assert.Contains("msstore.exe was not found", ex.Message);
        Assert.Contains("0.3.9", ex.Message);
    }

    [Fact]
    public void Install_Throws_On_Null_InstallDir()
    {
        using var http = new HttpClient();
        Assert.Throws<ArgumentNullException>(() => MsStoreInstaller.Install("0.3.9", null!, http));
    }

    [Fact]
    public void Install_Throws_On_Null_HttpClient()
    {
        var installDir = AbsolutePath.Create(_scratch);
        Assert.Throws<ArgumentNullException>(() => MsStoreInstaller.Install("0.3.9", installDir, null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Install_Throws_On_Empty_Version(string version)
    {
        var installDir = AbsolutePath.Create(_scratch);
        using var http = new HttpClient();
        Assert.Throws<ArgumentException>(() => MsStoreInstaller.Install(version, installDir, http));
    }

    // ─── DefaultWindowsInstallDir is well-formed ─────────────────────────

    [Fact]
    public void DefaultWindowsInstallDir_Points_Into_LocalApplicationData_Programs()
    {
        var dir = MsStoreInstaller.DefaultWindowsInstallDir();
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        Assert.StartsWith(localAppData, dir.Value);
        Assert.Contains("msstore-cli", dir.Value);
    }

    // ─── Stale install dir cleanup ──────────────────────────────────────

    [Fact]
    public void Install_Wipes_Stale_Files_From_Previous_Install()
    {
        var installDir = AbsolutePath.Create(Path.Combine(_scratch, "msstore-cli"));
        installDir.EnsureDirectoryExists();
        (installDir / "stale-debug-helper.exe").WriteAllText("leftover from prior install");

        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(BuildFakeMsStoreZip()),
        });
        using var http = new HttpClient(handler);

        MsStoreInstaller.Install("0.3.9", installDir, http);

        // The stale file must not survive — leaving executables behind from
        // an older install could shadow the new version on PATH.
        Assert.False((installDir / "stale-debug-helper.exe").FileExists());
        Assert.True((installDir / "msstore.exe").FileExists());
    }
}
