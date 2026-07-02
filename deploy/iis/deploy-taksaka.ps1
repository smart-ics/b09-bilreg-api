# Deploy Taksaka to IIS (D:\Taksaka)
# Run this script in an elevated PowerShell (Administrator).

$ErrorActionPreference = 'Stop'

$RepoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$ApiTarget = 'D:\Taksaka\api'
$PortalTarget = 'D:\Taksaka\portal'
$SiteRoot = 'D:\Taksaka'
$PublishDir = Join-Path $RepoRoot 'src\taksaka.backend\Taksaka.Server\bin\Release\net8.0\publish'
$PortalDist = Join-Path $RepoRoot 'src\taksaka.frontend\Taksaka.Web\dist'

Write-Host 'Building backend...'
Push-Location (Join-Path $RepoRoot 'src\taksaka.backend')
dotnet publish Taksaka.Server\Taksaka.Server.csproj -c Release -o $PublishDir --no-self-contained
Pop-Location

Write-Host 'Building frontend...'
Push-Location (Join-Path $RepoRoot 'src\taksaka.frontend\Taksaka.Web')
npm run build
Pop-Location

Write-Host 'Stopping IIS...'
iisreset /stop

Write-Host 'Deploying API...'
New-Item -ItemType Directory -Force -Path $ApiTarget | Out-Null
Copy-Item -Path (Join-Path $PublishDir '*') -Destination $ApiTarget -Recurse -Force

Write-Host 'Deploying portal...'
New-Item -ItemType Directory -Force -Path $PortalTarget | Out-Null
Copy-Item -Path (Join-Path $PortalDist '*') -Destination $PortalTarget -Recurse -Force

Write-Host 'Installing site root web.config...'
Copy-Item -Path (Join-Path $RepoRoot 'deploy\iis\Taksaka-site-web.config') -Destination (Join-Path $SiteRoot 'web.config') -Force

Write-Host 'Starting IIS...'
iisreset /start

Write-Host 'Done. Verify:'
Write-Host '  http://localhost:8081/api/'
Write-Host '  http://localhost:8081/api/health'
Write-Host '  http://localhost:8081/portal/'
