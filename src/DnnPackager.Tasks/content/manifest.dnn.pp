<?xml version="1.0" encoding="utf-8"?>
<dotnetnuke type="Package" version="6.0">
  <packages>
    <!--        
      
    For guidance regarding the format of this manifest file, please see http://www.dnnsoftware.com/wiki/Page/Manifests
  
    -->
    <package name="$rootnamespace$" type="Module" version="0.0.0">
      <friendlyName>$rootnamespace$</friendlyName>
      <description></description>
      <owner>
        <name>[OwnerName]</name>
        <organization>$registeredorganization$</organization>
        <url>http://www.someurl.com</url>
        <email><![CDATA[<a href="mailto:support@someorg.com">support@someorg.com</a>]]></email>
      </owner>
      <license src="License.lic">
      </license>
      <releaseNotes src="ReleaseNotes.txt">
      </releaseNotes>
      <dependencies>
        <!--Example dependencies-->
        <!--<dependency type="CoreVersion">05.04.01</dependency>-->
        <!--<dependency type="package">SomeOtherPackage</dependency>-->
      </dependencies>
      <components>
        <component type="Module">
          <desktopModule>
            <moduleName>$rootnamespace$</moduleName>
            <foldername>$rootnamespace$</foldername>
            <businessControllerClass />
            <supportedFeatures />
            <moduleDefinitions>
              <moduleDefinition>
                <friendlyName>[Friendly Module Name]</friendlyName>
                <defaultCacheTime>60</defaultCacheTime>
                <moduleControls>
                  <moduleControl>
                    <controlKey>
                    </controlKey>
                    <controlSrc>[YourControllerOrPathToView]/[YourViewFileName].[YourViewFileExtension]</controlSrc>
                    <supportsPartialRendering>False</supportsPartialRendering>
                    <controlTitle>[Default title when added to page]</controlTitle>
                    <controlType>View</controlType>
                    <helpUrl>
                    </helpUrl>
                  </moduleControl>
                  <moduleControl>
                    <controlKey>settings</controlKey>
                    <controlSrc>[YourControllerOrPathToSettings]/[YourSettingsFileName].[YourSettingsFileExtension]</controlSrc>
                    <supportsPartialRendering>False</supportsPartialRendering>
                    <controlTitle>[Default settings title]</controlTitle>
                    <controlType>View</controlType>
                    <helpUrl>
                    </helpUrl>
                  </moduleControl>
                </moduleControls>
                <permissions>
                </permissions>
              </moduleDefinition>
            </moduleDefinitions>
          </desktopModule>
        </component>
        <component type="Assembly">
          <assemblies>
            <assembly>
              <path>bin</path>
              <name>$assemblyname$.dll</name>
            </assembly>
          </assemblies>
        </component>
        <component type="ResourceFile">
          <resourceFiles>
            <basePath>DesktopModules/$rootnamespace$</basePath>
            <resourceFile>
              <name>Resources.zip</name>
            </resourceFile>
          </resourceFiles>
        </component>
      </components>
    </package>

    <!--example skin-->
    <!--<package name="MySkin" type="Skin" version="5.0.6">
      <friendlyName>My Fantastic Skin</friendlyName>
      <description>My Fantastic Skin</description>
      <owner>
        <name>Joe Bloggs</name>
        <organization>Some Org</organization>
        <url>http://www.someurl.com</url>
        <email><![CDATA[<a href="mailto:joebloggs@someorg.com">joebloggs@someorg.com</a>]]></email>
      </owner>
      <license src="License.lic"></license>
      <releaseNotes></releaseNotes>
      <components>
        <component type="Skin">
          <skinFiles>
            <skinName>MySkin</skinName>
            <basePath>Portals\_default\Skins\MySkin</basePath>
          </skinFiles>
        </component>
        <component type="Assembly">
          <assemblies>
            <assembly>
              <path>bin</path>
              <name>MySkin.dll</name>
            </assembly>
          </assemblies>
        </component>
        <component type="ResourceFile">
          <resourceFiles>
            <basePath>Portals\_default\Skins\MySkin</basePath>
            <resourceFile>
              <name>resources.zip</name>
            </resourceFile>
          </resourceFiles>
        </component>
      </components>
    </package>-->

  </packages>
</dotnetnuke>
