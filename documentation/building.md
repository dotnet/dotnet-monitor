# Clone, build and test the repo
------------------------------

To clone, build and test the repo on Windows:

```cmd
cd $HOME
git clone https://github.com/dotnet/dotnet-monitor
cd dotnet-monitor
.\Build.cmd
.\Test.cmd
```


On Linux and macOS:

```bash
cd $HOME
git clone https://github.com/dotnet/dotnet-monitor
cd dotnet-monitor
./build.sh
./test.sh
```

If you prefer to use *Visual Studio*, *Visual Studio Code*, or *Visual Studio for Mac*, you can open the `dotnet monitor` solution at the root of the repo.

# Updating native build support

Part of the dotnet/runtime repo has been copied into this repo in order to facilitate building of native code. When needing to update the native build support, take a look at [runtime-version.txt](../src/external/runtime-version.txt) for what files should be synchronized from the dotnet/runtime repo. Synchronizing these files is currently done as a manual process. Update the version file with the new commit and file information if a new synchronization occurs.
