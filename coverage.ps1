$toolsPath = "$PSScriptRoot/.config/dotnet-tools.json"

if (!(Test-Path $toolsPath))
{
    dotnet new tool-manifest # if you are setting up this repo
}

dotnet tool install --local dotnet-reportgenerator-globaltool --version 5.1.9

dotnet test --collect:"XPlat Code Coverage" --results-directory coveragereport
dotnet reportgenerator -reports:coveragereport\*\*.xml -targetdir:report -reporttypes:Html
