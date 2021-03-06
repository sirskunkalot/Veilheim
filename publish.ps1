param(
    [Parameter(Mandatory)]
    [ValidateSet('Debug','Release')]
    [System.String]$Target,
    
    [Parameter(Mandatory)]
    [System.String]$TargetPath,
    
    [Parameter(Mandatory)]
    [System.String]$TargetAssembly,

    [Parameter(Mandatory)]
    [System.String]$ValheimPath,

    [Parameter(Mandatory)]
    [System.String]$SolutionPath,

    [Parameter()]
    [switch]$DebugScript
)

function Create-BepInEx{
    param (
        [Parameter(Mandatory)]
        [System.IO.DirectoryInfo]$DistPath,

        [Parameter(Mandatory)]
        [ValidateSet('Windows','Unix','Local')]
        [System.String]$DistSystem
    )
    Write-Host "Creating BepInEx in $DistPath"

    # copy needed files for this target system
    Copy-Item -Path "$SolutionPath\resources\$DistSystem\*" -Exclude 'BepInEx.cfg' -Destination "$DistPath" -Recurse -Force
    
    # create \BepInEx
    $bepinex = $DistPath.CreateSubdirectory('BepInEx')
    
    # create \BepInEx\config and copy config files
    $conf = $bepinex.CreateSubdirectory('config');
    Copy-Item -Path "$SolutionPath\resources\$DistSystem\*" -Include 'BepInEx.cfg' -Destination "$conf" -Force
    
    # create \BepInEx\core and copy core dlls from build
    $core = $bepinex.CreateSubdirectory('core');
    Copy-Item -Path "$TargetPath\*" -Filter 'BepInEx*.dll' -Destination "$core" -Force
    Copy-Item -Path "$TargetPath\*" -Filter '*Harmony*.dll' -Destination "$core" -Force
    Copy-Item -Path "$TargetPath\*" -Filter 'Mono.Cecil*.dll' -Destination "$core" -Force
    Copy-Item -Path "$TargetPath\*" -Filter 'MonoMod*.dll' -Destination "$core" -Force

    # create \BepInEx\plugins and copy plugin dlls from build
    $plug = $bepinex.CreateSubdirectory('plugins');
    Write-Host "Plugins: $TargetAssembly"
    Copy-Item -Path "$TargetPath\*" -Include $TargetAssembly.Split(',') -Destination "$plug" -Force

    # copy debug files when target system = Local
    if ($DistSystem.Equals("Local")) {
        foreach($asm in $TargetAssembly.Split(',')){
            $pdb = "$TargetPath\" + ($asm -Replace('.dll','.pdb'))
            if (Test-Path -Path "$pdb") {
                Write-Host "Copy Debug files for plugin $asm"
                Copy-Item -Path "$pdb" -Destination "$plug" -Force
                start "$SolutionPath\libraries\pdb2mdb.exe" "$plug\$asm"
            }
        }
    }

    # return basepath as DirectoryInfo
    return $base
}

function Copy-Corlib{
    param(
        [Parameter(Mandatory)]
        [System.IO.DirectoryInfo]$DistPath,
        
        [Parameter(Mandatory)]
        [System.IO.DirectoryInfo]$LibPath
    )
    Write-Host "Copying unstripped_corlib to $DistPath"

    $rel = $DistPath.CreateSubdirectory('unstripped_corlib')
    Copy-Item -Path "$LibPath\*" -Filter '*.dll' -Destination "$rel" -Force
}

function Copy-Config{
    param(
        [Parameter(Mandatory)]
        [System.IO.DirectoryInfo]$DistPath
    )
    Write-Host "Copying V+ config to $DistPath\BepInEx\config"

    Copy-Item -Path "$SolutionPath\*" -Include 'valheim_plus.cfg' -Destination "$DistPath\BepInEx\config" -Force
}

function Make-Archive{
    param(
        [Parameter(Mandatory)]
        [System.IO.DirectoryInfo]$DistPath
    )

    $rel = $DistPath.Parent.FullName
    $zip = $DistPath.Name + ".zip"
    
    Write-Host "Creating archive $zip for $DistPath"

    Compress-Archive -Path "$DistPath\*" -DestinationPath "$rel\$zip" -Force
}

$TargetPath = $TargetPath.Trim().TrimEnd('\');
$ValheimPath = $ValheimPath.Trim().TrimEnd('\');
$SolutionPath = $SolutionPath.Trim().TrimEnd('\');

Write-Host "Publishing for $Target from $TargetPath"

if ($DebugScript) {
    Write-Host "Just kidding, debugging myself"
    Write-Host ""
}

if ($Target.Equals("Debug")) {
    Write-Host "Updating local installation in $ValheimPath"

    $valheim = New-Item -ItemType Directory -Path "$ValheimPath" -Force
    Create-BepInEx -DistPath $valheim -DistSystem 'Local'
}

if ($Target.Equals("Release")) {
    $rel = New-Item -ItemType Directory -Path "$SolutionPath\release" -Force
    $lib = Get-Item -Path "$ValheimPath\unstripped_corlib"

    Write-Host "Building release packages to $rel"
    
    # create all distros as folders and zip
    ('Windows','Unix') | % {
        $dist = New-Item -ItemType Directory -Path "$rel\$_" -Force;
        Create-BepInEx -DistPath $dist -DistSystem $_
        Copy-Config -DistPath $dist
        Copy-Corlib -DistPath $dist -LibPath $lib
        Make-Archive -DistPath $dist
        $dist.Delete($true);
    }
}