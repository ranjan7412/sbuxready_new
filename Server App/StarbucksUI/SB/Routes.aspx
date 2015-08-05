<%@ Page Language="VB" MasterPageFile="~/SB/MasterPage.master" AutoEventWireup="false"
    CodeFile="Routes.aspx.vb" Inherits="SB_Routes" Title="SB" EnableEventValidation="false" %>

<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="asp" %>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="Server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder_Menu" runat="server">
    <span id="tabid" style="display: none">tabRoutes</span>
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="ContentPlaceHolder1" runat="Server">
    <asp:UpdateProgress ID="UpdateProgress1" runat="server" AssociatedUpdatePanelID="updatePanel1">
        <ProgressTemplate>
            <div class="divWaiting">
                <asp:Label ID="lblWait" runat="server" Text=" Please wait... " ForeColor="Blue" />
                <asp:Image ID="imgWait" runat="server" ImageAlign="Middle" ImageUrl="~/Common/Images/loader.gif"
                    Height="10%" /></div>
        </ProgressTemplate>
    </asp:UpdateProgress>
    <asp:UpdatePanel ID="updatePanel1" runat="server">
        <Triggers>
            <asp:PostBackTrigger ControlID="btnUpload" />
        </Triggers>
        <ContentTemplate>
            <div>
                <div style="padding-bottom: 30px">
                    <h1 style="font-size: 20px; line-height: 14px; font-family: Arial; margin-top: 7px;">
                        Routes</h1>
                    <span style="color: #CFCFCF;">This will show routes category</span>
                </div>
                <asp:Label ID="lblStatus" runat="server" ForeColor="Green"></asp:Label>
                <div style="text-align: right; padding-right: 8px">
                    <asp:FileUpload ID="FileUpload1" runat="server" />
                    <asp:Button ID="btnUpload" runat="server" Text="Upload" />
                    <asp:Button ID="btnExport" runat="server" Text="Export" />
                    <asp:Button ID="btnRefresh" runat="server" Text="Refresh" />
                </div>
            </div>
            <div style="width: 58px; height: 30px; background-color: #007042; float: left">
                <asp:Button ID="btnAdd" runat="server" Text="Add" BorderColor="Transparent" CssClass="GridTop" />
            </div>
            <asp:Panel ID="Panel3" runat="server" DefaultButton="btnFilter" Style="width: 644px;
                height: 26px; background-color: #007042; float: left; color: White; padding-top: 4px">
                Filter:<asp:TextBox ID="txtFilter" runat="server" /><asp:Button ID="btnClear" runat="server"
                    Text="x" OnClick="btnClear_Click" /><asp:Button ID="btnFilter" runat="server" OnClick="btnFilter_Click"
                        Style="display: none" />
            </asp:Panel>
            <div style="width: 58px; height: 30px; background-color: #007042; float: left">
            </div>
            <asp:ModalPopupExtender ID="ModalPopupExtender1" runat="server" TargetControlID="btnAdd"
                CancelControlID="btnClose" PopupControlID="Panel2" BackgroundCssClass="modalBackground">
            </asp:ModalPopupExtender>
            <asp:Panel ID="Panel2" runat="server" CssClass="modalPopup" align="center" Style="display: none">
                <div style="height: 30px; background-color: #007042;">
                    <asp:Label ID="Label1" runat="server" Text="Create Route" ForeColor="White"></asp:Label>
                </div>
                <div style="padding-top: 10px">
                    <asp:Label ID="Label2" runat="server" Text="Route Name:  "></asp:Label>
                    <asp:TextBox ID="txtRouteName" runat="server"></asp:TextBox>
                    <asp:RequiredFieldValidator ID="RequiredFieldValidator1" runat="server" ControlToValidate="txtRouteName"
                        ErrorMessage="*" Style="color: Red" ValidationGroup="vgCreateRoute">                    
                    </asp:RequiredFieldValidator>
                </div>
                <div style="padding-top: 10px">
                    <asp:Label ID="Label3" runat="server" Text="Select CDC:  "></asp:Label>
                    <asp:DropDownList ID="dpdnCDC" runat="server" Width="160px">
                    </asp:DropDownList>
                </div>
                <div style="padding-top: 10px; padding-bottom: 10px; height: 150px">
                    <asp:Label ID="Label4" runat="server" Text="Select Stores: "></asp:Label>
                    <asp:ListBox ID="lstStores" runat="server" SelectionMode="Multiple" Height="150px">
                    </asp:ListBox>
                    <asp:RequiredFieldValidator ID="RequiredFieldValidator2" runat="server" InitialValue=""
                        ControlToValidate="lstStores" ErrorMessage="*" Style="color: Red" ValidationGroup="vgCreateRoute">                    
                    </asp:RequiredFieldValidator>
                </div>
                <div style="height: 30px; background-color: #007042; padding-top: 10px;">
                    <asp:Button ID="btnCreate" runat="server" Text="Create" ValidationGroup="vgCreateRoute" />
                    <asp:Button ID="btnClose" runat="server" Text="Close" />
                </div>
            </asp:Panel>
            <div style="width: 306px; height: 30px; background-color: #007042; float: left">
                <asp:Label ID="Label11" runat="server" Text="Details" CssClass="GridTop"></asp:Label>
            </div>
            <div style="width: 758px; height: 600px; overflow: auto; float: left">
                <asp:GridView ID="gvRoutes" runat="server" BorderColor="Gray" Font-Size="Small" AutoGenerateColumns="false"
                    OnSelectedIndexChanged="OnSelectedIndexChanged" OnRowDataBound="OnRowDataBound">
                </asp:GridView>
            </div>
            <div style="height: 598px; float: left; border: 1px solid Gray">
                <asp:Panel ID="Panel1" runat="server">
                    <div>
                        <asp:Button ID="btnActivate" BackColor="#007042" ForeColor="White" runat="server"
                            Visible="false" />
                        <asp:HiddenField ID="hdnRouteId" runat="server" Visible="false" />
                    </div>
                    <div>
                        <asp:TextBox ID="txtRouteDetails" runat="server" Width="300px" Height="592px" BorderColor="Transparent"
                            TextMode="MultiLine" ReadOnly="true"></asp:TextBox></div>
                </asp:Panel>
            </div>
            <asp:Panel ID="Panel4" runat="server" DefaultButton="btnPageNumber" ForeColor="White"
                Font-Size="11px">
                <div id="dvButtons" runat="server" style="background-color: #007042; clear: both;
                    width: 1066px;">
                    <asp:Button ID="btnFirst" runat="server" Text="|<" />
                    <asp:Button ID="btnPrevious" runat="server" Text="<" />
                    <span style="padding-left: 10px">Page</span>
                    <asp:TextBox ID="txtPageNumber" runat="server" Width="20px" Font-Size="11px"></asp:TextBox>
                    <asp:Button ID="btnPageNumber" runat="server" OnClick="btnPageNumber_Click" Style="display: none" />
                    of
                    <asp:Label ID="lblPageNumber" runat="server"></asp:Label>
                    <asp:Button ID="btnNext" runat="server" Text=">" />
                    <asp:Button ID="btnLast" runat="server" Text=">|" />
                    <span style="padding-left: 350px">Displaying</span>
                    <asp:Label ID="lblFirstRecord" runat="server"></asp:Label>
                    -
                    <asp:Label ID="lblLastRecord" runat="server"></asp:Label>
                    of
                    <asp:Label ID="lblTotalRecords" runat="server"></asp:Label>
                </div>
            </asp:Panel>
            <asp:HiddenField ID="hdnTotalRows" runat="server" Visible="false" />
            <asp:HiddenField ID="hdnPageIndex" runat="server" Visible="false" />
        </ContentTemplate>
    </asp:UpdatePanel>
</asp:Content>
