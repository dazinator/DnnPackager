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

These templates may work for many people, I personally do not like these templates for a few reasons, the main one being that it places a requirement upon develoers that they have to checkout and work with their code within a particular directory - i.e the "DestkopModules/modulename" folder within a local Dnn Webiste. I don't agree with this coupling between a project and a particular developers environment / setup. I prefer to keep prjects agnostic of environmental assumptions. They should "just work" no matter whrere you check them out to. It can also lead to a process where developers only run the install / deployment process once, then make changes to code after it's installed to the weebsite, without ever re-testing the installation process. Later on, when the module is released, the installation issue becomes apparant during the deployment. I'd prefer to catch this earlier during the development, before it potentially goes out to a user or QA tester. Therefore I personally prefer to deploy my module after I make changes, to ensure the deployment process is allways working smoothly.

I also don't like the fact that most of the project templates are set up to include particular dependencies - for example Dnn7 dll's. I'd rather create a project, then be free to add the relevent NuGet packages for any dependencies I need, based on what I want to target. 


