$resourcesDir = Join-Path -Path $PSScriptRoot -ChildPath "Dependencies"

New-Item -ItemType Directory -Force -Path $resourcesDir

Get-ChildItem -Path $resourcesDir | Remove-Item -Force -Recurse

$tmpDir = New-Item -ItemType Directory -Force -Path "$env:TEMP\Download-Dependencies"

$depotDir = Join-Path -Path $tmpDir -ChildPath "DepotDownloader"
$rustDir = Join-Path -Path $depotDir -ChildPath "RustDLLs"

New-Item -ItemType Directory -Force -Path $depotDir
New-Item -ItemType Directory -Force -Path $rustDir

Invoke-WebRequest -Uri "https://github.com/SteamRE/DepotDownloader/releases/latest/download/DepotDownloader-windows-x64.zip" -OutFile "$depotDir\DepotDownloader.zip"
Expand-Archive -Path "$depotDir\DepotDownloader.zip" -DestinationPath $depotDir -Force
Remove-Item -Path "$depotDir\DepotDownloader.zip"

$fileListPath = Join-Path -Path $rustDir -ChildPath "filelist.txt"
@("regex:RustDedicated_Data/Managed/.*\.dll") | Set-Content -Path $fileListPath

$depotArgs = "-app 258550 -depot 258551 -filelist $fileListPath -dir $rustDir"
Start-Process -FilePath "$depotDir\DepotDownloader.exe" -ArgumentList $depotArgs -NoNewWindow -Wait

Move-Item -Path "$rustDir\RustDedicated_Data\Managed\*.dll" -Destination $resourcesDir -Force

$oxideDir = Join-Path -Path $tmpDir -ChildPath "Oxide"
New-Item -ItemType Directory -Force -Path $oxideDir
Invoke-WebRequest -Uri "https://github.com/OxideMod/Oxide.Rust/releases/latest/download/Oxide.Rust.zip" -OutFile "$oxideDir\Oxide.Rust.zip"
Expand-Archive -Path "$oxideDir\Oxide.Rust.zip" -DestinationPath $oxideDir -Force
Remove-Item -Path "$oxideDir\Oxide.Rust.zip"

Move-Item -Path "$oxideDir\RustDedicated_Data\Managed\*.dll" -Destination $resourcesDir -Force

Remove-Item -Path $tmpDir -Force -Recurse

Write-Host "Dependencies downloaded and copied to $PSScriptRoot."