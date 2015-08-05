<%@ Page Language="VB" MasterPageFile="~/SB/MasterPage.master" AutoEventWireup="false"
    CodeFile="PhotoSearch.aspx.vb" Inherits="SB_PhotoSearch" Title="SB" %>

<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="asp" %>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="Server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder_Menu" runat="server">
    <span id="tabid" style="display: none">tabPhotoSearch</span>
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
        <ContentTemplate>
            <div>
                <div style="padding-bottom: 30px">
                    <h1 style="font-size: 20px; line-height: 14px; font-family: Arial; margin-top: 7px;">
                        Search Photos</h1>
                    <span style="color: #CFCFCF;">Use this tab to retrieve photos</span>
                </div>
                <div>
                    <div style="float: left; padding-right: 42px">
                        <asp:Label ID="Label1" runat="server" Text="Delivery ID"></asp:Label>
                    </div>
                    <div style="float: left">
                        <asp:TextBox ID="txtDeliveryIdFrom" runat="server"></asp:TextBox>
                    </div>
                    <div style="float: left; padding-right: 10px">
                        <asp:Label ID="Label2" runat="server" Text="(From)"></asp:Label>
                    </div>
                    <div style="float: left">
                        <asp:TextBox ID="txtDeliveryIdTo" runat="server"></asp:TextBox>
                    </div>
                    <div style="float: left;">
                        <asp:Label ID="Label3" runat="server" Text="(To)"></asp:Label>
                    </div>
                    <div style="float: left; padding-left: 50px; padding-right: 12px">
                        <asp:Label ID="Label4" runat="server" Text="Child Reason"></asp:Label>
                    </div>
                    <div style="float: left;">
                        <asp:DropDownList ID="dpdnChildReasonCodes" runat="server" Width="160px">
                        </asp:DropDownList>
                    </div>
                    <div style="float: left; padding-left: 50px;">
                        <asp:ImageButton ID="imgBtnSearch" runat="server" ImageUrl="~/Common/Images/Search.png" />
                    </div>
                </div>
                <div style="clear: both">
                    <div style="float: left; padding-right: 83px">
                        <asp:Label ID="Label5" runat="server" Text="Date"></asp:Label>
                    </div>
                    <div style="float: left">
                        <asp:TextBox ID="txtFromDate" runat="server"></asp:TextBox>
                        <asp:CalendarExtender ID="CalendarExtender1" TargetControlID="txtFromDate" runat="server">
                        </asp:CalendarExtender>
                    </div>
                    <div style="float: left; padding-right: 10px">
                        <asp:Label ID="Label6" runat="server" Text="(From)"></asp:Label>
                    </div>
                    <div style="float: left">
                        <asp:TextBox ID="txtToDate" runat="server"></asp:TextBox>
                        <asp:CalendarExtender ID="CalendarExtender2" TargetControlID="txtToDate" runat="server">
                        </asp:CalendarExtender>
                    </div>
                    <div style="float: left;">
                        <asp:Label ID="Label7" runat="server" Text="(To)"></asp:Label>
                    </div>
                    <div style="float: left; padding-left: 50px; padding-right: 42px">
                        <asp:Label ID="Label8" runat="server" Text="Provider"></asp:Label>
                    </div>
                    <div style="float: left;">
                        <asp:DropDownList ID="dpdnProviders" runat="server" Width="160px">
                        </asp:DropDownList>
                    </div>
                    <div style="float: left; padding-left: 50px;">
                        <asp:ImageButton ID="imgBtnExport" runat="server" ImageUrl="~/Common/Images/Export.png" />
                    </div>
                </div>
                <div style="clear: both">
                    <div style="float: left; padding-right: 84px">
                        <asp:Label ID="Label9" runat="server" Text="Time"></asp:Label>
                    </div>
                    <div style="float: left">
                        <asp:DropDownList ID="dpdnFromHours" runat="server">
                        </asp:DropDownList>
                    </div>
                    <div style="float: left; padding-right: 127px">
                        <asp:Label ID="Label10" runat="server" Text="(From)"></asp:Label>
                    </div>
                    <div style="float: left">
                        <asp:DropDownList ID="dpdnToHours" runat="server">
                        </asp:DropDownList>
                    </div>
                    <div style="float: left;">
                        <asp:Label ID="Label11" runat="server" Text="(To)"></asp:Label>
                    </div>
                    <div style="float: left; padding-left: 164px; padding-right: 32px">
                        <asp:Label ID="Label12" runat="server" Text="Username"></asp:Label>
                    </div>
                    <div style="float: left;">
                        <asp:TextBox ID="txtUsername" runat="server" Width="152px"></asp:TextBox>
                    </div>
                    <div style="float: left; padding-left: 50px;">
                        <asp:ImageButton ID="imgBtnClear" runat="server" ImageUrl="~/Common/Images/Clear.png" />
                    </div>
                </div>
                <div style="clear: both">
                    <div style="float: left; padding-right: 24px">
                        <asp:Label ID="Label13" runat="server" Text="Store Number"></asp:Label>
                    </div>
                    <div style="float: left">
                        <asp:TextBox ID="txtStoreNo" runat="server"></asp:TextBox>
                    </div>
                    <div style="float: left; padding-right: 12px; padding-left: 6px">
                        <asp:Label ID="Label14" runat="server" Text="Route"></asp:Label>
                    </div>
                    <div style="float: left">
                        <asp:TextBox ID="txtRoute" runat="server"></asp:TextBox>
                    </div>
                    <div style="float: left; padding-right: 66px; padding-left: 80px">
                        <asp:Label ID="Label15" runat="server" Text="CDC"></asp:Label>
                    </div>
                    <div style="float: left;">
                        <asp:DropDownList ID="dpdnCDC" runat="server" Width="160px">
                        </asp:DropDownList>
                    </div>
                </div>
                <div style="clear: both; padding-bottom: 50px">
                    <div style="float: left; padding-right: 28px">
                        <asp:Label ID="Label16" runat="server" Text="Store Type"></asp:Label>
                    </div>
                    <div style="float: left; padding-left: 12px">
                        <asp:TextBox ID="txtStoreType" runat="server"></asp:TextBox>
                    </div>
                </div>
            </div>
            <div style="clear: both; background-color: #E4EAED; overflow:auto; height:600px">
                <asp:GridView ID="gvPhotoSearch" runat="server" AutoGenerateColumns="false" BorderColor="Gray"
                    Font-Size="Small">
                    <HeaderStyle ForeColor="White" BackColor="#007042" Font-Size="Small"></HeaderStyle>
                    <Columns>
                        <asp:TemplateField HeaderText="Photo" ItemStyle-BorderColor="Gray">
                            <ItemTemplate>
                                <a href="Redirect.aspx?id=<%#Eval("PhotoId") %>" target="_blank">
                                    <img src="../photos/<%#Eval("PhotoId")%>.jpg" width="50px" height="50px" /></a>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:BoundField DataField="DeliveryCode" HeaderText="Delivery Id">
                            <ItemStyle BorderColor="Gray" />
                        </asp:BoundField>
                        <asp:TemplateField HeaderText="Date" ItemStyle-BorderColor="Gray">
                            <ItemTemplate>
                                <%#IIf(Not IsDBNull(Eval("CompletedDate")) AndAlso Eval("CompletedDate") >= "01/01/2015", Eval("CompletedDate"), Eval("DateAdded"))%>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:BoundField DataField="StoreNumber" HeaderText="Store No">
                            <ItemStyle BorderColor="Gray" />
                        </asp:BoundField>
                        <asp:BoundField DataField="StoreName" HeaderText="Store Name">
                            <ItemStyle BorderColor="Gray" />
                        </asp:BoundField>
                        <asp:BoundField DataField="StoreOwnershipType" HeaderText="Store Type">
                            <ItemStyle BorderColor="Gray" />
                        </asp:BoundField>
                        <asp:BoundField DataField="CDCName" HeaderText="CDC">
                            <ItemStyle BorderColor="Gray" />
                        </asp:BoundField>
                        <asp:BoundField DataField="ChildReasonName" HeaderText="Child Reason">
                            <ItemStyle BorderColor="Gray" />
                        </asp:BoundField>
                        <asp:BoundField DataField="ProviderName" HeaderText="Provider">
                            <ItemStyle BorderColor="Gray" />
                        </asp:BoundField>
                        <asp:BoundField DataField="Username" HeaderText="User">
                            <ItemStyle BorderColor="Gray" />
                        </asp:BoundField>
                        <asp:BoundField DataField="RouteName" HeaderText="Route" ItemStyle-BorderColor="Green">
                            <ItemStyle BorderColor="Gray" />
                        </asp:BoundField>
                    </Columns>
                </asp:GridView>
            </div>
            <asp:Panel ID="Panel5" runat="server" DefaultButton="btnPageNumber" ForeColor="White"
                Font-Size="11px">
                <div id="dvButtons" runat="server" style="clear: both; width: 1066px;" visible="false">
                    <asp:Button ID="btnFirst" runat="server" Text="|<" />
                    <asp:Button ID="btnPrevious" runat="server" Text="<" />
                    <span style="color: #007042; font-weight: bold; padding-left: 10px">Page</span>
                    <asp:TextBox ID="txtPageNumber" runat="server" Width="30px" Font-Size="11px"></asp:TextBox>
                    <asp:Button ID="btnPageNumber" runat="server" Style="display: none" OnClick="btnPageNumber_Click" />
                    <span style="color: #007042; font-weight: bold;">of</span>
                    <asp:Label ID="lblPageNumber" runat="server" ForeColor="#007042" Font-Bold="true"></asp:Label>
                    <asp:Button ID="btnNext" runat="server" Text=">" />
                    <asp:Button ID="btnLast" runat="server" Text=">|" />
                    <span style="color: #007042; font-weight: bold; padding-left: 325px">Displaying</span>
                    <asp:Label ID="lblFirstRecord" runat="server" ForeColor="#007042" Font-Bold="true"></asp:Label>
                    <span style="color: #007042; font-weight: bold;">-</span>
                    <asp:Label ID="lblLastRecord" runat="server" ForeColor="#007042" Font-Bold="true"></asp:Label>
                    <span style="color: #007042; font-weight: bold;">of</span>
                    <asp:Label ID="lblTotalRecords" runat="server" ForeColor="#007042" Font-Bold="true"></asp:Label>
                </div>
            </asp:Panel>
        </ContentTemplate>
    </asp:UpdatePanel>
</asp:Content>
