[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$Query,

    [string]$AssetsPath,

    [string]$Root = (Get-Location).Path,

    [switch]$Json
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-JsonPropertyValue {
    param(
        [Parameter(Mandatory = $true)]
        [object]$Object,

        [Parameter(Mandatory = $true)]
        [string]$Name
    )

    $property = $Object.PSObject.Properties |
        Where-Object { $_.Name -eq $Name } |
        Select-Object -First 1

    if ($null -eq $property) {
        return $null
    }

    return $property.Value
}

function Test-ContainsLiteral {
    param(
        [string]$Value,

        [Parameter(Mandatory = $true)]
        [string]$Needle
    )

    if ([string]::IsNullOrEmpty($Value)) {
        return $false
    }

    return $Value.IndexOf($Needle, [System.StringComparison]::OrdinalIgnoreCase) -ge 0
}

function Resolve-AssetsFiles {
    param(
        [string]$ExplicitAssetsPath,

        [Parameter(Mandatory = $true)]
        [string]$SearchRoot
    )

    if (-not [string]::IsNullOrWhiteSpace($ExplicitAssetsPath)) {
        $resolvedPath = Resolve-Path -LiteralPath $ExplicitAssetsPath -ErrorAction Stop
        return @($resolvedPath.Path)
    }

    $candidateFiles = Get-ChildItem -LiteralPath $SearchRoot -Recurse -Filter project.assets.json -File |
        Sort-Object FullName

    return @($candidateFiles | ForEach-Object { $_.FullName })
}

$assetsFiles = @(Resolve-AssetsFiles -ExplicitAssetsPath $AssetsPath -SearchRoot $Root)

if ($assetsFiles.Count -eq 0) {
    throw "No project.assets.json files were found. Pass -AssetsPath explicitly."
}

$results = [System.Collections.Generic.List[object]]::new()

foreach ($assetsFile in $assetsFiles) {
    $assets = Get-Content -LiteralPath $assetsFile -Raw -Encoding UTF8 | ConvertFrom-Json
    $packageRoots = @($assets.packageFolders.PSObject.Properties.Name)

    foreach ($target in $assets.targets.PSObject.Properties) {
        foreach ($packageEntry in $target.Value.PSObject.Properties) {
            $packageKey = $packageEntry.Name

            if (-not (Test-ContainsLiteral -Value $packageKey -Needle "/")) {
                continue
            }

            $packageParts = $packageKey.Split("/", 2)
            $packageId = $packageParts[0]
            $packageVersion = $packageParts[1]
            $library = Get-JsonPropertyValue -Object $assets.libraries -Name $packageKey
            $packageRelativePath = $null

            if ($null -ne $library) {
                $pathValue = Get-JsonPropertyValue -Object $library -Name "path"
                if (-not [string]::IsNullOrWhiteSpace($pathValue)) {
                    $packageRelativePath = $pathValue
                }
            }

            if ([string]::IsNullOrWhiteSpace($packageRelativePath)) {
                $packageRelativePath = ($packageId.ToLowerInvariant() + "/" + $packageVersion)
            }

            foreach ($sectionName in @("compile", "runtime")) {
                $section = Get-JsonPropertyValue -Object $packageEntry.Value -Name $sectionName

                if ($null -eq $section) {
                    continue
                }

                foreach ($asset in $section.PSObject.Properties) {
                    $assetPath = $asset.Name

                    if (-not $assetPath.EndsWith(".dll", [System.StringComparison]::OrdinalIgnoreCase)) {
                        continue
                    }

                    $fileName = Split-Path -Path $assetPath -Leaf
                    $haystack = "$packageKey $packageId $fileName $assetPath"

                    if (-not (Test-ContainsLiteral -Value $haystack -Needle $Query)) {
                        continue
                    }

                    foreach ($packageRoot in $packageRoots) {
                        $packageDirectory = Join-Path -Path $packageRoot -ChildPath $packageRelativePath
                        $candidatePath = Join-Path -Path $packageDirectory -ChildPath $assetPath

                        if (-not (Test-Path -LiteralPath $candidatePath -PathType Leaf)) {
                            continue
                        }

                        $results.Add([pscustomobject]@{
                            AssetsPath = $assetsFile
                            Target = $target.Name
                            Package = $packageKey
                            Section = $sectionName
                            Asset = $assetPath
                            Path = (Resolve-Path -LiteralPath $candidatePath).Path
                        })
                    }
                }
            }
        }
    }
}

$uniqueResults = @($results |
    Sort-Object -Property Target, Package, Section, Asset, Path -Unique)

if ($Json) {
    $uniqueResults | ConvertTo-Json -Depth 4
    return
}

if (($null -eq $uniqueResults) -or ($uniqueResults.Count -eq 0)) {
    Write-Warning "No restored DLL candidates matched '$Query'. Try the package ID, assembly name, or a shorter fragment."
    return
}

$uniqueResults | Select-Object Target, Package, Section, Asset, Path
