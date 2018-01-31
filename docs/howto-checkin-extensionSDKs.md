
HowTo: Check in ExtensionSDKs
======

Follow the instructions at http://www.unravelingcode.com/how-to-use-extension-sdks-without-installing-them-in-visual-studio/

To sum it up:

1. Install the VSIX/msi of the extension SDK that you need
2. Go to c:\program files(x86)\Microsoft SDKs\
3. Copy the folder structure of whatever dependency you have into your repo/Libs/ExtensionSDKs folder
4. Git add the files
5. Modify the csproj to include the following just after the last `<Import>` line


`<PropertyGroup>
    <SDKReferenceDirectoryRoot>$(SolutionDir)Libs\ExtensionSDKs;$(SDKReferenceDirectoryRoot)</SDKReferenceDirectoryRoot>
  </PropertyGroup>`


:\Program Files (x86)\Microsoft SDKs\Windows\v8.1\ExtensionSDKs\SQLCipher.WinRT81\3.3.1\
#### Example
The SQLite ExtensionSDK is installed to c:\program files(x86)\UAP\v0.8.0.0\ExtensionSDKs\SQLCipher.UAP.2015, so we make a new folder under /Libs/ExtensionSDKs called UAP/v0.8.0.0/ExtensionSDKs/SQLCipher.UAP.2015 and copy all the contents to that folder

See [this commit](https://git.soma.salesforce.com/Windows/S1Lite/commit/5ac2616eccc2445b75fb71cb9abedbb79ea4e27c) as an example