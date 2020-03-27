
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
    dotnet test $item --collect:"XPlat Code Coverage" --results-directory "$reporttargetdir\source" --settings "$testprojectdir\runsettings.xml"  --no-build --no-restore
}

# reportgenerator
reportgenerator -reports:"$reporttargetdir\source\**\coverage.cobertura.xml" -targetdir:$reporttargetdir 
