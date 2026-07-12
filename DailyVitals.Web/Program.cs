using DailyVitals.Web.Components;
using DailyVitals.Web.Configuration;
using DailyVitals.Web.Health;
using DailyVitals.Web.Services;
using DailyVitals.Data.Configuration;
using DailyVitals.Data.Migrations;
using DailyVitals.Data.Services;
using DailyVitals.Data.Services.DailyVitals.App.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HostFiltering;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

DbConnectionFactory.Configure(
    builder.Configuration.GetConnectionString("DailyVitals"),
    builder.Configuration.GetConnectionString("DailyVitalsMigrations"));
OpenAiConfiguration.Configure(
    builder.Configuration["OpenAI:ApiKey"],
    builder.Configuration["OpenAI:Model"]);

// Add services to the container.
builder.Services.AddDailyVitalsDataProtection(builder.Configuration, builder.Environment);
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});
builder.Services.Configure<HostFilteringOptions>(options =>
{
    var allowedHosts = GetConfiguredAllowedHosts(builder.Configuration);
    var railwayPublicDomain = builder.Configuration["RAILWAY_PUBLIC_DOMAIN"];

    if (!string.IsNullOrWhiteSpace(railwayPublicDomain))
    {
        allowedHosts.Add(railwayPublicDomain.Trim());
    }

    if (allowedHosts.Count > 0)
    {
        options.AllowedHosts = allowedHosts
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
});

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/";
        options.AccessDeniedPath = "/";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseReadinessHealthCheck>(
        "database",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["ready"],
        timeout: TimeSpan.FromSeconds(5));
builder.Services.AddScoped<LoginUserService>();
builder.Services.AddScoped<BloodPressureService>();
builder.Services.AddScoped<BloodGlucoseService>();
builder.Services.AddScoped<WeightService>();
builder.Services.AddScoped<ExerciseService>();
builder.Services.AddScoped<FoodPhosphorusIntakeService>();
builder.Services.AddScoped<FoodPhosphorusEstimateService>();
builder.Services.AddScoped<NutritionCoachService>();
builder.Services.AddScoped<FluidIntakeService>();
builder.Services.AddScoped<NutritionGoalService>();
builder.Services.AddScoped<KidneyLabResultService>();
builder.Services.AddScoped<VitalThresholdService>();
builder.Services.AddScoped<RenalDietFoodService>();
builder.Services.AddScoped<PersonService>();
builder.Services.AddScoped<LocalLoginService>();
builder.Services.AddScoped<LocalLoginSession>();
builder.Services.AddSingleton<AuthTicketService>();
builder.Services.AddScoped<DemoAccountSeeder>();

var app = builder.Build();

var migrationRunner = new DatabaseMigrationRunner();
if (args.Contains("--migrate-only", StringComparer.OrdinalIgnoreCase))
{
    var appliedMigrations = await migrationRunner.RunAsync();
    app.Logger.LogInformation(
        "Database migration completed. Applied {MigrationCount} migration(s): {MigrationIds}",
        appliedMigrations.Count,
        string.Join(", ", appliedMigrations));
    return;
}

if (builder.Configuration.GetValue("DatabaseMigrations:RunOnStartup", builder.Environment.IsDevelopment()))
{
    var appliedMigrations = await migrationRunner.RunAsync();
    if (appliedMigrations.Count > 0)
        app.Logger.LogInformation("Applied database migrations: {MigrationIds}", string.Join(", ", appliedMigrations));
}
else
{
    var pendingMigrations = await migrationRunner.GetPendingMigrationIdsAsync();
    if (pendingMigrations.Count > 0)
    {
        throw new InvalidOperationException(
            $"Database has pending migrations: {string.Join(", ", pendingMigrations)}. " +
            "Run DailyVitals.Web with --migrate-only before starting the application.");
    }
}

await DbConnectionFactory.ValidateRuntimeSecurityAsync();

if (builder.Configuration.GetValue("DemoMode:Enabled", false))
{
    try
    {
        using var scope = app.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<DemoAccountSeeder>().EnsureSeeded();
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Demo Mode could not be initialized.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseForwardedHeaders();
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseWhen(
    context => !context.Request.Path.StartsWithSegments("/health"),
    branch => branch.UseHttpsRedirection());

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapHealthChecks(
    "/health/live",
    new HealthCheckOptions
    {
        Predicate = _ => false,
        ResponseWriter = WriteHealthResponseAsync
    })
    .AllowAnonymous();

app.MapHealthChecks(
    "/health/ready",
    new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready"),
        ResponseWriter = WriteHealthResponseAsync
    })
    .AllowAnonymous();

app.MapPost("/auth/session", async (
    AuthSessionRequest request,
    HttpContext httpContext,
    AuthTicketService ticketService) =>
{
    if (!ticketService.TryRedeem(request.Ticket, out var ticket))
        return Results.Unauthorized();

    var claims = new[]
    {
        new Claim(ClaimTypes.Name, ticket.UserName),
        new Claim(LocalLoginSession.AuthClaimTypes.PersonId, ticket.PersonId.ToString()),
        new Claim(LocalLoginSession.AuthClaimTypes.IsDemo, ticket.IsDemo.ToString()),
        new Claim(LocalLoginSession.AuthClaimTypes.RememberDevice, ticket.RememberDevice.ToString())
    };
    var principal = new ClaimsPrincipal(
        new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
    var properties = new AuthenticationProperties
    {
        IsPersistent = ticket.RememberDevice,
        AllowRefresh = true,
        ExpiresUtc = ticket.RememberDevice ? DateTimeOffset.UtcNow.AddDays(30) : null
    };

    await httpContext.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        principal,
        properties);
    return Results.NoContent();
}).DisableAntiforgery();

app.MapPost("/auth/signout", async (HttpContext httpContext) =>
{
    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.NoContent();
}).DisableAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

static Task WriteHealthResponseAsync(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json";
    return context.Response.WriteAsJsonAsync(
        new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString()
            })
        },
        context.RequestAborted);
}

static List<string> GetConfiguredAllowedHosts(IConfiguration configuration)
{
    return configuration["AllowedHosts"]?
        .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .ToList()
        ?? [];
}

internal sealed record AuthSessionRequest(string? Ticket);
