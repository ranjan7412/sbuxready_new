<%@ Page Language="VB" AutoEventWireup="false" CodeFile="Login.aspx.vb" Inherits="SB_Login" %>

<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="asp" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html class="x-border-box x-quirks x-viewport">
<head>
    <title>SB</title>
    <link rel="stylesheet" href="../Common/Styles/app.css" />
     <link rel="stylesheet" href="../Common/Styles/aspnet.css" />
</head>
<body id="ext-gen1018" class="x-body x-webkit x-chrome x-reset x-masked x-box-layout-ct x-container"
    style="padding-top: 70px">
    <form id="form1" runat="server">
    <asp:ScriptManager ID="ScriptManager1" runat="server">
    </asp:ScriptManager>
    <asp:UpdateProgress ID="UpdateProgress1" runat="server" AssociatedUpdatePanelID="updatePanel1">
    <ProgressTemplate>
        <div class="divWaiting">
            <asp:Label ID="lblWait" runat="server" Text=" Please wait... " ForeColor="Blue" />
            <asp:Image ID="imgWait" runat="server" ImageAlign="Middle" ImageUrl="~/Common/Images/loader.gif"
                Height="10%" /></div>
    </ProgressTemplate>
</asp:UpdateProgress>
    <asp:UpdatePanel ID="UpdatePanel1" runat="server">
        <ContentTemplate>
            <div id="mainviewport-innerCt" class="x-box-inner " role="presentation" style="height: 526px;
                width: 1366px;">
                <div id="mainviewport-targetEl" style="position: absolute; left: 0px; top: 0px; height: 1px;
                    width: 1366px;">
                    <div class="x-panel x-box-item x-panel-default" id="panel-1009" style="margin-top: 0px;
                        margin-right: 0px; margin-bottom: 0px; margin-left: 0px; width: 206px; height: 198px;
                        left: 580px; top: -6.5px;">
                        <div id="panel-1009-body" class="x-panel-body x-panel-body-default x-panel-body-default"
                            style="width: 206px; height: 198px; left: 0px; top: 0px;">
                            <img src="../Common/Images/starbucks-logo.jpg">
                            <div id="panel-1009-clearEl" class="x-clear" role="presentation">
                            </div>
                        </div>
                    </div>
                    <div class="x-panel starbucksLoginForm x-box-item x-panel-default" id="loginform"
                        style="margin-top: 0px; margin-right: 0px; margin-bottom: 0px; margin-left: 0px;
                        left: 528px; top: 211.5px; width: 310px; height: 141px;">
                        <div id="loginform-body" class="x-panel-body x-panel-body-default x-panel-body-default"
                            style="left: 0px; top: 0px; width: 310px; height: 141px;">
                            <div class="x-panel x-panel-default-framed" style="width: 310px; height: 141px;"
                                id="form-1010">
                                <div style="text-align: center">
                                    <asp:Label ID="lblMessage" runat="server" ForeColor="DarkRed"></asp:Label>
                                </div>
                                <div id="form-1010-body" class="x-panel-body x-panel-body-default-framed x-panel-body-default-framed x-docked-noborder-top x-docked-noborder-right x-docked-noborder-bottom x-docked-noborder-left"
                                    style="padding-top: 26px; padding-right: 26px; padding-bottom: 26px; padding-left: 26px;
                                    left: 0px; top: 0px; width: 302px; height: 133px;">
                                    <table class="x-field x-form-item x-field-default x-anchor-form-item" style="border-top-width: 0px;
                                        border-right-width: 0px; border-bottom-width: 0px; border-left-width: 0px; width: 250px;
                                        table-layout: auto;" cellpadding="0" id="uname">
                                        <tbody>
                                            <tr id="uname-inputRow">
                                                <td id="uname-labelCell" style="display: none;" valign="top" halign="left" width="105"
                                                    class="x-field-label-cell">
                                                    <label id="uname-labelEl" for="uname-inputEl" class="x-form-item-label x-form-item-label-left"
                                                        style="width: 100px; margin-right: 5px;">
                                                    </label>
                                                </td>
                                                <td class="x-form-item-body " id="uname-bodyEl" colspan="3" role="presentation">
                                                    <%--<input id="uname-inputEl" type="text" name="uname-inputEl" placeholder="username"
                                                style="width: 100%; -webkit-user-select: text;" class="x-form-field x-form-empty-field x-form-required-field x-form-text "
                                                autocomplete="off" aria-invalid="false">--%>
                                                    <asp:TextBox  ID="txtUsername" runat="server" ForeColor="Black" Style="width: 100%;
                                                        -webkit-user-select: text;" class="x-form-field x-form-empty-field x-form-required-field x-form-text "
                                                        autocomplete="off" aria-invalid="false" placeholder="username"></asp:TextBox>
                                                </td>
                                                <td id="uname-sideErrorCell" valign="middle" style="display: none;" width="18">
                                                    <asp:RequiredFieldValidator ID="RequiredFieldValidator1" runat="server" Text="*"
                                                        ControlToValidate="txtUsername"></asp:RequiredFieldValidator>
                                                    <div id="uname-errorEl" class="x-form-error-msg x-form-invalid-icon" style="width: 18px;
                                                        display: none;" data-errorqtip="">
                                                    </div>
                                                </td>
                                            </tr>
                                        </tbody>
                                    </table>
                                    <table class="x-field x-form-item x-field-default x-anchor-form-item" style="border-top-width: 0px;
                                        border-right-width: 0px; border-bottom-width: 0px; border-left-width: 0px; width: 250px;
                                        table-layout: auto;" cellpadding="0" id="pass">
                                        <tbody>
                                            <tr id="pass-inputRow">
                                                <td id="pass-labelCell" style="display: none;" valign="top" halign="left" width="105"
                                                    class="x-field-label-cell">
                                                    <label id="pass-labelEl" for="pass-inputEl" class="x-form-item-label x-form-item-label-left"
                                                        style="width: 100px; margin-right: 5px;">
                                                    </label>
                                                </td>
                                                <td class="x-form-item-body " id="pass-bodyEl" colspan="3" role="presentation">
                                                    <asp:TextBox ID="txtPassword" runat="server" TextMode="Password" placeholder="password"
                                                        Style="width: 100%; -webkit-user-select: text;" class="x-form-field x-form-empty-field x-form-required-field x-form-text "
                                                        autocomplete="off" aria-invalid="false" ForeColor="Black"></asp:TextBox>
                                                </td>
                                                <td id="pass-sideErrorCell" valign="middle" style="display: none;" width="18">
                                                    <div id="pass-errorEl" class="x-form-error-msg x-form-invalid-icon" style="width: 18px;
                                                        display: none;" data-errorqtip="">
                                                    </div>
                                                </td>
                                            </tr>
                                        </tbody>
                                    </table>
                                    <div class="x-panel x-panel-default" style="padding-top: 0px; padding-right: 0px;
                                        padding-bottom: 0px; padding-left: 0px; width: 250px; height: 27px;" id="panel-1011">
                                        <div id="panel-1011-body" class="x-panel-body x-panel-body-default x-panel-body-default"
                                            style="width: 250px; height: 27px; left: 0px; top: 0px;">
                                            <%--<button id="submitData" class="login">Login</button>--%>
                                            <asp:Button ID="btnLogin" runat="server" CssClass="login" Text="Login" />
                                            <%--<asp:PopupControlExtender ID="PopupControlExtender1" TargetControlID=btnLogin  runat="server">
                                            </asp:PopupControlExtender>--%>
                                            <div id="panel-1011-clearEl" class="x-clear" role="presentation">
                                            </div>
                                        </div>
                                    </div>
                                    <div id="form-1010-overflowPadderEl" style="font-size: 1px; width: 1px; height: 1px;
                                        display: none;">
                                    </div>
                                </div>
                            </div>
                            <div id="loginform-overflowPadderEl" style="font-size: 1px; width: 1px; height: 1px;
                                display: none;">
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </ContentTemplate>
    </asp:UpdatePanel>
    </form>
</body>
</html>
