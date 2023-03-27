namespace XperienceCommunity.TreeNodeSelectorFormControl.Constants
{
    public static class FormUserControlConstants
    {
        public const string ControlCodeName = "BlueModus-RelatedContentSelector"; // Required for backward compatibility
        public const string ControlDisplayName = "TreeNode Selector by Kentico Xperience Community";
        public const string ControlDescription = "This form control provides a tree-based selector, with configurable page type and path filters. It is installed by the Nuget package XperienceCommunity.TreeNodeSelectorFormControl.";
        public const string ControlFileName = "~/CMSFormControls/BlueModus/RelatedContentSelector/RelatedContentSelector.ascx";
        public const string ControlParameters =
@"<form version=""2"">
    <field allowempty=""true"" column=""PageTypes"" columnsize=""2000"" columntype=""text"" guid=""676973c3-5a26-4169-ae2d-08851c72bc65"" resolvedefaultvalue=""False"" visible=""true"">
        <properties>
            <fieldcaption>Page Types</fieldcaption>
        </properties>
        <settings>
            <AllowAll>False</AllowAll>
            <AllowEmpty>True</AllowEmpty>
            <controlname>selectclassnames</controlname>
            <HideInheritedClasses>False</HideInheritedClasses>
            <ReturnColumnName>ClassName</ReturnColumnName>
            <SelectionMode>3</SelectionMode>
            <ShowOnlyCoupled>False</ShowOnlyCoupled>
        </settings>
    </field>
    <field allowempty=""true"" column=""StartingPath"" columnsize=""2000"" columntype=""text"" guid=""208d7c77-0080-4e4b-8fc0-059d48d3df4b"" resolvedefaultvalue=""False"" visible=""true"">
        <properties>
            <fieldcaption>Starting Path</fieldcaption>
        </properties>
        <settings>
            <AutoCompleteEnableCaching>False</AutoCompleteEnableCaching>
            <AutoCompleteFirstRowSelected>False</AutoCompleteFirstRowSelected>
            <AutoCompleteShowOnlyCurrentWordInCompletionListItem>False</AutoCompleteShowOnlyCurrentWordInCompletionListItem>
            <controlname>TextBoxControl</controlname>
            <FilterMode>False</FilterMode>
            <Trim>False</Trim>
        </settings>
    </field>
    <field allowempty=""true"" column=""ReferenceLimit"" columntype=""integer"" guid=""a9a4e13d-838c-4649-bb89-a2d9a8111047"" resolvedefaultvalue=""False"" visible=""true"">
        <properties>
            <fieldcaption>Reference Limit</fieldcaption>
            <fielddescription>Max number of items that can be selected</fielddescription>
        </properties>
        <settings>
            <AutoCompleteEnableCaching>False</AutoCompleteEnableCaching>
            <AutoCompleteFirstRowSelected>False</AutoCompleteFirstRowSelected>
            <AutoCompleteShowOnlyCurrentWordInCompletionListItem>False</AutoCompleteShowOnlyCurrentWordInCompletionListItem>
            <controlname>TextBoxControl</controlname>
            <FilterMode>False</FilterMode>
            <Trim>False</Trim>
        </settings>
    </field>
</form>";
    }
}
