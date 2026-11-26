
# dotnet test --collect:"XPlat Code Coverage"
# reportgenerator -reports:**/coverage.cobertura.xml -targetdir:$(Build.SourcesDirectory)/coverlet/reports -reporttypes:"Cobertura"

$basedir = Split-Path  -Parent $MyInvocation.MyCommand.Definition
$reporttargetdir = "$basedir\TestResults"
$testprojectdir = "$basedir\src\tests"
$slnDir = "$basedir\MiCake.All.sln"

Write-Output $reporttargetdir

# Clearn reports
Write-Output "Clean Report Folder...."
Remove-Item -Path $reporttargetdir -Recurse

# Build
dotnet build $slnDir -c "release"

#test and get coverage
$alltestproj = Get-Item "$testprojectdir\**\*.csproj"
Write-Output $alltestproj | Format-List -Property Name

foreach ($item in $alltestproj) {
    # The performance baseline scenarios (Category=Performance) are excluded from the
    # regular suite: they are serialized and their elapsed measurements are sensitive to
    # machine state. Run them on demand with --filter "Category=Performance" against the
    # MiCake.IntegrationTests project, without this filter.
    dotnet test $item --collect:"XPlat Code Coverage" --results-directory "$reporttargetdir\source" --settings "$testprojectdir\runsettings.xml" --filter "Category!=Performance" --no-build --no-restore
}

# reportgenerator
reportgenerator -reports:"$reporttargetdir\source\**\coverage.cobertura.xml" -targetdir:$reporttargetdir 
