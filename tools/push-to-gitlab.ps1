<#
.SYNOPSIS
    Отправляет текущее состояние проекта в GitLab одним коммитом.

.DESCRIPTION
    В GitLab история разработки не переносится: там по одному коммиту на выпуск.
    Скрипт берёт дерево текущего HEAD как есть, делает из него коммит, родителем
    которого назначен последний коммит в GitLab, и отправляет его. Сколько бы
    коммитов ни накопилось в GitHub с прошлого раза, в GitLab появится один.

    Рабочие файлы при этом не трогаются: ни копирования, ни очистки папок.

.PARAMETER Message
    Сообщение коммита. Многострочное записывается в переменную заранее.

.PARAMETER Tag
    Необязательная метка, например v1.1. Ставится на новый коммит и отправляется.

.EXAMPLE
    .\tools\push-to-gitlab.ps1 -Message "Версия от 12.11.2026" -Tag v1.1

.NOTES
    Разово, перед первым запуском:
        git remote add gitlab http://192.168.0.9/<группа>/<проект>.git
#>
param(
    [Parameter(Mandatory = $true)][string] $Message,
    [string] $Tag,
    [string] $Remote = 'gitlab',
    [string] $Branch = 'main'
)

$ErrorActionPreference = 'Stop'

# Собственный вызов git: сам по себе он об ошибке не сообщит, только кодом возврата.
function Invoke-Git {
    $result = & git @args
    if ($LASTEXITCODE -ne 0) {
        throw "git $($args -join ' ') завершился с кодом $LASTEXITCODE"
    }
    return $result
}

if (Invoke-Git status --porcelain) {
    throw 'В рабочей копии есть незафиксированные изменения. Отправлять в GitLab нужно то, что зафиксировано.'
}

Write-Host "Запрашиваю состояние $Remote/$Branch…"
Invoke-Git fetch $Remote $Branch | Out-Null

$tree   = Invoke-Git rev-parse 'HEAD^{tree}'
$parent = Invoke-Git rev-parse "$Remote/$Branch"

# Сообщение передаётся файлом в UTF-8 без BOM: иначе кириллица в заголовке
# коммита приедет искажённой.
$messageFile = [IO.Path]::GetTempFileName()
try {
    [IO.File]::WriteAllText($messageFile, $Message, (New-Object Text.UTF8Encoding $false))
    $commit = Invoke-Git commit-tree $tree -p $parent -F $messageFile
}
finally {
    Remove-Item $messageFile -ErrorAction SilentlyContinue
}

Invoke-Git push $Remote "${commit}:refs/heads/$Branch" | Out-Null

if ($Tag) {
    Invoke-Git tag -a $Tag $commit -m $Message | Out-Null
    Invoke-Git push $Remote $Tag | Out-Null
}

$short = $commit.Substring(0, 8)
Write-Host "Готово: в $Remote/$Branch отправлен коммит $short$(if ($Tag) { ", метка $Tag" })."
Write-Host "Содержимое совпадает с текущим HEAD: $(Invoke-Git rev-parse --short HEAD)."
