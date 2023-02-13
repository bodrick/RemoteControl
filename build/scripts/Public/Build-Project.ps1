function Build-Project
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
        [switch]$NoIncremental,
        [switch]$Force
    )

    $BuildFlags = [List[string]]@(
        $Project,
        "--configuration $Configuration",
        '--no-restore'
        '--nologo',
        '--verbosity minimal'
    )

    if ($Force)
    {
        $BuildFlags += '--force'
    }

    if ($NoIncremental)
    {
        $BuildFlags += '--no-incremental'
    }

    $BaseCommand = "{0} {1} -bl:""$BasePath\build_logs\{0}.binlog"""

    if ($PSCmdlet.ShouldProcess($Project, 'Build project'))
    {
        $BuildCommand = $BaseCommand -f 'build', ($BuildFlags -join ' ')
        Write-Host 'Building project...' -ForegroundColor 'Magenta'
        Write-Host "Build Command: dotnet $BuildCommand" -ForegroundColor 'Cyan'
        Start-Process dotnet -NoNewWindow -Wait -ArgumentList $BuildCommand
        Write-Host 'Done building.' -ForegroundColor 'Green'
    }
}
