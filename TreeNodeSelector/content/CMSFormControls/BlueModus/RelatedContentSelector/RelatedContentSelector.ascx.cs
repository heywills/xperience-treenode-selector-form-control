/*
    This page is provided by the Nuget package XperienceCommunity.TreeNodeSelectorFormControl.
    Changes to this file will be lost when this package is restored or updated.
*/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using CMS.Base.Web.UI;
using CMS.DocumentEngine;
using CMS.FormEngine.Web.UI;
using CMS.Helpers;
using CMS.Localization;
using CMS.MacroEngine;
using CMS.SiteProvider;
using CMS.UIControls;
using DocumentFormat.OpenXml.Bibliography;

namespace CMSApp.CMSFormControls.RelatedContentSelector
{
    public partial class RelatedContentSelector : FormEngineUserControl, ICallbackEventHandler, IPostBackEventHandler
    {
        #region Variables

        /// <summary>
        /// Separator that is used to distinguish different objects/guids
        /// </summary>
        private const char ITEM_SEPARATOR = ';';
        private bool _hashIsValid = true;
        private string _validatedStartingPath = null;

        #endregion Variables


        #region Configuration

        /// <summary>
        /// The NodeAliasPath to use as the root of the tree selector.
        /// Because of a prior bug, this value could be a NodeGUID instead of
        /// a NodeAliasPath, so test for both.
        /// </summary>
        /// Use MacroResolver, in case value wasn't set in the 
        /// "Edit value (>)" dialog
        /// </remarks>
        private string StartingPath
        {
            get
            {
                return MacroResolver.Resolve(GetValue("StartingPath", ""));
            }
        }

        /// <summary>
        /// The maximum number of selected items to allow.
        /// </summary>
        public int LimitSelectionTo
        {
            get
            {
                return ValidationHelper.GetInteger(GetValue("ReferenceLimit"), 50);
            }
        }

        /// <summary>
        /// The list of page types to limiit the content selection to.
        /// </summary>
        /// <remarks>
        /// Use MacroResolver, in case value wasn't set in the 
        /// "Edit value (>)" dialog
        /// </remarks>
        public string PageTypes
        {
            get
            {
                return MacroResolver.Resolve(GetValue("PageTypes", ""));
            }
        }

        #endregion Configuration 

        #region Properties

        /// <summary>
        /// Represents unique prefix for the order guids
        /// </summary>
        public string SorableItemIDPrefix
        {
            get
            {
                return $"{this.ClientID}_li_";
            }
        }

        // <summary>
        /// Gets or sets the value that will be saved from this form control
        /// </summary>
        /// <summary>
        /// Gets or sets field value.
        /// </summary>
        public override object Value
        {
            get
            {
                return GetDelimiterWrappedValue().Trim(ITEM_SEPARATOR);
            }

            set
            {
                string fieldValue = EnsureValueFormat(ValidationHelper.GetString(value, ""));
                SetValue(fieldValue);
                InitializeRepeater(fieldValue);
            }
        }

        /// <summary>
        /// Control that provides a placeholder for messages.
        /// </summary>
        public override MessagesPlaceHolder MessagesPlaceHolder
        {
            get
            {
                return LocalMessagePlaceholder;
            }
        }

        #endregion Properties

        #region Events


        private static string GetNodeState(TreeNode node)
        {
            string nodeState = "";
            if (node.IsArchived)
            {
                nodeState = "archived";
            }
            else
            {
                if (node.IsPublished)
                {
                    if (node.IsInPublishStep)
                    {
                        nodeState = "published";
                    }
                    else
                    {
                        nodeState = "notinpublishedstep";
                    }
                }
            }

            return nodeState;
        }

        /// <summary>
        /// Used to register events
        /// </summary>
        protected override void OnInit(EventArgs e)
        {
            SetHashValue(true);
            base.OnInit(e);
        }

        /// <summary>
        /// Attempts to get a valid NodeAliasPath from the StartingPath property.
        /// Because of a prior bug, this value could be a NodeGUID instead of
        /// a NodeAliasPath, so test for both.
        /// </summary>
        /// <returns></returns>
        private string GetStartPath()
        {
            if (!string.IsNullOrWhiteSpace(_validatedStartingPath))
            {
                return _validatedStartingPath;
            }
            var providedStartingPathValue = this.StartingPath;
            if (string.IsNullOrWhiteSpace(providedStartingPathValue))
            {
                return null;
            }

            var treeNode = GetTreeNodeFromNodeAliasPath(this.StartingPath);
            if (treeNode == null)
            {
                treeNode = GetTreeNodeFromNodeGuid(this.StartingPath);
            }
            if (treeNode == null)
            {
                return null;
            }
            _validatedStartingPath = treeNode.NodeAliasPath;
            return _validatedStartingPath;
        }

        /// <summary>
        /// Get the edit form culture.
        /// </summary>
        /// <returns></returns>
        private string GetEditFormCulture()
        {
            var culture = Request.QueryString["culture"];

            if (string.IsNullOrEmpty(culture))
            {
                culture = LocalizationContext.PreferredCultureCode;
            }

            return culture;
        }

        private string[] GetPageTypeList()
        {
            var list = new string[0];
            // If the property is set by a macro, whitespace can easily be added
            var pageTypes = this.PageTypes.Trim();
            if (!string.IsNullOrEmpty(pageTypes))
            {
                list = pageTypes.Split(';');
                // If PageTypes is set with a macro, there might be an empty element from a trailing delimiter
                list = list.Where(t => (!String.IsNullOrWhiteSpace(t))).ToArray();
            }
            return list;
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            PrepareControl();
        }

        /// <summary>
        /// Custom ViewState restoration so that the hash is validated
        /// </summary>
        /// <param name="savedState"></param>
        protected override void LoadViewState(object savedState)
        {
            base.LoadViewState(savedState);
            // Do not overwrite value if ViewState doesn't contain value for current control (e.g. disabled control)
            if (Request.Form.AllKeys.Contains(HiddenIdentifier.UniqueID))
            {
                HiddenIdentifier.Value = Request.Form[HiddenIdentifier.UniqueID];
            }
            if (Request.Form.AllKeys.Contains(HiddenValue.UniqueID))
            {
                HiddenValue.Value = Request.Form[HiddenValue.UniqueID];
            }
            if (Request.Form.AllKeys.Contains(HiddenHash.UniqueID))
            {
                HiddenHash.Value = Request.Form[HiddenHash.UniqueID];
            }
            if (Request.Form.AllKeys.Contains(HiddenSafeValueSet.UniqueID))
            {
                HiddenSafeValueSet.Value = Request.Form[HiddenSafeValueSet.UniqueID];
            }
            // Validate value against hash
            ValidateValue();
        }


        #endregion Events

        #region Helper methods

        /// <summary>
        /// Initializes control
        /// </summary>
        private void PrepareControl()
        {
            ScriptHelper.RegisterBootstrapScripts(Page);
            ScriptHelper.RegisterJQueryUI(Page);

            AddStyles();

            // Apply Form Control properties.
            SetTreeSelectorDialogParameters();

            InitializeRepeater(GetDelimiterWrappedValue());

            // Register edit script file
            RegisterEditScripts();
            SetupSelectButton();
        }

        private void SetupSelectButton()
        {
            var selectionDialogUrl = "~/CMSFormControls/BlueModus/RelatedContentSelector/TreeSelectorDialog/TreeSelectorDialog.aspx"
                + $"?SelectionMode={GetSelectionMode()}"
                + $"&hidElem={HiddenValue.ClientID}"
                + $"&params={Server.UrlEncode(ScriptHelper.GetString(GetControlIdentifier(), false))}"
                + $"&clientId={this.ClientID}"
                + "&localize=0"
                + $"&hashElem={HiddenHash.ClientID}";


            var clientResponseFunctionName = $"SelectionDialogReady_{this.ClientID}";
            var clientEventFunctionName = $"PrepareSelectionDialog_{this.ClientID}";
            // Selectors dialog's based on the UniSelector require the parent page
            // to have client-side functions with the following naming convension.
            var parentReloadFunctionName = $"US_ReloadPage_{this.ClientID}";
            var parentRefreshFunctionName = $"US_RefreshPage_{this.ClientID}";
            var clientCallbackScript = $@"
                function {clientEventFunctionName} () {{
                    var hiddenResponseInput = document.getElementById('{HiddenValue.ClientID}');
                    var selectedValues = hiddenResponseInput.value;
                    {Page.ClientScript.GetCallbackEventReference(this, $"selectedValues", clientResponseFunctionName, string.Empty)};
                }}
                
                function {clientResponseFunctionName} (rvalue, context) {{
                    modalDialog('{HttpUtility.JavaScriptStringEncode(ScriptHelper.ResolveUrl(selectionDialogUrl))}' + ((rvalue != '') ? '&selectedvalue=' + rvalue : ''), 'Select content', 750, 650, null, null, true);
                    return false;
                }}

                function {parentReloadFunctionName} () {{
                    {Page.ClientScript.GetPostBackEventReference(this, "reload")};
                    return false;
                }}

                function {parentRefreshFunctionName} () {{
                    {Page.ClientScript.GetPostBackEventReference(this, "refresh")};
                    return false;
                }}
                ";

            AddItemsButton.OnClientClick = $"{clientEventFunctionName}();return false;";
            AddItemsButton.ResourceString = (this.GetSelectionMode() == SelectionModeEnum.SingleButton)
                                                ? "general.select"
                                                : "general.additems";
            ScriptHelper.RegisterClientScriptBlock(this, typeof(string), clientResponseFunctionName, ScriptHelper.GetScript(clientCallbackScript));
            ScriptHelper.RegisterScriptFile(Page, "Controls/uniselector.js");
        }
        /// <summary>
        /// CSS that is used to style order guids
        /// Note: Can be moved to separated CSS file
        /// </summary>
        private void AddStyles()
        {
            // register Bootstrap for UniSelector form cotnrol
            CssRegistration.RegisterBootstrap(Page);
            // register custom inline styles (this can be moved to separate CSS file)
            CssRegistration.RegisterCssBlock(this.Page, nameof(RelatedContentSelector),
            @"
                .related-items {
                  list-style-type: none;
                  padding: 0;
                }
                .related-items .sortedItem > span {
                  float: right;
                  display: inline-block;
                  cursor: pointer;
                }
                .related-items .sortedItem:hover {
                  background-color: #c0c0c0;
                }
                .related-items .sortedItem {
                  border-radius: 3px;
                  margin: 4px 0px;
                  padding: 6px 9px;
                  display: block;
                  background-color: #d6d9d6;
                  cursor: move;
                  max-width: 800px;
                }
                .related-items .sortedItem {
                  border-radius: 3px;
                  margin: 4px 0px;
                  padding: 6px 9px;
                  display: block;
                  background-color: #d6d9d6;
                  cursor: move;
                  max-width: 800px;
                }
                .related-items .wrapLeft {
                  width: 92%;
                  display: inline-block;
                }
                .related-items .wrapLeft .item-status {
                  display: inline-block;
                  width: 12px;
                  height: 12px;
                  padding: 0;
                  margin: 0;
                }
                .related-items .wrapLeft .item-status .tn-icon {
                  float: initial;
                }
                .related-items .wrapLeft .item-title {
                  display: inline-block;
                }
                .related-items .wrapLeft .item-path {
                  display: block;
                }
                .related-items .wrapRight {
                  width: 20px;
                  float: right;
                  display: inline-block;
                }
                .related-items .wrapRight i.icon-bin {
                  color: #b12628;
                }
                .related-items .wrapRight i.icon-edit {
                  color: #497d04;
                }
                .related-items .remove-item {
                  cursor: pointer;
                }
            ");
        }



        private SelectionModeEnum GetSelectionMode()
        {
            return LimitSelectionTo == 1 ? SelectionModeEnum.SingleButton : SelectionModeEnum.MultipleButton;
        }

        /// <summary>
        /// Get unique control identifier
        /// </summary>
        private string GetControlIdentifier()
        {
            string identifier = HiddenIdentifier.Value;
            if (string.IsNullOrEmpty(identifier))
            {
                identifier = Request.Form[HiddenIdentifier.UniqueID];
                if (string.IsNullOrEmpty(identifier))
                {
                    identifier = Guid.NewGuid().ToString();
                }
                HiddenIdentifier.Value = identifier;
            }
            return identifier;
        }

        /// <summary>
        /// Get the list of values delimited by ";", but ensure
        /// the list starts and ends with the delimiteer, because
        /// some out-of-the-box UniSelectors have client-side code
        /// that requires them for efficent parsing.
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        private string CreateSerializedValueFromList(List<Item> items)
        {
            if (items == null || items.Count == 0)
            {
                return String.Empty;
            }
            var uniValue = string.Join(ITEM_SEPARATOR.ToString(), items.Select(i => i.ItemValue));
            if (string.IsNullOrWhiteSpace(uniValue))
            {
                return String.Empty;
            }
            return ITEM_SEPARATOR + uniValue + ITEM_SEPARATOR;
        }

        private List<Item> GetRelatedItemsFromGuids(string[] itemIds)
        {
            var nodes = GetTreeNodesFromNodeGuids(itemIds);
            if(nodes == null)
            {
                return null;
            }
            var items = nodes.Select(node => new Item() 
                                {
                                    ItemId = node.NodeID.ToString(),
                                    ItemText = GetDisplayContent(node),
                                    ItemValue = node.NodeGUID.ToString(), 
                                    ItemState = GetNodeState(node)
                                })
                            .OrderBy(x => Array.IndexOf(itemIds, x.ItemValue))
                            .ToList();
            return items;
        }

        /// <summary>
        /// Set the parameters needed by the TreeSelectorDialog. These will be
        /// passed by the UniSelector control to the TreeSelectorDialog via
        /// the WindowHelper class.
        /// </summary>
        private void SetTreeSelectorDialogParameters()
        {
            Hashtable hashtable = new Hashtable();
            hashtable["SelectionMode"] = GetSelectionMode();
            hashtable["ReturnColumnName"] = "NodeGUID";
            hashtable["Values"] = GetDelimiterWrappedValue();
            hashtable["HasDependingFields"] = HasDependingFields;
            hashtable["SecurityPurpose"] = ClientID;
            hashtable["CultureCode"] = GetEditFormCulture();
            hashtable["SiteID"] = SiteContext.CurrentSiteID;
            hashtable["PageTypes"] = GetPageTypeList();
            hashtable["StartPath"] = GetStartPath();
            hashtable["LimitSelectionTo"] = this.LimitSelectionTo;
            WindowHelper.Add(GetControlIdentifier(), hashtable);
        }

        /// <summary>
        /// Create a list of Item objects from the delimited list of guids.
        /// </summary>
        /// <param name="fieldValue"></param>
        /// <returns></returns>
        /// <remarks>
        /// Consider refactoring so that this does one SQL round trip for the entire list
        /// </remarks>
        private List<Item> CreateItemListRepeaterFromSerializedValue(string fieldValue)
        {

            if (String.IsNullOrEmpty(fieldValue))
            {
                return new List<Item>();
            }
            var guids = GetItemGuidsFromValueString(fieldValue);
            var listItems = GetRelatedItemsFromGuids(guids);
            return listItems;
        }

        private string[] GetItemGuidsFromValueString(string fieldValue)
        {
            if (String.IsNullOrWhiteSpace(fieldValue))
            {
                return new string[0];
            }
            return fieldValue.Trim(ITEM_SEPARATOR).Split(ITEM_SEPARATOR);
        }

        /// <summary>
        /// Generates hash from value.
        /// </summary>
        /// <remarks>Client-side script will be allowed to remove, or reorder
        /// guids in the value, as long as it doesn't add new guids.
        /// This will allow deleting or reordering items in the HiddenValue control
        /// without updating the hash.
        /// Whenever a new item is added, a post-back will be required to
        /// update the hash, too.</remarks>
        /// <param name="initHash">Indicates whether hash should be initialized based on empty string value.</param>
        private void SetHashValue(bool initHash = false)
        {
            if (initHash)
            {
                if (String.IsNullOrEmpty(HiddenHash.Value))
                {
                    HiddenHash.Value = ValidationHelper.GetHashString(String.Empty, new HashSettings(ClientID));
                    HiddenSafeValueSet.Value = String.Empty;
                }
                return;
            }
            HiddenHash.Value = ValidationHelper.GetHashString(GetDelimiterWrappedValue(), new HashSettings(ClientID));
            HiddenSafeValueSet.Value = HiddenValue.Value;
        }

        /// <summary>
        /// Validates Value against hash.
        /// </summary>
        /// <remarks>To be valid, a hash created with the current value
        /// must match the stored hash.
        /// Or, a hash created with the HiddenSafeValueSet must match
        /// AND the current value must not have Guids that are not in
        /// the HiddenSafeValueSet.</remarks>
        private void ValidateValue()
        {
            string newValue = GetDelimiterWrappedValue();

            if (!String.IsNullOrEmpty(newValue))
            {
                // Validate hash (if not special value - all, empty...)
                var settings = new HashSettings(ClientID)
                {
                    Redirect = false
                };

                if (ValidationHelper.ValidateHash(newValue, HiddenHash.Value, settings))
                {
                    _hashIsValid = true;
                    return;
                }
                var safeValueSet = HiddenSafeValueSet.Value;
                if (ValidationHelper.ValidateHash(safeValueSet, HiddenHash.Value, settings))
                {
                    var safeValues = GetItemGuidsFromValueString(safeValueSet);
                    var newValues = GetItemGuidsFromValueString(newValue);
                    _hashIsValid = newValues.All(guidString => string.IsNullOrWhiteSpace(guidString) || safeValues.Contains(guidString));
                    if (_hashIsValid)
                    {
                        return;
                    }
                }
                if (!_hashIsValid)
                {
                    if (!IsLiveSite)
                    {
                        // Data is not consistent!
                        ShowWarning(GetString("uniselector.badhash"));
                    }
                    // Reset value
                    SetValue(string.Empty);
                }
            }
        }

        /// <summary>
        /// Sets the value to the selector.
        /// </summary>
        /// <param name="value">New value</param>
        private void SetValue(string value)
        {
            HiddenValue.Value = EnsureValueFormat(value);
            SetHashValue();
        }

        /// <summary>
        /// Gets the current value of the selector.
        /// </summary>
        /// <param name="trim">Indicates if separators should be trimmed from the value</param>
        /// <remarks>Use this method whenever the value is needed internally, so that the
        /// value is correctly wrapped with the delimiter. This is necessary to implify
        /// the client side code.</remarks>
        private string GetDelimiterWrappedValue()
        {
            string result = string.Empty;
            if (_hashIsValid)
            {
                result = HiddenValue.Value;
            }

            return result;
        }


        private string EnsureValueFormat(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }
            if (!value.StartsWith(ITEM_SEPARATOR.ToString()))
            {
                value = ITEM_SEPARATOR + value;
            }
            if (!value.EndsWith(ITEM_SEPARATOR.ToString()))
            {
                value += ITEM_SEPARATOR;
            }
            return value;
        }

        /// <summary>
        /// Initializes repeater with list guids
        /// </summary>
        /// <remarks>
        /// This method is relatively expensive, because it queries Kentico
        /// for each guid to get the node details. 
        /// </remarks>
        private void InitializeRepeater(string fieldValue)
        {
            var itemList = CreateItemListRepeaterFromSerializedValue(fieldValue);
            var validatedValue = CreateSerializedValueFromList(itemList);
            ItemListRepeater.DataSource = itemList;
            ItemListRepeater.DataBind();
            SetValue(validatedValue);
        }

        private TreeNode GetTreeNodeFromNodeGuid(string guidString)
        {
            if (string.IsNullOrWhiteSpace(guidString))
            {
                return null;
            }

            if (!Guid.TryParse(guidString, out _))
            {
                return null;
            }
            var nodes = GetTreeNodesFromNodeGuids(new[] { guidString });
            return nodes.FirstOrDefault();
        }

        private List<TreeNode> GetTreeNodesFromNodeGuids(string[] guidStrings)
        {
            if (guidStrings is null || guidStrings.Length == 0)
            {
                return null;
            }
            var currentCulture = GetEditFormCulture();
            var cacheKey = $"{nameof(RelatedContentSelector)}|{nameof(GetTreeNodesFromNodeGuids)}|{currentCulture}|{string.Join("|", guidStrings)}";
            var cacheSettings = new CacheSettings(60, cacheKey);
            var cachedOutput = CacheHelper.Cache(c =>
            {
                var nodes = new MultiDocumentQuery()
                    .OnCurrentSite()
                    .Culture(currentCulture)
                    .CombineWithDefaultCulture()
                    .LatestVersion()
                    .WhereIn(nameof(TreeNode.NodeGUID), guidStrings)
                    .ToList();
                var dependencyKeys = nodes.Select(n => $"documentid|{n.DocumentID}").ToList();
                if(c.Cached)
                {
                    c.CacheDependency = CacheHelper.GetCacheDependency(dependencyKeys);
                }
                return nodes;
            }, cacheSettings);
            return cachedOutput;
        }

        private TreeNode GetTreeNodeFromNodeAliasPath(string nodeAliasPath)
        {
            if(string.IsNullOrWhiteSpace(nodeAliasPath))
            {
                return null;
            }
            var currentCulture = GetEditFormCulture();
            var cacheKey = $"{nameof(RelatedContentSelector)}|{nameof(GetTreeNodeFromNodeAliasPath)}|{currentCulture}{nodeAliasPath}";
            var cacheSettings = new CacheSettings(60, cacheKey);
            var cachedOutput = CacheHelper.Cache(c =>
            {
                var node = new MultiDocumentQuery()
                    .OnCurrentSite()
                    .Culture(GetEditFormCulture())
                    .CombineWithDefaultCulture()
                    .LatestVersion()
                    .Path(nodeAliasPath)
                    .FirstOrDefault();
                if (c.Cached && node != null)
                {
                    c.CacheDependency = CacheHelper.GetCacheDependency($"documentid|{node.DocumentID}");
                }
                return node;
            }, cacheSettings);
            return cachedOutput;
        }


        private string GetDisplayContent(TreeNode input)
        {
            if (input == null)
            {
                return "Error, no document found.";
            }
            var path = input.NodeAliasPath;
            var documentName = HTMLHelper.HTMLEncode(input.DocumentName);

            return $"<span class=\"item-title\"><b>{documentName}</b> ({input.ClassName})</span><em class=\"item-path\">{path}</em>";
        }

        protected string GetStatusIcon(object NodeState)
        {
            string nodeState = Convert.ToString(NodeState);
            string iconLabel = "";
            string iconClasses = "";

            switch (nodeState)
            {
                case "archived":
                    iconLabel = "Archived Page";
                    iconClasses = "icon-circle tn color-gray-100";
                    break;

                case "notinpublishedstep":
                    iconLabel = "New version not yet in the Published step";
                    iconClasses = "icon-diamond tn color-orange-80";
                    break;

                case "published":
                    iconLabel = "Published page";
                    iconClasses = "icon-check-circle tn color-green-100";
                    break;

                case "":
                    iconLabel = "Not Published page";
                    iconClasses = "icon-times-circle tn color-red-70";
                    break;
            }

            return $"<span><span class=\"tn-icon\"><i aria-hidden=\"true\" class=\"NodeLink {iconClasses}\" title=\"{iconLabel}\"></i><span class=\"sr-only\">{iconLabel}</span></span></span>";
        }

        #endregion Helper methods

        #region Form control methods

        /// <summary>
        /// Returns true if validation passes, false otherwise (see Kentico form control documentation for more info)
        /// </summary>
        public override bool IsValid()
        {
            var valid = true;

            var limit = this.LimitSelectionTo;

            if (limit > 0)
            {
                var itemListCount = CreateItemListRepeaterFromSerializedValue(this.GetDelimiterWrappedValue() as string).Count;

                if (itemListCount > limit)
                {
                    valid = false;
                    this.AddMessage(MessageTypeEnum.Error, $"{this.Field} field has a limit of {limit} selected items. You have {itemListCount} items selected.");
                }
            }
            return valid;
        }

        #endregion Form control methods

        #region Edit dialog

        #region Callback handling

        /// <summary>
        /// Raises the callback event from client-side code. It is only used
        /// to provide current values when the select-guids button is clicked.
        /// </summary>
        public void RaiseCallbackEvent(string eventArgument)
        {
            string value = eventArgument ?? string.Empty;
            var itemList = CreateItemListRepeaterFromSerializedValue(value);
            var validatedValue = CreateSerializedValueFromList(itemList);
            SetValue(validatedValue);
            SetTreeSelectorDialogParameters();
        }

        /// <summary>
        /// Return the callback result. 
        /// </summary>
        /// <remarks>
        /// The select-guids button does not need a result value.
        /// It's just waits for the callback to ensure the current
        /// value is stored.
        /// </remarks>
        public string GetCallbackResult()
        {
            return String.Empty;
        }

        #endregion Callback handling

        /// <summary>
        /// Registers script files for on-site editing
        /// </summary>
        /// <param name="pi">Current page info</param>
        private void RegisterEditScripts()
        {
            var nodeIdString = Request.QueryString["nodeid"];
            var culture = GetEditFormCulture();
            int nodeId;

            if (!(String.IsNullOrWhiteSpace(nodeIdString) || String.IsNullOrWhiteSpace(culture)) && int.TryParse(nodeIdString, out nodeId))
            {
                ScriptHelper.RegisterJQueryCookie(Page);
                StringBuilder script = new StringBuilder();

                // Dialog scripts
                ScriptHelper.RegisterDialogScript(Page);
                var editUrl = UrlResolver.ResolveUrl($"~/CMSFormControls/BlueModus/RelatedContentSelector/Dialogs/Edit.aspx");

                script.Append(@"
                // Define OEIsMobile for backward compatibility with Kentico 12 scripts
                if (typeof OEIsMobile == 'undefined') {
                   window.OEIsMobile = false;
                }

                function RefreshTree()
                {
                }


                function EditMe(editNodeId, culture)
                {
                    console.log('EditMe');
                    if (!CheckChanges()) { return false; }
                    modalDialog('" + editUrl + @"?dialog=1&nodeid=' + editNodeId + '&culture=' + culture, 'editpage', '90%', '90%');
                }");

                ScriptHelper.RegisterJQueryDialog(Page);

                // Register OnSiteEdit script file
                ScriptHelper.RegisterScriptFile(Page, "DesignMode/OnSiteEdit.js");
                ScriptHelper.RegisterScriptFile(Page, "~/CMSScripts/jquery/jquery-url.js");

                ControlsHelper.RegisterClientScriptBlock(this, Page, typeof(string), "OnSiteEditActions", ScriptHelper.GetScript(script.ToString()));
            }
        }

        /// <summary>
        /// Builds edit icon, but only for existing guids
        /// --------------------------------------------------------------------------------
        /// We don't want an author in create mode clicking to edit an existing page b/c once we
        /// figure out the how to refresh on Save/Close the parent will refresh without saving
        /// </summary>
        public string GetEditIcon(string ItemId)
        {
            if (!String.IsNullOrEmpty(Request.QueryString["nodeid"]))
            {
                var culture = GetEditFormCulture();
                return $"<span><a href=\"javascript: EditMe('{ItemId}', '{culture}');\"><i aria-hidden=\"true\" class=\"icon-edit cms-icon-50\" title=\"Edit Item\"></i></a></span>";
            }
            else
            {
                return String.Empty;
            }
        }

        public void RaisePostBackEvent(string eventArgument)
        {
            switch (eventArgument)
            {
                case "refresh":
                    // Reload the data without raising changed event
                    break;

                case "reload":
                    // Reload the data
                    InitializeRepeater(GetDelimiterWrappedValue());
                    break;
            }

            // Raise form engine user control Changed event
            RaiseOnChanged();
        }

    #endregion Edit dialog

    #region Item class declaration

        /// <summary>
        /// Represents displayed guid
        /// </summary>
        protected class Item
        {
            public string ItemId { get; set; }
            public string ItemText { get; set; }
            public string ItemValue { get; set; }
            public string ItemState { get; set; }
        }

    #endregion Item class declaration
    }
}
