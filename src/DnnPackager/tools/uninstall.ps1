param (
    $InstallPath,
    $ToolsPath,
    $Package,
    $Project
)

Add-Type -AssemblyName 'Microsoft.Build, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'

function RemoveImport {
    param (
        $Project, [string]$ImportFileName
    )

    Try
    {
        $MSBProject = [Microsoft.Build.Evaluation.ProjectCollection]::GlobalProjectCollection.GetLoadedProjects($Project.FullName) | Select-Object -First 1

        $ExistingImports = $MSBProject.Xml.Imports | Where-Object { $_.Project -like "*\$ImportFileName" }
        if ($ExistingImports) 
        {
            $ExistingImports | ForEach-Object {
                $MSBProject.Xml.RemoveChild($_) | Out-Null
            }
		        $Project.Save()    
        }       
    }
    Catch [system.exception]
    {
        Write-host "DnnPackager: Exception String: $_.Exception.Message"         
    }
}


$TargetsFile = 'DnnPackager.Build.targets'
$PropsFile = 'DnnPackageBuilderOverrides.props'

RemoveImport $Project $TargetsFile
RemoveImport $Project $PropsFile
