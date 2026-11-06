using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit.v3;

namespace Bookly.E2ETests;

public sealed class BookingTests : PageTest
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    private DistributedApplication _app = default!;

    [Fact]
    public async Task CreateAndCancelBooking()
    {
        var frontendUrl = _app.GetEndpoint("Frontend");

        await Page.GotoAsync(frontendUrl.AbsoluteUri);

        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Your name" }).FillAsync("Playwright Tester");
        await Page.GetByLabel("Room").SelectOptionAsync(new[] { "Meeting Room B" });
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Start date (UTC)" }).FillAsync(DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd"));
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Start time (UTC)" }).FillAsync("10:00");
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "End time (UTC)" }).FillAsync("11:00");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Book Meeting Room B, 10:00-11:00" }).ClickAsync();

        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Playwright Tester 10:00-11:00" })).ToBeVisibleAsync();

        await Page.GetByRole(AriaRole.Button, new() { Name = "Playwright Tester 10:00-11:00" }).ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Cancel booking" }).ClickAsync();
        await Page.GetByRole(AriaRole.Dialog).GetByRole(AriaRole.Button, new() { Name = "Cancel booking" }).ClickAsync();

        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Playwright Tester 10:00-11:00" })).Not.ToBeVisibleAsync();
    }

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();

        var cancellationToken = TestContext.Current.CancellationToken;

        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Bookly_AppHost>(cancellationToken);

        _app = await appHost.BuildAsync(cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);

        await _app.StartAsync(cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);

        await _app.ResourceNotifications.WaitForResourceHealthyAsync("Frontend", cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);
    }

    public override async ValueTask DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.DisposeAsync();
        }
    }
}
