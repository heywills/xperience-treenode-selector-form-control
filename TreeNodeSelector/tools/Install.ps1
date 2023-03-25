# Runs every time a package is installed in a project
# Credits:
# https://www.rhyous.com/2015/05/14/setting-file-properties-in-a-nuget-package-build-action-copy-to-output-directory-custom-tool/
# https://www.paraesthesia.com/archive/2013/05/15/setting-dependentupon-file-properties-on-nuget-package-install.aspx/
 
param($installPath, $toolsPath, $package, $project)
 
# $installPath is the path to the folder where the package is installed.
# $toolsPath is the path to the tools directory in the folder where the package is installed.
# $package is a reference to the package object.
# $project is a reference to the project the package was installed to.
 
function SetFilePropertiesRecursively
{
    param([__ComObject]$projectItem)
    $folderKind = "{6BB5F8EF-4483-11D3-8BCF-00C04F8EC28C}";
    foreach ($subItem in $projectItem.ProjectItems)
    {
        if ($subItem.Kind -ne $folderKind)
        {
            SetFileProperties $subItem
            SetItemDependentUponRelationship $subItem $projectItem
        }
        # Folders and non-folders can have child items.
        SetFilePropertiesRecursively $subItem
    }
}
 
function SetFileProperties
{
    param([__ComObject]$item)
    $buildAction = 0
    $message = ''
    Write-Host $item.Name
    switch -Wildcard ( $item.Name )
    {
        '*.cs'
        {
            $message = "  Setting Build Action to Compile"
            $buildAction = 1
            Break
        }
        '*.as?x'
        {
            $message = "  Setting Build Action to Content"
            $buildAction = 2
            Break
        }
        '*.xml'
        {
            $message = "  Setting Build Action to Content"
            $buildAction = 2
            Break
        }
        default
        {
            Write-Host "  Not setting Build Action"
            return
        }
    }
    Write-Host $message
    $item.Properties.Item("BuildAction").Value = $buildAction
}

function SetItemDependentUponRelationship
{
    param([__ComObject]$projectItem, [__ComObject]$parentProjectItem)
    # Set project dependencies
    $fileName = $projectItem.Name
    $targetParentFileName = ""
    switch -Wildcard ( $fileName )
    {
        '*.designer.cs'
        {
            $targetParentFileName = $fileName.Remove($fileName.Length - ".designer.cs".Length)
            Break
        }
        '*.as?x.cs'
        {
            $targetParentFileName = $fileName.Remove($fileName.Length - ".cs".Length)
            Break
        }
        default
        {
            Write-Host "  File is not dependent"
            return
        }
    }
    # Check current parent item
    if($parentProjectItem.Name -eq $targetParentFileName)
    {
        Write-Host "  The file $($fileName) is already dependent upon $($targetParentFileName)"
        return
    }
    # Get desired target
    $targetParentProjectItem = $parentProjectItem.ProjectItems.Item("$targetParentFileName")
    if($null -eq $targetParentProjectItem)
    {
        Write-Host "  Could not find the dependency parent $($targetParentFileName) for $($fileName)"
    }
    Write-Host "  Making $($fileName) dependent upon $($targetParentFileName)"
    $newProjectItem = $targetParentProjectItem.ProjectItems.AddFromFile($projectItem.Properties.Item("FullPath").Value)
    SetFileProperties $newProjectItem
}

# .Properties.Item("FullPath").Value
 
$cmsFormControls = $project.ProjectItems.Item("CMSFormControls")
$organizationFolder = $cmsFormControls.ProjectItems.Item("BlueModus")
$thisFormControlFolder = $organizationFolder.ProjectItems.Item("RelatedContentSelector")
SetFilePropertiesRecursively $thisFormControlFolder
