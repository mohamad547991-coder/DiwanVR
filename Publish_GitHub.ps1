param([string]$Owner='mohamad547991-coder',[string]$Repository='DiwanVR')
$ErrorActionPreference='Stop'
function Invoke-Probe {
    param([string]$Command,[string[]]$Arguments)
    # Windows PowerShell 5.1 turns native stderr into ErrorRecords.
    # Expected failures (no origin/repository) must reach the exit-code check.
    $previousPreference=$ErrorActionPreference
    try {
        $ErrorActionPreference='Continue'
        $output=@(& $Command @Arguments 2>$null)
        $code=$LASTEXITCODE
    } finally {$ErrorActionPreference=$previousPreference}
    return [pscustomobject]@{Code=$code;Text=($output -join "`n").Trim()}
}
Push-Location -LiteralPath $PSScriptRoot
try {
    $identity=Invoke-Probe 'gh' @('api','user','--jq','.login')
    if($identity.Code -ne 0){throw 'GitHub connection or sign-in failed. Run: gh auth login --hostname github.com --web --git-protocol https'}
    if($identity.Text -ne $Owner){throw "Signed in as $($identity.Text); expected $Owner. Switch to the intended account first."}
    & git rev-parse --verify HEAD | Out-Null
    if($LASTEXITCODE -ne 0){throw 'No prepared local commit exists.'}
    $branch=(& git branch --show-current).Trim()
    if($branch -ne 'main'){throw "Expected branch main, found $branch. Review before publishing."}
    $slug="$Owner/$Repository"
    $existing=Invoke-Probe 'gh' @('repo','view',$slug,'--json','nameWithOwner')
    if($existing.Code -ne 0){
        & gh repo create $slug --private --description 'Unity Quest office workflow training simulator'
        if($LASTEXITCODE -ne 0){throw 'Could not create or access the GitHub repository.'}
    }
    $url="https://github.com/$slug.git"
    $remote=Invoke-Probe 'git' @('remote','get-url','origin')
    if($remote.Code -eq 0){
        if($remote.Text -ne $url){throw 'Existing origin points elsewhere. Review it before publishing.'}
    } else {
        & git remote add origin $url
        if($LASTEXITCODE -ne 0){throw 'Could not add origin.'}
    }
    & git -c credential.helper= -c 'credential.helper=!gh auth git-credential' push --set-upstream origin main
    if($LASTEXITCODE -ne 0){throw 'Push failed. Existing remote history has not been overwritten.'}
    $local=(& git rev-parse HEAD).Trim()
    $remoteHead=(& gh api "repos/$slug/commits/main" --jq '.sha')
    if($LASTEXITCODE -ne 0 -or $remoteHead.Trim() -ne $local){throw 'Could not verify the uploaded commit.'}
    Write-Host "Uploaded and verified: https://github.com/$slug"
} finally {Pop-Location}
