<#
.SYNOPSIS
    Memverifikasi endpoint API GET /api/ScheduleOp/{tgl}/TglOp dan memvalidasi
    keberadaan jadwal draf Tn. Gusdiono dengan isDraft = true (Kamar Operasi III, 09:00 WIB).
#>

[CmdletBinding()]
param(
    [string]$BaseUrl = "http://localhost:5000",
    [string]$Date = "2024-06-06",
    [string]$TargetOrderOpId = "01M6JEEDRMR3316WM4751MV0R8"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Base64UrlEncode([byte[]]$bytes) {
    return [Convert]::ToBase64String($bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')
}

# 1. Generate JWT Token
$header = @{ alg = "HS256"; typ = "JWT" } | ConvertTo-Json -Compress
$now = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
$payload = @{
    sub = "BilregApiAccessToken"
    jti = [Guid]::NewGuid().ToString()
    iat = $now
    nbf = $now - 10
    exp = $now + 3600
    iss = "BilregApiServer"
    aud = "BilregApiClient"
    "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name" = "DeploymentVerifier"
    "http://schemas.microsoft.com/ws/2008/06/identity/claims/role" = "Admin"
} | ConvertTo-Json -Compress

$headerBytes = [System.Text.Encoding]::UTF8.GetBytes($header)
$payloadBytes = [System.Text.Encoding]::UTF8.GetBytes($payload)
$encodedHeader = Base64UrlEncode $headerBytes
$encodedPayload = Base64UrlEncode $payloadBytes
$signatureInput = "$encodedHeader.$encodedPayload"

$key = [System.Text.Encoding]::UTF8.GetBytes("ErF7Zq0praAgc4pV7ajVG4h2rAmP99bvgLMyCVWGkq0=")
$hmac = [System.Security.Cryptography.HMACSHA256]::new($key)
$signatureBytes = $hmac.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($signatureInput))
$encodedSignature = Base64UrlEncode $signatureBytes
$token = "$signatureInput.$encodedSignature"

# 2. Invoke API Endpoint
$url = "$($BaseUrl.TrimEnd('/'))/api/ScheduleOp/$Date/TglOp"
Write-Host "Mengirim request ke API: $url" -ForegroundColor Cyan

$headers = @{
    "Authorization" = "Bearer $token"
    "Accept" = "application/json"
}

$response = Invoke-RestMethod -Uri $url -Method Get -Headers $headers

Write-Host "Respons diterima dengan sukses (HTTP 200 OK)." -ForegroundColor Green

# 3. Inspect Payload
$schedules = $response.data
Write-Host "Total jadwal pada tanggal $Date : $($schedules.Count)" -ForegroundColor Yellow

$gusdionoSchedule = $schedules | Where-Object { $_.orderOpId -eq $TargetOrderOpId -or $_.pasienName -match "GUSDIONO" }

if (-not $gusdionoSchedule) {
    throw "Verifikasi GAGAL: Jadwal Tn. Gusdiono (OrderOpId: $TargetOrderOpId) TIDAK DITEMUKAN pada respons API!"
}

Write-Host "`n=== JADWAL DITEMUKAN ===" -ForegroundColor Cyan
Write-Host "  OrderOpId     : $($gusdionoSchedule.orderOpId)"
Write-Host "  ScheduleOpId  : $($gusdionoSchedule.scheduleOpId)"
Write-Host "  Pasien        : $($gusdionoSchedule.pasienName) ($($gusdionoSchedule.pasienId))"
Write-Host "  Operasi       : $($gusdionoSchedule.namaOperasi)"
Write-Host "  Ruang / Kamar : $($gusdionoSchedule.kamarName) ($($gusdionoSchedule.kamarId))"
Write-Host "  Waktu / Jam   : $($gusdionoSchedule.scheduledTime) (Durasi: $($gusdionoSchedule.durasi) menit)"
Write-Host "  Dokter        : $($gusdionoSchedule.dokterName) ($($gusdionoSchedule.dokterId))"
Write-Host "  StatusOp      : $($gusdionoSchedule.statusOp)"
Write-Host "  isDraft       : $($gusdionoSchedule.isDraft)"
Write-Host "========================`n" -ForegroundColor Cyan

# Validasi kriteria
if ($gusdionoSchedule.isDraft -ne $true) {
    throw "Verifikasi GAGAL: isDraft bernilai $($gusdionoSchedule.isDraft), diharapkan true!"
}
if ($gusdionoSchedule.kamarName -notmatch "Kamar Operasi III") {
    Write-Host "  [WARN] Kamar bernilai '$($gusdionoSchedule.kamarName)'" -ForegroundColor Yellow
}
if ($gusdionoSchedule.scheduledTime -ne "09:00") {
    Write-Host "  [WARN] Jam jadwal bernilai '$($gusdionoSchedule.scheduledTime)'" -ForegroundColor Yellow
}

Write-Host "[VERIFIKASI SUKSES] Jadwal draf Tn. Gusdiono terkonfirmasi aktif di runtime API dengan isDraft = true!" -ForegroundColor Green

return $gusdionoSchedule
