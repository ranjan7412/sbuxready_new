using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class _Default : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        Starbucks.StarbucksServices objService = new Starbucks.StarbucksServices();
         obj.DotNetGetAllRouteMappings(0,20,"1");


        DataTable dtPhotoSearch;
        dtPhotoSearch = objService.GetPhotos("", 0, 10);

        Starbucks.LoginResponse objLogin;


        objLogin = objService.LoginForAdminPanel("ttestm", "1234");

    }
}