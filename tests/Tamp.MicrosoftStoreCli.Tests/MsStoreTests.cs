using System;
using System.Collections.Generic;
using System.Linq;
using Tamp;
using Tamp.MicrosoftStoreCli;
using Xunit;

namespace Tamp.MicrosoftStoreCli.Tests;

public sealed class MsStoreTests
{
    private static Tool FakeTool() => new(AbsolutePath.Create("/fake/msstore"));

    private static int IndexOf(IReadOnlyList<string> args, string token)
    {
        for (var i = 0; i < args.Count; i++) if (args[i] == token) return i;
        return -1;
    }

    // ─── reconfigure ──────────────────────────────────────────────────────

    [Fact]
    public void Reconfigure_ClientSecret_Path()
    {
        var sec = new Secret("test_secret", "pc_client_secret_test_value");
        var plan = MsStore.Reconfigure(FakeTool(), s => s
            .SetTenantId("tenant-uuid")
            .SetSellerId("12345")
            .SetClientId("client-uuid")
            .SetClientSecret(sec));
        Assert.Equal("reconfigure", plan.Arguments[0]);
        Assert.Equal("tenant-uuid", plan.Arguments[IndexOf(plan.Arguments, "--tenantId") + 1]);
        Assert.Equal("12345", plan.Arguments[IndexOf(plan.Arguments, "--sellerId") + 1]);
        Assert.Equal("client-uuid", plan.Arguments[IndexOf(plan.Arguments, "--clientId") + 1]);
        Assert.Equal("pc_client_secret_test_value", plan.Arguments[IndexOf(plan.Arguments, "--clientSecret") + 1]);
        Assert.Contains(sec, plan.Secrets);  // Secret flows through CommandPlan for masking
    }

    [Fact]
    public void Reconfigure_Certificate_File_With_Password()
    {
        var pwd = new Secret("test_secret", "pfx_pwd");
        var plan = MsStore.Reconfigure(FakeTool(), s => s
            .SetTenantId("t").SetSellerId("s").SetClientId("c")
            .SetCertificateFilePath("cert.pfx")
            .SetCertificatePassword(pwd));
        Assert.Equal("cert.pfx", plan.Arguments[IndexOf(plan.Arguments, "--certificateFilePath") + 1]);
        Assert.Equal("pfx_pwd", plan.Arguments[IndexOf(plan.Arguments, "--certificatePassword") + 1]);
        Assert.Contains(pwd, plan.Secrets);
    }

    [Fact]
    public void Reconfigure_Certificate_Thumbprint_Path()
    {
        var plan = MsStore.Reconfigure(FakeTool(), s => s
            .SetTenantId("t").SetSellerId("s").SetClientId("c")
            .SetCertificateThumbprint("ABCDEF1234567890"));
        Assert.Equal("ABCDEF1234567890", plan.Arguments[IndexOf(plan.Arguments, "--certificateThumbprint") + 1]);
    }

    [Fact]
    public void Reconfigure_Mutually_Exclusive_Auth_Paths()
    {
        Assert.Throws<InvalidOperationException>(() =>
            MsStore.Reconfigure(FakeTool(), s => s
                .SetClientSecret(new Secret("test_secret", "x"))
                .SetCertificateThumbprint("y")).Arguments.ToList());
        Assert.Throws<InvalidOperationException>(() =>
            MsStore.Reconfigure(FakeTool(), s => s
                .SetClientSecret(new Secret("test_secret", "x"))
                .SetCertificateFilePath("y.pfx")).Arguments.ToList());
        Assert.Throws<InvalidOperationException>(() =>
            MsStore.Reconfigure(FakeTool(), s => s
                .SetCertificateThumbprint("a")
                .SetCertificateFilePath("b.pfx")).Arguments.ToList());
    }

    [Fact]
    public void Reconfigure_Password_Requires_File_Path()
    {
        // Setting password with thumbprint-based auth is wrong
        Assert.Throws<InvalidOperationException>(() =>
            MsStore.Reconfigure(FakeTool(), s => s
                .SetCertificateThumbprint("abc")
                .SetCertificatePassword(new Secret("test_secret", "pwd"))).Arguments.ToList());
    }

    [Fact]
    public void Reconfigure_Reset_Flag()
    {
        var plan = MsStore.Reconfigure(FakeTool(), s => s.SetReset());
        Assert.Contains("--reset", plan.Arguments);
    }

    // ─── publish (load-bearing verb) ──────────────────────────────────────

    [Fact]
    public void Publish_With_InputFile_Is_Upgrade_Path()
    {
        var plan = MsStore.Publish(FakeTool(), s => s
            .SetPathOrUrl("./DasBook")
            .SetInputFile("artifacts/DasBook_1.0.6.0_x64.msix"));
        Assert.Equal("publish", plan.Arguments[0]);
        Assert.Equal("./DasBook", plan.Arguments[1]);
        Assert.Equal("artifacts/DasBook_1.0.6.0_x64.msix",
            plan.Arguments[IndexOf(plan.Arguments, "--inputFile") + 1]);
    }

    [Fact]
    public void Publish_With_Rollout_Percentage()
    {
        var plan = MsStore.Publish(FakeTool(), s => s
            .SetPathOrUrl(".")
            .SetInputFile("x.msix")
            .SetPackageRolloutPercentage(25));
        Assert.Equal("25", plan.Arguments[IndexOf(plan.Arguments, "--packageRolloutPercentage") + 1]);
    }

    [Fact]
    public void Publish_NoCommit_Draft_Mode()
    {
        var plan = MsStore.Publish(FakeTool(), s => s
            .SetPathOrUrl(".").SetInputFile("x.msix").SetNoCommit());
        Assert.Contains("--noCommit", plan.Arguments);
    }

    [Fact]
    public void Publish_FlightId_For_Ring_Deploy()
    {
        var plan = MsStore.Publish(FakeTool(), s => s
            .SetPathOrUrl(".").SetInputFile("x.msix")
            .SetFlightId("flight-uuid"));
        Assert.Equal("flight-uuid", plan.Arguments[IndexOf(plan.Arguments, "--flightId") + 1]);
    }

    [Fact]
    public void Publish_AppId_Override()
    {
        var plan = MsStore.Publish(FakeTool(), s => s
            .SetPathOrUrl(".").SetInputFile("x.msix").SetAppId("9P53PC5S0PHJ"));
        Assert.Equal("9P53PC5S0PHJ", plan.Arguments[IndexOf(plan.Arguments, "--appId") + 1]);
    }

    [Fact]
    public void Publish_Requires_PathOrUrl()
    {
        Assert.Throws<InvalidOperationException>(() =>
            MsStore.Publish(FakeTool(), s => s.SetInputFile("x.msix")).Arguments.ToList());
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    [InlineData(int.MaxValue)]
    public void Publish_Rejects_Out_Of_Range_Rollout(int pct)
    {
        Assert.Throws<InvalidOperationException>(() =>
            MsStore.Publish(FakeTool(), s => s
                .SetPathOrUrl(".").SetInputFile("x.msix")
                .SetPackageRolloutPercentage(pct))
            .Arguments.ToList());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(25)]
    [InlineData(100)]
    public void Publish_Accepts_In_Range_Rollout(int pct)
    {
        var plan = MsStore.Publish(FakeTool(), s => s
            .SetPathOrUrl(".").SetInputFile("x.msix").SetPackageRolloutPercentage(pct));
        Assert.Equal(pct.ToString(), plan.Arguments[IndexOf(plan.Arguments, "--packageRolloutPercentage") + 1]);
    }

    // ─── submission lifecycle ─────────────────────────────────────────────

    [Fact]
    public void Submission_Status_Builds_Command()
    {
        var plan = MsStore.Submission.Status(FakeTool(), s => s.SetProductId("9P53PC5S0PHJ"));
        Assert.Equal(new[] { "submission", "status", "9P53PC5S0PHJ" }, plan.Arguments);
    }

    [Fact]
    public void Submission_Get_Builds_Command()
    {
        var plan = MsStore.Submission.Get(FakeTool(), s => s.SetProductId("p"));
        Assert.Equal(new[] { "submission", "get", "p" }, plan.Arguments);
    }

    [Fact]
    public void Submission_Update_Requires_Package_Json()
    {
        Assert.Throws<InvalidOperationException>(() =>
            MsStore.Submission.Update(FakeTool(), s => s.SetProductId("p")).Arguments.ToList());
    }

    [Fact]
    public void Submission_Update_Builds_Command()
    {
        var plan = MsStore.Submission.Update(FakeTool(), s => s
            .SetProductId("p").SetPackage("{\"version\":\"1.0.6\"}").SetSkipInitialPolling());
        Assert.Equal("submission", plan.Arguments[0]);
        Assert.Equal("update", plan.Arguments[1]);
        Assert.Equal("p", plan.Arguments[2]);
        Assert.Equal("{\"version\":\"1.0.6\"}", plan.Arguments[3]);
        Assert.Contains("--skipInitialPolling", plan.Arguments);
    }

    [Fact]
    public void Submission_Publish_Requires_ProductId()
    {
        Assert.Throws<InvalidOperationException>(() =>
            MsStore.Submission.Publish(FakeTool(), s => { }).Arguments.ToList());
    }

    [Fact]
    public void Submission_Delete_NoConfirm_Default_On_For_CI()
    {
        var plan = MsStore.Submission.Delete(FakeTool(), s => s.SetProductId("p"));
        Assert.Contains("--no-confirm", plan.Arguments);
    }

    [Fact]
    public void Submission_Poll_Builds_Command()
    {
        var plan = MsStore.Submission.Poll(FakeTool(), s => s.SetProductId("p"));
        Assert.Equal(new[] { "submission", "poll", "p" }, plan.Arguments);
    }

    // ─── apps ─────────────────────────────────────────────────────────────

    [Fact]
    public void Apps_List_Has_Verb()
    {
        var plan = MsStore.Apps.List(FakeTool());
        Assert.Equal(new[] { "apps", "list" }, plan.Arguments);
    }

    [Fact]
    public void Apps_Get_Requires_ProductId()
    {
        Assert.Throws<InvalidOperationException>(() =>
            MsStore.Apps.Get(FakeTool(), s => { }).Arguments.ToList());
        var plan = MsStore.Apps.Get(FakeTool(), s => s.SetProductId("9P53PC5S0PHJ"));
        Assert.Equal(new[] { "apps", "get", "9P53PC5S0PHJ" }, plan.Arguments);
    }

    // ─── flights ──────────────────────────────────────────────────────────

    [Fact]
    public void Flights_List_Builds_Command()
    {
        var plan = MsStore.Flights.List(FakeTool(), s => s.SetProductId("p"));
        Assert.Equal(new[] { "flights", "list", "p" }, plan.Arguments);
    }

    [Fact]
    public void Flights_Create_Requires_Group_Ids()
    {
        Assert.Throws<InvalidOperationException>(() =>
            MsStore.Flights.Create(FakeTool(), s => s
                .SetProductId("p").SetFriendlyName("Insider Ring")).Arguments.ToList());

        var plan = MsStore.Flights.Create(FakeTool(), s => s
            .SetProductId("p").SetFriendlyName("Insider Ring")
            .AddGroupIds("group-a", "group-b")
            .SetRankHigherThan("flight-x"));
        Assert.Equal("group-a,group-b", plan.Arguments[IndexOf(plan.Arguments, "--group-ids") + 1]);
        Assert.Equal("flight-x", plan.Arguments[IndexOf(plan.Arguments, "--rank-higher-than") + 1]);
    }

    [Fact]
    public void Flight_Submission_Update_Builds_Command()
    {
        var plan = MsStore.Flights.Submission.Update(FakeTool(), s => s
            .SetProductId("p").SetFlightId("f").SetProduct("{\"v\":1}"));
        Assert.Equal(new[] { "flights", "submission", "update", "p", "f", "{\"v\":1}" }, plan.Arguments);
    }

    [Fact]
    public void Flight_Rollout_Update_Requires_Percentage()
    {
        Assert.Throws<InvalidOperationException>(() =>
            MsStore.Flights.Submission.Rollout.Update(FakeTool(), s => s
                .SetProductId("p").SetFlightId("f")).Arguments.ToList());
    }

    [Theory]
    [InlineData(-5)]
    [InlineData(150)]
    public void Flight_Rollout_Update_Rejects_Out_Of_Range(int pct)
    {
        Assert.Throws<InvalidOperationException>(() =>
            MsStore.Flights.Submission.Rollout.Update(FakeTool(), s => s
                .SetProductId("p").SetFlightId("f").SetPercentage(pct))
            .Arguments.ToList());
    }

    [Fact]
    public void Flight_Rollout_Halt_Builds_Command()
    {
        var plan = MsStore.Flights.Submission.Rollout.Halt(FakeTool(), s => s
            .SetProductId("p").SetFlightId("f").SetSubmissionId("sub-1"));
        Assert.Equal(new[] { "flights", "submission", "rollout", "halt", "p", "f", "--submissionId", "sub-1" }, plan.Arguments);
    }

    [Fact]
    public void Flight_Rollout_Finalize_Builds_Command()
    {
        var plan = MsStore.Flights.Submission.Rollout.Finalize(FakeTool(), s => s
            .SetProductId("p").SetFlightId("f"));
        Assert.Equal(new[] { "flights", "submission", "rollout", "finalize", "p", "f" }, plan.Arguments);
    }

    // ─── init / package / info / setpdn / raw ─────────────────────────────

    [Fact]
    public void Init_Requires_PathOrUrl()
    {
        Assert.Throws<InvalidOperationException>(() =>
            MsStore.Init(FakeTool(), s => { }).Arguments.ToList());
    }

    [Fact]
    public void Init_Full_Surface()
    {
        var plan = MsStore.Init(FakeTool(), s => s
            .SetPathOrUrl("./app")
            .SetPublisherDisplayName("Brewing Coder")
            .SetPackage()
            .SetPublish()
            .SetFlightId("flight-x")
            .SetPackageRolloutPercentage(10)
            .AddArchitectures("x64", "arm64")
            .SetOutput("./bin")
            .SetVersion("1.0.6"));
        Assert.Equal("init", plan.Arguments[0]);
        Assert.Equal("./app", plan.Arguments[1]);
        Assert.Contains("--package", plan.Arguments);
        Assert.Contains("--publish", plan.Arguments);
        Assert.Equal("Brewing Coder", plan.Arguments[IndexOf(plan.Arguments, "--publisherDisplayName") + 1]);
    }

    [Fact]
    public void Info_Has_Verb()
    {
        var plan = MsStore.Info(FakeTool());
        Assert.Equal(new[] { "info" }, plan.Arguments);
    }

    [Fact]
    public void SetPdn_Requires_Name()
    {
        Assert.Throws<InvalidOperationException>(() =>
            MsStore.SetPdn(FakeTool(), s => { }).Arguments.ToList());
        var plan = MsStore.SetPdn(FakeTool(), s => s.SetPublisherDisplayName("Brewing Coder"));
        Assert.Equal(new[] { "settings", "setpdn", "Brewing Coder" }, plan.Arguments);
    }

    [Fact]
    public void Raw_Allows_Arbitrary_Verb()
    {
        var plan = MsStore.Raw(FakeTool(), "flights", "delete", "p", "f");
        Assert.Equal(new[] { "flights", "delete", "p", "f" }, plan.Arguments);
    }

    [Fact]
    public void Raw_Rejects_Empty()
    {
        Assert.Throws<ArgumentException>(() => MsStore.Raw(FakeTool()));
    }

    [Fact]
    public void Verbose_Flag_Appends_v()
    {
        var plan = MsStore.Apps.List(FakeTool(), s => s.SetVerbose());
        Assert.Contains("-v", plan.Arguments);
    }

    [Fact]
    public void WorkingDirectory_Propagates()
    {
        var plan = MsStore.Apps.List(FakeTool(), s => s.SetWorkingDirectory("/repo"));
        Assert.Equal("/repo", plan.WorkingDirectory);
    }
}
