using DailyVitals.Web.Components;
using DailyVitals.Web.Services;
using DailyVitals.Data.Services;
using DailyVitals.Data.Services.DailyVitals.App.Services;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

var dataProtectionKeysPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "Keys");
Directory.CreateDirectory(dataProtectionKeysPath);

// Add services to the container.
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
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
builder.Services.AddScoped<DemoAccountSeeder>();

var app = builder.Build();

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
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
