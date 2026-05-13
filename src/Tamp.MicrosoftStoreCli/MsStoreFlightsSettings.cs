namespace Tamp.MicrosoftStoreCli;

// ────────────────────────────────────────────────────────────────────────────
//  Flights — ring/insider deploys. Flight = a subset of users who get a
//  pre-release submission before the main store.
// ────────────────────────────────────────────────────────────────────────────

/// <summary>Settings for <c>msstore flights list &lt;productId&gt;</c>.</summary>
public sealed class MsStoreFlightsListSettings : MsStoreSettingsBase
{
    public string? ProductId { get; set; }

    public MsStoreFlightsListSettings SetProductId(string id) { ProductId = id; return this; }
    public MsStoreFlightsListSettings SetVerbose(bool v = true) { Verbose = v; return this; }

    protected override IEnumerable<string> Verb => new[] { "flights", "list" };

    protected override void AppendArguments(List<string> args)
    {
        if (string.IsNullOrEmpty(ProductId))
            throw new InvalidOperationException("ProductId is required for `msstore flights list`.");
        args.Add(ProductId!);
    }
}

/// <summary>Settings for <c>msstore flights get &lt;productId&gt; &lt;flightId&gt;</c>.</summary>
public sealed class MsStoreFlightsGetSettings : MsStoreSettingsBase
{
    public string? ProductId { get; set; }
    public string? FlightId { get; set; }

    public MsStoreFlightsGetSettings SetProductId(string id) { ProductId = id; return this; }
    public MsStoreFlightsGetSettings SetFlightId(string id) { FlightId = id; return this; }
    public MsStoreFlightsGetSettings SetVerbose(bool v = true) { Verbose = v; return this; }

    protected override IEnumerable<string> Verb => new[] { "flights", "get" };

    protected override void AppendArguments(List<string> args)
    {
        if (string.IsNullOrEmpty(ProductId)) throw new InvalidOperationException("ProductId is required.");
        if (string.IsNullOrEmpty(FlightId)) throw new InvalidOperationException("FlightId is required.");
        args.Add(ProductId!);
        args.Add(FlightId!);
    }
}

/// <summary>Settings for <c>msstore flights delete &lt;productId&gt; &lt;flightId&gt;</c>.</summary>
public sealed class MsStoreFlightsDeleteSettings : MsStoreSettingsBase
{
    public string? ProductId { get; set; }
    public string? FlightId { get; set; }

    public MsStoreFlightsDeleteSettings SetProductId(string id) { ProductId = id; return this; }
    public MsStoreFlightsDeleteSettings SetFlightId(string id) { FlightId = id; return this; }
    public MsStoreFlightsDeleteSettings SetVerbose(bool v = true) { Verbose = v; return this; }

    protected override IEnumerable<string> Verb => new[] { "flights", "delete" };

    protected override void AppendArguments(List<string> args)
    {
        if (string.IsNullOrEmpty(ProductId)) throw new InvalidOperationException("ProductId is required.");
        if (string.IsNullOrEmpty(FlightId)) throw new InvalidOperationException("FlightId is required.");
        args.Add(ProductId!);
        args.Add(FlightId!);
    }
}

/// <summary>Settings for <c>msstore flights create &lt;productId&gt; &lt;friendlyName&gt; --group-ids ...</c>.</summary>
public sealed class MsStoreFlightsCreateSettings : MsStoreSettingsBase
{
    public string? ProductId { get; set; }
    public string? FriendlyName { get; set; }
    public List<string> GroupIds { get; } = new();
    public string? RankHigherThan { get; set; }

    public MsStoreFlightsCreateSettings SetProductId(string id) { ProductId = id; return this; }
    public MsStoreFlightsCreateSettings SetFriendlyName(string name) { FriendlyName = name; return this; }
    public MsStoreFlightsCreateSettings AddGroupId(string id) { GroupIds.Add(id); return this; }
    public MsStoreFlightsCreateSettings AddGroupIds(params string[] ids) { GroupIds.AddRange(ids); return this; }
    public MsStoreFlightsCreateSettings SetRankHigherThan(string flightId) { RankHigherThan = flightId; return this; }
    public MsStoreFlightsCreateSettings SetVerbose(bool v = true) { Verbose = v; return this; }

    protected override IEnumerable<string> Verb => new[] { "flights", "create" };

    protected override void AppendArguments(List<string> args)
    {
        if (string.IsNullOrEmpty(ProductId)) throw new InvalidOperationException("ProductId is required.");
        if (string.IsNullOrEmpty(FriendlyName)) throw new InvalidOperationException("FriendlyName is required.");
        if (GroupIds.Count == 0) throw new InvalidOperationException("At least one GroupId is required (use AddGroupId / AddGroupIds).");

        args.Add(ProductId!);
        args.Add(FriendlyName!);
        args.Add("--group-ids");
        args.Add(string.Join(",", GroupIds));
        if (!string.IsNullOrEmpty(RankHigherThan)) { args.Add("--rank-higher-than"); args.Add(RankHigherThan!); }
    }
}

// ────────────────────────────────────────────────────────────────────────────
//  Flights → submission verbs (get / delete / update / publish / poll / status)
// ────────────────────────────────────────────────────────────────────────────

public abstract class FlightSubmissionBase : MsStoreSettingsBase
{
    public string? ProductId { get; set; }
    public string? FlightId { get; set; }
    protected abstract string Verb3 { get; }
    protected override IEnumerable<string> Verb => new[] { "flights", "submission", Verb3 };
    protected void RequireBoth()
    {
        if (string.IsNullOrEmpty(ProductId)) throw new InvalidOperationException($"ProductId is required for `msstore flights submission {Verb3}`.");
        if (string.IsNullOrEmpty(FlightId)) throw new InvalidOperationException($"FlightId is required for `msstore flights submission {Verb3}`.");
    }
}

public sealed class MsStoreFlightSubmissionGetSettings : FlightSubmissionBase
{
    public MsStoreFlightSubmissionGetSettings SetProductId(string id) { ProductId = id; return this; }
    public MsStoreFlightSubmissionGetSettings SetFlightId(string id) { FlightId = id; return this; }
    public MsStoreFlightSubmissionGetSettings SetVerbose(bool v = true) { Verbose = v; return this; }
    protected override string Verb3 => "get";
    protected override void AppendArguments(List<string> args) { RequireBoth(); args.Add(ProductId!); args.Add(FlightId!); }
}

public sealed class MsStoreFlightSubmissionDeleteSettings : FlightSubmissionBase
{
    public bool NoConfirm { get; set; } = true;
    public MsStoreFlightSubmissionDeleteSettings SetProductId(string id) { ProductId = id; return this; }
    public MsStoreFlightSubmissionDeleteSettings SetFlightId(string id) { FlightId = id; return this; }
    public MsStoreFlightSubmissionDeleteSettings SetNoConfirm(bool v = true) { NoConfirm = v; return this; }
    public MsStoreFlightSubmissionDeleteSettings SetVerbose(bool v = true) { Verbose = v; return this; }
    protected override string Verb3 => "delete";
    protected override void AppendArguments(List<string> args)
    {
        RequireBoth(); args.Add(ProductId!); args.Add(FlightId!);
        if (NoConfirm) args.Add("--no-confirm");
    }
}

public sealed class MsStoreFlightSubmissionUpdateSettings : FlightSubmissionBase
{
    public string? Product { get; set; }
    public bool SkipInitialPolling { get; set; }
    public MsStoreFlightSubmissionUpdateSettings SetProductId(string id) { ProductId = id; return this; }
    public MsStoreFlightSubmissionUpdateSettings SetFlightId(string id) { FlightId = id; return this; }
    public MsStoreFlightSubmissionUpdateSettings SetProduct(string json) { Product = json; return this; }
    public MsStoreFlightSubmissionUpdateSettings SetSkipInitialPolling(bool v = true) { SkipInitialPolling = v; return this; }
    public MsStoreFlightSubmissionUpdateSettings SetVerbose(bool v = true) { Verbose = v; return this; }
    protected override string Verb3 => "update";
    protected override void AppendArguments(List<string> args)
    {
        RequireBoth();
        if (string.IsNullOrEmpty(Product)) throw new InvalidOperationException("Product JSON is required for `msstore flights submission update`.");
        args.Add(ProductId!); args.Add(FlightId!); args.Add(Product!);
        if (SkipInitialPolling) args.Add("--skipInitialPolling");
    }
}

public sealed class MsStoreFlightSubmissionPublishSettings : FlightSubmissionBase
{
    public MsStoreFlightSubmissionPublishSettings SetProductId(string id) { ProductId = id; return this; }
    public MsStoreFlightSubmissionPublishSettings SetFlightId(string id) { FlightId = id; return this; }
    public MsStoreFlightSubmissionPublishSettings SetVerbose(bool v = true) { Verbose = v; return this; }
    protected override string Verb3 => "publish";
    protected override void AppendArguments(List<string> args) { RequireBoth(); args.Add(ProductId!); args.Add(FlightId!); }
}

public sealed class MsStoreFlightSubmissionPollSettings : FlightSubmissionBase
{
    public MsStoreFlightSubmissionPollSettings SetProductId(string id) { ProductId = id; return this; }
    public MsStoreFlightSubmissionPollSettings SetFlightId(string id) { FlightId = id; return this; }
    public MsStoreFlightSubmissionPollSettings SetVerbose(bool v = true) { Verbose = v; return this; }
    protected override string Verb3 => "poll";
    protected override void AppendArguments(List<string> args) { RequireBoth(); args.Add(ProductId!); args.Add(FlightId!); }
}

public sealed class MsStoreFlightSubmissionStatusSettings : FlightSubmissionBase
{
    public MsStoreFlightSubmissionStatusSettings SetProductId(string id) { ProductId = id; return this; }
    public MsStoreFlightSubmissionStatusSettings SetFlightId(string id) { FlightId = id; return this; }
    public MsStoreFlightSubmissionStatusSettings SetVerbose(bool v = true) { Verbose = v; return this; }
    protected override string Verb3 => "status";
    protected override void AppendArguments(List<string> args) { RequireBoth(); args.Add(ProductId!); args.Add(FlightId!); }
}

// ────────────────────────────────────────────────────────────────────────────
//  Flight rollout verbs — gradual ring deploys
// ────────────────────────────────────────────────────────────────────────────

public abstract class FlightRolloutBase : MsStoreSettingsBase
{
    public string? ProductId { get; set; }
    public string? FlightId { get; set; }
    public string? SubmissionId { get; set; }
    protected abstract string Verb4 { get; }
    protected override IEnumerable<string> Verb => new[] { "flights", "submission", "rollout", Verb4 };
    protected void RequireProductAndFlight()
    {
        if (string.IsNullOrEmpty(ProductId)) throw new InvalidOperationException("ProductId is required.");
        if (string.IsNullOrEmpty(FlightId)) throw new InvalidOperationException("FlightId is required.");
    }
    protected void AppendCommonTail(List<string> args)
    {
        args.Add(ProductId!); args.Add(FlightId!);
        if (!string.IsNullOrEmpty(SubmissionId)) { args.Add("--submissionId"); args.Add(SubmissionId!); }
    }
}

public sealed class MsStoreFlightRolloutGetSettings : FlightRolloutBase
{
    public MsStoreFlightRolloutGetSettings SetProductId(string id) { ProductId = id; return this; }
    public MsStoreFlightRolloutGetSettings SetFlightId(string id) { FlightId = id; return this; }
    public MsStoreFlightRolloutGetSettings SetSubmissionId(string id) { SubmissionId = id; return this; }
    public MsStoreFlightRolloutGetSettings SetVerbose(bool v = true) { Verbose = v; return this; }
    protected override string Verb4 => "get";
    protected override void AppendArguments(List<string> args) { RequireProductAndFlight(); AppendCommonTail(args); }
}

public sealed class MsStoreFlightRolloutUpdateSettings : FlightRolloutBase
{
    public int? Percentage { get; set; }
    public MsStoreFlightRolloutUpdateSettings SetProductId(string id) { ProductId = id; return this; }
    public MsStoreFlightRolloutUpdateSettings SetFlightId(string id) { FlightId = id; return this; }
    public MsStoreFlightRolloutUpdateSettings SetPercentage(int pct) { Percentage = pct; return this; }
    public MsStoreFlightRolloutUpdateSettings SetSubmissionId(string id) { SubmissionId = id; return this; }
    public MsStoreFlightRolloutUpdateSettings SetVerbose(bool v = true) { Verbose = v; return this; }
    protected override string Verb4 => "update";
    protected override void AppendArguments(List<string> args)
    {
        RequireProductAndFlight();
        if (Percentage is null) throw new InvalidOperationException("Percentage is required for `msstore flights submission rollout update`.");
        if (Percentage is < 0 or > 100) throw new InvalidOperationException($"Percentage must be 0-100; got {Percentage}.");
        args.Add(ProductId!); args.Add(FlightId!); args.Add(Percentage.Value.ToString());
        if (!string.IsNullOrEmpty(SubmissionId)) { args.Add("--submissionId"); args.Add(SubmissionId!); }
    }
}

public sealed class MsStoreFlightRolloutHaltSettings : FlightRolloutBase
{
    public MsStoreFlightRolloutHaltSettings SetProductId(string id) { ProductId = id; return this; }
    public MsStoreFlightRolloutHaltSettings SetFlightId(string id) { FlightId = id; return this; }
    public MsStoreFlightRolloutHaltSettings SetSubmissionId(string id) { SubmissionId = id; return this; }
    public MsStoreFlightRolloutHaltSettings SetVerbose(bool v = true) { Verbose = v; return this; }
    protected override string Verb4 => "halt";
    protected override void AppendArguments(List<string> args) { RequireProductAndFlight(); AppendCommonTail(args); }
}

public sealed class MsStoreFlightRolloutFinalizeSettings : FlightRolloutBase
{
    public MsStoreFlightRolloutFinalizeSettings SetProductId(string id) { ProductId = id; return this; }
    public MsStoreFlightRolloutFinalizeSettings SetFlightId(string id) { FlightId = id; return this; }
    public MsStoreFlightRolloutFinalizeSettings SetSubmissionId(string id) { SubmissionId = id; return this; }
    public MsStoreFlightRolloutFinalizeSettings SetVerbose(bool v = true) { Verbose = v; return this; }
    protected override string Verb4 => "finalize";
    protected override void AppendArguments(List<string> args) { RequireProductAndFlight(); AppendCommonTail(args); }
}
