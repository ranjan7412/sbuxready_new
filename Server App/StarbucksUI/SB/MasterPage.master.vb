
Partial Class SB_MasterPage
    Inherits System.Web.UI.MasterPage
    Protected Sub Page_Init(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Init
        'If Not IsPostBack Then
        If (Session("Username") Is Nothing) Then
            Response.Redirect("Login.aspx")
        End If
        'End If
    End Sub

    Protected Sub btnLogout_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnLogout.Click
        Session("Username") = Nothing
        Session("UserType") = Nothing
        Session("ProviderID") = Nothing
        Response.Redirect("Login.aspx")
    End Sub
End Class

