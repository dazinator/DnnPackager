## READ ME         

For latest information refer to the wiki: https://github.com/dazinator/DnnPackager/wiki          
========================================================================================                   
                                                                       
Build your project, and it will be packaged up into a DotNetNuke Installation Zip file - check your projects output directory for the install zip.
Update the "manifest.dnn" file in your project appropriately. I have put placeholders in square brackets as a guideline as to what needs to be set, but its entirely up to you.

## Deploying (and debugging) your module

Deploying to a local DNN website is easy:

	1. In VS, Open up the "Package Manager Console" window, and select your project from the projects dropdown.
	2. Type: Install-Module [name of your website] and hit enter.
	3. Watch as your module project is built, packaged up as a zip, and then the zip is deployed to your local Dnn website!

For example, if your Dnn website is named "Dnn7" in IIS, then you would run: 
Install-Module Dnn7	

Note: This will build and install the module for your active build configuration. You can override this by using:

Install-Module [name of your website] [Build Configuration Name]

e.g if your current active build configuration was debug, but you wanted to install the release build of your module, you could type: Install-Module Dnn7 Release

For debugging, you can automatically attach the debugger! Run:

Install-Module [name of your website] [Build Configuration Name] Attach

e.g: Install-Module Dnn7 Debug Attach

That will install your module, and then attach the debugger for you.

Note: To save time in future, you can hit "up" arrow key in Package Console Manager to get the last command you executed to save you having to type if every time. So typically you can just hit "up" and then hit enter key, and you will be debugging your module in no time.

## Controlling Installation package Content.

There are multiple ways.

If you simply want a file to be included in the installation zip, just ensure you set the BuildAction to "Content". 
This will result in it being included in the "resources.zip" file within the install zip, which by default get's extracted to your modules install folder when the package is installed.
 
A file named "DnnPackageBuilderOverrides.props" has been added to your project, which allows you to override the default packaging logic.
You can use this to include additional dll's, files, directly in your zip file. Please take a look at the contents of "DnnPackageBuilderOverrides.props" - it has commented out sections
that demonstrate this further.

I hope this helps you!

Darrell Tunnell (Dazinator)
http://darrelltunnell.net/