function Install-Module($iisWebsiteName, $buildConfigName) 
{

# Clear existing output directory for extension zips (old builds might be in it)
	$solution = Get-Interface $dte.Solution ([EnvDTE80.Solution2])
    $solutionFolderPath =  Split-Path $solution.FullName -parent
	$installPackagesPath = join-path $solutionFolderPath "InstallPackages"
	Clear-Folder $installPackagesPath

# Build the currently selected project, this should cause the new zip to be output to $installPackagesPath.	
	$project = Get-Project	
	Build-Project $project $buildConfigName
		
# Now deploy the output zips to the specified dnn website.
	Deploy-Modules-To-IIS-Website $installPackagesPath $iisWebsiteName	

# Todo could look at subscribing to the event dte.Events.BuildEvents.OnBuildProjConfigDone which fires on completion of builds
# andlets you know if was successful, could then automatically deploy the module output?
 
}

function Deploy-Modules-To-IIS-Website($installPackagesPath, $websiteName)
{   
    $thisScriptDir = Get-ScriptDirectory
	$commandPath = Join-Path $thisScriptDir "DnnPackager.exe"
	Write-Host "Executing $commandPath iiswebsite $installPackagesPath $websiteName"
	& $commandPath "iiswebsite" $installPackagesPath $websiteName | Write-Host
}

function Get-ScriptDirectory {
    Split-Path -parent $PSCommandPath
}

function Clear-Folder($path)
{
    Write-Host "Clearing zip files from $path"
    $path = "$path{0}" -f "* -include .zip"
	Remove-Item $path -recurse -force
}

function Build-Project($project, $configuration)
{
    $projectName = [System.Convert]::ToString($project.UniqueName) 
	if (!$configuration)
	{
	    $solution = Get-Interface $dte.Solution ([EnvDTE80.Solution2])
		$solBuild = Get-Interface $solution.SolutionBuild ([EnvDTE.SolutionBuild])
		$solActiveConfig = Get-Interface $solBuild.ActiveConfiguration ([EnvDTE.SolutionConfiguration])
		$configuration = [System.Convert]::ToString($solActiveConfig.Name) 
	}    

	Write-Host "Building: $projectName in configuration: $configuration"

    $DTE.Solution.SolutionBuild.BuildProject($configuration, $projectName, $true)

    if ($DTE.Solution.SolutionBuild.LastBuildInfo)
    {       
        # throw "The project '$projectName' failed to build."
    }
}

function Get-SingleProject($name)
{
    $project = Get-Project $name

    if ($project -is [array])
    {
        throw "More than one project '$name' was found. Specify the full name of the one to use."
    }

    return $project
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