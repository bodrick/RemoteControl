function Build-Packages
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
        [ValidateSet('Release', 'Debug')]
        [string]$Configuration = 'Release',
        [switch]$Force
    )

    $BuildFlags = [List[string]]@(
        $Project,
        "--configuration $Configuration",
        '--no-restore'
        '--no-build',
        '--nologo',
        '--verbosity minimal'
    )

    if ($Force)
    {
        $BuildFlags += '--force'
    }

    $BaseCommand = "{0} {1} -bl:""$BasePath\build_logs\{0}.binlog"""

    if ($PSCmdlet.ShouldProcess($Project, 'Create packages'))
    {
        $PackCommand = $BaseCommand -f 'pack', ($BuildFlags -join ' ')
        Write-Host 'Creating packages...' -ForegroundColor 'Magenta'
        Write-Host "Pack Command: dotnet $PackCommand" -ForegroundColor 'Cyan'
        Start-Process dotnet -NoNewWindow -Wait -ArgumentList $PackCommand
        Write-Host 'Done creating packages.' -ForegroundColor 'Green'
    }
}
