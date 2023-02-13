using namespace System.Collections.Generic

Set-StrictMode -Version Latest

Write-Verbose 'Starting Importing'
Write-Verbose $PSScriptRoot

# Get public and private function definition files
$Classes = @(Get-ChildItem -Path "$PSScriptRoot\Classes\*.ps1" -Recurse -Exclude '*.Tests.ps1' -ErrorAction SilentlyContinue)
$Private = @(Get-ChildItem -Path "$PSScriptRoot\Private\*.ps1" -Recurse -Exclude '*.Tests.ps1' -ErrorAction SilentlyContinue)
$Public = @(Get-ChildItem -Path "$PSScriptRoot\Public\*.ps1" -Recurse -Exclude '*.Tests.ps1' -ErrorAction SilentlyContinue)

# Dot source the .ps1 definition files
foreach ($import in @($Classes + $Private + $Public))
{
    try
    {
        . $import.FullName
    }
    catch
    {
        Write-Error -Message "Failed to import function $($import.FullName)"
        Write-Error $_.Exception.Message
        Write-Error $_.ScriptStackTrace
        $PSCmdlet.ThrowTerminatingError($_)
    }
}

# Export the public functions/module members only
Export-ModuleMember -Function $Public.BaseName
