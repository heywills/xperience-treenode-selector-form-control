<%-- 
    This page is provided by the Nuget package XperienceCommunity.TreeNodeSelectorFormControl.
    Changes to this file will be lost when this package is restored or updated.
--%>
<%@ Page Language="C#" AutoEventWireup="false" CodeBehind="TreeSelectorDialog.aspx.cs" Inherits="CMSApp.CMSFormControls.BlueModus.RelatedContentSelector.TreeSelectorDialog.TreeSelectorDialog" Theme="default" MasterPageFile="~/CMSMasterPages/UI/Dialogs/ModalDialogPage.master" Title="Selection dialog" ValidateRequest="false"%>
<%@ Register Src="~/CMSAdminControls/UI/Trees/UniTree.ascx" TagName="UniTree" TagPrefix="cms" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="plcContent" runat="Server">
    <asp:Panel ID="BodyPanel" runat="server" CssClass="UniSelectorDialogBody Categories">
        <cms:CMSUpdatePanel runat="server" ID="UpdatePanel" UpdateMode="Conditional">
            <ContentTemplate>
                <asp:Panel ID="SearchDialogPanel" runat="server">
                    <div class="form-horizontal form-filter">
                        <div class="form-group form-group-buttons">
                            <div class="filter-form-value-cell">
                                <cms:CMSTextBox ID="SearchTermBox" MaxLength="50" runat="server" WatermarkCssClass="WatermarkText" WatermarkText="Search by name"/>
                                <cms:CMSRegularExpressionValidator ID="SearchTermBoxValidator" ValidationGroup="SearchValidationGroup" runat="server" Display="Dynamic" ControlToValidate="SearchTermBox" EnableClientScript="true" ValidationExpression="\S{3,}.*$" Text="Please provide at least 3 characters." />
                                <asp:HiddenField ID="HiddenCurrentSearchTerm" runat="server" EnableViewState="true" />
                            </div>
                            <div class="filter-form-buttons-cell-narrow">
                                <cms:LocalizedButton ID="SearchButton" ButtonStyle="Primary" ResourceString="general.search"
                                    EnableViewState="false" runat="server" ValidationGroup="SearchValidationGroup" CausesValidation="true" OnClick="SearchButton_Click" />
                                <cms:LocalizedButton ID="ResetSearchButton" ButtonStyle="Default" ResourceString="general.reset"
                                    EnableViewState="false" runat="server" OnClick="ResetSearchButton_Click" />
                            </div>
                        </div>
                    </div>
                </asp:Panel>
                <asp:PlaceHolder runat="server" ID="InstructionsPanel">
                </asp:PlaceHolder>
                <asp:Panel runat="server" ID="TreePanel">
                    <div class="ContentTree">
                        <asp:Panel ID="TreeAreaPanel" runat="server" CssClass="TreeAreaTree">
                            <cms:UniTree runat="server" ID="TreeControl" ShortID="tg" Localize="true" IsLiveSite="false" OnOnNodeCreated="TreeControl_OnNodeCreated" />
                            <div class="ClearBoth">
                            </div>
                        </asp:Panel>
                    </div>
                </asp:Panel>
            </ContentTemplate>
        </cms:CMSUpdatePanel>
        <cms:CMSUpdatePanel runat="server" ID="HiddenPanel" UpdateMode="Conditional">
            <ContentTemplate>
                <asp:HiddenField ID="HiddenValueField" runat="server" EnableViewState="false" />
                <asp:HiddenField ID="HiddenHashField" runat="server" EnableViewState="false" />
            </ContentTemplate>
        </cms:CMSUpdatePanel>
    </asp:Panel>
</asp:Content>
