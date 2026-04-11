param(
    [string]$BackupDirectory = "$env:USERPROFILE\OneDrive\Backups",
    [string]$Database = "DailyVitals",
    [string]$HostName = "localhost",
    [int]$Port = 5433,
    [string]$Username = "postgres",
    [string]$Password = "newpassword",
    [string]$PgDumpPath = "C:\Program Files\PostgreSQL\18\bin\pg_dump.exe"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $PgDumpPath)) {
    throw "pg_dump not found at '$PgDumpPath'."
}

New-Item -ItemType Directory -Force -Path $BackupDirectory | Out-Null

$timestamp = Get-Date -Format "yyyy-MM-dd_HHmmss"
$backupFile = Join-Path $BackupDirectory "${Database}_${timestamp}.backup"

try {
    $env:PGPASSWORD = $Password

    & $PgDumpPath `
        -h $HostName `
        -p $Port `
        -U $Username `
        -F c `
        -b `
        -v `
        -f $backupFile `
        $Database

    if ($LASTEXITCODE -ne 0) {
        throw "pg_dump failed with exit code $LASTEXITCODE."
    }

    $file = Get-Item -LiteralPath $backupFile
    Write-Host "Backup created:" $file.FullName
    Write-Host "Size:" $file.Length "bytes"
    Write-Host "Updated:" $file.LastWriteTime
}
finally {
    Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue
}
