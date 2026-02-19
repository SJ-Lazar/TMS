using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Context;
using TMS.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

const string CorsPolicy = "ClientCors";

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<TMS.Application.Services.TicketService>();

// Allow the Blazor WASM host to call the API (adjust origins as needed)
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy =>
    {
        policy
            .WithOrigins(
                "https://localhost:5001", // API default dev URL
                "http://localhost:5001",
                "https://localhost:5002", // Blazor dev URL (if using separate port)
                "http://localhost:5002")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// Global exception handling (ProblemDetails)
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();
        var ex = exceptionHandlerFeature?.Error;

        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
        context.Response.ContentType = "application/problem+json";

        // Default to 500 unless we have a known "client" error.
        var statusCode = ex is ArgumentException or InvalidOperationException
            ? StatusCodes.Status400BadRequest
            : StatusCodes.Status500InternalServerError;

        context.Response.StatusCode = statusCode;

        Log.Error(ex, "Unhandled exception. TraceId: {TraceId}", traceId);

        var problem = Results.Problem(
            title: statusCode == StatusCodes.Status400BadRequest ? "Bad request" : "Server error",
            detail: app.Environment.IsDevelopment() ? ex?.Message : null,
            statusCode: statusCode,
            instance: context.Request.Path,
            extensions: new Dictionary<string, object?> { ["traceId"] = traceId });

        await problem.ExecuteAsync(context);
    });
});

// Correlation id in logs for every request
app.Use(async (context, next) =>
{
    var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
    using (LogContext.PushProperty("TraceId", traceId))
    {
        context.Response.Headers.TryAdd("X-Trace-Id", traceId);
        await next();
    }
});

app.UseSerilogRequestLogging();

app.UseHttpsRedirection();

app.UseCors(CorsPolicy);

app.UseAuthorization();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Scalar reference UI for OpenAPI (use this page to test the API during development)
    app.MapScalarApiReference(options =>
    {
        options.Title = "TMS API";
    });

    // Make Scalar the default page in development (redirect root to /scalar)
    app.MapGet("/", () => Results.Redirect("/scalar", permanent: false));
}

app.MapControllers();

// Ensure DB exists and seed.
// For production scenarios, prefer migrations.
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbInit");
    var dbContext = scope.ServiceProvider.GetRequiredService<TMS.Infrastructure.Persistence.TmsDbContext>();

    var strategy = dbContext.Database.CreateExecutionStrategy();
    await strategy.ExecuteAsync(async () =>
    {
        try
        {
            await dbContext.Database.MigrateAsync();
            await TMS.Infrastructure.Persistence.SeedData.EnsureRosterAsync(dbContext);
            await TMS.Infrastructure.Persistence.SeedData.EnsureTicketsAsync(dbContext);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database initialization failed");
            throw;
        }
    });
}

try
{
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
