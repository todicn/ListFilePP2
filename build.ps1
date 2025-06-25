#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Build script for ListFile project
.DESCRIPTION
    This script builds the ListFile solution, runs tests, and optionally packages the project.
.PARAMETER Configuration
    The build configuration (Debug or Release). Default is Release.
.PARAMETER SkipTests
    Skip running tests
.PARAMETER Pack
    Create NuGet packages
.EXAMPLE
    .\build.ps1
    .\build.ps1 -Configuration Debug
    .\build.ps1 -SkipTests -Pack
#>

param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [switch]$SkipTests,
    [switch]$Pack
)

$ErrorActionPreference = 'Stop'

# Script variables
$SolutionFile = "ListFilePP2.sln"
$TestProject = "ListFile.Tests/ListFile.Tests.csproj"
$CoreProject = "ListFile.Core/ListFile.Core.csproj"

Write-Host "Building ListFile solution..." -ForegroundColor Green

# Clean previous builds
Write-Host "Cleaning previous builds..." -ForegroundColor Yellow
dotnet clean $SolutionFile --configuration $Configuration --verbosity minimal

# Restore packages
Write-Host "Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore $SolutionFile --verbosity minimal

# Build solution
Write-Host "Building solution..." -ForegroundColor Yellow
dotnet build $SolutionFile --configuration $Configuration --no-restore --verbosity minimal

if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed"
    exit $LASTEXITCODE
}

# Run tests
if (-not $SkipTests) {
    Write-Host "Running tests..." -ForegroundColor Yellow
    dotnet test $TestProject --configuration $Configuration --no-build --verbosity minimal --logger "console;verbosity=detailed"
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Tests failed"
        exit $LASTEXITCODE
    }
}

# Create packages
if ($Pack) {
    Write-Host "Creating NuGet packages..." -ForegroundColor Yellow
    dotnet pack $CoreProject --configuration $Configuration --no-build --output "./artifacts"
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Packaging failed"
        exit $LASTEXITCODE
    }
}

Write-Host "Build completed successfully!" -ForegroundColor Green 