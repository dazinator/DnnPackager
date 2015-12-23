param (
    $InstallPath,
    $ToolsPath,
    $Package,
    $Project
)

Write-host "DnnPackager: Install Path: $InstallPath"
Write-host "DnnPackager: Tools Path: $ToolsPath"
Write-host "DnnPackager: Project Fullname: $($Project.FullName)"

$Project.Save($Project.FullName)

Add-Type -AssemblyName 'Microsoft.Build, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'

$msBuildProjects = [Microsoft.Build.Evaluation.ProjectCollection]::GlobalProjectCollection
$msBuildProject = $msBuildProjects.GetLoadedProjects($Project.FullName) | Select-Object -First 1

Write-host "DnnPackager: MSBuild project loaded for: $($msBuildProject.FullPath)"

if($msBuildProject -eq $null)
{
    Write-host "DnnPackager: Could Not Load MSBuild Project $($Project.FullName) as it wasn't in the GlobalProjectCollection. Wait for it to be added? "		
}

$PropsFile = 'DnnPackager.props'
$PropsPath = $ToolsPath | Join-Path -ChildPath $PropsFile

$ExistingImports = $msBuildProject.Xml.Imports |
    Where-Object { $_.Project -like "*\$PropsFile" }
if ($ExistingImports) {
    $ExistingImports | 
        ForEach-Object {
            $msBuildProject.Xml.RemoveChild($_) | Out-Null
        }
}

$ProjectUri = New-Object -TypeName Uri -ArgumentList "file://$($Project.FullName)"
$PropsUri = New-Object -TypeName Uri -ArgumentList "file://$PropsPath"
$RelativePropsPath = $ProjectUri.MakeRelativeUri($PropsUri) -replace '/','\'
$msBuildProject.Xml.AddImport($RelativePropsPath) | Out-Null

Write-host "DnnPackager: Imported props file."

#.GetLoadedProjects($Project.FullName)
# register-objectEvent -inputObject $projects -eventName "EventArrived" -action $action


 


#$DnnPackagerExeShortFileName = 'DnnPackager.exe'
#$DnnPackagerExeFile = $ToolsPath | Join-Path -ChildPath $DnnPackagerExeShortFileName

#Write-Host "Executing install-targets --projectfilepath $($Project.FullName) --toolspath $ToolsPath"
#& $DnnPackagerExeFile "install-targets" "--projectfilepath" $Project.FullName "--toolspath" $ToolsPath | Write-Host


 function Add-SolutionFolder {
    param(
       [string]$Name
    )
    $solution2 = Get-Interface $dte.Solution ([EnvDTE80.Solution2])
    $solution2.AddSolutionFolder($Name)
}
 
function Get-SolutionFolder {
    param (
        [string]$Name
    )
	$solution2 = Get-Interface $dte.Solution ([EnvDTE80.Solution2])
    $solution2.Projects | ?{ $_.Kind -eq [EnvDTE80.ProjectKinds]::vsProjectKindSolutionFolder -and $_.Name -eq $Name }
}

 # Ensure solution packaging folder exists.
 $SolutionPackagingFolderName = "Solution Items"
 
 Write-host "DnnPackager: Getting solution folder $SolutionPackagingFolderName"

 $SolutionPackagingFolder = Get-SolutionFolder $SolutionPackagingFolderName

 if($SolutionPackagingFolder -eq $null)
	{
	    Write-host "DnnPackager: Creating solution folder $SolutionPackagingFolderName because $($SolutionPackagingFolder -eq $null)"
		$SolutionPackagingFolder = Add-SolutionFolder $SolutionPackagingFolderName
	}

# Add a solution nuspec file to the solution.

$solution = Get-Interface $dte.Solution ([EnvDTE80.Solution2])
$solutionFolderPath =  Split-Path $solution.FullName -parent

$DestinationSolutionNuspecFileName = "Solution.nuspec"
$SourceSolutionNuspecFileName = "Solution.nuspecc"
$ToolsSolutionNuspecPath = $ToolsPath | Join-Path -ChildPath $SourceSolutionNuspecFileName
$DestinationSolutionNuspecFilePath = $solutionFolderPath | Join-Path -ChildPath $DestinationSolutionNuspecFileName

Write-host "DnnPackager: Saving '$ToolsSolutionNuspecPath' to '$DestinationSolutionNuspecFilePath'." 

$xml = New-Object XML
$xml.Load("$ToolsSolutionNuspecPath")
# Can manipulate file here if necessary.

# set package id based on solution name.
$solutionFileName = [System.IO.Path]::GetFileNameWithoutExtension($solution.FullName);
$packageIdName = $solutionFileName -replace " ", ""

$xml.package.metadata.id = "DotNetNuke.SolutionPackages.$packageIdName"
$xml.package.metadata.title = "$solutionFileName DotNetNuke Solution Packages"
$xml.package.metadata.description = "Contains the $solutionFileName solution packages"

Write-host "DnnPackager: Saving '$DestinationSolutionNuspecFilePath'." 
$xml.Save("$DestinationSolutionNuspecFilePath")

# Add the solution nuspec file to the solution
Write-host "DnnPackager: Adding solution nuspec to solution." 

$projectItems = Get-Interface $SolutionPackagingFolder.ProjectItems ([EnvDTE.ProjectItems])
$projectItems.AddFromFile("$DestinationSolutionNuspecFilePath")


#$addedSolutionNuspecFile = $sfolder.AddFromFile("$DestinationSolutionNuspecFilePath")



  
  

