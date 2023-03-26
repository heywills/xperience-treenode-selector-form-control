<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Edit.aspx.cs" Inherits="CMSApp.CMSFormControls.RelatedContentSelector.Dialogs.Edit"     
    ValidateRequest="false" Theme="Default" EnableEventValidation="false" 
    MasterPageFile="~/CMSMasterPages/UI/SimplePage.master" %>

<%@ Register Src="~/CMSModules/Content/Controls/EditMenu.ascx" TagName="editmenu"
    TagPrefix="cms" %>
<asp:Content ID="cntMenu" ContentPlaceHolderID="plcBeforeContent" runat="server">
    <cms:editmenu ID="menuElem" ShortID="m" runat="server" ShowProperties="false" ShowSpellCheck="true"
        IsLiveSite="false" />
</asp:Content>
<asp:Content ID="cntBody" ContentPlaceHolderID="plcContent" runat="server">
    <div id="CMSHeaderDiv" class="shadow-holder">
        <div id="CKToolbar">
        </div>
    </div>
    <asp:Panel runat="server" ID="pnlContent" CssClass="ContentEditArea">
        <cms:MessagesPlaceHolder runat="server" ID="plcMess" />
        <cms:CMSForm runat="server" ID="formElem" Visible="false" HtmlAreaToolbarLocation="Out:CKToolbar"
            ShowOkButton="false" IsLiveSite="false" ShortID="f" MarkRequiredFields="true"/>
        <span class="ClearBoth"></span>
        <br />
        <%-- SKU binding --%>
        <asp:PlaceHolder ID="plcSkuBinding" runat="server" Visible="false">
            <div class="PageSeparator">
                <cms:LocalizedLabel ID="lblBindSKUInfo" runat="server" ResourceString="com.bindAnSkuInfo"
                    CssClass="InfoLabel EditingFormLabel" EnableViewState="false" />
                <cms:LocalizedButton ID="btnBindSku" runat="server" ButtonStyle="Default" EnableViewState="false" ResourceString="com.skubinding.bind" />
                <span class="ClearBoth"></span>
            </div>
        </asp:PlaceHolder>
    </asp:Panel>
    <asp:Button ID="btnRefresh" runat="server" CssClass="HiddenButton" EnableViewState="false"
        OnClick="btnRefresh_Click" UseSubmitBehavior="false" />
    <asp:PlaceHolder ID="plcCKFooter" runat="server">
        <div id="CMSFooterDiv">
            <div id="CKFooter">
            </div>
        </div>
    </asp:PlaceHolder>
    <script type="text/javascript">
        function returnToParent(){
             return CloseDialog(true);
        }
    </script>
</asp:Content>