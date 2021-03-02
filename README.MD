# Developing

How to setup the development enviroment for this project.

1. Install [Visual Studio 2019](https://visualstudio.microsoft.com) and add the C# workload.
2. Download this package: https://mega.nz/file/hBM0BDBS#eAY26LfmSSVlIz2MZn0ox8WKspfKmeIssz7durp5SfM
3. Unpack into your Valheim root folder. You should now see a new folder called .\Valheim\unstripped_corlib.
4. [Compile](https://github.com/MrPurple6411/Bepinex-Tools) or [Download](https://mega.nz/file/oQxEjCJI#_XPXEjwLfv9zpcF2HRakYzepMwaUXflA9txxhx4tACA) Publicizers executable version.
5. Drag and drop all assembly_.dll files from .\Valheim\valheim_Data\Managed onto "AssemblyPublicizer.exe". You should get a new folder .\Valheim\valheim_Data\Managed\publicized_assemblies with all dragged dll files in it.
6. Checkout this repository using git. That should create a new folder Veilheim.
7. Create a new user project file "Veilheim.csproj.user" alongside the project file within .\Veilheim\Veilheim to define some properties local to you. Paste this snippet and configure the paths as they are present on your computer.
```xml
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="Current" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <!-- Needs to be your path to the base Valheim folder -->
    <ValheimDir>F:\Steam\steamapps\common\Valheim</ValheimDir>
  </PropertyGroup>
  <PropertyGroup Condition="'$(Configuration)|$(Platform)' == 'Debug|AnyCPU'">
    <!-- Runs Steam opening Valheim after building debug -->
    <StartAction>Program</StartAction>
    <StartProgram>C:\Program Files %28x86%29\Steam\steam.exe</StartProgram>
    <StartArguments>-applaunch 892970</StartArguments>
  </PropertyGroup>
</Project>
```
8. Open the Solution file .\Veilheim\Veilheim.sln. It should prompt you a message at the top that some NuGet-Packages are missing. Click "Restore" and restart Visual Studio when finished.

# Debugging with dnSpy

Thanks to mono and unity-mono being open source, we could patch and compile our own mono runtime and enable actual live debugging of the game and the mod itself with dnSpy.

1. Download [dnSpy-net-win64](https://github.com/dnSpy/dnSpy/releases) and extract the exe.
2. Load all assemblies from .\Valheim\unstripped_corlib into dnSpy (just drag&drop the folder onto it).
3. Load all assembly_* from .\Valheim\valheim_Data\Managed into dnSpy (*do not load the publicized ones, they will not be loaded into the process and therefore can not be debugged*).
4. Load Veilheim.dll from .\Valheim\BepInEx\plugins into dnSpy.
5. Copy resources\mono-2.0-bdwgc.dll from this repo into .\Valheim\MonoBleedingEdge\EmbedRuntime and overwrite the existing file.
6. Now go to `Debug` -> `Start Debugging` and select Unity debug engine. Select your valheim.exe as the executable and hit OK.
7. If you did set some breakpoints, the game will halt when it hits the breakpoint in memory and dnSpy will show you the objects in memory and much more useful stuff.

# Debugging with Visual Studio

Your own code can be debugged in source with Visual Studio itself. You cannot debug game disassemblies as with dnSpy, though.

1. Install Visual Studio Tools for Unity (can be done in VS via `Extensions` -> `Manage Extensions`)
2. Copy resources\mono-2.0-bdwgc.dll from this repo into .\Valheim\MonoBleedingEdge\EmbedRuntime and overwrite the existing file.
3. Build the project with target `Debug`. The publish.ps1 PowerShell script from this repo automatically generates a Veilheim.dll.mdb file after a Debug build, which is needed for Unity/mono debugging. It should be under .\Valheim\BepInEx\plugins after a successful build.
4. Start Valheim (either directly from Steam or hit F5 in Visual Studio when Steam is running)
5. Go to `Debug` -> `Attach Unity debugger`
6. Since the patched mono dll does not open the same port as Unity Dev builds, you have to click on `Input IP`. It should fill in your local IP automatically. you just have to change the port to `55555`and the debugger should connect.
