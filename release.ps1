# check if there are uncomitted changes
if(0 -ne (git diff --quiet --exit-code)) { 
    Write-Error "There are uncomitted changes. Aborting release."
    exit 1 
}

# check if there are staged changes
if(0 -ne  (git diff --quiet --cached --exit-code)) { 
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
$releaseVersion = '' 
while ( -not ($releaseVersion -match '^\d+\.\d+\.\d+$')) { 
    $releaseVersion = Read-Host -Prompt 'Enter release version (e.g. 1.0.0): '
}
$releaseVersion = "v$releaseVersion"


# git tag $releaseVersion
# git push origin $releaseVersion
