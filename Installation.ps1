Install-Module -Name PnP.PowerShell
Import-Module -Name PnP.PowerShell
$app = Register-PnPAzureADApp -Interactive -ApplicationName "ACSSolutions.CDRImporter" -Tenant <sensitive>.onmicrosoft.com -OutPath C:\temp -CertificatePassword (ConvertTo-SecureString -String "<Password>" -AsPlainText -Force) -GraphApplicationPermissions @( "Group.Read.All", "User.Read.All" ) -SharePointApplicationPermissions @( "Sites.ReadWrite.All", "User.Read.All" ) -Store CurrentUser
<#
C:\Users\alasdair> $app

Pfx file               : C:\temp\ACSSolutions.CDRImporter.pfx
Cer file               : C:\temp\ACSSolutions.CDRImporter.cer
AzureAppId/ClientId    : cfeda5...
Certificate Thumbprint : F335...
#>
