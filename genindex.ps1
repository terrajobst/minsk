$indexDir = "bin\index"
$toolsDir = "bin\indexTools"
$zipFile = "$toolsDir\indexTools.zip"
$url = "https://www.nuget.org/api/v2/package/SourceBrowser/1.0.25"
$toolExe = "$toolsDir\tools\HtmlGenerator.exe"
$sln = "src\minsk.sln"

mkdir $toolsDir | out-null
wget $url -outFile $zipFile | out-null
Expand-Archive $zipFile $toolsDir | out-null
& $toolExe $sln /out:$indexDir
