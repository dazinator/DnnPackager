param (
    $InstallPath,
    $ToolsPath,
    $Package,
    $Project
)

if(!$InstallPath)
{
    Write-host "DnnPackager: No Install Path.."
}

Write-host "DnnPackager: Install Path: $InstallPath"
Write-host "DnnPackager: Tools Path: $ToolsPath"
Write-host "DnnPackager: Package: $Package"
Write-host "DnnPackager: Project: $Project"
Write-host "DnnPackager: Project Fullname: $Project.FullName"

#$PropsFile = 'DnnPackager.props'
$PropsPath = $ToolsPath | Join-Path -ChildPath $PropsFile
$ProjectPath = Split-Path $Project.FullName -parent
$ProjectPropsFile = 'DnnPackageBuilderOverrides.props'
$ProjectPropsPath = $ProjectPath | Join-Path -ChildPath $ProjectPropsFile

#$OldTargetsFileV1 = 'DnnPackager.targets'
#$TargetsFile = 'DnnPackager.Build.targets'
# $TargetsFolder = 'build\'
# $TargetsPath = $InstallPath | Join-Path -ChildPath $TargetsFolder
# $TargetsPath = $InstallPath | Join-Path -ChildPath $TargetsFile 
$TargetsPath = $ToolsPath | Join-Path -ChildPath $TargetsFile 
#$OctoPackTargetsFile = 'OctoPack.targets'

Add-Type -AssemblyName 'Microsoft.Build, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'

$MSBProject = [Microsoft.Build.Evaluation.ProjectCollection]::GlobalProjectCollection.GetLoadedProjects($Project.FullName) |
    Select-Object -First 1
    
Write-host "DnnPackager: MSBuild Project FullPath: $MSBProject.FullPath"

$ProjectUri = New-Object -TypeName Uri -ArgumentList "file://$($Project.FullName)"
$PropsUri = New-Object -TypeName Uri -ArgumentList "file://$PropsPath"
$TargetUri = New-Object -TypeName Uri -ArgumentList "file://$TargetsPath"
$RelativePropsPath = $ProjectUri.MakeRelativeUri($PropsUri) -replace '/','\'
$RelativeProjectPropsPath = $ProjectUri.MakeRelativeUri($ProjectPropsPath) -replace '/','\'
$RelativePath = $ProjectUri.MakeRelativeUri($TargetUri) -replace '/','\'

# PACKAGE BUILDER PROPS
# ================
# Ensure global props file added, remove existing if found.
#$ExistingImports = $MSBProject.Xml.Imports |
#    Where-Object { $_.Project -like "*\$PropsFile" }
#if ($ExistingImports) {
#    $ExistingImports | 
#        ForEach-Object {
#            $MSBProject.Xml.RemoveChild($_) | Out-Null
#        }
#}
#$MSBProject.Xml.AddImport($RelativePropsPath) | Out-Null

Write-host "DnnPackager: Adding import for project props file.."

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


# PACKAGE BUILDER TARGETS
# =======================

# REMOVE OLD V1 TARGETS FILE
#$ExistingImports = $MSBProject.Xml.Imports |
#    Where-Object { $_.Project -like "*\$OldTargetsFileV1" }
#if ($ExistingImports) {
#    $ExistingImports | 
#        ForEach-Object {
#            $MSBProject.Xml.RemoveChild($_) | Out-Null
#        }
#}

# ADD NEW TARGETS FILE. REMOVE FIRST IF EXISTS.
#$ExistingImports = $MSBProject.Xml.Imports |
#    Where-Object { $_.Project -like "*\$TargetsFile" }
#if ($ExistingImports) {
#    $ExistingImports | 
#        ForEach-Object {
#            $MSBProject.Xml.RemoveChild($_) | Out-Null
#        }
#}
#$MSBProject.Xml.AddImport($RelativePath) | Out-Null


# OCTOPUS TARGETS
# ================
# If the octopack targets file exists, ensure it is added after our targets / props.
#$ExistingImports = $MSBProject.Xml.Imports |
#    Where-Object { $_.Project -like "*\OctoPack.targets" }
#if ($ExistingImports) {
#    $ExistingImports | 
#        ForEach-Object {
#		    $OctoImport = $_
#            $MSBProject.Xml.RemoveChild($OctoImport) | Out-Null
#			# Add it back in at the end..
#			$MSBProject.Xml.AddImport('$(SolutionDir)\.octopack\OctoPack.targets') | Out-Null
#        }
#}

# save changes to project file.
$Project.Save()

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



  
  

