$indexDir = "bin\index"
$toolsDir = "bin\indexTools"
$zipFile = "$toolsDir\indexTools.zip"
$url = "https://github.com/KirillOsenkov/SourceBrowser/releases/download/v1.0.21/HtmlGenerator.zip"
$toolExe = "$toolsDir\HtmlGenerator\HtmlGenerator.exe"
$sln = "src\minsk.sln"

mkdir $toolsDir | out-null
wget $url -outFile $zipFile | out-null
Expand-Archive $zipFile $toolsDir | out-null
& $toolExe $sln /out:$indexDir