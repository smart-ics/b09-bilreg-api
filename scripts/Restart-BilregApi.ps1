<#
.SYNOPSIS
    Menghentikan proses host Bilreg.Api aktif, mengompilasi ulang projek host API,
    memverifikasi kebaruan timestamp binary terhadap source code, menyalakan kembali
    daemon API di background, dan memeriksa ketersediaan service melalui healthcheck ping.

.DESCRIPTION
    Skrip otomasi siklus hidup proses host dev Bilreg.Api (TD-43) untuk memastikan runtime server
    selalu mencerminkan binary assembly terbaru hasil kompilasi. Mengimplementasikan 5 tahapan:
    - Step 1: Stop Process: Get-Process -Name "Bilreg.Api" -ErrorAction SilentlyContinue | Stop-Process -Force
    - Step 2: Build Executable: dotnet build src/bilreg/Bilreg.Api/Bilreg.Api.csproj -c Debug ($LASTEXITCODE -eq 0)
    - Step 3: Timestamp Verification: Validasi LastWriteTime Bilreg.Application.dll & Bilreg.Api.dll terhadap source code
    - Step 4: Launch Daemon: Start-Process -FilePath ".../Bilreg.Api.exe" -WindowStyle Hidden
    - Step 5: Healthcheck: Polling endpoint HTTP hingga menerima status 200/OK dalam batas waktu 10-15 detik

.PARAMETER Urls
    Binding URL untuk Kestrel ASP.NET Core. Default: "http://localhost:5000;http://localhost:5189"

.PARAMETER HealthCheckUrl
    URL endpoint untuk healthcheck polling. Jika tidak dispesifikasikan, otomatis menggunakan <FirstUrl>/openapi/v1.json.

.PARAMETER TimeoutSeconds
    Batas waktu maksimum polling healthcheck dalam detik. Default: 15.

.PARAMETER SkipBuild
    Opsi untuk melewati proses kompilasi dotnet build (hanya me-restart daemon).

.EXAMPLE
    .\scripts\Restart-BilregApi.ps1

.EXAMPLE
    .\scripts\Restart-BilregApi.ps1 -Urls "http://localhost:5000" -TimeoutSeconds 15
#>

[CmdletBinding()]
param(
    [string]$Urls = "http://localhost:5000;http://localhost:5189",
    [string]$HealthCheckUrl = "",
    [int]$TimeoutSeconds = 15,
    [switch]$SkipBuild,
    [switch]$Wait
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Menentukan lokasi root repositori b09-bilreg-api
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = (Resolve-Path (Join-Path $ScriptDir "..")).Path

# Menentukan path projek, binary, dan file referensi source code
$CsprojPath = Join-Path $RepoRoot "src\bilreg\Bilreg.Api\Bilreg.Api.csproj"
$ProjectDir = Join-Path $RepoRoot "src\bilreg\Bilreg.Api"
$BinDir = Join-Path $RepoRoot "src\bilreg\Bilreg.Api\bin\Debug\net8.0"
$ExePath = Join-Path $BinDir "Bilreg.Api.exe"
$DllAppPath = Join-Path $BinDir "Bilreg.Application.dll"
$DllApiPath = Join-Path $BinDir "Bilreg.Api.dll"
$SourceRefPath = Join-Path $RepoRoot "src\bilreg\Bilreg.Application\BedUsageContext\KamarOperasiFeature\UseCases\OkScheduleOpSetCommand.cs"

# Tentukan HealthCheckUrl default jika tidak diisi
if ([string]::IsNullOrWhiteSpace($HealthCheckUrl)) {
    $firstUrl = ($Urls -split ';')[0].TrimEnd('/')
    $HealthCheckUrl = "$firstUrl/openapi/v1.json"
}

Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "  BILREG.API DEV-HOST LIFECYCLE AUTOMATION (TD-43)" -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host " Repo Root        : $RepoRoot"
Write-Host " Project File     : $CsprojPath"
Write-Host " Target Exe       : $ExePath"
Write-Host " Binding URLs     : $Urls"
Write-Host " Health Check URL : $HealthCheckUrl"
Write-Host "=================================================================`n"

# -------------------------------------------------------------------------
# Step 1: Stop Process
# -------------------------------------------------------------------------
Write-Host "[Step 1/5] Menghentikan Proses Bilreg.Api Aktif..." -ForegroundColor Yellow
$killedPids = @()
$runningProcesses = Get-Process -Name "Bilreg.Api" -ErrorAction SilentlyContinue

if ($runningProcesses) {
    foreach ($proc in $runningProcesses) {
        $killedPids += $proc.Id
        Write-Host "  -> Menghentikan proses PID $($proc.Id) (StartTime: $($proc.StartTime))..." -ForegroundColor DarkYellow
        Stop-Process -Id $proc.Id -Force
    }
    
    # Tunggu hingga proses benar-benar terminasi
    $stopTimeout = 10
    $stopStopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    while ($stopStopwatch.Elapsed.TotalSeconds -lt $stopTimeout) {
        $check = Get-Process -Name "Bilreg.Api" -ErrorAction SilentlyContinue
        if (-not $check) {
            break
        }
        Start-Sleep -Milliseconds 500
    }
    
    $stillRunning = Get-Process -Name "Bilreg.Api" -ErrorAction SilentlyContinue
    if ($stillRunning) {
        throw "Gagal menghentikan seluruh proses Bilreg.Api! Proses yang masih berjalan: $($stillRunning.Id -join ', ')"
    }
    Write-Host "  [OK] Berhasil menghentikan $($killedPids.Count) proses Bilreg.Api (PID: $($killedPids -join ', '))." -ForegroundColor Green
} else {
    Write-Host "  [INFO] Tidak ada proses Bilreg.Api yang sedang aktif." -ForegroundColor Gray
}

# -------------------------------------------------------------------------
# Step 2: Build Executable
# -------------------------------------------------------------------------
Write-Host "`n[Step 2/5] Mengompilasi Ulang Host API (Bilreg.Api.csproj)..." -ForegroundColor Yellow
$buildDuration = "0.00"

if (-not $SkipBuild) {
    if (-not (Test-Path $CsprojPath)) {
        throw "File projek tidak ditemukan: $CsprojPath"
    }

    Write-Host "  -> Menjalankan 'dotnet build `"$CsprojPath`" -c Debug'..." -ForegroundColor DarkYellow
    $buildStopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    
    & dotnet build "$CsprojPath" -c Debug
    $buildExitCode = $LASTEXITCODE
    $buildStopwatch.Stop()
    $buildDuration = $buildStopwatch.Elapsed.TotalSeconds.ToString("F2")
    
    if ($buildExitCode -ne 0) {
        throw "Kompilasi dotnet build gagal dengan exit code $buildExitCode setelah $buildDuration detik!"
    }
    Write-Host "  [OK] Kompilasi berhasil dalam $buildDuration detik (ExitCode: $buildExitCode)." -ForegroundColor Green
} else {
    Write-Host "  [SKIP] Kompilasi dilewati (Switch -SkipBuild aktif)." -ForegroundColor Gray
}

# -------------------------------------------------------------------------
# Step 3: Timestamp Verification
# -------------------------------------------------------------------------
Write-Host "`n[Step 3/5] Memverifikasi Kebaruan Timestamp Binary Output..." -ForegroundColor Yellow

if (-not (Test-Path $DllAppPath)) {
    throw "File assembly binary Bilreg.Application.dll tidak ditemukan di $DllAppPath"
}
if (-not (Test-Path $DllApiPath)) {
    throw "File assembly binary Bilreg.Api.dll tidak ditemukan di $DllApiPath"
}
if (-not (Test-Path $ExePath)) {
    throw "File binary Bilreg.Api.exe tidak ditemukan di $ExePath"
}

$dllAppTime = (Get-Item $DllAppPath).LastWriteTime
$dllApiTime = (Get-Item $DllApiPath).LastWriteTime
$exeTime = (Get-Item $ExePath).LastWriteTime

Write-Host "  -> Bilreg.Application.dll LastWriteTime : $($dllAppTime.ToString('yyyy-MM-dd HH:mm:ss.fff'))" -ForegroundColor DarkYellow
Write-Host "  -> Bilreg.Api.dll         LastWriteTime : $($dllApiTime.ToString('yyyy-MM-dd HH:mm:ss.fff'))" -ForegroundColor DarkYellow
Write-Host "  -> Bilreg.Api.exe         LastWriteTime : $($exeTime.ToString('yyyy-MM-dd HH:mm:ss.fff'))" -ForegroundColor DarkYellow

if (Test-Path $SourceRefPath) {
    $sourceRefTime = (Get-Item $SourceRefPath).LastWriteTime
    Write-Host "  -> Source Ref (OkScheduleOpSetCommand.cs) : $($sourceRefTime.ToString('yyyy-MM-dd HH:mm:ss.fff'))" -ForegroundColor DarkYellow
    
    if ($dllAppTime -lt $sourceRefTime) {
        throw "Verifikasi Timestamp GAGAL: Bilreg.Application.dll ($dllAppTime) lebih tua daripada source code referensi ($sourceRefTime)!"
    }
    if ($dllApiTime -lt $sourceRefTime) {
        throw "Verifikasi Timestamp GAGAL: Bilreg.Api.dll ($dllApiTime) lebih tua daripada source code referensi ($sourceRefTime)!"
    }
    Write-Host "  [OK] Verifikasi Timestamp sukses: Binary DLL lebih mutakhir daripada source code referensi." -ForegroundColor Green
} else {
    Write-Host "  [WARN] Source code referensi tidak ditemukan di $SourceRefPath. Pengecekan perbandingan source code dilewati." -ForegroundColor Yellow
}

# -------------------------------------------------------------------------
# Step 4: Launch Daemon
# -------------------------------------------------------------------------
Write-Host "`n[Step 4/5] Menyalakan Kembali Host API Daemon di Background..." -ForegroundColor Yellow

$processArgs = @("--urls", $Urls)
$startParams = @{
    FilePath = $ExePath
    ArgumentList = $processArgs
    WorkingDirectory = $ProjectDir
    WindowStyle = "Hidden"
    PassThru = $true
}

Write-Host "  -> Memulai proses executable: $ExePath" -ForegroundColor DarkYellow
Write-Host "  -> Argumen: $($processArgs -join ' ')" -ForegroundColor DarkYellow
Write-Host "  -> Working Directory: $ProjectDir" -ForegroundColor DarkYellow

$newProc = Start-Process @startParams

if (-not $newProc -or $newProc.HasExited) {
    throw "Gagal menjalankan daemon Bilreg.Api.exe di background! Proses tidak dapat dimulai atau langsung terhenti."
}

Write-Host "  [OK] Daemon Bilreg.Api berhasil diluncurkan dengan PID: $($newProc.Id) (StartTime: $($newProc.StartTime))." -ForegroundColor Green

# -------------------------------------------------------------------------
# Step 5: Healthcheck
# -------------------------------------------------------------------------
Write-Host "`n[Step 5/5] Memeriksa Ketersediaan Service melalui Healthcheck Polling..." -ForegroundColor Yellow
Write-Host "  -> Polling ke endpoint: $HealthCheckUrl (Batas waktu: $TimeoutSeconds detik)" -ForegroundColor DarkYellow

$healthStopwatch = [System.Diagnostics.Stopwatch]::StartNew()
$isHealthy = $false
$statusCode = 0
$statusDescription = ""
$lastError = ""

while ($healthStopwatch.Elapsed.TotalSeconds -lt $TimeoutSeconds) {
    # Pastikan daemon masih berjalan
    $procCheck = Get-Process -Id $newProc.Id -ErrorAction SilentlyContinue
    if (-not $procCheck -or $procCheck.HasExited) {
        throw "Daemon Bilreg.Api (PID: $($newProc.Id)) terhenti secara prematur selama fase healthcheck!"
    }

    try {
        $response = Invoke-WebRequest -Uri $HealthCheckUrl -UseBasicParsing -TimeoutSec 2 -ErrorAction Stop
        $statusCode = [int]$response.StatusCode
        $statusDescription = $response.StatusDescription
        if ($statusCode -eq 200) {
            $isHealthy = $true
            break
        }
    } catch [System.Net.WebException] {
        if ($_.Exception.Response) {
            $httpResp = [System.Net.HttpWebResponse]$_.Exception.Response
            $statusCode = [int]$httpResp.StatusCode
            $statusDescription = $httpResp.StatusDescription
            if ($statusCode -eq 200 -or $statusCode -eq 401) {
                # 401 Unauthorized mengonfirmasi Kestrel HTTP listener aktif dan merespons rute API
                $isHealthy = $true
                break
            }
        }
        $lastError = $_.Exception.Message
    } catch {
        $lastError = $_.Exception.Message
    }

    Start-Sleep -Milliseconds 500
}
$healthStopwatch.Stop()

if ($isHealthy) {
    Write-Host "  [OK] Healthcheck BERHASIL!" -ForegroundColor Green
    Write-Host "       HTTP Status : $statusCode $statusDescription" -ForegroundColor Green
    Write-Host "       Waktu Respons : $($healthStopwatch.Elapsed.TotalSeconds.ToString('F2')) detik" -ForegroundColor Green
} else {
    # Matikan proses jika healthcheck gagal agar tidak meninggalkan proses orphan
    try { Stop-Process -Id $newProc.Id -Force -ErrorAction SilentlyContinue } catch {}
    throw "Healthcheck GAGAL setelah $TimeoutSeconds detik! Service pada $HealthCheckUrl tidak merespons HTTP 200/OK. Terakhir error: $lastError"
}

# -------------------------------------------------------------------------
# Ringkasan Akhir
# -------------------------------------------------------------------------
Write-Host "`n=================================================================" -ForegroundColor Cyan
Write-Host "  RINGKASAN SIKLUS HIDUP HOST DEV BILREG.API" -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "  PID Lama Dihentikan : $(if ($killedPids.Count -gt 0) { $killedPids -join ', ' } else { 'Tidak ada (proses belum berjalan)' })"
Write-Host "  Waktu Kompilasi     : $(if (-not $SkipBuild) { "$buildDuration detik" } else { 'Dilewati (-SkipBuild)' })"
Write-Host "  PID Baru Aktif      : $($newProc.Id)"
Write-Host "  Waktu Mulai PID     : $($newProc.StartTime)"
Write-Host "  Assembly DLL App    : $($dllAppTime.ToString('yyyy-MM-dd HH:mm:ss.fff'))"
Write-Host "  Assembly DLL Api    : $($dllApiTime.ToString('yyyy-MM-dd HH:mm:ss.fff'))"
Write-Host "  Binding URLs        : $Urls"
Write-Host "  Status Layanan      : ONLINE & SEHAT (HTTP $statusCode $statusDescription)"
Write-Host "=================================================================`n" -ForegroundColor Cyan

if ($Wait) {
    Write-Host "  -> Host daemon aktif (PID $($newProc.Id)). Menunggu sinyal penghentian (-Wait)..." -ForegroundColor Cyan
    Wait-Process -Id $newProc.Id
}

return [PSCustomObject]@{
    KilledPids    = $killedPids
    NewPid        = $newProc.Id
    StartTime     = $newProc.StartTime
    BuildDuration = $buildDuration
    DllAppTime    = $dllAppTime
    DllApiTime    = $dllApiTime
    StatusCode    = $statusCode
    Urls          = $Urls
}
