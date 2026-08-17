param([string]$Configuration = "Release")
$ErrorActionPreference = "Stop"
dotnet publish "$PSScriptRoot\src\HardwareBench.App" -c $Configuration -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true -o "$PSScriptRoot\publish"
Get-Item "$PSScriptRoot\publish\HardwareBench.exe" | Select-Object FullName, @{n="SizeMB";e={[math]::Round($_.Length/1MB,1)}}
Get-FileHash "$PSScriptRoot\publish\HardwareBench.exe" -Algorithm SHA256 | Format-List