del *.nupkg
msbuild .\TreeNodeSelector.csproj /property:Configuration=Release -t:restore,build -p:RestorePackagesConfig=true
nuget pack TreeNodeSelector.csproj -Prop Configuration=Release
copy .\XperienceCommunity.TreeNodeSelectorFormControl.*.nupkg C:\_OfflineNugetSource\
@echo off
pause
