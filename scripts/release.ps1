<#
.SYNOPSIS
    Genera el instalador de Breaksy con Velopack y, opcionalmente, lo publica en GitHub Releases.

.EXAMPLE
    ./scripts/release.ps1
    Genera el instalador con la versión definida en src/Breaksy.csproj (solo local).

.EXAMPLE
    ./scripts/release.ps1 -Version 0.2.0 -Upload
    Genera la versión 0.2.0 y la publica en GitHub Releases, donde la verán las instalaciones existentes.
#>
param(
    # Versión SemVer (p. ej. 0.2.0). Por defecto, la de <Version> en src/Breaksy.csproj.
    [string]$Version,
    # Publicar en GitHub Releases. Usa la sesión de GitHub CLI (gh auth login).
    [switch]$Upload
)

$ErrorActionPreference = 'Stop'

$root = Split-Path $PSScriptRoot -Parent
$project = Join-Path $root 'src/Breaksy.csproj'
$publishDir = Join-Path $root 'publish'
$releasesDir = Join-Path $root 'Releases'
$icon = Join-Path $root 'assets/icon/icon.ico'
$repoUrl = 'https://github.com/DavidForero22/Breaksy'

if (-not $Version) {
    $Version = ([xml](Get-Content $project)).Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
}
if (-not $Version) { throw 'No se ha indicado -Version y no hay <Version> en src/Breaksy.csproj.' }

# El token solo hace falta para subir la release; se toma de la sesión de GitHub CLI
# (o de GITHUB_TOKEN si está definida, p. ej. en CI).
if ($Upload -and -not (Get-Command gh -ErrorAction SilentlyContinue)) {
    throw 'Para -Upload hace falta GitHub CLI (gh): https://cli.github.com'
}
$token = $env:GITHUB_TOKEN
if ($Upload -and -not $token) {
    $token = gh auth token
    if ($LASTEXITCODE -or -not $token) { throw 'No hay sesión en GitHub CLI. Ejecuta "gh auth login" primero.' }
}

Push-Location $root
try {
    Write-Host "==> Restaurando herramientas (vpk)" -ForegroundColor Cyan
    dotnet tool restore
    if ($LASTEXITCODE) { throw 'dotnet tool restore falló.' }

    Write-Host "==> Publicando Breaksy $Version (self-contained, win-x64)" -ForegroundColor Cyan
    if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
    dotnet publish $project -c Release -r win-x64 --self-contained true -p:Version=$Version -o $publishDir
    if ($LASTEXITCODE) { throw 'dotnet publish falló.' }

    # Se parte de una carpeta limpia: restos de ejecuciones anteriores harían fallar vpk pack
    if (Test-Path $releasesDir) { Remove-Item $releasesDir -Recurse -Force }

    # Descarga la última versión publicada para poder generar actualizaciones delta (más pequeñas).
    # Si todavía no hay ninguna release, se continúa sin deltas.
    Write-Host "==> Descargando la última release (para deltas)" -ForegroundColor Cyan
    dotnet vpk download github --repoUrl $repoUrl -o $releasesDir
    if ($LASTEXITCODE) { Write-Warning 'No se pudo descargar la release anterior; se generará sin deltas.' }

    Write-Host "==> Empaquetando instalador" -ForegroundColor Cyan
    dotnet vpk pack `
        --packId Breaksy `
        --runtime win-x64 `
        --packVersion $Version `
        --packTitle Breaksy `
        --packDir $publishDir `
        --mainExe Breaksy.exe `
        --icon $icon `
        --outputDir $releasesDir
    if ($LASTEXITCODE) { throw 'vpk pack falló.' }

    # Instalador comprimido, para distinguirlo claramente del .zip portable en la página de la release
    $setupExe = Join-Path $releasesDir 'Breaksy-win-Setup.exe'
    $setupZip = Join-Path $releasesDir 'Breaksy-win-Setup.zip'
    Write-Host "==> Comprimiendo instalador en Breaksy-win-Setup.zip" -ForegroundColor Cyan
    Compress-Archive -Path $setupExe -DestinationPath $setupZip -Force

    if ($Upload) {
        Write-Host "==> Publicando v$Version en GitHub Releases" -ForegroundColor Cyan
        dotnet vpk upload github `
            --repoUrl $repoUrl `
            --token $token `
            --publish `
            --releaseName "Breaksy $Version" `
            --tag "v$Version" `
            --outputDir $releasesDir
        if ($LASTEXITCODE) { throw 'vpk upload falló.' }

        # vpk solo sube sus propios archivos; el .zip del instalador se añade con GitHub CLI
        Write-Host "==> Añadiendo Breaksy-win-Setup.zip a la release" -ForegroundColor Cyan
        $env:GH_TOKEN = $token
        gh release upload "v$Version" $setupZip --repo $repoUrl --clobber
        if ($LASTEXITCODE) { throw 'No se pudo subir Breaksy-win-Setup.zip a la release.' }

        # El .exe suelto duplica al .zip y confunde en la página de la release
        Write-Host "==> Quitando Breaksy-win-Setup.exe suelto de la release" -ForegroundColor Cyan
        gh release delete-asset "v$Version" 'Breaksy-win-Setup.exe' --repo $repoUrl --yes
        if ($LASTEXITCODE) { Write-Warning 'No se pudo quitar Breaksy-win-Setup.exe de la release.' }

        # Notas con los enlaces de descarga, a partir de la plantilla scripts/release-notes.md
        Write-Host "==> Añadiendo notas de descarga a la release" -ForegroundColor Cyan
        $notes = (Get-Content (Join-Path $PSScriptRoot 'release-notes.md') -Raw -Encoding UTF8).Replace('{{VERSION}}', $Version)
        $notesFile = Join-Path $releasesDir 'release-notes.md'
        [IO.File]::WriteAllText($notesFile, $notes, [Text.UTF8Encoding]::new($false))
        gh release edit "v$Version" --repo $repoUrl --notes-file $notesFile
        if ($LASTEXITCODE) { Write-Warning 'No se pudieron añadir las notas a la release.' }
    }

    Write-Host ""
    Write-Host "Listo. Instalador: $setupZip | Portable: $(Join-Path $releasesDir 'Breaksy-win-Portable.zip')" -ForegroundColor Green
}
finally {
    Pop-Location
}
