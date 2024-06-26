﻿<#
The script must be called by the project's post-build event like this:

powershell.exe -ExecutionPolicy Bypass -NoProfile -NonInteractive -File $(ProjectDir)\build\post-build.ps1 -projectDir "$(ProjectDir)\" -assemblyPath $(TargetPath) -moduleName [KenticoModuleName]

** ModuleName **
The module name is used to create deep paths in the Kentico file system, so
be cautions about using the full assembly name or full Nuget package name
as the module name. In a prior version, a long name created a path in
CMS projects like this:
src/packages/XperienceCommunity.TreeNodeSelectorFormControl.1.0.2/content/App_Data/CMSModules/XperienceCommunity.TreeNodeSelectorFormControl/Install/XperienceCommunity.TreeNodeSelectorFormControl_1.0.2.zip
It forced a 205 chars long path leaving only 51 for the parent path.

Instead, use a short unique name for the module, but make sure it's the same
name used for the ResourceInfo.ResourceName property of the Kentico module.

It creates two artifacts that will cause Kentico to track the installation
of the module, so that when the NuGet package is uninstalled it will automatically
delete the Kentico database objects related to the module.

If you don't care about uninstalling the module, these artifacts are not needed.

** Module meta file **
~\App_Data\CMSModules\CMSInstallation\Packages\[module name]_[version].xml

If Kentico sees this file. It will recognize that a NuGet package with 
a module was installed.
Kentico will copy it to ~\App_Data\CMSModules\CMSInstallation\Packages\Installed\.

If later, the file provided by the NuGet package is removed, Kentico will 
uninstall the module objects from the database, and then remove its Copy
of the module meta file.

** Module export file **
~\App_Data\CMSModules\[module name]\Install\[module name]_[version].zip
When Kentico finds the module meta file, it will look for this ZIP File
and import it. If the zip file does not exist, an exception will occur.
This project doesn't need to import a ZIP file, because the module code
creates the needed Kentico data objects, to prevent needing to use
the Kentico admin UI to create a package.
Therefore, this script copies an empty export package to the 
needed path, to prevent Kentico's exception.
#>

param(
	[Parameter(Mandatory=$true)] $projectDir,
	[Parameter(Mandatory=$true)] $assemblyPath,
	[Parameter(Mandatory=$true)] $moduleName
)

function GetModuleVersion($assemblyPath)
{
	$fileVersion = (Get-Command $assemblyPath).FileVersionInfo.FileVersion
	Write-Host ("    File version: {0}" -f $fileVersion)
	$versionObject = New-Object -TypeName System.Version -ArgumentList $fileVersion
	$moduleVersion = $versionObject.ToString(3)
	return $moduleVersion
}

function CreateCorrectlyNamedEmptyExport($projectDir, $moduleVersion, $moduleName)
{
	Write-Host "    Creating empty module export file"
    $projectDirTrimmed = TrimTrailingSlash $projectDir
	$templateExportPath = $projectDirTrimmed + "\build\EmptyExport.zip"
	$targetFolderPath = ("{0}\content\App_Data\CMSModules\{1}\Install" -f $projectDirTrimmed, $moduleName)
	$targetExportPath = ("{0}\{1}_{2}.zip" -f $targetFolderPath, $moduleName, $moduleVersion)
	if (Test-Path -Path $targetFolderPath) {
		Write-Host ("      Deleting contents of: {0}" -f $targetFolderPath)
		Remove-Item ("{0}\*.*" -f $targetFolderPath)
	}
    else
    {
        $newFolder = New-Item $targetFolderPath -ItemType Directory
    }
	Write-Host ("      Export template: {0}" -f $templateExportPath)
	Write-Host ("      Target export path: {0}" -f $targetExportPath)

	Copy-Item $templateExportPath $targetExportPath
}

function CreateModuleMetaFile($projectDir, $moduleVersion, $moduleName, $assemblyPath)
{
	Write-Host "    Creating module meta file"
    $binFolderPath = Split-Path $assemblyPath
    $cmsCoreAssemblyPath = ("{0}\CMS.Core.dll" -f $binFolderPath)
    Add-Type -Path $cmsCoreAssemblyPath
    $projectDirTrimmed = TrimTrailingSlash $projectDir
    $moduleMetaDataObject = New-Object -TypeName CMS.Core.ModuleInstallationMetaData
    $moduleMetaDataObject.Name = $moduleName
    $moduleMetaDataObject.Version = $moduleVersion
	$targetFolderPath = ("{0}\content\App_Data\CMSModules\CMSInstallation\Packages" -f $projectDirTrimmed, $moduleName)
	$targetMetaFilePath = ("{0}\{1}_{2}.xml" -f $targetFolderPath, $moduleName, $moduleVersion)
	if (Test-Path -Path $targetFolderPath) {
		Write-Host ("      Deleting contents of: {0}" -f $targetFolderPath)
		Remove-Item ("{0}\*.*" -f $targetFolderPath)
	}
    else
    {
        $newFolder = New-Item $targetFolderPath -ItemType Directory
    }
	Write-Host ("      Target meta file path: {0}" -f $targetMetaFilePath)


    $xmlSerializer = New-Object System.Xml.Serialization.XmlSerializer($moduleMetaDataObject.GetType())
    $xmlTextWriter = New-Object System.Xml.XmlTextWriter($targetMetaFilePath, $Null)
    $xmlTextWriter.Formatting = "Indented"
    $xmlSerializer.Serialize($xmlTextWriter, $moduleMetaDataObject)
    $xmlTextWriter.Close()
}

function TrimTrailingSlash($path)
{
    return $path.Trim(@("/","\"))
}

try {
	Write-Host "START: post-build.ps1 (Create artifacts for Kentico module package)"
	Write-Host ("    Project directory: {0}" -f $projectDir)
	Write-Host ("    Module name: {0}" -f $moduleName)
	Write-Host ("    Assembly path: {0}" -f $assemblyPath)
	$moduleVersion = GetModuleVersion $assemblyPath
	Write-Host ("    Module version: {0}" -f $moduleVersion)
	CreateCorrectlyNamedEmptyExport $projectDir $moduleVersion $moduleName
	CreateModuleMetaFile $projectDir $moduleVersion $moduleName $assemblyPath
	Write-Host ("COMPLETE: post-build.ps1")

	exit 0
}
catch {
    Write-Host $_
	exit 1
}