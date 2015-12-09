function Install-Module($iisWebsiteName, $buildConfigName, $attachFlag) 
{
	# Call DnnPackager.exe command line to build and deploy the selected project using EnvDte automation.	
	$project = Get-Project	
	$projectName = $project.ProjectName
	$dteVersion = $project.DTE.Version
	$processId = [System.Diagnostics.Process]::GetCurrentProcess().Id

	$thisScriptDir = Get-ScriptDirectory
	$commandPath = Join-Path $thisScriptDir "DnnPackager.exe"	

	if (!$buildConfigName)
	{
	    $solution = Get-Interface $dte.Solution ([EnvDTE80.Solution2])
		$solBuild = Get-Interface $solution.SolutionBuild ([EnvDTE.SolutionBuild])
		$solActiveConfig = Get-Interface $solBuild.ActiveConfiguration ([EnvDTE.SolutionConfiguration])
		$buildConfigName = [System.Convert]::ToString($solActiveConfig.Name) 
	}    

	if(!$attachFlag)
	{
			Write-Host "Executing $commandPath iiswebsite $installPackagesPath $websiteName"
	& $commandPath "build --envdteversion " $dteVersion "--processid " $processId "--configuration " $buildConfigName "--name " $projectName "--websitename " $iisWebsiteName | Write-Host
	}
	else
	{
			Write-Host "Executing $commandPath iiswebsite $installPackagesPath $websiteName"
	& $commandPath "build --envdteversion " $dteVersion "--processid " $processId "--configuration " $buildConfigName "--name " $projectName "--websitename " $iisWebsiteName " --attach" | Write-Host
	}	
}


function Get-ScriptDirectory {
    Split-Path -parent $PSCommandPath
}

function Get-Configurations()
{
    $solution = Get-Interface $dte.Solution ([EnvDTE80.Solution2])
    $solBuild = Get-Interface $solution.SolutionBuild ([EnvDTE.SolutionBuild])
	$configs = $solBuild.SolutionConfigurations
	$configs = [EnvDTE.SolutionConfigurations]::$solBuild.SolutionConfigurations 
	return $configs
}

Register-TabExpansion 'Install-Module' @{
   'buildConfigName' = { Get-Configurations | Select-Object -Property Name } 
}

Export-ModuleMember Install-Module