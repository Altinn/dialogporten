param(
    [Parameter(Mandatory=$true)]
    [string]$DirectoryPath
)

# Verify that the directory exists
if (-not (Test-Path $DirectoryPath -PathType Container)) {
    Write-Error "The supplied directory does not exist!"
    exit 1
}

# Get all *.js files in the directory except for "all-tests.js"
$jsFiles = Get-ChildItem -Path $DirectoryPath -Filter "*.js" |
    Where-Object { $_.Name -ne "all-tests.js" } |
    Where-Object { $_.Directory.FullName -notlike "$(Join-Path -ChildPath "$(Join-Path 'performance' '*')" -Path *)" } #  OS agnostic "*/performance/*"

# Create the import strings
$importStatements = $jsFiles | ForEach-Object {
    "import { default as $($_.BaseName) } from './$($_.Name)';"
}

# Create the function string
$functionBody = $jsFiles | ForEach-Object {
    "$($_.BaseName)();"
}

$scriptContent = @"
// This file is generated, see "scripts" directory
$($importStatements -join "`n")

export default function() {
  $($functionBody -join "`n  ")
}

"@

# Output the script content to "all-tests.js"
$scriptContent -replace "`r`n","`n" | Out-File -FilePath (Join-Path $DirectoryPath "all-tests.js") -Encoding utf8 -NoNewline

Write-Output "all-tests.js has been generated successfully."
