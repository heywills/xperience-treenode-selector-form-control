﻿using System;
using CMS.Base;
using CMS.DocumentEngine;
using System.Linq;
using CMS.Base.Web.UI;
using CMS.Core;
using CMS.DataEngine;
using CMS.FormEngine;
using CMS.Helpers;
using CMS.LicenseProvider;
using CMS.Localization;
using CMS.Membership;
using CMS.PortalEngine;
using CMS.SiteProvider;
using CMS.UIControls;
using CMS.WorkflowEngine;

namespace CMSApp.CMSFormControls.RelatedContentSelector.Dialogs
{
    public partial class Edit : CMSContentPage
    {
        #region "Variables"

        protected bool newdocument = false;
        protected bool newculture = false;
        protected bool mShowToolbar = false;

        protected DataClassInfo ci = null;

        #endregion


        #region "Protected variables"

        /// <summary>
        /// Local page message placeholder
        /// </summary>
        public override MessagesPlaceHolder MessagesPlaceHolder
        {
            get
            {
                return plcMess;
            }
        }


        /// <summary>
        /// Class identifier for new documents.
        /// </summary>
        protected int ClassID
        {
            get
            {
                return QueryHelper.GetInteger("classid", 0);
            }
        }


        /// <summary> 
        /// TemplateID, used when Use template selection is enabled for class of newly created document.
        /// </summary>
        protected int TemplateID
        {
            get
            {
                return QueryHelper.GetInteger("templateid", -1);
            }
        }


        /// <summary>
        /// Identifier of parent document. (For newly created documents.)
        /// </summary>
        protected int ParentNodeID
        {
            get
            {
                return QueryHelper.GetInteger("parentnodeid", 0);
            }
        }


        /// <summary>
        /// Culture of parent document. (For newly created documents.)
        /// </summary>
        protected string ParentCulture
        {
            get
            {
                return QueryHelper.GetString("parentculture", LocalizationContext.PreferredCultureCode);
            }
        }


        /// <summary>
        /// Indicates if e-commerce product section is edited.
        /// </summary>
        protected bool ProductSection
        {
            get
            {
                return (Mode != null) && (Mode.ToLowerCSafe() == "productssection") && (ci != null) && (ci.ClassIsProductSection);
            }
        }


        /// <summary>
        /// Identifies if the page is used for products UI
        /// </summary>
        protected override bool IsProductsUI
        {
            get
            {
                return ProductSection;
            }
        }

        #endregion


        #region "Page events"

        protected override void OnPreInit(EventArgs e)
        {
            // Do not redirect for non-existing document if new culture version is being created
            DocumentManager.RedirectForNonExistingDocument = (Action != "newculture");

            base.OnPreInit(e);
        }


        protected override void OnInit(EventArgs e)
        {
            var currentUser = MembershipContext.AuthenticatedUser;

            // Check UIProfile
            if (!currentUser.IsAuthorizedPerUIElement("CMS.Content", new[] { "EditForm", "Edit" }, SiteContext.CurrentSiteName))
            {
                RedirectToUIElementAccessDenied("CMS.Content", "EditForm");
            }

            base.OnInit(e);

            DocumentManager.OnAfterAction += DocumentManager_OnAfterAction;
            DocumentManager.OnLoadData += DocumentManager_OnLoadData;

            // Register scripts
            string script = "function " + formElem.ClientID + "_RefreshForm(){" + Page.ClientScript.GetPostBackEventReference(btnRefresh, "") + " }";
            ScriptHelper.RegisterClientScriptBlock(this, typeof(string), formElem.ClientID + "_RefreshForm", ScriptHelper.GetScript(script));

            ScriptHelper.RegisterCompletePageScript(this);
            ScriptHelper.RegisterLoader(this);
            ScriptHelper.RegisterDialogScript(this);

            formElem.OnBeforeDataLoad += formElem_OnBeforeDataLoad;
            formElem.OnAfterDataLoad += formElem_OnAfterDataLoad;

            // Update view mode
            if (!RequiresDialog)
            {
                PortalContext.ViewMode = ViewModeEnum.EditForm;
            }

            // Analyze the action parameter
            switch (Action)
            {
                case "new":
                case "convert":
                    {
                        newdocument = true;

                        // Check if document type is allowed under parent node
                        if ((ParentNodeID > 0) && (ClassID > 0))
                        {
                            // Check class
                            ci = DataClassInfoProvider.GetDataClassInfo(ClassID);
                            if (ci == null)
                            {
                                throw new Exception("[Content/Edit.aspx]: Class ID '" + ClassID + "' not found.");
                            }

                            if (!LicenseHelper.LicenseVersionCheck(RequestContext.CurrentDomain, FeatureEnum.Documents, ObjectActionEnum.Insert))
                            {
                                RedirectToAccessDenied(String.Format(GetString("cmsdesk.documentslicenselimits"), ""));
                            }

                            // Check if need template selection, if so, then redirect to template selection page
                            int templateId = TemplateID;
                            if (!ProductSection && ci.ClassShowTemplateSelection && (templateId < 0))
                            {
                                URLHelper.Redirect("~/CMSModules/Content/CMSDesk/TemplateSelection.aspx" + RequestContext.CurrentQueryString);
                            }

                            // Set default template ID
                            // formElem.DefaultPageTemplateId = (templateId > 0) ? templateId : ci.ClassDefaultPageTemplateID;

                            string newClassName = ci.ClassName;
                            formElem.FormName = newClassName + ".default";

                            DocumentManager.Mode = FormModeEnum.Insert;
                            DocumentManager.ParentNodeID = ParentNodeID;
                            DocumentManager.NewNodeCultureCode = ParentCulture;
                            DocumentManager.NewNodeClassID = ClassID;

                            // Check allowed document type
                            TreeNode parentNode = DocumentManager.ParentNode;
                            if ((parentNode == null) || !DocumentHelper.IsDocumentTypeAllowed(parentNode, ClassID))
                            {
                                AddNotAllowedScript("child");
                            }

                            if (!currentUser.IsAuthorizedToCreateNewDocument(DocumentManager.ParentNode, DocumentManager.NewNodeClassName))
                            {
                                AddNotAllowedScript("new");
                            }
                        }

                        if (RequiresDialog)
                        {
                            SetTitle(GetString("Content.NewTitle"));
                        }
                        else
                        {
                            EnsureDocumentBreadcrumbs(PageBreadcrumbs, action: String.Format(GetString("content.newdocument"), ci.ClassDisplayName));
                        }
                    }
                    break;

                case "newculture":
                    {
                        newculture = true;
                        int nodeId = QueryHelper.GetInteger("nodeid", 0);
                        DocumentManager.Mode = FormModeEnum.InsertNewCultureVersion;
                        formElem.NodeID = nodeId;

                        // Check permissions
                        bool authorized = false;
                        if (nodeId > 0)
                        {
                            // Get the node                    
                            TreeNode treeNode = DocumentManager.Tree.SelectSingleNode(nodeId);
                            if (treeNode != null)
                            {
                                DocumentManager.NewNodeClassID = treeNode.GetIntegerValue("NodeClassID", 0);
                                DocumentManager.ParentNodeID = ParentNodeID;
                                DocumentManager.NewNodeCultureCode = ParentCulture;
                                DocumentManager.SourceDocumentID = QueryHelper.GetInteger("sourcedocumentid", 0);
                                authorized = currentUser.IsAuthorizedToCreateNewDocument(treeNode.NodeParentID, treeNode.NodeClassName);

                                if (authorized)
                                {
                                    string className = DocumentManager.NewNodeClassName;

                                    if (!LicenseHelper.LicenseVersionCheck(RequestContext.CurrentDomain, FeatureEnum.Documents, ObjectActionEnum.Insert))
                                    {
                                        RedirectToAccessDenied(String.Format(GetString("cmsdesk.documentslicenselimits"), ""));
                                    }

                                    ci = DataClassInfoProvider.GetDataClassInfo(className);
                                    formElem.FormName = className + ".default";
                                }

                                // Setup page title
                                PageTitle.TitleText = GetString("Content.NewCultureVersionTitle");
                            }
                        }

                        if (!authorized)
                        {
                            AddNotAllowedScript("newculture");
                        }

                        if (RequiresDialog)
                        {
                            SetTitle(GetString("content.newcultureversiontitle"));
                        }
                        else
                        {
                            if (!IsProductsUI)
                            {
                                EnsureDocumentBreadcrumbs(PageBreadcrumbs, action: GetString("content.newcultureversiontitle"));
                            }
                        }
                    }
                    break;

                default:
                    {
                        TreeNode node = Node;
                        if (node == null)
                        {
                            RedirectToNewCultureVersionPage();
                        }
                        else
                        {
                            EnableSplitMode = true;
                            DocumentManager.Mode = FormModeEnum.Update;
                            ci = DataClassInfoProvider.GetDataClassInfo(node.NodeClassName);
                            if (RequiresDialog)
                            {
                                menuElem.ShowSaveAndClose = true;

                                // Add the document name to the properties header title
                                string nodeName = node.GetDocumentName();
                                // Get name for root document
                                if (node.IsRoot())
                                {
                                    nodeName = SiteContext.CurrentSite.DisplayName;
                                }

                                SetTitle(GetString("Content.EditTitle") + " \"" + HTMLHelper.HTMLEncode(ResHelper.LocalizeString(nodeName)) + "\"");
                            }
                        }
                    }
                    break;
            }

            formElem.Visible = true;

            // Display / hide the CK editor toolbar area
            FormInfo fi = FormHelper.GetFormInfo(ci.ClassName, false);

            if (fi.UsesHtmlArea())
            {
                // Add script to display toolbar
                if (formElem.HtmlAreaToolbarLocation.ToLowerCSafe() == "out:cktoolbar")
                {
                    mShowToolbar = true;
                }
            }

            // Init form for product section edit
            if (ProductSection)
            {
                // Form prefix
                formElem.FormPrefix = "ecommerce";

                // Hide Apply workflow action
                menuElem.ShowApplyWorkflow = false;
            }

            if (RequiresDialog)
            {
                plcCKFooter.Visible = false;
            }
        }


        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            formElem.MessagesPlaceHolder.ClearLabels();

            InitBindSkuAction();
        }


        protected override void OnPreRender(EventArgs e)
        {
            base.OnPreRender(e);

            // Register script files
            ScriptHelper.RegisterTooltip(this);
            ScriptHelper.RegisterEditScript(Page);

            bool isNew = Action == "new";

            // Title and breadcrumbs for product section edit
            if (ProductSection)
            {
                TreeNode node = DocumentManager.Node;

                // Title
                PageTitle.TitleText = GetString(isNew ? "com.productsection.new" : "com.productsection.properties");

                if (!isNew)
                {
                    if (newculture)
                    {
                        EnsureDocumentBreadcrumbs(PageBreadcrumbs, action: GetString("content.newcultureversiontitle"));
                    }
                    else
                    {
                        EnsureDocumentBreadcrumbs(PageBreadcrumbs, node);
                    }
                }
            }
            else if (isNew)
            {
                PageTitle.TitleText = HTMLHelper.HTMLEncode(String.Format(GetString("content.newdocument"), ci.ClassDisplayName));
            }

            if (!newdocument && !newculture)
            {
                formElem.Enabled = DocumentManager.AllowSave;
                btnBindSku.Enabled = DocumentManager.AllowSave;
            }
        }

        #endregion


        #region "Document manager events"

        protected void DocumentManager_OnAfterAction(object sender, DocumentManagerEventArgs e)
        {
            TreeNode node = e.Node;

            int newNodeId = node.NodeID;
            if (newdocument || newculture)
            {
                // Store error text
                if (!string.IsNullOrEmpty(formElem.MessagesPlaceHolder.ErrorText))
                {
                    SessionHelper.SetValue("FormErrorText|" + newNodeId, formElem.MessagesPlaceHolder.ErrorText);
                }
            }
            else
            {
                // Reload the values in the form
                formElem.LoadControlValues();
                string eventArgument = Page.Request.Form["__EVENTARGUMENT"].ToLower();
                if(eventArgument.IndexOf("save;saveandclose") >= 0)
                {
                    Page.ClientScript.RegisterStartupScript(this.GetType(), "CallToReturnToParent", "returnToParent()", true);
                }
            }
        }


        protected void DocumentManager_OnLoadData(object sender, DocumentManagerEventArgs e)
        {
            formElem.LoadControlValues();
        }

        #endregion


        #region "Methods"

        /// <summary>
        /// Adds script for redirecting to NotAllowed page.
        /// </summary>
        /// <param name="action">Action string</param>
        private void AddNotAllowedScript(string action)
        {
            AddScript("window.location.replace('../NotAllowed.aspx?action=" + action + "')");
        }


        /// <summary>
        /// Adds the script to the output request window.
        /// </summary>
        /// <param name="script">Script to add</param>
        public override void AddScript(string script)
        {
            ScriptHelper.RegisterStartupScript(this, typeof(string), script.GetHashCode().ToString(), ScriptHelper.GetScript(script));
        }


        protected void btnRefresh_Click(object sender, EventArgs e)
        {
            TreeNode node = DocumentManager.Node;

            // Check permission to modify document
            if (MembershipContext.AuthenticatedUser.IsAuthorizedPerDocument(node, NodePermissionsEnum.Modify) == AuthorizationResultEnum.Allowed)
            {
                // Ensure version for later detection whether node is published
                node.VersionManager.EnsureVersion(node, node.IsPublished);

                // Tree refresh is needed only if node was archived or published
                WorkflowStepInfo currentStep = node.WorkflowStep;
                bool refreshTree = (currentStep != null) && (currentStep.StepIsArchived || currentStep.StepIsPublished);

                // Move to edit step
                node.MoveToFirstStep();

                // Refresh tree
                if (refreshTree)
                {
                    ScriptHelper.RefreshTree(this, node.NodeID, node.NodeID);
                }

                // Reload form
                formElem.LoadForm(true);

                if (DocumentManager.SaveChanges)
                {
                    ScriptHelper.RegisterStartupScript(this, typeof(string), "moveToEditStepChange", ScriptHelper.GetScript("Changed();"));
                }
            }
        }


        protected void formElem_OnBeforeDataLoad(object sender, EventArgs e)
        {
            bool hasCategories = (formElem.FormInformation != null) && formElem.FormInformation.ItemsList.OfType<FormCategoryInfo>().Any();
            if (hasCategories)
            {
                formElem.DefaultFormLayout = FormLayoutEnum.Divs;
                formElem.DefaultCategoryName = ResHelper.GetString("general.general");
            }
        }


        protected void formElem_OnAfterDataLoad(object sender, EventArgs e)
        {
            TreeNode node = DocumentManager.Node;
            if (node != null)
            {
                // Show stored error message
                string frmError = SessionHelper.GetValue("FormErrorText|" + node.NodeID) as string;
                if (!string.IsNullOrEmpty(frmError))
                {
                    formElem.ShowError(frmError);
                    // Remove error message
                    SessionHelper.Remove("FormErrorText|" + node.NodeID);
                }
            }
        }


        private void InitBindSkuAction()
        {
            if (RequiresDialog)
            {
                return;
            }

            if ((DocumentManager.NodeID > 0) && (DocumentManager.Node != null) && (Action != "newculture"))
            {
                var dataClass = DataClassInfoProvider.GetDataClassInfo(DocumentManager.Node.ClassName);
                if ((dataClass != null) && ModuleEntryManager.IsModuleLoaded(ModuleName.ECOMMERCE))
                {
                    plcSkuBinding.Visible = dataClass.ClassIsProduct;

                    btnBindSku.Click += (sender, args) =>
                    {
                        string url = "~/CMSModules/Ecommerce/Pages/Content/Product/Product_Edit_General.aspx";
                        url = URLHelper.AddParameterToUrl(url, "nodeid", DocumentManager.NodeID.ToString());
                        url = URLHelper.AddParameterToUrl(url, "action", "bindsku");
                        if (RequiresDialog)
                        {
                            url = URLHelper.AddParameterToUrl(url, "dialog", "1");
                        }
                        URLHelper.ResponseRedirect(url);
                    };
                }
            }
        }

        #endregion
    }

}
