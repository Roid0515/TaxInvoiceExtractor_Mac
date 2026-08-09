$ErrorActionPreference = 'Stop'
$env:AVALONIA_TELEMETRY_OPTOUT = '1'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$output = Join-Path $root 'artifacts\osx-arm64-publish'

dotnet publish (Join-Path $root 'TaxInvoiceExtractor.Mac.csproj') `
  --configuration Release `
  --runtime osx-arm64 `
  --self-contained true `
  --output $output `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:DebugType=None `
  -p:DebugSymbols=false

Write-Host "Apple Silicon 게시물 생성 완료: $output"
Write-Host '실제 .app/.dmg 생성은 macOS에서 bash build-macos.sh를 실행하세요.'
