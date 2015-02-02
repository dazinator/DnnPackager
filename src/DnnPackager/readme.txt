READ ME                                      
                                                                       
## When You Build Your Project..

Your project will be packaged up into a DotNetNuke Installation Zip. Check your projects output directory for the zip file.

## What You Should Do Next.

    1. Update the "manifest.dnn" file in your project appropriately. 

## Deploying your module

You can deploy your module to a local DotNetNuke website (hosted on your local IIS) very easily.

	1. In VS, Open up the "Package Manager Console" window, and select your project from the projects dropdown.
	2. Type: Install-Module [name of your website] and hit enter.
	3. Watch as your module project is built, packaged up as a zip, and then the zip is deployed to your local Dnn website!

For example, if your Dnn website is named "Dnn7" in IIS, then you would run: 
Install-Module Dnn7	

## Customising Installation package Content.
A file named "DnnPackageBuilderOverrides.props" has been added to your project. 
This allows you to override the default packaging logic, for example, you could include additional files in your zip file etc.
Please take a look at the contents of "DnnPackageBuilderOverrides.props" - it has commented out sections that demonstrate properties that you can override
if you want to. It has some commented out examples showing how to include additional files, dll's etc etc.

# Advanced Usage --> Read On For Advanced Features
====================================================

## Solution Level Packaging

Each project in your solution that you add DnnPackager too, will have an installation zip file produced when you build the project.
By default those will appear in the standard output directory for the project - i.e bin\release etc.
However sometimes it's useful to be able to grab all the installation zip packages together.
DnnPackager will create a folder on disk, named "InstallPackages", alongside your .sln file. 
(You can change this if invoking MSBuild.exe, by passing in the msbuild property: /p:SolutionBuildPackagesFolder "yourfolderpath") 
and the zip files will be copied to that directory.
When MSBuild is invoked, the "InstallPackages" folder is cleared right at the beginning of the build, so that only modules that are built 
appear in that folder, and not the results from previous builds.

## Octopack Integration

DnnPackager supports OctoPack: https://github.com/OctopusDeploy/OctoPack

If you add OctoPack (available on NuGet) to your project, then you will find that the NuGet deployment packages that OctoPack produces:-

1. Will contain your module installation zip file.
2. Will have NuGet package metadata (Id, version number, etc) derived form your Dnn manifest file.  

This allows you to maintian the version information in one place - the dnn manifest file, and have OctoPack package up your module installation zips in 
nice NuGet deployment packages, ready for automated deployments via Octopus Deploy.

DnnPackager supports creating individual NuGet deployment packages per module / project, as well as creating a single NuGet Deployment package, 
containing all the module zips in the Solution.

The msbuild properties to enable / disable project level, and solution level nuget packaging are:

/p:CreateDeploymentNugetPackages=true    # when true, and when OctoPack is present, each module will have a NuGet deployment package containing it's zip.
/p:CreateSolutionDeploymentPackage=true  # when true, and when OctoPack is present, a single NuGet deployment package will be produced, containing all the module installation zip's in the solution.

You can then control which NuGet feeds those NuGet deployment packages get published to, by setting the following msbuild properties:

/p:OctoPackPublishPackageToHttp = "http://somefeed.com/api" # individual module NuGet deployment packages are pushed to this feed.
/p:OctoPackPublishApiKey = "some nuget api key" # This is the API key for that feed.

You can also control which NuGet feed the solution level nuget deployment package is pushed to, by setting the following msbuild properties:

/p:PushSolutionPackageTo = "http://someotherfeed.com/api" 
/p:PushSolutionPackagesApiKey = "some nuget api key" # This is the API key for that feed.

NuGet.exe is used in the process of packaging and pushing packages. 
By default, the path to Nuget.Exe is resolved to your NuGet packages directory, but if for
some reason you store NuGet.exe somewhere different, you can tell DnnPackager where NuGet.exe is, by setting the following msbuild property:

p:NuGetExeFilePath="C:/NuGet.exe" 

DotNetNuke does not support SemVer for it's extension version numbers, However, if you would like to use a Build Number for the 3rd digit (patch version) of
your Dnn Extensions, then you can pass in the build number by setting the following msbuild property:

p:BuildVersionNumber="12345" 

Any resultant module installation zips (and NuGet deployment packages), will then have the version number: Major.Minor.12345 - where Major and Minor are what
you have specified in the module's manifest file, and 12345 was the value passed in for the BuildVersionNumber msbuild property.

I hope this helps you!

Darrell Tunnell (Dazinator)
http://darrelltunnell.net/