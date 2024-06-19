del *.nupkg
msbuild .\TreeNodeSelector.csproj /property:Configuration=Release 
nuget pack TreeNodeSelector.csproj -Prop Configuration=Release
copy .\XperienceCommunity.TreeNodeSelectorFormControl.*.nupkg C:\_OfflineNugetSource\
@echo off
pause
