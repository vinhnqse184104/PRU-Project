$a='C:/Users/Admin/source/repos/PRU-Project/Assets/Chapter2VillageEnhancement'
$template=[IO.File]::ReadAllText("$a/Bamboo_WarmTimber.mat")
foreach($f in Get-ChildItem "$a/Foliage_*.mat"){
 $s=[IO.File]::ReadAllText($f.FullName)
 $tex=[regex]::Match($s,'- _AlbedoMap:\r?\n        m_Texture: (\{[^\r\n]+\})').Groups[1].Value
 if(!$tex -or $tex -eq '{fileID: 0}'){throw "Missing diffuse $($f.Name)"}
 $bark=$f.Name -match '0c7635408f4c1244e88fa5bd2f861e4a|efc2c165e91d26e40ae320bac6b5441f'
 $flower=$f.Name -match '1528b25a59b7e5a47b88be2911d4fe8c'
 $color=if($bark){'{r: 0.58, g: 0.46, b: 0.32, a: 1}'}elseif($flower){'{r: 0.9, g: 0.94, b: 0.82, a: 1}'}else{'{r: 0.46, g: 0.7, b: 0.32, a: 1}'}
 $n=$template.Replace('m_Name: Bamboo_WarmTimber','m_Name: '+$f.BaseName)
 $n=[regex]::Replace($n,'(- _BaseMap:\r?\n        m_Texture: )\{[^\r\n]+\}',{param($m)$m.Groups[1].Value+$tex})
 $n=[regex]::Replace($n,'(- _BaseColor: )\{[^\r\n]+\}',{param($m)$m.Groups[1].Value+$color})
 if(!$bark){$n=$n.Replace('m_ValidKeywords: []',"m_ValidKeywords:`n  - _ALPHATEST_ON").Replace('m_CustomRenderQueue: -1','m_CustomRenderQueue: 2450').Replace('RenderType: Opaque','RenderType: TransparentCutout').Replace('_AlphaClip: 0','_AlphaClip: 1').Replace('_Cull: 2','_Cull: 0').Replace('_Cutoff: 0.5','_Cutoff: 0.4')}
 [IO.File]::WriteAllText($f.FullName,$n,[Text.UTF8Encoding]::new($false))
}
