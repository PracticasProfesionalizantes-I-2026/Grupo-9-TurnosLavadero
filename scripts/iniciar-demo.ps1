$ErrorActionPreference = "Stop"
Set-Location (Split-Path $PSScriptRoot -Parent)
dotnet restore TurnosLavadero.slnx
if ($LASTEXITCODE -ne 0) { throw "No se pudieron restaurar las dependencias." }
dotnet build TurnosLavadero.slnx --no-restore
if ($LASTEXITCODE -ne 0) { throw "La compilación falló." }
$names = @("ConnectionStrings__DefaultConnection", "TURNOSLAVADERO_SEED_ADMIN_EMAIL", "TURNOSLAVADERO_SEED_ADMIN_PASSWORD", "ASPNETCORE_ENVIRONMENT", "ASPNETCORE_URLS", "Recordatorios__Habilitados")
$previous = @{}
foreach ($name in $names) { $previous[$name] = [Environment]::GetEnvironmentVariable($name, "Process") }
try {
    $demoRun = [guid]::NewGuid().ToString("N")
    $demoEmail = "admin-demo-$demoRun@lavadero.local"
    $demoPassword = [guid]::NewGuid().ToString("N") + [guid]::NewGuid().ToString("N")
    $env:ConnectionStrings__DefaultConnection = "Data Source=$PWD/demo-presentacion.db"
    $env:TURNOSLAVADERO_SEED_ADMIN_EMAIL = $demoEmail
    $env:TURNOSLAVADERO_SEED_ADMIN_PASSWORD = $demoPassword
    $env:ASPNETCORE_ENVIRONMENT = "Development"
    $env:ASPNETCORE_URLS = "http://localhost:5088"
    $env:Recordatorios__Habilitados = "false"
    $brunoEnvironment = @"
vars {
  baseUrl: http://localhost:5088
  adminEmail: $demoEmail
  adminPassword: $demoPassword
  token:
  clienteId:
  servicioId:
  turnoId:
}
"@
    Set-Content -Path "bruno/environments/DemoLocal.bru" -Value $brunoEnvironment -Encoding UTF8
    Write-Host "Bruno: entorno DemoLocal, carpeta 00 Demo, ejecutar en orden."
    dotnet run --no-build --no-launch-profile --project API
} finally {
    foreach ($name in $names) { [Environment]::SetEnvironmentVariable($name, $previous[$name], "Process") }
}
