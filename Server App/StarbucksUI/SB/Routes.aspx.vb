Imports System.Data
Imports System.IO
Imports System.Linq


Partial Class SB_Routes
    Inherits System.Web.UI.Page
    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        Page.Form.Attributes.Add("enctype", "multipart/form-data")
        lblStatus.Text = String.Empty

        If (Not IsPostBack) Then
            hdnTotalRows.Value = 0
            hdnPageIndex.Value = 0
            PopulateCDC()
            'PopulateStores()
            CreateGridStructure()
            PopulateDataTable(0, 50)
            PopulateGrid()
        End If

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


    End Sub
    Public Sub PopulateStores() ' Populate Stores in listbox
        Dim objService As Starbucks.StarbucksClient = New Starbucks.StarbucksClient()
        Dim objResp As Starbucks.ResponseStoreList
        objResp = objService.GetAllStores()

        lstStores.DataSource = objResp.stores
        lstStores.DataValueField = "storeID"
        lstStores.DataTextField = "storeNumber"
        lstStores.DataBind()

    End Sub
    Protected Sub btnCreate_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnCreate.Click
        ' Create new route
        Dim objSvc As New Starbucks.StarbucksClient
        Dim objResp As New Starbucks.Response

        Dim objRoute As New Starbucks.Route
        objRoute.routeName = txtRouteName.Text

        objRoute.cdc = New Starbucks.CDC
        objRoute.cdc.id = Convert.ToInt32(dpdnCDC.SelectedValue)

        'objRoute.stores = New Generic.List(Of Starbucks.Store)
        For Each lstItem As ListItem In lstStores.Items
            If (lstItem.Selected) Then
                Dim objStore As New Starbucks.Store
                objStore.storeID = lstItem.Value
                objRoute.stores.ToList().Add(objStore)
            End If
        Next
        objResp = objSvc.CreateRoute(objRoute)
        If (objResp.statusCode <> 0) Then
            lblStatus.Text = objResp.statusDescription
        Else
            lblStatus.Text = "Route Created Successfully"
        End If
        PopulateDataTable(hdnPageIndex.Value *  50, 50)
        PopulateGrid()
    End Sub

    Private Sub CreateGridStructure()
        Dim bfStatus As New BoundField()
        bfStatus.HeaderText = "Status"
        bfStatus.DataField = "Status"
        bfStatus.HeaderStyle.BorderColor = Drawing.Color.Gray
        bfStatus.HeaderStyle.BackColor = Drawing.Color.LightGray
        bfStatus.HeaderStyle.Font.Bold = False
        bfStatus.ItemStyle.BorderColor = Drawing.Color.Gray
        gvRoutes.Columns.Add(bfStatus)

        Dim bfRoute As New BoundField()
        bfRoute.HeaderText = "Route Name"
        bfRoute.DataField = "RouteName"
        bfRoute.HeaderStyle.BorderColor = Drawing.Color.Gray
        bfRoute.HeaderStyle.BackColor = Drawing.Color.LightGray
        bfRoute.HeaderStyle.Font.Bold = False
        bfRoute.ItemStyle.BorderColor = Drawing.Color.Gray
        gvRoutes.Columns.Add(bfRoute)

        Dim bfCDC As New BoundField()
        bfCDC.HeaderText = "CDC"
        bfCDC.DataField = "CDCName"
        bfCDC.HeaderStyle.BorderColor = Drawing.Color.Gray
        bfCDC.HeaderStyle.BackColor = Drawing.Color.LightGray
        bfCDC.HeaderStyle.Font.Bold = False
        bfCDC.ItemStyle.BorderColor = Drawing.Color.Gray
        gvRoutes.Columns.Add(bfCDC)

        For i = 1 To 35
            Dim bfStop As New BoundField()
            bfStop.HeaderText = "Stop " & i
            bfStop.DataField = "Stop" & i
            bfStop.HeaderStyle.BorderColor = Drawing.Color.Gray
            bfStop.HeaderStyle.BackColor = Drawing.Color.LightGray
            bfStop.HeaderStyle.Font.Bold = False
            bfStop.ItemStyle.BorderColor = Drawing.Color.Gray
            gvRoutes.Columns.Add(bfStop)
        Next
    End Sub

    Private Sub PopulateDataTable(ByVal startIndex As Int32, ByVal maxRows As Int32)
        Dim objService As Starbucks.StarbucksClient = New Starbucks.StarbucksClient()
        Dim objRoutesList As Starbucks.ResponseRouteList

        ' Check if filter is applied or not
        If String.IsNullOrEmpty(txtFilter.Text.Trim) Then
            objRoutesList = objService.DotNetGetAllRouteMappings(startIndex, maxRows, Session("ProviderID"))
        Else
            objRoutesList = objService.DotNetGetAllFilteredRouteMappings(txtFilter.Text.Trim, startIndex, maxRows, Session("ProviderID"))
        End If

        If Not objRoutesList.routes Is Nothing Then

            Session("RoutesList") = objRoutesList

            Dim dt As New DataTable
            dt.TableName = "Routes"

            dt.Columns.Add("Status", GetType(String))
            dt.Columns.Add("RouteName", GetType(String))
            dt.Columns.Add("CDCName", GetType(String))

            For j = 1 To 35
                dt.Columns.Add("Stop" & j, GetType(String))
            Next

            For i = 0 To objRoutesList.routes.Count - 1
                Dim row = dt.NewRow
                row("Status") = IIf(objRoutesList.routes(i).routeStatus = 1, "Active", "Deactivated")
                row("RouteName") = objRoutesList.routes(i).routeName
                row("CDCName") = objRoutesList.routes(i).cdcName

                For j = 0 To objRoutesList.routes(i).stores.Count - 1
                    row("Stop" & j + 1) = objRoutesList.routes(i).stores(j).storeNumber
                Next
                dt.Rows.Add(row)
            Next

            hdnTotalRows.Value = objRoutesList.numberOfRecords
            Session("RoutesData") = dt
        Else
            Session("RoutesList") = Nothing
            Session("RoutesData") = Nothing
        End If

        ' Enable/Disable navigation buttons
        If hdnPageIndex.Value = 0 Then
            btnPrevious.Enabled = False
            btnFirst.Enabled = False
        Else
            btnPrevious.Enabled = True
            btnFirst.Enabled = True
        End If
        If (hdnPageIndex.Value = Math.Truncate(hdnTotalRows.Value / 50) Or (hdnPageIndex.Value + 1 = Math.Truncate(hdnTotalRows.Value / 50) And hdnTotalRows.Value Mod 50 = 0)) Then
            btnNext.Enabled = False
            btnLast.Enabled = False
        Else
            btnNext.Enabled = True
            btnLast.Enabled = True
        End If

        ' Display Page Count and Row Count
        If (hdnTotalRows.Value Mod 50 = 0) Then
            lblPageNumber.Text = Math.Truncate(hdnTotalRows.Value / 50)
        Else
            lblPageNumber.Text = Math.Truncate(hdnTotalRows.Value / 50) + 1
        End If

        txtPageNumber.Text = hdnPageIndex.Value + 1
        lblFirstRecord.Text = startIndex + 1
        If startIndex + maxRows > hdnTotalRows.Value Then
            lblLastRecord.Text = hdnTotalRows.Value
        Else
            lblLastRecord.Text = startIndex + maxRows
        End If

        lblTotalRecords.Text = hdnTotalRows.Value
    End Sub
    Private Sub PopulateGrid()

        Dim dtRoutes As New DataTable
        dtRoutes = Session("RoutesData")

        gvRoutes.DataSource = dtRoutes
        gvRoutes.DataBind()

        ' If no rows to be displayed
        If dtRoutes Is Nothing Then
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
    Protected Sub OnRowDataBound(ByVal sender As Object, ByVal e As GridViewRowEventArgs)
        If e.Row.RowType = DataControlRowType.DataRow Then
            e.Row.Attributes("onclick") = Page.ClientScript.GetPostBackClientHyperlink(gvRoutes, "Select$" & e.Row.RowIndex)
            e.Row.ToolTip = "Click to select this row."
            e.Row.Attributes.Add("onmouseover", "this.style.backgroundColor='lightgray'")
            e.Row.Attributes.Add("onmouseout", "this.style.backgroundColor='white'")
            e.Row.Attributes.Add("style", "cursor: default")
        End If
    End Sub
    Protected Sub OnSelectedIndexChanged(ByVal sender As Object, ByVal e As EventArgs)
        ' Display route details in right side, on clicking of a paricular Route
        If Session("UserType") = 1 Then
            btnActivate.Visible = True
            If (gvRoutes.SelectedRow.Cells(0).Text = "Active") Then
                btnActivate.Text = "Deactivate"
            ElseIf (gvRoutes.SelectedRow.Cells(0).Text = "Deactivated") Then
                btnActivate.Text = "Activate"
            End If
        End If

        Dim rName As String = Server.HtmlDecode(gvRoutes.SelectedRow.Cells(1).Text)
        Dim objRoutesList As Starbucks.ResponseRouteList = Session("RoutesList")
        Dim route As Starbucks.Route = objRoutesList.routes.ToList().Find(Function(p) p.routeName = rName)

        txtRouteDetails.Text = vbCrLf & "Route: " & vbTab & route.routeName & vbCrLf & vbCrLf & "CDC: " & vbTab & route.cdcName
        For i = 0 To route.stores.Count - 1
            txtRouteDetails.Text &= vbCrLf & vbCrLf & "Store " & route.stores(i).storeNumber & ":" & vbTab & route.stores(i).storeName
        Next

        hdnRouteId.Value = route.routeID

    End Sub

    Protected Sub btnFilter_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        ' Search gridview for routes
        hdnPageIndex.Value = 0

        PopulateDataTable(0, 50)
        PopulateGrid()
    End Sub
    Protected Sub btnClear_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        txtFilter.Text = ""
        hdnPageIndex.Value = 0
        hdnTotalRows.Value = 0
        PopulateDataTable(0, 50)
        PopulateGrid()
    End Sub

    Protected Sub btnExport_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnExport.Click
        ' Export Routes data
        Dim objService As New Starbucks.StarbucksClient

        Dim dtExport As New DataTable

        PopulateDataTable(0, hdnTotalRows.Value)
        dtExport = Session("RoutesData")

        Dim filename As String = objService.ExportRoutes(dtExport)

        If Not String.IsNullOrEmpty(filename) Then
            ScriptManager.RegisterClientScriptBlock(Me, Me.GetType(), "Download", "window.location='" + ConfigurationManager.AppSettings("baseWebURL") + "/downloads/" + filename + "';", True)
        End If

    End Sub

    Protected Sub btnUpload_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnUpload.Click
        ' Bulk upload routes data
        lblStatus.Text = ""
        If (FileUpload1.HasFile) Then

            Dim currentPath As String = HttpContext.Current.Server.MapPath("~")
            Dim currentTime As Long = DateTime.Now.ToFileTimeUtc()
            Dim fileName As String = Path.GetFileName(FileUpload1.FileName)
            fileName = "routes_" & currentTime & "_" & fileName
            Dim finalPath As String = currentPath & "\\uploads\\" & fileName
            FileUpload1.SaveAs(finalPath)


            Dim obj As Starbucks.StarbucksClient = New Starbucks.StarbucksClient()
            Dim objResp As Starbucks.Response = New Starbucks.Response()
            objResp = obj.UploadRoutesDotNet(fileName, "")
            lblStatus.Text = objResp.statusDescription

            PopulateDataTable(0, 50)
            PopulateGrid()

        End If

    End Sub

    Protected Sub btnRefresh_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnRefresh.Click

        lblStatus.Text = ""
        hdnTotalRows.Value = 0
        hdnPageIndex.Value = 0
        txtFilter.Text = ""
        txtRouteDetails.Text = ""
        btnActivate.Visible = False

        PopulateDataTable(0, 50)
        PopulateGrid()
    End Sub

    Protected Sub btnActivate_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnActivate.Click
        ' Activate/Deactivate particular route
        Dim objSvc As New Starbucks.StarbucksClient
        If (btnActivate.Text = "Activate") Then
            objSvc.UpdateRouteStatusToActive(hdnRouteId.Value)
            btnActivate.Text = "Deactivate"
        Else
            objSvc.UpdateRouteStatusToDeactive(hdnRouteId.Value)
            btnActivate.Text = "Activate"
        End If
        PopulateDataTable(hdnPageIndex.Value * 50, 50)
        PopulateGrid()
    End Sub

    Protected Sub btnPageNumber_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        If (IsNumeric(txtPageNumber.Text.Trim) And Not txtPageNumber.Text.Trim.Contains(".") And Convert.ToDouble(txtPageNumber.Text.Trim) > 0) Then
            If Convert.ToInt32(txtPageNumber.Text.Trim) > Math.Truncate(hdnTotalRows.Value / 50) And hdnTotalRows.Value Mod 50 = 0 Then
                txtPageNumber.Text = Math.Truncate(hdnTotalRows.Value / 50)
                hdnPageIndex.Value = Math.Truncate(hdnTotalRows.Value / 50) - 1
            ElseIf Convert.ToInt32(txtPageNumber.Text.Trim) > Math.Truncate(hdnTotalRows.Value / 50) And hdnTotalRows.Value Mod 50 > 0 Then
                txtPageNumber.Text = Math.Truncate(hdnTotalRows.Value / 50) + 1
                hdnPageIndex.Value = Math.Truncate(hdnTotalRows.Value / 50)
            Else
                hdnPageIndex.Value = Convert.ToInt32(txtPageNumber.Text.Trim) - 1
            End If
            PopulateDataTable(hdnPageIndex.Value * 50, 50)
            PopulateGrid()
        End If
    End Sub

    Protected Sub btnFirst_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnFirst.Click
        hdnPageIndex.Value = 0
        PopulateDataTable(hdnPageIndex.Value, 50)
        PopulateGrid()
    End Sub

    Protected Sub btnLast_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnLast.Click
        If (hdnTotalRows.Value Mod 50 = 0) Then
            hdnPageIndex.Value = Math.Truncate(hdnTotalRows.Value / 50) - 1
            PopulateDataTable(hdnPageIndex.Value * 50, 50)
        Else
            hdnPageIndex.Value = Math.Truncate(hdnTotalRows.Value / 50)
            PopulateDataTable(hdnPageIndex.Value * 50, hdnTotalRows.Value Mod 50)
        End If
        PopulateGrid()
    End Sub

    Protected Sub btnPrevious_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnPrevious.Click
        hdnPageIndex.Value = hdnPageIndex.Value - 1
        PopulateDataTable(hdnPageIndex.Value * 50, 50)
        PopulateGrid()
    End Sub

    Protected Sub btnNext_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnNext.Click
        'If hdnPageIndex.Value < Math.Truncate(hdnTotalRows.Value / 50) Then
        hdnPageIndex.Value = hdnPageIndex.Value + 1
        PopulateDataTable(hdnPageIndex.Value * 50, 50)
        PopulateGrid()
        'End If
    End Sub
End Class
