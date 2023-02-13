
function Restore-Project
{
    [CmdletBinding(SupportsShouldProcess)]
    param
    (
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string]$Project,
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string]$BasePath,
        [switch]$Force
    )

    $RestoreFlags = [List[string]]@(
        $Project,
        '--nologo',
        '--verbosity minimal'
    )

    if ($Force)
    {
        $RestoreFlags += '--force'
    }

    $BaseCommand = "{0} {1} -bl:""$BasePath\build_logs\{0}.binlog"""

    if ($PSCmdlet.ShouldProcess($Project, 'Restore project'))
    {
        $RestoreCommand = $BaseCommand -f 'restore', ( $RestoreFlags -join ' ')
        Write-Host 'Restore project...' -ForegroundColor 'Magenta'
        Write-Host "Restore Command: dotnet $RestoreCommand" -ForegroundColor 'Cyan'
        Start-Process dotnet -NoNewWindow -Wait -ArgumentList $RestoreCommand
        Write-Host 'Done restore.' -ForegroundColor 'Green'
    }
}
