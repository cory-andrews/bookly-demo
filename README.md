# Bookly
A demo application showing how to build a simple room booking system (including double booking prevention) using:
- Aspire 13.6
- ASP.NET Core 10
- EF Core
- SQL Server 2022

## Getting Started
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
dotnet run --project .\tests\IntegrationTests
```