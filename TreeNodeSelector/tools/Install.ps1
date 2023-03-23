# Runs every time a package is installed in a project
# Credit: https://www.rhyous.com/2015/05/14/setting-file-properties-in-a-nuget-package-build-action-copy-to-output-directory-custom-tool/
 
param($installPath, $toolsPath, $package, $project)
 
# $installPath is the path to the folder where the package is installed.
# $toolsPath is the path to the tools directory in the folder where the package is installed.
# $package is a reference to the package object.
# $project is a reference to the project the package was installed to.
 
function SetFilePropertiesRecursively
{
    $folderKind = "{6BB5F8EF-4483-11D3-8BCF-00C04F8EC28C}";
    foreach ($subItem in $args[0].ProjectItems)
    {
        $path = $args[1]
        if ($subItem.Kind -eq $folderKind)
        {
            SetFilePropertiesRecursively $subItem ("{0}{1}{2}" -f $path, $args[0].Name, "\")
        }
        else
        {
            Write-Host -NoNewLine ("{0}{1}{2}" -f $path, $args[0].Name, "\")
            SetFileProperties $subItem
        }
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
        '*.aspx'
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
            Write-Information "  Skipping file"
            return
        }
    }
    Write-Host $message
    $item.Properties.Item("BuildAction").Value = $buildAction
}
 
$cmsFormControls = $project.ProjectItems.Item("CMSFormControls")
$thisModuleItem = $cmsFormControls.ProjectItems.Item("BlueModus")
SetFilePropertiesRecursively $thisModuleItem
