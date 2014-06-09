## Introduction #

TcmDevelopment exposes functionality which makes it easier to develop and debug a Tridion presentation website on the local workstation.

 * Load a specified Tridion configuration based on configuration, i.e. development vs staging
 * Ensure the Tridion VM parameters are set when automatically when debugging a website in either IIS Express or WebDevelopment webserver
 * Support virtual paths while working in WebDevelopment webserver
 * Support local file overrides in either IIS Express or Webdevelopment webserver
 * Support _Mock_ authentication, simulating user authentication for development purposes

### Download #

Pre-compiled binary versions available for download can be found here:
[TcmDevelopment on Google Drive](https://drive.google.com/folderview?id=0B7HbFVRJj_UnajBBbmJRWHFPLXc&usp=sharing)


### Configuration #

TcmDevelopment allows to easily load a Tridion JVM for broker and linking from its configuration file.
Assumed is that both that access to the content presentation database and Tridion published filesystem (if required) can be established from the development workstation.
Advice is to provide read-only accounts for both.

TcmDevelopment.dll is to be placed in the target website ~/bin directory.
TcmDevelopment.config can be placed in the website root or ~/config directory.

### Tridion Content Presentation JVM #

In the below configuration example the Tridion published path is exposed as a read-only shared folder for either the development or staging Tridion environments:

_Note: Folder mappings only need to be defined when using ASP.Net WebDevelopment webserver, if using IIS Express the remote folders can be configured as virtual directories in applicationHost.config._

    <?xml version="1.0" encoding="utf-8" ?>
    <!-- Tridion: Tridion home directory -->
    <!-- basePath: Local folder to check for content before retrieving remote content -->
    <!-- mode: Prefix for the tridion configuration files to load -->
    <tcmdevelopment tridion="D:\Workspace\Code\Tridion" basePath="D:\Workspace\Code\Tridion\www" mode="staging">
	        <!-- Development Mappings -->
	        <path mode="development" virtual="~/de" location="\\development\tridion\de" />
	        <path mode="development" virtual="~/nl" location="\\development\tridion\nl" />
          ....


The configured Tridion directory simply has to contain the required Tridion jar files and an empty cd_storage.config inside the config subfolder.

For example for the above sample configuration snippet, the physical folder layout would be as follows:

    D:\Workspace\Code\Tridion\lib\<tridion jar files>
    D:\Workspace\Code\Tridion\config\cd_storage.config
    D:\Workspace\Code\Tridion\staging\config\<staging configuration files>
    D:\Workspace\Code\Tridion\development\config\<development configuration files>

By switching the @mode parameter in the configuration file a different site of configuration files is loaded inside the Tridion JVM, allowing to switch easily between environments for debugging.

### File Overrides #

Additionally, the developer can edit content locally, easily overriding Tridion published content for quick testing purposes.
If a file is placed in the same relative path in "D:\Workspace\Code\Tridion\www" (as per configuration), it will be loaded before the Tridion published content on the share (or the IIS Express virtual folder).
For example an image in "D:\Workspace\Code\Tridion\www\english\images\header_tcm5-21145.jpg" will be served for any request with the url "http://localhost/english/images/header_tcm5_21145.jpg". All other requests will be served according to the configured mapping or IIS Express configuration in case of IIS Express.

### Mock Authentication #

As is quite common in an website application, some pages are for privileged users only. In order to ensure that these pages are never accidentally accessible, a good practice is to ensure the are blocked as part of the default web.config deployment.

A sample web.config section to a secure website section  could look like this:

    <location path="adminpanel">
	    <system.webServer>
		    <security>
			    <authorization>
				    <remove users="*" />
				    <add accessType="Allow" roles="administrators" />
			    </authorization>
		    </security>
	   </system.webServer>
	   <system.web>
		   <authorization>
			   <deny users="?" />
			   <allow roles="administrators" />
		   </authorization>
	   </system.web>
    </location>

However on development the user authentication information to connect to this section might not be available, or even unaccessible due to company security policies.
This is why TcmDevelopment supports a mock authentication module, which allows the user to locally authentication without any backing userstore.
Obviously this means the password is not checked and all passwords are accepted as valid.

This can be configured in IIS Express with the following configuration in applicationHost.config:

    <location path="Website">
	    <system.webServer>
		    <modules>
			    <!-- Mock authentication when working locally -->
			    <add name="MockAuthentication" type="TcmDevelopment.MockAuthentication, TcmDevelopment" />
		    </modules>
	    </system.webServer>
    </location>

### Deployment #

When using publishing profiles, the following configuration can be added in order not to include TcmDevelopment as part of the deployment:

    <PropertyGroup>
	    ....
	    <!-- Exclude development libraries from deployment -->
	    <ExcludeFilesFromDeployment>bin\TcmDevelopment.dll;config\tcmdevelopment.config</ExcludeFilesFromDeployment>
    </PropertyGroup>


### Technical #

On ASP.NET application pool startup TcmDevelopment automatically initializes itself using a [PreApplicationStartMethod](http://msdn.microsoft.com/en-us/library/system.web.preapplicationstartmethodattribute.aspx).

This allows the TcmDevelopment to initialize itself without additional configuration, simply by dropping the library into the ~/bin directory.

On initialization the Tridion JVM is configured using environment variables as documented on the [JuggerNET site](http://codemesh.com/products/juggernet/doc/runtime_config.html).

TcmDevelopment makes use of a [Virtual Path Provider](http://msdn.microsoft.com/en-us/library/system.web.hosting.virtualpathprovider(v=vs.110).aspx) in order to provide the file mapping overrides for both IIS Express and ASP.NET WebDevelopment server.

Additionally it uses some dark magic to replace the [IConfigMapPath](http://msdn.microsoft.com/en-us/library/system.web.configuration.iconfigmappath(v=vs.110).aspx) interface on the [HttpRuntime](http://msdn.microsoft.com/en-us/library/system.web.httpruntime(v=vs.110).aspx) module.
This allows it to capture and handle all MapPath calls such Server.MapPath and Page.MapPath and redirect these accordingly to the configured file locations.

[![githalytics.com alpha](https://cruel-carlota.pagodabox.com/cb439853d159fe1de33e3197f1caf6f7 "githalytics.com")](http://githalytics.com/github.com/TcmExtensions)
