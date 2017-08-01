<%@ Control Language="C#" AutoEventWireup="true" %>
<%@ Register Assembly="Wokhan.WebControls" Namespace="Wokhan.WebControls" TagPrefix="uc" %>
<div id="divMain" class="ExtendedGridView" runat="server">
    <asp:Label ID="lblCat" CssClass="ExtendedGridViewTitle" runat="server" />
    <asp:Timer runat="server" Enabled="false" ID="timeInfos" Interval="1000" />
    <div class="ExtendedGridViewTimer" id="divTimer" runat="server">
        <asp:CheckBox ID="chkTimer" runat="server" AutoPostBack="true" />
    </div>
    <div class="ExtendedGridViewFilterOwner" id="divFilter" runat="server">
        <asp:Panel CssClass="ExtendedGridViewFilterTarget" runat="server" ID="pnlFilterOn">
            <asp:Label ID="lblFilterOn" runat="server" AssociatedControlID="ddlFilterOn" />
            <uc:ExtendedDropDownList ID="ddlFilterOn" CssClass="ExtendedGridViewFilterOn" runat="server"
                AutoPostBack="true" />
        </asp:Panel>
        <asp:Repeater ID="rptDetailedSearch" runat="server">
            <ItemTemplate>
                <div class="ExtendedGridViewFilterValue">
                    <asp:Label ID="lblFilter" CssClass="ExtendedGridViewFilterTitle" runat="server" />
                    <asp:TextBox ID="txtFilter" CssClass="ExtendedGridViewFilter" runat="server" />
                    <asp:DropDownList ID="ddlFilter" CssClass="ExtendedGridViewFilter" runat="server" />
                </div>
            </ItemTemplate>
        </asp:Repeater>
        <asp:Button ID="btnSearch" CssClass="ExtendedGridViewButtonSearch" Text="Search" runat="server" />
        <asp:Button ID="btnClear" CssClass="ExtendedGridViewButtonClear" Text="Clear" runat="server" OnClientClick="ExtendedGridView.ClearFields();" />
    </div>
    <%--<asp:HiddenField ID="comp" runat="server" />--%>
    <asp:UpdatePanel ID="uplInfos" runat="server" UpdateMode="Conditional">
        <Triggers>
            <asp:AsyncPostBackTrigger ControlID="timeInfos" EventName="Tick" />
            <asp:AsyncPostBackTrigger ControlID="chkTimer" EventName="CheckedChanged" />
            <asp:AsyncPostBackTrigger ControlID="ddlFilterOn" EventName="SelectedIndexChanged" />
            <asp:AsyncPostBackTrigger ControlID="btnSearch" EventName="Click" />
            <asp:AsyncPostBackTrigger ControlID="btnClear" EventName="Click" />
        </Triggers>
        <ContentTemplate>
            <asp:HiddenField ID="hidCtrlDown" runat="server" />
            <asp:UpdateProgress ID="uplPanelProgress" runat="server" DynamicLayout="false">
                <ProgressTemplate>
                    <div class="ExtendedGridViewLoading"></div>
                </ProgressTemplate>
            </asp:UpdateProgress>
            <asp:GridView AllowSorting="true" ID="gridEv" CssClass="ExtendedGridViewGrid"
                Style="clear: both" runat="server" AutoGenerateColumns="true" EnableViewState="false"
                CellSpacing="1" CellPadding="4" HeaderStyle-CssClass="ExtendedGridViewHeader"
                FooterStyle-CssClass="ExtendedGridViewFooter" PagerSettings-Position="Bottom" PagerStyle-CssClass="ExtendedGridViewPager"
                EmptyDataRowStyle-CssClass="ExtendedGridViewEmptyDataRow" ShowFooter="true" GridLines="None" RowStyle-CssClass="ExtendedGridViewRow"
                AlternatingRowStyle-CssClass="ExtendedGridViewRowAlternate" ShowHeaderWhenEmpty="true">
                <EmptyDataTemplate>
                    <asp:Label Text="No data found." runat="server" />
                </EmptyDataTemplate>
            </asp:GridView>
        </ContentTemplate>
    </asp:UpdatePanel>
</div>
