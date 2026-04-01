$ErrorActionPreference = 'Stop'

$inputTag = ($env:INPUT_TAG ?? '').Trim()
$refName = ($env:REF_NAME ?? '').Trim()
$eventName = $env:EVENT_NAME
$repository = ($env:GITHUB_REPOSITORY ?? '').Trim()
$githubToken = ($env:GITHUB_TOKEN ?? '').Trim()

$tag = if ($eventName -eq 'workflow_dispatch') { $inputTag } else { $refName }
if ([string]::IsNullOrWhiteSpace($tag)) {
    Write-Error 'Release tag is required.'
    exit 1
}

$normalizedTag = if ($tag.StartsWith('refs/tags/')) { $tag.Substring('refs/tags/'.Length) } else { $tag }
if ([string]::IsNullOrWhiteSpace($normalizedTag)) {
    Write-Error "Invalid release tag: '$tag'"
    exit 1
}

$semverLikeTag = '^v\d+\.\d+\.\d+([-.][0-9A-Za-z.-]+)?$'
if ($normalizedTag -notmatch $semverLikeTag) {
    Write-Error "Tag must match vMAJOR.MINOR.PATCH[-suffix], got: '$normalizedTag'"
    exit 1
}

if ($eventName -eq 'workflow_dispatch') {
    if ([string]::IsNullOrWhiteSpace($repository)) {
        Write-Error 'GITHUB_REPOSITORY is required.'
        exit 1
    }

    if ([string]::IsNullOrWhiteSpace($githubToken)) {
        Write-Error 'GITHUB_TOKEN is required.'
        exit 1
    }

    $uri = "https://api.github.com/repos/$repository/git/ref/tags/$normalizedTag"
    $headers = @{
        Authorization = "Bearer $githubToken"
        'User-Agent'  = 'applanch-cd-release-workflow'
        Accept        = 'application/vnd.github+json'
    }

    try {
        Invoke-RestMethod -Method Get -Uri $uri -Headers $headers | Out-Null
    }
    catch {
        Write-Error "Tag '$normalizedTag' was not found in this repository."
        exit 1
    }
}

$version = if ($normalizedTag.StartsWith('v')) { $normalizedTag.Substring(1) } else { $normalizedTag }
$isPre = if ($version.Contains('-')) { 'true' } else { 'false' }

"release_tag=$normalizedTag" | Out-File -FilePath $env:GITHUB_OUTPUT -Encoding utf8 -Append
"app_version=$version" | Out-File -FilePath $env:GITHUB_OUTPUT -Encoding utf8 -Append
"is_prerelease=$isPre" | Out-File -FilePath $env:GITHUB_OUTPUT -Encoding utf8 -Append
