/*
    This page is provided by the Nuget package XperienceCommunity.TreeNodeSelectorFormControl.
    Changes to this file will be lost when this package is restored or updated.
*/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using CMS.Base.Web.UI;
using CMS.DataEngine;
using CMS.Helpers;
using CMS.Localization;
using CMS.SiteProvider;
using CMS.UIControls;

namespace CMSApp.CMSFormControls.BlueModus.RelatedContentSelector.TreeSelectorDialog
{
    [Title(ResourceString= "content.selectdialog")]
    public partial class TreeSelectorDialog : CMSModalPage, ICallbackEventHandler
    {
        #region "Variables and constants"

        private const char NODE_SEPARATOR = '|';
        private const int DEFAULT_SELECTION_LIMIT = 50;
        private const int MAX_NODES_TO_EXPAND_FOR_SEARCH_HITS = 3;
        private readonly string _notTranslatedTooltip;
        private string _valuesSeparator = ";";
        private string _callbackValues;
        private string _returnColumnName;
        private string _securityPurpose;
        private int _createdNodes;
        private int _nodesExpandedForSearchHits;
        private string _cultureCode;
        private int _siteId;
        private string[] _pageTypeList;
        private string _startPath;
        private int _limitSelectionTo;

        #endregion

        public TreeSelectorDialog()
        {
            _notTranslatedTooltip = this.GetString("tree.nottranslatednodetooltip");
        }

        #region Properties

        private bool UseSingleSelectMode => (_limitSelectionTo == 1);

        #endregion Properties


        #region "Page events"

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);

            LoadParameters();

            SetSaveJavascript("return US_Submit();");
            SetSaveResourceString("general.select");
        }


        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // Register JQuery
            ScriptHelper.RegisterDialogScript(Page);
            ScriptHelper.RegisterJQuery(Page);

            // If the page is not in a callback, delay initializing
            // the tree to PreRender so that search parameters applied
            // by postback events can be used.
            if(Page.IsCallback)
            {
                InitializeTreeMenu();
            }

            if (_limitSelectionTo > 1 && _limitSelectionTo < DEFAULT_SELECTION_LIMIT)
            {
                InstructionsPanel.Controls.Add(new Literal{ Text = $"<p>Select up to {_limitSelectionTo} items.</p>" });
            }
        }


        protected override void OnPreRender(EventArgs e)
        {
            base.OnPreRender(e);
            // Prepare tree menu
            InitializeTreeMenu();
            // Reload tree
            TreeControl.ReloadData();

            ScriptHelper.RegisterClientScriptBlock(this, typeof(string), "TreeSelectorScript", GetSelectorScript(), true);
            SearchButton.OnClientClick = "displayLoader(); return true;";
            RegisterStyles();

            if (_createdNodes == 0)
            {
                BodyPanel.Controls.Add(new Literal { Text = "There is no content available for this filter." });
            }
        }

        #endregion


        #region "Event handlers"

        protected TreeNode TreeControl_OnNodeCreated(DataRow itemData, TreeNode defaultNode)
        {
            defaultNode.Selected = false;
            if (itemData != null)
            {
                var treeNode = CMS.DocumentEngine.TreeNode.New(itemData);
                var hasCheckedChildren = ValidationHelper.GetInteger(itemData["ChildChecked"], 0) > 0;
                var nodeLevel = ValidationHelper.GetInteger(itemData["NodeLevel"], 0);
                var value = String.Equals(_returnColumnName, "NodeGUID", StringComparison.OrdinalIgnoreCase) ? treeNode.NodeGUID.ToString() : treeNode.NodeID.ToString();
                var displayName = ValidationHelper.GetString(itemData["DocumentName"], string.Empty);
                var typeIconClass = ValidationHelper.GetString(itemData["ClassIconClass"], string.Empty);
                var selectedNodeGuids = GetSelectedNodeGuids();
                var nodeIsSelectable = ValidationHelper.GetBoolean(itemData["IsSelectable"], false);
                var hasChildSearchHit = ValidationHelper.GetBoolean(itemData["HasChildSearchHit"], false);
                var isAvailableInCurrentCulture = ValidationHelper.GetBoolean(itemData["IsCurrentCulture"], false);
                var typeIcon = typeIconClass.IsNullOrWhiteSpace()
                        ? string.Empty
                        : $"<i aria-hidden='true' class='{typeIconClass} cms-icon-80'></i>";

                if (nodeIsSelectable && isAvailableInCurrentCulture)
                {
                    var nodeGuid = Guid.TryParse(value, out var parsedGuid) ? parsedGuid : Guid.Empty;

                    var isSelected = (nodeGuid != Guid.Empty) && selectedNodeGuids.Contains(nodeGuid);
                    if (this.UseSingleSelectMode)
                    {
                        string onclick = $"ProcessTreeLink(this,'{ValidationHelper.GetHashString(treeNode.NodeID.ToString(), new HashSettings(_securityPurpose))}',true);";
                        var selectedClass = isSelected ? "ContentTreeItem ContentTreeSelectedItem" : "ContentTreeItem";
                        defaultNode.Text = $"<a href='javascript:void(0);' id='lnk{treeNode.NodeID}' style='text-decoration:none'>"
                                               + $"<span class='{selectedClass}' onclick=\"{onclick}\" data-value='{value}' id ='nde{treeNode.NodeID}'>"
                                               + $"<span class='Name'>&nbsp;{typeIcon}&nbsp;{displayName}</span></span></a>";
                    }
                    else
                    {
                        string onclick = $"ProcessTreeCheckbox(this,'{ValidationHelper.GetHashString(treeNode.NodeID.ToString(), new HashSettings(_securityPurpose))}',false,true);";
                        var checkedAttribute = isSelected ? "checked='checked' " : String.Empty;
                        defaultNode.Text = $"<span class='checkbox tree-checkbox'>"
                                                + $"<input id='chk{treeNode.NodeID}' data-value='{value}' type='checkbox' onclick=\"{onclick}\" name='{treeNode.NodeID}_{treeNode.NodeParentID}' {checkedAttribute}/>"
                                                + $"<label for='chk{treeNode.NodeID}' style='padding-left:0'>"
                                                + $"<span class='Name'>&nbsp;{typeIcon}&nbsp;{displayName}</span></label>";
                    }
                }
                else if ((!isAvailableInCurrentCulture) && (nodeLevel != 0))
                {

                    defaultNode.Text = $"{typeIcon}&nbsp;<span class='color-gray-100' title='{_notTranslatedTooltip}'>{displayName}"
                                       + $"&nbsp;<i aria-hidden='true' class='cms-icon-50  icon-rectangle-a-o color-gray-100'></i>"
                                       + "</span>";
                }
                else
                {
                    defaultNode.Text = $"{typeIcon}&nbsp;{displayName}";
                }

                var expandForSearchHit = false;
                if (hasChildSearchHit)
                {
                    _nodesExpandedForSearchHits++;
                }
                if(hasChildSearchHit 
                    && (_nodesExpandedForSearchHits <= MAX_NODES_TO_EXPAND_FOR_SEARCH_HITS))
                {
                    expandForSearchHit = true;
                }
                else if(hasChildSearchHit && 
                        (_nodesExpandedForSearchHits == (MAX_NODES_TO_EXPAND_FOR_SEARCH_HITS + 1)))
                {
                    InstructionsPanel.Controls.Add(new Literal { Text = $"<p>Up to {MAX_NODES_TO_EXPAND_FOR_SEARCH_HITS} nodes were automatically expanded to show the first search hits. Expand the tree sections or enter a more specific search term to find what you need.</p>" });
                }
                // Expand selected items
                if (hasCheckedChildren || expandForSearchHit)
                {
                    defaultNode.Expand();
                }
                _createdNodes++;

                return defaultNode;
            }

            return null;
        }

        /// <summary>
        /// Apply the user-provided search term, if it passes validation.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <remarks>This assumes client validation was performed and does not
        /// provide user feedback if the validation fails.</remarks>
        protected void SearchButton_Click(Object sender, EventArgs e)
        {
            if(!Page.IsValid)
            {
                return;
            }
            var searchText = SearchTermBox.Text.Trim();
            HiddenCurrentSearchTerm.Value = searchText;
            SearchTermBox.Text = searchText;
        }

        protected void ResetSearchButton_Click(Object sender, EventArgs e)
        {
            HiddenCurrentSearchTerm.Value = string.Empty;
            SearchTermBox.Text = string.Empty;
        }

        #endregion


        #region "Methods"


        /// <summary>
        /// Loads control parameters.
        /// </summary>
        /// <remarks>The RelatedContentSelector uses the WindowHelper to pass parameters
        /// to this dialog, via the UniSelector control.</remarks>
        private void LoadParameters()
        {
            string identifier = QueryHelper.GetString("params", null);
            var parameters = (Hashtable)WindowHelper.GetItem(identifier);

            if (parameters == null)
            {
                _valuesSeparator = ";";
                _returnColumnName = "NodeID";
                _securityPurpose = String.Empty;
                _cultureCode = LocalizationContext.PreferredCultureCode;
                _siteId = SiteContext.CurrentSiteID;
                _pageTypeList = new string[0];
                _startPath = "/";
                _limitSelectionTo = DEFAULT_SELECTION_LIMIT;
                return;
            }
            // Load values from session
            _valuesSeparator = ValidationHelper.GetString(parameters["ValuesSeparator"], ";");
            _returnColumnName = ValidationHelper.GetString(parameters["ReturnColumnName"], "NodeID");
            _securityPurpose = ValidationHelper.GetString(parameters["SecurityPurpose"], string.Empty);
            _cultureCode = ValidationHelper.GetString(parameters["CultureCode"], string.Empty);
            _siteId = ValidationHelper.GetInteger(parameters["SiteID"], 0);
            _pageTypeList = ValidationHelper.GetValue<string[]>(parameters["PageTypes"], new string[0]);
            _startPath = ValidationHelper.GetString(parameters["StartPath"], string.Empty);
            _limitSelectionTo = ValidationHelper.GetInteger(parameters["LimitSelectionTo"], DEFAULT_SELECTION_LIMIT);


            // Pre-select unigrid values passed from parent window
            if (!RequestHelper.IsPostBack())
            {
                string values = ValidationHelper.GetString(parameters["Values"], string.Empty);
                HiddenValueField.Value = values;
                HiddenHashField.Value = ValidationHelper.GetHashString(HiddenValueField.Value, new HashSettings(_securityPurpose));
                parameters["Values"] = null;
            }
        }


        /// <summary>
        /// Creates tree provider.
        /// </summary>
        private UniTreeProvider CreateTreeProvider()
        {
            // The SQL query will be optimized to avoid an unnecessary LIKE condition
            // if "/" is is replaced with an empty string.
            var rootPath = (_startPath.IsNullOrWhiteSpace() || (_startPath == "/")) ? string.Empty : _startPath;
            var rootLevel = rootPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries).Length;
            var selectedNodeGuids = GetSelectedNodeGuids();
            var searchText = HiddenCurrentSearchTerm.Value.Trim();


            // Create and set tree provider
            var queryDataParameters = new QueryDataParameters
                {
                    { "@SiteID", _siteId },
                    { "@Culture", _cultureCode },
                    {"@StartingNodeAliasPath", rootPath },
                    {"@SearchText", searchText },
                    CreateGuidTableParameter("@SelectedNodeGuids", selectedNodeGuids),
                    CreateStringTableParameter("@SelectablePageTypes", _pageTypeList?.ToList())
                };
            var provider = new UniTreeProvider
            {
                RootLevelOffset = rootLevel,
                QueryName = "cms.root.relatedcontentselectortree",
                DisplayNameColumn = "DocumentName",
                IDColumn = "NodeID",
                LevelColumn = "NodeLevel",
                OrderColumn = "NodeOrder",
                ParentIDColumn = "NodeParentID",
                PathColumn = "NodeAliasPath",
                ValueColumn = "NodeGUID",
                ChildCountColumn = "NodeChildCount",
                IconClassColumn = "ClassIconClass",
                Parameters = queryDataParameters
            };
            return provider;
        }

        private void InitializeTreeMenu()
        {
            TreeControl.NodeTemplate = string.Empty;
            TreeControl.UsePostBack = false;
            TreeControl.ProviderObject = CreateTreeProvider();
            TreeControl.ExpandPath = "/";
        }


        private string GetSelectorScript()
        {
            var parentClientId = QueryHelper.GetControlClientId("clientId", string.Empty);

            var script = $@"
var limitSelectionTo = {_limitSelectionTo};

function pageLoad(sender, args) 
{{ 
    EnsureLimitEnableState();
    WatchTreeChanges();
    hideLoader();
}}

// When tree expansion occurs, ASP.NET Script Callbacks are used by
// UniTree to get the data and create the nodes. 
// Only aparent way to detect new nodes and apply enabled state.
function WatchTreeChanges()
{{
    let observer = new MutationObserver(mutationRecords => {{
        EnsureLimitEnableState(); 
    }});

    var treeElement = GetTreeElem();
    
    observer.observe(treeElement, {{
      childList: true,
      subtree: true,
    }});
}}



function SelectItemsReload(items, hiddenFieldId, txtClientId, hidValue, hashClientId, hash) {{
    if (items.length > 0) {{
        wopener.US_SetItems(items, '', hiddenFieldId, txtClientId, hidValue, hashClientId, hash);
    }} else {{
        wopener.US_SetItems('','', hiddenFieldId, txtClientId);
    }}
    wopener.US_ReloadPage_{parentClientId}();
    return CloseDialog();
}}
  
function GetTreeElem() {{
    return document.getElementById('{TreeControl.ClientID}');
}}

function ItemsElem() {{
    return document.getElementById('{HiddenValueField.ClientID}');
}}

function HashElem() {{
    return document.getElementById('{HiddenHashField.ClientID}');
}}

function SetHash(hashvalue) {{
    var hashElem = HashElem();
    if (hashElem != null) {{
        hashElem.value = hashvalue;
    }}
}}

function unselectItem(itemValue) {{
    var itemsElem = ItemsElem();
    var re = new RegExp('{_valuesSeparator}' + itemValue + '{_valuesSeparator}', 'i');
    itemsElem.value = itemsElem.value.replace(re, '{_valuesSeparator}');
}}

function ProcessTreeLink(link, hash, getHash) {{
    var itemsElem = ItemsElem();
    var selectionHash = '';
    if (link != null) {{
        var itemValue = link.getAttribute('data-value');
        var itemId = link.id.substr(3);
        itemsElem.value = '{_valuesSeparator}' + itemValue + '{_valuesSeparator}';
        selectionHash = '|' + itemId + '#' + hash;
        UnselectAllLinks();
        link.classList.add('ContentTreeSelectedItem');
    }}
    if (getHash) {{
        {Page.ClientScript.GetCallbackEventReference(this, "itemsElem.value + selectionHash", "SetHash", null)};
    }}
}}

function UnselectAllLinks() {{
    const priorSelectedLinks = document.querySelectorAll('.ContentTreeSelectedItem');
    for (const link of priorSelectedLinks) {{
      link.classList.remove('ContentTreeSelectedItem');
    }}    
}}

function EnsureLimitEnableState()
{{
    var currentCount = GetSelectionCount();
    ApplyLimitEnableState(currentCount < limitSelectionTo);
}}

function ApplyLimitEnableState(enable)
{{
    const uncheckedBoxes = document.querySelectorAll('.TreeTree input[type=""checkbox""]:not(:checked)');
    for (const box of uncheckedBoxes) {{
      box.disabled = (!enable);
    }}    
}}

function GetSelectionCount() {{
    var itemsElem = ItemsElem();
    var items = itemsElem.value;
    var trimmedItems = items.replace(/^{_valuesSeparator}+|{_valuesSeparator}+$/g, '');
    var separatorCount = (trimmedItems.match(new RegExp('{_valuesSeparator}', 'g')) || []).length;
    return (trimmedItems === null || trimmedItems.length === 0) ? 
                0 : 
                separatorCount + 1;
}}


function ProcessTreeCheckbox(chkbox, hash, changeChecked, getHash) {{
    var itemsElem = ItemsElem();
    var items = itemsElem.value;
    var checkHash = '';
    if (chkbox != null) {{
        var itemValue = chkbox.getAttribute('data-value');
        var itemId = chkbox.id.substr(3);
        if (changeChecked) {{
            chkbox.checked = !chkbox.checked;
        }}
        var currentCount = GetSelectionCount();
        if (chkbox.checked) {{
            if(currentCount >= limitSelectionTo)
            {{
                chkbox.checked = !chkbox.checked;
                return;
            }}
            if (items == '') {{
                itemsElem.value = '{_valuesSeparator}' + itemValue + '{_valuesSeparator}';
            }}
            else if (items.toLowerCase().indexOf('{_valuesSeparator}' + itemValue.toLowerCase() + '{_valuesSeparator}') < 0) {{
                itemsElem.value += itemValue + '{_valuesSeparator}';
            }}
            if((currentCount + 1) == limitSelectionTo) {{
                ApplyLimitEnableState(false);
            }}
        }}
        else {{
            unselectItem(itemValue);
            if((currentCount) == limitSelectionTo) {{
                ApplyLimitEnableState(true);
            }}
        }}

        checkHash = '|' + itemId + '#' + hash;
    }}
    else {{
        checkHash = '|' + items.replace('{_valuesSeparator}',';') + '#' + hash;
    }}
    if (getHash) {{
        {Page.ClientScript.GetCallbackEventReference(this, "itemsElem.value + checkHash", "SetHash", null)};
    }}
}}
            
function Cancel() {{
    wopener.US_RefreshPage_{parentClientId}(); CloseDialog();
}}

function displayLoader() {{
    if (window.Loader) {{
        window.Loader.show();
    }}
}}

function hideLoader() {{
    if (window.Loader) {{
        window.Loader.hide();
    }}
}}

{GetButtonsScript()}";

            return script;
        }


        private string GetButtonsScript()
        {
            var txtClientId = ScriptHelper.GetString(QueryHelper.GetString("txtElem", string.Empty));
            var hdnClientId = ScriptHelper.GetString(QueryHelper.GetString("hidElem", string.Empty));
            var hdnDrpClientId = ScriptHelper.GetString(QueryHelper.GetString("selectElem", string.Empty));
            var hashElementClientId = ScriptHelper.GetString(QueryHelper.GetString("hashElem", string.Empty));

            return $@"
function US_Cancel(){{ Cancel(); return false; }}
function US_Submit(){{ SelectItemsReload(encodeURIComponent(ItemsElem().value).replace(/'/g, '%27'), {hdnClientId}, {txtClientId}, {hdnDrpClientId}, {hashElementClientId}, HashElem().value); return false; }}";
        }


        private List<Guid> GetSelectedNodeGuids()
        {
            var selectedNodeGUIDs = new List<Guid>();
            var hiddenValue = HiddenValueField.Value;
            if (!string.IsNullOrEmpty(hiddenValue))
            {
                var splitValues = hiddenValue.Split(new[]{_valuesSeparator}, StringSplitOptions.RemoveEmptyEntries);
                selectedNodeGUIDs = splitValues.Select(x => ValidationHelper.GetGuid(x, Guid.Empty)).ToList();
            }
            return selectedNodeGUIDs;
        }

        /// <summary>
        /// CSS that is used to customize tree selector
        /// </summary>
        private void RegisterStyles()
        {
            // register custom inline styles (this can be moved to separate CSS file)
            CssRegistration.RegisterCssBlock(this.Page, nameof(TreeSelectorDialog),
            @"
                .form-horizontal.form-filter {
                    width: 100%;
                }

                .cms-bootstrap .form-filter .filter-form-value-cell,
                .cms-bootstrap .form-filter .filter-form-buttons-cell-narrow {
                    margin-bottom: 8px;
                }

                .cms-bootstrap .form-filter .filter-form-value-cell {
                    text-align: center;
                }

                .cms-bootstrap .form-control-error:before {
                    content: """";
                    margin-right: 0;
                }

                @media screen and (min-width: 660px) {
                    .cms-bootstrap .form-filter .filter-form-buttons-cell-narrow {
                        width:30%
                    }
                }
            ");
        }


        /// <summary>
        /// Return a DataParameter that defines a Kentico table parameter of
        /// type Type_CMS_GuidTable
        /// </summary>
        /// <param name="parameterName"></param>
        /// <param name="guids"></param>
        /// <returns></returns>
        /// <remarks>
        /// A table parameter must have at least one row.
        /// But we must provide the table, otherwise the parameter will
        /// not bedefined in the query.
        /// We could switch to a stored procedure, except the UniTreeProvider
        /// requires being able to replace the ##WHERE## macro.
        /// </remarks>
        private DataParameter CreateGuidTableParameter(string parameterName, List<Guid> guids)
        {
            // A table parameter must have at least one row.
            if(!guids.Any())
            {
                guids.Add(Guid.Empty);
            }
            var guidTableParameter = new DataParameter(parameterName, SqlHelper.BuildGuidTable(guids))
            {
                Type = typeof(IEnumerable<Guid>)
            };
            return guidTableParameter;
        }

        /// <summary>
        /// Return a DataParameter that defines a Kentico table parameter of
        /// type Type_CMS_StringTable
        /// </summary>
        /// <param name="parameterName"></param>
        /// <param name="guids"></param>
        /// <returns></returns>
        /// <remarks>
        /// A table parameter must have at least one row.
        /// But we must provide the table, otherwise the parameter will
        /// not bedefined in the query.
        /// We could switch to a stored procedure, except the UniTreeProvider
        /// requires being able to replace the ##WHERE## macro.
        /// </remarks>
        private DataParameter CreateStringTableParameter(string parameterName, List<string> strings)
        {
            var inputStrings = (strings != null && strings.Any())
                ? strings 
                : new List<string>() { String.Empty };

            var dataParameter = new DataParameter(parameterName, SqlHelper.BuildStringTable(inputStrings))
            {
                Type = typeof(IEnumerable<string>)
            };
            return dataParameter;
        }

        #endregion


        #region "ICallbackEventHandler Members"

        string ICallbackEventHandler.GetCallbackResult()
        {
            // Prepare the parameters for dialog
            string result = string.Empty;
            if (!string.IsNullOrEmpty(_callbackValues))
            {
                bool isValid = false;

                string[] values = _callbackValues.Split(NODE_SEPARATOR);
                if (values.Length == 2)
                {
                    // Check hash of the selected item
                    string[] checkValues = values[1].Split('#');

                    var settings = new HashSettings(_securityPurpose)
                    {
                        Redirect = false
                    };

                    isValid = (checkValues.Length == 2) && ValidationHelper.ValidateHash(checkValues[0].Trim(';'), checkValues[1], settings);
                }

                if (isValid)
                {
                    // Get new hash for currently selected items
                    result = ValidationHelper.GetHashString(values[0], new HashSettings(_securityPurpose));
                }
            }

            return result;
        }


        void ICallbackEventHandler.RaiseCallbackEvent(string eventArgument)
        {
            _callbackValues = eventArgument;
        }

        #endregion
    }
}
