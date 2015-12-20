param (
    $InstallPath,
    $ToolsPath,
    $Package,
    $Project
)

Write-host "DnnPackager: Install Path: $InstallPath"
Write-host "DnnPackager: Tools Path: $ToolsPath"
Write-host "DnnPackager: Project Fullname: $($Project.FullName)"

$PropsFile = 'DnnPackager.props'
$PropsPath = $ToolsPath | Join-Path -ChildPath $PropsFile
$ProjectPath = Split-Path $Project.FullName -parent
$ProjectPropsFile = 'DnnPackageBuilderOverrides.props'
$ProjectPropsPath = $ProjectPath | Join-Path -ChildPath $ProjectPropsFile

$TargetsFile = 'dnnpackager.targets'
# $TargetsFolder = 'build\'
# $TargetsPath = $InstallPath | Join-Path -ChildPath $TargetsFolder
# $TargetsPath = $InstallPath | Join-Path -ChildPath $TargetsFile 
$TargetsPath = $ToolsPath | Join-Path -ChildPath $TargetsFile 

$ProjectUri = New-Object -TypeName Uri -ArgumentList "file://$($Project.FullName)"
$PropsUri = New-Object -TypeName Uri -ArgumentList "file://$PropsPath"
$TargetUri = New-Object -TypeName Uri -ArgumentList "file://$TargetsPath"
$RelativePropsPath = $ProjectUri.MakeRelativeUri($PropsUri) -replace '/','\'
$RelativeProjectPropsPath = $ProjectUri.MakeRelativeUri($ProjectPropsPath) -replace '/','\'
$RelativePath = $ProjectUri.MakeRelativeUri($TargetUri) -replace '/','\'

Write-host "DnnPackager: Project URI: $ProjectUri"
Write-host "DnnPackager: Props URI: $PropsUri"
Write-host "DnnPackager: Target URI: $TargetUri"
Write-host "DnnPackager: Relative Props Path: $RelativePropsPath"
Write-host "DnnPackager: Relative Project Props Path: $RelativeProjectPropsPath"
Write-host "DnnPackager: Relative Target Path: $RelativePath"

Add-Type -AssemblyName 'Microsoft.Build, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'

$MSBProject = [Microsoft.Build.Evaluation.ProjectCollection]::GlobalProjectCollection.GetLoadedProjects($Project.FullName) | Select-Object -First 1
    
Write-host "DnnPackager: MSBuild Project FullPath: $($MSBProject.FullPath)"
Write-host "DnnPackager: Ensuring global props imported.."

# PACKAGE BUILDER PROPS
# ================
# Ensure global props file added, replace existing if found.
$ExistingImports = $MSBProject.Xml.Imports |
    Where-Object { $_.Project -like "*\$PropsFile" }
if ($ExistingImports) {
    $ExistingImports | 
        ForEach-Object {
            $MSBProject.Xml.RemoveChild($_) | Out-Null
        }
}
$MSBProject.Xml.AddImport($RelativePropsPath) | Out-Null

Write-host "DnnPackager: Ensuring project props imported.."

# PACKAGE BUILDER PROJECT PROPS / OVERRIDES
# =========================================
# Ensure project level props file added.
$ExistingImports = $MSBProject.Xml.Imports |
    Where-Object { $_.Project -like "$ProjectPropsFile" }
if ($ExistingImports) {
    $ExistingImports | 
        ForEach-Object {
            $MSBProject.Xml.RemoveChild($_) | Out-Null
        }
}
$MSBProject.Xml.AddImport($RelativeProjectPropsPath) | Out-Null

Write-host "DnnPackager: Added import for project props file.."

Write-host "DnnPackager: Ensuring targets imported.."

# PACKAGE BUILDER TARGETS
# =======================
# REMOVE OLD V1 TARGETS FILE
$OldTargetsFile = 'DnnPackager.Build.targets'
$ExistingImports = $MSBProject.Xml.Imports |
    Where-Object { $_.Project -like "*\$OldTargetsFile" }
if ($ExistingImports) {
    $ExistingImports | 
        ForEach-Object {
            $MSBProject.Xml.RemoveChild($_) | Out-Null
        }
}

# ADD NEW TARGETS FILE. Replace existing.
$ExistingImports = $MSBProject.Xml.Imports |
    Where-Object { $_.Project -like "*\$TargetsFile" }
if ($ExistingImports) {
    $ExistingImports | 
        ForEach-Object {
            $MSBProject.Xml.RemoveChild($_) | Out-Null
        }
}
$MSBProject.Xml.AddImport($RelativePath) | Out-Null


# OCTOPUS TARGETS
# ================
# If the octopack targets file exists, ensure it is added after our targets / props.
$ExistingImports = $MSBProject.Xml.Imports |
    Where-Object { $_.Project -like "*\OctoPack.targets" }
if ($ExistingImports) {
    $ExistingImports | 
        ForEach-Object {
		    $OctoImport = $_
            $MSBProject.Xml.RemoveChild($OctoImport) | Out-Null
			# Add it back in at the end..
			$MSBProject.Xml.AddImport('$(SolutionDir)\.octopack\OctoPack.targets') | Out-Null
        }
}

# save changes to project file.
Write-host "DnnPackager: Project Saved? $($Project.Saved)"
Write-host "DnnPackager: Saving Project.."
$Project.Save($Project.FullName)
#$MSBProject.Save()
Write-host "DnnPackager: Project Saved."

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



  
  

