using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.DataProtection;

namespace DailyVitals.Web.Configuration;

internal static class DataProtectionConfiguration
{
    private const string DefaultApplicationName = "DailyVitals.Web";

    public static void AddDailyVitalsDataProtection(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var section = configuration.GetSection("DataProtection");
        var configuredKeysPath = section["KeysPath"];
        var certificatePath = section["CertificatePath"];
        var certificatePassword = section["CertificatePassword"];
        var configuredApplicationName = section["ApplicationName"];
        var applicationName = string.IsNullOrWhiteSpace(configuredApplicationName)
            ? DefaultApplicationName
            : configuredApplicationName;

        if (!environment.IsDevelopment())
        {
            RequireProductionSetting(configuredKeysPath, "DataProtection:KeysPath");
            RequireProductionSetting(certificatePath, "DataProtection:CertificatePath");

            if (!Path.IsPathFullyQualified(configuredKeysPath!))
                throw new InvalidOperationException("DataProtection:KeysPath must be an absolute path in production.");

            if (!Path.IsPathFullyQualified(certificatePath!))
                throw new InvalidOperationException("DataProtection:CertificatePath must be an absolute path in production.");
        }

        var keysPath = string.IsNullOrWhiteSpace(configuredKeysPath)
            ? Path.Combine(environment.ContentRootPath, "App_Data", "Keys")
            : Path.GetFullPath(configuredKeysPath, environment.ContentRootPath);

        Directory.CreateDirectory(keysPath);

        var dataProtection = services.AddDataProtection()
            .SetApplicationName(applicationName)
            .PersistKeysToFileSystem(new DirectoryInfo(keysPath));

        if (!string.IsNullOrWhiteSpace(certificatePath))
        {
            var resolvedCertificatePath = Path.GetFullPath(certificatePath, environment.ContentRootPath);
            if (!File.Exists(resolvedCertificatePath))
                throw new InvalidOperationException($"The Data Protection certificate was not found at '{resolvedCertificatePath}'.");

            var certificate = X509CertificateLoader.LoadPkcs12FromFile(
                resolvedCertificatePath,
                certificatePassword,
                X509KeyStorageFlags.EphemeralKeySet);

            if (!certificate.HasPrivateKey)
                throw new InvalidOperationException("The Data Protection certificate must contain a private key.");

            var now = DateTime.UtcNow;
            if (now < certificate.NotBefore.ToUniversalTime() || now > certificate.NotAfter.ToUniversalTime())
                throw new InvalidOperationException("The Data Protection certificate is not currently valid.");

            dataProtection.ProtectKeysWithCertificate(certificate);
        }
    }

    private static void RequireProductionSetting(string? value, string settingName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"{settingName} is required outside Development. " +
                "Configure durable protected Data Protection key storage before starting the application.");
        }
    }
}
