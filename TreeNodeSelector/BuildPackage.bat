del *.nupkg
dotnet build .\TreeNodeSelector.csproj -c Release
nuget pack TreeNodeSelector.csproj -Prop Configuration=Release
copy .\XperienceCommunity.TreeNodeSelectorFormControl.*.nupkg C:\_OfflineNugetSource\
@echo off
pause
