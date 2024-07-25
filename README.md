# ACSSolutions.CDRRelay

A daemon to listen on TCP/IP for CDR records from a PBX and publish them to a SharePoint List.

Tested with 3CX v18 and v20 running on a 3CX Linux server running in AWS EC2.

## Installation Overview

 

1. Create a List in SharePoint with the required column names and types.
	![Screenshot of list columns in SharePoint Online](Assets/SharePoint%20List%20Configuration.png)
1. On your server, install the dotnet SDK (which includes the runtime) - see https://learn.microsoft.com/en-gb/dotnet/core/install/linux-debian#install-the-sdk
1. Create a self-signed certificate in the dotnet certificate store. There are many ways; this way is mine.
	You already have the Microsoft package sources configured and the dotnet framework installed, so lets use it.
	1. Install the cross-platform PowerShell as a global tool - see https://learn.microsoft.com/en-gb/powershell/scripting/install/install-other-linux#install-as-a-net-global-tool
	1. Since you just installed the .NET SDK, you will need to logout or restart your session before running the tool you installed.
	1. Run powershel (pwsh) and install the PnP.Powershell module:
	```
	admin@etc:~$ pwsh
	PowerShell 7.4.4
	PS /home/admin> Install-Module PnP.PowerShell -Scope CurrentUser

	Untrusted repository
	You are installing the modules from an untrusted repository. If you trust this
	repository, change its InstallationPolicy value by running the Set-PSRepository
	 cmdlet. Are you sure you want to install the modules from 'PSGallery'?
	[Y] Yes  [A] Yes to All  [N] No  [L] No to All  [S] Suspend  [?] Help
	(default is "N"):A
	PS /home/admin>
	```
	1. Finally, we can create the certificate. Don't worry about the error, it still seems to work.
	```
	PS /home/admin> New-PnPAzureCertificate -CommonName "ACSSolutions.CRDRelay" -OutPfx pnp.pfx -OutCert pnp.cer -ValidYears 5
	New-PnPAzureCertificate: Object reference not set to an instance of an object.
	PS /home/admin> ls -ll pnp.*
	-rw-r--r-- 1 admin admin     773 Jul 24 23:34 pnp.cer
	-rw-r--r-- 1 admin admin    2367 Jul 24 23:34 pnp.pfx
	```
	1. Then get a tool that can install certificates in the right place to get loaded in .net
	```
	PS /home/admin> dotnet tool install --global dotnet-certificate-tool
	You can invoke the tool using the following command: certificate-tool
	Tool 'dotnet-certificate-tool' (version '2.0.9') was successfully installed.
	```
	1. And use that tool to install the certificate in the local user's store
	```
	PS /home/admin> certificate-tool add --file ./pnp.pfx
	Installing certificate from './pnp.pfx' to 'My' certificate store (location: CurrentUser)...
	Done.
	```
	1. Finally snag the certificate file: pnp.cer onto your personal computer. That's going to Azure, but you'll want to open it and copy the Thumbprint. You can also get it on the linux box if you don't mind editing out the colons:
	```
	PS /home/admin> openssl x509 -in pnp.cer -fingerprint -sha1
	sha1 Fingerprint=EB:7F:BD:8C:ED:1D:CE:C7:03:27:1B:8D:A4:60:55:4D:56:0E:59:E1
	-----BEGIN CERTIFICATE-----
	```
1. Create an "App Registration" in Microsoft Entra ID at https://portal.azure.com/#view/Microsoft_AAD_RegisteredApps/CreateApplicationBlade
	1. Enter a name like "ACSSolutions.CDRRelay" and typically select "Accounts in this organizational directory only". 
	1. Select access for "Accounts in this organizational directory only (Xyxxy only - Single tenant)
	1. I don't think a Redirect URI is required.
	1. Click Register
	1. Go to the Manage, Certificates and Secrets section and select Certificates
	![Screenshot of the Azure Portal, ready to upload the certificate file](Assets/Microsoft%20Entra%20ID%20-%20Uploading%20the%20Certificate.png)
	1. Click the upload button and upload the pnp.cer file with a suitable description.
1. Grant the new App Registration full access to your SharePoint. This is over-permissioning but I don't know how to restrict it to specific sites or lists.
	1. Go to the Manage, API Permissions section and create & grant the permissions in the screenshot below:
	![Screenshot showing API Permissions completed](Assets/App%20Registration%20API%20Permissions.png)
1. Get the CDR Relay app binaries. Either of the these will work:
	1. Snag the binaries from the [releases on GitHub](https://github.com/ACS-Solutions/CDR-Relay/releases), or
	1. git clone the code from https://github.com/ACS-Solutions/CDR-Relay.git, and publish it in Visual Studio 2022 using the provided profile to a local folder
1. Put the binaries somewhere on your server. I used /usr/local/cdr-relay/bin - but I'm not a Linux guy so that might not be the best place.
	1. I ziped them and uploaded the zip file
	1. I installed unzip: ```$ sudo apt install unzip``` and unzipped them ```$ sudo unzip publish.zip```
1. Configure the appsettings.json for your environment.
	1. You'll need an editor. Nano is the simplest and almost universally available. 
	1. Note that the configuration system is designed to let you provide an override file in ../config. I do not recommend editing the appsettings.json file in the bin directory.
	1. Create a config folder as a sibling of the bin folder and create a new appsettings.json containing the following, but enter your deets:
	```json
	{
		"Settings:ListName": "<The name of the list you created in your SharePoint site>",
		"Settings:Port": 12345,
		"PnPCore:Credentials:Configurations:certificate:ClientId": "<From the Essentials part of the Overview section on the App Registration>",
		"PnPCore:Credentials:Configurations:certificate:TenantId": "<From the Essentials part of the Overview section on the App Registration>",
		"PnPCore:Credentials:Configurations:certificate:x509certificate:Thumbprint": "<From the pnp.cer file we created earlier>",
		"PnPCore:Sites:site:SiteUrl": "e.g. https://yourtenant.sharepoint.com/sites/yoursite",
		"ApplicationInsights:InstrumentationKey": "<From the overview section of your Application Insights instance>",
		"ApplicationInsights:ConnectionString": "<From the overview section of your Application Insights instance>"
	}
	```
1. Configure 3CX to send CDR records via TCP/IP
![Screenshot of 3CX v20 CDR configuration screen](Assets/3CX%20CDR%20Setup.png)
1. Test Run from the console
	```
	admin@etc:/usr/local/cdr-relay/bin$ /usr/bin/dotnet ACSSolutions.CDRRelay.dll
	[01:49:13 INF] So it begins...
	[01:49:13 DBG] Enter
	[01:49:13 DBG] Exit
	[01:49:13 DBG] Enter
	[01:49:13 DBG] Exit
	[01:49:13 DBG] Enter
	[01:49:13 DBG] Exit
	[01:49:13 DBG] Adding Systemd Support
	[01:49:13 DBG] Added Systemd Support
	[01:49:13 DBG] Host Building
	[01:49:13 DBG] Enter: ConfigureConfiguration:Callback
	[01:49:13 DBG] Exit: ConfigureConfiguration:Callback
	[01:49:13 DBG] Enter: ConfigureServices
	[01:49:13 DBG] Exit: ConfigureServices
	[01:49:13 DBG] Enter: ConfigureLogging:UseSerilog
	[01:49:13 DBG] Exit: ConfigureLogging:UseSerilog
	[2024-07-25 01:49:13.570 Debug ] .: Host Built
	[2024-07-25 01:49:13.625 Debug ] .: Settings Valid
	[2024-07-25 01:49:13.661 Information ] Microsoft.Hosting.Lifetime.: Application started. Press Ctrl+C to shut down.
	[2024-07-25 01:49:13.668 Information ] Microsoft.Hosting.Lifetime.: Hosting environment: "Production"
	[2024-07-25 01:49:13.670 Information ] Microsoft.Hosting.Lifetime.: Content root path: "/usr/local/cdr-relay/bin"
	[2024-07-25 01:49:16.450 Debug ] .: Client Connected on socket 186
	[2024-07-25 01:49:16.721 Information ] PnP.Core.Auth.OAuthAuthenticationProvider.: Initialized X509CertificateAuthenticationProvider with certificate with thumbprint "eb7fbd8ced1dcec703271b8da460554d560e59e1" from store My location CurrentUser
	[2024-07-25 01:49:16.774 Information ] PnP.Core.Auth.OAuthAuthenticationProvider.: Initialized X509CertificateAuthenticationProvider with certificate with thumbprint "eb7fbd8ced1dcec703271b8da460554d560e59e1" from store My location CurrentUser
	```
1. Make a test call and see you your CDR data arrives in SharePoint
1. Shut it down with Ctrl-C
1. Configure systemd to run CDRRelay as a background service
This is documented in the provided ```ACSSolutions.CDRRelay.service``` file.
1. Optional: create an Application Insights instance in Azure and enter its connection string into the appsettings.json

If that seems like a lot of steps then I'm sorry, but I can't think of a way to shorten it.

These instructions are imperfect. If you need support - email me.

## Contributing

Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change.

## License

[GPLv3](https://www.gnu.org/licenses/gpl-3.0.en.html)
