#Requires -Version 7.0
<#
  .SYNOPSIS
    Encerra processos de build travados e remove bin/obj de todos os projetos em course/.
  .DESCRIPTION
    Mata apenas dotnet e VBCSCompiler (holders típicos de lock de build) e apaga
    bin/obj de cada diretório que contém um *.csproj sob course/ (inclui tests/).
    Todos os caminhos derivam de $PSScriptRoot — nenhum path de máquina hardcoded.
    NÃO mata Docker/node (derrubaria banco de dev/teste e outros apps).
  .EXAMPLE
    pwsh -NoProfile -ExecutionPolicy Bypass -File "automation/clean_api.ps1"
  .EXAMPLE
    pwsh -NoProfile -ExecutionPolicy Bypass -File "automation/clean_api.ps1" -WhatIf
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param()

$ErrorActionPreference = 'Stop'

$raiz = Split-Path -Parent $PSScriptRoot
$srcDir = Join-Path $raiz 'src'
if (-not (Test-Path -LiteralPath $srcDir)) {
    Write-Output "clean: src/ não encontrado em: $raiz"
    exit 1
}

Write-Host "`n[1/2] Encerrando processos..." -ForegroundColor Yellow
foreach ($procName in @('dotnet', 'VBCSCompiler', 'com.docker.backend', 'Docker Desktop', 'DockerDesktop', 'com.docker.build')) {
    $processes = Get-Process -Name $procName -ErrorAction SilentlyContinue
    if ($processes) {
        Write-Host "  -> Parando processo: $procName (Count: $($processes.Count))" -ForegroundColor Red
        foreach ($p in $processes) {
            Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue
        }
    } else {
        Write-Host "  -> Processo finalizado: $procName" -ForegroundColor Gray
    }
}
Start-Sleep -Seconds 2

Write-Host "`n[2/2] Navegando pelos projetos em course para remover bin e obj..." -ForegroundColor Yellow
$projetos = @(Get-ChildItem -LiteralPath $srcDir -Recurse -Filter '*.csproj' -File |
    Select-Object -ExpandProperty DirectoryName -Unique)
$limpos = 0
foreach ($dir in $projetos) {
    foreach ($alvo in @('bin', 'obj')) {
        $caminho = Join-Path $dir $alvo
        if (Test-Path -LiteralPath $caminho) {
            Write-Host "    -> Deletando pasta: $alvo ($caminho)" -ForegroundColor Red
            if ($PSCmdlet.ShouldProcess($caminho, 'Remove-Item -Recurse')) {
                Remove-Item -LiteralPath $caminho -Recurse -Force
                $limpos++
            }
        } else {
            Write-Host "    -> Pasta $alvo ($caminho) ausente ou já removida." -ForegroundColor Gray
        }
    }
}
Write-Output ("clean: OK — {0} projeto(s), {1} pasta(s) bin/obj removidas." -f $projetos.Count, $limpos)
