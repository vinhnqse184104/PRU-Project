$content = Get-Content 'Assets/Scenes/Chapter3_MieuChanTinh.unity'
$names = foreach ($line in $content) {
    if ($line -match 'm_Name:\s*(.+)') {
        $matches[1]
    }
}
$names | Select-Object -Unique | Out-File -FilePath 'scratch/scene_objects.txt' -Encoding utf8
Write-Host "Done writing scene objects"
