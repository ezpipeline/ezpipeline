You need .net 6 to run the tool.

Create a tool manifest if you don't have it yet:
```
dotnet new tool-manifest
```

Then install the tool:
```
dotnet tool install ezpipeline
```
You can invoke the tool from this directory using the following commands:
```
dotnet tool run ezpipeline
```
or
```
dotnet ezpipeline
```

For example:
```
dotnet ezpipeline --help
Description:

Usage:
  ezpipeline [command] [options]

Options:
  --version       Show version information
  -?, -h, --help  Show help and usage information

Commands:
  cpu-info                  Get CPU info
  fetch-tool                Fetch tool (ninja, cmake, etc.)
  notify-discord            Send a notification message via WebHook
  notify-telegram           Send a notification message via Telegram Bot
  patch-dllimport           Patch dllimport in an assembly bytecode
  untgz                     Unarchive content of .tar.gz file
  unzip                     Unarchive content of .zip file
  unzip-blob                Unarchive content of Azure Blob
  unzip-url                 Unarchive content of a web file
  vsenv                     Setup VisualStudio environment variables
  winsdkenv                 Setup WindowsSDK environment variables
  xcode-setbuildsystemtype  Patch XCode build system workspace property
  zip                       Archive folder into .zip file
  zip-to-blob               Archive folder into .zip Azure Blob
```

Running command may have effect on environment variables. In this case run the next command in a separate pipeline task.

This won't work:
```
-script: |
    dotnet ezpipeline fetch-tool -n Ninja -o tools --path
    ninja
```
as the task won't be able to find ninja in PATH. On other hand if you split this into two tasks it will be able to find it:
```
-script: dotnet ezpipeline fetch-tool -n Ninja -o tools --path
-script: ninja
```
