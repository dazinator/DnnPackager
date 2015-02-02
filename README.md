# DnnPackager

##In Brief
DnnPackager is a NuGet package that aims to streamline DotNetNuke development.

1. When this Nuget package is added to a project within Visual Studio, it will:
    - Enhance the build of that project to automatically produce the DotNetNuke installation zip file on successful builds (containing all module content etc).
    - Automatically offers to add a `manifest.dnn` file, `licence.txt` file and a `ReleaseNotes.txt` file to the project.
    - Allows you to have build configuration specific manifest files, i.e a `manifest.[buildconfig].dnn` file. For example, you could add to your project a `manifest.debug.dnn`file which includes additional symbols (pdb) files. Now when you do a Debug build, your installation zip file will contain the debug version of the manifest instead of the default manifest file.

2. Adds powershell commands to the "Package Manager Console" in Visual Studio.
    - Deploy your module to a DotNetNuke website hosted on your local IIS with one simple command. Open the Package Manager Console, and with your project selected, type: `Install-Module yourdnnwebsitenamehere`. Then watch as your project is built, packaged, and deployed to your Dnn website.

## Why?

Many people use visual studio DotNetNuke project templates for their Dnn development. I have a number of issues with project templates - which I describe: https://github.com/dazinator/DnnPackager/wiki/Why-not-use-a-DotNetNuke-Project-Template!%3F

I have tried to eliminate the need for project templates, and instead take advantage of NuGet and the Package Manager Console to streamline Dnn development. This is the same principal that Entity Framework Code First uses.



