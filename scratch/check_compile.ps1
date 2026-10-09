$unityEngineDir = "C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Data\Managed"
$unityEngineModules = "C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Data\Managed\UnityEngine"
$packageCache = "c:\Users\Admin\PRU-Project\Library\ScriptAssemblies"

$csFiles = Get-ChildItem -Path "Assets" -Recurse -Filter "*.cs" | ForEach-Object { $_.FullName }

$refDlls = @()
if (Test-Path "$packageCache\Assembly-CSharp.dll") { $refDlls += "$packageCache\Assembly-CSharp.dll" }
if (Test-Path "$packageCache\Unity.InputSystem.dll") { $refDlls += "$packageCache\Unity.InputSystem.dll" }
if (Test-Path "$packageCache\Unity.TextMeshPro.dll") { $refDlls += "$packageCache\Unity.TextMeshPro.dll" }

Get-ChildItem -Path $unityEngineDir -Filter "*.dll" | ForEach-Object { $refDlls += $_.FullName }
Get-ChildItem -Path $unityEngineModules -Filter "*.dll" | ForEach-Object { $refDlls += $_.FullName }

Write-Host "Found $($csFiles.Count) C# files in Assets."
Write-Host "Checking compilation using Unity Editor Log..."
