[CmdletBinding()]
param([switch]$Check)

$ErrorActionPreference = 'Stop'
$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$utf8 = [System.Text.UTF8Encoding]::new($false)
$rules = @(
    @{ Source = '.codex/arquitetura-frontend.mdc'; Target = '.claude/rules/arquitetura-frontend.md'; Paths = @('hortacomunitaria-webapp/**/*', '.codex/arquitetura-frontend.mdc', 'documentos/planejamento/frontend/**/*') },
    @{ Source = '.codex/arquitetura-backend.md'; Target = '.claude/rules/arquitetura-backend.md'; Paths = @('HortaComunitaria.Api/**/*', 'tests/HortaComunitaria.Api.IntegrationTests/**/*', '.codex/arquitetura-backend.md') }
)
$outdated = @()
foreach ($rule in $rules) {
    $sourcePath = Join-Path $projectRoot $rule.Source
    $targetPath = Join-Path $projectRoot $rule.Target
    $body = [System.IO.File]::ReadAllText($sourcePath).Replace("`r`n", "`n")
    # Trocar somente o frontmatter MDC e os caminhos relativos de documentação.
    $body = [regex]::Replace($body, '\A---\n.*?\n---\n', '', [System.Text.RegularExpressions.RegexOptions]::Singleline)
    $body = $body.Replace('(../documentos/', '(../../documentos/')
    $frontmatter = "---`npaths:`n" + (($rule.Paths | ForEach-Object { '  - "' + $_ + '"' }) -join "`n") + "`n---`n`n"
    $notice = '<!-- Gerado de ' + $rule.Source + '. Não editar este espelho; executar .claude/scripts/sync-architecture.ps1. -->' + "`n`n"
    $expected = $frontmatter + $notice + $body.Trim() + "`n"
    if ($Check) {
        if (!(Test-Path -LiteralPath $targetPath) -or [System.IO.File]::ReadAllText($targetPath).Replace("`r`n", "`n") -cne $expected) {
            $outdated += $rule.Target
        }
    } else {
        [System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($targetPath)) | Out-Null
        [System.IO.File]::WriteAllText($targetPath, $expected, $utf8)
        Write-Output "Sincronizado: $($rule.Target)"
    }
}
if ($outdated.Count) { throw ('Espelhos desatualizados: ' + ($outdated -join ', ')) }
if ($Check) { Write-Output 'Regras de arquitetura sincronizadas com .codex.' }
