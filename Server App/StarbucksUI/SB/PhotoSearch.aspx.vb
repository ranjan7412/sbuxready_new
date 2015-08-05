Imports System.Data

Partial Class SB_PhotoSearch
    Inherits System.Web.UI.Page
    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        If (Not IsPostBack) Then
            PopulateHours()
            PopulateChildReasonCode()
            PopulateProvider()
            PopulateCDC()
        End If
    End Sub

    Public Sub PopulateHours() ' Populate Hours dropdown
        For index As Integer = 0 To 23
            dpdnFromHours.Items.Add(index.ToString())
            dpdnToHours.Items.Add(index.ToString())
        Next
    End Sub

    Public Sub PopulateChildReasonCode() ' Populate Child Reason Code in dropdown
        Dim objService As Starbucks.StarbucksClient = New Starbucks.StarbucksClient()
        Dim objResp As Starbucks.ResponseReasonWithChildrenList
        objResp = objService.GetAllParentReasonsWithChildren()

        Dim ChildReasonList As New Generic.List(Of Starbucks.ReasonChild)

        For i = 0 To objResp.reasons.Count - 1
            For j = 0 To objResp.reasons(i).children.Count - 1
                ChildReasonList.Add(objResp.reasons(i).children(j))
            Next j
        Next i
        dpdnChildReasonCodes.DataSource = ChildReasonList
        dpdnChildReasonCodes.DataValueField = "childReasonCode"
        dpdnChildReasonCodes.DataTextField = "childReasonName"
        dpdnChildReasonCodes.DataBind()
        dpdnChildReasonCodes.Items.Insert(0, New ListItem("-- Select --"))

    End Sub
    Public Sub PopulateProvider() ' Populate Providers in dropdown
        Dim objService As Starbucks.StarbucksClient = New Starbucks.StarbucksClient()
        Dim objResp As New Starbucks.ResponseProviderList()
        objResp = objService.GetAllProviders()

        Dim ProviderList As New Generic.List(Of Starbucks.Provider)

        For i = 0 To objResp.providers.Count - 1
            ProviderList.Add(objResp.providers(i))
        Next i

        dpdnProviders.DataSource = ProviderList
        dpdnProviders.DataValueField = "providerID"
        dpdnProviders.DataTextField = "providerName"
        dpdnProviders.DataBind()
        dpdnProviders.Items.Insert(0, New ListItem("-- Select --"))

    End Sub
    Public Sub PopulateCDC() ' Populate CDCs in dropdown
        Dim objService As Starbucks.StarbucksClient = New Starbucks.StarbucksClient()
        Dim objResp As Starbucks.ResponseCDCList
        objResp = objService.GetAllCDCs()

        Dim CDCList As New Generic.List(Of Starbucks.CDC)

        For i = 0 To objResp.cdcs.Count - 1
            CDCList.Add(objResp.cdcs(i))
        Next i

        dpdnCDC.DataSource = CDCList
        dpdnCDC.DataValueField = "id"
        dpdnCDC.DataTextField = "name"
        dpdnCDC.DataBind()
        dpdnCDC.Items.Insert(0, New ListItem("-- Select --"))

    End Sub

    Public Sub PopulateGrid(ByVal condition As String, ByVal startIndex As Int32, ByVal maxRows As Int32) ' Populate Gridview with photos listing
        Dim objService As New Starbucks.StarbucksClient
        Dim dtPhotoSearch As New DataTable
        dtPhotoSearch = objService.GetPhotos(condition, startIndex, maxRows)
        If Not dtPhotoSearch Is Nothing Then
            If (ViewState("TotalRows") = 0 And dtPhotoSearch.Rows.Count > 0) Then
                ViewState("TotalRows") = Convert.ToInt32(dtPhotoSearch.Rows(0)("Cnt").ToString())
            End If
            gvPhotoSearch.DataSource = dtPhotoSearch
            gvPhotoSearch.DataBind()
        End If

        ' Enable/Disable navigation buttons
        If ViewState("PageIndex") = 0 Then
            btnPrevious.Enabled = False
            btnFirst.Enabled = False
        Else
            btnPrevious.Enabled = True
            btnFirst.Enabled = True
        End If
        If (ViewState("PageIndex") = Math.Truncate(ViewState("TotalRows") / 50) Or (ViewState("PageIndex") + 1 = Math.Truncate(ViewState("TotalRows") / 50) And ViewState("TotalRows") Mod 50 = 0)) Then
            btnNext.Enabled = False
            btnLast.Enabled = False
        Else
            btnNext.Enabled = True
            btnLast.Enabled = True
        End If

        ' Display Page Count and Row Count
        If (ViewState("TotalRows") Mod 50 = 0) Then
            lblPageNumber.Text = Math.Truncate(ViewState("TotalRows") / 50)
        Else
            lblPageNumber.Text = Math.Truncate(ViewState("TotalRows") / 50) + 1
        End If

        txtPageNumber.Text = ViewState("PageIndex") + 1
        lblFirstRecord.Text = startIndex + 1
        If startIndex + maxRows > ViewState("TotalRows") Then
            lblLastRecord.Text = ViewState("TotalRows")
        Else
            lblLastRecord.Text = startIndex + maxRows
        End If

        lblTotalRecords.Text = ViewState("TotalRows")

        ' If no rows to be displayed
        If dtPhotoSearch.Rows.Count = 0 Then
            btnPrevious.Enabled = False
            btnFirst.Enabled = False
            btnNext.Enabled = False
            btnLast.Enabled = False
            txtPageNumber.Text = 0
            lblPageNumber.Text = 0
            lblFirstRecord.Text = 0
            lblLastRecord.Text = 0
            lblTotalRecords.Text = 0
        End If
    End Sub

    Protected Sub imgBtnSearch_Click(ByVal sender As Object, ByVal e As System.Web.UI.ImageClickEventArgs) Handles imgBtnSearch.Click
        ' Search photos
        dvButtons.Visible = True
        ViewState("TotalRows") = 0
        ViewState("PageIndex") = 0
        ViewState("Condition") = 0

        Dim condition As String = ""

        If Not (String.IsNullOrEmpty(txtDeliveryIdFrom.Text.Trim())) Then
            condition += " And Delivery.DeliveryCode >= " + txtDeliveryIdFrom.Text.Trim() + ""
        End If

        If Not (String.IsNullOrEmpty(txtDeliveryIdTo.Text.Trim())) Then
            condition += " AND Delivery.DeliveryCode <= " + txtDeliveryIdTo.Text.Trim() + ""
        End If

        If Not (String.IsNullOrEmpty(txtFromDate.Text.Trim())) Then
            condition += " AND (Stop.CompletedDate >= '" + txtFromDate.Text.Trim() + " " + dpdnFromHours.Text + ":00'"
            condition += " OR Stop.DateAdded >= '" + txtFromDate.Text.Trim() + " " + dpdnFromHours.Text + ":00')"
        End If

        If Not (String.IsNullOrEmpty(txtToDate.Text.Trim())) Then
            condition += " AND (Stop.CompletedDate <= '" + txtToDate.Text.Trim() + " " + dpdnToHours.Text + ":00'"
            condition += " OR Stop.DateAdded <= '" + txtToDate.Text.Trim() + " " + dpdnToHours.Text + ":00')"
        End If

        If Not (String.IsNullOrEmpty(txtStoreNo.Text.Trim())) Then
            condition += " AND Store.StoreNumber = '" + txtStoreNo.Text.Trim() + "'"
        End If

        If Not (String.IsNullOrEmpty(txtStoreType.Text.Trim())) Then
            condition += " AND Store.StoreOwnershipType = '" + txtStoreType.Text.Trim() + "'"
        End If

        If Not (String.IsNullOrEmpty(txtRoute.Text.Trim())) Then
            condition += " AND Route.RouteName = '" + txtRoute.Text.Trim() + "'"
        End If

        If Not (String.IsNullOrEmpty(txtUsername.Text.Trim())) Then
            condition += " And [User].Username =  '" + txtUsername.Text.Trim() + "'"
        End If

        If (dpdnChildReasonCodes.SelectedIndex <> 0) Then
            condition += " AND ChildReason.ChildReasonID = " + dpdnChildReasonCodes.SelectedValue
        End If

        If (dpdnProviders.SelectedIndex <> 0) Then
            condition += " AND Provider.ProviderID = " + dpdnProviders.SelectedValue
        End If

        If (dpdnCDC.SelectedIndex <> 0) Then
            condition += " AND CDC.CDCID= " + dpdnCDC.SelectedValue
        End If

        If Session("UserType") = 2 Then
            condition += " AND CDC.ProviderID= " + Session("ProviderID")
        End If

        ViewState("Condition") = condition
        PopulateGrid(condition, ViewState("PageIndex"), 50)

    End Sub

    Protected Sub imgBtnExport_Click(ByVal sender As Object, ByVal e As System.Web.UI.ImageClickEventArgs) Handles imgBtnExport.Click
        ' Export searched photos list
        Dim objService As New Starbucks.StarbucksClient

        Dim dtExport As New DataTable
        dtExport = objService.GetPhotos(ViewState("Condition"), 0, ViewState("TotalRows"))

        Dim filename As String = objService.ExportPhotoSearch(dtExport)

        If Not String.IsNullOrEmpty(filename) Then
            ScriptManager.RegisterClientScriptBlock(Me, Me.GetType(), "Download", "window.location='" + ConfigurationManager.AppSettings("baseWebURL") + "/downloads/" + filename + "';", True)
        End If

    End Sub

    Protected Sub imgBtnClear_Click(ByVal sender As Object, ByVal e As System.Web.UI.ImageClickEventArgs) Handles imgBtnClear.Click
        ClearControls()
    End Sub
    Protected Sub ClearControls()
        dvButtons.Visible = False
        ViewState("TotalRows") = 0
        ViewState("PageIndex") = 0
        ViewState("Condition") = 0

        txtDeliveryIdFrom.Text = String.Empty
        txtDeliveryIdTo.Text = String.Empty
        txtFromDate.Text = String.Empty
        txtToDate.Text = String.Empty
        txtStoreNo.Text = String.Empty
        txtStoreType.Text = String.Empty
        txtRoute.Text = String.Empty
        txtUsername.Text = String.Empty

        dpdnChildReasonCodes.SelectedIndex = 0
        dpdnProviders.SelectedIndex = 0
        dpdnCDC.SelectedIndex = 0
        dpdnFromHours.SelectedIndex = 0
        dpdnToHours.SelectedIndex = 0

        gvPhotoSearch.DataSource = Nothing
        gvPhotoSearch.DataBind()
    End Sub

    Protected Sub btnPageNumber_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        If (IsNumeric(txtPageNumber.Text.Trim) And Not txtPageNumber.Text.Trim.Contains(".") And Convert.ToDouble(txtPageNumber.Text.Trim) > 0) Then
            If Convert.ToInt32(txtPageNumber.Text.Trim) > Math.Truncate(ViewState("TotalRows") / 50) And ViewState("TotalRows") Mod 50 = 0 Then
                txtPageNumber.Text = Math.Truncate(ViewState("TotalRows") / 50)
                ViewState("PageIndex") = Math.Truncate(ViewState("TotalRows") / 50) - 1
            ElseIf Convert.ToInt32(txtPageNumber.Text.Trim) > Math.Truncate(ViewState("TotalRows") / 50) And ViewState("TotalRows") Mod 50 > 0 Then
                txtPageNumber.Text = Math.Truncate(ViewState("TotalRows") / 50) + 1
                ViewState("PageIndex") = Math.Truncate(ViewState("TotalRows") / 50)
            Else
                ViewState("PageIndex") = Convert.ToInt32(txtPageNumber.Text.Trim) - 1
            End If
            PopulateGrid(ViewState("Condition"), ViewState("PageIndex") * 50, 50)
        End If
    End Sub
    Protected Sub btnFirst_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnFirst.Click
        ViewState("PageIndex") = 0
        PopulateGrid(ViewState("Condition"), ViewState("PageIndex"), 50)
    End Sub

    Protected Sub btnLast_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnLast.Click
        'ViewState("PageIndex") = Math.Truncate(ViewState("TotalRows") / 50)
        If (ViewState("TotalRows") Mod 50 = 0) Then
            ViewState("PageIndex") = Math.Truncate(ViewState("TotalRows") / 50) - 1
        Else
            ViewState("PageIndex") = Math.Truncate(ViewState("TotalRows") / 50)
        End If
        'PopulateGrid(ViewState("PageIndex") * 50, ViewState("TotalRows") Mod 50)
        PopulateGrid(ViewState("Condition"), ViewState("PageIndex") * 50, 50)
    End Sub

    Protected Sub btnPrevious_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnPrevious.Click
        ViewState("PageIndex") = ViewState("PageIndex") - 1
        PopulateGrid(ViewState("Condition"), ViewState("PageIndex") * 50, 50)
    End Sub

    Protected Sub btnNext_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnNext.Click
        ViewState("PageIndex") = ViewState("PageIndex") + 1
        PopulateGrid(ViewState("Condition"), ViewState("PageIndex") * 50, 50)
    End Sub
   
End Class
