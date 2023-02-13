$cleanExtensions = '*.suo', '*.user', '*.cache', '*.keep', 'StyleCop.Cache'
$cleanRecurseDirs = 'bin', 'obj', 'ClientBin', '_Resharper.*', '_Upgrade*', 'TestResults'
$cleanDirs = 'Generated', 'artifacts'

function Clean-Build
{
    Write-Verbose "Cleaning build path - $SolutionDir"
    $files = Get-ChildItem -Path $SolutionDir -Include $cleanExtensions -Recurse -File
    $files | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

    $dirs = Get-ChildItem -Path $SolutionDir -Include $cleanRecurseDirs -Recurse -Directory
    $dirs | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

    $dirs = Get-ChildItem -Path $SolutionDir -Include $cleanDirs -Directory
    $dirs | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
    Write-Verbose 'Cleaning finished'
}
