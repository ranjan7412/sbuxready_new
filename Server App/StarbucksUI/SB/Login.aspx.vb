
Partial Class SB_Login
    Inherits System.Web.UI.Page

    Protected Sub btnLogin_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnLogin.Click
        Dim objSvc As New Starbucks.StarbucksClient()
        Dim objLogin As Starbucks.LoginResponse

        ' Login to Web Tool
        objLogin = objSvc.LoginForAdminPanel(txtUsername.Text, txtPassword.Text)
        If (objLogin.statusCode = 0) Then
            Session("Username") = objLogin.user.username
            Session("UserType") = objLogin.user.userType
            Session("ProviderID") = objLogin.user.associatedID
            ' Redirect to first tab
            Response.Redirect("PhotoSearch.aspx")
        Else
            lblMessage.Text = objLogin.statusDescription
        End If

    End Sub
End Class
