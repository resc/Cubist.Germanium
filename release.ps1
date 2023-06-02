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
$releaseVersion = "v$releaseVersion"

git tag $releaseVersion

# push the current branch and the tag
git push origin HEAD
git push origin $releaseVersion

gh release create $releaseVersion --verify-tag --generate-notes