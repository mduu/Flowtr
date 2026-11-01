using Flowtr.Server.Features.Inbox;
using Microsoft.Extensions.Configuration;
using Flowtr.Server.Features.Extraction;
using Flowtr.Shared;
using Flowtr.Shared.Inbox;
using Flowtr.Tests.Receipts;
using static Flowtr.Tests.Inbox.MailTestData;

namespace Flowtr.Tests.Inbox;

/// <summary>
/// Story #011: the scheduled job fetches mail while the app sleeps. It wakes the database only for mail.
/// </summary>
public class InboxJobTests
{
    [Fact]
    public async Task Empty_mailbox_does_not_wake_the_database()
    {
        // Arrange
        using var app = new TestApp();

        // Act
        var exitCode = await app.Get<InboxJob>().Run(CancellationToken.None);

        // Assert
        exitCode.ShouldBe(0);
        app.Database.Prepared.ShouldBe(0);
    }

    [Fact]
    public async Task Mail_is_imported_and_read_by_the_AI()
    {
        // Arrange
        using var app = new TestApp();
        await app.Invite(User);
        app.Mailbox.Receive(Mail(attachments: ("rechnung.pdf", ReceiptTestData.Pdf("Azure"))));
        app.Reader.Next = Result.Success(new ReceiptReading("2026-10-01", "Expense", 64.20m, "CHF", "Azure Abo", "Microsoft", null, null));

        // Act
        var exitCode = await app.Get<InboxJob>().Run(CancellationToken.None);

        // Assert
        exitCode.ShouldBe(0);
        app.Database.Prepared.ShouldBe(1);
        (await app.Inbox()).Items.ShouldHaveSingleItem().Analysis.ShouldBe(AnalysisState.Done);
        app.Mailbox.Inbox.ShouldBeEmpty();
    }

    [Fact]
    public async Task Unreachable_mailbox_fails_without_the_database()
    {
        // Arrange
        using var app = new TestApp();
        app.Mailbox.OpenError = new IOException("Connection refused");

        // Act
        var exitCode = await app.Get<InboxJob>().Run(CancellationToken.None);

        // Assert
        exitCode.ShouldBe(1);
        app.Database.Prepared.ShouldBe(0);
    }

    [Fact]
    public async Task Mail_stays_in_the_mailbox_when_the_database_does_not_wake_up()
    {
        // Arrange
        using var app = new TestApp();
        app.Database.Asleep = true;
        app.Mailbox.Receive(Mail(attachments: ("rechnung.pdf", ReceiptTestData.Pdf("Azure"))));
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        // Act
        var exitCode = await app.Get<InboxJob>().Run(stop.Token, databaseWait: TimeSpan.FromMilliseconds(50));

        // Assert
        exitCode.ShouldBe(1);
        app.Mailbox.Inbox.Count.ShouldBe(1);
    }

    [Theory]
    [InlineData("inbox", true)]
    [InlineData(null, false)]
    [InlineData("other", false)]
    public void Job_is_requested_with_the_argument(string? job, bool requested)
    {
        var configuration = new ConfigurationBuilder()
            .AddCommandLine(job is null ? [] : ["--job", job])
            .Build();

        InboxJob.IsRequested(configuration).ShouldBe(requested);
    }
}
