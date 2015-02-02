param (
    $InstallPath,
    $ToolsPath,
    $Package,
    $Project
)

$PropsFile = 'DnnPackager.props'
$PropsPath = $ToolsPath | Join-Path -ChildPath $PropsFile
$ProjectPath = Split-Path $Project.FullName -parent
$ProjectPropsFile = 'DnnPackageBuilderOverrides.props'
$ProjectPropsPath = $ProjectPath | Join-Path -ChildPath $ProjectPropsFile

$TargetsFile = 'DnnPackager.targets'
# $TargetsFolder = 'build\'
# $TargetsPath = $InstallPath | Join-Path -ChildPath $TargetsFolder
# $TargetsPath = $InstallPath | Join-Path -ChildPath $TargetsFile 
$TargetsPath = $ToolsPath | Join-Path -ChildPath $TargetsFile 
$OctoPackTargetsFile = 'OctoPack.targets'

Add-Type -AssemblyName 'Microsoft.Build, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'

$MSBProject = [Microsoft.Build.Evaluation.ProjectCollection]::GlobalProjectCollection.GetLoadedProjects($Project.FullName) |
    Select-Object -First 1

$ProjectUri = New-Object -TypeName Uri -ArgumentList "file://$($Project.FullName)"
$PropsUri = New-Object -TypeName Uri -ArgumentList "file://$PropsPath"
$TargetUri = New-Object -TypeName Uri -ArgumentList "file://$TargetsPath"
$RelativePropsPath = $ProjectUri.MakeRelativeUri($PropsUri) -replace '/','\'
$RelativeProjectPropsPath = $ProjectUri.MakeRelativeUri($ProjectPropsPath) -replace '/','\'
$RelativePath = $ProjectUri.MakeRelativeUri($TargetUri) -replace '/','\'

# PACKAGE BUILDER PROPS
# ================
# Ensure global props file added, remove existing if found.
$ExistingImports = $MSBProject.Xml.Imports |
    Where-Object { $_.Project -like "*\$PropsFile" }
if ($ExistingImports) {
    $ExistingImports | 
        ForEach-Object {
            $MSBProject.Xml.RemoveChild($_) | Out-Null
        }
}
$MSBProject.Xml.AddImport($RelativePropsPath) | Out-Null

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
# Ensure targets file added, remove existing if found.
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
$Project.Save()

# Check for nuspec file and rename.
$oldnuspecFileName = "rename.nuspecc"
$newnuspecFileName = $project.Name + ".nuspec"

Try
 {
     # is there allready a nuspec file there.
	 Write-host "Looking for existing project nuspec file in project items named: $oldnuspecFileName" 
	
	 $existingFile = $project.ProjectItems.Item($oldnuspecFileName)
	 if($existingFile -eq $NULL)
		{
		 Write-host "Did not find $oldnuspecFileName in project." 
		}
		else
		{
		  # yes, so rename..
		  # remove the 'old' item from the project
					Write-host "Removing $oldnuspecFileName" 
					$existingFile.Remove();
					Write-host "Successfully removed $oldnuspecFileName now renaming underlying file." 
				
					# Rename the underlying file.
					
					$ProjectNuspecPath = $ProjectPath | Join-Path -ChildPath $oldnuspecFileName					
					Rename-Item $ProjectNuspecPath $newnuspecFileName
					Write-host "Successfully renamed file to $newnuspecFileName" 
					
					# Move-Item $ProjectNuspecPath $NewProjectNuspecPath
					

					$NewProjectNuspecPath = $ProjectPath | Join-Path -ChildPath $newnuspecFileName							

					Write-host "Adding $NewProjectNuspecPath file to project" 
										
					$newlyAddedFile = $project.ProjectItems.AddFromFile("$NewProjectNuspecPath");

					Write-host "Successfully added $NewProjectNuspecPath file to project." 

					#$newlyAddedFile.Properties.Item("SubType").Value = $sourceFile.Properties.Item("SubType").Value;				    
					#Write-host "Successfully set subtype to  $NewProjectNuspecPath file to project, now setting subtype." 
					#$sourceFile.Name = $newnuspecFileName				   


		}	 
	 
 }
 Catch [system.exception]
 {
     Write-host "Exception String: $_.Exception.Message" 
 }

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
 
 Write-host "Getting solution folder $SolutionPackagingFolderName"

 $SolutionPackagingFolder = Get-SolutionFolder $SolutionPackagingFolderName

 if($SolutionPackagingFolder -eq $null)
	{
	    Write-host "Creating solution folder $SolutionPackagingFolderName because $($SolutionPackagingFolder -eq $null)"
		$SolutionPackagingFolder = Add-SolutionFolder $SolutionPackagingFolderName
	}

# Add a solution nuspec file to the solution.

$solution = Get-Interface $dte.Solution ([EnvDTE80.Solution2])
$solutionFolderPath =  Split-Path $solution.FullName -parent

$DestinationSolutionNuspecFileName = "Solution.nuspec"
$SourceSolutionNuspecFileName = "Solution.nuspecc"
$ToolsSolutionNuspecPath = $ToolsPath | Join-Path -ChildPath $SourceSolutionNuspecFileName
$DestinationSolutionNuspecFilePath = $solutionFolderPath | Join-Path -ChildPath $DestinationSolutionNuspecFileName

Write-host "Saving '$ToolsSolutionNuspecPath' to '$DestinationSolutionNuspecFilePath'." 

$xml = New-Object XML
$xml.Load("$ToolsSolutionNuspecPath")
# Can manipulate file here if necessary.

# set package id based on solution name.
$solutionFileName = [System.IO.Path]::GetFileNameWithoutExtension($solution.FullName);
$packageIdName = $solutionFileName -replace " ", ""

$xml.package.metadata.id = "DotNetNuke.SolutionPackages.$packageIdName"
$xml.package.metadata.title = "$solutionFileName DotNetNuke Solution Packages"
$xml.package.metadata.description = "Contains the $solutionFileName solution packages"

Write-host "Saving '$DestinationSolutionNuspecFilePath'." 
$xml.Save("$DestinationSolutionNuspecFilePath")

# Add the solution nuspec firl to the solution
Write-host "Adding solution nuspec to solution." 

$projectItems = Get-Interface $SolutionPackagingFolder.ProjectItems ([EnvDTE.ProjectItems])
$projectItems.AddFromFile("$DestinationSolutionNuspecFilePath")


#$addedSolutionNuspecFile = $sfolder.AddFromFile("$DestinationSolutionNuspecFilePath")



  
  

