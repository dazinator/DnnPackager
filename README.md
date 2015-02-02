# DnnPackager

##In Brief
DnnPackager is a NuGet package that aims to streamline DotNetNuke development.

1. When this Nuget package is added to a project within Visual Studio, it will:
    - Enhance the build of that project to automatically produce the DotNetNuke installation zip file on successful builds (containing all module content etc).
    - Automatically offers to add a `manifest.dnn` file, `licence.txt` file and a `ReleaseNotes.txt` file to the project.
    - Allows you to have build configuration specific manifest files, i.e a `manifest.[buildconfig].dnn` file. For example, you could add to your project a `manifest.debug.dnn`file which includes additional symbols (pdb) files. Now when you do a Debug build, your installation zip file will contain the debug version of the manifest instead of the default manifest file.

2. Adds powershell commands to the "Package Manager Console" in Visual Studio.
    - Deploy your module to a DotNetNuke website hosted on your local IIS with one simple command. Open the Package Manager Console, and with your project selected, type: `Install-Module yourdnnwebsitenamehere`. Then watch as your project is built, packaged, and deployed to your Dnn website. 

##Why not use a Dnn Project Template

Many people may choose to use a project template such as this in order to streamline development: https://christoctemplate.codeplex.com/

These templates may work for many people, I personally do not like these templates for a few reasons, the main one being that it places a requirement upon developers that they have to checkout and work with their code within a particular directory - i.e the "DestkopModules/modulename" folder within a local Dnn Webiste. I don't agree with this coupling between a project and it's location on disk. I prefer to keep projects agnostic of their physical location. They should "just work" no matter whrere a developer check's them out to. It can also lead to a process where developers only run the install / deployment process once, then proceed to make many changes to the code, adding files / dependencies etc, without ever re-testing the installation process. Later on, when the module is released, the installation issue becomes apparant (i.e forgetting to include a dll etc) during the actual deployment. I'd prefer to catch this earlier, during development itself, before it potentially goes out to a user or QA tester. Therefore I personally prefer to use the deployment process as the singlular means of deploying code changes that I make to my local Dnn website. This ensures the deployment process is allways working smoothly.

I also don't like the fact that most of the project templates are set up to include particular dependencies - for example Dnn7 dll's. I'd rather create a project, then be free to add the relevent NuGet packages for any dependencies I need, as opposed to having a project template for Dnn 5, another for Dnn 6, another for Dnn 7 etc etc. Perhaps this is just me.

Hence DnnPackager was born.


