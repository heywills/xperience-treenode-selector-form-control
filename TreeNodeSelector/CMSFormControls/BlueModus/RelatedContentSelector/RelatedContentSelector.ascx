<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="RelatedContentSelector.ascx.cs" Inherits="CMSApp.CMSFormControls.RelatedContentSelector.RelatedContentSelector" %>

<asp:UpdatePanel runat="server" EnableViewState="false">
    <ContentTemplate>
        <div id="<%= this.ClientID %>">
            <asp:HiddenField runat="server" ID="HiddenValue" EnableViewState="false" />
            <asp:HiddenField runat="server" ID="HiddenSafeValueSet" EnableViewState="false" />
            <asp:HiddenField ID="HiddenIdentifier" runat="server" EnableViewState="false" />
            <asp:HiddenField ID="HiddenHash" runat="server" EnableViewState="false" />

            <cms:MessagesPlaceHolder ID="LocalMessagePlaceholder" runat="server" ContainerCssClass="alerts-container" WarningBasicCssClass="alert-status-error" ErrorBasicCssClass="alert-status-error"  />

            <ul class="related-items">
                <asp:Repeater runat="server" ID="ItemListRepeater" EnableViewState="false">
                    <ItemTemplate>
                        <li id='<%# SorableItemIDPrefix %><%# Container.ItemIndex + 1 %>' data-display='<%# Eval("ItemText") %>' data-value='<%# Eval("ItemValue") %>' class="sortedItem">
                            <div class="wrapLeft">
                                <span class="item-status"><%# GetStatusIcon(Eval("ItemState")) %></span>
                                <%# Eval("ItemText") %>
                            </div>
                            <div class="wrapRight">
                                <%# GetEditIcon(Eval("ItemId").ToString()) %>
                                <span><i aria-hidden="true" class="remove-item icon-bin cms-icon-50" title="Remove Item"></i></span>
                            </div>
                        </li>
                    </ItemTemplate>
                </asp:Repeater>
            </ul>

            <asp:Literal runat="server" ID="InfoLiteral"></asp:Literal>

            <div class="btn-actions keep-white-space-fixed">
                <cms:LocalizedButton ID="AddItemsButton" runat="server" ButtonStyle="Default" UseSubmitBehavior="false"
                    EnableViewState="False" ResourceString="general.additems" />
            </div>
        </div>

        <%--
            Implement the jQuery UI Sortable widget with the list created
            by the bound repeater
        --%>
        <script type="text/javascript">
            $cmsj(function () {
                var itemSeparator = ";";
                // ID of control (so that multiple form controls can be used on the same page)
                var parentControlId = '<%= this.ClientID %>';
                var hiddenResultId = '<%= HiddenValue.ClientID %>';

                initializeSortable();
                // Reinitialize Sortable and event handling after
                // partial postbacks.
                Sys.WebForms.PageRequestManager.getInstance().add_endRequest(initializeSortable);

                function initializeSortable() {
                    var parentControl = document.getElementById(parentControlId);
                    var hiddenResultControl = document.getElementById(hiddenResultId);

                    var $parentControl = $cmsj(parentControl);
                    var $sortableList = $parentControl.find(".related-items");


                    // initialize the JQuery UI sortable list
                    $sortableList.disableSelection();

                    $sortableList.sortable({
                        stop: function (event, ui) {
                            UpdateHiddenField();
                        },
                    });

                    // remove item from DOM upon click
                    $sortableList.find(".remove-item").click(function () {
                        $cmsj(this).closest('li').remove();
                        UpdateHiddenField();
                    });

                    // updates value in Hidden field based on current sortable state
                    function UpdateHiddenField() {
                        var valueOfHiddenField = "";
                        var idsInOrder = $sortableList.sortable("toArray");
                        if (idsInOrder.length > 0) {
                            valueOfHiddenField = itemSeparator;
                        }
                        $cmsj.each(idsInOrder, function (index, elementID) {
                            var element = $sortableList.find("#" + elementID);
                            var itemValue = element.data("value");
                            valueOfHiddenField += itemValue + itemSeparator;
                        });
                        hiddenResultControl.value = valueOfHiddenField;
                        // Notify Kentico content manager.
                        window.Changed();
                    }
                }
            });
        </script>
    </ContentTemplate>
</asp:UpdatePanel>