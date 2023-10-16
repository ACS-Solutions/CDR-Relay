# ACSSolutions.CDRRelay

A daemon to listen on TCP/IP for CDR records from a PBX and publish them to a SharePoint List. Tested with 3CX v18.

## Installation Overview

 1. Configure 3CX to send CDR records via TCP/IP
 1. Install dotnet SDK 7.0 and runtime 6.0 - this is slightly messy because as part of the installation process we use a tool that requires and SDK and dotnet 6.0 runtime to install a certificate
 1. Put the binaries somewhere on your server. I used /usr/local/cdr-relay/bin - but I'm not a Linux guy so that might not be the best place.
 1. Create a List in SharePoint with the required column anmes and types
 1. Create an "Application" in Azure AD with access to your SharePoint and add a self-signed certificate for App Authentication
 1. Install the certificate in the dotnet certificate store
 1. Edit the appsettings.json for your environment
 1. Test Run from the console
 1. Configure systemd to run CDRRelay as a background service
 1. Optional: create an Application Insights instance in Azure and enter it's connection string into the appsettings.json

If that seems like a lot of steps then I'm sorry, but I can't think of a way to shorten it.

These instructions are unfinished. If you need to know right now - email me.

## Contributing

Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change.

## License

[GPLv3](https://www.gnu.org/licenses/gpl-3.0.en.html)
