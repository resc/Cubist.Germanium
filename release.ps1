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
Write-Host "Current versions:"
git tag --list "v*" --sort=-version:refname --column

$releaseVersion = '' 
while ( -not ($releaseVersion -match '^\d+\.\d+\.\d+$')) { 
    $releaseVersion = Read-Host -Prompt 'Enter release version (e.g. 1.0.0) '
}
$releaseVersion = "v$releaseVersion"


git tag $releaseVersion
git push origin $releaseVersion

gh release create $releaseVersion --verify-tag -t $releaseVersion