Clear-Host

$source = 'C:\prg\myProjects\GitHub\RxSockets\RxSockets\bin\Release'
$dest   = 'C:\prg\Nuget\repo'
$file   = "RxSockets.3.0.0-alpha.nupkg"
Copy-Item "$source\$file" -Destination "$dest"

$cache   = 'C:\Users\david\.nuget\packages\rxsockets'
Remove-Item -Path "$cache" -Recurse

Write-Output "Complete!"
