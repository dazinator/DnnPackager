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

function Remove-Import {
    param(
       [Microsoft.Build.Construction.ProjectRootElement]$projectRootXml,
       [string]$projectName
    )
    $ExistingImports = $projectRootXml.Imports | Where-Object { $_.Project -like "*\$projectName" }
    if ($ExistingImports) {
        $ExistingImports | ForEach-Object {
            $projectRootXml.RemoveChild($_) | Out-Null
        }
    }
}

function Add-Import {
    param(
       [Microsoft.Build.Construction.ProjectRootElement]$projectRootXml,      
       [string]$projectFullPath        
    )
    
    Write-host "DnnPackager: Adding Import for: $($projectFullPath)"
    $ParentProjectUri = New-Object -TypeName Uri -ArgumentList "file://$($Project.FullName)"
    $ChildProjectToImportUri = New-Object -TypeName Uri -ArgumentList "file://$projectFullPath"
    $RelativePathForImport = $ParentProjectUri.MakeRelativeUri($ChildProjectToImportUri) -replace '/','\'
    $projectRootXml.AddImport($RelativePathForImport) | Out-Null   
}

function Move-ImportToEndIfExists {
    param(
       [Microsoft.Build.Construction.ProjectRootElement]$projectRootXml,
       [string]$projectFullPath
    )

    $name = [System.IO.Path]::GetFileName($projectFullPath)

    $ExistingImports = $projectRootXml.Imports | Where-Object { $_.Project -like "*\$name" }
    if ($ExistingImports) {
        $ExistingImports | ForEach-Object {
            $projectRootXml.RemoveChild($_) | Out-Null
            $projectRootXml.AddImport($_.Project) | Out-Null            
        }
    }   
}

function Ensure-Import {
    param(
       [Microsoft.Build.Construction.ProjectRootElement]$projectRootXml,
       [string]$projectFullPath
    )

    $name = [System.IO.Path]::GetFileName($projectFullPath)
    Remove-Import $projectRootXml $name
    Add-Import $projectRootXml $projectFullPath    
}

function Install-Imports {
    param(
       [Microsoft.Build.Construction.ProjectRootElement]$projectRootXml      
    )

    # ensure import of dnnpackager.props from package tools dir.
    $PropsFile = 'DnnPackager.props'
    $PropsFilePath = $ToolsPath | Join-Path -ChildPath $PropsFile
    Ensure-Import $projectRootXml $PropsFilePath

    # ensure import of DnnPackageBuilderOverrides.props from the projects dir.
    $ProjectPath = Split-Path $Project.FullName -parent
    $ProjectPropsFile = 'DnnPackageBuilderOverrides.props'
    $ProjectPropsPath = $ProjectPath | Join-Path -ChildPath $ProjectPropsFile
    Ensure-Import $projectRootXml $ProjectPropsPath

    # ensure old targets file is not imported.
    $oldTargetsFile = 'DnnPackager.Build.targets'
    Remove-Import $oldTargetsFile

    # ensure targets file is imported.
    $targetsFile = 'dnnpackager.targets'
    $targetsFilePath = $ToolsPath | Join-Path -ChildPath $targetsFile
    Ensure-Import $projectRootXml $targetsFilePath

    # mode octopack import to end if it exists
    $octopackTargetFile = 'OctoPack.targets'
    Move-ImportToEndIfExists $octopackTargetFile

    $Project.Save()
    Write-host "DnnPackager: Imports installed."
}

function Get-VSSolution {
    $vsSolutionObject = [Microsoft.VisualStudio.Shell.Package]::GetGlobalService([Microsoft.VisualStudio.Shell.Interop.IVsSolution])
    $vsSolution = Get-Interface $vsSolutionObject ([Microsoft.VisualStudio.Shell.Interop.IVsSolution])
    return $vsSolution
}

function Get-VSProjectHierarchy {
    $vsSolution = Get-VSSolution
    $projectHierarchyObject = $null
    $project = Get-Project
    $result = $vsSolution.GetProjectOfUniqueName($project.UniqueName,([ref]$projectHierarchyObject))
    return $projectHierarchyObject
}

function Is-CpsProject {
    $vsHierarchy = Get-VSProjectHierarchy    
    $isCpsProject = [Microsoft.VisualStudio.Shell.PackageUtilities]::IsCapabilityMatch($vsHierarchy, "CPS")
    return $isCpsProject
}


$isCps = Is-CpsProject
$msBuildProject = $null
if($isCps)
{
    Write-host "DnnPackager: CPS Project Detected.."	
    Add-Type -AssemblyName 'Microsoft.VisualStudio.ProjectSystem.V14Only, Version=14.1.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
    Add-Type -Path (Join-Path $ToolsPath "DnnPackager.CpsProjectSupport.dll")

     # get vs project
    $vsProjectHierarchy = Get-VSProjectHierarchy    
    $projectLockService = $vsProjectHierarchy.UnconfiguredProject.ProjectService.Services.ProjectLockService   
    if($projectLockService -eq $null)
    {
        Write-host "DnnPackager: Failed to find project lock service."
        return $null
    }
   
    try
    {                         
                 
                
        Write-host "DnnPackager: Getting unconfigured project."
        $unconfiguredProject = $vsProjectHierarchy.UnconfiguredProject                
        #Add-Type -ReferencedAssemblies $Assem -TypeDefinition $Source -Language CSharp  

        #Write-host "DnnPackager: Creating callback.."  
        #[Action[Microsoft.Build.Evaluation.Project]]$action = {
        #    param($proj)   
        #    Write-host "DnnPackager: Callback executing.."              
        #    $projectRoot = $proj.Xml
        #    Install-Imports $projectRoot                
        #}
      
        Write-host "DnnPackager: Getting MsBuild Project.."  
        $task = [DnnPackager.CpsProjectSupport.CpsHelper]::InstallTargets($Project, $projectLockService, $unconfiguredProject, $ToolsPath)
        Write-host "DnnPackager: Waiting.."  
        $task.Wait()
        Write-host "DnnPackager: Finished."  
        $Project.Save()            
    }
    catch [system.exception]
    {
        Write-host "DnnPackager: Exception String:  $($_.Exception.Message)" 
    }   
    finally
    {
        #if($releaser -ne $null)
        #{
        #    $forDispose = $releaser -as [System.IDisposable]
        #    $forDispose.Dispose()
        #}              
    }          
}
else
{
    $msBuildProjects = [Microsoft.Build.Evaluation.ProjectCollection]::GlobalProjectCollection
    $msBuildProject = $msBuildProjects.GetLoadedProjects($Project.FullName) | Select-Object -First 1      
        
    if($msBuildProject -eq $null)
    {    
        Write-host "DnnPackager: Waiting for $($Project.FullName) to be added to the global project collection.."	
        $action = {  
            Write-host "DnnPackager: New Project Loaded.."
            $projectAddedArgs = [Microsoft.Build.Evaluation.ProjectCollection.ProjectAddedToProjectCollectionEventArgs]::$EventArgs
            $rootElement = [Microsoft.Build.Construction.ProjectRootElement]::$projectAddedArgs.ProjectRootElement 
        
            Write-host "DnnPackager: New Project Full Path is: $($rootElement.FullPath)"
            if($rootElement.FullPath -eq $Project.FullName)
            {           
                Install-Imports $rootElement
            }             
        }	    
        register-objectEvent -inputObject $msBuildProjects -eventName "ProjectAdded" -action $action
    }
    else
    {
        $projectRoot = $msBuildProject.Xml;
        Install-Imports $projectRoot
    }  
}


#.GetLoadedProjects($Project.FullName)
# register-objectEvent -inputObject $projects -eventName "EventArrived" -action $action
#$DnnPackagerExeShortFileName = 'DnnPackager.exe'
#$DnnPackagerExeFile = $ToolsPath | Join-Path -ChildPath $DnnPackagerExeShortFileName
#Write-Host "Executing install-targets --projectfilepath $($Project.FullName) --toolspath $ToolsPath"
#& $DnnPackagerExeFile "install-targets" "--projectfilepath" $Project.FullName "--toolspath" $ToolsPath | Write-Host

# function Add-SolutionFolder {
#    param(
#       [string]$Name
#    )
#    $solution2 = Get-Interface $dte.Solution ([EnvDTE80.Solution2])
#    $solution2.AddSolutionFolder($Name)
#}
 
#function Get-SolutionFolder {
#    param (
#        [string]$Name
#    )
#	$solution2 = Get-Interface $dte.Solution ([EnvDTE80.Solution2])
#    $solution2.Projects | ?{ $_.Kind -eq [EnvDTE80.ProjectKinds]::vsProjectKindSolutionFolder -and $_.Name -eq $Name }
#}

# # Ensure solution packaging folder exists.
# $SolutionPackagingFolderName = "Solution Items"
 
# Write-host "DnnPackager: Getting solution folder $SolutionPackagingFolderName"

# $SolutionPackagingFolder = Get-SolutionFolder $SolutionPackagingFolderName

# if($SolutionPackagingFolder -eq $null)
#	{
#	    Write-host "DnnPackager: Creating solution folder $SolutionPackagingFolderName because $($SolutionPackagingFolder -eq $null)"
#		$SolutionPackagingFolder = Add-SolutionFolder $SolutionPackagingFolderName
#	}

## Add a solution nuspec file to the solution.

#$solution = Get-Interface $dte.Solution ([EnvDTE80.Solution2])
#$solutionFolderPath =  Split-Path $solution.FullName -parent

#$DestinationSolutionNuspecFileName = "Solution.nuspec"
#$SourceSolutionNuspecFileName = "Solution.nuspecc"
#$ToolsSolutionNuspecPath = $ToolsPath | Join-Path -ChildPath $SourceSolutionNuspecFileName
#$DestinationSolutionNuspecFilePath = $solutionFolderPath | Join-Path -ChildPath $DestinationSolutionNuspecFileName

#Write-host "DnnPackager: Saving '$ToolsSolutionNuspecPath' to '$DestinationSolutionNuspecFilePath'." 

#$xml = New-Object XML
#$xml.Load("$ToolsSolutionNuspecPath")
## Can manipulate file here if necessary.

## set package id based on solution name.
#$solutionFileName = [System.IO.Path]::GetFileNameWithoutExtension($solution.FullName);
#$packageIdName = $solutionFileName -replace " ", ""

#$xml.package.metadata.id = "DotNetNuke.SolutionPackages.$packageIdName"
#$xml.package.metadata.title = "$solutionFileName DotNetNuke Solution Packages"
#$xml.package.metadata.description = "Contains the $solutionFileName solution packages"

#Write-host "DnnPackager: Saving '$DestinationSolutionNuspecFilePath'." 
#$xml.Save("$DestinationSolutionNuspecFilePath")

## Add the solution nuspec file to the solution
#Write-host "DnnPackager: Adding solution nuspec to solution." 

#$projectItems = Get-Interface $SolutionPackagingFolder.ProjectItems ([EnvDTE.ProjectItems])
#$projectItems.AddFromFile("$DestinationSolutionNuspecFilePath")





  
  

