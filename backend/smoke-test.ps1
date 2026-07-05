$base = "http://localhost:5102"
$ErrorActionPreference = "Stop"

function Show($title, $obj) {
    Write-Host ""
    Write-Host "== $title ==" -ForegroundColor Cyan
    $obj | ConvertTo-Json -Depth 5
}

function Err($step, $ex) {
    Write-Host ""
    Write-Host "== $step FAILED ==" -ForegroundColor Red
    Write-Host $ex.Exception.Message -ForegroundColor Red
    if ($ex.ErrorDetails) { Write-Host $ex.ErrorDetails.Message -ForegroundColor Red }
}

$email = "smoke_" + [Guid]::NewGuid().ToString("N").Substring(0, 8) + "@example.com"
$pw = "SuperSecret123"

# 1) Register
try {
    $u = Invoke-RestMethod -Uri "$base/api/auth/register" -Method Post -ContentType "application/json" `
        -Body (@{ name = "Smoke Test"; email = $email; password = $pw } | ConvertTo-Json)
    Show "REGISTER OK" $u
} catch { Err "REGISTER" $_; exit 1 }

# 2) Login
try {
    $login = Invoke-RestMethod -Uri "$base/api/auth/login" -Method Post -ContentType "application/json" `
        -Body (@{ email = $email; password = $pw } | ConvertTo-Json)
    Show "LOGIN OK" $login
} catch { Err "LOGIN" $_ }

# 3) Login with wrong password (should be 400 "Invalid credentials")
try {
    Invoke-RestMethod -Uri "$base/api/auth/login" -Method Post -ContentType "application/json" `
        -Body (@{ email = $email; password = "wrong" } | ConvertTo-Json) | Out-Null
    Write-Host ""
    Write-Host "== WRONG PW: unexpected 200 ==" -ForegroundColor Red
} catch {
    Write-Host ""
    Write-Host "== WRONG PW correctly rejected ==" -ForegroundColor Green
    if ($_.ErrorDetails) { $_.ErrorDetails.Message }
}

# 4) Register duplicate (should 409)
try {
    Invoke-RestMethod -Uri "$base/api/auth/register" -Method Post -ContentType "application/json" `
        -Body (@{ name = "Smoke Test"; email = $email; password = $pw } | ConvertTo-Json) | Out-Null
    Write-Host ""
    Write-Host "== DUPE REG: unexpected 200 ==" -ForegroundColor Red
} catch {
    Write-Host ""
    Write-Host "== DUPE REG correctly rejected ==" -ForegroundColor Green
    if ($_.ErrorDetails) { $_.ErrorDetails.Message }
}

# 5) Create tour
$tour = $null
try {
    $tour = Invoke-RestMethod -Uri "$base/api/tours" -Method Post -ContentType "application/json" `
        -Body (@{
            name          = "Refactor Smoke Tour"
            description   = "After DTO move"
            from          = "Vienna"
            to            = "Salzburg"
            transportType = "driving"
            distance      = 299.6
            duration      = 198
            color         = "from-cyan-500 to-blue-500"
            imageUrl      = "https://picsum.photos/seed/refactor/800/400"
        } | ConvertTo-Json)
    Show "CREATE TOUR OK" $tour
} catch { Err "CREATE TOUR" $_; exit 1 }

# 6) Create tour with invalid transportType (mapper → ArgumentException → middleware → 400)
try {
    Invoke-RestMethod -Uri "$base/api/tours" -Method Post -ContentType "application/json" `
        -Body (@{ name = "BadEnumTour"; from = "A"; to = "B"; transportType = "teleport"; distance = 1; duration = 1 } | ConvertTo-Json) | Out-Null
    Write-Host ""
    Write-Host "== INVALID ENUM: unexpected 200 ==" -ForegroundColor Red
} catch {
    Write-Host ""
    Write-Host "== INVALID ENUM correctly rejected ==" -ForegroundColor Green
    if ($_.ErrorDetails) { $_.ErrorDetails.Message }
}

# 7) Update tour
try {
    $body = @{
        name          = $tour.name + " (updated)"
        description   = $tour.description
        from          = $tour.from
        to            = $tour.to
        transportType = $tour.transportType
        distance      = $tour.distance
        duration      = $tour.duration
        status        = "completed"
        color         = $tour.color
        imageUrl      = $tour.imageUrl
    } | ConvertTo-Json
    $updated = Invoke-RestMethod -Uri "$base/api/tours/$($tour.id)" -Method Put -ContentType "application/json" -Body $body
    Show "UPDATE TOUR OK" $updated
} catch { Err "UPDATE TOUR" $_ }

# 8) Create tour log
$log = $null
try {
    $log = Invoke-RestMethod -Uri "$base/api/logs" -Method Post -ContentType "application/json" `
        -Body (@{
            tourId        = $tour.id
            loggedAt      = (Get-Date).ToUniversalTime().ToString("o")
            comment       = "Great!"
            difficulty    = "medium"
            totalDistance = 300
            duration      = 205
            rating        = 4
        } | ConvertTo-Json)
    Show "CREATE LOG OK (tourName should be populated via Include(Tour))" $log
} catch { Err "CREATE LOG" $_; exit 1 }

# 9) List logs for tour (should include tourName from Include(l => l.Tour))
try {
    $forTour = Invoke-RestMethod -Uri "$base/api/logs?tourId=$($tour.id)" -Method Get
    Show "LOGS FOR TOUR" $forTour
} catch { Err "LIST LOGS FOR TOUR" $_ }

# 10) Delete log
try {
    Invoke-RestMethod -Uri "$base/api/logs/$($log.id)" -Method Delete | Out-Null
    Write-Host ""
    Write-Host "== DELETE LOG OK ==" -ForegroundColor Green
} catch { Err "DELETE LOG" $_ }

# 11) Delete tour
try {
    Invoke-RestMethod -Uri "$base/api/tours/$($tour.id)" -Method Delete | Out-Null
    Write-Host "== DELETE TOUR OK ==" -ForegroundColor Green
} catch { Err "DELETE TOUR" $_ }

# 12) Get deleted tour → 404
try {
    Invoke-RestMethod -Uri "$base/api/tours/$($tour.id)" -Method Get | Out-Null
    Write-Host "== GET DELETED: unexpected 200 ==" -ForegroundColor Red
} catch {
    Write-Host "== 404 on deleted tour: OK ==" -ForegroundColor Green
    if ($_.ErrorDetails) { $_.ErrorDetails.Message }
}

Write-Host ""
Write-Host "===== SMOKE TEST FINISHED =====" -ForegroundColor Yellow
