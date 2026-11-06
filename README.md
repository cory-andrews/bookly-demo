# Bookly
A demo application showing how to build a simple room booking system (including double booking prevention) using:
- Aspire 13.6
- ASP.NET Core 10
- EF Core
- SQL Server 2022
- Vue 3 and Vite

## Getting Started
Running the application requires the .NET 10 SDK, PowerShell, Node.js 24 and a running Docker daemon (for SQL Server).

To run the application, it is recommended to use the Aspire CLI. You can install it from [here](https://aspire.dev/get-started/install-cli/). With the CLI installed, from the root of this repository simply execute:
```bash
aspire run
```

If you do not have the Aspire CLI installed, you can also run the application using the .NET CLI. First, ensure that you have the .NET 10 SDK installed. Then, from the root of the repository, execute:
```bash
dotnet run --project .\src\Aspire\AppHost
```

Both of these options will run the Aspire AppHost, which once started will display a link to the Aspire dashboard in your terminal. You can open this link in your browser to access the Aspire Dashboard which can be used as the launching point for accessing:
- Scalar UI for API docs
- Frontend app

## Running Tests
### Integration Tests
The integration tests for the API can be run by executing the following command from the root of the repository:
```bash
dotnet test .\tests\IntegrationTests
```

### Frontend Tests
The frontend's unit and interaction tests can be run by executing the following commands from the root of the repository:
```bash
cd src/Frontend
npm install
npm test
```

### End-to-End Tests
The end-to-end tests can be run by executing the following commands from the root of the repository:
```bash
dotnet build --project .\tests\E2ETests
pwsh .\tests\E2ETests\bin\Debug\net10.0\playwright.ps1 install --with-deps
dotnet test .\tests\E2ETests
```

**NOTE**: PowerShell is required to to install the Playwright dependencies.

## Preventing Double Bookings
The approach taken in this application to prevent double bookings relies heavily on EF Core's optimistic concurrency implementation. More specifically, I have opted to utilize [Application-managed concurrency tokens](https://learn.microsoft.com/en-us/ef/core/saving/concurrency?tabs=data-annotations#application-managed-concurrency-tokens).

When first attempting to create a booking, we will check the database for any existing bookings that might overlap the requested period for the given room. If there's no overlaps, we proceed to fetch the `Room` entity from the database and call it's `Book()` method:

```csharp
public sealed class Room
{
    ...

    [ConcurrencyCheck]
    public Guid ConcurrencyToken { get; private set; }

    public Booking Book(string occupantName, DateTime startsAtUtc, DateTime endsAtUtc)
    {
        ConcurrencyToken = Guid.NewGuid();

        return Booking.Create(this, occupantName, startsAtUtc, endsAtUtc);
    }
}
```
This method is key to preventing double-bookings. The `ConcurrencyToken` property on the `Room` instance gets updated everytime that room is booked. This adds the `Room` to the `DbContext`'s `ChangeTracker`, so when it times to complete the unit of work (i.e. save the booking), if the `ConcurrencyToken` property's previous value does not match what is currently in the database, EF Core will throw a `DbUpdateConcurrencyException`. This happens automatically due to the `[ConcurrencyCheck]` attribute that decorates the `ConcurrencyToken` property. If that exception gets thrown, we retry the operation from the beginning (e.g. perform an overlap check, fetch the `Room`, etc.).

The major downside to this approach is that we update the `Room` entity everytime it is booked, which means a concurrency exception could be thrown often if there's a popular room that gets booked frequently. The current implementation will keep retrying the operation automatically until it either fails the explicit overlap check, or ultimately succeeds in creating the booking.

An alternative approach considered was to have a double conflict check; one that happens before creating the booking, and one that happens after. If there's a conflict after the booking's already saved, we'd need to roll it back, which ends up causing a bit of a race condition of its own (e.g. what happens if the concurrent requests both rollback, and subsequently retry?). Overall, the additional complexity in that approach made it seem less viable than utilizing EF Core's built in functionality.

## What to Improve Next?
- Security (e.g. users, login, authorized endpoints, etc.)
- Modify bookings (e.g. adjust meeting length)

## AI Tool Usage
- Claude Design was used to generate the UI mockups.
- Claude Code was used to implement the Vue frontend from those mockups, including its tests.