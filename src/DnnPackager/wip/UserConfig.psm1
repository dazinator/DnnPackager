function Set-DnnConfig($configKey, $configValue)
{



}

Register-TabExpansion 'Set-DnnConfig' @{
   'configKey' = { 
      "LocalDnnWebsiteName",
      "AutoDeployAfterSuccessfulBuild"      
}

function GetActiveConfigXml($toolsPath, $solutionFolderPath) 
{
	# Get the open solution.
	$solution = Get-Interface $dte.Solution ([EnvDTE80.Solution2])

	# Create the parent solution folder. todo: only if it doesn't exist already.
	$configProjectName = ".DnnPackageBuilderConfig";

	# Create a solution level folder to hold config. If it already exists then don't do anything.
	$configProject = $solution.Projects | Where {$_.ProjectName -eq $configProjectName}
	if (!$configProject) {
		$configProject = $solution.AddSolutionFolder($configProjectName)
	}

	# Ensure config file exists.
	$currentConfigFilePath = GetCurrentConfigFileName $solutionFolderPath	
	$configFileExists = Test-Path $currentConfigFilePath

	if(!$configFileExists)
	{
		# There is no current config file, so create one by copying the default.
		$defaultConfigFileName = "user.config"
		$defaultConfigFilePath = join-path $toolsPath $defaultConfigFileName
		Copy-Item -Path $defaultConfigFilePath -Destination $currentConfigFilePath
	}

	# Ensure the current config file is in the config project.
	$configProjectItems = Get-Interface $configProject.ProjectItems ([EnvDTE.ProjectItems])
	$currentConfigItem = $configProjectItems | Where {$_.FileNames[1] -eq $currentConfigFilePath}
	if (!$currentConfigItem) {   
		$configProjectItems.AddFromFile($currentConfigFilePath)	
	}

	[xml]$xml = Get-Content $currentConfigFilePath
	return $xml	
}

function GetCurrentConfigFileName($solutionFolderPath)
{
	# We formulate a config file named for the current machine name.
	# This ensures different config is used when the solution is opened on different machines.
	# This is necessary because different machines need to have configurable values for their local IIS website names / configuration. 
	$configName = $env:computername	
	$configName -replace ".", ""
	$configName -replace "\", ""
	$configName -replace "/", ""
	$configName = "machine-$configName.xml"
	$userConfigFilePath = join-path $solutionFolderPath $configName
	return $userConfigFilePath
}