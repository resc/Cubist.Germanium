$releaseBranch = 'master'

$currentBranch = git branch --show-current

# check if the current branch is the release branch
if ($currentBranch -ne $releaseBranch) { 
    Write-Error "You are not on the release branch '$releaseBranch'. Aborting release."
    exit 1 
}

# check if there are uncomitted changes
git diff --quiet --exit-code
if($LASTEXITCODE -ne  0) { 
    Write-Error "There are uncomitted changes. Aborting release."
    exit 1 
}

# check if there are staged changes
git diff --cached --quiet --exit-code
if($LASTEXITCODE -ne  0) { 
    Write-Error "There are staged changes. Aborting release."
    exit 1 
}

# check if unit tests pass
dotnet test ./src/src.sln
if ($LASTEXITCODE -ne 0) { 
    Write-Host "Tests failed. Aborting release."
    exit $LASTEXITCODE 
}

# get the release version from the user
Write-Host "Latest releases:"
gh release list -L 5

$releaseVersion = '' 
while ( -not ($releaseVersion -match '^\d+\.\d+\.\d+$')) { 
    $releaseVersion = Read-Host -Prompt 'Enter release version (e.g. 1.0.0) '
}
$releaseTag = "v$releaseVersion"

git tag $releaseTag

# push the current branch and the tag
git push origin $releaseBranch
git push origin $releaseTag

gh release create $releaseTag --verify-tag --generate-notes