# Runs the unit tests, and uploads them to the CI server

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

If (Test-Path "nunit-console-x86.exe")
{
	$testFiles = [System.IO.Directory]::GetFiles("..\FlashDevelop\Bin\Debug", "*.Tests.dll")
	IF ($testFiles.Count -eq 0)
    {
    	Write-Output "No test assemblies found"
    }
    ELSE
    {
        $testErrors = $false
        #Should we break on first error? what if we want to set categories and prioritize them? think later if we want this
        foreach($testFile in $testFiles)
        {
            nunit-console-x86.exe "$testFile"
            $testErrors = $testErrors -or $LASTEXITCODE -ne 0
            if ((Test-Path env:\APPVEYOR_JOB_ID) -And (Test-Path TestResult.xml))
            {
                $wc = New-Object 'System.Net.WebClient'
                $wc.UploadFile("https://ci.appveyor.com/api/testresults/nunit/$($env:APPVEYOR_JOB_ID)", (Resolve-Path .\TestResult.xml))
            }
        }

        if ($testErrors -eq $true)
        {            exit 1        }    }
}
ELSE
{
	Write-Output "NUnit runner not found"
}