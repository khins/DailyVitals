using DailyVitals.Web.Components;
using DailyVitals.Web.Configuration;
using DailyVitals.Web.Health;
using DailyVitals.Web.Services;
using DailyVitals.Data.Configuration;
using DailyVitals.Data.Migrations;
using DailyVitals.Data.Services;
using DailyVitals.Data.Services.DailyVitals.App.Services;
using DailyVitals.Domain.Models.Calculations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HostFiltering;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Globalization;
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

    AddAllowedHost(allowedHosts, builder.Configuration["RAILWAY_PUBLIC_DOMAIN"]);
    AddAllowedHost(allowedHosts, builder.Configuration["RAILWAY_STATIC_URL"]);
    AddAllowedHost(allowedHosts, "myactivevitals-production.up.railway.app");
    AddAllowedHost(allowedHosts, "myactivevitals.com");
    AddAllowedHost(allowedHosts, "www.myactivevitals.com");

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
        options.LoginPath = "/signin";
        options.AccessDeniedPath = "/signin";
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
builder.Services.AddHttpContextAccessor();
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
var blazorRuntimePath = app.Environment.IsDevelopment() ? null : Path.Combine(
    AppContext.BaseDirectory,
    "wwwroot",
    "_framework",
    "blazor.web.js");

if (blazorRuntimePath is not null && !File.Exists(blazorRuntimePath))
{
    throw new FileNotFoundException(
        "The published Blazor runtime is missing. Interactive pages cannot start.",
        blazorRuntimePath);
}

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

// MapStaticAssets provides optimized endpoint metadata, while UseStaticFiles
// guarantees framework boot resources are served from the published wwwroot.
// Without blazor.web.js, server-rendered pages display normally but none of the
// interactive component event handlers can run.
app.UseStaticFiles();

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

app.MapPost("/auth/login", async (
    HttpContext httpContext,
    LocalLoginService loginService) =>
{
    var form = await httpContext.Request.ReadFormAsync(httpContext.RequestAborted);
    var userName = form["UserName"].ToString().Trim();
    var password = form["Password"].ToString();
    var rememberDevice = IsChecked(form["RememberMe"].ToString());

    if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
        return Results.Redirect("/signin?signin=missing");

    var loginResult = loginService.Authenticate(userName, password);
    if (loginResult.IsLocked)
        return Results.Redirect("/signin?signin=locked");

    var loginUser = loginResult.LoginUser;
    if (loginUser?.PersonId is not > 0)
        return Results.Redirect("/signin?signin=invalid");

    var claims = new[]
    {
        new Claim(ClaimTypes.Name, loginUser.UserName),
        new Claim(LocalLoginSession.AuthClaimTypes.PersonId, loginUser.PersonId.Value.ToString()),
        new Claim(LocalLoginSession.AuthClaimTypes.IsDemo, loginUser.IsDemo.ToString()),
        new Claim(LocalLoginSession.AuthClaimTypes.RememberDevice, rememberDevice.ToString())
    };
    var principal = new ClaimsPrincipal(
        new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
    var properties = new AuthenticationProperties
    {
        IsPersistent = rememberDevice,
        AllowRefresh = true,
        ExpiresUtc = rememberDevice ? DateTimeOffset.UtcNow.AddDays(30) : null
    };

    await httpContext.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        principal,
        properties);

    return Results.Redirect("/dashboard");
}).DisableAntiforgery();

app.MapPost("/auth/register", async (
    HttpContext httpContext,
    PersonService personService,
    LoginUserService loginUserService,
    ILoggerFactory loggerFactory) =>
{
    var logger = loggerFactory.CreateLogger("AccountRegistrationEndpoint");
    var form = await httpContext.Request.ReadFormAsync(httpContext.RequestAborted);
    var firstName = form["FirstName"].ToString().Trim();
    var lastName = form["LastName"].ToString().Trim();
    var userName = form["UserName"].ToString().Trim();
    var password = form["Password"].ToString();
    var confirmPassword = form["ConfirmPassword"].ToString();

    if (string.IsNullOrWhiteSpace(firstName) ||
        string.IsNullOrWhiteSpace(lastName) ||
        string.IsNullOrWhiteSpace(userName) ||
        string.IsNullOrWhiteSpace(password) ||
        string.IsNullOrWhiteSpace(confirmPassword))
    {
        return Results.Redirect("/create-account?registration=missing");
    }

    if (firstName.Length > 100 || lastName.Length > 100 || userName.Length > 255)
        return Results.Redirect("/create-account?registration=invalid");

    if (!System.Net.Mail.MailAddress.TryCreate(userName, out var emailAddress) ||
        !string.Equals(emailAddress.Address, userName, StringComparison.OrdinalIgnoreCase))
    {
        return Results.Redirect("/create-account?registration=invalid-email");
    }

    if (password.Length < 8)
        return Results.Redirect("/create-account?registration=short-password");

    if (!string.Equals(password, confirmPassword, StringComparison.Ordinal))
        return Results.Redirect("/create-account?registration=password-mismatch");

    if (loginUserService.UserNameExists(userName))
        return Results.Redirect("/create-account?registration=email-in-use");

    try
    {
        var personId = personService.InsertPerson(firstName, lastName, null, null, null);
        var loginUser = loginUserService.CreateLoginUser(personId, userName, password);

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, loginUser.UserName),
            new Claim(LocalLoginSession.AuthClaimTypes.PersonId, personId.ToString()),
            new Claim(LocalLoginSession.AuthClaimTypes.IsDemo, bool.FalseString),
            new Claim(LocalLoginSession.AuthClaimTypes.RememberDevice, bool.TrueString)
        };
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        var properties = new AuthenticationProperties
        {
            IsPersistent = true,
            AllowRefresh = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
        };

        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            properties);

        return Results.Redirect("/profile");
    }
    catch (InvalidOperationException ex) when (
        ex.Message.Contains("already in use", StringComparison.OrdinalIgnoreCase))
    {
        return Results.Redirect("/create-account?registration=email-in-use");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Account registration failed for {UserName}.", userName);
        return Results.Redirect("/create-account?registration=failed");
    }
}).DisableAntiforgery();

app.MapPost("/blood-pressure/save", async (
    HttpContext httpContext,
    BloodPressureService bloodPressureService,
    ILoggerFactory loggerFactory) =>
{
    var logger = loggerFactory.CreateLogger("BloodPressureSaveEndpoint");
    var principal = httpContext.User;
    var personIdValue = principal.FindFirstValue(LocalLoginSession.AuthClaimTypes.PersonId);
    if (principal.Identity?.IsAuthenticated != true ||
        !long.TryParse(personIdValue, out var personId) ||
        personId <= 0)
    {
        return Results.Redirect("/signin?signin=invalid");
    }

    var isDemo = string.Equals(
        principal.FindFirstValue(LocalLoginSession.AuthClaimTypes.IsDemo),
        bool.TrueString,
        StringComparison.OrdinalIgnoreCase);
    if (isDemo)
        return Results.Redirect("/blood-pressure?status=demo");

    var form = await httpContext.Request.ReadFormAsync(httpContext.RequestAborted);
    var systolicText = form["Systolic"].ToString();
    var diastolicText = form["Diastolic"].ToString();
    var pulseText = form["Pulse"].ToString();
    var readingTimeText = form["ReadingTimeText"].ToString();
    var notes = form["Notes"].ToString();
    var editingIdText = form["EditingId"].ToString();
    var userName = principal.Identity.Name ?? string.Empty;

    if (!int.TryParse(systolicText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var systolic) ||
        !int.TryParse(diastolicText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var diastolic) ||
        !int.TryParse(pulseText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var pulse) ||
        !DateTime.TryParse(readingTimeText, CultureInfo.InvariantCulture, DateTimeStyles.None, out var readingTime))
    {
        return Results.Redirect("/blood-pressure?status=invalid");
    }

    if (systolic <= diastolic)
        return Results.Redirect("/blood-pressure?status=invalid");

    try
    {
        if (long.TryParse(editingIdText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var editingId) &&
            editingId > 0)
        {
            var updated = bloodPressureService.UpdateBloodPressureForPerson(
                personId,
                editingId,
                systolic,
                diastolic,
                pulse,
                readingTime,
                notes,
                userName);

            return Results.Redirect(updated
                ? "/blood-pressure?status=updated"
                : "/blood-pressure?status=not-found");
        }

        bloodPressureService.InsertBloodPressure(
            personId,
            systolic,
            diastolic,
            pulse,
            readingTime,
            notes,
            userName);

        return Results.Redirect("/blood-pressure?status=saved");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Blood pressure server save failed.");
        return Results.Redirect("/blood-pressure?status=save-error");
    }
}).DisableAntiforgery();

app.MapPost("/blood-glucose/save", async (
    HttpContext httpContext,
    BloodGlucoseService bloodGlucoseService,
    ILoggerFactory loggerFactory) =>
{
    var logger = loggerFactory.CreateLogger("BloodGlucoseSaveEndpoint");
    var principal = httpContext.User;
    var personIdValue = principal.FindFirstValue(LocalLoginSession.AuthClaimTypes.PersonId);
    if (principal.Identity?.IsAuthenticated != true ||
        !long.TryParse(personIdValue, out var personId) ||
        personId <= 0)
    {
        return Results.Redirect("/signin?signin=invalid");
    }

    var isDemo = string.Equals(
        principal.FindFirstValue(LocalLoginSession.AuthClaimTypes.IsDemo),
        bool.TrueString,
        StringComparison.OrdinalIgnoreCase);
    if (isDemo)
        return Results.Redirect("/blood-glucose?status=demo");

    var form = await httpContext.Request.ReadFormAsync(httpContext.RequestAborted);
    var glucoseText = form["GlucoseValue"].ToString();
    var readingTimeText = form["ReadingTimeText"].ToString();
    var notes = form["Notes"].ToString();
    var editingIdText = form["EditingId"].ToString();
    var userName = principal.Identity.Name ?? string.Empty;

    if (!int.TryParse(glucoseText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var glucoseValue) ||
        glucoseValue <= 0 ||
        !DateTime.TryParse(readingTimeText, CultureInfo.InvariantCulture, DateTimeStyles.None, out var readingTime))
    {
        return Results.Redirect("/blood-glucose?status=invalid");
    }

    try
    {
        if (long.TryParse(editingIdText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var editingId) &&
            editingId > 0)
        {
            bloodGlucoseService.Update(
                editingId,
                personId,
                glucoseValue,
                readingTime,
                notes,
                userName);

            return Results.Redirect("/blood-glucose?status=updated");
        }

        bloodGlucoseService.Insert(
            personId,
            glucoseValue,
            readingTime,
            notes,
            userName);

        return Results.Redirect("/blood-glucose?status=saved");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Blood glucose server save failed.");
        return Results.Redirect("/blood-glucose?status=save-error");
    }
}).DisableAntiforgery();

app.MapPost("/weight/save", async (
    HttpContext httpContext,
    WeightService weightService,
    PersonService personService,
    ILoggerFactory loggerFactory) =>
{
    var logger = loggerFactory.CreateLogger("WeightSaveEndpoint");
    var principal = httpContext.User;
    var personIdValue = principal.FindFirstValue(LocalLoginSession.AuthClaimTypes.PersonId);
    if (principal.Identity?.IsAuthenticated != true ||
        !long.TryParse(personIdValue, out var personId) ||
        personId <= 0)
    {
        return Results.Redirect("/signin?signin=invalid");
    }

    var isDemo = string.Equals(
        principal.FindFirstValue(LocalLoginSession.AuthClaimTypes.IsDemo),
        bool.TrueString,
        StringComparison.OrdinalIgnoreCase);
    if (isDemo)
        return Results.Redirect("/weight?status=demo");

    var heightFt = personService.GetPersonById(personId)?.HeightFt;
    if (!heightFt.HasValue)
        return Results.Redirect("/weight?status=height-required");

    var form = await httpContext.Request.ReadFormAsync(httpContext.RequestAborted);
    var weightText = form["WeightValue"].ToString();
    var weightUnit = form["WeightUnit"].ToString();
    var readingTimeText = form["ReadingTimeText"].ToString();
    var notes = form["Notes"].ToString();
    var editingIdText = form["EditingId"].ToString();
    var userName = principal.Identity.Name ?? string.Empty;

    if (string.IsNullOrWhiteSpace(weightUnit))
        weightUnit = "lb";

    if (!decimal.TryParse(weightText, NumberStyles.Number, CultureInfo.InvariantCulture, out var weightValue) ||
        !IsWeightInValidRange(weightValue, weightUnit) ||
        !DateTime.TryParse(readingTimeText, CultureInfo.InvariantCulture, DateTimeStyles.None, out var readingTime))
    {
        return Results.Redirect("/weight?status=invalid");
    }

    try
    {
        if (long.TryParse(editingIdText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var editingId) &&
            editingId > 0)
        {
            var updated = weightService.UpdateWeightForPerson(
                personId,
                editingId,
                weightValue,
                weightUnit,
                readingTime,
                notes,
                userName);

            return Results.Redirect(updated
                ? "/weight?status=updated"
                : "/weight?status=not-found");
        }

        weightService.InsertWeight(
            personId,
            weightValue,
            weightUnit,
            heightFt.Value,
            readingTime,
            notes,
            userName);

        return Results.Redirect("/weight?status=saved");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Weight server save failed.");
        return Results.Redirect("/weight?status=save-error");
    }
}).DisableAntiforgery();

app.MapPost("/exercise/save", async (
    HttpContext httpContext,
    ExerciseService exerciseService,
    WeightService weightService,
    ILoggerFactory loggerFactory) =>
{
    const long otherExerciseTypeId = -1;
    var logger = loggerFactory.CreateLogger("ExerciseSaveEndpoint");
    var principal = httpContext.User;
    var personIdValue = principal.FindFirstValue(LocalLoginSession.AuthClaimTypes.PersonId);
    if (principal.Identity?.IsAuthenticated != true ||
        !long.TryParse(personIdValue, out var personId) ||
        personId <= 0)
    {
        return Results.Redirect("/signin?signin=invalid");
    }

    var isDemo = string.Equals(
        principal.FindFirstValue(LocalLoginSession.AuthClaimTypes.IsDemo),
        bool.TrueString,
        StringComparison.OrdinalIgnoreCase);
    if (isDemo)
        return Results.Redirect("/exercise?status=demo");

    var form = await httpContext.Request.ReadFormAsync(httpContext.RequestAborted);
    var exerciseTypeText = form["ExerciseTypeId"].ToString();
    var otherExerciseName = form["OtherExerciseName"].ToString();
    var durationText = form["DurationMinutes"].ToString();
    var caloriesText = form["CaloriesExpended"].ToString();
    var intensity = form["Intensity"].ToString();
    var startTimeText = form["StartTimeText"].ToString();
    var notes = form["Notes"].ToString();
    var editingIdText = form["EditingId"].ToString();
    var userName = principal.Identity.Name ?? string.Empty;

    if (string.IsNullOrWhiteSpace(intensity))
        intensity = "Moderate";

    if (!long.TryParse(exerciseTypeText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var exerciseTypeId))
        return Results.Redirect("/exercise?status=missing-exercise");

    if (exerciseTypeId == otherExerciseTypeId)
    {
        if (string.IsNullOrWhiteSpace(otherExerciseName))
            return Results.Redirect("/exercise?status=missing-other");

        exerciseTypeId = exerciseService.GetOrCreateExerciseType(otherExerciseName);
    }

    if (!decimal.TryParse(durationText, NumberStyles.Number, CultureInfo.InvariantCulture, out var durationMinutes) ||
        durationMinutes <= 0 ||
        !DateTime.TryParse(startTimeText, CultureInfo.InvariantCulture, DateTimeStyles.None, out var startTime))
    {
        return Results.Redirect("/exercise?status=invalid");
    }

    decimal? calories = null;
    if (!string.IsNullOrWhiteSpace(caloriesText))
    {
        if (!decimal.TryParse(caloriesText, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedCalories) ||
            parsedCalories < 0)
        {
            return Results.Redirect("/exercise?status=invalid-calories");
        }

        calories = parsedCalories;
    }
    else
    {
        var latestWeight = weightService.GetLatestForPerson(personId);
        if (latestWeight is not null)
        {
            calories = ExerciseMetrics.EstimateCaloriesBurned(
                durationMinutes,
                intensity,
                latestWeight.WeightValue,
                latestWeight.WeightUnit);
        }
    }

    try
    {
        if (long.TryParse(editingIdText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var editingId) &&
            editingId > 0)
        {
            var updated = exerciseService.UpdateExerciseSessionForPerson(
                editingId,
                personId,
                exerciseTypeId,
                startTime,
                durationMinutes,
                calories,
                intensity,
                notes,
                userName);

            return Results.Redirect(updated
                ? "/exercise?status=updated"
                : "/exercise?status=not-found");
        }

        exerciseService.InsertExerciseSession(
            personId,
            exerciseTypeId,
            startTime,
            durationMinutes,
            calories,
            intensity,
            notes,
            userName);

        return Results.Redirect("/exercise?status=saved");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Exercise server save failed.");
        return Results.Redirect("/exercise?status=save-error");
    }
}).DisableAntiforgery();

app.MapPost("/exercise/delete", async (
    HttpContext httpContext,
    ExerciseService exerciseService,
    ILoggerFactory loggerFactory) =>
{
    var logger = loggerFactory.CreateLogger("ExerciseDeleteEndpoint");
    var principal = httpContext.User;
    var personIdValue = principal.FindFirstValue(LocalLoginSession.AuthClaimTypes.PersonId);
    if (principal.Identity?.IsAuthenticated != true ||
        !long.TryParse(personIdValue, out var personId) ||
        personId <= 0)
    {
        return Results.Redirect("/signin?signin=invalid");
    }

    var isDemo = string.Equals(
        principal.FindFirstValue(LocalLoginSession.AuthClaimTypes.IsDemo),
        bool.TrueString,
        StringComparison.OrdinalIgnoreCase);
    if (isDemo)
        return Results.Redirect("/exercise?status=demo");

    var form = await httpContext.Request.ReadFormAsync(httpContext.RequestAborted);
    var editingIdText = form["EditingId"].ToString();

    if (!long.TryParse(editingIdText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var exerciseSessionId) ||
        exerciseSessionId <= 0)
    {
        return Results.Redirect("/exercise?status=not-found");
    }

    try
    {
        var deleted = exerciseService.DeleteExerciseSessionForPerson(personId, exerciseSessionId);
        return Results.Redirect(deleted
            ? "/exercise?status=deleted"
            : "/exercise?status=not-found");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Exercise server delete failed.");
        return Results.Redirect("/exercise?status=delete-error");
    }
}).DisableAntiforgery();

app.MapPost("/fluid-intake/save", async (
    HttpContext httpContext,
    FluidIntakeService fluidIntakeService,
    ILoggerFactory loggerFactory) =>
{
    const string fluidOuncesUnit = "fl oz";
    const string millilitersUnit = "mL";
    const decimal millilitersPerUsFluidOunce = 29.5735295625m;
    var logger = loggerFactory.CreateLogger("FluidIntakeSaveEndpoint");
    var principal = httpContext.User;
    var personIdValue = principal.FindFirstValue(LocalLoginSession.AuthClaimTypes.PersonId);
    if (principal.Identity?.IsAuthenticated != true ||
        !long.TryParse(personIdValue, out var personId) ||
        personId <= 0)
    {
        return Results.Redirect("/signin?signin=invalid");
    }

    var isDemo = string.Equals(
        principal.FindFirstValue(LocalLoginSession.AuthClaimTypes.IsDemo),
        bool.TrueString,
        StringComparison.OrdinalIgnoreCase);
    if (isDemo)
        return Results.Redirect("/fluid-intake?status=demo");

    var form = await httpContext.Request.ReadFormAsync(httpContext.RequestAborted);
    var beverageName = form["BeverageName"].ToString().Trim();
    var amountText = form["Amount"].ToString();
    var unit = form["Unit"].ToString();
    var consumedAtText = form["ConsumedAtText"].ToString();
    var notes = form["Notes"].ToString().Trim();
    var editingIdText = form["EditingId"].ToString();
    var userName = principal.Identity.Name ?? string.Empty;

    if (string.IsNullOrWhiteSpace(beverageName) || beverageName.Length > 120)
        return Results.Redirect("/fluid-intake?status=missing-beverage");

    if (!string.Equals(unit, fluidOuncesUnit, StringComparison.Ordinal) &&
        !string.Equals(unit, millilitersUnit, StringComparison.Ordinal))
    {
        unit = fluidOuncesUnit;
    }

    if (!DateTime.TryParse(consumedAtText, CultureInfo.InvariantCulture, DateTimeStyles.None, out var consumedAt))
        return Results.Redirect("/fluid-intake?status=invalid-time");

    if (!decimal.TryParse(amountText, NumberStyles.Number, CultureInfo.InvariantCulture, out var enteredAmount) ||
        enteredAmount <= 0)
    {
        return Results.Redirect("/fluid-intake?status=invalid-amount");
    }

    var convertedMl = string.Equals(unit, fluidOuncesUnit, StringComparison.Ordinal)
        ? enteredAmount * millilitersPerUsFluidOunce
        : enteredAmount;

    convertedMl = Math.Round(convertedMl, 0, MidpointRounding.AwayFromZero);
    if (convertedMl < 1m || convertedMl > 10000m)
        return Results.Redirect("/fluid-intake?status=invalid-amount");

    var fluidMl = decimal.ToInt32(convertedMl);

    try
    {
        if (long.TryParse(editingIdText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var editingId) &&
            editingId > 0)
        {
            var updated = fluidIntakeService.UpdateForPerson(
                editingId,
                personId,
                consumedAt,
                fluidMl,
                enteredAmount,
                unit,
                beverageName,
                notes,
                userName);

            return Results.Redirect(updated
                ? "/fluid-intake?status=updated"
                : "/fluid-intake?status=not-found");
        }

        fluidIntakeService.Insert(
            personId,
            consumedAt,
            fluidMl,
            enteredAmount,
            unit,
            beverageName,
            notes,
            userName);

        return Results.Redirect("/fluid-intake?status=saved");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Fluid intake server save failed.");
        return Results.Redirect("/fluid-intake?status=save-error");
    }
}).DisableAntiforgery();

app.MapPost("/auth/signout", async (HttpContext httpContext) =>
{
    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.NoContent();
}).DisableAntiforgery();

app.MapGet("/signout", async (HttpContext httpContext) =>
{
    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/");
}).AllowAnonymous();

if (blazorRuntimePath is not null)
{
    app.MapGet("/client/blazor.web.js", () =>
            Results.File(blazorRuntimePath, "text/javascript"))
        .AllowAnonymous();
}

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

static void AddAllowedHost(List<string> allowedHosts, string? configuredHost)
{
    var host = NormalizeHost(configuredHost);
    if (!string.IsNullOrWhiteSpace(host))
        allowedHosts.Add(host);
}

static string? NormalizeHost(string? configuredHost)
{
    if (string.IsNullOrWhiteSpace(configuredHost))
        return null;

    var trimmed = configuredHost.Trim();
    if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
        return uri.Host;

    var slashIndex = trimmed.IndexOf('/');
    if (slashIndex >= 0)
        trimmed = trimmed[..slashIndex];

    var colonIndex = trimmed.IndexOf(':');
    if (colonIndex > 0)
        trimmed = trimmed[..colonIndex];

    return trimmed;
}

static bool IsChecked(string? value)
{
    return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(value, "on", StringComparison.OrdinalIgnoreCase);
}

static bool IsWeightInValidRange(decimal weightValue, string weightUnit)
{
    return string.Equals(weightUnit, "kg", StringComparison.OrdinalIgnoreCase)
        ? weightValue is >= 3.0m and <= 500.0m
        : weightValue is >= 6.6m and <= 1102.3m;
}

internal sealed record AuthSessionRequest(string? Ticket);
