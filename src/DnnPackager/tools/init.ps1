param (
    $InstallPath,
    $ToolsPath,
    $Package   
)

Write-Host "DnnPackager: Setting up solution post build target"
echo "DnnPackager: Running package builder init ps1!"

# Get solution file name
# and produce "after.solutionname.sln" file.

$solution = Get-Interface $dte.Solution ([EnvDTE80.Solution2])
$solutionFileName = [System.IO.Path]::GetFileNameWithoutExtension($solution.FullName);
$solutionFolderPath =  Split-Path $solution.FullName -parent

# after solution build.
$sourceTargetsFileName = "after.solutionname.sln.targets";
$sourceSolutionTargetsFileName = join-path $ToolsPath $sourceTargetsFileName
$destinationTargetsFileName = "after.$solutionFileName.sln.targets";
$destinationTargetsFilePath = join-path $solutionFolderPath $destinationTargetsFileName

# If file allready exists then don't do anything, otherwise create it.
if (Test-Path $destinationTargetsFilePath)
{
    Try
    {


      # allready exists.
		  Write-Host "DnnPackager: Solution post build targets file already exists. Overwriting.."
      $xml = New-Object XML
	    $xml.Load("$sourceSolutionTargetsFileName")	
	    $xml.Save("$destinationTargetsFilePath")

     }
    Catch [system.exception]
    {
     Write-host "DnnPackager: Exception String: $_.Exception.Message" 
    }
}
else
{    
	$xml = New-Object XML
	$xml.Load("$sourceSolutionTargetsFileName")
	# Can manipulate file here if necessary.
	$xml.Save("$destinationTargetsFilePath")
    # copy-item $sourceSolutionTargetsFileName $destinationSolutionTargetsFileName
}

#before solution build
$sourceBeforeTargetsFileName = "before.solutionname.sln.targets";
$sourceBeforeSolutionTargetsFileName = join-path $ToolsPath $sourceBeforeTargetsFileName
$destinationBeforeTargetsFileName = "before.$solutionFileName.sln.targets";
$destinationBeforeTargetsFilePath = join-path $solutionFolderPath $destinationBeforeTargetsFileName

# If file allready exists then don't do anything, otherwise create it.
if (Test-Path $destinationBeforeTargetsFilePath)
{
     Try
    {


      # allready exists.
		  Write-Host "DnnPackager: Solution before build targets file already exists."
      $xml = New-Object XML
	    $xml.Load("$sourceBeforeSolutionTargetsFileName")	
	    $xml.Save("$destinationBeforeTargetsFilePath")

     }
    Catch [system.exception]
    {
     Write-host "DnnPackager: Exception String: $_.Exception.Message" 
    }   
		 
}
else
{    
	$xml = New-Object XML
	$xml.Load("$sourceBeforeSolutionTargetsFileName")
	# Can manipulate file here if necessary.
	$xml.Save("$destinationBeforeTargetsFilePath")   
}

Import-Module (Join-Path $ToolsPath ModuleDeployment.psm1)
Write-Host "DnnPackager: Imported DnnPackager Powershell CmdLets."






