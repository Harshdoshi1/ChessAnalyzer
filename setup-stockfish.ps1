# Script to download Stockfish Windows binary for local development if not already present
$targetDir = Join-Path $PSScriptRoot "ChessAnalyzer.Infrastructure\Chess\Stockfish"
$targetExe = Join-Path $targetDir "stockfish-windows-x86-64-universal.exe"

if (Test-Path $targetExe) {
    Write-Host "Stockfish executable already exists at: $targetExe" -ForegroundColor Green
    exit 0
}

Write-Host "Downloading Stockfish 17 for Windows..." -ForegroundColor Cyan
New-Item -ItemType Directory -Path $targetDir -Force | Out-Null

$zipUrl = "https://github.com/official-stockfish/Stockfish/releases/download/sf_17/stockfish-windows-x86-64-avx2.zip"
$tempZip = Join-Path $env:TEMP "stockfish.zip"
$tempExtract = Join-Path $env:TEMP "stockfish_extracted"

Invoke-WebRequest -Uri $zipUrl -OutFile $tempZip
Expand-Archive -Path $tempZip -DestinationPath $tempExtract -Force

$extractedExe = Get-ChildItem -Path $tempExtract -Filter "*.exe" -Recurse | Select-Object -First 1
if ($extractedExe) {
    Copy-Item $extractedExe.FullName -Destination $targetExe -Force
    Write-Host "Stockfish installed successfully to: $targetExe" -ForegroundColor Green
} else {
    Write-Host "Failed to extract Stockfish." -ForegroundColor Red
}

Remove-Item -Path $tempZip -Force -ErrorAction SilentlyContinue
Remove-Item -Path $tempExtract -Recurse -Force -ErrorAction SilentlyContinue
