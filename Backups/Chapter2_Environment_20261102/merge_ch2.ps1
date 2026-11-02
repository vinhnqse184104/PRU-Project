$p='C:/Users/Admin/source/repos/PRU-Project'
$b="$p/Backups/Chapter2_Environment_20261102"
$src=[IO.File]::ReadAllText("$b/Chapter2_LyThong.before.unity.txt").Replace("`r`n","`n")
$stage=[IO.File]::ReadAllText("$p/Assets/Chapter2VillageEnhancement/EnvironmentOnly.unity").Replace("`r`n","`n")
$base=[regex]::Matches($src,'(?m)^--- !u![\s\S]*?(?=^--- !u!|\z)')
$sb=[regex]::Matches($stage,'(?m)^--- !u![\s\S]*?(?=^--- !u!|\z)')
$ids=@{};$added=@();$render='';$roots=''
foreach($m in $sb){$k=[regex]::Match($m.Value,'--- !u!(\d+) &(-?\d+)');$type=[int]$k.Groups[1].Value;if($type -eq 104){$render=$m.Value};if($type -eq 1660057539){$roots=$m.Value};if($type -notin @(29,104,157,196,1660057539)){$ids[$k.Groups[2].Value]=[string]([long]880000000000000000+$added.Count);$added+=,$m.Value}}
$oldguid=[regex]::Match([IO.File]::ReadAllText("$p/Assets/TerrainData_b2421e0b-39b9-4f52-b5c8-c91b43d9cf78.asset.meta"),'guid: (\w+)').Groups[1].Value
$newguid=[regex]::Match([IO.File]::ReadAllText("$p/Assets/Chapter2VillageEnhancement/Chapter2_Terrain.asset.meta"),'guid: (\w+)').Groups[1].Value
$out=[Text.StringBuilder]::new();[void]$out.Append($src.Substring(0,$src.IndexOf('--- !u!')));$changes=@()
foreach($m in $base){$v=$m.Value;$k=[regex]::Match($v,'--- !u!(\d+) &(-?\d+)');$type=[int]$k.Groups[1].Value;$n=$v
if($type -eq 104){$n=$render;$sun=[regex]::Match($v,'(?m)^  m_Sun:.*$').Value;if($sun){$n=[regex]::Replace($n,'(?m)^  m_Sun:.*$',$sun)}}
if($type -in @(218,154)){$n=$n.Replace($oldguid,$newguid)}
if($type -eq 1660057539){$n=$n.TrimEnd()+"`n";foreach($r in [regex]::Matches($roots,'- \{fileID: (-?\d+)\}')){$n+='  - {fileID: '+$ids[$r.Groups[1].Value]+"}`n"}}
if($n -cne $v){if($type -notin @(104,218,154,1660057539)){throw 'Unexpected change'};$changes+=@{type=$type;id=$k.Groups[2].Value}}
[void]$out.Append($n)}
foreach($v in $added){$n=[regex]::Replace($v,'(?m)^(--- !u!\d+ &)(-?\d+)',{param($m) $m.Groups[1].Value+$ids[$m.Groups[2].Value]});$n=[regex]::Replace($n,'\{fileID: (-?\d+)\}',{param($m) $id=$m.Groups[1].Value;if($ids.ContainsKey($id)){'{fileID: '+$ids[$id]+'}'}else{$m.Value}});[void]$out.Append($n)}
[IO.File]::WriteAllText("$p/Assets/Scenes/Chapter2_LyThong.unity",$out.ToString(),[Text.UTF8Encoding]::new($false))
@{original_blocks=$base.Count;original_blocks_changed=$changes;added_blocks=$added.Count;original_gameplay_camera_transforms_identical=$true}|ConvertTo-Json -Depth 5|Set-Content "$b/scene_integrity.json"
Get-Content "$b/scene_integrity.json"
