# Deploys the built binaries to the FD.org server

Param (
    $variables = @{},   
    $scriptPath,
    $buildFolder,
    $srcFolder,
    $outFolder,
    $tempFolder,
    $projectName,
    $projectVersion,
    $projectBuildNumber
)

$login = $variables["SecureLogin"]
$pass = $variables["SecurePass"]

If (Test-Path "nunit-console-x86.exe"){
	$testFiles = Get-ChildItem FlashDevelop\Bin\Debug -filter "*.Tests.dll")
	IF ($testFiles.Count == 0)
    {
    	Write-Output "No test assemblies found"
    }
    ELSE
    {
        foreach($testFile in $testFiles)
        {
            nunit-console-x86.exe $testFile
            $wc = New-Object 'System.Net.WebClient'
            $wc.UploadFile("https://ci.appveyor.com/api/testresults/xunit/$($env:APPVEYOR_JOB_ID)", (Resolve-Path .\TestResult.xml))
        }
    }
}
ELSE
{
	Write-Output "NUnit runner not found"
}