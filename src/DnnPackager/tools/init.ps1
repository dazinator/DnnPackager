param (
    $InstallPath,
    $ToolsPath,
    $Package   
)

Write-Host "Setting up solution post build target"
echo "Running package builder init ps1!"

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
          # allready exists.
		  Write-Host "Solution post build targets file already exists."
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
          # allready exists.
		  Write-Host "Solution before build targets file already exists."
}
else
{    
	$xml = New-Object XML
	$xml.Load("$sourceBeforeSolutionTargetsFileName")
	# Can manipulate file here if necessary.
	$xml.Save("$destinationBeforeTargetsFilePath")   
}

Import-Module (Join-Path $toolsPath ModuleDeployment.psm1)







