# Kentico Xperience TreeNode Selector Form Control

This Kentico Xperience form control provides a tree-based selector, with configurable page type and path filters. 

## Purpose
Creating reusable, atomic content, and composing pages using content relationships is becoming increasingly popular with the advent of headless CMS and hybrid CMS platforms. In Kentico Xperience, this control provides an easy way to provide an authoring experience for composing content using content references.

## Features
### Tree-based browsing
Unlike out-of-the-box selectors, this selector provides a tree view so that authors can drill-down to the content they want to select.
![Tree-based selector](https://raw.githubusercontent.com/heywills/xperience-treenode-selector-form-control/main/docs/images/01-tree-selector.png)

### Single-selection
When the control's **Reference Limit** is set to **1**, the control can be used with a **Unique identifier (GUID)** or a **Text** field type and will operate in a single-select mode. The value of the field will be the NodeGUID of the selected item.
![Single-select mode](https://raw.githubusercontent.com/heywills/xperience-treenode-selector-form-control/main/docs/images/02-single-select.png)

### Multi-selection
When the control's **Reference Limit** is set to greater than **1**, the control can be used with a **Long text** field type and will operate in a multi-select mode. The field value will be a semi-colon delimited list of the selected NodeGUIDs.
![Multi-select mode](https://raw.githubusercontent.com/heywills/xperience-treenode-selector-form-control/main/docs/images/03-multi-select.png)

### Search support
The selector supports searching for content by name.
![Search support](https://raw.githubusercontent.com/heywills/xperience-treenode-selector-form-control/main/docs/images/04-search-support.png)

### Page type filtering
This control has a **Page Types** property that allows selecting the page types that are allowed in a field. For example, the selector could be configured to only allow Articles, Manufacturers, Coffee, and Landing pages (to use Dancing Goat examples).

In this screenshot, notice that only the configured page types can be selected.
![Page type filtering controls what content can be selected](https://raw.githubusercontent.com/heywills/xperience-treenode-selector-form-control/main/docs/images/05-page-type-filtering.png)

Here's an example of the **Page Types** property. You can populate it with a macro expression. For example, you could use a macro to return all the page types that inherit from a base type.
![Page types property](https://raw.githubusercontent.com/heywills/xperience-treenode-selector-form-control/main/docs/images/05a-page-type-filtering.png)

### Starting path filtering
The control's **Starting Path** configuration allows filtering the section of the tree that may be selected. Simply enter the **NodeAliasPath** to use as the root of the selector.
![Starting path filter](https://raw.githubusercontent.com/heywills/xperience-treenode-selector-form-control/main/docs/images/06-starting-path.png)

### Reference Limit configuration
When the control's **Reference Limit** is reached, the remaining nodes are disabled to prevent additional selection.
![Reference Limit](https://raw.githubusercontent.com/heywills/xperience-treenode-selector-form-control/main/docs/images/07-limit-config.png)

### Multi-culture support
The control will allow browsing the content tree even when the parent nodes are not translated into the current culture.
![Multi-culture support](https://raw.githubusercontent.com/heywills/xperience-treenode-selector-form-control/main/docs/images/08-multi-culture.png)

### Related content editing
The control allows authors to quickly edit related content. Clicking the **Edit** button, opens the related node's Content form for quick access to the related content.
![Related content editing](https://raw.githubusercontent.com/heywills/xperience-treenode-selector-form-control/main/docs/images/09-edit-content.png)

### Content order
The control allows controlling the order of the related content using drag-and-drop.
![Content ordering](https://raw.githubusercontent.com/heywills/xperience-treenode-selector-form-control/main/docs/images/10-content-order.png)

## Install
1. Add the NuGet package, [XperienceCommunity.TreeNodeSelectorFormControl](https://www.nuget.org/packages/XperienceCommunity.TreeNodeSelectorFormControl/) to your Kentico CMS project.
2. Add the package version of [Kentico.Xperience.Libraries](https://www.nuget.org/packages/Kentico.Xperience.Libraries/) that matches your Xperience hotfix version. This is **critical** to prevent the  13.0.0 version of the Xperience libraries from being used when compiling your project.
3. Build your CMS project.
4. The first time you start the application, the **XperienceCommunity.TreeNodeSelectorFormControl** module will create the Kentico **Form control** configuration and **Query** object to support this selector.

## Uninstall
1. Remove the package **XperienceCommunity.TreeNodeSelectorFormControl** from your CMS project.
2. Rebuild the CMS project.
3. Kentico will remove the **Module** and **Form control** configuration the next time the CMS application starts. The query ***cms.root.relatedcontentselectortree** will not be removed, because Kentico does not relate queries to modules, unless the module has a custom class and the query is added to it.

## Compatibility
* .NET 4.8 or higher for the admin app or MVC5 projects
* Kentico Xperience versions 13.0.0 or higher

## License
This project uses a standard MIT license which can be found here.

## Contribution
Contributions are welcome. Feel free to submit pull requests to the repo.

## Support
Please report bugs as issues in this GitHub repo. We'll respond as soon as possible.

https://github.com/heywills/xperience-treenode-selector-form-control/issues









