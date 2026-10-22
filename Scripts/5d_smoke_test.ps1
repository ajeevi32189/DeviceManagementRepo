# ════════════════════════════════════════════════════════════════
#  5D Layout API — smoke test
#
#  Chalane se pehle API start kar lijiye (F5 ya `dotnet run`),
#  phir PowerShell me:
#
#      cd C:\Users\jatin\source\repos\ajeevi32189\DeviceManagementRepo\Scripts
#      .\5d_smoke_test.ps1 -Token "<JWT>" -FloorId "<existing floor GUID>"
#
#  Token kahan se: login API se jo accessToken milta hai wahi.
#  FloorId kahan se: GET /api/Floor/get-all-floors
#
#  Ye script kuch bigaadta nahi — ek naya DataCenter aur ek test room
#  banata hai, aur aakhir me layout unlock chhod deta hai.
# ════════════════════════════════════════════════════════════════
param(
    [Parameter(Mandatory=$true)][string]$Token,
    [Parameter(Mandatory=$true)][string]$FloorId,
    [string]$BaseUrl = "http://localhost:5300"
)

$ErrorActionPreference = "Stop"
$H = @{ Authorization = "Bearer $Token"; "Content-Type" = "application/json" }
$session = [guid]::NewGuid().ToString()
$pass = 0; $fail = 0

function Step($n, $msg, $block) {
    Write-Host "`n[$n] $msg" -ForegroundColor Cyan
    try { & $block; $script:pass++ ; Write-Host "    PASS" -ForegroundColor Green }
    catch { $script:fail++ ; Write-Host "    FAIL -> $($_.Exception.Message)" -ForegroundColor Red }
}

function Call($method, $path, $body) {
    $uri = "$BaseUrl$path"
    if ($body) { return Invoke-RestMethod -Method $method -Uri $uri -Headers $H -Body ($body | ConvertTo-Json -Depth 12) }
    else       { return Invoke-RestMethod -Method $method -Uri $uri -Headers $H }
}

Write-Host "5D smoke test  |  $BaseUrl  |  floor $FloorId" -ForegroundColor Yellow
Write-Host "edit session: $session"

# ── 1. Scene padho ────────────────────────────────────────────
$script:scene = $null
Step 1 "GET scene (khali bhi chalega)" {
    $script:scene = Call GET "/api/Layout/floor/$FloorId"
    Write-Host "    rooms=$($scene.counts.rooms) racks=$($scene.counts.racks) floorDevices=$($scene.counts.floorDevices) cables=$($scene.counts.cables)"
    Write-Host "    deviceTypes=$($scene.deviceTypes.Count)  mode=$($scene.lock.mode)  revision=$($scene.lock.revision)"
    $withShape = ($scene.deviceTypes | Where-Object { $_.shape }).Count
    Write-Host "    shape bhare hue device types: $withShape / $($scene.deviceTypes.Count)"
    if ($withShape -eq 0) { Write-Host "    (!) koi shape nahi mila - pehle 5d_seed_device_shapes.sql chalaiye" -ForegroundColor Yellow }
}

# ── 2. Bina lease ke save -> mana hona chahiye ────────────────
Step 2 "Bina edit mode ke save (409/423 aana CHAHIYE)" {
    try {
        Call POST "/api/Layout/floor/$FloorId/save" @{ editSessionId = $session; rooms = @() } | Out-Null
        throw "Save ho gaya - ye nahi hona chahiye tha!"
    } catch {
        $code = $_.Exception.Response.StatusCode.value__
        if ($code -eq 409 -or $code -eq 423) { Write-Host "    sahi: HTTP $code" }
        else { throw "Galat code: $code" }
    }
}

# ── 3. Edit mode on ───────────────────────────────────────────
Step 3 "acquire-edit" {
    $r = Call POST "/api/Layout/floor/$FloorId/acquire-edit" @{ editSessionId = $session; leaseMinutes = 10 }
    if ($r.mode -ne "edit") { throw "mode = $($r.mode), 'edit' expected" }
    Write-Host "    mode=$($r.mode) editingBy=$($r.editingBy) tak=$($r.editingExpiresAt)"
}

# ── 4. Ek room save ───────────────────────────────────────────
$roomId = [guid]::NewGuid().ToString()
Step 4 "Ek test room save" {
    $body = @{
        editSessionId = $session
        rooms = @(@{
            id = $roomId; name = "SMOKE TEST ROOM"; roomType = "server"
            positionX = 0; positionZ = 0; width = 10; length = 8; height = 4
            colorHex = "#38bdf8"; powerBudgetKW = 180; dimensionUnit = "m"
            rotationAngle = 0; sortOrder = 99; isActive = $true
        })
    }
    $r = Call POST "/api/Layout/floor/$FloorId/save" $body
    Write-Host "    revision=$($r.revision) created.rooms=$($r.created.rooms) updated.rooms=$($r.updated.rooms) ms=$($r.elapsedMs)"
    if (-not $r.success) { throw "success=false" }
}

# ── 5. Wapas padho ────────────────────────────────────────────
Step 5 "Scene dobara padho - room wapas aana chahiye" {
    $s2 = Call GET "/api/Layout/floor/$FloorId"
    $r = $s2.rooms | Where-Object { $_.id -eq $roomId }
    if (-not $r) { throw "Room wapas nahi aaya" }
    Write-Host "    mila: $($r.name) type=$($r.roomType) ${($r.width)}x${($r.length)}m budget=$($r.powerBudgetKW)kW"
}

# ── 6. Stale revision -> 409 ──────────────────────────────────
Step 6 "Purane revision se save (409 aana CHAHIYE)" {
    try {
        Call POST "/api/Layout/floor/$FloorId/save" @{ editSessionId = $session; expectedRevision = 0; rooms = @() } | Out-Null
        throw "Save ho gaya - stale revision pakda nahi gaya!"
    } catch {
        $code = $_.Exception.Response.StatusCode.value__
        if ($code -eq 409) { Write-Host "    sahi: HTTP 409" } else { throw "Galat code: $code" }
    }
}

# ── 7. Lock lagao ─────────────────────────────────────────────
Step 7 "Layout lock" {
    $r = Call POST "/api/Layout/floor/$FloorId/lock" @{ isLocked = $true; lockNote = "smoke test" }
    if (-not $r.isLocked) { throw "isLocked=false" }
    Write-Host "    mode=$($r.mode) lockedBy=$($r.lockedBy)"
}

# ── 8. Lock ke baad save -> 423 ───────────────────────────────
Step 8 "Lock ke baad save (423 aana CHAHIYE)" {
    try {
        Call POST "/api/Layout/floor/$FloorId/save" @{ editSessionId = $session; rooms = @() } | Out-Null
        throw "Locked layout me save ho gaya!"
    } catch {
        $code = $_.Exception.Response.StatusCode.value__
        if ($code -eq 423) { Write-Host "    sahi: HTTP 423 Locked" } else { throw "Galat code: $code" }
    }
}

# ── 9. Saaf karo ──────────────────────────────────────────────
Step 9 "Unlock + release (cleanup)" {
    Call POST "/api/Layout/floor/$FloorId/lock" @{ isLocked = $false } | Out-Null
    Call POST "/api/Layout/floor/$FloorId/release-edit" @{ editSessionId = $session } | Out-Null
    Write-Host "    unlock + release done"
}

Write-Host "`n═══════════════════════════════════" -ForegroundColor Yellow
Write-Host "  PASS: $pass    FAIL: $fail" -ForegroundColor $(if ($fail -eq 0) { "Green" } else { "Red" })
Write-Host "═══════════════════════════════════" -ForegroundColor Yellow
if ($fail -eq 0) {
    Write-Host "`nSab theek. Test room abhi bhi DB me hai -" -ForegroundColor Green
    Write-Host "hatana ho to: $roomId  ko DeletedRoomIds me bhej dijiye." -ForegroundColor Green
}
