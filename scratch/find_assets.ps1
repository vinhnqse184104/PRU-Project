$prefabs = Get-ChildItem -Path "Assets" -Recurse -Filter "*.prefab"
$torchPrefabs = $prefabs | Where-Object { $_.Name -like "*torch*" -or $_.Name -like "*fire*" -or $_.Name -like "*light*" }
$rockPrefabs = $prefabs | Where-Object { $_.Name -like "*rock*" -or $_.Name -like "*stone*" -or $_.Name -like "*wall*" -or $_.Name -like "*floor*" -or $_.Name -like "*cave*" -or $_.Name -like "*cliff*" }

Write-Host "=== TORCH PREFABS ==="
$torchPrefabs | Select-Object -First 30 | ForEach-Object { $_.FullName }

Write-Host "`n=== ROCK / WALL / FLOOR PREFABS ==="
$rockPrefabs | Select-Object -First 50 | ForEach-Object { $_.FullName }
