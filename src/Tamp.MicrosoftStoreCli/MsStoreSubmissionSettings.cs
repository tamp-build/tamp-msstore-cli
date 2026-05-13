namespace Tamp.MicrosoftStoreCli;

// ────────────────────────────────────────────────────────────────────────────
//  Submission verbs — the fine-grain lifecycle:
//    get → mutate JSON → update → publish → poll
//  Adopters who want explicit submission control use these instead of (or in
//  addition to) MsStore.Publish.
// ────────────────────────────────────────────────────────────────────────────

internal abstract class SubmissionVerbSettingsBase : MsStoreSettingsBase
{
    public string? ProductId { get; set; }
    protected abstract string Verb2 { get; }
    protected override IEnumerable<string> Verb => new[] { "submission", Verb2 };
    protected void RequireProductId()
    {
        if (string.IsNullOrEmpty(ProductId))
            throw new InvalidOperationException($"ProductId is required for `msstore submission {Verb2}`.");
    }
}

/// <summary>Settings for <c>msstore submission status &lt;productId&gt;</c>.</summary>
public sealed class MsStoreSubmissionStatusSettings : MsStoreSettingsBase
{
    public string? ProductId { get; set; }

    public MsStoreSubmissionStatusSettings SetProductId(string id) { ProductId = id; return this; }
    public MsStoreSubmissionStatusSettings SetVerbose(bool v = true) { Verbose = v; return this; }

    protected override IEnumerable<string> Verb => new[] { "submission", "status" };

    protected override void AppendArguments(List<string> args)
    {
        if (string.IsNullOrEmpty(ProductId))
            throw new InvalidOperationException("ProductId is required for `msstore submission status`.");
        args.Add(ProductId!);
    }
}

/// <summary>Settings for <c>msstore submission get &lt;productId&gt;</c>.</summary>
public sealed class MsStoreSubmissionGetSettings : MsStoreSettingsBase
{
    public string? ProductId { get; set; }

    public MsStoreSubmissionGetSettings SetProductId(string id) { ProductId = id; return this; }
    public MsStoreSubmissionGetSettings SetVerbose(bool v = true) { Verbose = v; return this; }

    protected override IEnumerable<string> Verb => new[] { "submission", "get" };

    protected override void AppendArguments(List<string> args)
    {
        if (string.IsNullOrEmpty(ProductId))
            throw new InvalidOperationException("ProductId is required for `msstore submission get`.");
        args.Add(ProductId!);
    }
}

/// <summary>Settings for <c>msstore submission getListingAssets &lt;productId&gt;</c>.</summary>
public sealed class MsStoreSubmissionGetListingAssetsSettings : MsStoreSettingsBase
{
    public string? ProductId { get; set; }

    public MsStoreSubmissionGetListingAssetsSettings SetProductId(string id) { ProductId = id; return this; }
    public MsStoreSubmissionGetListingAssetsSettings SetVerbose(bool v = true) { Verbose = v; return this; }

    protected override IEnumerable<string> Verb => new[] { "submission", "getListingAssets" };

    protected override void AppendArguments(List<string> args)
    {
        if (string.IsNullOrEmpty(ProductId))
            throw new InvalidOperationException("ProductId is required for `msstore submission getListingAssets`.");
        args.Add(ProductId!);
    }
}

/// <summary>Settings for <c>msstore submission updateMetadata &lt;productId&gt; &lt;metadata-json&gt;</c>.</summary>
public sealed class MsStoreSubmissionUpdateMetadataSettings : MsStoreSettingsBase
{
    public string? ProductId { get; set; }

    /// <summary>JSON metadata payload (positional argument). Typically produced by mutating the output of <c>submission get</c>.</summary>
    public string? Metadata { get; set; }

    /// <summary>Skip the initial polling before executing the action (<c>--skipInitialPolling</c>).</summary>
    public bool SkipInitialPolling { get; set; }

    public MsStoreSubmissionUpdateMetadataSettings SetProductId(string id) { ProductId = id; return this; }
    public MsStoreSubmissionUpdateMetadataSettings SetMetadata(string json) { Metadata = json; return this; }
    public MsStoreSubmissionUpdateMetadataSettings SetSkipInitialPolling(bool v = true) { SkipInitialPolling = v; return this; }
    public MsStoreSubmissionUpdateMetadataSettings SetVerbose(bool v = true) { Verbose = v; return this; }

    protected override IEnumerable<string> Verb => new[] { "submission", "updateMetadata" };

    protected override void AppendArguments(List<string> args)
    {
        if (string.IsNullOrEmpty(ProductId))
            throw new InvalidOperationException("ProductId is required for `msstore submission updateMetadata`.");
        if (string.IsNullOrEmpty(Metadata))
            throw new InvalidOperationException("Metadata JSON is required for `msstore submission updateMetadata`.");
        args.Add(ProductId!);
        args.Add(Metadata!);
        if (SkipInitialPolling) args.Add("--skipInitialPolling");
    }
}

/// <summary>Settings for <c>msstore submission update &lt;productId&gt; &lt;package-json&gt;</c>.</summary>
public sealed class MsStoreSubmissionUpdateSettings : MsStoreSettingsBase
{
    public string? ProductId { get; set; }

    /// <summary>JSON package payload (positional argument). Typically produced by mutating the output of <c>submission get</c>.</summary>
    public string? Package { get; set; }

    /// <summary>Skip the initial polling before executing the action (<c>--skipInitialPolling</c>).</summary>
    public bool SkipInitialPolling { get; set; }

    public MsStoreSubmissionUpdateSettings SetProductId(string id) { ProductId = id; return this; }
    public MsStoreSubmissionUpdateSettings SetPackage(string json) { Package = json; return this; }
    public MsStoreSubmissionUpdateSettings SetSkipInitialPolling(bool v = true) { SkipInitialPolling = v; return this; }
    public MsStoreSubmissionUpdateSettings SetVerbose(bool v = true) { Verbose = v; return this; }

    protected override IEnumerable<string> Verb => new[] { "submission", "update" };

    protected override void AppendArguments(List<string> args)
    {
        if (string.IsNullOrEmpty(ProductId))
            throw new InvalidOperationException("ProductId is required for `msstore submission update`.");
        if (string.IsNullOrEmpty(Package))
            throw new InvalidOperationException("Package JSON is required for `msstore submission update`.");
        args.Add(ProductId!);
        args.Add(Package!);
        if (SkipInitialPolling) args.Add("--skipInitialPolling");
    }
}

/// <summary>Settings for <c>msstore submission poll &lt;productId&gt;</c>.</summary>
public sealed class MsStoreSubmissionPollSettings : MsStoreSettingsBase
{
    public string? ProductId { get; set; }

    public MsStoreSubmissionPollSettings SetProductId(string id) { ProductId = id; return this; }
    public MsStoreSubmissionPollSettings SetVerbose(bool v = true) { Verbose = v; return this; }

    protected override IEnumerable<string> Verb => new[] { "submission", "poll" };

    protected override void AppendArguments(List<string> args)
    {
        if (string.IsNullOrEmpty(ProductId))
            throw new InvalidOperationException("ProductId is required for `msstore submission poll`.");
        args.Add(ProductId!);
    }
}

/// <summary>Settings for <c>msstore submission publish &lt;productId&gt;</c>.</summary>
public sealed class MsStoreSubmissionPublishSettings : MsStoreSettingsBase
{
    public string? ProductId { get; set; }

    public MsStoreSubmissionPublishSettings SetProductId(string id) { ProductId = id; return this; }
    public MsStoreSubmissionPublishSettings SetVerbose(bool v = true) { Verbose = v; return this; }

    protected override IEnumerable<string> Verb => new[] { "submission", "publish" };

    protected override void AppendArguments(List<string> args)
    {
        if (string.IsNullOrEmpty(ProductId))
            throw new InvalidOperationException("ProductId is required for `msstore submission publish`.");
        args.Add(ProductId!);
    }
}

/// <summary>Settings for <c>msstore submission delete &lt;productId&gt;</c>.</summary>
public sealed class MsStoreSubmissionDeleteSettings : MsStoreSettingsBase
{
    public string? ProductId { get; set; }

    /// <summary>Skip the interactive confirmation prompt (<c>--no-confirm</c>). Required in CI.</summary>
    public bool NoConfirm { get; set; } = true;

    public MsStoreSubmissionDeleteSettings SetProductId(string id) { ProductId = id; return this; }
    public MsStoreSubmissionDeleteSettings SetNoConfirm(bool v = true) { NoConfirm = v; return this; }
    public MsStoreSubmissionDeleteSettings SetVerbose(bool v = true) { Verbose = v; return this; }

    protected override IEnumerable<string> Verb => new[] { "submission", "delete" };

    protected override void AppendArguments(List<string> args)
    {
        if (string.IsNullOrEmpty(ProductId))
            throw new InvalidOperationException("ProductId is required for `msstore submission delete`.");
        args.Add(ProductId!);
        if (NoConfirm) args.Add("--no-confirm");
    }
}
