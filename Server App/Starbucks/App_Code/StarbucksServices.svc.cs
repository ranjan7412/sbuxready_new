using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Web.SessionState;
using System.Security.Cryptography;
//using Microsoft.Office.Interop.Excel;
using System.IO;
using System.Web;
using System.ServiceModel.Activation;
using System.Data.OleDb;
using System.Data;
using AntsCode.Util;
using NPOI.HSSF.UserModel;
using NPOI.HPSF;
using NPOI.POIFS.FileSystem;
using NPOI.XSSF.UserModel;
using NPOI.XSSF;
//using NPOI.OOXML;
using System.Reflection;
using OfficeOpenXml;
using NPOI.SS.UserModel;
using System.Net.Mail;
using System.Net.Mime;
using System.Text.RegularExpressions;

namespace Starbucks
{
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    public class StarbucksServices : IStarbucks
    {
        SqlConnection theConnection = null;
        SqlDataReader theReader = null;
        SqlTransaction theTrans = null;

        static String baseWebURL = "http://localhost:39739/Starbucks";
        static String baseURL = "http://localhost:39739/StarbucksServices.svc/";
        static String baseURLForPhotoLink = baseURL + "Photos/View/";

        public void establishDataConnection()
        {
            if (theConnection == null)
            {
                theConnection = new SqlConnection(ConfigurationManager.ConnectionStrings["StarbucksConnectionString"].ConnectionString);
            }
        }

        public void openDataConnection()
        {
            establishDataConnection();

            if (theConnection != null)
            {
                if (theConnection.State != System.Data.ConnectionState.Open)
                {
                    theConnection.Open();
                }
            }
        }

        public void closeDataConnection()
        {
            if (theConnection != null)
            {
                if (theConnection.State != System.Data.ConnectionState.Closed)
                {
                    theConnection.Close();
                }
            }
        }

        public LoginResponse LoginForAdminPanel(string aUsername, string aPassword)
        {
            LoginResponse theResponse = new LoginResponse();

            if (aUsername.Equals("") || aPassword.Equals(""))
            {
                theResponse.statusCode = 1;
                theResponse.statusDescription = "No credentials were provided";

                return theResponse;
            }

            openDataConnection();

            SqlCommand login = new SqlCommand("LoginForAdminPanel", theConnection);
            login.Parameters.Add(new SqlParameter("@username", aUsername));
            login.Parameters.Add(new SqlParameter("@password", aPassword));
            login.CommandType = System.Data.CommandType.StoredProcedure;
            theReader = login.ExecuteReader();

            if (theReader.HasRows)
            {
                while (theReader.Read())
                {
                    theResponse.user = new StarbucksUser();
                    theResponse.user.username = theReader["Username"].ToString();
                    theResponse.user.userType = Int32.Parse(theReader["UserTypeID"].ToString());
                    theResponse.user.emailAddress = theReader["EmailAddress"].ToString();
                    theResponse.user.phoneNumber = theReader["PhoneNumber"].ToString();
                    theResponse.user.firstName = theReader["FirstName"].ToString();
                    theResponse.user.lastName = theReader["LastName"].ToString();
                    theResponse.user.state = Boolean.Parse(theReader["State"].ToString());
                    theResponse.user.associatedID = theReader["AssociatedID"].ToString();
                }

                theReader.Close();

                if (theResponse.user.state == false || theResponse.user.userType == 3)
                {
                    theResponse.statusCode = 2;
                    theResponse.statusDescription = "The specified user is not authorized for access";

                    return theResponse;
                }

                string newSessionID = CreateSession(theResponse.user.username, "2");

                if (!newSessionID.Equals(""))
                {
                    theResponse.statusCode = 0;
                    theResponse.sessionID = newSessionID;
                }
                else
                {
                    theResponse.statusCode = 6;
                    theResponse.statusDescription = "Could not establish session";
                }
            }
            else
            {
                theResponse.statusCode = 4;
                theResponse.statusDescription = "Invalid credentials entered. Please try again";
            }

            closeDataConnection();

            return theResponse;
        }

        public LoginResponse LoginForDevice(string aUsername, string aPassword, string aRouteNumber)
        {
            LoginResponse theResponse = new LoginResponse();

            if (aUsername.Equals("") || aPassword.Equals("") || aRouteNumber.Equals(""))
            {
                theResponse.statusCode = 1;
                theResponse.statusDescription = "No credentials were provided";

                return theResponse;
            }

            openDataConnection();

            SqlCommand login = new SqlCommand("LoginForDevice", theConnection);
            login.Parameters.Add(new SqlParameter("@username", aUsername));
            login.Parameters.Add(new SqlParameter("@password", aPassword));
            login.CommandType = System.Data.CommandType.StoredProcedure;
            theReader = login.ExecuteReader();

            if (theReader.HasRows)
            {
                while (theReader.Read())
                {
                    theResponse.user = new StarbucksUser();
                    theResponse.user.username = theReader["Username"].ToString();
                    theResponse.user.userType = Int32.Parse(theReader["UserTypeID"].ToString());
                    theResponse.user.emailAddress = theReader["EmailAddress"].ToString();
                    theResponse.user.phoneNumber = theReader["PhoneNumber"].ToString();
                    theResponse.user.firstName = theReader["FirstName"].ToString();
                    theResponse.user.lastName = theReader["LastName"].ToString();
                    theResponse.user.state = Boolean.Parse(theReader["State"].ToString());
                    theResponse.user.associatedID = theReader["AssociatedID"].ToString();
                }

                theReader.Close();

                if (theResponse.user.state == false || theResponse.user.userType != 3)
                {
                    theResponse.statusCode = 2;
                    theResponse.statusDescription = "The specified user is not authorized for access";

                    return theResponse;
                }

                string newSessionID = CreateSession(theResponse.user.username, "1");

                if (!newSessionID.Equals(""))
                {
                    theResponse.statusCode = 0;
                    theResponse.sessionID = newSessionID;
                }
                else
                {
                    theResponse.statusCode = 6;
                    theResponse.statusDescription = "Could not establish session";
                }

                SqlCommand checkRoute = new SqlCommand("RouteExists", theConnection);
                checkRoute.Parameters.Add(new SqlParameter("@routeName", aRouteNumber));
                checkRoute.CommandType = System.Data.CommandType.StoredProcedure;
                theReader = checkRoute.ExecuteReader();

                if (!theReader.HasRows)
                {
                    theResponse.statusCode = 4;
                    theResponse.statusDescription = "Route not found";

                    theReader.Close();
                }
                else
                {
                    theReader.Close();

                    if (theResponse.user.associatedID != null && !theResponse.user.associatedID.Equals("NULL") && !theResponse.user.associatedID.Equals(""))
                    {
                        SqlCommand checkRouteAccess = new SqlCommand("RouteAllowed", theConnection);
                        checkRouteAccess.Parameters.AddWithValue("@routeName", aRouteNumber);
                        checkRouteAccess.Parameters.AddWithValue("@username", aUsername);
                        checkRouteAccess.CommandType = System.Data.CommandType.StoredProcedure;

                        theReader = checkRouteAccess.ExecuteReader();

                        if (theReader.HasRows)
                        {
                            theResponse.statusCode = 0;
                            theResponse.statusDescription = "";
                        }
                        else
                        {
                            theResponse.statusCode = 5;
                            theResponse.statusDescription = "The specified user is not allowed to access the specified route";
                        }

                        theReader.Close();
                    }
                }
            }
            else
            {
                theResponse.statusCode = 4;
                theResponse.statusDescription = "Invalid Credentials entered. Please try again";

                return theResponse;
            }

            closeDataConnection();

            return theResponse;
        }

        private string GenerateSessionID()
        {
            int maxSize = 32;

            char[] chars = new char[63];
            chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890-".ToCharArray();
            byte[] data = new byte[1];
            RNGCryptoServiceProvider crypto = new RNGCryptoServiceProvider();
            crypto.GetNonZeroBytes(data);
            data = new byte[maxSize];
            crypto.GetNonZeroBytes(data);
            StringBuilder result = new StringBuilder(maxSize);
            foreach (byte b in data)
            {
                result.Append(chars[b % (chars.Length)]);
            }
            string sessionID = result.ToString();

            return sessionID;
        }

        private string CreateSession(string aUsername, string aClientID)
        {
            string sessionID = "";

            openDataConnection();

            SqlCommand cmdGetClientDetails = new SqlCommand("GetClientDetail", theConnection);
            cmdGetClientDetails.Parameters.Add(new SqlParameter("@clientID", Convert.ToInt32(aClientID)));
            cmdGetClientDetails.CommandType = System.Data.CommandType.StoredProcedure;
            theReader = cmdGetClientDetails.ExecuteReader();

            if (theReader.HasRows)
            {
                theReader.Close();

                sessionID = GenerateSessionID();

                if (!sessionID.Equals(""))
                {
                    SqlCommand cmdCheckSessionID = new SqlCommand(@"SessionIDExists", theConnection);
                    cmdCheckSessionID.Parameters.Add("@sessionID", sessionID);
                    cmdCheckSessionID.CommandType = System.Data.CommandType.StoredProcedure;
                    theReader = cmdCheckSessionID.ExecuteReader();

                    if (!theReader.HasRows)
                    {
                        theReader.Close();

                        SqlCommand cmdCreateSession = new SqlCommand("CreateSession", theConnection);
                        cmdCreateSession.Parameters.Add("@sessionID", sessionID);
                        cmdCreateSession.Parameters.Add("@username", aUsername);
                        cmdCreateSession.Parameters.Add("@clientID", Convert.ToInt32(aClientID));
                        cmdCreateSession.CommandType = System.Data.CommandType.StoredProcedure;

                        cmdCreateSession.ExecuteNonQuery();
                    }
                    else
                    {
                        theReader.Close();

                        sessionID = "";
                    }
                }
            }

            return sessionID;
        }

        public Response Logout(string aUsername, string aSessionID)
        {
            Response theResponse = new Response();

            if (aUsername.Equals("") || aSessionID.Equals(""))
            {
                theResponse.statusCode = 1;
                theResponse.statusDescription = "No credentials were provided";

                return theResponse;
            }

            openDataConnection();

            SqlCommand cmdCheckIfSessionIsOpen = new SqlCommand("IsSessionOpen", theConnection);
            cmdCheckIfSessionIsOpen.Parameters.Add(new SqlParameter("@sessionID", aSessionID));
            cmdCheckIfSessionIsOpen.Parameters.Add(new SqlParameter("@username", aUsername));
            cmdCheckIfSessionIsOpen.CommandType = System.Data.CommandType.StoredProcedure;
            theReader = cmdCheckIfSessionIsOpen.ExecuteReader();

            if (!theReader.HasRows)
            {
                theReader.Close();

                theResponse.statusCode = 6;
                theResponse.statusDescription = "Session does not exist or is already closed";
            }
            else
            {
                theReader.Close();

                SqlCommand cmdLogout = new SqlCommand("Logout", theConnection);
                cmdLogout.Parameters.Add(new SqlParameter("@username", aUsername));
                cmdLogout.Parameters.Add(new SqlParameter("@sessionID", aSessionID));
                cmdLogout.CommandType = System.Data.CommandType.StoredProcedure;
                int numRowsAffected = cmdLogout.ExecuteNonQuery();

                if (numRowsAffected <= 0)
                {
                    theResponse.statusCode = 4;
                    theResponse.statusDescription = "Session does not exist";
                }
                else
                {
                    theResponse.statusCode = 0;
                }
            }

            closeDataConnection();

            return theResponse;
        }

        public ResponseUserList GetAllUsers()
        {
            ResponseUserList theResponse = new ResponseUserList();

            openDataConnection();

            SqlCommand cmdGetAllUsers = new SqlCommand("SELECT \"User\".*, UserType.TypeName, UserAssociation.AssociatedID FROM \"User\" JOIN UserType ON \"User\".UserTypeID = UserType.TypeID LEFT JOIN UserAssociation ON \"User\".Username = UserAssociation.UserName", theConnection);
            theReader = cmdGetAllUsers.ExecuteReader();

            if (theReader.HasRows)
            {
                List<StarbucksUser> listOfUsers = new List<StarbucksUser>();

                while (theReader.Read())
                {
                    StarbucksUser thisUser = new StarbucksUser();

                    thisUser.username = theReader["Username"].ToString();
                    thisUser.firstName = theReader["FirstName"].ToString();
                    thisUser.lastName = theReader["LastName"].ToString();
                    thisUser.phoneNumber = theReader["PhoneNumber"].ToString();
                    thisUser.emailAddress = theReader["EmailAddress"].ToString();
                    thisUser.userType = (int)theReader["UserTypeID"];
                    thisUser.userTypeName = theReader["TypeName"].ToString();
                    thisUser.state = Boolean.Parse(theReader["State"].ToString());
                    thisUser.associatedID = theReader["AssociatedID"].ToString();

                    if (thisUser.userType == 2)
                    {
                        thisUser.associatedFieldName = "Provider";
                    }
                    else if (thisUser.userType == 3)
                    {
                        thisUser.associatedFieldName = "CDC";
                    }

                    listOfUsers.Add(thisUser);
                }

                theResponse.users = listOfUsers;

                theResponse.statusCode = 0;
                theResponse.statusDescription = "";
            }
            else
            {
                theResponse.statusCode = 4;
                theResponse.statusDescription = "There are no users in the database";
            }

            theReader.Close();

            if (theResponse.users != null && theResponse.users.Count > 0)
            {
                for (int i = 0, l = theResponse.users.Count; i < l; i++)
                {
                    StarbucksUser thisUser = theResponse.users[i];

                    if (thisUser != null)
                    {
                        if (!thisUser.associatedID.ToUpper().Equals("NULL") && !thisUser.associatedID.Equals(""))
                        {
                            if (thisUser.userType == 2)
                            {
                                SqlCommand cmdGetAssociatedValue = new SqlCommand("SELECT * FROM Provider WHERE ProviderID = " + thisUser.associatedID, theConnection);

                                theReader = cmdGetAssociatedValue.ExecuteReader();

                                string providerName = "";

                                if (theReader.HasRows)
                                {
                                    while (theReader.Read())
                                    {
                                        providerName = theReader["ProviderName"].ToString();
                                    }
                                }

                                theReader.Close();

                                thisUser.associatedFieldValue = providerName;
                            }
                            else if (thisUser.userType == 3)
                            {
                                SqlCommand cmdGetAssociatedValue = new SqlCommand("SELECT * FROM CDC WHERE CDCID = " + thisUser.associatedID, theConnection);

                                theReader = cmdGetAssociatedValue.ExecuteReader();

                                string cdcName = "";

                                if (theReader.HasRows)
                                {
                                    while (theReader.Read())
                                    {
                                        cdcName = theReader["CDCName"].ToString();
                                    }
                                }

                                theReader.Close();

                                thisUser.associatedFieldValue = cdcName;
                            }
                        }
                    }
                }
            }

            closeDataConnection();

            return theResponse;
        }

        public ResponseUserList GetAllUsersByType(string userType)
        {
            ResponseUserList theResponse = new ResponseUserList();

            openDataConnection();

            SqlCommand cmdGetAllUsers = new SqlCommand("SELECT \"User\".*, UserType.TypeName, UserAssociation.AssociatedID FROM \"User\" JOIN UserType ON \"User\".UserTypeID = UserType.TypeID LEFT JOIN UserAssociation ON \"User\".Username = UserAssociation.Username WHERE UserTypeID = " + userType, theConnection);
            theReader = cmdGetAllUsers.ExecuteReader();

            if (theReader.HasRows)
            {
                List<StarbucksUser> listOfUsers = new List<StarbucksUser>();

                while (theReader.Read())
                {
                    StarbucksUser thisUser = new StarbucksUser();

                    thisUser.username = theReader["Username"].ToString();
                    thisUser.firstName = theReader["FirstName"].ToString();
                    thisUser.lastName = theReader["LastName"].ToString();
                    thisUser.phoneNumber = theReader["PhoneNumber"].ToString();
                    thisUser.emailAddress = theReader["EmailAddress"].ToString();
                    thisUser.userType = (int)theReader["UserTypeID"];
                    thisUser.userTypeName = theReader["TypeName"].ToString();
                    thisUser.state = Boolean.Parse(theReader["State"].ToString());
                    thisUser.associatedID = theReader["AssociatedID"].ToString();

                    if (thisUser.userType == 2)
                    {
                        thisUser.associatedFieldName = "Provider";
                    }
                    else if (thisUser.userType == 3)
                    {
                        thisUser.associatedFieldName = "CDC";
                    }

                    listOfUsers.Add(thisUser);
                }

                theResponse.users = listOfUsers;

                theResponse.statusCode = 0;
                theResponse.statusDescription = "";
            }
            else
            {
                theResponse.statusCode = 4;
                theResponse.statusDescription = "There are no users in the database";
            }

            theReader.Close();

            if (theResponse.users != null && theResponse.users.Count > 0)
            {
                for (int i = 0, l = theResponse.users.Count; i < l; i++)
                {
                    StarbucksUser thisUser = theResponse.users[i];

                    if (thisUser != null)
                    {
                        if (!thisUser.associatedID.ToUpper().Equals("NULL") && !thisUser.associatedID.Equals(""))
                        {
                            if (thisUser.userType == 2)
                            {
                                SqlCommand cmdGetAssociatedValue = new SqlCommand("SELECT * FROM Provider WHERE ProviderID = " + thisUser.associatedID, theConnection);

                                theReader = cmdGetAssociatedValue.ExecuteReader();

                                string providerName = "";

                                if (theReader.HasRows)
                                {
                                    while (theReader.Read())
                                    {
                                        providerName = theReader["ProviderName"].ToString();
                                    }
                                }

                                theReader.Close();

                                thisUser.associatedFieldValue = providerName;
                            }
                            else if (thisUser.userType == 3)
                            {
                                SqlCommand cmdGetAssociatedValue = new SqlCommand("SELECT * FROM CDC WHERE CDCID = " + thisUser.associatedID, theConnection);

                                theReader = cmdGetAssociatedValue.ExecuteReader();

                                string cdcName = "";

                                if (theReader.HasRows)
                                {
                                    while (theReader.Read())
                                    {
                                        cdcName = theReader["CDCName"].ToString();
                                    }
                                }

                                theReader.Close();

                                thisUser.associatedFieldValue = cdcName;
                            }
                        }
                    }
                }
            }

            closeDataConnection();

            return theResponse;
        }

        public ResponseUserList GetAllUsersByTypeAndProvider(string userType, string providerId)
        {
            ResponseUserList theResponse = new ResponseUserList();

            openDataConnection();

            SqlCommand cmdGetCDCIDByProviderID = new SqlCommand("select CDCID from CDC where ProviderID =" + Convert.ToInt32(providerId), theConnection);
            //cmdGetCDCIDByProviderID.Parameters.AddWithValue("@providerID", Convert.ToInt32(providerId));

            theReader = cmdGetCDCIDByProviderID.ExecuteReader();

            List<int> cdcId = new List<int>();


            if (theReader.HasRows)
            {
                while (theReader.Read())
                {
                    cdcId.Add(int.Parse(theReader["CDCID"].ToString()));
                }
            }
            theReader.Close();
            List<StarbucksUser> listOfUsers = new List<StarbucksUser>();

            foreach (int id in cdcId)
            {

                SqlCommand cmdGetAllUsers = new SqlCommand("SELECT \"User\".*, UserType.TypeName, UserAssociation.AssociatedID FROM \"User\" JOIN UserType ON \"User\".UserTypeID = UserType.TypeID LEFT JOIN UserAssociation ON \"User\".Username = UserAssociation.Username WHERE UserTypeID = " + userType + "and AssociatedID = " + id, theConnection);
                theReader = cmdGetAllUsers.ExecuteReader();

                if (theReader.HasRows)
                {


                    while (theReader.Read())
                    {
                        StarbucksUser thisUser = new StarbucksUser();

                        thisUser.username = theReader["Username"].ToString();
                        thisUser.firstName = theReader["FirstName"].ToString();
                        thisUser.lastName = theReader["LastName"].ToString();
                        thisUser.phoneNumber = theReader["PhoneNumber"].ToString();
                        thisUser.emailAddress = theReader["EmailAddress"].ToString();
                        thisUser.userType = (int)theReader["UserTypeID"];
                        thisUser.userTypeName = theReader["TypeName"].ToString();
                        thisUser.state = Boolean.Parse(theReader["State"].ToString());
                        thisUser.associatedID = theReader["AssociatedID"].ToString();

                        if (thisUser.userType == 2)
                        {
                            thisUser.associatedFieldName = "Provider";
                        }
                        else if (thisUser.userType == 3)
                        {
                            thisUser.associatedFieldName = "CDC";
                        }

                        listOfUsers.Add(thisUser);
                    }



                    theResponse.statusCode = 0;
                    theResponse.statusDescription = "";
                }
                else
                {
                    theResponse.statusCode = 4;
                    theResponse.statusDescription = "There are no users in the database";
                }
                theReader.Close();
            }
            theResponse.users = listOfUsers;


            if (theResponse.users != null && theResponse.users.Count > 0)
            {
                for (int i = 0, l = theResponse.users.Count; i < l; i++)
                {
                    StarbucksUser thisUser = theResponse.users[i];

                    if (thisUser != null)
                    {
                        if (!thisUser.associatedID.ToUpper().Equals("NULL") && !thisUser.associatedID.Equals(""))
                        {
                            if (thisUser.userType == 2)
                            {
                                SqlCommand cmdGetAssociatedValue = new SqlCommand("SELECT * FROM Provider WHERE ProviderID = " + thisUser.associatedID, theConnection);

                                theReader = cmdGetAssociatedValue.ExecuteReader();

                                string providerName = "";

                                if (theReader.HasRows)
                                {
                                    while (theReader.Read())
                                    {
                                        providerName = theReader["ProviderName"].ToString();
                                    }
                                }

                                theReader.Close();

                                thisUser.associatedFieldValue = providerName;
                            }
                            else if (thisUser.userType == 3)
                            {
                                SqlCommand cmdGetAssociatedValue = new SqlCommand("SELECT * FROM CDC WHERE CDCID = " + thisUser.associatedID, theConnection);

                                theReader = cmdGetAssociatedValue.ExecuteReader();

                                string cdcName = "";

                                if (theReader.HasRows)
                                {
                                    while (theReader.Read())
                                    {
                                        cdcName = theReader["CDCName"].ToString();
                                    }
                                }

                                theReader.Close();

                                thisUser.associatedFieldValue = cdcName;
                            }
                        }
                    }
                }
            }

            closeDataConnection();

            return theResponse;
        }


        public ResponseUserList GetUserDetail(string aUsername)
        {
            ResponseUserList theResponse = new ResponseUserList();

            if (aUsername.Equals(""))
            {
                theResponse.statusCode = 1;
                theResponse.statusDescription = "No username was provided";

                return theResponse;
            }

            openDataConnection();

            SqlCommand cmdUserDetail = new SqlCommand("SELECT \"User\".*, UserAssociation.AssociatedID FROM \"User\" LEFT JOIN UserAssociation ON \"User\".Username = UserAssociation.Username WHERE \"User\".Username = '" + aUsername + "'", theConnection);
            theReader = cmdUserDetail.ExecuteReader();

            if (theReader.HasRows)
            {
                theResponse.users = new List<StarbucksUser>();

                while (theReader.Read())
                {
                    StarbucksUser thisUser = new StarbucksUser();

                    thisUser.username = aUsername;
                    thisUser.firstName = theReader["FirstName"].ToString();
                    thisUser.lastName = theReader["LastName"].ToString();
                    thisUser.emailAddress = theReader["EmailAddress"].ToString();
                    thisUser.phoneNumber = theReader["PhoneNumber"].ToString();
                    thisUser.userType = (int)theReader["UserTypeID"];
                    thisUser.password = theReader["Password"].ToString();
                    thisUser.state = Boolean.Parse(theReader["State"].ToString());
                    thisUser.associatedID = theReader["AssociatedID"].ToString();

                    if (thisUser.userType == 2)
                    {
                        thisUser.associatedFieldName = "Provider";
                    }
                    else if (thisUser.userType == 3)
                    {
                        thisUser.associatedFieldName = "CDC";
                    }

                    theResponse.users.Add(thisUser);
                }

                theResponse.statusCode = 0;
            }
            else
            {
                theResponse.statusCode = 4;
                theResponse.statusDescription = "The user " + aUsername + " could not be found";
            }

            theReader.Close();

            if (theResponse.users != null && theResponse.users.Count > 0)
            {
                for (int i = 0, l = theResponse.users.Count; i < l; i++)
                {
                    StarbucksUser thisUser = theResponse.users[i];

                    if (thisUser != null)
                    {
                        if (!thisUser.associatedID.ToUpper().Equals("NULL") && !thisUser.associatedID.Equals(""))
                        {
                            if (thisUser.userType == 2)
                            {
                                SqlCommand cmdGetAssociatedValue = new SqlCommand("SELECT * FROM Provider WHERE ProviderID = " + thisUser.associatedID, theConnection);

                                theReader = cmdGetAssociatedValue.ExecuteReader();

                                string providerName = "";

                                if (theReader.HasRows)
                                {
                                    while (theReader.Read())
                                    {
                                        providerName = theReader["ProviderName"].ToString();
                                    }
                                }

                                theReader.Close();

                                thisUser.associatedFieldValue = providerName;
                            }
                            else if (thisUser.userType == 3)
                            {
                                SqlCommand cmdGetAssociatedValue = new SqlCommand("SELECT * FROM CDC WHERE CDCID = " + thisUser.associatedID, theConnection);

                                theReader = cmdGetAssociatedValue.ExecuteReader();

                                string cdcName = "";

                                if (theReader.HasRows)
                                {
                                    while (theReader.Read())
                                    {
                                        cdcName = theReader["CDCName"].ToString();
                                    }
                                }

                                theReader.Close();

                                thisUser.associatedFieldValue = cdcName;
                            }
                        }
                    }
                }
            }

            closeDataConnection();

            return theResponse;
        }

        public Response CreateUser(StarbucksUser aUserModel)
        {
            Response theResponse = new Response();

            if (aUserModel != null)
            {
                if (aUserModel.username == null)
                {
                    theResponse.statusDescription = "Username was not supplied";
                }
                if (aUserModel.password == null)
                {
                    theResponse.statusDescription = "Password was not supplied";
                }
                if (aUserModel.firstName == null)
                {
                    theResponse.statusDescription = "First Name was not supplied";
                }
                if (aUserModel.lastName == null)
                {
                    theResponse.statusDescription = "Last Name was not supplied";
                }
                if (aUserModel.userType == 0)
                {
                    theResponse.statusDescription = "User Type was not supplied";
                }

                if (theResponse.statusDescription.Equals(""))
                {
                    openDataConnection();

                    SqlCommand cmdCheckUserExists = new SqlCommand("UserExists", theConnection);
                    cmdCheckUserExists.Parameters.AddWithValue("@username", aUserModel.username.ToString());
                    cmdCheckUserExists.CommandType = System.Data.CommandType.StoredProcedure;
                    theReader = cmdCheckUserExists.ExecuteReader();

                    if (theReader.HasRows)
                    {
                        theReader.Close();

                        theResponse.statusCode = 3;
                        theResponse.statusDescription = "Username already exists";
                    }
                    else
                    {
                        theReader.Close();

                        SqlCommand cmdCreateUser = new SqlCommand("CreateUser", theConnection);
                        cmdCreateUser.Parameters.AddWithValue("@username", aUserModel.username.ToString());
                        cmdCreateUser.Parameters.AddWithValue("@password", aUserModel.password.ToString());
                        cmdCreateUser.Parameters.AddWithValue("@firstName", aUserModel.firstName.ToString());
                        cmdCreateUser.Parameters.AddWithValue("@lastName", aUserModel.lastName.ToString());
                        cmdCreateUser.Parameters.AddWithValue("@userTypeID", aUserModel.userType.ToString());
                        cmdCreateUser.Parameters.AddWithValue("@phoneNumber", aUserModel.phoneNumber != null ? aUserModel.phoneNumber.ToString() : (object)DBNull.Value);
                        cmdCreateUser.Parameters.AddWithValue("@emailAddress", aUserModel.emailAddress != null ? aUserModel.emailAddress.ToString() : (object)DBNull.Value);

                        cmdCreateUser.CommandType = System.Data.CommandType.StoredProcedure;

                        int numRowsAffected = cmdCreateUser.ExecuteNonQuery();

                        if (numRowsAffected > 0)
                        {
                            if (aUserModel.associatedID != null && !aUserModel.associatedID.Equals(""))
                            {
                                SqlCommand cmdAssociate = new SqlCommand("AssociateUserToProvider", theConnection);
                                cmdAssociate.Parameters.AddWithValue("@username", aUserModel.username);
                                cmdAssociate.Parameters.AddWithValue("@providerID", Int32.Parse(aUserModel.associatedID));
                                cmdAssociate.CommandType = System.Data.CommandType.StoredProcedure;

                                int numRowsAffectedInAssociation = cmdAssociate.ExecuteNonQuery();

                                if (numRowsAffectedInAssociation > 0)
                                {
                                    theResponse.statusCode = 0;
                                    theResponse.statusDescription = "";
                                }
                                else
                                {
                                    SqlCommand cmdDeleteUser = new SqlCommand("DeleteUser", theConnection);
                                    cmdDeleteUser.Parameters.AddWithValue("@username", aUserModel.username);
                                    cmdDeleteUser.CommandType = System.Data.CommandType.StoredProcedure;

                                    int numRowsAffectedInDeletion = cmdDeleteUser.ExecuteNonQuery();

                                    theResponse.statusCode = 3;
                                    theResponse.statusDescription = "The user could not be created as it could not be associated to the supplied Provider ID or CDC ID";
                                }
                            }
                            else
                            {
                                theResponse.statusCode = 0;
                                theResponse.statusDescription = "";
                            }
                        }
                    }
                }
            }
            else
            {
                theResponse.statusCode = 6;
                theResponse.statusDescription = "Expected User Model not received";
            }

            return theResponse;
        }

        public Response UpdateUser(StarbucksUser aUserModel)
        {
            Response theResponse = new Response();

            if (aUserModel != null)
            {
                if (aUserModel.username == null)
                {
                    theResponse.statusDescription = "Username was not supplied";
                }
                if (aUserModel.firstName == null)
                {
                    theResponse.statusDescription = "First Name was not supplied";
                }
                if (aUserModel.lastName == null)
                {
                    theResponse.statusDescription = "Last Name was not supplied";
                }

                if (theResponse.statusDescription.Equals(""))
                {
                    openDataConnection();

                    SqlCommand cmdUpdateUser = new SqlCommand("UpdateUser", theConnection);
                    cmdUpdateUser.Parameters.AddWithValue("@username", aUserModel.username);
                    cmdUpdateUser.Parameters.AddWithValue("@firstName", aUserModel.firstName);
                    cmdUpdateUser.Parameters.AddWithValue("@lastName", aUserModel.lastName);
                    cmdUpdateUser.Parameters.AddWithValue("@phoneNumber", aUserModel.phoneNumber != null ? aUserModel.phoneNumber.ToString() : (object)DBNull.Value);
                    cmdUpdateUser.Parameters.AddWithValue("@emailAddress", aUserModel.emailAddress != null ? aUserModel.emailAddress.ToString() : (object)DBNull.Value);
                    cmdUpdateUser.CommandType = System.Data.CommandType.StoredProcedure;

                    int numRowsAffected = cmdUpdateUser.ExecuteNonQuery();

                    if (numRowsAffected > 0)
                    {
                        theResponse.statusCode = 0;
                        theResponse.statusDescription = "";
                    }
                    else
                    {
                        theResponse.statusCode = 6;
                        theResponse.statusDescription = "The user " + aUserModel.username + " could not be updated";
                    }
                }
            }
            else
            {
                theResponse.statusCode = 6;
                theResponse.statusDescription = "Expected User Model not received";
            }

            closeDataConnection();

            return theResponse;
        }

        public Response UpdateUserPassword(StarbucksUser aUserModel)
        {
            Response theResponse = new Response();

            if (aUserModel != null)
            {
                if (aUserModel.username == null)
                {
                    theResponse.statusDescription = "Username was not supplied";
                }
                if (aUserModel.password == null)
                {
                    theResponse.statusDescription = "Password was not supplied";
                }

                if (theResponse.statusDescription.Equals(""))
                {
                    openDataConnection();

                    SqlCommand cmdUpdateUser = new SqlCommand("UpdateUserPassword", theConnection);
                    cmdUpdateUser.Parameters.AddWithValue("@username", aUserModel.username);
                    cmdUpdateUser.Parameters.AddWithValue("@password", aUserModel.password);
                    cmdUpdateUser.CommandType = System.Data.CommandType.StoredProcedure;

                    int numRowsAffected = cmdUpdateUser.ExecuteNonQuery();

                    if (numRowsAffected > 0)
                    {
                        theResponse.statusCode = 0;
                        theResponse.statusDescription = "";
                    }
                    else
                    {
                        theResponse.statusCode = 6;
                        theResponse.statusDescription = "The password of the user " + aUserModel.username + " could not be updated";
                    }
                }
            }
            else
            {
                theResponse.statusCode = 6;
                theResponse.statusDescription = "Expected User Model not received";
            }

            closeDataConnection();

            return theResponse;
        }

        public Response ActivateUser(string aUsername)
        {
            Response theResponse = new Response();

            if (aUsername.Equals(""))
            {
                theResponse.statusCode = 1;
                theResponse.statusDescription = "No username was provided";

                return theResponse;
            }

            openDataConnection();

            SqlCommand cmdActivate = new SqlCommand("ActivateUser", theConnection);
            cmdActivate.Parameters.AddWithValue("@username", aUsername.ToString());
            cmdActivate.CommandType = System.Data.CommandType.StoredProcedure;

            int numRowsAffected = cmdActivate.ExecuteNonQuery();

            if (numRowsAffected > 0)
            {
                theResponse.statusCode = 0;
            }
            else
            {
                theResponse.statusCode = 4;
                theResponse.statusDescription = "The desired user could not be found";
            }

            closeDataConnection();

            return theResponse;
        }

        public Response DeactivateUser(string aUsername)
        {
            Response theResponse = new Response();

            if (aUsername.Equals(""))
            {
                theResponse.statusCode = 1;
                theResponse.statusDescription = "No username was provided";

                return theResponse;
            }

            openDataConnection();

            SqlCommand cmdActivate = new SqlCommand("DeactivateUser", theConnection);
            cmdActivate.Parameters.AddWithValue("@username", aUsername.ToString());
            cmdActivate.CommandType = System.Data.CommandType.StoredProcedure;

            int numRowsAffected = cmdActivate.ExecuteNonQuery();

            if (numRowsAffected > 0)
            {
                theResponse.statusCode = 0;
            }
            else
            {
                theResponse.statusCode = 4;
                theResponse.statusDescription = "The desired user could not be found";
            }

            closeDataConnection();

            return theResponse;
        }

        public Response DeleteUser(string aUsername)
        {
            Response theResponse = new Response();

            if (aUsername.Equals(""))
            {
                theResponse.statusCode = 1;
                theResponse.statusDescription = "No username was provided";

                return theResponse;
            }

            openDataConnection();

            SqlCommand cmdActivate = new SqlCommand("DeleteUser", theConnection);
            cmdActivate.Parameters.AddWithValue("@username", aUsername.ToString());
            cmdActivate.CommandType = System.Data.CommandType.StoredProcedure;

            try
            {
                int numRowsAffected = cmdActivate.ExecuteNonQuery();

                if (numRowsAffected > 0)
                {
                    theResponse.statusCode = 0;
                }
                else
                {
                    theResponse.statusCode = 4;
                    theResponse.statusDescription = "The desired user could not be found";
                }
            }
            catch
            {
                theResponse.statusCode = 6;
                theResponse.statusDescription = "The user " + aUsername + " could not be deleted due to a foreign key constraint";
            }

            closeDataConnection();

            return theResponse;
        }

        public ResponseStoreList GetAllStores()
        {
            ResponseStoreList theResponse = new ResponseStoreList();

            openDataConnection();

            SqlCommand cmdGetAllUsers = new SqlCommand("SELECT * FROM Store", theConnection);
            theReader = cmdGetAllUsers.ExecuteReader();

            if (theReader.HasRows)
            {
                List<Store> listOfStores = new List<Store>();

                int numRecords = 0;

                while (theReader.Read())
                {
                    Store thisStore = new Store();

                    thisStore.storeID = (int)theReader["StoreID"];
                    thisStore.storeName = theReader["StoreName"].ToString();
                    thisStore.storeAddress = theReader["StoreAddress"].ToString();
                    thisStore.storeCity = theReader["StoreCity"].ToString();
                    thisStore.storeZip = theReader["StoreZip"].ToString();
                    thisStore.storeState = theReader["StoreState"].ToString();
                    thisStore.storePhone = theReader["StorePhone"].ToString();
                    thisStore.storeManagerName = theReader["StoreManagerName"].ToString();
                    thisStore.storeEmailAddress = theReader["StoreEmail"].ToString();
                    thisStore.storeNumber = theReader["StoreNumber"].ToString();
                    thisStore.storeOwnershipType = theReader["StoreOwnershipType"].ToString();

                    listOfStores.Add(thisStore);

                    numRecords++;
                }

                theResponse.stores = listOfStores;

                theResponse.numberOfRecords = numRecords;

                theResponse.statusCode = 0;
                theResponse.statusDescription = "";
            }
            else
            {
                theResponse.statusCode = 4;
                theResponse.statusDescription = "There are no stores in the database";
            }

            theReader.Close();

            closeDataConnection();

            return theResponse;
        }

        public ResponseStoreList GetAllStoresWithRange(string startingIndex, string endingIndex)
        {
            ResponseStoreList theResponse = new ResponseStoreList();

            openDataConnection();

            SqlCommand cmdGetAllUsers = new SqlCommand("SELECT * FROM Store", theConnection);
            theReader = cmdGetAllUsers.ExecuteReader();

            if (theReader.HasRows)
            {
                List<Store> listOfStores = new List<Store>();

                int numRecords = 0;

                while (theReader.Read())
                {
                    Store thisStore = new Store();

                    thisStore.storeID = (int)theReader["StoreID"];
                    thisStore.storeName = theReader["StoreName"].ToString();
                    thisStore.storeAddress = theReader["StoreAddress"].ToString();
                    thisStore.storeCity = theReader["StoreCity"].ToString();
                    thisStore.storeZip = theReader["StoreZip"].ToString();
                    thisStore.storeState = theReader["StoreState"].ToString();
                    thisStore.storePhone = theReader["StorePhone"].ToString();
                    thisStore.storeManagerName = theReader["StoreManagerName"].ToString();
                    thisStore.storeEmailAddress = theReader["StoreEmail"].ToString();
                    thisStore.storeNumber = theReader["StoreNumber"].ToString();
                    thisStore.storeOwnershipType = theReader["StoreOwnershipType"].ToString();

                    listOfStores.Add(thisStore);

                    numRecords++;
                }

                theResponse.stores = new List<Store>();

                int startIndex = Int32.Parse(startingIndex);
                int endIndex = Int32.Parse(endingIndex);
                endIndex = startIndex + endIndex;

                if (startIndex <= 0)
                {
                    startIndex = 1;
                }

                if (startIndex > 0 && endIndex >= startIndex)
                {
                    if (endIndex > numRecords)
                    {
                        endIndex = numRecords;
                    }

                    for (int i = startIndex; i <= endIndex; i++)
                    {
                        theResponse.stores.Add(listOfStores[i - 1]);
                    }

                    theResponse.numberOfRecords = numRecords;

                    theResponse.statusCode = 0;
                    theResponse.statusDescription = "";
                }
                else
                {
                    theResponse.statusCode = 6;
                    theResponse.statusDescription = "The starting or ending index did not fall within the data range";
                }
            }
            else
            {
                theResponse.statusCode = 4;
                theResponse.statusDescription = "There are no stores in the database";
            }

            theReader.Close();

            closeDataConnection();

            return theResponse;
        }

        //Retrieve photos by search
        public DataTable GetPhotos(string condition, int startIndex, int maxRows)
        {
            try
            {
                openDataConnection();
                SqlCommand cmdGet = new SqlCommand("GetPhotos", theConnection);
                cmdGet.Parameters.AddWithValue("@Condition", condition);
                cmdGet.Parameters.AddWithValue("@StartRowIndex", startIndex);
                cmdGet.Parameters.AddWithValue("@MaximumRows", maxRows);
                cmdGet.CommandType = System.Data.CommandType.StoredProcedure;
                cmdGet.CommandTimeout = 600;
                SqlDataAdapter da = new SqlDataAdapter(cmdGet);
                DataTable dtPhotos = new DataTable();
                dtPhotos.TableName = "Photos"; 
                da.Fill(dtPhotos);
                return dtPhotos;
            }
            catch (Exception)
            {
                return null;
            }
            finally
            {
                closeDataConnection();
            }
        }

        //retrieve Stores within a particular range
        public DataTable GetStores(int startIndex, int maxRows)
        {
            try
            {
                openDataConnection();
                SqlCommand cmdGet = new SqlCommand("GetStores", theConnection);
                cmdGet.Parameters.AddWithValue("@StartRowIndex", startIndex);
                cmdGet.Parameters.AddWithValue("@MaximumRows", maxRows);
                cmdGet.CommandType = System.Data.CommandType.StoredProcedure;
                cmdGet.CommandTimeout = 600;
                SqlDataAdapter da = new SqlDataAdapter(cmdGet);
                DataTable dtStores = new DataTable();
                da.Fill(dtStores);
                return dtStores;
            }
            catch (Exception)
            {
                return null;
            }
            finally
            {
                closeDataConnection();
            }
        }

        //retrieve Filtered Stores within a particular range
        public DataTable GetStoresFilter(string filterText, int startIndex, int maxRows)
        {
            try
            {
                openDataConnection();
                SqlCommand cmdGet = new SqlCommand("GetStoresFilter", theConnection);
                cmdGet.Parameters.AddWithValue("@FilterText", filterText);
                cmdGet.Parameters.AddWithValue("@StartRowIndex", startIndex);
                cmdGet.Parameters.AddWithValue("@MaximumRows", maxRows);
                cmdGet.CommandType = System.Data.CommandType.StoredProcedure;
                cmdGet.CommandTimeout = 600;
                SqlDataAdapter da = new SqlDataAdapter(cmdGet);
                DataTable dtStores = new DataTable();
                da.Fill(dtStores);
                return dtStores;
            }
            catch (Exception)
            {
                return null;
            }
            finally
            {
                closeDataConnection();
            }
        }

        //retrieve Ops within a particular range
        public DataTable GetOps(int startIndex, int maxRows)
        {
            try
            {
                openDataConnection();
                SqlCommand cmdGet = new SqlCommand("GetOps", theConnection);
                cmdGet.Parameters.AddWithValue("@StartRowIndex", startIndex);
                cmdGet.Parameters.AddWithValue("@MaximumRows", maxRows);
                cmdGet.CommandType = System.Data.CommandType.StoredProcedure;
                cmdGet.CommandTimeout = 600;
                SqlDataAdapter da = new SqlDataAdapter(cmdGet);
                DataTable dtOps = new DataTable();
                da.Fill(dtOps);
                return dtOps;
            }
            catch (Exception)
            {
                return null;
            }
            finally
            {
                closeDataConnection();
            }
        }

        //retrieve Filtered Ops within a particular range
        public DataTable GetOpsFilter(string filterText, int startIndex, int maxRows)
        {
            try
            {
                openDataConnection();
                SqlCommand cmdGet = new SqlCommand("GetOpsFilter", theConnection);
                cmdGet.Parameters.AddWithValue("@FilterText", filterText);
                cmdGet.Parameters.AddWithValue("@StartRowIndex", startIndex);
                cmdGet.Parameters.AddWithValue("@MaximumRows", maxRows);
                cmdGet.CommandType = System.Data.CommandType.StoredProcedure;
                cmdGet.CommandTimeout = 600;
                SqlDataAdapter da = new SqlDataAdapter(cmdGet);
                DataTable dtOps = new DataTable();
                da.Fill(dtOps);
                return dtOps;
            }
            catch (Exception)
            {
                return null;
            }
            finally
            {
                closeDataConnection();
            }
        }

        //retrieve Users within a particular range
        public DataTable GetUsers(int startIndex, int maxRows, string condition)
        {
            try
            {
                openDataConnection();
                SqlCommand cmdGet = new SqlCommand("GetUsers", theConnection);
                cmdGet.Parameters.AddWithValue("@StartRowIndex", startIndex);
                cmdGet.Parameters.AddWithValue("@MaximumRows", maxRows);
                cmdGet.Parameters.AddWithValue("@Condition", condition);
                cmdGet.CommandType = System.Data.CommandType.StoredProcedure;
                cmdGet.CommandTimeout = 600;
                SqlDataAdapter da = new SqlDataAdapter(cmdGet);
                DataTable dtUsers = new DataTable();
                da.Fill(dtUsers);
                return dtUsers;
            }
            catch (Exception)
            {
                return null;
            }
            finally
            {
                closeDataConnection();
            }
        }

        //retrieve Filtered Users within a particular range
        public DataTable GetUsersFilter(string filterText, int startIndex, int maxRows, string condition)
        {
            try
            {
                openDataConnection();
                SqlCommand cmdGet = new SqlCommand("GetUsersFilter", theConnection);
                cmdGet.Parameters.AddWithValue("@FilterText", filterText);
                cmdGet.Parameters.AddWithValue("@StartRowIndex", startIndex);
                cmdGet.Parameters.AddWithValue("@MaximumRows", maxRows);
                cmdGet.Parameters.AddWithValue("@Condition", condition);
                cmdGet.CommandType = System.Data.CommandType.StoredProcedure;
                cmdGet.CommandTimeout = 600;
                SqlDataAdapter da = new SqlDataAdapter(cmdGet);
                DataTable dtUsers = new DataTable();
                da.Fill(dtUsers);
                return dtUsers;
            }
            catch (Exception)
            {
                return null;
            }
            finally
            {
                closeDataConnection();
            }
        }

        public ResponseStoreList GetStoresForCDC(string aCDCID)
        {
            ResponseStoreList theResponse = new ResponseStoreList();

            if (aCDCID == null || aCDCID.Equals(""))
            {
                theResponse.statusCode = 1;
                theResponse.statusDescription = "CDC ID is missing";
            }
            else
            {
                openDataConnection();

                SqlCommand cmdGet = new SqlCommand("GetStoresForCDC", theConnection);
                cmdGet.Parameters.AddWithValue("@cdcID", aCDCID);
                cmdGet.CommandType = System.Data.CommandType.StoredProcedure;

                try
                {
                    theReader = cmdGet.ExecuteReader();

                    if (theReader.HasRows)
                    {
                        theResponse.stores = new List<Store>();

                        while (theReader.Read())
                        {
                            Store thisStore = new Store();

                            thisStore.storeID = (int)theReader["StoreID"];
                            thisStore.storeName = theReader["StoreName"].ToString();
                            thisStore.storeAddress = theReader["StoreAddress"].ToString();
                            thisStore.storeCity = theReader["StoreCity"].ToString();
                            thisStore.storeZip = theReader["StoreZip"].ToString();
                            thisStore.storeState = theReader["StoreState"].ToString();
                            thisStore.storePhone = theReader["StorePhone"].ToString();
                            thisStore.storeManagerName = theReader["StoreManagerName"].ToString();
                            thisStore.storeEmailAddress = theReader["StoreEmail"].ToString();
                            thisStore.storeNumber = theReader["StoreNumber"].ToString();
                            thisStore.storeOwnershipType = theReader["StoreOwnershipType"].ToString();

                            theResponse.stores.Add(thisStore);
                        }

                        theResponse.statusCode = 0;
                        theResponse.statusDescription = "";
                    }
                    else
                    {
                        theResponse.statusCode = 4;
                        theResponse.statusDescription = "There are no stores for CDC ID " + aCDCID;
                    }

                    theReader.Close();
                }
                catch (Exception _exception)
                {
                    theResponse.statusCode = 6;
                    theResponse.statusDescription = _exception.Message;
                }

                closeDataConnection();
            }

            return theResponse;
        }

        public ResponseStoreList GetStoreDetail(string aStoreID)
        {
            ResponseStoreList theResponse = new ResponseStoreList();

            if (aStoreID.Equals(""))
            {
                theResponse.statusCode = 1;
                theResponse.statusDescription = "Store ID not provided";

                return theResponse;
            }

            openDataConnection();

            SqlCommand cmdUserDetail = new SqlCommand("SELECT * FROM Store WHERE StoreID = " + aStoreID + " OR StoreName = '" + aStoreID + "'", theConnection);
            theReader = cmdUserDetail.ExecuteReader();

            if (theReader.HasRows)
            {
                theResponse.stores = new List<Store>();

                while (theReader.Read())
                {
                    Store thisStore = new Store();

                    thisStore.storeID = (int)theReader["StoreID"];
                    thisStore.storeName = theReader["StoreName"].ToString();
                    thisStore.storeAddress = theReader["StoreAddress"].ToString();
                    thisStore.storeCity = theReader["StoreCity"].ToString();
                    thisStore.storeZip = theReader["StoreZip"].ToString();
                    thisStore.storeState = theReader["StoreState"].ToString();
                    thisStore.storePhone = theReader["StorePhone"].ToString();
                    thisStore.storeManagerName = theReader["StoreManagerName"].ToString();
                    thisStore.storeEmailAddress = theReader["StoreEmail"].ToString();
                    thisStore.storeNumber = theReader["StoreNumber"].ToString();
                    thisStore.storeOwnershipType = theReader["StoreOwnershipType"].ToString();

                    theResponse.stores.Add(thisStore);
                }

                theResponse.statusCode = 0;
            }
            else
            {
                theResponse.statusCode = 4;
                theResponse.statusDescription = "The store " + aStoreID + " could not be found";
            }

            theReader.Close();

            closeDataConnection();

            return theResponse;
        }

        public Response CreateStore(Store aStoreModel)
        {
            Response theResponse = new Response();

            if (aStoreModel != null)
            {
                if (aStoreModel.storeName == null)
                {
                    theResponse.statusDescription = "Store Name was not supplied";
                }
                if (aStoreModel.storeAddress == null)
                {
                    theResponse.statusDescription = "Store Address was not supplied";
                }
                if (aStoreModel.storeCity == null)
                {
                    theResponse.statusDescription = "Store City was not supplied";
                }
                if (aStoreModel.storeZip == null)
                {
                    theResponse.statusDescription = "Store Zip Code was not supplied";
                }
                else if (aStoreModel.storeZip.Count() > 10)
                {
                    theResponse.statusDescription = "Store Zip Code is longer than 10 characters";
                }
                if (aStoreModel.storeState == null)
                {
                    theResponse.statusDescription = "Store State was not supplied";
                }
                if (aStoreModel.storePhone == null)
                {
                    theResponse.statusDescription = "Store Phone Number was not supplied";
                }
                if (aStoreModel.storeManagerName == null)
                {
                    theResponse.statusDescription = "Store Manager's name was not supplied";
                }
                if (aStoreModel.storeEmailAddress == null)
                {
                    theResponse.statusDescription = "Store's email address was not supplied";
                }
                if (doesStoreExist(aStoreModel.storeNumber))
                {
                    theResponse.statusCode = 3;
                    theResponse.statusDescription = "Store Number already exists";
                }

                if (theResponse.statusDescription.Equals(""))
                {
                    openDataConnection();

                    SqlCommand cmdCreateStore = new SqlCommand("CreateStore", theConnection);
                    cmdCreateStore.Parameters.AddWithValue("@storeName", aStoreModel.storeName);
                    cmdCreateStore.Parameters.AddWithValue("@storeAddress", aStoreModel.storeAddress);
                    cmdCreateStore.Parameters.AddWithValue("@storeCity", aStoreModel.storeCity);
                    cmdCreateStore.Parameters.AddWithValue("@storeZip", aStoreModel.storeZip);
                    cmdCreateStore.Parameters.AddWithValue("@storeState", aStoreModel.storeState);
                    cmdCreateStore.Parameters.AddWithValue("@storePhone", aStoreModel.storePhone);
                    cmdCreateStore.Parameters.AddWithValue("@storeEmail", aStoreModel.storeEmailAddress);
                    cmdCreateStore.Parameters.AddWithValue("@storeManager", aStoreModel.storeManagerName);
                    cmdCreateStore.Parameters.AddWithValue("@storeNumber", aStoreModel.storeNumber);
                    cmdCreateStore.Parameters.AddWithValue("@storeOwnershipType", aStoreModel.storeOwnershipType);
                    cmdCreateStore.Parameters.AddWithValue("@PODRequired", aStoreModel.PODRequired);
                    cmdCreateStore.CommandType = System.Data.CommandType.StoredProcedure;

                    int numRowsAffected = cmdCreateStore.ExecuteNonQuery();

                    if (numRowsAffected > 0)
                    {
                        theResponse.statusCode = 0;
                        theResponse.statusDescription = "";
                    }
                    else
                    {
                        theResponse.statusCode = 6;
                        theResponse.statusDescription = "Could not add this store";
                    }
                }
            }
            else
            {
                theResponse.statusCode = 6;
                theResponse.statusDescription = "Expected User Model not received";
            }

            return theResponse;
        }

        public Response UpdateStore(Store aStoreModel)
        {
            Response theResponse = new Response();

            if (aStoreModel != null)
            {
                if (aStoreModel.storeID == 0)
                {
                    theResponse.statusDescription = "Store ID was not supplied";
                }
                if (aStoreModel.storeName == null)
                {
                    theResponse.statusDescription = "Store Name was not supplied";
                }
                if (aStoreModel.storeAddress == null)
                {
                    theResponse.statusDescription = "Store Address was not supplied";
                }
                if (aStoreModel.storeCity == null)
                {
                    theResponse.statusDescription = "Store City was not supplied";
                }
                if (aStoreModel.storeZip == null)
                {
                    theResponse.statusDescription = "Store Zip Code was not supplied";
                }
                else if (aStoreModel.storeZip.Count() > 10)
                {
                    theResponse.statusDescription = "Store Zip is longer than 10 characters";
                }
                if (aStoreModel.storeState == null)
                {
                    theResponse.statusDescription = "Store State was not supplied";
                }
                if (aStoreModel.storePhone == null)
                {
                    theResponse.statusDescription = "Store Phone Number was not supplied";
                }
                if (aStoreModel.storeManagerName == null)
                {
                    theResponse.statusDescription = "Store Manager's name was not supplied";
                }
                if (aStoreModel.storeEmailAddress == null)
                {
                    theResponse.statusDescription = "Store's email address was not supplied";
                }

                if (theResponse.statusDescription.Equals(""))
                {
                    openDataConnection();

                    SqlCommand cmdCreateStore = new SqlCommand("UpdateStore", theConnection);
                    cmdCreateStore.Parameters.AddWithValue("@storeID", aStoreModel.storeID);
                    cmdCreateStore.Parameters.AddWithValue("@storeName", aStoreModel.storeName);
                    cmdCreateStore.Parameters.AddWithValue("@storeAddress", aStoreModel.storeAddress);
                    cmdCreateStore.Parameters.AddWithValue("@storeCity", aStoreModel.storeCity);
                    cmdCreateStore.Parameters.AddWithValue("@storeZip", aStoreModel.storeZip);
                    cmdCreateStore.Parameters.AddWithValue("@storeState", aStoreModel.storeState);
                    cmdCreateStore.Parameters.AddWithValue("@storePhone", aStoreModel.storePhone);
                    cmdCreateStore.Parameters.AddWithValue("@storeEmailAddress", aStoreModel.storeEmailAddress);
                    cmdCreateStore.Parameters.AddWithValue("@storeManagerName", aStoreModel.storeManagerName);
                    cmdCreateStore.Parameters.AddWithValue("@storeNumber", aStoreModel.storeNumber);
                    cmdCreateStore.Parameters.AddWithValue("@storeOwnershipType", aStoreModel.storeOwnershipType);
                    cmdCreateStore.Parameters.AddWithValue("@PODRequired", aStoreModel.PODRequired);
                    cmdCreateStore.CommandType = System.Data.CommandType.StoredProcedure;

                    int numRowsAffected = cmdCreateStore.ExecuteNonQuery();

                    if (numRowsAffected > 0)
                    {
                        theResponse.statusCode = 0;
                        theResponse.statusDescription = "";
                    }
                    else
                    {
                        theResponse.statusCode = 6;
                        theResponse.statusDescription = "Could not update this store";
                    }
                }
            }
            else
            {
                theResponse.statusCode = 6;
                theResponse.statusDescription = "Expected User Model not received";
            }

            return theResponse;
        }

        public ResponseRouteList GetAllRouteMappings()
        {
            ResponseRouteList theResponse = new ResponseRouteList();

            openDataConnection();

            SqlCommand cmdAllRouteMaps = new SqlCommand("GetAllRouteMappingsWithStoreDetails", theConnection);

            theReader = cmdAllRouteMaps.ExecuteReader();

            if (theReader.HasRows)
            {
                List<Route> allRoutes = new List<Route>();

                string currentRouteName = "";
                Route currentRoute = null;

                while (theReader.Read())
                {
                    if (theReader["RouteName"].ToString().Equals(""))
                    {
                        continue;
                    }

                    if (!currentRouteName.Equals(theReader["RouteName"].ToString()))
                    {
                        currentRoute = new Route();
                        currentRoute.routeName = theReader["RouteName"].ToString();
                        currentRoute.routeID = (int)theReader["RouteID"];
                        currentRoute.routeStatus = (int)theReader["Status"];
                        currentRoute.stores = new List<Store>();

                        currentRoute.cdc = new CDC(theReader["CDCName"].ToString(), (int)theReader["CDCID"]);
                        currentRoute.cdcName = theReader["CDCName"].ToString();

                        currentRouteName = currentRoute.routeName;

                        allRoutes.Add(currentRoute);
                    }

                    Store thisStore = new Store();

                    thisStore.storeID = (int)theReader["StoreID"];
                    thisStore.storeName = theReader["StoreName"].ToString();
                    thisStore.storeAddress = theReader["StoreAddress"].ToString();
                    thisStore.storeCity = theReader["StoreCity"].ToString();
                    thisStore.storeZip = theReader["StoreZip"].ToString();
                    thisStore.storeState = theReader["StoreState"].ToString();
                    thisStore.storePhone = theReader["StorePhone"].ToString();
                    thisStore.storeManagerName = theReader["StoreManagerName"].ToString();
                    thisStore.storeEmailAddress = theReader["StoreEmail"].ToString();
                    thisStore.storeNumber = theReader["StoreNumber"].ToString();
                    thisStore.storeOwnershipType = theReader["StoreOwnershipType"].ToString();

                    currentRoute.stores.Add(thisStore);
                }

                theResponse.routes = allRoutes;

                theResponse.statusCode = 0;
                theResponse.statusDescription = "";
            }
            else
            {
                theResponse.statusCode = 4;
                theResponse.statusDescription = "There are no route-store mappings defined";
            }

            theReader.Close();

            closeDataConnection();

            return theResponse;
        }

        public ResponseRouteList GetAllRouteMappingsWithRange(string startingIndex, string endingIndex)
        {
            ResponseRouteList theResponse = new ResponseRouteList();

            openDataConnection();

            SqlCommand cmdAllRouteMaps = new SqlCommand("GetAllRouteMappingsWithStoreDetails", theConnection);

            theReader = cmdAllRouteMaps.ExecuteReader();

            int numRecords = 0;

            if (theReader.HasRows)
            {
                List<Route> allRoutes = new List<Route>();

                string currentRouteName = "";
                Route currentRoute = null;

                while (theReader.Read())
                {
                    if (theReader["RouteName"].ToString().Equals(""))
                    {
                        continue;
                    }

                    if (!currentRouteName.Equals(theReader["RouteName"].ToString()))
                    {
                        currentRoute = new Route();
                        currentRoute.routeName = theReader["RouteName"].ToString();
                        currentRoute.routeID = (int)theReader["RouteID"];
                        currentRoute.routeStatus = (int)theReader["Status"];
                        currentRoute.stores = new List<Store>();

                        currentRoute.cdc = new CDC(theReader["CDCName"].ToString(), (int)theReader["CDCID"]);
                        currentRoute.cdcName = theReader["CDCName"].ToString();

                        currentRouteName = currentRoute.routeName;

                        allRoutes.Add(currentRoute);

                        numRecords++;
                    }

                    Store thisStore = new Store();

                    thisStore.storeID = (int)theReader["StoreID"];
                    thisStore.storeName = theReader["StoreName"].ToString();
                    thisStore.storeAddress = theReader["StoreAddress"].ToString();
                    thisStore.storeCity = theReader["StoreCity"].ToString();
                    thisStore.storeZip = theReader["StoreZip"].ToString();
                    thisStore.storeState = theReader["StoreState"].ToString();
                    thisStore.storePhone = theReader["StorePhone"].ToString();
                    thisStore.storeManagerName = theReader["StoreManagerName"].ToString();
                    thisStore.storeEmailAddress = theReader["StoreEmail"].ToString();
                    thisStore.storeNumber = theReader["StoreNumber"].ToString();
                    thisStore.storeOwnershipType = theReader["StoreOwnershipType"].ToString();

                    currentRoute.stores.Add(thisStore);
                }

                theResponse.numberOfRecords = numRecords;

                List<Route> finalRoutes = new List<Route>();

                int startIndex = Int32.Parse(startingIndex);
                int endIndex = Int32.Parse(endingIndex);
                endIndex = startIndex + endIndex;

                if (startIndex <= 0)
                {
                    startIndex = 1;
                }

                if (startIndex > 0 && endIndex >= startIndex)
                {
                    if (endIndex > numRecords)
                    {
                        endIndex = numRecords;
                    }

                    for (int i = startIndex; i <= endIndex; i++)
                    {
                        finalRoutes.Add(allRoutes[i - 1]);
                    }

                    theResponse.routes = finalRoutes;

                    theResponse.statusCode = 0;
                    theResponse.statusDescription = "";
                }
                else
                {
                    theResponse.statusCode = 6;
                    theResponse.statusDescription = "The starting or ending index did not fall within the data range";
                }
            }
            else
            {
                theResponse.statusCode = 4;
                theResponse.statusDescription = "There are no route-store mappings defined";
            }

            theReader.Close();

            closeDataConnection();

            return theResponse;
        }

        public ResponseRouteList GetAllRouteMappingsForProvider(string providerID)
        {
            ResponseRouteList theResponse = new ResponseRouteList();

            openDataConnection();

            SqlCommand cmdAllRouteMaps = new SqlCommand("GetAllRouteMappingsWithStoreDetailsForCDC", theConnection);
            cmdAllRouteMaps.Parameters.AddWithValue(@"providerID", providerID);
            cmdAllRouteMaps.CommandType = System.Data.CommandType.StoredProcedure;

            theReader = cmdAllRouteMaps.ExecuteReader();

            if (theReader.HasRows)
            {
                List<Route> allRoutes = new List<Route>();

                string currentRouteName = "";
                Route currentRoute = null;

                while (theReader.Read())
                {
                    if (theReader["RouteName"].ToString().Equals("") || theReader["status"].Equals("0"))
                    {
                        continue;
                    }

                    if (!currentRouteName.Equals(theReader["RouteName"].ToString()))
                    {
                        currentRoute = new Route();
                        currentRoute.routeName = theReader["RouteName"].ToString();
                        currentRoute.routeID = (int)theReader["RouteID"];
                        currentRoute.routeStatus = (int)theReader["Status"];
                        currentRoute.stores = new List<Store>();

                        currentRoute.cdc = new CDC(theReader["CDCName"].ToString(), (int)theReader["CDCID"]);
                        currentRoute.cdcName = theReader["CDCName"].ToString();

                        currentRouteName = currentRoute.routeName;

                        allRoutes.Add(currentRoute);
                    }

                    Store thisStore = new Store();

                    thisStore.storeID = (int)theReader["StoreID"];
                    thisStore.storeName = theReader["StoreName"].ToString();
                    thisStore.storeAddress = theReader["StoreAddress"].ToString();
                    thisStore.storeCity = theReader["StoreCity"].ToString();
                    thisStore.storeZip = theReader["StoreZip"].ToString();
                    thisStore.storeState = theReader["StoreState"].ToString();
                    thisStore.storePhone = theReader["StorePhone"].ToString();
                    thisStore.storeManagerName = theReader["StoreManagerName"].ToString();
                    thisStore.storeEmailAddress = theReader["StoreEmail"].ToString();
                    thisStore.storeNumber = theReader["StoreNumber"].ToString();
                    thisStore.storeOwnershipType = theReader["StoreOwnershipType"].ToString();

                    currentRoute.stores.Add(thisStore);
                }

                theResponse.routes = allRoutes;

                theResponse.statusCode = 0;
                theResponse.statusDescription = "";

                theReader.Close();
            }
            else
            {
                theReader.Close();

                theResponse.statusCode = 4;
                theResponse.statusDescription = "There are no route-store mappings defined for Provider " + providerID + " (" + getProviderNameFromID(Int32.Parse(providerID)) + ")";
            }

            closeDataConnection();

            return theResponse;
        }

        public ResponseRouteList GetAllRouteMappingsForProviderWithRange(string providerID, string startingIndex, string endingIndex)
        {
            ResponseRouteList theResponse = new ResponseRouteList();

            openDataConnection();

            SqlCommand cmdAllRouteMaps = new SqlCommand("GetAllRouteMappingsWithStoreDetailsForCDC", theConnection);
            cmdAllRouteMaps.Parameters.AddWithValue(@"providerID", providerID);
            cmdAllRouteMaps.CommandType = System.Data.CommandType.StoredProcedure;

            theReader = cmdAllRouteMaps.ExecuteReader();

            int numRecords = 0;

            if (theReader.HasRows)
            {
                List<Route> allRoutes = new List<Route>();

                string currentRouteName = "";
                Route currentRoute = null;

                while (theReader.Read())
                {
                    if (theReader["RouteName"].ToString().Equals(""))
                    {
                        continue;
                    }

                    if (!currentRouteName.Equals(theReader["RouteName"].ToString()))
                    {
                        currentRoute = new Route();
                        currentRoute.routeName = theReader["RouteName"].ToString();
                        currentRoute.routeID = (int)theReader["RouteID"];
                        currentRoute.routeStatus = (int)theReader["Status"];
                        currentRoute.stores = new List<Store>();

                        currentRoute.cdc = new CDC(theReader["CDCName"].ToString(), (int)theReader["CDCID"]);
                        currentRoute.cdcName = theReader["CDCName"].ToString();

                        currentRouteName = currentRoute.routeName;

                        allRoutes.Add(currentRoute);

                        numRecords++;
                    }

                    Store thisStore = new Store();

                    thisStore.storeID = (int)theReader["StoreID"];
                    thisStore.storeName = theReader["StoreName"].ToString();
                    thisStore.storeAddress = theReader["StoreAddress"].ToString();
                    thisStore.storeCity = theReader["StoreCity"].ToString();
                    thisStore.storeZip = theReader["StoreZip"].ToString();
                    thisStore.storeState = theReader["StoreState"].ToString();
                    thisStore.storePhone = theReader["StorePhone"].ToString();
                    thisStore.storeManagerName = theReader["StoreManagerName"].ToString();
                    thisStore.storeEmailAddress = theReader["StoreEmail"].ToString();
                    thisStore.storeNumber = theReader["StoreNumber"].ToString();
                    thisStore.storeOwnershipType = theReader["StoreOwnershipType"].ToString();

                    currentRoute.stores.Add(thisStore);
                }

                theResponse.numberOfRecords = numRecords;

                List<Route> finalRoutes = new List<Route>();

                int startIndex = Int32.Parse(startingIndex);
                int endIndex = Int32.Parse(endingIndex);
                endIndex = startIndex + endIndex;

                if (startIndex <= 0)
                {
                    startIndex = 1;
                }

                if (startIndex > 0 && endIndex >= startIndex)
                {
                    if (endIndex > numRecords)
                    {
                        endIndex = numRecords;
                    }

                    for (int i = startIndex; i <= endIndex; i++)
                    {
                        finalRoutes.Add(allRoutes[i - 1]);
                    }

                    theResponse.routes = finalRoutes;

                    theResponse.statusCode = 0;
                    theResponse.statusDescription = "";
                }
                else
                {
                    theResponse.statusCode = 6;
                    theResponse.statusDescription = "The starting or ending index did not fall within the data range";
                }

                theReader.Close();
            }
            else
            {
                theReader.Close();

                theResponse.statusCode = 4;
                theResponse.statusDescription = "There are no route-store mappings defined for Provider " + providerID + " (" + getProviderNameFromID(Int32.Parse(providerID)) + ")";
            }

            closeDataConnection();

            return theResponse;
        }

        public ResponseRouteList DotNetGetAllRouteMappings(int startIndex, int maxRows, string providerId)
        {
            ResponseRouteList theResponse = new ResponseRouteList();

            openDataConnection();

            SqlCommand cmdAllRouteMaps = new SqlCommand("DotNetGetAllRouteMappingsWithStoreDetails", theConnection);
            cmdAllRouteMaps.Parameters.AddWithValue(@"StartRowIndex", startIndex);
            cmdAllRouteMaps.Parameters.AddWithValue(@"MaximumRows", maxRows);
            cmdAllRouteMaps.Parameters.AddWithValue(@"ProviderId", providerId);
            cmdAllRouteMaps.CommandType = System.Data.CommandType.StoredProcedure;

            theReader = cmdAllRouteMaps.ExecuteReader();

            if (theReader.HasRows)
            {
                List<Route> allRoutes = new List<Route>();

                string currentRouteName = "";
                Route currentRoute = null;

                while (theReader.Read())
                {
                    if (theReader["RouteName"].ToString().Equals(""))
                    {
                        continue;
                    }

                    if (!currentRouteName.Equals(theReader["RouteName"].ToString()))
                    {
                        currentRoute = new Route();
                        currentRoute.routeName = theReader["RouteName"].ToString();
                        currentRoute.routeID = (int)theReader["RouteID"];
                        currentRoute.routeStatus = (int)theReader["Status"];
                        currentRoute.stores = new List<Store>();

                        currentRoute.cdcName = theReader["CDCName"].ToString();

                        currentRouteName = currentRoute.routeName;

                        allRoutes.Add(currentRoute);
                    }

                    Store thisStore = new Store();

                    thisStore.storeName = theReader["StoreName"].ToString();
                    thisStore.storeNumber = theReader["StoreNumber"].ToString();
                    
                    currentRoute.stores.Add(thisStore);
                }

                theResponse.routes = allRoutes;

                theResponse.statusCode = 0;
                theResponse.statusDescription = "";
            }
            else
            {
                theResponse.statusCode = 4;
                theResponse.statusDescription = "There are no route-store mappings defined";
            }

            theReader.Close();

            SqlCommand cmdCheckRecordCount;
            if (string.IsNullOrEmpty(providerId))
                cmdCheckRecordCount = new SqlCommand("SELECT COUNT(Distinct Route.RouteID) FROM Route JOIN RouteStoreMap ON " +
                           "Route.RouteID = RouteStoreMap.RouteID JOIN CDC ON Route.CDCID = CDC.CDCID  And RouteStoreMap.State=1 And RouteName <> '' ", theConnection);
            else
                cmdCheckRecordCount = new SqlCommand("SELECT COUNT(Distinct Route.RouteID) FROM Route JOIN RouteStoreMap ON " +
                         "Route.RouteID = RouteStoreMap.RouteID JOIN CDC ON Route.CDCID = CDC.CDCID  And CDC.CDCID = " + providerId + " And RouteStoreMap.State=1 And RouteName <> '' ", theConnection);

            int RecordCount = (int)cmdCheckRecordCount.ExecuteScalar();

            closeDataConnection();

            theResponse.numberOfRecords = RecordCount;
            return theResponse;
        }

        public ResponseRouteList DotNetGetAllFilteredRouteMappings(string filterText, int startIndex, int maxRows, string providerId)
        {
            ResponseRouteList theResponse = new ResponseRouteList();

            openDataConnection();

            SqlCommand cmdAllRouteMaps = new SqlCommand("DotNetGetAllFilteredRouteMappingsWithStoreDetails", theConnection);
            cmdAllRouteMaps.Parameters.AddWithValue(@"FilterText", filterText);
            cmdAllRouteMaps.Parameters.AddWithValue(@"StartRowIndex", startIndex);
            cmdAllRouteMaps.Parameters.AddWithValue(@"MaximumRows", maxRows);
            cmdAllRouteMaps.Parameters.AddWithValue(@"ProviderId", providerId);
            cmdAllRouteMaps.CommandType = System.Data.CommandType.StoredProcedure;

            theReader = cmdAllRouteMaps.ExecuteReader();

            if (theReader.HasRows)
            {
                List<Route> allRoutes = new List<Route>();

                string currentRouteName = "";
                Route currentRoute = null;

                while (theReader.Read())
                {
                    if (theReader["RouteName"].ToString().Equals(""))
                    {
                        continue;
                    }

                    if (!currentRouteName.Equals(theReader["RouteName"].ToString()))
                    {
                        currentRoute = new Route();
                        currentRoute.routeName = theReader["RouteName"].ToString();
                        currentRoute.routeID = (int)theReader["RouteID"];
                        currentRoute.routeStatus = (int)theReader["Status"];
                        currentRoute.stores = new List<Store>();

                        currentRoute.cdcName = theReader["CDCName"].ToString();

                        currentRouteName = currentRoute.routeName;

                        allRoutes.Add(currentRoute);
                    }

                    Store thisStore = new Store();

                    thisStore.storeName = theReader["StoreName"].ToString();
                    thisStore.storeNumber = theReader["StoreNumber"].ToString();
                    
                    currentRoute.stores.Add(thisStore);
                }

                theResponse.routes = allRoutes;

                theResponse.statusCode = 0;
                theResponse.statusDescription = "";
            }
            else
            {
                theResponse.statusCode = 4;
                theResponse.statusDescription = "There are no route-store mappings defined";
            }

            theReader.Close();

            SqlCommand cmdCheckRecordCount;
            if (string.IsNullOrEmpty(providerId))
                cmdCheckRecordCount = new SqlCommand("SELECT COUNT(Distinct Route.RouteID) FROM CDC JOIN Route ON CDC.CDCID = Route.CDCID	JOIN RouteStoreMap ON Route.RouteID = RouteStoreMap.RouteID " +
                                                     "JOIN Store ON RouteStoreMap.StoreID = Store.StoreID WHERE RouteStoreMap.State = 1 AND (Route.RouteName LIKE '%" + filterText + "%'  OR CDC.CDCName  LIKE '%" + filterText + "%'  OR Store.StoreNumber LIKE '" + filterText + "') ", theConnection);
            else
                cmdCheckRecordCount = new SqlCommand("SELECT COUNT(Distinct Route.RouteID) FROM CDC JOIN Route ON CDC.CDCID = Route.CDCID	JOIN RouteStoreMap ON Route.RouteID = RouteStoreMap.RouteID " +
                                                     "JOIN Store ON RouteStoreMap.StoreID = Store.StoreID WHERE RouteStoreMap.State = 1 AND CDC.CDCID = " + providerId + " AND (Route.RouteName LIKE '%" + filterText + "%'  OR CDC.CDCName  LIKE '%" + filterText + "%'  OR Store.StoreNumber LIKE '" + filterText + "') ", theConnection);

            int RecordCount = (int)cmdCheckRecordCount.ExecuteScalar();

            closeDataConnection();

            theResponse.numberOfRecords = RecordCount;
            return theResponse;
        }

        //public ResponseRouteList LinqGetAllRouteMappings(string providerId, int startIndex, int maxRows)
        //{

        //    ResponseRouteList theResponse = new ResponseRouteList();
        //    DataClassesDataContext dc = new DataClassesDataContext();

        //    // Querying and creating a Route object using LINQ

        //    IQueryable<Route> RouteQuery;
        //    if (string.IsNullOrEmpty(providerId))
        //    {
        //        RouteQuery = (from route in dc.Routes
        //                      join cdc in dc.CDCs on route.CDCID equals cdc.CDCID
        //                      select new Route
        //                      {
        //                          routeID = route.RouteID,
        //                          routeName = route.RouteName,
        //                          routeStatus = (int)route.status,
        //                          cdcName = cdc.CDCName
        //                      }).Skip(startIndex).Take(maxRows);
        //    }
        //    else
        //    {
        //        RouteQuery = (from route in dc.Routes
        //                      join cdc in dc.CDCs on route.CDCID equals cdc.CDCID
        //                      where cdc.ProviderID == Convert.ToInt32(providerId)
        //                      select new Route
        //                      {
        //                          routeID = route.RouteID,
        //                          routeName = route.RouteName,
        //                          routeStatus = (int)route.status,
        //                          cdcName = cdc.CDCName
        //                      }).Skip(startIndex).Take(maxRows);
        //    }
        //    IList<Route> allRoutes = new List<Route>();

        //    foreach (Route route in RouteQuery)
        //    {
        //        Route currentRoute = new Route
        //        {
        //            routeName = route.routeName,
        //            routeID = route.routeID,
        //            routeStatus = route.routeStatus,
        //            cdcName = route.cdcName
        //        };

        //        currentRoute.stores = new List<Store>();

        //        allRoutes.Add(currentRoute);

        //        // Querying and creating a Store object using LINQ
        //        IQueryable<Store> StoreQuery = from routeStore in dc.RouteStoreMaps
        //                                       join store in dc.Stores on routeStore.StoreID equals store.StoreID
        //                                       where (routeStore.RouteID == currentRoute.routeID) && (routeStore.State == true)
        //                                       select new Store
        //                                       {
        //                                           storeID = store.StoreID,
        //                                           storeName = store.StoreName,
        //                                           storeNumber = store.StoreNumber
        //                                       };
        //        foreach (Store store in StoreQuery)
        //        {
        //            Store thisStore = new Store
        //            {
        //                storeID = store.storeID,
        //                storeName = store.storeName,
        //                storeNumber = store.storeNumber
        //            };
        //            currentRoute.stores.Add(thisStore);
        //        }
        //    }
        //    theResponse.routes = (List<Route>)allRoutes;
        //    theResponse.statusCode = 0;
        //    theResponse.statusDescription = "";

        //    theResponse.numberOfRecords = (from route in dc.Routes
        //                                   join cdc in dc.CDCs on route.CDCID equals cdc.CDCID
        //                                   select route).Count();

        //    return theResponse;

        //}

        //public ResponseRouteList LinqGetAllFilteredRouteMappings(string filterText, int startIndex, int maxRows)
        //{
        //    ResponseRouteList theResponse = new ResponseRouteList();
        //    DataClassesDataContext dc = new DataClassesDataContext();
        //    int rowCnt = 0;
        //    IList<Route> allRoutes = new List<Route>();

        //    IQueryable<Route> RouteQuery =
        //        (from route in dc.Routes
        //         join cdc in dc.CDCs on route.CDCID equals cdc.CDCID
        //         select new Route
        //         {
        //             routeID = route.RouteID,
        //             routeName = route.RouteName,
        //             routeStatus = (int)route.status,
        //             cdcName = cdc.CDCName
        //         }).Skip(startIndex);

        //    foreach (Route r in RouteQuery)
        //    {
        //        //Check if the filter text exist in route name or cdc name
        //        if (r.routeName.ToLower().Contains(filterText) || r.cdcName.ToLower().Contains(filterText))
        //        {
        //            rowCnt++;
        //            r.stores = new List<Store>();
        //            allRoutes.Add(r);

        //            // Querying and creating a Store object using LINQ
        //            IQueryable<Store> StoreQuery = from routeStore in dc.RouteStoreMaps
        //                                           join store in dc.Stores on routeStore.StoreID equals store.StoreID
        //                                           where (routeStore.RouteID == r.routeID) && (routeStore.State == true)
        //                                           select new Store
        //                                           {
        //                                               storeID = store.StoreID,
        //                                               storeName = store.StoreName,
        //                                               storeNumber = store.StoreNumber
        //                                           };
        //            foreach (Store store in StoreQuery)
        //            {
        //                Store thisStore = new Store
        //                {
        //                    storeID = store.storeID,
        //                    storeName = store.storeName,
        //                    storeNumber = store.storeNumber
        //                };
        //                r.stores.Add(thisStore);
        //            }
        //        }
        //        else
        //        {
        //            //Check if the filter text exist in store number
        //            IQueryable<Store> StoreFilterQuery = from routeStore in dc.RouteStoreMaps
        //                                                 join store in dc.Stores on routeStore.StoreID equals store.StoreID
        //                                                 where (routeStore.RouteID == r.routeID) && (routeStore.State == true) && (store.StoreNumber.ToLower().Contains(filterText))
        //                                                 select new Store
        //                                                 {
        //                                                     storeID = store.StoreID,
        //                                                     storeName = store.StoreName,
        //                                                     storeNumber = store.StoreNumber
        //                                                 };
        //            if (StoreFilterQuery.Count() > 0) // if filter text exist in store number
        //            {
        //                rowCnt++;
        //                r.stores = new List<Store>();
        //                allRoutes.Add(r);
        //                IQueryable<Store> StoreQuery = from routeStore in dc.RouteStoreMaps
        //                                               join store in dc.Stores on routeStore.StoreID equals store.StoreID
        //                                               where (routeStore.RouteID == r.routeID) && (routeStore.State == true)
        //                                               select new Store
        //                                               {
        //                                                   storeID = store.StoreID,
        //                                                   storeName = store.StoreName,
        //                                                   storeNumber = store.StoreNumber
        //                                               };
        //                foreach (Store store in StoreQuery)
        //                {
        //                    Store thisStore = new Store
        //                    {
        //                        storeID = store.storeID,
        //                        storeName = store.storeName,
        //                        storeNumber = store.storeNumber
        //                    };
        //                    r.stores.Add(thisStore);
        //                }
        //            }
        //        }

        //        if (rowCnt == maxRows)
        //            break;
        //    }

        //    theResponse.routes = (List<Route>)allRoutes;
        //    return theResponse;
        //}

        public ResponseRouteList GetRouteDetail(string routeName)
        {
            ResponseRouteList theResponse = new ResponseRouteList();

            openDataConnection();

            SqlCommand cmdRouteMap = new SqlCommand("GetRouteMappingWithStoreDetail", theConnection);
            cmdRouteMap.Parameters.AddWithValue("@routeName", routeName);
            cmdRouteMap.CommandType = System.Data.CommandType.StoredProcedure;

            theReader = cmdRouteMap.ExecuteReader();

            if (theReader.HasRows)
            {
                bool firstRead = true;

                List<Route> allRoutes = new List<Route>();

                Route thisRoute = new Route();
                thisRoute.routeName = routeName;

                thisRoute.stores = new List<Store>();

                allRoutes.Add(thisRoute);

                while (theReader.Read())
                {
                    if (firstRead)
                    {
                        thisRoute.routeID = (int)theReader["RouteID"];
                        thisRoute.cdc = new CDC(theReader["CDCName"].ToString(), (int)theReader["CDCID"]);
                        thisRoute.cdcName = theReader["CDCName"].ToString();
                        firstRead = false;
                    }

                    Store thisStore = new Store();

                    thisStore.storeID = (int)theReader["StoreID"];
                    thisStore.storeName = theReader["StoreName"].ToString();
                    thisStore.storeAddress = theReader["StoreAddress"].ToString();
                    thisStore.storeCity = theReader["StoreCity"].ToString();
                    thisStore.storeZip = theReader["StoreZip"].ToString();
                    thisStore.storeState = theReader["StoreState"].ToString();
                    thisStore.storePhone = theReader["StorePhone"].ToString();
                    thisStore.storeManagerName = theReader["StoreManagerName"].ToString();
                    thisStore.storeEmailAddress = theReader["StoreEmail"].ToString();
                    thisStore.storeNumber = theReader["StoreNumber"].ToString();
                    thisStore.storeOwnershipType = theReader["StoreOwnershipType"].ToString();

                    thisRoute.stores.Add(thisStore);
                }

                theResponse.routes = allRoutes;

                theResponse.statusCode = 0;
                theResponse.statusDescription = "";
            }
            else
            {
                theResponse.statusCode = 4;
                theResponse.statusDescription = "There are no route-store mappings defined for " + routeName;
            }

            theReader.Close();

            closeDataConnection();

            return theResponse;
        }

        public Response CreateRoute(Route routeModel)
        {
            Response theResponse = new Response();

            if (routeModel != null)
            {
                if (routeModel.routeName == null)
                {
                    theResponse.statusDescription = "Route Name not supplied";
                }
                if (routeModel.cdc == null || routeModel.cdc.id == 0)
                {
                    theResponse.statusDescription = "CDC information not supplied";
                }

                if (theResponse.statusDescription.Equals(""))
                {
                    openDataConnection();

                    SqlCommand cmdCheck = new SqlCommand("RouteExists", theConnection);
                    cmdCheck.Parameters.AddWithValue("@routeName", routeModel.routeName);
                    cmdCheck.CommandType = System.Data.CommandType.StoredProcedure;

                    theReader = cmdCheck.ExecuteReader();

                    if (theReader.HasRows)
                    {
                        theReader.Close();

                        theResponse.statusCode = 3;
                        theResponse.statusDescription = "The route name " + routeModel.routeName + " already exists";
                    }
                    else
                    {
                        theReader.Close();

                        SqlCommand cmdCreate = new SqlCommand("CreateRoute", theConnection);
                        cmdCreate.Parameters.AddWithValue("@routeName", routeModel.routeName);
                        cmdCreate.Parameters.AddWithValue("@cdcID", routeModel.cdc.id);
                        cmdCreate.CommandType = System.Data.CommandType.StoredProcedure;

                        try
                        {
                            int numRowsAffected = cmdCreate.ExecuteNonQuery();

                            if (numRowsAffected > 0)
                            {
                                if (routeModel.stores != null)
                                {
                                    int totalStoresGiven = routeModel.stores.Count;
                                    int totalStoresAdded = 0;

                                    for (int i = 0; i < totalStoresGiven; i++)
                                    {
                                        int thisStoreID = routeModel.stores[i].storeID;

                                        SqlCommand cmdAddStoreToRoute = new SqlCommand("AddStoreToRoute", theConnection);
                                        cmdAddStoreToRoute.Parameters.AddWithValue("@routeName", routeModel.routeName);
                                        cmdAddStoreToRoute.Parameters.AddWithValue("@storeID", thisStoreID);
                                        cmdAddStoreToRoute.CommandType = System.Data.CommandType.StoredProcedure;

                                        int numRowsAffectedForAddStoreToRoute = cmdAddStoreToRoute.ExecuteNonQuery();

                                        if (numRowsAffectedForAddStoreToRoute > 0)
                                        {
                                            totalStoresAdded++;
                                        }
                                    }

                                    if (totalStoresAdded == totalStoresGiven)
                                    {
                                        theResponse.statusCode = 0;
                                        theResponse.statusDescription = "";
                                    }
                                    else
                                    {
                                        theResponse.statusCode = 0;
                                        theResponse.statusDescription = "Only " + totalStoresAdded + " out of " + totalStoresGiven + " were added to the route " + routeModel.routeName;
                                    }
                                }
                                else
                                {
                                    theResponse.statusCode = 0;
                                    theResponse.statusDescription = "";
                                }
                            }
                            else
                            {
                                theResponse.statusCode = 6;
                            }
                        }
                        catch (Exception _exception)
                        {
                            theResponse.statusCode = 6;
                            theResponse.statusDescription = _exception.Message;
                        }
                    }

                    closeDataConnection();
                }
                else
                {
                    theResponse.statusCode = 6;
                }
            }
            else
            {
                theResponse.statusCode = 6;
                theResponse.statusDescription = "Route Model was expected but not supplied";
            }

            return theResponse;
        }

        public Response AddStoreToRoute(string routeName, string storeID)
        {
            Response theResponse = new Response();

            if (routeName.Equals("") || storeID.Equals(""))
            {
                theResponse.statusCode = 1;
                theResponse.statusDescription = "Missing required parameters";

                return theResponse;
            }

            openDataConnection();

            SqlCommand cmdCheckMap = new SqlCommand("CheckIfRouteMapExists", theConnection);
            cmdCheckMap.Parameters.AddWithValue("@routeName", routeName);
            cmdCheckMap.Parameters.AddWithValue("@storeID", storeID);
            cmdCheckMap.CommandType = System.Data.CommandType.StoredProcedure;

            theReader = cmdCheckMap.ExecuteReader();

            if (theReader.HasRows)
            {
                theReader.Close();

                theResponse.statusCode = 3;
                theResponse.statusDescription = "The Mapping [Route " + routeName + " -> " + storeID + "] already exists";
            }
            else
            {
                theReader.Close();

                SqlCommand cmdAddMap = new SqlCommand("AddStoreToRoute", theConnection);
                cmdAddMap.Parameters.AddWithValue("@routeName", routeName);
                cmdAddMap.Parameters.AddWithValue("@storeID", storeID);
                cmdAddMap.CommandType = System.Data.CommandType.StoredProcedure;

                try
                {
                    int numRowsAffected = cmdAddMap.ExecuteNonQuery();

                    if (numRowsAffected > 0)
                    {
                        theResponse.statusCode = 0;
                        theResponse.statusDescription = "";
                    }
                    else
                    {
                        theResponse.statusCode = 6;
                        theResponse.statusDescription = "The Mapping [Route " + routeName + " -> " + storeID + "] could not be added";
                    }
                }
                catch (Exception theException)
                {
                    theResponse.statusCode = 6;
                    theResponse.statusDescription = theException.Message;
                }
            }

            closeDataConnection();

            return theResponse;
        }

        public ResponseReasonList GetAllParentReasons()
        {
            ResponseReasonList theResponse = new ResponseReasonList();

            openDataConnection();

            SqlCommand cmdGet = new SqlCommand("GetAllParentReasons", theConnection);
            cmdGet.CommandType = System.Data.CommandType.StoredProcedure;

            theReader = cmdGet.ExecuteReader();

            if (theReader.HasRows)
            {
                List<Reason> allReasons = new List<Reason>();

                while (theReader.Read())
                {
                    Reason thisReason = new Reason();
                    thisReason.reasonCode = (int)theReader["ReasonID"];
                    thisReason.reasonName = theReader["ReasonName"].ToString();

                    allReasons.Add(thisReason);
                }

                theResponse.reasons = allReasons;

                theResponse.statusCode = 0;
                theResponse.statusDescription = "";
            }
            else
            {
                theResponse.statusCode = 4;
                theResponse.statusDescription = "There are no reasons defined";
            }

            theReader.Close();

            closeDataConnection();

            return theResponse;
        }

        public ResponseReasonWithChildrenList GetAllParentReasonsWithChildren()
        {
            ResponseReasonWithChildrenList theResponse = new ResponseReasonWithChildrenList();

            openDataConnection();

            SqlCommand cmdGet = new SqlCommand("GetAllParentReasonsWithChildren", theConnection);
            cmdGet.CommandType = System.Data.CommandType.StoredProcedure;

            theReader = cmdGet.ExecuteReader();

            if (theReader.HasRows)
            {
                List<ReasonWithChildren> allReasons = new List<ReasonWithChildren>();

                int currentReasonID = 0;
                ReasonWithChildren currentReason = null;

                while (theReader.Read())
                {
                    int thisReasonID = (int)theReader["ReasonID"];

                    if (thisReasonID != currentReasonID)
                    {
                        currentReason = new ReasonWithChildren();
                        currentReason.reasonCode = thisReasonID;
                        currentReason.reasonName = theReader["ReasonName"].ToString();
                        currentReason.children = new List<ReasonChild>();

                        allReasons.Add(currentReason);

                        currentReasonID = thisReasonID;
                    }

                    if (theReader["ChildReasonName"] != DBNull.Value)
                    {
                        ReasonChild thisChildReason = new ReasonChild();
                        thisChildReason.reasonCode = thisReasonID;
                        thisChildReason.childReasonCode = (int)theReader["ChildReasonID"];
                        thisChildReason.childReasonName = theReader["ChildReasonName"].ToString();
                        thisChildReason.childReasonExplanation = theReader["ChildReasonExplanation"].ToString();
                        thisChildReason.escalation = (bool)theReader["Escalation"];
                        thisChildReason.photoRequired = (bool)theReader["PhotoRequired"];
                        if (theReader["ValueRequired"] == DBNull.Value)
                        {
                            thisChildReason.valueRequired = false;
                        }
                        else
                        {
                            thisChildReason.valueRequired = (bool)theReader["ValueRequired"];
                        }
                        if (theReader["PODRequired"] == DBNull.Value)
                        {
                            thisChildReason.PODRequired = false;
                        }
                        else
                        {
                            thisChildReason.PODRequired = (bool)theReader["PODRequired"];
                        }
                        if (theReader["ValueUnit"] == DBNull.Value)
                        {
                            thisChildReason.valueUnit = "";
                        }
                        else
                        {
                            thisChildReason.valueUnit = theReader["ValueUnit"].ToString();
                        }
                        if (theReader["ValueUnitPrice"] == DBNull.Value)
                        {
                            thisChildReason.valueUnitPrice = 0;
                        }
                        else
                        {
                            thisChildReason.valueUnitPrice = (float)Double.Parse(theReader["ValueUnitPrice"].ToString());
                        }

                        currentReason.children.Add(thisChildReason);
                    }
                }

                theResponse.reasons = allReasons;

                theResponse.statusCode = 0;
                theResponse.statusDescription = "";
            }
            else
            {
                theResponse.statusCode = 4;
                theResponse.statusDescription = "There are no reasons defined";
            }

            theReader.Close();

            closeDataConnection();

            return theResponse;
        }

        public ResponseReasonWithChildrenList GetChildrenOfParentReason(string reasonCode)
        {
            ResponseReasonWithChildrenList theResponse = new ResponseReasonWithChildrenList();

            openDataConnection();

            SqlCommand cmdGet = new SqlCommand("GetAllChildrenOfParentReason", theConnection);
            cmdGet.Parameters.AddWithValue("@parentReasonID", reasonCode);
            cmdGet.CommandType = System.Data.CommandType.StoredProcedure;

            theReader = cmdGet.ExecuteReader();

            if (theReader.HasRows)
            {
                List<ReasonWithChildren> allReasons = new List<ReasonWithChildren>();

                ReasonWithChildren theReason = new ReasonWithChildren();
                theReason.children = new List<ReasonChild>();

                allReasons.Add(theReason);

                while (theReader.Read())
                {
                    if (theReason.reasonName == null)
                    {
                        theReason.reasonName = theReader["ReasonName"].ToString();
                        theReason.reasonCode = (int)theReader["ReasonID"];
                    }

                    ReasonChild thisChildReason = new ReasonChild();
                    thisChildReason.childReasonCode = (int)theReader["ChildReasonID"];
                    thisChildReason.childReasonName = theReader["ChildReasonName"].ToString();
                    thisChildReason.childReasonExplanation = theReader["ChildReasonExplanation"].ToString();
                    thisChildReason.escalation = (bool)theReader["Escalation"];
                    thisChildReason.photoRequired = (bool)theReader["PhotoRequired"];
                    thisChildReason.reasonCode = theReason.reasonCode;
                    if (theReader["ValueRequired"] == DBNull.Value)
                    {
                        thisChildReason.valueRequired = false;
                    }
                    else
                    {
                        thisChildReason.valueRequired = (bool)theReader["ValueRequired"];
                    }
                    if (theReader["PODRequired"] == DBNull.Value)
                    {
                        thisChildReason.PODRequired = false;
                    }
                    else
                    {
                        thisChildReason.PODRequired = (bool)theReader["PODRequired"];
                    }
                    if (theReader["ValueUnit"] == DBNull.Value)
                    {
                        thisChildReason.valueUnit = "";
                    }
                    else
                    {
                        thisChildReason.valueUnit = theReader["ValueUnit"].ToString();
                    }
                    if (theReader["ValueUnitPrice"] == DBNull.Value)
                    {
                        thisChildReason.valueUnitPrice = 0;
                    }
                    else
                    {
                        thisChildReason.valueUnitPrice = (float)Double.Parse(theReader["ValueUnitPrice"].ToString());
                    }

                    theReason.children.Add(thisChildReason);
                }

                theResponse.reasons = allReasons;

                theResponse.statusCode = 0;
                theResponse.statusDescription = "";
            }
            else
            {
                theResponse.statusCode = 4;
                theResponse.statusDescription = "There are no reasons defined";
            }

            theReader.Close();

            closeDataConnection();

            return theResponse;
        }

        public Response CreateReason(Reason aReasonModel)
        {
            Response theResponse = new Response();

            if (aReasonModel != null)
            {
                if (aReasonModel.reasonName == null)
                {
                    theResponse.statusDescription = "Reason Name not supplied";
                }
                if (parentReasonExists(aReasonModel.reasonName))
                {
                    theResponse.statusCode = 3;
                    theResponse.statusDescription = "Reason name already exists";

                    return theResponse;
                }

                if (theResponse.statusDescription.Equals(""))
                {
                    openDataConnection();

                    SqlCommand cmdCreateReason = new SqlCommand("CreateParentReason", theConnection);
                    cmdCreateReason.Parameters.AddWithValue("@reasonName", aReasonModel.reasonName);
                    cmdCreateReason.CommandType = System.Data.CommandType.StoredProcedure;

                    try
                    {
                        int numRowsAffected = cmdCreateReason.ExecuteNonQuery();

                        if (numRowsAffected > 0)
                        {
                            theResponse.statusCode = 0;
                            theResponse.statusDescription = "";
                        }
                        else
                        {
                            theResponse.statusCode = 6;
                            theResponse.statusDescription = "The specified reason could not be added";
                        }
                    }
                    catch (Exception _exception)
                    {
                        theResponse.statusCode = 6;
                        theResponse.statusDescription = _exception.Message;
                    }

                    closeDataConnection();
                }
                else
                {
                    theResponse.statusCode = 6;
                }
            }
            else
            {
                theResponse.statusCode = 6;
                theResponse.statusDescription = "Expected Reason Model not received";
            }

            return theResponse;
        }

        public Response UpdateReason(Reason aReasonModel)
        {
            Response theResponse = new Response();

            if (aReasonModel != null)
            {
                if (aReasonModel.reasonName == null)
                {
                    theResponse.statusDescription = "Updated Reason Name not supplied";
                }
                if (aReasonModel.reasonCode < 1)
                {
                    theResponse.statusDescription = "Reason Code ID not supplied";
                }

                if (theResponse.statusDescription.Equals(""))
                {
                    openDataConnection();

                    SqlCommand cmdUpdate = new SqlCommand("UpdateParentReason", theConnection);
                    cmdUpdate.Parameters.AddWithValue("@reasonID", aReasonModel.reasonCode);
                    cmdUpdate.Parameters.AddWithValue("@reasonName", aReasonModel.reasonName);
                    cmdUpdate.CommandType = System.Data.CommandType.StoredProcedure;

                    try
                    {
                        int numRowsAffected = cmdUpdate.ExecuteNonQuery();

                        if (numRowsAffected > 0)
                        {
                            theResponse.statusCode = 0;
                            theResponse.statusDescription = "";
                        }
                        else
                        {
                            theResponse.statusCode = 6;
                        }
                    }
                    catch (Exception _exception)
                    {
                        theResponse.statusCode = 6;
                        theResponse.statusDescription = _exception.Message;
                    }

                    closeDataConnection();
                }
                else
                {
                    theResponse.statusCode = 6;
                }
            }
            else
            {
                theResponse.statusCode = 6;
                theResponse.statusDescription = "Expected Reason Model not received";
            }

            return theResponse;
        }

        public Response CreateReasonChild(ReasonChildWithParent aChildReasonModel)
        {
            Response theResponse = new Response();

            if (aChildReasonModel != null)
            {
                if (aChildReasonModel.childReasonName == null)
                {
                    theResponse.statusDescription = "Child Reason Name was not supplied";
                }
                if (aChildReasonModel.childReasonExplanation == null)
                {
                    theResponse.statusDescription = "Child Reason Explanation was not supplied";
                }
                if (aChildReasonModel.parentReason == null)
                {
                    theResponse.statusDescription = "Child Reason's Parent Reason was not supplied";
                }
                if (childReasonExists(aChildReasonModel.childReasonName))
                {
                    theResponse.statusCode = 3;
                    theResponse.statusDescription = "Child Reason already exists";

                    return theResponse;
                }

                if (theResponse.statusDescription.Equals(""))
                {
                    openDataConnection();

                    SqlCommand cmdCreateChildReason = new SqlCommand("CreateChildReason", theConnection);
                    cmdCreateChildReason.Parameters.AddWithValue("@parentReasonID", aChildReasonModel.parentReason.reasonCode.ToString());
                    cmdCreateChildReason.Parameters.AddWithValue("@childReasonName", aChildReasonModel.childReasonName);
                    cmdCreateChildReason.Parameters.AddWithValue("@childReasonExplanation", aChildReasonModel.childReasonExplanation);
                    cmdCreateChildReason.Parameters.AddWithValue("@escalation", (object)aChildReasonModel.escalation);
                    cmdCreateChildReason.Parameters.AddWithValue("@photoRequired", (object)aChildReasonModel.photoRequired);
                    cmdCreateChildReason.Parameters.AddWithValue("@valueRequired", (object)aChildReasonModel.valueRequired);
                    cmdCreateChildReason.Parameters.AddWithValue("@PODRequired", (object)aChildReasonModel.PODRequired);
                    cmdCreateChildReason.Parameters.AddWithValue("@valueUnitPrice", aChildReasonModel.valueUnitPrice);
                    cmdCreateChildReason.CommandType = System.Data.CommandType.StoredProcedure;

                    try
                    {
                        int numRowsAffected = cmdCreateChildReason.ExecuteNonQuery();

                        if (numRowsAffected > 0)
                        {
                            theResponse.statusCode = 0;
                            theResponse.statusDescription = "";
                        }
                        else
                        {
                            theResponse.statusCode = 6;
                        }
                    }
                    catch (Exception _exception)
                    {
                        theResponse.statusCode = 6;
                        theResponse.statusDescription = _exception.Message;
                    }

                    closeDataConnection();
                }
                else
                {
                    theResponse.statusCode = 6;
                }
            }
            else
            {
                theResponse.statusCode = 6;
                theResponse.statusDescription = "Expected Child Reason Model not received";
            }

            return theResponse;
        }

        public Response UpdateReasonChild(ReasonChildWithParent aChildReasonModel)
        {
            Response theResponse = new Response();

            if (aChildReasonModel != null)
            {
                if (aChildReasonModel.childReasonCode < 1)
                {
                    theResponse.statusDescription = "Child Reason Code was not supplied";
                }
                if (aChildReasonModel.childReasonName == null)
                {
                    theResponse.statusDescription = "Child Reason Name was not supplied";
                }
                if (aChildReasonModel.childReasonExplanation == null)
                {
                    theResponse.statusDescription = "Child Reason Explanation was not supplied";
                }

                if (theResponse.statusDescription.Equals(""))
                {
                    openDataConnection();

                    SqlCommand cmdUpdateChildReason = new SqlCommand("UpdateChildReason", theConnection);
                    cmdUpdateChildReason.Parameters.AddWithValue("@childReasonID", aChildReasonModel.childReasonCode);
                    cmdUpdateChildReason.Parameters.AddWithValue("@childReasonName", aChildReasonModel.childReasonName);
                    cmdUpdateChildReason.Parameters.AddWithValue("@childReasonExplanation", aChildReasonModel.childReasonExplanation);
                    cmdUpdateChildReason.Parameters.AddWithValue("@escalation", (object)aChildReasonModel.escalation);
                    cmdUpdateChildReason.Parameters.AddWithValue("@photoRequired", (object)aChildReasonModel.photoRequired);
                    cmdUpdateChildReason.Parameters.AddWithValue("@valueRequired", (object)aChildReasonModel.valueRequired);
                    cmdUpdateChildReason.Parameters.AddWithValue("@valueUnitPrice", aChildReasonModel.valueUnitPrice);
                    cmdUpdateChildReason.Parameters.AddWithValue("@PODRequired", (object)aChildReasonModel.PODRequired);
                    cmdUpdateChildReason.CommandType = System.Data.CommandType.StoredProcedure;

                    try
                    {
                        int numRowsAffected = cmdUpdateChildReason.ExecuteNonQuery();

                        if (numRowsAffected > 0)
                        {
                            theResponse.statusCode = 0;
                            theResponse.statusDescription = "";
                        }
                        else
                        {
                            theResponse.statusCode = 6;
                        }
                    }
                    catch (Exception _exception)
                    {
                        theResponse.statusCode = 6;
                        theResponse.statusDescription = _exception.Message;
                    }

                    closeDataConnection();
                }
                else
                {
                    theResponse.statusCode = 6;
                }
            }
            else
            {
                theResponse.statusCode = 6;
                theResponse.statusDescription = "Expected Child Reason Model not received";
            }

            return theResponse;
        }

        public ResponseCDCList GetAllCDCs()
        {
            ResponseCDCList theResponse = new ResponseCDCList();

            openDataConnection();

            SqlCommand cmdRead = new SqlCommand("GetAllCDCs", theConnection);
            cmdRead.CommandType = System.Data.CommandType.StoredProcedure;

            theReader = cmdRead.ExecuteReader();

            if (theReader.HasRows)
            {
                List<CDC> allCDCs = new List<CDC>();

                while (theReader.Read())
                {
                    CDC thisCDC = new CDC();
                    thisCDC.id = (int)theReader["CDCID"];
                    thisCDC.name = theReader["CDCName"].ToString();
                    thisCDC.address = theReader["CDCAddress"].ToString();
                    thisCDC.state = theReader["CDCState"].ToString();
                    thisCDC.zip = theReader["CDCZip"].ToString();
                    thisCDC.phone = theReader["CDCPhone"].ToString();
                    thisCDC.email = theReader["CDCEmailAddress"].ToString();
                    thisCDC.providerID = (int)theReader["ProviderID"];

                    allCDCs.Add(thisCDC);
                }

                theResponse.cdcs = allCDCs;

                theResponse.statusCode = 0;
                theResponse.statusDescription = "";
            }
            else
            {
                theResponse.statusCode = 4;
                theResponse.statusDescription = "No CDCs have been defined";
            }

            theReader.Close();

            closeDataConnection();

            return theResponse;
        }

        public ResponseCDCList GetAllCDCsForProvider(string providerID)
        {
            ResponseCDCList theResponse = new ResponseCDCList();

            openDataConnection();

            SqlCommand cmdRead = new SqlCommand("GetAllCDCs", theConnection);
            cmdRead.CommandType = System.Data.CommandType.StoredProcedure;

            theReader = cmdRead.ExecuteReader();

            if (theReader.HasRows)
            {
                List<CDC> allCDCs = new List<CDC>();

                int wantedProviderID = Int32.Parse(providerID);

                while (theReader.Read())
                {
                    int thisProviderID = (int)theReader["ProviderID"];

                    if (thisProviderID > 0 && thisProviderID == wantedProviderID)
                    {
                        CDC thisCDC = new CDC();
                        thisCDC.id = (int)theReader["CDCID"];
                        thisCDC.name = theReader["CDCName"].ToString();
                        thisCDC.address = theReader["CDCAddress"].ToString();
                        thisCDC.state = theReader["CDCState"].ToString();
                        thisCDC.zip = theReader["CDCZip"].ToString();
                        thisCDC.phone = theReader["CDCPhone"].ToString();
                        thisCDC.email = theReader["CDCEmailAddress"].ToString();
                        thisCDC.providerID = (int)theReader["ProviderID"];

                        allCDCs.Add(thisCDC);
                    }
                }

                theResponse.cdcs = allCDCs;

                theResponse.statusCode = 0;
                theResponse.statusDescription = "";
            }
            else
            {
                theResponse.statusCode = 4;
                theResponse.statusDescription = "No CDCs have been defined";
            }

            theReader.Close();

            closeDataConnection();

            return theResponse;
        }

        public Response CreateCDC(CDC aCDCModel)
        {
            Response theResponse = new Response();

            if (aCDCModel != null)
            {
                if (aCDCModel.name == null || aCDCModel.name.Equals(""))
                {
                    theResponse.statusDescription = "CDC Name was not provided";
                }
                if (aCDCModel.providerID == 0)
                {
                    theResponse.statusDescription = "CDC Provider ID was not provided";
                }

                if (theResponse.statusDescription.Equals(""))
                {
                    if (cdcExists(aCDCModel.name))
                    {
                        theResponse.statusCode = 3;
                        theResponse.statusDescription = "A CDC by the name of " + aCDCModel.name + " already exists";

                        return theResponse;
                    }

                    openDataConnection();

                    SqlCommand cmdCreate = null;

                    if (aCDCModel.address == null && aCDCModel.state == null && aCDCModel.zip == null && aCDCModel.phone == null && aCDCModel.email == null)
                    {
                        cmdCreate = new SqlCommand("CreateCDC", theConnection);
                    }
                    else
                    {
                        cmdCreate = new SqlCommand("CreateCDCWithDetail", theConnection);
                        cmdCreate.Parameters.AddWithValue("@cdcAddress", aCDCModel.address);
                        cmdCreate.Parameters.AddWithValue("@cdcState", aCDCModel.state);
                        cmdCreate.Parameters.AddWithValue("@cdcZip", aCDCModel.zip);
                        cmdCreate.Parameters.AddWithValue("@cdcPhone", aCDCModel.phone);
                        cmdCreate.Parameters.AddWithValue("@cdcEmailAddress", aCDCModel.email);
                    }
                    cmdCreate.Parameters.AddWithValue("@cdcName", aCDCModel.name);
                    cmdCreate.Parameters.AddWithValue("@providerID", aCDCModel.providerID);
                    cmdCreate.CommandType = System.Data.CommandType.StoredProcedure;

                    try
                    {
                        int numRowsAffected = cmdCreate.ExecuteNonQuery();

                        if (numRowsAffected > 0)
                        {
                            theResponse.statusCode = 0;
                            theResponse.statusDescription = "";
                        }
                        else
                        {
                            theResponse.statusCode = 6;
                            theResponse.statusDescription = "Specified CDC could not be added";
                        }
                    }
                    catch (Exception _exception)
                    {
                        theResponse.statusCode = 6;
                        theResponse.statusDescription = _exception.Message;
                    }

                    closeDataConnection();
                }
                else
                {
                    theResponse.statusCode = 6;
                }
            }
            else
            {
                theResponse.statusCode = 6;
                theResponse.statusDescription = "Expected CDC Model not recieved";
            }

            return theResponse;
        }

        public Response UpdateCDC(CDC aCDCModel)
        {
            Response theResponse = new Response();

            if (aCDCModel != null)
            {
                if (aCDCModel.id == null || aCDCModel.id == 0)
                {
                    theResponse.statusDescription = "CDC ID was not provided";
                }
                if (aCDCModel.name == null || aCDCModel.name.Equals(""))
                {
                    theResponse.statusDescription = "CDC Name was not provided";
                }
                if (aCDCModel.providerID == 0)
                {
                    theResponse.statusDescription = "CDC Provider ID was not provided";
                }

                if (theResponse.statusDescription.Equals(""))
                {
                    openDataConnection();

                    SqlCommand cmdUpdate = null;

                    if (aCDCModel.address == null && aCDCModel.state == null && aCDCModel.zip == null && aCDCModel.phone == null && aCDCModel.email == null)
                    {
                        cmdUpdate = new SqlCommand("UpdateCDC", theConnection);
                    }
                    else
                    {
                        cmdUpdate = new SqlCommand("UpdateCDCWithDetail", theConnection);
                        cmdUpdate.Parameters.AddWithValue("@cdcAddress", aCDCModel.address);
                        cmdUpdate.Parameters.AddWithValue("@cdcState", aCDCModel.state);
                        cmdUpdate.Parameters.AddWithValue("@cdcZip", aCDCModel.zip);
                        cmdUpdate.Parameters.AddWithValue("@cdcPhone", aCDCModel.phone);
                        cmdUpdate.Parameters.AddWithValue("@cdcEmailAddress", aCDCModel.email);
                    }
                    cmdUpdate.Parameters.AddWithValue("@cdcID", aCDCModel.id);
                    cmdUpdate.Parameters.AddWithValue("@cdcName", aCDCModel.name);
                    cmdUpdate.Parameters.AddWithValue("@providerID", aCDCModel.providerID);
                    cmdUpdate.CommandType = System.Data.CommandType.StoredProcedure;

                    try
                    {
                        int numRowsAffected = cmdUpdate.ExecuteNonQuery();

                        if (numRowsAffected > 0)
                        {
                            theResponse.statusCode = 0;
                            theResponse.statusDescription = "";
                        }
                        else
                        {
                            theResponse.statusCode = 6;
                            theResponse.statusDescription = "Specified CDC could not be updated";
                        }
                    }
                    catch (Exception _exception)
                    {
                        theResponse.statusCode = 6;
                        theResponse.statusDescription = _exception.Message;
                    }

                    closeDataConnection();
                }
                else
                {
                    theResponse.statusCode = 6;
                }
            }
            else
            {
                theResponse.statusCode = 6;
                theResponse.statusDescription = "Expected CDC Model not recieved";
            }

            return theResponse;
        }

        public ResponseTripList GetAllOpenTripsByTripId(string tripId)
        {
            ResponseTripList theResponse = new ResponseTripList();

            openDataConnection();

            SqlCommand cmdGetTrip = new SqlCommand("SELECT * FROM Trip WHERE Closed = 0 and tripid = " + tripId, theConnection);

            try
            {
                theReader = cmdGetTrip.ExecuteReader();
            }
            catch (Exception _exception)
            {
                theResponse.statusCode = 6;
                theResponse.statusDescription = _exception.Message;
            }

            if (!theReader.HasRows)
            {
                theResponse.statusCode = 4;
                theResponse.statusDescription = "No Open Trips found";

                theReader.Close();
            }
            else
            {
                theResponse.trips = new List<Trip>();

                while (theReader.Read())
                {
                    int tripID = 0;

                    tripID = (int)theReader["TripID"];

                    if (tripID > 0)
                    {
                        Trip thisTrip = new Trip();

                        thisTrip.id = tripID;
                        thisTrip.routeName = theReader["RouteName"].ToString(); ;
                        thisTrip.username = theReader["Username"].ToString();
                        thisTrip.closed = false;
                        if (theReader["DateStarted"] != DBNull.Value)
                        {
                            thisTrip.dateStarted = (DateTime)theReader["DateStarted"];

                            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);
                            TimeSpan span = (thisTrip.dateStarted - epoch);
                            double unixTime = span.TotalSeconds;

                            thisTrip.dateStartedEpoch = (int)unixTime;

                            thisTrip.dateStartedString = thisTrip.dateStarted.Hour.ToString() + ":" + thisTrip.dateStarted.Minute.ToString() + " " + thisTrip.dateStarted.Month.ToString() + "." + thisTrip.dateStarted.Day.ToString() + "." + thisTrip.dateStarted.Year.ToString();
                        }
                        if (theReader["DateClosed"] != DBNull.Value)
                        {
                            thisTrip.dateClosed = (DateTime)theReader["DateClosed"];

                            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);
                            TimeSpan span = (thisTrip.dateClosed - epoch);
                            double unixTime = span.TotalSeconds;

                            thisTrip.dateClosedEpoch = (int)unixTime;
                        }
                        if (theReader["Latitude"] != DBNull.Value)
                        {
                            thisTrip.latitude = Convert.ToSingle(theReader["Latitude"].ToString());
                        }
                        if (theReader["Longitude"] != DBNull.Value)
                        {
                            thisTrip.longitude = Convert.ToSingle(theReader["Longitude"].ToString());
                        }
                        thisTrip.tripDetails = thisTrip.username + " / " + thisTrip.routeName + " / " + thisTrip.dateStartedString;

                        theResponse.trips.Add(thisTrip);
                    }
                }

                theResponse.statusCode = 0;
                theResponse.statusDescription = "";
            }

            closeDataConnection();

            return theResponse;
        }

        public ResponseTripList GetAllOpenTrips()
        {
            ResponseTripList theResponse = new ResponseTripList();

            openDataConnection();

            SqlCommand cmdGetTrip = new SqlCommand("SELECT * FROM Trip WHERE Closed = 0", theConnection);

            try
            {
                theReader = cmdGetTrip.ExecuteReader();
            }
            catch (Exception _exception)
            {
                theResponse.statusCode = 6;
                theResponse.statusDescription = _exception.Message;
            }

            if (!theReader.HasRows)
            {
                theResponse.statusCode = 4;
                theResponse.statusDescription = "No Open Trips found";

                theReader.Close();
            }
            else
            {
                theResponse.trips = new List<Trip>();

                while (theReader.Read())
                {
                    int tripID = 0;

                    tripID = (int)theReader["TripID"];

                    if (tripID > 0)
                    {
                        Trip thisTrip = new Trip();

                        thisTrip.id = tripID;
                        thisTrip.routeName = theReader["RouteName"].ToString(); ;
                        thisTrip.username = theReader["Username"].ToString();
                        thisTrip.closed = false;
                        if (theReader["DateStarted"] != DBNull.Value)
                        {
                            thisTrip.dateStarted = (DateTime)theReader["DateStarted"];

                            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);
                            TimeSpan span = (thisTrip.dateStarted - epoch);
                            double unixTime = span.TotalSeconds;

                            thisTrip.dateStartedEpoch = (int)unixTime;

                            thisTrip.dateStartedString = thisTrip.dateStarted.Hour.ToString() + ":" + thisTrip.dateStarted.Minute.ToString() + " " + thisTrip.dateStarted.Month.ToString() + "." + thisTrip.dateStarted.Day.ToString() + "." + thisTrip.dateStarted.Year.ToString();
                        }
                        if (theReader["DateClosed"] != DBNull.Value)
                        {
                            thisTrip.dateClosed = (DateTime)theReader["DateClosed"];

                            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);
                            TimeSpan span = (thisTrip.dateClosed - epoch);
                            double unixTime = span.TotalSeconds;

                            thisTrip.dateClosedEpoch = (int)unixTime;
                        }
                        if (theReader["Latitude"] != DBNull.Value)
                        {
                            thisTrip.latitude = Convert.ToSingle(theReader["Latitude"].ToString());
                        }
                        if (theReader["Longitude"] != DBNull.Value)
                        {
                            thisTrip.longitude = Convert.ToSingle(theReader["Longitude"].ToString());
                        }
                        thisTrip.tripDetails = thisTrip.username + " / " + thisTrip.routeName + " / " + thisTrip.dateStartedString;

                        theResponse.trips.Add(thisTrip);
                    }
                }

                theResponse.statusCode = 0;
                theResponse.statusDescription = "";
            }

            closeDataConnection();

            return theResponse;
        }

        public ResponseTripList GetAllOpenTripsForProvider(string aProviderID)
        {
            ResponseTripList theResponse = new ResponseTripList();

            openDataConnection();

            int providerID = Convert.ToInt32(aProviderID);

            SqlCommand cmdGetTrip = new SqlCommand("GetOpenTripsForProvider", theConnection);
            cmdGetTrip.Parameters.AddWithValue("@providerID", providerID);
            cmdGetTrip.CommandType = System.Data.CommandType.StoredProcedure;

            try
            {
                theReader = cmdGetTrip.ExecuteReader();
            }
            catch (Exception _exception)
            {
                theResponse.statusCode = 6;
                theResponse.statusDescription = _exception.Message;
            }

            if (!theReader.HasRows)
            {
                theResponse.statusCode = 4;
                theResponse.statusDescription = "No Open Trips found";

                theReader.Close();
            }
            else
            {
                theResponse.trips = new List<Trip>();

                while (theReader.Read())
                {
                    int tripID = 0;

                    tripID = (int)theReader["TripID"];

                    if (tripID > 0)
                    {
                        Trip thisTrip = new Trip();

                        thisTrip.id = tripID;
                        thisTrip.routeName = theReader["RouteName"].ToString(); ;
                        thisTrip.username = theReader["Username"].ToString();
                        thisTrip.closed = false;
                        if (theReader["DateStarted"] != DBNull.Value)
                        {
                            thisTrip.dateStarted = (DateTime)theReader["DateStarted"];

                            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);
                            TimeSpan span = (thisTrip.dateStarted - epoch);
                            double unixTime = span.TotalSeconds;

                            thisTrip.dateStartedEpoch = (int)unixTime;

                            thisTrip.dateStartedString = thisTrip.dateStarted.Hour.ToString() + ":" + thisTrip.dateStarted.Minute.ToString() + " " + thisTrip.dateStarted.Month.ToString() + "." + thisTrip.dateStarted.Day.ToString() + "." + thisTrip.dateStarted.Year.ToString();
                        }
                        if (theReader["DateClosed"] != DBNull.Value)
                        {
                            thisTrip.dateClosed = (DateTime)theReader["DateClosed"];

                            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);
                            TimeSpan span = (thisTrip.dateClosed - epoch);
                            double unixTime = span.TotalSeconds;

                            thisTrip.dateClosedEpoch = (int)unixTime;
                        }
                        if (theReader["Latitude"] != DBNull.Value)
                        {
                            thisTrip.latitude = Convert.ToSingle(theReader["Latitude"].ToString());
                        }
                        if (theReader["Longitude"] != DBNull.Value)
                        {
                            thisTrip.longitude = Convert.ToSingle(theReader["Longitude"].ToString());
                        }
                        thisTrip.tripDetails = thisTrip.username + " / " + thisTrip.routeName + " / " + thisTrip.dateStartedString;

                        theResponse.trips.Add(thisTrip);
                    }
                }

                theResponse.statusCode = 0;
                theResponse.statusDescription = "";
            }

            closeDataConnection();

            return theResponse;
        }

        public ResponseTripList GetAllOpenTripsForCDC(string aCDCID)
        {
            ResponseTripList theResponse = new ResponseTripList();

            if (aCDCID == null || aCDCID.Equals(""))
            {
                theResponse.statusCode = 1;
                theResponse.statusDescription = "CDC ID is missing";
            }
            else
            {
                openDataConnection();

                SqlCommand cmdGet = new SqlCommand("GetOpenTripsForCDC", theConnection);
                cmdGet.Parameters.AddWithValue("@cdcID", aCDCID);
                cmdGet.CommandType = System.Data.CommandType.StoredProcedure;

                try
                {
                    theReader = cmdGet.ExecuteReader();

                    if (theReader.HasRows)
                    {
                        theResponse.trips = new List<Trip>();

                        while (theReader.Read())
                        {
                            int tripID = 0;

                            tripID = (int)theReader["TripID"];

                            if (tripID > 0)
                            {
                                Trip thisTrip = new Trip();

                                thisTrip.id = tripID;
                                thisTrip.routeName = theReader["RouteName"].ToString(); ;
                                thisTrip.username = theReader["Username"].ToString();
                                thisTrip.closed = false;
                                if (theReader["DateStarted"] != DBNull.Value)
                                {
                                    thisTrip.dateStarted = (DateTime)theReader["DateStarted"];

                                    DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);
                                    TimeSpan span = (thisTrip.dateStarted - epoch);
                                    double unixTime = span.TotalSeconds;

                                    thisTrip.dateStartedEpoch = (int)unixTime;

                                    thisTrip.dateStartedString = thisTrip.dateStarted.Hour.ToString() + ":" + thisTrip.dateStarted.Minute.ToString() + " " + thisTrip.dateStarted.Month.ToString() + "." + thisTrip.dateStarted.Day.ToString() + "." + thisTrip.dateStarted.Year.ToString();
                                }
                                if (theReader["DateClosed"] != DBNull.Value)
                                {
                                    thisTrip.dateClosed = (DateTime)theReader["DateClosed"];

                                    DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);
                                    TimeSpan span = (thisTrip.dateClosed - epoch);
                                    double unixTime = span.TotalSeconds;

                                    thisTrip.dateClosedEpoch = (int)unixTime;
                                }
                                if (theReader["Latitude"] != DBNull.Value)
                                {
                                    thisTrip.latitude = Convert.ToSingle(theReader["Latitude"].ToString());
                                }
                                if (theReader["Longitude"] != DBNull.Value)
                                {
                                    thisTrip.longitude = Convert.ToSingle(theReader["Longitude"].ToString());
                                }
                                thisTrip.tripDetails = thisTrip.username + " / " + thisTrip.routeName + " / " + thisTrip.dateStartedString;

                                theResponse.trips.Add(thisTrip);
                            }
                        }

                        theResponse.statusCode = 0;
                        theResponse.statusDescription = "";
                    }
                    else
                    {
                        theResponse.statusCode = 2;
                        theResponse.statusDescription = "There are no open trips for CDC ID " + aCDCID;
                    }
                }
                catch (Exception _exception)
                {
                    theResponse.statusCode = 6;
                    theResponse.statusDescription = _exception.Message;
                }

                closeDataConnection();
            }

            return theResponse;
        }

        public Response SetupTrip(string aRouteName, string aUsername)
        {
            Response theResponse = new Response();

            if (aRouteName.Equals("") || aUsername.Equals(""))
            {
                theResponse.statusCode = 1;
            }
            else
            {
                openDataConnection();

                SqlCommand cmdGetTrip = new SqlCommand("SELECT * FROM Trip WHERE RouteName = '" + aRouteName + "' AND Username = '" + aUsername + "' AND Closed = 0", theConnection);

                theReader = cmdGetTrip.ExecuteReader();

                if (theReader.HasRows)
                {
                    theResponse.statusDescription = "There is already an open trip for " + aRouteName + " for driver id " + aUsername;
                    theResponse.statusCode = 6;

                    theReader.Close();
                }
                else
                {
                    theReader.Close();

                    SqlCommand cmdCheckRoute = new SqlCommand("SELECT * FROM Route WHERE RouteName = '" + aRouteName + "'", theConnection);

                    theReader = cmdCheckRoute.ExecuteReader();

                    if (!theReader.HasRows)
                    {
                        theReader.Close();

                        theResponse.statusCode = 4;
                        theResponse.statusDescription = aRouteName + " does not exist";
                    }
                    else
                    {
                        int routeIntegerID = 0;

                        theReader.Read();

                        routeIntegerID = (int)theReader["RouteID"];

                        theReader.Close();

                        if (routeIntegerID > 0)
                        {
                            SqlCommand cmdGetRouteDetail = new SqlCommand("SELECT * FROM RouteStoreMap WHERE RouteID = " + routeIntegerID + " AND State = 1", theConnection);

                            theReader = cmdGetRouteDetail.ExecuteReader();

                            if (!theReader.HasRows)
                            {
                                theReader.Close();

                                theResponse.statusCode = 4;
                                theResponse.statusDescription = aRouteName + " does not have any stores associated with it";
                            }
                            else
                            {
                                List<int> mappingIDs = new List<int>();

                                while (theReader.Read())
                                {
                                    mappingIDs.Add((int)theReader["MappingID"]);
                                }

                                theReader.Close();

                                if (mappingIDs.Count > 0)
                                {
                                    SqlCommand cmdCreateTrip = new SqlCommand("CreateTrip", theConnection);
                                    cmdCreateTrip.Parameters.AddWithValue("@routeName", aRouteName);
                                    cmdCreateTrip.Parameters.AddWithValue("@username", aUsername);
                                    cmdCreateTrip.CommandType = System.Data.CommandType.StoredProcedure;

                                    int newTripID = 0;

                                    try
                                    {
                                        newTripID = Int32.Parse(cmdCreateTrip.ExecuteScalar().ToString());
                                    }
                                    catch (Exception _exception)
                                    {
                                        theResponse.statusCode = 6;
                                        theResponse.statusDescription = _exception.Message;
                                    }

                                    if (newTripID > 0)
                                    {
                                        int stopsAddedToTrip = 0;

                                        for (int i = 0, l = mappingIDs.Count; i < l; i++)
                                        {
                                            SqlCommand cmdAddStop = new SqlCommand("AddStopForTrip", theConnection);
                                            cmdAddStop.Parameters.AddWithValue("@tripID", newTripID);
                                            cmdAddStop.Parameters.AddWithValue("@mappingID", mappingIDs[i]);
                                            cmdAddStop.CommandType = System.Data.CommandType.StoredProcedure;

                                            int numRowsAffected = 0;

                                            try
                                            {
                                                numRowsAffected = cmdAddStop.ExecuteNonQuery();
                                            }
                                            catch (Exception _exception)
                                            {
                                                theResponse.statusCode = 6;
                                                theResponse.statusDescription = _exception.Message;
                                            }

                                            if (numRowsAffected > 0)
                                            {
                                                stopsAddedToTrip += 1;
                                            }
                                        }

                                        if (stopsAddedToTrip > 0)
                                        {
                                            theResponse.statusCode = 0;

                                            if (stopsAddedToTrip == mappingIDs.Count)
                                            {
                                                theResponse.statusDescription = "";
                                            }
                                            else
                                            {
                                                theResponse.statusDescription = "Some stops from the route could not be added to this trip";
                                            }
                                        }
                                    }
                                    else
                                    {
                                        theResponse.statusCode = 6;
                                        theResponse.statusDescription = "A Trip against " + aRouteName + " could not be created";
                                    }
                                }
                            }
                        }
                    }
                }

                closeDataConnection();
            }

            return theResponse;
        }

        public Response SetupTripV7(string aRouteName, string aUsername, string aGmtOffset)
        {
            Response theResponse = new Response();

            if (aRouteName.Equals("") || aUsername.Equals(""))
            {
                theResponse.statusCode = 1;
            }
            else
            {
                openDataConnection();

                SqlCommand cmdGetTrip = new SqlCommand("SELECT * FROM Trip WHERE RouteName = '" + aRouteName + "' AND Username = '" + aUsername + "' AND Closed = 0", theConnection);

                theReader = cmdGetTrip.ExecuteReader();

                if (theReader.HasRows)
                {
                    theResponse.statusDescription = "There is already an open trip for " + aRouteName + " for driver id " + aUsername;
                    theResponse.statusCode = 6;

                    theReader.Close();
                }
                else
                {
                    theReader.Close();

                    SqlCommand cmdCheckRoute = new SqlCommand("SELECT * FROM Route WHERE RouteName = '" + aRouteName + "'", theConnection);

                    theReader = cmdCheckRoute.ExecuteReader();

                    if (!theReader.HasRows)
                    {
                        theReader.Close();

                        theResponse.statusCode = 4;
                        theResponse.statusDescription = aRouteName + " does not exist";
                    }
                    else
                    {
                        int routeIntegerID = 0;

                        theReader.Read();

                        routeIntegerID = (int)theReader["RouteID"];

                        theReader.Close();

                        if (routeIntegerID > 0)
                        {
                            SqlCommand cmdGetRouteDetail = new SqlCommand("SELECT * FROM RouteStoreMap WHERE RouteID = " + routeIntegerID + " AND State = 1", theConnection);

                            theReader = cmdGetRouteDetail.ExecuteReader();

                            if (!theReader.HasRows)
                            {
                                theReader.Close();

                                theResponse.statusCode = 4;
                                theResponse.statusDescription = aRouteName + " does not have any stores associated with it";
                            }
                            else
                            {
                                List<int> mappingIDs = new List<int>();

                                while (theReader.Read())
                                {
                                    mappingIDs.Add((int)theReader["MappingID"]);
                                }

                                theReader.Close();

                                if (mappingIDs.Count > 0)
                                {
                                    SqlCommand cmdCreateTrip = new SqlCommand("CreateTripV7", theConnection);
                                    cmdCreateTrip.Parameters.AddWithValue("@routeName", aRouteName);
                                    cmdCreateTrip.Parameters.AddWithValue("@username", aUsername);
                                    cmdCreateTrip.Parameters.AddWithValue("@GMTOffset", aGmtOffset);
                                    cmdCreateTrip.CommandType = System.Data.CommandType.StoredProcedure;

                                    int newTripID = 0;

                                    try
                                    {
                                        newTripID = Int32.Parse(cmdCreateTrip.ExecuteScalar().ToString());
                                    }
                                    catch (Exception _exception)
                                    {
                                        theResponse.statusCode = 6;
                                        theResponse.statusDescription = _exception.Message;
                                    }

                                    if (newTripID > 0)
                                    {
                                        int stopsAddedToTrip = 0;

                                        for (int i = 0, l = mappingIDs.Count; i < l; i++)
                                        {
                                            SqlCommand cmdAddStop = new SqlCommand("AddStopForTrip", theConnection);
                                            cmdAddStop.Parameters.AddWithValue("@tripID", newTripID);
                                            cmdAddStop.Parameters.AddWithValue("@mappingID", mappingIDs[i]);
                                            cmdAddStop.CommandType = System.Data.CommandType.StoredProcedure;

                                            int numRowsAffected = 0;

                                            try
                                            {
                                                numRowsAffected = cmdAddStop.ExecuteNonQuery();
                                            }
                                            catch (Exception _exception)
                                            {
                                                theResponse.statusCode = 6;
                                                theResponse.statusDescription = _exception.Message;
                                            }

                                            if (numRowsAffected > 0)
                                            {
                                                stopsAddedToTrip += 1;
                                            }
                                        }

                                        if (stopsAddedToTrip > 0)
                                        {
                                            theResponse.statusCode = 0;

                                            if (stopsAddedToTrip == mappingIDs.Count)
                                            {
                                                theResponse.statusDescription = "";
                                            }
                                            else
                                            {
                                                theResponse.statusDescription = "Some stops from the route could not be added to this trip";
                                            }
                                        }
                                    }
                                    else
                                    {
                                        theResponse.statusCode = 6;
                                        theResponse.statusDescription = "A Trip against " + aRouteName + " could not be created";
                                    }
                                }
                            }
                        }
                    }
                }

                closeDataConnection();
            }

            return theResponse;
        }

        public Response CloseTrip(string aTripID)
        {
            Response theResponse = new Response();

            if (aTripID != null && !aTripID.Equals(""))
            {
                openDataConnection();

                SqlCommand cmdCloseTrip = new SqlCommand("CloseTrip", theConnection);
                cmdCloseTrip.Parameters.AddWithValue("@tripID", aTripID);
                cmdCloseTrip.CommandType = System.Data.CommandType.StoredProcedure;

                int numRowsAffected = 0;

                try
                {
                    numRowsAffected = cmdCloseTrip.ExecuteNonQuery();
                }
                catch (Exception _exception)
                {
                    theResponse.statusCode = 6;
                    theResponse.statusDescription = _exception.Message;
                }

                if (numRowsAffected > 0)
                {
                    theResponse.statusCode = 0;
                    theResponse.statusDescription = "";

                    try
                    {
                        SqlCommand cmdGetDetails = new SqlCommand("GetTripDetails", theConnection);
                        cmdGetDetails.Parameters.AddWithValue("@tripID", aTripID);
                        cmdGetDetails.CommandType = System.Data.CommandType.StoredProcedure;

                        theReader = cmdGetDetails.ExecuteReader();

                        if (theReader.HasRows)
                        {
                            while (theReader.Read())
                            {
                                string cdcName = theReader["CDCName"].ToString();
                                string cdcEmail = theReader["CDCEmailAddress"].ToString();
                                string routeName = theReader["RouteName"].ToString();
                                DateTime dateClosed = (DateTime)theReader["DateClosed"];

                                //SendEmailToCDC(cdcName, "sdreadiness@starbucks.com", routeName, dateClosed);
                                SendEmailToCDC(cdcName, cdcEmail, routeName, dateClosed);
                            }
                        }

                        theReader.Close();
                    }
                    catch (Exception _exceptionMail)
                    {
                        theResponse.statusDescription = _exceptionMail.Message + " / " + _exceptionMail.StackTrace;
                    }
                }

                closeDataConnection();
            }
            else
            {
                theResponse.statusCode = 4;
                theResponse.statusDescription = "Missing Trip ID";
            }

            return theResponse;
        }

        public ResponseTrip GetOpenTripForRouteNameAndUser(string aRouteName, string aUsername)
        {
            ResponseTrip theResponse = new ResponseTrip();

            if (aRouteName != null && !aRouteName.Equals("") && aUsername != null && !aUsername.Equals(""))
            {
                openDataConnection();

                SqlCommand cmdGetTrip = new SqlCommand("SELECT * FROM Trip WHERE RouteName = '" + aRouteName + "' AND Username = '" + aUsername + "' AND Closed = 0", theConnection);

                try
                {
                    theReader = cmdGetTrip.ExecuteReader();
                }
                catch (Exception _exception)
                {
                    theResponse.statusCode = 6;
                    theResponse.statusDescription = _exception.Message;
                }

                if (!theReader.HasRows)
                {
                    theResponse.statusCode = 4;
                    theResponse.statusDescription = "No Open Trip for " + aRouteName + " exists for " + aUsername;

                    theReader.Close();

                    Response setupResponse = SetupTrip(aRouteName, aUsername);

                    if (setupResponse.statusCode == 0)
                    {
                        theResponse = null;

                        return GetOpenTripForRouteNameAndUser(aRouteName, aUsername);
                    }
                }
                else
                {
                    int tripID = 0;

                    theReader.Read();

                    tripID = (int)theReader["TripID"];

                    if (tripID > 0)
                    {
                        TripWithStops thisTrip = new TripWithStops();

                        thisTrip.id = tripID;
                        thisTrip.routeName = aRouteName;
                        thisTrip.username = aUsername;
                        thisTrip.closed = false;
                        if (theReader["DateStarted"] != DBNull.Value)
                        {
                            thisTrip.dateStarted = (DateTime)theReader["DateStarted"];

                            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);
                            TimeSpan span = (thisTrip.dateStarted - epoch);
                            double unixTime = span.TotalSeconds;

                            thisTrip.dateStartedEpoch = (int)unixTime;
                        }
                        if (theReader["DateClosed"] != DBNull.Value)
                        {
                            thisTrip.dateClosed = (DateTime)theReader["DateClosed"];

                            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);
                            TimeSpan span = (thisTrip.dateClosed - epoch);
                            double unixTime = span.TotalSeconds;

                            thisTrip.dateClosedEpoch = (int)unixTime;
                        }

                        theReader.Close();

                        int routeID = 0;

                        SqlCommand cmdGetRouteId = new SqlCommand("SELECT RouteID FROM Route WHERE RouteName = '" + aRouteName + "'", theConnection);
                        theReader = cmdGetRouteId.ExecuteReader();

                        if (theReader.HasRows)
                        {
                            theReader.Read();

                            routeID = (int)theReader["RouteID"];

                            theReader.Close();

                            if (routeID > 0)
                            {
                                SqlCommand cmdRouteStoreMappings = new SqlCommand("SELECT * FROM Stop WHERE TripID = " + tripID.ToString(), theConnection);

                                theReader = cmdRouteStoreMappings.ExecuteReader();

                                if (theReader.HasRows)
                                {

                                    List<StopWithStore> stops = new List<StopWithStore>();

                                    while (theReader.Read())
                                    {
                                        StopWithStore thisStop = new StopWithStore();

                                        thisStop.id = (int)theReader["StopID"];
                                        thisStop.committed = true;
                                        thisStop.tripID = (int)theReader["TripID"];
                                        thisStop.mappingID = (int)theReader["MappingID"];
                                        thisStop.completed = (bool)theReader["Completed"];
                                        //          thisStop.comment = theReader["Comment"].ToString();

                                        if (theReader["DateAdded"] != DBNull.Value)
                                        {
                                            thisStop.dateAdded = (DateTime)theReader["DateAdded"];

                                            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);
                                            TimeSpan span = (thisStop.dateAdded - epoch);
                                            double unixTime = span.TotalSeconds;

                                            thisStop.dateAddedEpoch = (int)unixTime;
                                        }
                                        if (theReader["DateUpdated"] != DBNull.Value)
                                        {
                                            thisStop.dateUpdated = (DateTime)theReader["DateUpdated"];

                                            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);
                                            TimeSpan span = (thisStop.dateUpdated - epoch);
                                            double unixTime = span.TotalSeconds;

                                            thisStop.dateUpdatedEpoch = (int)unixTime;
                                        }

                                        stops.Add(thisStop);
                                    }

                                    theReader.Close();

                                    for (int i = 0, l = stops.Count; i < l; i++)
                                    {
                                        StopWithStore thisStop = stops[i];

                                        SqlCommand cmdGetStoreFromMappingID = new SqlCommand("GetStoreFromMappingID", theConnection);
                                        cmdGetStoreFromMappingID.Parameters.AddWithValue("@mappingID", thisStop.mappingID);
                                        cmdGetStoreFromMappingID.CommandType = System.Data.CommandType.StoredProcedure;

                                        theReader = cmdGetStoreFromMappingID.ExecuteReader();

                                        if (theReader.HasRows)
                                        {
                                            while (theReader.Read())
                                            {
                                                Store thisStore = new Store();

                                                thisStore.storeID = (int)theReader["StoreID"];
                                                thisStore.storeName = theReader["StoreName"].ToString();
                                                thisStore.storeAddress = theReader["StoreAddress"].ToString();
                                                thisStore.storeCity = theReader["StoreCity"].ToString();
                                                thisStore.storeZip = theReader["StoreZip"].ToString();
                                                thisStore.storeState = theReader["StoreState"].ToString();
                                                thisStore.storePhone = theReader["StorePhone"].ToString();
                                                thisStore.storeManagerName = theReader["StoreManagerName"].ToString();
                                                thisStore.storeEmailAddress = theReader["StoreEmail"].ToString();
                                                thisStore.storeNumber = theReader["StoreNumber"].ToString();
                                                thisStore.storeOwnershipType = theReader["StoreOwnershipType"].ToString();

                                                thisStop.store = thisStore;
                                            }
                                        }

                                        theReader.Close();
                                    }

                                    for (int i = 0, l = stops.Count; i < l; i++)
                                    {
                                        StopWithStore thisStop = stops[i];

                                        SqlCommand cmdGetFailuresForStop = new SqlCommand("GetFailuresForStop", theConnection);
                                        cmdGetFailuresForStop.Parameters.AddWithValue("@stopID", thisStop.id);
                                        cmdGetFailuresForStop.CommandType = System.Data.CommandType.StoredProcedure;

                                        theReader = cmdGetFailuresForStop.ExecuteReader();

                                        if (theReader.HasRows)
                                        {
                                            thisStop.failure = new List<FailureWithReason>();

                                            while (theReader.Read())
                                            {
                                                FailureWithReason thisFailure = new FailureWithReason();
                                                thisFailure.failureID = (int)theReader["FailureID"];
                                                thisFailure.stopID = (int)theReader["StopID"];
                                                thisFailure.parentReasonCode = (int)theReader["ReasonID"];
                                                thisFailure.childReasonCode = (int)theReader["ChildReasonID"];
                                                thisFailure.emailSent = (bool)theReader["EmailSent"];
                                                if (theReader["Comment"] != System.DBNull.Value)
                                                    thisFailure.comment = (string)theReader["Comment"];
                                                else
                                                    thisFailure.comment = "";

                                                thisStop.failure.Add(thisFailure);
                                            }
                                        }

                                        theReader.Close();
                                    }

                                    for (int i = 0, l = stops.Count; i < l; i++)
                                    {
                                        StopWithStore thisStop = stops[i];
                                        List<FailureWithReason> thisFailure = thisStop.failure;

                                        if (thisFailure != null)
                                        {
                                            for (int j = 0, k = thisFailure.Count; j < k; j++)
                                            {
                                                SqlCommand cmdDetail = new SqlCommand("GetChildReasonDetail", theConnection);
                                                cmdDetail.Parameters.AddWithValue("@childReasonCode", thisFailure[j].childReasonCode.ToString());
                                                cmdDetail.CommandType = System.Data.CommandType.StoredProcedure;

                                                theReader = cmdDetail.ExecuteReader();

                                                if (theReader.HasRows)
                                                {
                                                    while (theReader.Read())
                                                    {
                                                        ReasonChildWithParent theReason = new ReasonChildWithParent();

                                                        theReason.childReasonCode = thisFailure[j].childReasonCode;
                                                        theReason.childReasonExplanation = theReader["ChildReasonExplanation"].ToString();
                                                        theReason.childReasonName = theReader["ChildReasonName"].ToString();
                                                        theReason.escalation = (bool)theReader["Escalation"];
                                                        theReason.photoRequired = (bool)theReader["PhotoRequired"];

                                                        Reason theParentReason = new Reason();
                                                        theParentReason.reasonCode = (int)theReader["ReasonID"];
                                                        theParentReason.reasonName = theReader["ReasonName"].ToString();

                                                        theReason.parentReason = theParentReason;

                                                        thisFailure[j].reason = theReason;
                                                    }
                                                }

                                                theReader.Close();
                                            }
                                        }
                                    }

                                    thisTrip.stops = stops;
                                }
                                else
                                {
                                    theReader.Close();
                                }
                            }
                        }

                        if (thisTrip.stops == null)
                        {
                            try
                            {
                                SqlCommand cmdResetTrip = new SqlCommand("DELETE FROM Trip WHERE TripID = " + thisTrip.id, theConnection);
                                int numRowsAffected = cmdResetTrip.ExecuteNonQuery();

                                if (numRowsAffected > 0)
                                {
                                    theResponse = null;

                                    return GetOpenTripForRouteNameAndUser(aRouteName, aUsername);
                                }
                                else
                                {
                                    theResponse.statusCode = 6;
                                    theResponse.statusDescription = "Invalid Trip Data. Please contact the service center.";
                                }
                            }
                            catch (Exception _exception)
                            {
                                theResponse.statusCode = 6;
                                theResponse.statusDescription = _exception.Message + " / " + _exception.StackTrace;
                            }
                        }
                        else
                        {
                            theResponse.trip = thisTrip;

                            theResponse.statusCode = 0;
                            theResponse.statusDescription = "";
                        }
                    }
                }

                closeDataConnection();
            }
            else
            {
                theResponse.statusCode = 4;
                theResponse.statusDescription = "Route Name or Username not provided";
            }

            return theResponse;
        }

        public ResponseTrip GetOpenTripForRouteNameAndUserV7(string aRouteName, string aUsername, string aGmtOffset)
        {
            ResponseTrip theResponse = new ResponseTrip();

            TimeSpan tSpan = TimeSpan.FromMinutes(Convert.ToDouble(aGmtOffset));
            aGmtOffset = tSpan.TotalHours.ToString();

            if (aRouteName != null && !aRouteName.Equals("") && aUsername != null && !aUsername.Equals(""))
            {
                openDataConnection();

                SqlCommand cmdGetTrip = new SqlCommand("SELECT * FROM Trip WHERE RouteName = '" + aRouteName + "' AND Username = '" + aUsername + "' AND Closed = 0", theConnection);

                try
                {
                    theReader = cmdGetTrip.ExecuteReader();
                }
                catch (Exception _exception)
                {
                    theResponse.statusCode = 6;
                    theResponse.statusDescription = _exception.Message;
                }

                if (!theReader.HasRows)
                {
                    theResponse.statusCode = 4;
                    theResponse.statusDescription = "No Open Trip for " + aRouteName + " exists for " + aUsername;

                    theReader.Close();

                    Response setupResponse = SetupTripV7(aRouteName, aUsername, aGmtOffset);

                    if (setupResponse.statusCode == 0)
                    {
                        theResponse = null;

                        return GetOpenTripForRouteNameAndUserV7(aRouteName, aUsername, aGmtOffset);
                    }
                }
                else
                {
                    int tripID = 0;

                    theReader.Read();

                    tripID = (int)theReader["TripID"];

                    if (tripID > 0)
                    {
                        TripWithStops thisTrip = new TripWithStops();

                        thisTrip.id = tripID;
                        thisTrip.routeName = aRouteName;
                        thisTrip.username = aUsername;
                        thisTrip.closed = false;
                        if (theReader["GMTOffset"] != DBNull.Value)
                        {
                            thisTrip.GMTOffset = Convert.ToSingle(theReader["GMTOffset"]);
                        }
                        if (theReader["DateStarted"] != DBNull.Value)
                        {
                            thisTrip.dateStarted = (DateTime)theReader["DateStarted"];

                            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);
                            TimeSpan span = (thisTrip.dateStarted - epoch);
                            double unixTime = span.TotalSeconds;

                            thisTrip.dateStartedEpoch = (int)unixTime;
                        }
                        if (theReader["DateClosed"] != DBNull.Value)
                        {
                            thisTrip.dateClosed = (DateTime)theReader["DateClosed"];

                            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);
                            TimeSpan span = (thisTrip.dateClosed - epoch);
                            double unixTime = span.TotalSeconds;

                            thisTrip.dateClosedEpoch = (int)unixTime;
                        }

                        theReader.Close();

                        int routeID = 0;

                        SqlCommand cmdGetRouteId = new SqlCommand("SELECT RouteID FROM Route WHERE RouteName = '" + aRouteName + "'", theConnection);
                        theReader = cmdGetRouteId.ExecuteReader();

                        if (theReader.HasRows)
                        {
                            theReader.Read();

                            routeID = (int)theReader["RouteID"];

                            theReader.Close();

                            if (routeID > 0)
                            {
                                SqlCommand cmdRouteStoreMappings = new SqlCommand("SELECT * FROM Stop WHERE TripID = " + tripID.ToString(), theConnection);

                                theReader = cmdRouteStoreMappings.ExecuteReader();

                                if (theReader.HasRows)
                                {

                                    List<StopWithStore> stops = new List<StopWithStore>();

                                    while (theReader.Read())
                                    {
                                        StopWithStore thisStop = new StopWithStore();

                                        thisStop.id = (int)theReader["StopID"];
                                        thisStop.committed = true;
                                        thisStop.tripID = (int)theReader["TripID"];
                                        thisStop.mappingID = (int)theReader["MappingID"];
                                        thisStop.completed = (bool)theReader["Completed"];
                                        //          thisStop.comment = theReader["Comment"].ToString();

                                        if (theReader["DateAdded"] != DBNull.Value)
                                        {
                                            thisStop.dateAdded = (DateTime)theReader["DateAdded"];

                                            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);
                                            TimeSpan span = (thisStop.dateAdded - epoch);
                                            double unixTime = span.TotalSeconds;

                                            thisStop.dateAddedEpoch = (int)unixTime;
                                        }
                                        if (theReader["DateUpdated"] != DBNull.Value)
                                        {
                                            thisStop.dateUpdated = (DateTime)theReader["DateUpdated"];

                                            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);
                                            TimeSpan span = (thisStop.dateUpdated - epoch);
                                            double unixTime = span.TotalSeconds;

                                            thisStop.dateUpdatedEpoch = (int)unixTime;
                                        }

                                        stops.Add(thisStop);
                                    }

                                    theReader.Close();

                                    for (int i = 0, l = stops.Count; i < l; i++)
                                    {
                                        StopWithStore thisStop = stops[i];

                                        SqlCommand cmdGetStoreFromMappingID = new SqlCommand("GetStoreFromMappingID", theConnection);
                                        cmdGetStoreFromMappingID.Parameters.AddWithValue("@mappingID", thisStop.mappingID);
                                        cmdGetStoreFromMappingID.CommandType = System.Data.CommandType.StoredProcedure;

                                        theReader = cmdGetStoreFromMappingID.ExecuteReader();

                                        if (theReader.HasRows)
                                        {
                                            while (theReader.Read())
                                            {
                                                Store thisStore = new Store();

                                                thisStore.storeID = (int)theReader["StoreID"];
                                                thisStore.storeName = theReader["StoreName"].ToString();
                                                thisStore.storeAddress = theReader["StoreAddress"].ToString();
                                                thisStore.storeCity = theReader["StoreCity"].ToString();
                                                thisStore.storeZip = theReader["StoreZip"].ToString();
                                                thisStore.storeState = theReader["StoreState"].ToString();
                                                thisStore.storePhone = theReader["StorePhone"].ToString();
                                                thisStore.storeManagerName = theReader["StoreManagerName"].ToString();
                                                thisStore.storeEmailAddress = theReader["StoreEmail"].ToString();
                                                thisStore.storeNumber = theReader["StoreNumber"].ToString();
                                                thisStore.storeOwnershipType = theReader["StoreOwnershipType"].ToString();
                                                if (theReader["PODRequired"] == DBNull.Value)
                                                {
                                                    thisStore.PODRequired = false;
                                                }
                                                else
                                                {
                                                    thisStore.PODRequired = (bool)theReader["PODRequired"];
                                                }

                                                thisStop.store = thisStore;
                                            }
                                        }

                                        theReader.Close();
                                    }

                                    for (int i = 0, l = stops.Count; i < l; i++)
                                    {
                                        StopWithStore thisStop = stops[i];

                                        SqlCommand cmdGetFailuresForStop = new SqlCommand("GetFailuresForStop", theConnection);
                                        cmdGetFailuresForStop.Parameters.AddWithValue("@stopID", thisStop.id);
                                        cmdGetFailuresForStop.CommandType = System.Data.CommandType.StoredProcedure;

                                        theReader = cmdGetFailuresForStop.ExecuteReader();

                                        if (theReader.HasRows)
                                        {
                                            thisStop.failure = new List<FailureWithReason>();

                                            while (theReader.Read())
                                            {
                                                FailureWithReason thisFailure = new FailureWithReason();
                                                thisFailure.failureID = (int)theReader["FailureID"];
                                                thisFailure.stopID = (int)theReader["StopID"];
                                                thisFailure.parentReasonCode = (int)theReader["ReasonID"];
                                                thisFailure.childReasonCode = (int)theReader["ChildReasonID"];
                                                thisFailure.emailSent = (bool)theReader["EmailSent"];
                                                if (theReader["Comment"] != System.DBNull.Value)
                                                    thisFailure.comment = (string)theReader["Comment"];
                                                else
                                                    thisFailure.comment = "";

                                                thisStop.failure.Add(thisFailure);
                                            }
                                        }

                                        theReader.Close();
                                    }

                                    for (int i = 0, l = stops.Count; i < l; i++)
                                    {
                                        StopWithStore thisStop = stops[i];
                                        List<FailureWithReason> thisFailure = thisStop.failure;

                                        if (thisFailure != null)
                                        {
                                            for (int j = 0, k = thisFailure.Count; j < k; j++)
                                            {
                                                SqlCommand cmdDetail = new SqlCommand("GetChildReasonDetail", theConnection);
                                                cmdDetail.Parameters.AddWithValue("@childReasonCode", thisFailure[j].childReasonCode.ToString());
                                                cmdDetail.CommandType = System.Data.CommandType.StoredProcedure;

                                                theReader = cmdDetail.ExecuteReader();

                                                if (theReader.HasRows)
                                                {
                                                    while (theReader.Read())
                                                    {
                                                        ReasonChildWithParent theReason = new ReasonChildWithParent();

                                                        theReason.childReasonCode = thisFailure[j].childReasonCode;
                                                        theReason.childReasonExplanation = theReader["ChildReasonExplanation"].ToString();
                                                        theReason.childReasonName = theReader["ChildReasonName"].ToString();
                                                        theReason.escalation = (bool)theReader["Escalation"];
                                                        theReason.photoRequired = (bool)theReader["PhotoRequired"];

                                                        Reason theParentReason = new Reason();
                                                        theParentReason.reasonCode = (int)theReader["ReasonID"];
                                                        theParentReason.reasonName = theReader["ReasonName"].ToString();

                                                        theReason.parentReason = theParentReason;

                                                        thisFailure[j].reason = theReason;
                                                    }
                                                }

                                                theReader.Close();
                                            }
                                        }
                                    }

                                    thisTrip.stops = stops;
                                }
                                else
                                {
                                    theReader.Close();
                                }
                            }
                        }

                        if (thisTrip.stops == null)
                        {
                            try
                            {
                                SqlCommand cmdResetTrip = new SqlCommand("DELETE FROM Trip WHERE TripID = " + thisTrip.id, theConnection);
                                int numRowsAffected = cmdResetTrip.ExecuteNonQuery();

                                if (numRowsAffected > 0)
                                {
                                    theResponse = null;

                                    return GetOpenTripForRouteNameAndUserV7(aRouteName, aUsername, aGmtOffset);
                                }
                                else
                                {
                                    theResponse.statusCode = 6;
                                    theResponse.statusDescription = "Invalid Trip Data. Please contact the service center.";
                                }
                            }
                            catch (Exception _exception)
                            {
                                theResponse.statusCode = 6;
                                theResponse.statusDescription = _exception.Message + " / " + _exception.StackTrace;
                            }
                        }
                        else
                        {
                            theResponse.trip = thisTrip;

                            theResponse.statusCode = 0;
                            theResponse.statusDescription = "";
                        }
                    }
                }

                closeDataConnection();
            }
            else
            {
                theResponse.statusCode = 4;
                theResponse.statusDescription = "Route Name or Username not provided";
            }

            return theResponse;
        }

        public Response AddCommentToStop(Comment aComment)
        {
            Response theResponse = new Response();

            if (aComment != null)
            {
                if (aComment.comment == null || aComment.comment.Equals(""))
                {
                    theResponse.statusDescription = "Comment is missing from model";
                }
                if (aComment.stopID == null || aComment.stopID <= 0)
                {
                    theResponse.statusDescription = "StopID is missing from model";
                }

                if (theResponse.statusDescription.Equals(""))
                {
                    string filteredComment = getFilteredText(aComment.comment);

                    openDataConnection();

                    SqlCommand cmdAddComment = new SqlCommand("AddCommentForStop", theConnection);
                    cmdAddComment.Parameters.AddWithValue("@stopID", aComment.stopID);
                    cmdAddComment.Parameters.AddWithValue("@comment", filteredComment);
                    cmdAddComment.CommandType = System.Data.CommandType.StoredProcedure;

                    int numRowsAffected = cmdAddComment.ExecuteNonQuery();

                    if (numRowsAffected > 0)
                    {
                        theResponse.statusCode = 0;
                        theResponse.statusDescription = "";
                    }
                    else
                    {
                        theResponse.statusCode = 6;
                        theResponse.statusDescription = "Could not add comment";
                    }

                    closeDataConnection();
                }
                else
                {
                    theResponse.statusCode = 6;
                }
            }
            else
            {
                theResponse.statusCode = 3;
                theResponse.statusDescription = "Comment Model missing";
            }

            return theResponse;
        }

        public ResponsePhoto GetPhotosForStop(string aStopID)
        {
            ResponsePhoto theResponse = new ResponsePhoto();

            if (aStopID == null || aStopID.Equals(""))
            {
                theResponse.statusCode = 3;
                theResponse.statusDescription = "StopID missing";
            }
            else
            {
                openDataConnection();

                SqlCommand cmdGetPhotos = new SqlCommand("GetImagesForStop", theConnection);
                cmdGetPhotos.Parameters.AddWithValue("@stopID", aStopID);
                cmdGetPhotos.CommandType = System.Data.CommandType.StoredProcedure;

                theReader = cmdGetPhotos.ExecuteReader();

                if (!theReader.HasRows)
                {
                    theResponse.statusCode = 4;
                    theResponse.statusDescription = "There are no photos uploaded for this stop, yet";
                }
                else
                {
                    List<Photo> photos = new List<Photo>();

                    while (theReader.Read())
                    {
                        Photo thisPhoto = new Photo();

                        thisPhoto.stopID = Int32.Parse(aStopID);
                        thisPhoto.imageData = theReader["Photo"].ToString();
                        thisPhoto.photoID = (int)theReader["PhotoID"];

                        if (theReader["FailureID"] != DBNull.Value)
                            thisPhoto.failureID = (int)theReader["FailureID"];
                        else
                            thisPhoto.failureID = 0;

                        photos.Add(thisPhoto);
                    }

                    theResponse.photos = photos;
                    theResponse.statusCode = 0;
                    theResponse.statusDescription = "";
                }

                closeDataConnection();
            }

            return theResponse;
        }

        public Response AddPhotoToStop(Photo aPhoto)
        {
            Response theResponse = new Response();

            if (aPhoto != null)
            {
                if (aPhoto.stopID <= 0)
                {
                    theResponse.statusDescription = "Stop ID is missing";
                }
                if (aPhoto.imageData == null || aPhoto.imageData.Equals(""))
                {
                    theResponse.statusDescription = "Image Data is missing";
                }

                if (theResponse.statusDescription.Equals(""))
                {
                    openDataConnection();

                    SqlCommand cmdAddPhoto = new SqlCommand("AddPhotoForStop", theConnection);
                    cmdAddPhoto.Parameters.AddWithValue("@stopID", aPhoto.stopID);
                    cmdAddPhoto.Parameters.AddWithValue("@failureID", aPhoto.failureID);
                    cmdAddPhoto.Parameters.AddWithValue("@photoData", aPhoto.imageData);
                    cmdAddPhoto.CommandType = System.Data.CommandType.StoredProcedure;

                    int numRowsAffected = cmdAddPhoto.ExecuteNonQuery();

                    if (numRowsAffected > 0)
                    {
                        theResponse.statusCode = 0;
                        theResponse.statusDescription = "";
                    }
                    else
                    {
                        theResponse.statusDescription = "Could not add photo to StopID " + aPhoto.stopID;
                    }

                    closeDataConnection();
                }
                else
                {
                    theResponse.statusCode = 6;
                }
            }
            else
            {
                theResponse.statusCode = 4;
                theResponse.statusDescription = "Photo Model missing";
            }

            return theResponse;
        }

        public Response AddPhotoToStopTransaction(Photo aPhoto)
        {
            Response theResponse = new Response();

            if (aPhoto != null)
            {
                if (aPhoto.stopID <= 0)
                {
                    theResponse.statusDescription = "Stop ID is missing";
                }
                if (aPhoto.imageData == null || aPhoto.imageData.Equals(""))
                {
                    theResponse.statusDescription = "Image Data is missing";
                }

                if (theResponse.statusDescription.Equals(""))
                {
                    //openDataConnection();

                    SqlCommand cmdAddPhoto = new SqlCommand("AddPhotoForStop", theConnection);
                    cmdAddPhoto.Transaction = theTrans;
                    cmdAddPhoto.Parameters.AddWithValue("@stopID", aPhoto.stopID);
                    cmdAddPhoto.Parameters.AddWithValue("@failureID", aPhoto.failureID);
                    //cmdAddPhoto.Parameters.AddWithValue("@failureID", null);
                    cmdAddPhoto.Parameters.AddWithValue("@photoData", aPhoto.imageData);
                    cmdAddPhoto.CommandType = System.Data.CommandType.StoredProcedure;

                    int numRowsAffected = cmdAddPhoto.ExecuteNonQuery();

                    if (numRowsAffected > 0)
                    {
                        theResponse.statusCode = 0;
                        theResponse.statusDescription = "";
                    }
                    else
                    {
                        theResponse.statusDescription = "Could not add photo to StopID " + aPhoto.stopID;
                    }

                    //closeDataConnection();
                }
                else
                {
                    theResponse.statusCode = 6;
                }
            }
            else
            {
                theResponse.statusCode = 4;
                theResponse.statusDescription = "Photo Model missing";
            }

            return theResponse;
        }

        public Response AddDeliveryTransaction(Delivery aDelivery)
        {
            Response theResponse = new Response();

            if (aDelivery != null)
            {
                if (aDelivery.stopID <= 0)
                {
                    theResponse.statusDescription = "Stop ID is missing";
                }
                if (aDelivery.deliveryCode <= 0)
                {
                    theResponse.statusDescription = "Delivery Code is  missing";
                }

                if (theResponse.statusDescription.Equals(""))
                {
                    SqlCommand cmdAddDelivery = new SqlCommand("AddDelivery", theConnection);
                    cmdAddDelivery.Transaction = theTrans;
                    cmdAddDelivery.Parameters.AddWithValue("@stopID", aDelivery.stopID);
                    cmdAddDelivery.Parameters.AddWithValue("@failureID", aDelivery.failureID);
                    cmdAddDelivery.Parameters.AddWithValue("@deliveryCode", aDelivery.deliveryCode);
                    //    cmdAddDelivery.Parameters.AddWithValue("@dateAdded", Convert.ToDateTime(aDelivery.dateAdded));
                    cmdAddDelivery.CommandType = System.Data.CommandType.StoredProcedure;

                    int numRowsAffected = cmdAddDelivery.ExecuteNonQuery();

                    if (numRowsAffected > 0)
                    {
                        theResponse.statusCode = 0;
                        theResponse.statusDescription = "";
                    }
                    else
                    {
                        theResponse.statusDescription = "Could not add Delivery to StopID " + aDelivery.stopID;
                    }

                }
                else
                {
                    theResponse.statusCode = 6;
                }
            }
            else
            {
                theResponse.statusCode = 4;
                theResponse.statusDescription = "Delivery Model missing";
            }

            return theResponse;
        }

        public ResponseFailure AddFailure(Failure aFailure)
        {
            ResponseFailure theResponse = new ResponseFailure();

            if (aFailure == null)
            {
                theResponse.statusCode = 2;
                theResponse.statusDescription = "Expected Failure model missing";
            }
            else
            {
                if (aFailure.stopID == null || aFailure.stopID == 0)
                {
                    theResponse.statusDescription = "Stop ID is missing";
                }
                if (aFailure.parentReasonCode == null || aFailure.parentReasonCode == 0)
                {
                    theResponse.statusDescription = "Parent Reason ID is missing";
                }
                if (aFailure.childReasonCode == null || aFailure.childReasonCode == 0)
                {
                    theResponse.statusDescription = "Child Reason ID is missing";
                }

                if (!theResponse.statusDescription.Equals(""))
                {
                    theResponse.statusCode = 2;
                    theResponse.statusDescription = "Failure model missing";
                }
                else
                {
                    openDataConnection();

                    SqlCommand cmdAddFailure = new SqlCommand("AddFailure", theConnection);
                    cmdAddFailure.Parameters.AddWithValue("@stopID", aFailure.stopID.ToString());
                    cmdAddFailure.Parameters.AddWithValue("@reasonID", aFailure.parentReasonCode.ToString());
                    cmdAddFailure.Parameters.AddWithValue("@childReasonID", aFailure.childReasonCode.ToString());
                    cmdAddFailure.Parameters.AddWithValue("@valueEntered", aFailure.valueEntered);
                    cmdAddFailure.Parameters.AddWithValue("@comment", aFailure.comment);
                    cmdAddFailure.CommandType = System.Data.CommandType.StoredProcedure;

                    int newFailureID = 0;
                    try
                    {
                        newFailureID = Int32.Parse(cmdAddFailure.ExecuteScalar().ToString());
                    }
                    catch (Exception _exception)
                    {
                        theResponse.statusCode = 6;
                        theResponse.statusDescription = _exception.Message;
                    }

                    if (newFailureID > 0)
                    {
                        theResponse.statusCode = 0;
                        theResponse.statusDescription = "";
                        theResponse.failure = new Failure();
                        theResponse.failure.failureID = newFailureID;
                        theResponse.failure.stopID = aFailure.stopID;
                        theResponse.failure.parentReasonCode = aFailure.parentReasonCode;
                        theResponse.failure.childReasonCode = aFailure.childReasonCode;
                        theResponse.failure.valueEntered = aFailure.valueEntered;
                        theResponse.failure.comment = aFailure.comment;
                        theResponse.failure.emailSent = false;
                    }

                    closeDataConnection();
                }
            }

            return theResponse;
        }

        public ResponseFailure AddFailureTransaction(Failure aFailure)
        {
            ResponseFailure theResponse = new ResponseFailure();

            if (aFailure == null)
            {
                theResponse.statusCode = 2;
                theResponse.statusDescription = "Expected Failure model missing";
            }
            else
            {
                if (aFailure.uniqueID == null || aFailure.uniqueID == "")
                {
                    theResponse.statusDescription = "UniqueID is missing";
                }
                if (aFailure.stopID == null || aFailure.stopID == 0)
                {
                    theResponse.statusDescription = "Stop ID is missing";
                }
                if (aFailure.parentReasonCode == null || aFailure.parentReasonCode == 0)
                {
                    theResponse.statusDescription = "Parent Reason ID is missing";
                }
                if (aFailure.childReasonCode == null || aFailure.childReasonCode == 0)
                {
                    theResponse.statusDescription = "Child Reason ID is missing";
                }

                if (!theResponse.statusDescription.Equals(""))
                {
                    theResponse.statusCode = 2;
                    theResponse.statusDescription = "Failure model missing";
                }
                else
                {
                    //openDataConnection();

                    SqlCommand cmdAddFailure = new SqlCommand("AddFailureTransaction", theConnection);
                    cmdAddFailure.Transaction = theTrans;
                    cmdAddFailure.Parameters.AddWithValue("@stopID", aFailure.stopID.ToString());
                    cmdAddFailure.Parameters.AddWithValue("@reasonID", aFailure.parentReasonCode.ToString());
                    cmdAddFailure.Parameters.AddWithValue("@childReasonID", aFailure.childReasonCode.ToString());
                    cmdAddFailure.Parameters.AddWithValue("@valueEntered", aFailure.valueEntered);
                    cmdAddFailure.Parameters.AddWithValue("@comment", aFailure.comment);
                    cmdAddFailure.Parameters.AddWithValue("@uniqueID", aFailure.uniqueID.ToString());
                    cmdAddFailure.CommandType = System.Data.CommandType.StoredProcedure;

                    int newFailureID = 0;
                    //     try
                    //     {
                    newFailureID = Int32.Parse(cmdAddFailure.ExecuteScalar().ToString());
                    //     }
                    //     catch (Exception _exception)
                    //     {
                    //         theResponse.statusCode = 6;
                    //         theResponse.statusDescription = _exception.Message;
                    //     }

                    if (newFailureID > 0)
                    {
                        theResponse.statusCode = 0;
                        theResponse.statusDescription = "";
                        theResponse.failure = new Failure();
                        theResponse.failure.failureID = newFailureID;
                        theResponse.failure.stopID = aFailure.stopID;
                        theResponse.failure.parentReasonCode = aFailure.parentReasonCode;
                        theResponse.failure.childReasonCode = aFailure.childReasonCode;
                        theResponse.failure.valueEntered = aFailure.valueEntered;
                        theResponse.failure.comment = aFailure.comment;
                        theResponse.failure.emailSent = false;
                    }

                    //closeDataConnection();
                }
            }

            return theResponse;
        }

        public Response CompleteStop(string aStopID)
        {
            Response theResponse = new Response();

            if (aStopID != null && !aStopID.Equals(""))
            {
                openDataConnection();

                SqlCommand cmdComplete = new SqlCommand("CompleteStop", theConnection);
                cmdComplete.Parameters.AddWithValue("@stopID", aStopID);
                cmdComplete.CommandType = System.Data.CommandType.StoredProcedure;

                int numRowsAffected = 0;

                try
                {
                    numRowsAffected = cmdComplete.ExecuteNonQuery();
                }
                catch (Exception _exception)
                {
                    theResponse.statusCode = 6;
                    theResponse.statusDescription = _exception.Message;
                }

                if (numRowsAffected > 0)
                {
                    theResponse.statusCode = 0;
                    theResponse.statusDescription = "Stop Completed";
                }

                closeDataConnection();
            }
            else
            {
                theResponse.statusCode = 2;
                theResponse.statusDescription = "Stop ID is missing";
            }

            return theResponse;
        }

        public Response CompleteStopV7(string aStopID, string aStopCompletedDate)
        {
            Response theResponse = new Response();

            aStopCompletedDate = aStopCompletedDate.Replace('S', ' ').Replace('C', ':').Replace('D', '.');

            if (aStopID != null && !aStopID.Equals(""))
            {
                if (aStopCompletedDate != null && !aStopCompletedDate.Equals(""))
                {
                    openDataConnection();
                    theTrans = theConnection.BeginTransaction();

                    Response addStopCompletedDateResponse = AddStopCompletedDate(aStopID, aStopCompletedDate);

                    Response addCompleteStopTransactionResponse = CompleteStopTransaction(aStopID);

                    if (addStopCompletedDateResponse.statusCode == 0 || addCompleteStopTransactionResponse.statusCode == 0)
                    {
                        theTrans.Commit();
                        theResponse.statusCode = 0;
                        theResponse.statusDescription = "Stop Completed";
                    }
                    else
                    {
                        theTrans.Rollback();
                        theResponse.statusCode = 6;
                        theResponse.statusDescription = "Unable to update data";
                    }
                    closeDataConnection();
                }
                else
                {
                    theResponse.statusCode = 2;
                    theResponse.statusDescription = "Stop ID is missing";
                }
            }
            else
            {
                theResponse.statusCode = 2;
                theResponse.statusDescription = "Stop Completed Date is missing";
            }

            return theResponse;
        }


        public Response CompleteStopTransaction(string aStopID)
        {
            Response theResponse = new Response();

            if (aStopID != null && !aStopID.Equals(""))
            {
                //openDataConnection();

                SqlCommand cmdComplete = new SqlCommand("CompleteStop", theConnection);
                cmdComplete.Transaction = theTrans;
                cmdComplete.Parameters.AddWithValue("@stopID", aStopID);
                cmdComplete.CommandType = System.Data.CommandType.StoredProcedure;

                int numRowsAffected = 0;

                //    try
                //    {
                numRowsAffected = cmdComplete.ExecuteNonQuery();
                //    }
                //    catch (Exception _exception)
                //    {
                //        theResponse.statusCode = 6;
                //        theResponse.statusDescription = _exception.Message;
                //    }


                //    string text = "";
                //    text += " No of Rows Affected(Transaction): " + numRowsAffected + "\n";
                //    text += " StatusCode: " + theResponse.statusCode + "\n";
                //     WriteToFile(text);


                if (numRowsAffected > 0)
                {
                    theResponse.statusCode = 0;
                    theResponse.statusDescription = "";
                }

                //closeDataConnection();
            }
            else
            {
                theResponse.statusCode = 2;
                theResponse.statusDescription = "Stop ID is missing";
            }

            //	WriteToFile(aStopID + "\n");
            //    	WriteToFile(theResponse.statusCode.ToString()+ "\n");

            return theResponse;
        }

        public Response AddStopCompletedDate(string aStopID, string aStopCompletedDate)
        {
            Response theResponse = new Response();

            if (aStopID != null && !aStopID.Equals(""))
            {
                SqlCommand cmdComplete = new SqlCommand("AddStopCompletedDate", theConnection);
                cmdComplete.Transaction = theTrans;
                cmdComplete.Parameters.AddWithValue("@stopID", aStopID);
                cmdComplete.Parameters.AddWithValue("@stopCompletedDate", aStopCompletedDate);
                cmdComplete.CommandType = System.Data.CommandType.StoredProcedure;

                int numRowsAffected = 0;

                numRowsAffected = cmdComplete.ExecuteNonQuery();

                if (numRowsAffected > 0)
                {
                    theResponse.statusCode = 0;
                    theResponse.statusDescription = "";
                }

            }
            else
            {
                theResponse.statusCode = 2;
                theResponse.statusDescription = "Stop ID is missing";
            }


            return theResponse;
        }

        public Response DeleteAllIssues(string aStopID)
        {
            Response theResponse = new Response();

            if (aStopID == null || aStopID.Equals(""))
            {
                theResponse.statusCode = 2;
                theResponse.statusDescription = "Stop ID is missing";
            }
            else
            {
                openDataConnection();

                SqlCommand cmdDelete = new SqlCommand("DeleteAllIssuesForStop", theConnection);
                cmdDelete.Parameters.AddWithValue("@stopID", aStopID);
                cmdDelete.CommandType = System.Data.CommandType.StoredProcedure;

                int numRowsAffected = 0;

                try
                {
                    numRowsAffected = cmdDelete.ExecuteNonQuery();

                    if (numRowsAffected >= 0)
                    {
                        theResponse.statusCode = 0;
                        theResponse.statusDescription = "";
                    }
                }
                catch (Exception _exception)
                {
                    theResponse.statusDescription = _exception.Message;
                    theResponse.statusCode = 6;
                }

                closeDataConnection();
            }

            return theResponse;
        }

        public Response SetGeoPosition(GeoPosition aPosition)
        {
            //GeoPosition aPosition = new GeoPosition();
            //aPosition.tripID = int.Parse(id);
            //aPosition.longitude = float.Parse(longi);
            //aPosition.latitude = float.Parse(lat);
            Response theResponse = new Response();

            if (aPosition != null && aPosition.tripID > 0 && aPosition.latitude != null && aPosition.longitude != null)
            {
                openDataConnection();

                string currentPath = HttpContext.Current.Server.MapPath(".");

                string currentTime = DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString() + "-" + DateTime.Now.Year.ToString();

                string targetPath = currentPath + "\\routeDetails\\";

                SqlCommand cmdSetGeo = new SqlCommand("UpdateGeoPositionForTrip", theConnection);
                cmdSetGeo.Parameters.AddWithValue("@tripID", aPosition.tripID);
                cmdSetGeo.Parameters.AddWithValue("@lat", aPosition.latitude);
                cmdSetGeo.Parameters.AddWithValue("@long", aPosition.longitude);
                cmdSetGeo.CommandType = System.Data.CommandType.StoredProcedure;

                int numRowsAffected = 0;

                try
                {
                    numRowsAffected = cmdSetGeo.ExecuteNonQuery();
                    string newFilePath = "";
                    SqlCommand cmdGet = new SqlCommand(" select routeId, route.RouteName from route join trip on trip.RouteName =  Route.RouteName where tripid = @tripId", theConnection);
                    cmdGet.Parameters.AddWithValue("@tripId", aPosition.tripID);
                    cmdGet.CommandType = System.Data.CommandType.Text;

                    theReader = cmdGet.ExecuteReader();
                    int routeId = 0;
                    string routeName = string.Empty;

                    if (theReader.HasRows)
                    {
                        while (theReader.Read())
                        {
                            routeId = int.Parse(theReader[0].ToString());
                            routeName = theReader[1].ToString();
                        }
                    }
                    string targetFilename = "report_" + routeId + "_" + routeName + "_" + currentTime + ".csv";
                    newFilePath = targetPath + targetFilename;

                    string values = "" + aPosition.tripID + "," + routeId + "," + routeName + "," + aPosition.longitude + "," + aPosition.latitude + "," + DateTime.Now + Environment.NewLine;

                    if (!File.Exists(newFilePath))
                    {
                        string clientHeader = "Trip Id," + "Route Id" + "," + "Route Name" + "," + "Longitude" + "," + "Latitude" + "," + "Date and Time" + Environment.NewLine;

                        File.WriteAllText(newFilePath, clientHeader);
                    }

                    File.AppendAllText(newFilePath, values);

                    if (numRowsAffected > 0)
                    {
                        theResponse.statusCode = 0;
                        theResponse.statusDescription = "";

                    }
                }
                catch (Exception _exception)
                {
                    //string text = "Exception :" + _exception.Message + "\n";
                    //WriteToFile(text);

                    theResponse.statusCode = 6;
                    theResponse.statusDescription = _exception.Message;
                }

                closeDataConnection();
            }
            else
            {
                theResponse.statusCode = 2;
                theResponse.statusDescription = "Missing parameters";
            }

            return theResponse;
        }

        public Response AddOp(Op anOp)
        {
            Response theResponse = new Response();

            if (anOp == null)
            {
                theResponse.statusCode = 2;
                theResponse.statusDescription = "Expected Op Model is missing";
            }
            else
            {
                if (anOp.storeID == null || anOp.storeID == 0)
                {
                    theResponse.statusDescription = "Store ID not provided";
                }
                if (anOp.area == null || anOp.area.Equals(""))
                {
                    theResponse.statusDescription = "Area not provided";
                }
                if (anOp.division == null || anOp.division.Equals(""))
                {
                    theResponse.statusDescription = "Division not provided";
                }
                if (anOp.region == null || anOp.region.Equals(""))
                {
                    theResponse.statusDescription = "Region not provided";
                }
                if (anOp.district == null || anOp.district.Equals(""))
                {
                    theResponse.statusDescription = "District not provided";
                }

                if (theResponse.statusDescription.Equals(""))
                {
                    openDataConnection();

                    SqlCommand cmdAddOp = new SqlCommand("AddOpToHierarchy", theConnection);
                    cmdAddOp.Parameters.AddWithValue("@storeID", anOp.storeID.ToString());
                    cmdAddOp.Parameters.AddWithValue("@division", anOp.division);
                    cmdAddOp.Parameters.AddWithValue("@region", anOp.region);
                    cmdAddOp.Parameters.AddWithValue("@area", anOp.area);
                    cmdAddOp.Parameters.AddWithValue("@district", anOp.district);
                    cmdAddOp.Parameters.AddWithValue("@divisionName", anOp.divisionName == null ? DBNull.Value.ToString() : anOp.divisionName);
                    cmdAddOp.Parameters.AddWithValue("@dvpOutlookName", anOp.dvpOutlookname == null ? DBNull.Value.ToString() : anOp.dvpOutlookname);
                    cmdAddOp.Parameters.AddWithValue("@dvpEmailAddress", anOp.dvpEmailAddress == null ? DBNull.Value.ToString() : anOp.dvpEmailAddress);
                    cmdAddOp.Parameters.AddWithValue("@regionName", anOp.regionName == null ? DBNull.Value.ToString() : anOp.regionName);
                    cmdAddOp.Parameters.AddWithValue("@rvpOutlookName", anOp.rvpOutlookName == null ? DBNull.Value.ToString() : anOp.rvpOutlookName);
                    cmdAddOp.Parameters.AddWithValue("@rvpEmailAddress", anOp.rvpEmailAddress == null ? DBNull.Value.ToString() : anOp.rvpEmailAddress);
                    cmdAddOp.Parameters.AddWithValue("@areaName", anOp.areaName == null ? DBNull.Value.ToString() : anOp.areaName);
                    cmdAddOp.Parameters.AddWithValue("@rdOutlookName", anOp.rdOutlookName == null ? DBNull.Value.ToString() : anOp.rdOutlookName);
                    cmdAddOp.Parameters.AddWithValue("@rdEmailAddress", anOp.rdEmailAddress == null ? DBNull.Value.ToString() : anOp.rdEmailAddress);
                    cmdAddOp.Parameters.AddWithValue("@districtName", anOp.districtName == null ? DBNull.Value.ToString() : anOp.districtName);
                    cmdAddOp.Parameters.AddWithValue("@dmOutlookName", anOp.dmOutlookName == null ? DBNull.Value.ToString() : anOp.dmOutlookName);
                    cmdAddOp.Parameters.AddWithValue("@dmEmailAddress", anOp.dmEmailAddress == null ? DBNull.Value.ToString() : anOp.dmEmailAddress);
                    cmdAddOp.CommandType = System.Data.CommandType.StoredProcedure;

                    int numRowsAffected = 0;

                    try
                    {
                        numRowsAffected = cmdAddOp.ExecuteNonQuery();
                    }
                    catch (Exception _exception)
                    {
                        theResponse.statusCode = 6;
                        theResponse.statusDescription = _exception.Message;
                    }

                    if (numRowsAffected > 0)
                    {
                        theResponse.statusCode = 0;
                        theResponse.statusDescription = "";
                    }

                    closeDataConnection();
                }
                else
                {
                    theResponse.statusCode = 2;
                    theResponse.statusDescription = "Op model missing";
                }
            }

            return theResponse;
        }

        public Response UpdateOp(Op anOp)
        {
            Response theResponse = new Response();

            if (anOp == null)
            {
                theResponse.statusCode = 2;
                theResponse.statusDescription = "Expected Op Model is missing";
            }
            else
            {
                if (anOp.storeID == null || anOp.storeID == 0)
                {
                    theResponse.statusDescription = "Store ID not provided";
                }
                if (anOp.area == null || anOp.area.Equals(""))
                {
                    theResponse.statusDescription = "Area not provided";
                }
                if (anOp.division == null || anOp.division.Equals(""))
                {
                    theResponse.statusDescription = "Division not provided";
                }
                if (anOp.region == null || anOp.region.Equals(""))
                {
                    theResponse.statusDescription = "Region not provided";
                }
                if (anOp.district == null || anOp.district.Equals(""))
                {
                    theResponse.statusDescription = "District not provided";
                }

                if (theResponse.statusDescription.Equals(""))
                {
                    openDataConnection();

                    SqlCommand cmdUpdateOp = new SqlCommand("UpdateOp", theConnection);
                    cmdUpdateOp.Parameters.AddWithValue("@storeID", anOp.storeID.ToString());
                    cmdUpdateOp.Parameters.AddWithValue("@division", anOp.division);
                    cmdUpdateOp.Parameters.AddWithValue("@region", anOp.region);
                    cmdUpdateOp.Parameters.AddWithValue("@area", anOp.area);
                    cmdUpdateOp.Parameters.AddWithValue("@district", anOp.district);
                    cmdUpdateOp.Parameters.AddWithValue("@divisionName", anOp.divisionName == null ? DBNull.Value.ToString() : anOp.divisionName);
                    cmdUpdateOp.Parameters.AddWithValue("@dvpOutlookName", anOp.dvpOutlookname == null ? DBNull.Value.ToString() : anOp.dvpOutlookname);
                    cmdUpdateOp.Parameters.AddWithValue("@dvpEmailAddress", anOp.dvpEmailAddress == null ? DBNull.Value.ToString() : anOp.dvpEmailAddress);
                    cmdUpdateOp.Parameters.AddWithValue("@regionName", anOp.regionName == null ? DBNull.Value.ToString() : anOp.regionName);
                    cmdUpdateOp.Parameters.AddWithValue("@rvpOutlookName", anOp.rvpOutlookName == null ? DBNull.Value.ToString() : anOp.rvpOutlookName);
                    cmdUpdateOp.Parameters.AddWithValue("@rvpEmailAddress", anOp.rvpEmailAddress == null ? DBNull.Value.ToString() : anOp.rvpEmailAddress);
                    cmdUpdateOp.Parameters.AddWithValue("@areaName", anOp.areaName == null ? DBNull.Value.ToString() : anOp.areaName);
                    cmdUpdateOp.Parameters.AddWithValue("@rdOutlookName", anOp.rdOutlookName == null ? DBNull.Value.ToString() : anOp.rdOutlookName);
                    cmdUpdateOp.Parameters.AddWithValue("@rdEmailAddress", anOp.rdEmailAddress == null ? DBNull.Value.ToString() : anOp.rdEmailAddress);
                    cmdUpdateOp.Parameters.AddWithValue("@districtName", anOp.districtName == null ? DBNull.Value.ToString() : anOp.districtName);
                    cmdUpdateOp.Parameters.AddWithValue("@dmOutlookName", anOp.dmOutlookName == null ? DBNull.Value.ToString() : anOp.dmOutlookName);
                    cmdUpdateOp.Parameters.AddWithValue("@dmEmailAddress", anOp.dmEmailAddress == null ? DBNull.Value.ToString() : anOp.dmEmailAddress);
                    cmdUpdateOp.CommandType = System.Data.CommandType.StoredProcedure;

                    int numRowsAffected = 0;

                    try
                    {
                        numRowsAffected = cmdUpdateOp.ExecuteNonQuery();
                    }
                    catch (Exception _exception)
                    {
                        WriteToFile(_exception.Message);
                        theResponse.statusCode = 6;
                        theResponse.statusDescription = _exception.Message;
                    }

                    if (numRowsAffected > 0)
                    {
                        theResponse.statusCode = 0;
                        theResponse.statusDescription = "";
                    }
                    else
                    {
                        theResponse.statusDescription = "No rows were updated";
                    }

                    closeDataConnection();
                }
                else
                {
                    theResponse.statusCode = 2;
                    theResponse.statusDescription = "Op model missing";
                }
            }

            return theResponse;
        }

        public Response UpdateRouteStatusToActive(string routeId)
        {
            Response theResponse = new Response();

            openDataConnection();

            SqlCommand cmdAllRouteMaps = new SqlCommand("update route set status = @status where RouteID = @routeID", theConnection);
            cmdAllRouteMaps.Parameters.AddWithValue(@"status", 1);
            cmdAllRouteMaps.Parameters.AddWithValue(@"routeID", int.Parse(routeId));
            cmdAllRouteMaps.CommandType = System.Data.CommandType.Text;
            //cmdUpdate.CommandType = System.Data.CommandType.StoredProcedure;

            try
            {
                int numRowsAffected = cmdAllRouteMaps.ExecuteNonQuery();

                if (numRowsAffected > 0)
                {
                    theResponse.statusCode = 0;
                    theResponse.statusDescription = "";
                }
                else
                {
                    theResponse.statusCode = 6;
                }
            }
            catch (Exception _exception)
            {
                theResponse.statusCode = 6;
                theResponse.statusDescription = _exception.Message;
            }

            closeDataConnection();

            return theResponse;
        }

        public Response UpdateRouteStatusToDeactive(string routeId)
        {
            Response theResponse = new Response();

            openDataConnection();

            SqlCommand cmdAllRouteMaps = new SqlCommand("update route set status = @status where RouteID = @routeID", theConnection);
            cmdAllRouteMaps.Parameters.AddWithValue(@"status", 0);
            cmdAllRouteMaps.Parameters.AddWithValue(@"routeID", int.Parse(routeId));
            cmdAllRouteMaps.CommandType = System.Data.CommandType.Text;
            //cmdUpdate.CommandType = System.Data.CommandType.StoredProcedure;

            try
            {
                int numRowsAffected = cmdAllRouteMaps.ExecuteNonQuery();

                if (numRowsAffected > 0)
                {
                    theResponse.statusCode = 0;
                    theResponse.statusDescription = "";
                }
                else
                {
                    theResponse.statusCode = 6;
                }
            }
            catch (Exception _exception)
            {
                theResponse.statusCode = 6;
                theResponse.statusDescription = _exception.Message;
            }

            closeDataConnection();

            return theResponse;
        }

        public ResponseOpList GetOpsForStoreID(string storeID)
        {
            ResponseOpList theResponse = new ResponseOpList();

            if (storeID == null || storeID.Equals(""))
            {
                theResponse.statusCode = 3;
                theResponse.statusDescription = "Missing Store ID";
            }
            else
            {
                openDataConnection();

                SqlCommand cmdGet = new SqlCommand("GetOpsForStore", theConnection);
                cmdGet.Parameters.AddWithValue("@storeID", storeID);
                cmdGet.CommandType = System.Data.CommandType.StoredProcedure;

                try
                {
                    theReader = cmdGet.ExecuteReader();

                    if (theReader.HasRows)
                    {
                        theResponse.ops = new List<Op>();

                        while (theReader.Read())
                        {
                            Op thisOp = new Op();

                            thisOp.storeID = (int)theReader["StoreID"];
                            thisOp.storeNumber = theReader["StoreNumber"].ToString();
                            thisOp.area = theReader["Area"].ToString();
                            thisOp.division = theReader["Division"].ToString();
                            thisOp.region = theReader["Region"].ToString();
                            thisOp.district = theReader["District"].ToString();
                            thisOp.areaName = theReader["AreaName"].ToString();
                            thisOp.rdOutlookName = theReader["RDOutlookName"].ToString();
                            thisOp.rdEmailAddress = theReader["RDEmailAddress"].ToString();
                            thisOp.divisionName = theReader["DivisionName"].ToString();
                            thisOp.dvpOutlookname = theReader["DVPOutlookName"].ToString();
                            thisOp.dvpEmailAddress = theReader["DVPEmailAddress"].ToString();
                            thisOp.regionName = theReader["RegionName"].ToString();
                            thisOp.rdOutlookName = theReader["RDOutlookName"].ToString();
                            thisOp.rdEmailAddress = theReader["RDEmailAddress"].ToString();
                            thisOp.divisionName = theReader["DivisionName"].ToString();
                            thisOp.dmOutlookName = theReader["DMOutlookName"].ToString();
                            thisOp.dmEmailAddress = theReader["DMEmailAddress"].ToString();
                            thisOp.rvpOutlookName = theReader["RVPOutlookName"].ToString();
                            thisOp.rvpEmailAddress = theReader["RVPEmailAddress"].ToString();

                            theResponse.ops.Add(thisOp);
                        }

                        theResponse.statusCode = 0;
                        theResponse.statusDescription = "";
                    }
                    else
                    {
                        theResponse.statusCode = 4;
                        theResponse.statusDescription = "There are no ops for the store ID " + storeID;
                    }
                }
                catch (Exception _exception)
                {
                    theResponse.statusCode = 6;
                    theResponse.statusDescription = _exception.Message;
                }

                theReader.Close();

                closeDataConnection();
            }

            return theResponse;
        }

        public ResponseOpList GetOpsForStoreNumber(string storeNumber)
        {
            int storeID = getStoreIDForStoreNumber(storeNumber);

            return GetOpsForStoreID(storeID.ToString());
        }

        public ResponseOpList GetAllOps()
        {
            ResponseOpList theResponse = new ResponseOpList();

            openDataConnection();

            SqlCommand cmdGetAllOps = new SqlCommand("GetAllOps", theConnection);
            cmdGetAllOps.CommandType = System.Data.CommandType.StoredProcedure;

            theReader = cmdGetAllOps.ExecuteReader();

            if (theReader.HasRows)
            {
                theResponse.ops = new List<Op>();

                while (theReader.Read())
                {
                    Op thisOp = new Op();

                    thisOp.storeID = (int)theReader["StoreID"];
                    thisOp.storeNumber = theReader["StoreNumber"].ToString();
                    thisOp.area = theReader["Area"].ToString();
                    thisOp.division = theReader["Division"].ToString();
                    thisOp.region = theReader["Region"].ToString();
                    thisOp.district = theReader["District"].ToString();
                    thisOp.areaName = theReader["AreaName"].ToString();
                    thisOp.rdOutlookName = theReader["RDOutlookName"].ToString();
                    thisOp.rdEmailAddress = theReader["RDEmailAddress"].ToString();
                    thisOp.divisionName = theReader["DivisionName"].ToString();
                    thisOp.dvpOutlookname = theReader["DVPOutlookName"].ToString();
                    thisOp.dvpEmailAddress = theReader["DVPEmailAddress"].ToString();
                    thisOp.regionName = theReader["RegionName"].ToString();
                    thisOp.rdOutlookName = theReader["RDOutlookName"].ToString();
                    thisOp.rdEmailAddress = theReader["RDEmailAddress"].ToString();
                    thisOp.divisionName = theReader["DivisionName"].ToString();
                    thisOp.dmOutlookName = theReader["DMOutlookName"].ToString();
                    thisOp.dmEmailAddress = theReader["DMEmailAddress"].ToString();
                    thisOp.rvpOutlookName = theReader["RVPOutlookName"].ToString();
                    thisOp.rvpEmailAddress = theReader["RVPEmailAddress"].ToString();
                    thisOp.districtName = theReader["DistrictName"].ToString();

                    theResponse.ops.Add(thisOp);
                }

                theResponse.statusCode = 0;
                theResponse.statusDescription = "";
            }
            else
            {
                theResponse.statusCode = 4;
                theResponse.statusDescription = "There are no Ops defined";
            }

            closeDataConnection();

            return theResponse;
        }

        public ResponseOpList GetAllOpsWithRange(string startingIndex, string endingIndex)
        {
            ResponseOpList theResponse = new ResponseOpList();

            openDataConnection();

            SqlCommand cmdGetAllOps = new SqlCommand("GetAllOps", theConnection);
            cmdGetAllOps.CommandType = System.Data.CommandType.StoredProcedure;

            theReader = cmdGetAllOps.ExecuteReader();

            int numRecords = 0;

            if (theReader.HasRows)
            {
                List<Op> listOfOps = new List<Op>();

                while (theReader.Read())
                {
                    Op thisOp = new Op();

                    thisOp.storeID = (int)theReader["StoreID"];
                    thisOp.storeNumber = theReader["StoreNumber"].ToString();
                    thisOp.area = theReader["Area"].ToString();
                    thisOp.division = theReader["Division"].ToString();
                    thisOp.region = theReader["Region"].ToString();
                    thisOp.district = theReader["District"].ToString();
                    thisOp.areaName = theReader["AreaName"].ToString();
                    thisOp.rdOutlookName = theReader["RDOutlookName"].ToString();
                    thisOp.rdEmailAddress = theReader["RDEmailAddress"].ToString();
                    thisOp.divisionName = theReader["DivisionName"].ToString();
                    thisOp.dvpOutlookname = theReader["DVPOutlookName"].ToString();
                    thisOp.dvpEmailAddress = theReader["DVPEmailAddress"].ToString();
                    thisOp.regionName = theReader["RegionName"].ToString();
                    thisOp.rdOutlookName = theReader["RDOutlookName"].ToString();
                    thisOp.rdEmailAddress = theReader["RDEmailAddress"].ToString();
                    thisOp.divisionName = theReader["DivisionName"].ToString();
                    thisOp.dmOutlookName = theReader["DMOutlookName"].ToString();
                    thisOp.dmEmailAddress = theReader["DMEmailAddress"].ToString();
                    thisOp.rvpEmailAddress = theReader["RVPEmailAddress"].ToString();
                    thisOp.rvpOutlookName = theReader["RVPOutlookName"].ToString();

                    listOfOps.Add(thisOp);

                    numRecords++;
                }

                theResponse.ops = new List<Op>();

                int startIndex = Int32.Parse(startingIndex);
                int endIndex = Int32.Parse(endingIndex);
                endIndex = startIndex + endIndex;

                if (startIndex <= 0)
                {
                    startIndex = 1;
                }

                if (startIndex > 0 && endIndex >= startIndex)
                {
                    if (endIndex > numRecords)
                    {
                        endIndex = numRecords;
                    }

                    for (int i = startIndex; i <= endIndex; i++)
                    {
                        theResponse.ops.Add(listOfOps[i - 1]);
                    }

                    theResponse.numberOfRecords = numRecords;

                    theResponse.statusCode = 0;
                    theResponse.statusDescription = "";
                }
                else
                {
                    theResponse.statusCode = 6;
                    theResponse.statusDescription = "The starting or ending index did not fall within the data range";
                }
            }
            else
            {
                theResponse.statusCode = 4;
                theResponse.statusDescription = "There are no Ops defined";
            }

            closeDataConnection();

            return theResponse;
        }

        public MyResponse UploadStores(Stream fileStream, string username)
        {
            MyResponse theResponse = new MyResponse();
            //   MYRESPONSE theResponses = new MYRESPONSE();


            string currentPath = HttpContext.Current.Server.MapPath(".");
            long currentTime = DateTime.Now.ToFileTimeUtc();
            string fileName = "stores_" + currentTime;
            string finalPath = currentPath + "\\uploads\\" + fileName;
            FileStream fileToUpload = new FileStream(finalPath, FileMode.Create);

            MultipartParser parser = new MultipartParser(fileStream);

            if (parser.Success)
            {
                fileToUpload.Write(parser.FileContents, 0, parser.FileContents.Length);
                fileToUpload.Close();
                fileToUpload.Dispose();
            }
            else
            {
                theResponse.statusCode = 6;
                theResponse.statusDescription = "Unable to parse input data";

                return theResponse;
            }

            int recordsFound = 0;
            int recordsAdded = 0;
            int recordsUpdated = 0;

            //string connectionString = connectionString = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + finalPath + ";Extended Properties=\"Excel 8.0;HDR=Yes;IMEX=2\"";

            string connectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + finalPath + ";Extended Properties=\"Excel 8.0;HDR=Yes;ReadOnly=False\"";

            List<string> feedback = new List<string>();

            int currentRowPointer = 1;

            try
            {
                OleDbConnection con = new OleDbConnection(connectionString);
                OleDbCommand cmd = new OleDbCommand();
                cmd.CommandType = System.Data.CommandType.Text;
                cmd.Connection = con;
                OleDbDataAdapter dAdapter = new OleDbDataAdapter(cmd);
                DataTable dtExcelRecords = new DataTable();
                con.Open();
                DataTable dtExcelSheetName = con.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
                string getExcelSheetName = dtExcelSheetName.Rows[0]["Table_Name"].ToString();
                cmd.CommandText = "SELECT * FROM [" + getExcelSheetName + "]";

                OleDbDataReader oleReader;
                oleReader = cmd.ExecuteReader();

                if (oleReader.HasRows)
                {
                    List<Store> stores = new List<Store>();

                    while (oleReader.Read())
                    {
                        currentRowPointer++;

                        if (oleReader[0].ToString().Equals("Store #"))
                        {
                            continue;
                        }
                        if (oleReader[0].ToString().Equals(""))
                        {
                            break;
                        }

                        string thisStoreNumber = oleReader[0].ToString();
                        if (doesStoreExist(thisStoreNumber))
                        {
                            Store thisStoreForUpdate = new Store();

                            thisStoreForUpdate.storeID = getStoreIDForStoreNumber(thisStoreNumber);
                            thisStoreForUpdate.storeNumber = oleReader[0].ToString();
                            thisStoreForUpdate.storeName = oleReader[1].ToString();
                            thisStoreForUpdate.storeAddress = oleReader[2].ToString();
                            thisStoreForUpdate.storeCity = oleReader[3].ToString();
                            thisStoreForUpdate.storeZip = oleReader[4].ToString();
                            thisStoreForUpdate.storeState = oleReader[5].ToString();
                            thisStoreForUpdate.storePhone = oleReader[6].ToString();
                            thisStoreForUpdate.storeManagerName = oleReader[7].ToString();
                            thisStoreForUpdate.storeEmailAddress = oleReader[8].ToString();
                            if (oleReader.FieldCount > 9)
                            {
                                thisStoreForUpdate.storeOwnershipType = oleReader[9].ToString();
                            }

                            Response updateStoreResponse = UpdateStore(thisStoreForUpdate);

                            if (updateStoreResponse.statusCode == 0)
                            {
                                feedback.Add("The Store Number " + thisStoreNumber + " already exists in the database. The record was updated.");

                                recordsUpdated++;
                            }
                            else
                            {
                                feedback.Add("The Store Number " + thisStoreNumber + " already exists in the database. The record could not be updated.");
                            }

                            recordsFound++;

                            continue;
                        }

                        Store thisStore = new Store();

                        thisStore.storeNumber = oleReader[0].ToString();
                        thisStore.storeName = oleReader[1].ToString();
                        thisStore.storeAddress = oleReader[2].ToString();
                        thisStore.storeCity = oleReader[3].ToString();
                        thisStore.storeZip = oleReader[4].ToString();
                        thisStore.storeState = oleReader[5].ToString();
                        thisStore.storePhone = oleReader[6].ToString();
                        thisStore.storeManagerName = oleReader[7].ToString();
                        thisStore.storeEmailAddress = oleReader[8].ToString();
                        if (oleReader.FieldCount > 9)
                        {
                            thisStore.storeOwnershipType = oleReader[9].ToString();
                        }

                        stores.Add(thisStore);

                        recordsFound++;
                    }

                    for (int i = 0, l = stores.Count; i < l; i++)
                    {
                        Response createResponse = CreateStore(stores[i]);

                        if (createResponse.statusCode == 0)
                        {
                            recordsAdded++;
                        }
                    }
                }

                oleReader.Close();
            }
            catch (Exception _exception)
            {
                theResponse.statusCode = 6;
                theResponse.statusDescription = _exception.Message + " - " + _exception.StackTrace + " - Last Row Pointer was at " + currentRowPointer;

                return theResponse;
            }

            if (recordsFound > 0)
            {
                theResponse.statusCode = 0;
                theResponse.statusDescription = "Found " + recordsFound + " store records in the file. Added " + recordsAdded + " records to the database.Updated " + recordsUpdated + " records in the database.";
            }
            else
            {
                theResponse.statusCode = 2;
                theResponse.statusDescription = "No records found in the excel file";
            }

            if (feedback.Count > 0)
            {
                string emailString = "";

                theResponse.statusDescription += "Feedback:";
                for (int i = 0, l = feedback.Count; i < l; i++)
                {
                    theResponse.statusDescription += feedback[i];
                    emailString += feedback[i];
                }
                //theResponse.statusDescription;
                //emailString ;

                SendEmailForUploadErrors("Stores", username, emailString, "");
            }

            //           theResponse = new Response();
            //         theResponse.statusCode = 0;
            theResponse.statusDescription = "Response added successfully";
            theResponse.statusCode = 0;
            WebOperationContext.Current.OutgoingResponse.ContentType = "text/html";
            return theResponse;
        }

        public Response UploadStoresDotNet(string fileName, string username)
        {

            int currentRowPointer = 0;

            Response theResponse = new Response();

            string currentPath = HttpContext.Current.Server.MapPath("~");
            long currentTime = DateTime.Now.ToFileTimeUtc();
            //string fileName = "stores_" + currentTime;
            string finalPath = currentPath + "\\uploads\\" + fileName;
            //FileStream fileToUpload = new FileStream(finalPath, FileMode.Create);

            //MultipartParser parser = new MultipartParser(fileStream);

            //if (parser.Success)
            //{
            //    fileToUpload.Write(parser.FileContents, 0, parser.FileContents.Length);
            //    fileToUpload.Close();
            //    fileToUpload.Dispose();
            //}
            //else
            //{
            //    theResponse.statusCode = 6;
            //    theResponse.statusDescription = "Unable to parse input data";

            //    return theResponse;
            //}

            int recordsFound = 0;
            int recordsAdded = 0;
            int recordsUpdated = 0;

            List<string> feedback = new List<string>();

            string connectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + finalPath + ";Extended Properties=\"Excel 8.0;HDR=Yes;ReadOnly=False\"";
            //string connectionString = connectionString = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + finalPath + ";Extended Properties=\"Excel 8.0;HDR=Yes;IMEX=2\"";

            List<Op> opsToBeUpdated = new List<Op>();
            try
            {
                OleDbConnection con = new OleDbConnection(connectionString);
                OleDbCommand cmd = new OleDbCommand();
                cmd.CommandType = System.Data.CommandType.Text;
                cmd.Connection = con;
                OleDbDataAdapter dAdapter = new OleDbDataAdapter(cmd);
                DataTable dtExcelRecords = new DataTable();
                con.Open();
                DataTable dtExcelSheetName = con.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
                string getExcelSheetName = dtExcelSheetName.Rows[0]["Table_Name"].ToString();
                cmd.CommandText = "SELECT * FROM [" + getExcelSheetName + "]";
                cmd.CommandTimeout = 10000;

                OleDbDataReader oleReader;
                oleReader = cmd.ExecuteReader();

                if (oleReader.HasRows)
                {
                    List<Store> stores = new List<Store>();

                    while (oleReader.Read())
                    {
                        currentRowPointer++;

                        if (oleReader[0].ToString().Equals("Store #"))
                        {
                            continue;
                        }
                        if (oleReader[0].ToString().Equals(""))
                        {
                            break;
                        }

                        string thisStoreNumber = oleReader[0].ToString();
                        if (doesStoreExist(thisStoreNumber))
                        {
                            Store thisStoreForUpdate = new Store();

                            thisStoreForUpdate.storeID = getStoreIDForStoreNumber(thisStoreNumber);
                            thisStoreForUpdate.storeNumber = oleReader[0].ToString();
                            thisStoreForUpdate.storeName = oleReader[1].ToString();
                            thisStoreForUpdate.storeAddress = oleReader[2].ToString();
                            thisStoreForUpdate.storeCity = oleReader[3].ToString();
                            thisStoreForUpdate.storeZip = oleReader[4].ToString();
                            thisStoreForUpdate.storeState = oleReader[5].ToString();
                            thisStoreForUpdate.storePhone = oleReader[6].ToString();
                            thisStoreForUpdate.storeManagerName = oleReader[7].ToString();
                            thisStoreForUpdate.storeEmailAddress = oleReader[8].ToString();
                            if (oleReader.FieldCount > 9)
                            {
                                thisStoreForUpdate.storeOwnershipType = oleReader[9].ToString();
                            }
                            if (!String.IsNullOrEmpty(oleReader[10].ToString()))
                            {
                                thisStoreForUpdate.PODRequired = Convert.ToBoolean(oleReader[10].ToString());
                            }


                            Response updateStoreResponse = UpdateStore(thisStoreForUpdate);

                            if (updateStoreResponse.statusCode == 0)
                            {
                                feedback.Add("The Store Number " + thisStoreNumber + " already exists in the database. The record was updated.");

                                recordsUpdated++;
                            }
                            else
                            {
                                feedback.Add("The Store Number " + thisStoreNumber + " already exists in the database. The record could not be updated.");
                            }

                            recordsFound++;

                            continue;
                        }

                        Store thisStore = new Store();

                        thisStore.storeNumber = oleReader[0].ToString();
                        thisStore.storeName = oleReader[1].ToString();
                        thisStore.storeAddress = oleReader[2].ToString();
                        thisStore.storeCity = oleReader[3].ToString();
                        thisStore.storeZip = oleReader[4].ToString();
                        thisStore.storeState = oleReader[5].ToString();
                        thisStore.storePhone = oleReader[6].ToString();
                        thisStore.storeManagerName = oleReader[7].ToString();
                        thisStore.storeEmailAddress = oleReader[8].ToString();
                        if (oleReader.FieldCount > 9)
                        {
                            thisStore.storeOwnershipType = oleReader[9].ToString();
                        }
                        if (!String.IsNullOrEmpty(oleReader[10].ToString()))
                        {
                            thisStore.PODRequired = Convert.ToBoolean(oleReader[10].ToString());
                        }

                        stores.Add(thisStore);

                        recordsFound++;
                    }

                    for (int i = 0, l = stores.Count; i < l; i++)
                    {
                        Response createResponse = CreateStore(stores[i]);

                        if (createResponse.statusCode == 0)
                        {
                            recordsAdded++;
                        }
                    }
                }

                oleReader.Close();
            }
            catch (Exception _exception)
            {
                theResponse.statusCode = 6;
                theResponse.statusDescription = _exception.Message + " - " + _exception.StackTrace + " - Last Row Pointer was at " + currentRowPointer;

                return theResponse;
            }

            if (recordsFound > 0)
            {
                theResponse.statusCode = 0;
                theResponse.statusDescription = "Found " + recordsFound + " store records in the file. Added " + recordsAdded + " records to the database.Updated " + recordsUpdated + " records in the database.";
            }
            else
            {
                theResponse.statusCode = 2;
                theResponse.statusDescription = "No records found in the excel file";
            }

            if (feedback.Count > 0)
            {
                /*  string emailString = "";

                  theResponse.statusDescription += "Feedback:";
                  for (int i = 0, l = feedback.Count; i < l; i++)
                  {
                      theResponse.statusDescription += feedback[i];
                      emailString += feedback[i];
                  }
                  */

                string emailString = "<ul>";

                theResponse.statusDescription += "<br /><br /><p>Feedback:</p><ul>";
                for (int i = 0, l = feedback.Count; i < l; i++)
                {
                    theResponse.statusDescription += "<li>" + feedback[i] + "</li>";
                    emailString += "<li>" + feedback[i] + "</li>";
                }
                theResponse.statusDescription += "</ul>";
                emailString += "</ul>";

                //theResponse.statusDescription;
                //emailString ;

                SendEmailForUploadErrors("Stores", username, emailString, "");
            }

            //           theResponse = new Response();
            //         theResponse.statusCode = 0;
            // theResponse.statusDescription = "Response added successfully";
            theResponse.statusCode = 0;
            //WebOperationContext.Current.OutgoingResponse.ContentType = "text/html";
            return theResponse;
        }

        public Response UploadRoutes(Stream fileStream, string username)
        {
            Response theResponse = new Response();

            string cdcName = "";

            string currentPath = HttpContext.Current.Server.MapPath(".");
            long currentTime = DateTime.Now.ToFileTimeUtc();
            string fileName = "routes_" + currentTime;
            string finalPath = currentPath + "\\uploads\\" + fileName;
            FileStream fileToUpload = new FileStream(finalPath, FileMode.Create);

            MultipartParser parser = new MultipartParser(fileStream);

            if (parser.Success)
            {
                fileToUpload.Write(parser.FileContents, 0, parser.FileContents.Length);
                fileToUpload.Close();
                fileToUpload.Dispose();
            }
            else
            {
                theResponse.statusCode = 6;
                theResponse.statusDescription = "Unable to parse input data";

                return theResponse;
            }

            int recordsFound = 0;
            int recordsAdded = 0;
            int recordsUpdated = 0;

            List<string> feedback = new List<string>();

            //string connectionString = connectionString = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + finalPath + ";Extended Properties=\"Excel 8.0;HDR=Yes;IMEX=2\"";
            string connectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + finalPath + ";Extended Properties=\"Excel 8.0;HDR=Yes;IMEX=1;ReadOnly=False\"";
            try
            {
                OleDbConnection con = new OleDbConnection(connectionString);
                OleDbCommand cmd = new OleDbCommand();
                cmd.CommandType = System.Data.CommandType.Text;
                cmd.Connection = con;
                OleDbDataAdapter dAdapter = new OleDbDataAdapter(cmd);
                DataTable dtExcelRecords = new DataTable();
                con.Open();
                DataTable dtExcelSheetName = con.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
                string getExcelSheetName = dtExcelSheetName.Rows[0]["Table_Name"].ToString();
                cmd.CommandText = "SELECT * FROM [" + getExcelSheetName + "]";

                OleDbDataReader oleReader;
                oleReader = cmd.ExecuteReader();

                if (oleReader.HasRows)
                {
                    List<Route> routes = new List<Route>();

                    int rowCounter = 0;

                    while (oleReader.Read())
                    {
                        rowCounter += 1;


                        if (oleReader[0].ToString().Equals("Transaction Type") || oleReader[1].ToString().Equals("CDC"))
                        {
                            continue;
                        }
                        if (oleReader[0].ToString().Equals(""))
                        {
                            break;
                        }

                        if (cdcName == "")
                            cdcName = oleReader[1].ToString();

                        if (oleReader[0].ToString().ToUpper().Equals("ADD"))
                        {
                            recordsFound++;

                            if (!doesCDCExist(oleReader[1].ToString()))
                            {
                                feedback.Add("CDC " + oleReader[1].ToString() + " does not exist");

                                continue;
                            }

                            if (doesRouteExist(oleReader[2].ToString()))
                            {
                                feedback.Add("The route " + oleReader[2].ToString() + " already exists");

                                continue;
                            }

                            Route thisRoute = new Route();

                            thisRoute.cdc = new CDC();
                            thisRoute.cdc.name = oleReader[1].ToString();


                            thisRoute.cdc.id = getCDCIDForCDCName(oleReader[1].ToString());

                            thisRoute.routeName = oleReader[2].ToString();

                            int numberOfStopsForThisRoute = oleReader.FieldCount;

                            List<Store> stops = new List<Store>();

                            for (int i = 3; i < numberOfStopsForThisRoute; i++)
                            {
                                Store thisStore = new Store();

                                string thisStoreNumber = oleReader[i].ToString();

                                thisStore.storeID = getStoreIDForStoreNumber(thisStoreNumber);

                                if (thisStore.storeID > 0)
                                {
                                    stops.Add(thisStore);
                                }
                                else
                                {
                                    if (!thisStoreNumber.Equals("0") && !thisStoreNumber.Equals(""))
                                    {
                                        feedback.Add("The Store Number " + thisStoreNumber + " was not found in the database");
                                    }
                                }
                            }

                            if (stops.Count > 0)
                            {
                                thisRoute.stores = stops;

                                feedback.Add("Route " + thisRoute.routeName + " has " + thisRoute.stores.Count + " stops.");

                                routes.Add(thisRoute);
                            }
                            else
                            {
                                feedback.Add("The Route " + thisRoute.routeName + " could not be added as none of the stores listed against this route are present in the database");
                            }
                        }
                        else if (oleReader[0].ToString().ToUpper().Equals("UPDATE"))
                        {
                            recordsFound++;

                            if (!doesRouteExist(oleReader[2].ToString()))
                            {
                                feedback.Add("The route " + oleReader[2].ToString() + " does not exist thus cannot be updated");

                                continue;
                            }
                            else
                            {
                                string thisRouteName = oleReader[2].ToString();

                                ResponseRouteList thisRouteDetail = GetRouteDetail(thisRouteName);

                                bool validRoute = false;
                                int numberOfStops = 0;

                                if (thisRouteDetail != null)
                                {
                                    validRoute = true;

                                    if (thisRouteDetail.routes != null)
                                    {
                                        numberOfStops = thisRouteDetail.routes[0].stores.Count;
                                    }
                                }

                                if (validRoute)
                                {
                                    openDataConnection();

                                    SqlCommand cmdDisableMappingsForRoute = new SqlCommand("DisableCurrentMappingsForRouteName", theConnection);
                                    cmdDisableMappingsForRoute.Parameters.AddWithValue("@routeName", thisRouteName);
                                    cmdDisableMappingsForRoute.CommandType = System.Data.CommandType.StoredProcedure;

                                    int numMappingsDisabled = cmdDisableMappingsForRoute.ExecuteNonQuery();

                                    closeDataConnection();

                                    if (numMappingsDisabled >= numberOfStops)
                                    {
                                        List<Store> newStops = new List<Store>();

                                        int numberOfStopsForThisRoute = oleReader.FieldCount;

                                        for (int i = 3; i < numberOfStopsForThisRoute; i++)
                                        {
                                            Store thisStore = new Store();

                                            string thisStoreNumber = oleReader[i].ToString();

                                            if (thisStoreNumber != null && !thisStoreNumber.Equals(""))
                                            {
                                                thisStore.storeNumber = thisStoreNumber;
                                                thisStore.storeID = getStoreIDForStoreNumber(thisStoreNumber);

                                                if (thisStore.storeID > 0)
                                                {
                                                    newStops.Add(thisStore);
                                                }
                                                else
                                                {
                                                    feedback.Add("The Store Number " + thisStoreNumber + " was not found in the database");
                                                }
                                            }
                                        }

                                        openDataConnection();

                                        foreach (Store aStore in newStops)
                                        {
                                            SqlCommand cmdAddStoreToRoute = new SqlCommand("AddStoreToRoute", theConnection);
                                            cmdAddStoreToRoute.Parameters.AddWithValue("@routeName", thisRouteName);
                                            cmdAddStoreToRoute.Parameters.AddWithValue("@storeID", aStore.storeID);
                                            cmdAddStoreToRoute.CommandType = System.Data.CommandType.StoredProcedure;

                                            int numRowsAffectedForAddStoreToRoute = cmdAddStoreToRoute.ExecuteNonQuery();

                                            feedback.Add("Added Store " + aStore.storeNumber + " to Route " + thisRouteName);
                                        }
                                        feedback.Add("Updated Route " + thisRouteName);

                                        closeDataConnection();

                                        recordsUpdated++;
                                    }
                                    else
                                    {
                                        feedback.Add("The route " + oleReader[2].ToString() + " had " + numberOfStops + " mappings but only " + numMappingsDisabled + " were disabled and this route cannot be updated");

                                        continue;
                                    }
                                }
                                else
                                {
                                    feedback.Add("The route " + oleReader[2].ToString() + " does not seem to be valid and thus cannot be updated");

                                    continue;
                                }
                            }
                        }

                    }

                    for (int i = 0, l = routes.Count; i < l; i++)
                    {
                        Response createResponse = CreateRoute(routes[i]);

                        if (createResponse.statusCode == 0)
                        {
                            recordsAdded++;
                        }
                    }
                }

                oleReader.Close();
            }
            catch (Exception _exception)
            {
                theResponse.statusCode = 6;
                theResponse.statusDescription = _exception.Message + " Line Number: " + _exception.StackTrace;

                return theResponse;
            }

            if (recordsFound > 0)
            {
                theResponse.statusCode = 0;
                theResponse.statusDescription = "Found " + recordsFound + " route records in the file.<br />Added " + recordsAdded + " records to the database.<br />Updated " + recordsUpdated + " records in the database.";
            }
            else
            {
                theResponse.statusCode = 2;
                theResponse.statusDescription = "No records found in the excel file";
            }

            if (feedback.Count > 0)
            {
                string emailString = "<ul>";

                theResponse.statusDescription += "<br /><br /><p>Feedback:</p><ul>";
                for (int i = 0, l = feedback.Count; i < l; i++)
                {
                    theResponse.statusDescription += "<li>" + feedback[i] + "</li>";
                    emailString += "<li>" + feedback[i] + "</li>";
                }
                theResponse.statusDescription += "</ul>";
                emailString += "</ul>";

                SendEmailForUploadErrors("Routes", username, emailString, cdcName);
            }

            return theResponse;
        }

        public Response UploadRoutesDotNet(string fileName, string username)
        {
            Response theResponse = new Response();

            string cdcName = "";

            string currentPath = HttpContext.Current.Server.MapPath("~");
            long currentTime = DateTime.Now.ToFileTimeUtc();
            //string fileName = "routes_" + currentTime;
            string finalPath = currentPath + "\\uploads\\" + fileName;
            //FileStream fileToUpload = new FileStream(finalPath, FileMode.Create);

            //MultipartParser parser = new MultipartParser(fileStream);

            //if (parser.Success)
            //{
            //    fileToUpload.Write(parser.FileContents, 0, parser.FileContents.Length);
            //    fileToUpload.Close();
            //    fileToUpload.Dispose();
            //}
            //else
            //{
            //    theResponse.statusCode = 6;
            //    theResponse.statusDescription = "Unable to parse input data";

            //    return theResponse;
            //}

            int recordsFound = 0;
            int recordsAdded = 0;
            int recordsUpdated = 0;

            List<string> feedback = new List<string>();

            //string connectionString = connectionString = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + finalPath + ";Extended Properties=\"Excel 8.0;HDR=Yes;IMEX=2\"";
            string connectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + finalPath + ";Extended Properties=\"Excel 8.0;HDR=Yes;IMEX=1;ReadOnly=False\"";
            try
            {
                OleDbConnection con = new OleDbConnection(connectionString);
                OleDbCommand cmd = new OleDbCommand();
                cmd.CommandType = System.Data.CommandType.Text;
                cmd.Connection = con;
                OleDbDataAdapter dAdapter = new OleDbDataAdapter(cmd);
                DataTable dtExcelRecords = new DataTable();
                con.Open();
                DataTable dtExcelSheetName = con.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
                string getExcelSheetName = dtExcelSheetName.Rows[0]["Table_Name"].ToString();
                cmd.CommandText = "SELECT * FROM [" + getExcelSheetName + "]";

                OleDbDataReader oleReader;
                oleReader = cmd.ExecuteReader();

                if (oleReader.HasRows)
                {
                    List<Route> routes = new List<Route>();

                    int rowCounter = 0;

                    while (oleReader.Read())
                    {
                        rowCounter += 1;


                        if (oleReader[0].ToString().Equals("Transaction Type") || oleReader[1].ToString().Equals("CDC"))
                        {
                            continue;
                        }
                        if (oleReader[0].ToString().Equals(""))
                        {
                            break;
                        }

                        if (cdcName == "")
                            cdcName = oleReader[1].ToString();

                        if (oleReader[0].ToString().ToUpper().Trim().Equals("ADD"))
                        {
                            recordsFound++;

                            if (!doesCDCExist(oleReader[1].ToString()))
                            {
                                feedback.Add("CDC " + oleReader[1].ToString() + " does not exist");

                                continue;
                            }

                            if (doesRouteExist(oleReader[2].ToString()))
                            {
                                feedback.Add("The route " + oleReader[2].ToString() + " already exists");

                                continue;
                            }

                            Route thisRoute = new Route();

                            thisRoute.cdc = new CDC();
                            thisRoute.cdc.name = oleReader[1].ToString();


                            thisRoute.cdc.id = getCDCIDForCDCName(oleReader[1].ToString());

                            thisRoute.routeName = oleReader[2].ToString();

                            int numberOfStopsForThisRoute = oleReader.FieldCount;

                            List<Store> stops = new List<Store>();

                            for (int i = 3; i < numberOfStopsForThisRoute; i++)
                            {
                                Store thisStore = new Store();

                                string thisStoreNumber = oleReader[i].ToString();

                                thisStore.storeID = getStoreIDForStoreNumber(thisStoreNumber);

                                if (thisStore.storeID > 0)
                                {
                                    stops.Add(thisStore);
                                }
                                else
                                {
                                    if (!thisStoreNumber.Equals("0") && !thisStoreNumber.Equals(""))
                                    {
                                        feedback.Add("The Store Number " + thisStoreNumber + " was not found in the database");
                                    }
                                }
                            }

                            if (stops.Count > 0)
                            {
                                thisRoute.stores = stops;

                                feedback.Add("Route " + thisRoute.routeName + " has " + thisRoute.stores.Count + " stops.");

                                routes.Add(thisRoute);
                            }
                            else
                            {
                                feedback.Add("The Route " + thisRoute.routeName + " could not be added as none of the stores listed against this route are present in the database");
                            }
                        }
                        else if (oleReader[0].ToString().ToUpper().Trim().Equals("UPDATE"))
                        {
                            recordsFound++;

                            if (!doesRouteExist(oleReader[2].ToString()))
                            {
                                feedback.Add("The route " + oleReader[2].ToString() + " does not exist thus cannot be updated");

                                continue;
                            }
                            else
                            {
                                string thisRouteName = oleReader[2].ToString();

                                ResponseRouteList thisRouteDetail = GetRouteDetail(thisRouteName);

                                bool validRoute = false;
                                int numberOfStops = 0;

                                if (thisRouteDetail != null)
                                {
                                    validRoute = true;

                                    if (thisRouteDetail.routes != null)
                                    {
                                        numberOfStops = thisRouteDetail.routes[0].stores.Count;
                                    }
                                }

                                if (validRoute)
                                {
                                    openDataConnection();

                                    SqlCommand cmdDisableMappingsForRoute = new SqlCommand("DisableCurrentMappingsForRouteName", theConnection);
                                    cmdDisableMappingsForRoute.Parameters.AddWithValue("@routeName", thisRouteName);
                                    cmdDisableMappingsForRoute.CommandType = System.Data.CommandType.StoredProcedure;

                                    int numMappingsDisabled = cmdDisableMappingsForRoute.ExecuteNonQuery();

                                    closeDataConnection();

                                    if (numMappingsDisabled >= numberOfStops)
                                    {
                                        List<Store> newStops = new List<Store>();

                                        int numberOfStopsForThisRoute = oleReader.FieldCount;

                                        for (int i = 3; i < numberOfStopsForThisRoute; i++)
                                        {
                                            Store thisStore = new Store();

                                            string thisStoreNumber = oleReader[i].ToString();

                                            if (thisStoreNumber != null && !thisStoreNumber.Equals(""))
                                            {
                                                thisStore.storeNumber = thisStoreNumber;
                                                thisStore.storeID = getStoreIDForStoreNumber(thisStoreNumber);

                                                if (thisStore.storeID > 0)
                                                {
                                                    newStops.Add(thisStore);
                                                }
                                                else
                                                {
                                                    feedback.Add("The Store Number " + thisStoreNumber + " was not found in the database");
                                                }
                                            }
                                        }

                                        openDataConnection();

                                        foreach (Store aStore in newStops)
                                        {
                                            SqlCommand cmdAddStoreToRoute = new SqlCommand("AddStoreToRoute", theConnection);
                                            cmdAddStoreToRoute.Parameters.AddWithValue("@routeName", thisRouteName);
                                            cmdAddStoreToRoute.Parameters.AddWithValue("@storeID", aStore.storeID);
                                            cmdAddStoreToRoute.CommandType = System.Data.CommandType.StoredProcedure;

                                            int numRowsAffectedForAddStoreToRoute = cmdAddStoreToRoute.ExecuteNonQuery();

                                            feedback.Add("Added Store " + aStore.storeNumber + " to Route " + thisRouteName);
                                        }
                                        feedback.Add("Updated Route " + thisRouteName);

                                        closeDataConnection();

                                        recordsUpdated++;
                                    }
                                    else
                                    {
                                        feedback.Add("The route " + oleReader[2].ToString() + " had " + numberOfStops + " mappings but only " + numMappingsDisabled + " were disabled and this route cannot be updated");

                                        continue;
                                    }
                                }
                                else
                                {
                                    feedback.Add("The route " + oleReader[2].ToString() + " does not seem to be valid and thus cannot be updated");

                                    continue;
                                }
                            }
                        }

                    }

                    for (int i = 0, l = routes.Count; i < l; i++)
                    {
                        Response createResponse = CreateRoute(routes[i]);

                        if (createResponse.statusCode == 0)
                        {
                            recordsAdded++;
                        }
                    }
                }

                oleReader.Close();
            }
            catch (Exception _exception)
            {
                theResponse.statusCode = 6;
                theResponse.statusDescription = _exception.Message + " Line Number: " + _exception.StackTrace;

                return theResponse;
            }

            if (recordsFound > 0)
            {
                theResponse.statusCode = 0;
                theResponse.statusDescription = "Found " + recordsFound + " route records in the file.<br />Added " + recordsAdded + " records to the database.<br />Updated " + recordsUpdated + " records in the database.";
            }
            else
            {
                theResponse.statusCode = 2;
                theResponse.statusDescription = "No records found in the excel file";
            }

            if (feedback.Count > 0)
            {
                string emailString = "<ul>";

                theResponse.statusDescription += "<br /><br /><p>Feedback:</p><ul>";
                for (int i = 0, l = feedback.Count; i < l; i++)
                {
                    theResponse.statusDescription += "<li>" + feedback[i] + "</li>";
                    emailString += "<li>" + feedback[i] + "</li>";
                }
                theResponse.statusDescription += "</ul>";
                emailString += "</ul>";

                SendEmailForUploadErrors("Routes", username, emailString, cdcName);
            }

            return theResponse;
        }

        public Response UploadOps(Stream fileStream, string username)
        {
            Response theResponse = new Response();

            string currentPath = HttpContext.Current.Server.MapPath(".");
            long currentTime = DateTime.Now.ToFileTimeUtc();
            string fileName = "ops_" + currentTime;
            string finalPath = currentPath + "\\uploads\\" + fileName;
            FileStream fileToUpload = new FileStream(finalPath, FileMode.Create);

            MultipartParser parser = new MultipartParser(fileStream);

            if (parser.Success)
            {
                fileToUpload.Write(parser.FileContents, 0, parser.FileContents.Length);
                fileToUpload.Close();
                fileToUpload.Dispose();
            }
            else
            {
                theResponse.statusCode = 6;
                theResponse.statusDescription = "Unable to parse input data";

                return theResponse;
            }

            int recordsFound = 0;
            int recordsAdded = 0;
            int recordsUpdated = 0;

            List<string> feedback = new List<string>();

            string connectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + finalPath + ";Extended Properties=\"Excel 8.0;HDR=Yes;ReadOnly=False\"";
            //string connectionString = connectionString = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + finalPath + ";Extended Properties=\"Excel 8.0;HDR=Yes;IMEX=2\"";

            List<Op> opsToBeUpdated = new List<Op>();
            try
            {
                OleDbConnection con = new OleDbConnection(connectionString);
                OleDbCommand cmd = new OleDbCommand();
                cmd.CommandType = System.Data.CommandType.Text;
                cmd.Connection = con;
                OleDbDataAdapter dAdapter = new OleDbDataAdapter(cmd);
                DataTable dtExcelRecords = new DataTable();
                con.Open();
                DataTable dtExcelSheetName = con.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
                string getExcelSheetName = dtExcelSheetName.Rows[0]["Table_Name"].ToString();
                cmd.CommandText = "SELECT * FROM [" + getExcelSheetName + "]";

                OleDbDataReader oleReader;
                oleReader = cmd.ExecuteReader();

                if (oleReader.HasRows)
                {
                    List<Op> ops = new List<Op>();


                    while (oleReader.Read())
                    {
                        if (oleReader.FieldCount < 18)
                        {
                            continue;
                        }

                        if (oleReader[0].ToString().Equals("Transaction Type"))
                        {
                            continue;
                        }
                        if (oleReader[0].ToString().Equals(""))
                        {
                            break;
                        }

                        if (oleReader[0].ToString().ToUpper().Equals("ADD") || oleReader[0].ToString().ToUpper().Equals("UPDATE"))
                        {
                            recordsFound++;

                            string thisStoreNumber = oleReader[17].ToString();
                            if (!doesStoreExist(thisStoreNumber))
                            {
                                feedback.Add("The Store Number " + thisStoreNumber + " does not exist in the database");

                                continue;
                            }

                            int thisStoreID = getStoreIDForStoreNumber(thisStoreNumber);

                            Op thisOp = new Op();
                            thisOp.storeID = thisStoreID;
                            thisOp.division = oleReader[1].ToString();
                            thisOp.divisionName = oleReader[2].ToString();
                            thisOp.dvpOutlookname = oleReader[3].ToString();
                            thisOp.dvpEmailAddress = oleReader[4].ToString();
                            thisOp.region = oleReader[5].ToString();
                            thisOp.regionName = oleReader[6].ToString();
                            thisOp.rvpOutlookName = oleReader[7].ToString();
                            thisOp.rvpEmailAddress = oleReader[8].ToString();
                            thisOp.area = oleReader[9].ToString();
                            thisOp.areaName = oleReader[10].ToString();
                            thisOp.rdOutlookName = oleReader[11].ToString();
                            thisOp.rdEmailAddress = oleReader[12].ToString();
                            thisOp.district = oleReader[13].ToString();
                            thisOp.districtName = oleReader[14].ToString();
                            thisOp.dmOutlookName = oleReader[15].ToString();
                            thisOp.dmEmailAddress = oleReader[16].ToString();

                            if (oleReader[0].ToString().ToUpper().Equals("ADD") && !doesStoreExistInOps(thisOp.storeID.ToString()))
                            {

                                ops.Add(thisOp);
                            }
                            else if (oleReader[0].ToString().Trim().ToUpper().Equals("UPDATE"))
                            {
                                opsToBeUpdated.Add(thisOp);
                            }
                        }
                    }

                    for (int i = 0, l = ops.Count; i < l; i++)
                    {
                        Response opCreationResponse = AddOp(ops[i]);

                        if (opCreationResponse.statusCode == 0)
                        {
                            recordsAdded++;
                        }
                    }
                    for (int i = 0; i < opsToBeUpdated.Count; i++)
                    {
                        Response opUpdateResponse = UpdateOp(opsToBeUpdated[i]);

                        if (opUpdateResponse.statusCode == 0)
                        {
                            recordsUpdated++;
                        }
                    }
                }

                oleReader.Close();
            }
            catch (Exception _exception)
            {
                theResponse.statusCode = 6;
                theResponse.statusDescription = _exception.Message;

                return theResponse;
            }

            if (recordsFound > 0)
            {
                theResponse.statusCode = 0;
                theResponse.statusDescription = "Found " + recordsFound + " Op hierarchy records in the file.<br />Added " + recordsAdded + " records to the database.<br />Updated " + recordsUpdated + " records in the database";
            }
            else
            {
                theResponse.statusCode = 2;
                theResponse.statusDescription = "No records found in the excel file";
            }

            if (feedback.Count > 0)
            {
                string emailString = "<ul>";

                theResponse.statusDescription += "<br /><br /><p>Feedback:</p><ul>";
                for (int i = 0, l = feedback.Count; i < l; i++)
                {
                    theResponse.statusDescription += "<li>" + feedback[i] + "</li>";
                    emailString += "<li>" + feedback[i] + "</li>";
                }
                theResponse.statusDescription += "</ul>";
                emailString += "</ul>";

                SendEmailForUploadErrors("Ops Hierarchy", username, emailString, "");
            }

            return theResponse;
        }

        public Response UploadOpsDotNet(string fileName, string username)
        {
            Response theResponse = new Response();

            string currentPath = HttpContext.Current.Server.MapPath("~");
            long currentTime = DateTime.Now.ToFileTimeUtc();
            //string fileName = "ops_" + currentTime;
            string finalPath = currentPath + "\\uploads\\" + fileName;
            //FileStream fileToUpload = new FileStream(finalPath, FileMode.Create);

            //MultipartParser parser = new MultipartParser(fileStream);

            //if (parser.Success)
            //{
            //    fileToUpload.Write(parser.FileContents, 0, parser.FileContents.Length);
            //    fileToUpload.Close();
            //    fileToUpload.Dispose();
            //}
            //else
            //{
            //    theResponse.statusCode = 6;
            //    theResponse.statusDescription = "Unable to parse input data";

            //    return theResponse;
            //}

            int recordsFound = 0;
            int recordsAdded = 0;
            int recordsUpdated = 0;

            List<string> feedback = new List<string>();

            string connectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + finalPath + ";Extended Properties=\"Excel 8.0;HDR=Yes;Connect Timeout=12000; ReadOnly=False\"";
            //string connectionString = connectionString = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + finalPath + ";Extended Properties=\"Excel 8.0;HDR=Yes;IMEX=2\"";

            List<Op> opsToBeUpdated = new List<Op>();
            try
            {
                OleDbConnection con = new OleDbConnection(connectionString);
                OleDbCommand cmd = new OleDbCommand();
                cmd.CommandType = System.Data.CommandType.Text;
                cmd.Connection = con;
                OleDbDataAdapter dAdapter = new OleDbDataAdapter(cmd);
                DataTable dtExcelRecords = new DataTable();
                con.Open();
                DataTable dtExcelSheetName = con.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
                string getExcelSheetName = dtExcelSheetName.Rows[0]["Table_Name"].ToString();
                cmd.CommandText = "SELECT * FROM [" + getExcelSheetName + "]";
                cmd.CommandTimeout = 6000;

                OleDbDataReader oleReader;
                oleReader = cmd.ExecuteReader();

                if (oleReader.HasRows)
                {
                    List<Op> ops = new List<Op>();


                    while (oleReader.Read())
                    {

                        if (oleReader.FieldCount < 18)
                        {
                            continue;
                        }


                        if (oleReader[0].ToString().Equals("Transaction Type"))
                        {
                            continue;
                        }

                        if (oleReader[0].ToString().Equals(""))
                        {
                            break;
                        }


                        if (oleReader[0].ToString().ToUpper().Equals("ADD") || oleReader[0].ToString().ToUpper().Equals("UPDATE"))
                        {

                            recordsFound++;

                            string thisStoreNumber = oleReader[17].ToString();

                            if (!doesStoreExist(thisStoreNumber))
                            {
                                feedback.Add("The Store Number " + thisStoreNumber + " does not exist in the database");

                                continue;
                            }

                            int thisStoreID = getStoreIDForStoreNumber(thisStoreNumber);


                            Op thisOp = new Op();
                            thisOp.storeID = thisStoreID;
                            thisOp.division = oleReader[1].ToString();
                            thisOp.divisionName = oleReader[2].ToString();
                            thisOp.dvpOutlookname = oleReader[3].ToString();
                            thisOp.dvpEmailAddress = oleReader[4].ToString();
                            thisOp.region = oleReader[5].ToString();
                            thisOp.regionName = oleReader[6].ToString();
                            thisOp.rvpOutlookName = oleReader[7].ToString();
                            thisOp.rvpEmailAddress = oleReader[8].ToString();
                            thisOp.area = oleReader[9].ToString();
                            thisOp.areaName = oleReader[10].ToString();
                            thisOp.rdOutlookName = oleReader[11].ToString();
                            thisOp.rdEmailAddress = oleReader[12].ToString();
                            thisOp.district = oleReader[13].ToString();
                            thisOp.districtName = oleReader[14].ToString();
                            thisOp.dmOutlookName = oleReader[15].ToString();
                            thisOp.dmEmailAddress = oleReader[16].ToString();

                            if (oleReader[0].ToString().ToUpper().Equals("ADD") && !doesStoreExistInOps(thisOp.storeID.ToString()))
                            {

                                ops.Add(thisOp);
                            }
                            else if (oleReader[0].ToString().Trim().ToUpper().Equals("UPDATE"))
                            {
                                opsToBeUpdated.Add(thisOp);
                            }
                        }
                    }

                    for (int i = 0, l = ops.Count; i < l; i++)
                    {
                        Response opCreationResponse = AddOp(ops[i]);

                        if (opCreationResponse.statusCode == 0)
                        {
                            recordsAdded++;
                        }
                    }
                    for (int i = 0; i < opsToBeUpdated.Count; i++)
                    {
                        Response opUpdateResponse = UpdateOp(opsToBeUpdated[i]);

                        if (opUpdateResponse.statusCode == 0)
                        {
                            recordsUpdated++;
                        }
                    }
                }

                oleReader.Close();
            }
            catch (Exception _exception)
            {

                theResponse.statusCode = 6;
                theResponse.statusDescription = _exception.Message;


                return theResponse;
            }

            if (recordsFound > 0)
            {
                theResponse.statusCode = 0;
                theResponse.statusDescription = "Found " + recordsFound + " Op hierarchy records in the file.<br />Added " + recordsAdded + " records to the database.<br />Updated " + recordsUpdated + " records in the database";
            }
            else
            {
                theResponse.statusCode = 2;
                theResponse.statusDescription = "No records found in the excel file";
            }

            if (feedback.Count > 0)
            {
                string emailString = "<ul>";

                theResponse.statusDescription += "<br /><br /><p>Feedback:</p><ul>";
                for (int i = 0, l = feedback.Count; i < l; i++)
                {
                    theResponse.statusDescription += "<li>" + feedback[i] + "</li>";
                    emailString += "<li>" + feedback[i] + "</li>";
                }
                theResponse.statusDescription += "</ul>";
                emailString += "</ul>";

                SendEmailForUploadErrors("Ops Hierarchy", username, emailString, "");
            }

            return theResponse;
        }

        public int getStoreIDForStoreNumber(string storeNumber)
        {
            if (storeNumber == null || storeNumber.Equals(""))
            {
                return 0;
            }

            openDataConnection();

            int storeID = 0;

            SqlCommand cmdGetID = new SqlCommand("SELECT StoreID FROM Store WHERE StoreNumber = '" + storeNumber + "'", theConnection);
            theReader = cmdGetID.ExecuteReader();

            if (theReader.HasRows)
            {
                while (theReader.Read())
                {
                    storeID = (int)theReader["StoreID"];
                }
            }

            closeDataConnection();

            return storeID;
        }

        public int getCDCIDForCDCName(string cdcName)
        {
            if (cdcName == null || cdcName.Equals(""))
            {
                return 0;
            }

            openDataConnection();

            int cdcID = 0;

            SqlCommand cmdGetID = new SqlCommand("SELECT CDCID FROM CDC WHERE CDCName = '" + cdcName + "'", theConnection);
            theReader = cmdGetID.ExecuteReader();

            if (theReader.HasRows)
            {
                while (theReader.Read())
                {
                    cdcID = (int)theReader["CDCID"];
                }
            }

            closeDataConnection();

            return cdcID;
        }

        private bool doesRouteExist(string routeName)
        {
            if (routeName == null || routeName.Equals(""))
            {
                return false;
            }

            bool exists = false;

            openDataConnection();

            SqlCommand cmdCheck = new SqlCommand("RouteExists", theConnection);
            cmdCheck.Parameters.AddWithValue("@routename", routeName);
            cmdCheck.CommandType = System.Data.CommandType.StoredProcedure;

            theReader = cmdCheck.ExecuteReader();

            if (theReader.HasRows)
            {
                exists = true;
            }

            theReader.Close();

            closeDataConnection();

            return exists;
        }

        private bool doesCDCExist(string CDCName)
        {
            if (CDCName == null || CDCName.Equals(""))
            {
                return false;
            }

            bool exists = false;

            openDataConnection();

            SqlCommand cmdCheck = new SqlCommand("SELECT * FROM CDC WHERE CDCName = '" + CDCName + "'", theConnection);

            theReader = cmdCheck.ExecuteReader();

            if (theReader.HasRows)
            {
                exists = true;
            }

            theReader.Close();

            closeDataConnection();

            return exists;
        }

        private bool doesStoreExistInOps(string storeNumber)
        {
            if (storeNumber == null || storeNumber.Equals(""))
            {
                return false;
            }

            bool exists = false;

            openDataConnection();

            SqlCommand cmdCheck = new SqlCommand("SELECT * FROM OpsHierarchy WHERE StoreID = '" + storeNumber + "'", theConnection);

            theReader = cmdCheck.ExecuteReader();
            if (theReader != null || theReader.Read())
            {
                try
                {
                    while (theReader.Read())
                    {

                        string storeId = theReader["StoreID"].ToString();
                        exists = true;
                    }
                }
                catch (Exception exp)
                {

                }
            }
            theReader.Close();
            closeDataConnection();
            return exists;
        }

        private bool doesStoreExist(string storeNumber)
        {
            if (storeNumber == null || storeNumber.Equals(""))
            {
                return false;
            }

            bool exists = false;

            openDataConnection();

            SqlCommand cmdCheck = new SqlCommand("SELECT * FROM Store WHERE StoreNumber = '" + storeNumber + "'", theConnection);

            theReader = cmdCheck.ExecuteReader();

            if (theReader.HasRows)
            {
                exists = true;
            }

            theReader.Close();

            closeDataConnection();

            return exists;
        }

        public ResponseProviderList GetAllProviders()
        {
            ResponseProviderList theResponse = new ResponseProviderList();

            openDataConnection();

            SqlCommand getAll = new SqlCommand("GetAllProviders", theConnection);
            getAll.CommandType = System.Data.CommandType.StoredProcedure;

            theReader = getAll.ExecuteReader();

            if (theReader.HasRows)
            {
                theResponse.providers = new List<Provider>();

                while (theReader.Read())
                {
                    Provider thisProvider = new Provider();

                    thisProvider.providerID = (int)theReader["ProviderID"];
                    thisProvider.providerName = theReader["ProviderName"].ToString();

                    theResponse.providers.Add(thisProvider);
                }

                theResponse.statusCode = 0;
                theResponse.statusDescription = "";
            }
            else
            {
                theResponse.statusCode = 2;
                theResponse.statusDescription = "There are no Providers in the database";
            }

            theReader.Close();

            closeDataConnection();

            return theResponse;
        }

        public ResponseProviderWithCDCList GetAllProvidersWithCDCs()
        {
            ResponseProviderWithCDCList theResponse = new ResponseProviderWithCDCList();

            openDataConnection();

            SqlCommand getAll = new SqlCommand("GetAllProvidersWithCDCs", theConnection);
            getAll.CommandType = System.Data.CommandType.StoredProcedure;

            theReader = getAll.ExecuteReader();

            if (theReader.HasRows)
            {
                int currentProviderID = 0;
                ProviderWithCDC thisProvider = null;

                theResponse.providers = new List<ProviderWithCDC>();

                while (theReader.Read())
                {
                    int thisProviderID = (int)theReader["ProviderID"];

                    if (currentProviderID != thisProviderID)
                    {
                        thisProvider = new ProviderWithCDC();

                        thisProvider.providerID = (int)theReader["ProviderID"];
                        thisProvider.providerName = theReader["ProviderName"].ToString();

                        theResponse.providers.Add(thisProvider);

                        thisProvider.cdcs = new List<CDC>();

                        currentProviderID = thisProviderID;
                    }

                    CDC thisCDC = new CDC();

                    thisCDC.id = (int)theReader["CDCID"];
                    thisCDC.name = theReader["CDCName"].ToString();
                    thisCDC.address = theReader["CDCAddress"].ToString();
                    thisCDC.state = theReader["CDCState"].ToString();
                    thisCDC.zip = theReader["CDCZip"].ToString();
                    thisCDC.phone = theReader["CDCPhone"].ToString();
                    thisCDC.email = theReader["CDCEmailAddress"].ToString();
                    thisCDC.providerID = thisProvider.providerID;

                    thisProvider.cdcs.Add(thisCDC);
                }

                theResponse.statusCode = 0;
                theResponse.statusDescription = "";
            }
            else
            {
                theResponse.statusCode = 2;
                theResponse.statusDescription = "There are no Providers in the database";
            }

            theReader.Close();

            closeDataConnection();

            return theResponse;
        }

        private string getProviderNameFromID(int providerID)
        {
            string providerName = null;

            openDataConnection();

            SqlCommand cmdGetName = new SqlCommand("GetProviderByID", theConnection);
            cmdGetName.Parameters.AddWithValue("@providerID", providerID);
            cmdGetName.CommandType = System.Data.CommandType.StoredProcedure;

            theReader = cmdGetName.ExecuteReader();

            if (theReader.HasRows)
            {
                while (theReader.Read())
                {
                    providerName = theReader["ProviderName"].ToString();
                }
            }

            theReader.Close();

            closeDataConnection();

            return providerName;
        }

        public ResponseProviderWithCDCList GetCDCsForProvider(string providerID)
        {
            ResponseProviderWithCDCList theResponse = new ResponseProviderWithCDCList();

            string providerName = getProviderNameFromID(Int32.Parse(providerID));

            if (providerName == null)
            {
                theResponse.statusCode = 2;
                theResponse.statusDescription = "There is no provider with ID " + providerID;

                return theResponse;
            }

            openDataConnection();

            SqlCommand getCDCs = new SqlCommand("GetCDCsForProvider", theConnection);
            getCDCs.Parameters.AddWithValue("@providerID", providerID);
            getCDCs.CommandType = System.Data.CommandType.StoredProcedure;

            theReader = getCDCs.ExecuteReader();

            if (theReader.HasRows)
            {
                theResponse.providers = new List<ProviderWithCDC>();

                ProviderWithCDC thisProvider = new ProviderWithCDC();
                thisProvider.providerName = providerName;
                thisProvider.providerID = Int32.Parse(providerID);
                thisProvider.cdcs = new List<CDC>();

                theResponse.providers.Add(thisProvider);

                while (theReader.Read())
                {
                    CDC thisCDC = new CDC();

                    thisCDC.id = (int)theReader["CDCID"];
                    thisCDC.name = theReader["CDCName"].ToString();
                    thisCDC.address = theReader["CDCAddress"].ToString();
                    thisCDC.state = theReader["CDCState"].ToString();
                    thisCDC.zip = theReader["CDCZip"].ToString();
                    thisCDC.phone = theReader["CDCPhone"].ToString();
                    thisCDC.email = theReader["CDCEmailAddress"].ToString();
                    thisCDC.providerID = thisProvider.providerID;

                    thisProvider.cdcs.Add(thisCDC);
                }

                theResponse.statusCode = 0;
                theResponse.statusDescription = "";
            }
            else
            {
                theResponse.statusCode = 2;
                theResponse.statusDescription = "There are no Providers in the database";
            }

            theReader.Close();

            closeDataConnection();

            return theResponse;
        }

        public Response CreateProvider(Provider aProviderModel)
        {
            Response theResponse = new Response();

            if (aProviderModel == null)
            {
                theResponse.statusCode = 2;
                theResponse.statusDescription = "Provider Model missing";
            }
            else
            {
                if (aProviderModel.providerName == null || aProviderModel.providerName.Equals(""))
                {
                    theResponse.statusDescription = "Provider Name is missing";
                }

                if (theResponse.statusDescription.Equals(""))
                {
                    bool doesProviderExist = providerExists(aProviderModel.providerName);

                    if (doesProviderExist)
                    {
                        theResponse.statusCode = 3;
                        theResponse.statusDescription = "The provider " + aProviderModel.providerName + " already exists";

                        return theResponse;
                    }

                    openDataConnection();

                    SqlCommand addProvider = new SqlCommand("CreateProvider", theConnection);
                    addProvider.Parameters.AddWithValue("@providerName", aProviderModel.providerName);
                    addProvider.CommandType = System.Data.CommandType.StoredProcedure;

                    int numRowsAffected = 0;

                    try
                    {
                        numRowsAffected = addProvider.ExecuteNonQuery();
                    }
                    catch (Exception _exception)
                    {
                        theResponse.statusCode = 6;
                        theResponse.statusDescription = _exception.Message;
                    }

                    if (numRowsAffected > 0)
                    {
                        theResponse.statusCode = 0;
                        theResponse.statusDescription = "";
                    }
                    else
                    {
                        theResponse.statusCode = 6;
                        theResponse.statusDescription = "The provider " + aProviderModel.providerName + " could not be created";
                    }

                    closeDataConnection();
                }
                else
                {
                    theResponse.statusCode = 2;
                    theResponse.statusDescription = "Provider model missing";
                }
            }

            return theResponse;
        }

        public Response UpdateProvider(Provider aProviderModel)
        {
            Response theResponse = new Response();

            if (aProviderModel == null)
            {
                theResponse.statusCode = 2;
                theResponse.statusDescription = "Provider Model missing";
            }
            else
            {
                if (aProviderModel.providerID <= 0)
                {
                    theResponse.statusDescription = "Provider ID is missing";
                }
                if (aProviderModel.providerName == null || aProviderModel.providerName.Equals(""))
                {
                    theResponse.statusDescription = "Provider Name is missing";
                }

                if (theResponse.statusDescription.Equals(""))
                {
                    openDataConnection();

                    SqlCommand updateProvider = new SqlCommand("UpdateProvider", theConnection);
                    updateProvider.Parameters.AddWithValue("@providerID", aProviderModel.providerID);
                    updateProvider.Parameters.AddWithValue("@providerName", aProviderModel.providerName);
                    updateProvider.CommandType = System.Data.CommandType.StoredProcedure;

                    int numRowsAffected = 0;

                    try
                    {
                        numRowsAffected = updateProvider.ExecuteNonQuery();
                    }
                    catch (Exception _exception)
                    {
                        theResponse.statusCode = 6;
                        theResponse.statusDescription = _exception.Message;
                    }

                    if (numRowsAffected > 0)
                    {
                        theResponse.statusCode = 0;
                        theResponse.statusDescription = "";
                    }
                    else
                    {
                        theResponse.statusCode = 6;
                        theResponse.statusDescription = "The provider " + aProviderModel.providerName + " could not be updated";
                    }

                    closeDataConnection();
                }
                else
                {
                    theResponse.statusCode = 2;
                    theResponse.statusDescription = "Provider model missing";
                }
            }

            return theResponse;
        }

        public ResponsePhotoList GetPhotosForStore(string aStoreID)
        {
            ResponsePhotoList theResponse = new ResponsePhotoList();

            if (aStoreID == null || aStoreID.Equals(""))
            {
                theResponse.statusCode = 2;
                theResponse.statusDescription = "Missing Store ID";
            }
            else
            {
                openDataConnection();

                SqlCommand cmdGet = new SqlCommand("GetPhotosForStoreID", theConnection);
                cmdGet.Parameters.AddWithValue("@storeID", aStoreID);
                cmdGet.CommandType = System.Data.CommandType.StoredProcedure;

                theReader = cmdGet.ExecuteReader();

                if (theReader.HasRows)
                {
                    theResponse.photos = new List<PhotoWithStore>();

                    while (theReader.Read())
                    {
                        PhotoWithStore thisPhoto = new PhotoWithStore();

                        thisPhoto.stopID = (int)theReader["StopID"];
                        thisPhoto.storeID = (int)theReader["StoreID"];
                        thisPhoto.imageData = theReader["Photo"].ToString();
                        if (theReader["DateUpdated"] != DBNull.Value)
                        {
                            thisPhoto.dateUpdated = (DateTime)theReader["DateUpdated"];
                            thisPhoto.dateUpdatedString = thisPhoto.dateUpdated.ToLongDateString() + " " + thisPhoto.dateUpdated.ToLongTimeString();

                            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);
                            TimeSpan span = (thisPhoto.dateUpdated - epoch);
                            double unixTime = span.TotalSeconds;

                            thisPhoto.dateUpdatedEpoch = (int)unixTime;
                        }

                        DateTime todaysDate = DateTime.Today;
                        TimeSpan difference = todaysDate - thisPhoto.dateUpdated;

                        if (difference.Days <= 21)
                        {
                            theResponse.photos.Add(thisPhoto);
                        }
                    }

                    theResponse.statusCode = 0;
                    theResponse.statusDescription = "";
                }
                else
                {
                    theResponse.statusCode = 4;
                    theResponse.statusDescription = "There are no photos related to this store";
                }

                closeDataConnection();
            }

            return theResponse;
        }

        private bool providerExists(string providerName)
        {
            bool exists = false;

            if (providerName == null || providerName.Equals(""))
            {
                return exists;
            }

            openDataConnection();

            SqlCommand cmdCheck = new SqlCommand("ProviderExists", theConnection);
            cmdCheck.Parameters.AddWithValue("@providerName", providerName);
            cmdCheck.CommandType = System.Data.CommandType.StoredProcedure;

            try
            {
                theReader = cmdCheck.ExecuteReader();

                if (theReader.HasRows)
                {
                    exists = true;
                }

                theReader.Close();
            }
            catch (Exception _exception)
            {
                exists = false;
            }

            closeDataConnection();

            return exists;
        }

        private bool cdcExists(string cdcName)
        {
            bool exists = false;

            if (cdcName == null || cdcName.Equals(""))
            {
                return exists;
            }

            openDataConnection();

            SqlCommand cmdCheck = new SqlCommand("CDCExists", theConnection);
            cmdCheck.Parameters.AddWithValue("@cdcName", cdcName);
            cmdCheck.CommandType = System.Data.CommandType.StoredProcedure;

            try
            {
                theReader = cmdCheck.ExecuteReader();

                if (theReader.HasRows)
                {
                    exists = true;
                }

                theReader.Close();
            }
            catch (Exception _exception)
            {
                exists = false;
            }

            closeDataConnection();

            return exists;
        }

        private bool parentReasonExists(string reasonName)
        {
            bool exists = false;

            if (reasonName == null || reasonName.Equals(""))
            {
                return exists;
            }

            openDataConnection();

            SqlCommand cmdCheck = new SqlCommand("SELECT ReasonID FROM Reason WHERE ReasonName = '" + reasonName + "'", theConnection);

            try
            {
                theReader = cmdCheck.ExecuteReader();

                if (theReader.HasRows)
                {
                    exists = true;
                }

                theReader.Close();
            }
            catch (Exception _exception)
            {
                exists = false;
            }

            closeDataConnection();

            return exists;
        }

        private bool childReasonExists(string reasonName)
        {
            bool exists = false;

            if (reasonName == null || reasonName.Equals(""))
            {
                return exists;
            }

            openDataConnection();

            SqlCommand cmdCheck = new SqlCommand("SELECT ChildReasonID FROM ChildReason WHERE ChildReasonName = '" + reasonName + "'", theConnection);

            try
            {
                theReader = cmdCheck.ExecuteReader();

                if (theReader.HasRows)
                {
                    exists = true;
                }

                theReader.Close();
            }
            catch (Exception _exception)
            {
                exists = false;
            }

            closeDataConnection();

            return exists;
        }

        private string CopyReportTemplate(string reportType)
        {
            string currentPath = HttpContext.Current.Server.MapPath("~");
            //string currentPath = HttpRuntime.AppDomainAppPath;
            long currentTime = DateTime.Now.ToFileTimeUtc();
            string sourceFilename = reportType + ".xlsx";
            string targetFilename = "report_" + reportType + "_" + currentTime + ".xlsx";
            string sourcePath = currentPath + "\\templates\\";
            string targetPath = currentPath + "\\downloads\\";

            string sourceFile = System.IO.Path.Combine(sourcePath, sourceFilename);
            string destFile = System.IO.Path.Combine(targetPath, targetFilename);

            System.IO.File.Copy(sourceFile, destFile, true);

            return destFile;
        }

        private string CopyReportTemplateTemp(string reportType)
        {
            string currentPath = HttpContext.Current.Server.MapPath("~");
            long currentTime = DateTime.Now.ToFileTimeUtc();
            string sourceFilename = reportType + ".xlsx";
            string targetFilename = "report_" + reportType + "_" + currentTime + ".xlsx";
            string sourcePath = currentPath + "\\templates\\";
            string targetPath = currentPath + "\\downloads\\";

            string sourceFile = System.IO.Path.Combine(sourcePath, sourceFilename);
            string destFile = System.IO.Path.Combine(targetPath, targetFilename);

            System.IO.File.Copy(sourceFile, destFile, true);
            string fullpath = System.IO.Path.GetFullPath(destFile);
            return destFile;
        }

        public Response ReportStoreReadinessForSSC()
        {
            //Response theResponse = new Response();
            string newFilePath = "";
            Response theResponse = new Response();

            openDataConnection();

            /* Summary Data */

            SqlCommand cmdDVPRVPDeliveriesWithoutIssues = new SqlCommand("ReportNumberOfDeliveriesWithoutIssuesForSSC", theConnection);
            cmdDVPRVPDeliveriesWithoutIssues.CommandType = System.Data.CommandType.StoredProcedure;
            cmdDVPRVPDeliveriesWithoutIssues.CommandTimeout = 1200;

            theReader = cmdDVPRVPDeliveriesWithoutIssues.ExecuteReader();

            List<DVPRVPSummary> dvprvpData = new List<DVPRVPSummary>();

            if (theReader.HasRows)
            {
                while (theReader.Read())
                {
                    DVPRVPSummary thisRow = new DVPRVPSummary();

                    thisRow.dvpName = theReader["ProviderName"].ToString();
                    thisRow.rvpName = theReader["CDCName"].ToString();
                    thisRow.deliveries = (int)theReader["NumberOfDeliveriesWithoutIssues"];

                    dvprvpData.Add(thisRow);
                }
            }

            theReader.Close();

            SqlCommand cmdDVPRVPDeliveriesWithIssues = new SqlCommand("ReportNumberOfDeliveriesWithIssuesForSSC", theConnection);
            cmdDVPRVPDeliveriesWithIssues.CommandType = System.Data.CommandType.StoredProcedure;
            cmdDVPRVPDeliveriesWithIssues.CommandTimeout = 1200;

            theReader = cmdDVPRVPDeliveriesWithIssues.ExecuteReader();

            if (theReader.HasRows)
            {
                while (theReader.Read())
                {
                    string thisRowsDVP = theReader["ProviderName"].ToString();
                    string thisRowsRVP = theReader["CDCName"].ToString();

                    bool found = false;

                    for (int i = 0, l = dvprvpData.Count; i < l; i++)
                    {
                        DVPRVPSummary thisRowData = dvprvpData[i];

                        if (thisRowsDVP.Equals(thisRowData.dvpName) && thisRowsRVP.Equals(thisRowData.rvpName))
                        {
                            found = true;

                            int deliveriesWithIssues = (int)theReader["NumberOfDeliveriesWithIssues"];

                            thisRowData.deliveriesWithIssues = deliveriesWithIssues;
                            thisRowData.deliveries += thisRowData.deliveriesWithIssues;
                        }
                    }

                    if (!found)
                    {
                        DVPRVPSummary thisRow = new DVPRVPSummary();

                        thisRow.dvpName = thisRowsDVP;
                        thisRow.rvpName = thisRowsRVP;
                        thisRow.deliveriesWithIssues = (int)theReader["NumberOfDeliveriesWithIssues"];
                        thisRow.deliveries += thisRow.deliveriesWithIssues;

                        dvprvpData.Add(thisRow);

                        found = false;
                    }
                }
            }

            theReader.Close();

            SqlCommand cmdDVPRVPNumberOfIssues = new SqlCommand("ReportNumberOfIssuesForSSC", theConnection);
            cmdDVPRVPNumberOfIssues.CommandType = System.Data.CommandType.StoredProcedure;
            cmdDVPRVPNumberOfIssues.CommandTimeout = 1200;

            theReader = cmdDVPRVPNumberOfIssues.ExecuteReader();

            if (theReader.HasRows)
            {
                while (theReader.Read())
                {
                    string thisRowsDVP = theReader["ProviderName"].ToString();
                    string thisRowsRVP = theReader["CDCName"].ToString();

                    bool found = false;

                    for (int i = 0, l = dvprvpData.Count; i < l; i++)
                    {
                        DVPRVPSummary thisRowData = dvprvpData[i];

                        if (thisRowsDVP.Equals(thisRowData.dvpName) && thisRowsRVP.Equals(thisRowData.rvpName))
                        {
                            found = true;

                            int numberOfIssues = (int)theReader["NumberOfIssues"];

                            thisRowData.totalReadinessIssues = numberOfIssues;
                        }
                    }

                    if (!found)
                    {
                        DVPRVPSummary thisRow = new DVPRVPSummary();

                        thisRow.dvpName = thisRowsDVP;
                        thisRow.rvpName = thisRowsRVP;
                        thisRow.totalReadinessIssues = (int)theReader["NumberOfIssues"];

                        dvprvpData.Add(thisRow);

                        found = false;
                    }
                }
            }

            theReader.Close();

            SqlCommand cmdUnitsLeftout = new SqlCommand("ReportUnitsLeftoutForSSC", theConnection);
            cmdUnitsLeftout.CommandType = System.Data.CommandType.StoredProcedure;
            cmdUnitsLeftout.CommandTimeout = 1200;

            theReader = cmdUnitsLeftout.ExecuteReader();

            if (theReader.HasRows)
            {
                while (theReader.Read())
                {
                    string thisRowsDVP = theReader["ProviderName"].ToString();
                    string thisRowsRVP = theReader["CDCName"].ToString();

                    bool found = false;

                    for (int i = 0, l = dvprvpData.Count; i < l; i++)
                    {
                        DVPRVPSummary thisRowData = dvprvpData[i];

                        if (thisRowsDVP.Equals(thisRowData.dvpName) && thisRowsRVP.Equals(thisRowData.rvpName))
                        {
                            found = true;

                            int unitsLeftout = (int)theReader["UnitsLeftout"];

                            thisRowData.leftoutUnits = unitsLeftout;
                        }
                    }

                    if (!found)
                    {
                        DVPRVPSummary thisRow = new DVPRVPSummary();

                        thisRow.dvpName = thisRowsDVP;
                        thisRow.rvpName = thisRowsRVP;
                        thisRow.leftoutUnits = (int)theReader["UnitsLeftout"];

                        dvprvpData.Add(thisRow);

                        found = false;
                    }
                }
            }

            theReader.Close();

            SqlCommand cmdDVPRVPUnitsBackhauled = new SqlCommand("ReportUnitsBackhauledForSSC", theConnection);
            cmdDVPRVPUnitsBackhauled.CommandType = System.Data.CommandType.StoredProcedure;
            cmdDVPRVPUnitsBackhauled.CommandTimeout = 1200;

            theReader = cmdDVPRVPUnitsBackhauled.ExecuteReader();

            if (theReader.HasRows)
            {
                while (theReader.Read())
                {
                    string thisRowsDVP = theReader["ProviderName"].ToString();
                    string thisRowsRVP = theReader["CDCName"].ToString();

                    bool found = false;

                    for (int i = 0, l = dvprvpData.Count; i < l; i++)
                    {
                        DVPRVPSummary thisRowData = dvprvpData[i];

                        if (thisRowsDVP.Equals(thisRowData.dvpName) && thisRowsRVP.Equals(thisRowData.rvpName))
                        {
                            found = true;

                            int unitsBackhauled = (int)theReader["UnitsBackhauled"];

                            thisRowData.dairyBackhaulUnits = unitsBackhauled;
                        }
                    }

                    if (!found)
                    {
                        DVPRVPSummary thisRow = new DVPRVPSummary();

                        thisRow.dvpName = thisRowsDVP;
                        thisRow.rvpName = thisRowsRVP;
                        thisRow.dairyBackhaulUnits = (int)theReader["UnitsBackhauled"];

                        dvprvpData.Add(thisRow);

                        found = false;
                    }
                }
            }

            theReader.Close();

            SqlCommand cmdDVPRVPUnitsBackhauledCost = new SqlCommand("ReportUnitsBackhauledCostForSSC", theConnection);
            cmdDVPRVPUnitsBackhauledCost.CommandType = System.Data.CommandType.StoredProcedure;
            cmdDVPRVPUnitsBackhauledCost.CommandTimeout = 1200;

            theReader = cmdDVPRVPUnitsBackhauledCost.ExecuteReader();

            if (theReader.HasRows)
            {
                while (theReader.Read())
                {
                    string thisRowsDVP = theReader["ProviderName"].ToString();
                    string thisRowsRVP = theReader["CDCName"].ToString();

                    bool found = false;

                    for (int i = 0, l = dvprvpData.Count; i < l; i++)
                    {
                        DVPRVPSummary thisRowData = dvprvpData[i];

                        if (thisRowsDVP.Equals(thisRowData.dvpName) && thisRowsRVP.Equals(thisRowData.rvpName))
                        {
                            found = true;

                            if (theReader["BackhaulCost"] != DBNull.Value)
                            {
                                double backhaulCost = (double)theReader["BackhaulCost"];

                                thisRowData.dairyBackhaulCOGS = backhaulCost;
                            }
                        }
                    }

                    if (!found)
                    {
                        DVPRVPSummary thisRow = new DVPRVPSummary();

                        thisRow.dvpName = thisRowsDVP;
                        thisRow.rvpName = thisRowsRVP;
                        thisRow.dairyBackhaulCOGS = (double)theReader["BackhaulCost"];

                        dvprvpData.Add(thisRow);

                        found = false;
                    }
                }
            }

            theReader.Close();

            closeDataConnection();

            try
            {
                newFilePath = CopyReportTemplate("ssc");
                // Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + strUploadFileName + ";Extended Properties='Excel 12.0 Xml;HDR=YES;IMEX=1'";
                /* string oleConnectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + newFilePath + ";Extended Properties=\"Excel 12.0;HDR=Yes;ReadOnly=False\"";
                 OleDbConnection oleConnection = new OleDbConnection(oleConnectionString);
                 oleConnection.Open();*/

                openDataConnection();
                SqlCommand cmdReport = new SqlCommand("ReportStoresNotReady", theConnection);
                cmdReport.CommandType = System.Data.CommandType.StoredProcedure;
                cmdReport.CommandTimeout = 1200;

                theReader = cmdReport.ExecuteReader();


                FileStream template = new FileStream(newFilePath, FileMode.Open, FileAccess.Read);
                //FileStream fileRead = new FileStream(destFile, FileMode.Create, FileAccess.ReadWrite);
                //IWorkbook theWorkbook = new XSSFWorkbook(fileRead);
                //Create a stream of .xlsx file contained within my project using reflection


                //EPPlusTest = Namespace/Project
                //templates = folder
                //VendorTemplate.xlsx = file

                //ExcelPackage has a constructor that only requires a stream.
                ExcelPackage pck = new OfficeOpenXml.ExcelPackage(template);
                ExcelWorkbook workBook = pck.Workbook;
                //fileRead.Close();
                template.Close();

                if (theReader.HasRows)
                {
                    int rowIndex = 2;
                    //ISheet notReadySheet = theWorkbook.GetSheet("DetailedViewNotReady");
                    ExcelWorksheet notReadySheet = workBook.Worksheets["DetailedViewNotReady"];
                    //notReadySheet.ForceFormulaRecalculation = true;
                    string photoVal = "";
                    //IRow crow = null;

                    while (theReader.Read())
                    {
                        rowIndex++;
                        //crow = notReadySheet.CreateRow(rowIndex);
                        DateTime todaysDateTime = DateTime.Today;
                        DateTime thisEntrysDateTime = Convert.ToDateTime(theReader[0].ToString());
                        TimeSpan timeDifference = todaysDateTime - thisEntrysDateTime;
                        int differenceInDays = timeDifference.Days;
                        photoVal = "";
                        notReadySheet.Cells[rowIndex, 1].Value = theReader[0].ToString();
                        notReadySheet.Cells[rowIndex, 2].Value = theReader[1].ToString();
                        notReadySheet.Cells[rowIndex, 3].Value = theReader[2].ToString();
                        notReadySheet.Cells[rowIndex, 4].Value = theReader[3].ToString();
                        notReadySheet.Cells[rowIndex, 5].Value = theReader[4].ToString();
                        notReadySheet.Cells[rowIndex, 6].Value = theReader[9].ToString();
                        notReadySheet.Cells[rowIndex, 7].Value = theReader[10].ToString();
                        notReadySheet.Cells[rowIndex, 8].Value = theReader[11].ToString();
                        notReadySheet.Cells[rowIndex, 9].Value = getCellFriendlyText(theReader[12].ToString());
                        if (differenceInDays <= 21 && theReader[13].ToString() != null && !theReader[13].ToString().Equals(""))
                        {
                            photoVal += ",'" + baseURLForPhotoLink + theReader[13].ToString() + "'";
                        }
                        else
                        {
                            photoVal += ",'No Photos Available'";
                        }
                        notReadySheet.Cells[rowIndex, 10].Value = photoVal;

                    }
                    theResponse.statusCode = 0;
                    theResponse.statusDescription = extractFilename(newFilePath);
                }
                else
                {
                    theResponse.statusCode = 1;
                    theResponse.statusDescription = "There is no data logged between the dates that were selected";
                }
                theReader.Close();



                SqlCommand cmdReportNotReady = new SqlCommand("ReportStoresReady", theConnection);
                cmdReportNotReady.CommandType = System.Data.CommandType.StoredProcedure;
                cmdReportNotReady.CommandTimeout = 1200;

                theReader = cmdReportNotReady.ExecuteReader();
                if (theReader.HasRows)
                {
                    int rowIndex = 2;
                    ExcelWorksheet readySheet = workBook.Worksheets["DetailedViewReady"];

                    while (theReader.Read())
                    {
                        rowIndex++;

                        readySheet.Cells[rowIndex, 1].Value = theReader[0].ToString();
                        readySheet.Cells[rowIndex, 2].Value = theReader[1].ToString();
                        readySheet.Cells[rowIndex, 3].Value = theReader[2].ToString();
                        readySheet.Cells[rowIndex, 4].Value = theReader[3].ToString();
                        readySheet.Cells[rowIndex, 5].Value = theReader[4].ToString();

                    }
                    theResponse.statusCode = 0;
                    theResponse.statusDescription = extractFilename(newFilePath);
                }
                else
                {
                    theResponse.statusCode = 1;
                    theResponse.statusDescription = "There is no data logged between the dates that were selected";
                }
                theReader.Close();
                /*if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        //DateTime todaysDateTime = DateTime.Today;
                        //DateTime thisEntrysDateTime = Convert.ToDateTime(theReader[0].ToString());
                       // TimeSpan timeDifference = todaysDateTime - thisEntrysDateTime;
                       // int differenceInDays = timeDifference.Days;

                        string values = "'" + theReader[0] + "','" + theReader[1] + "','" + theReader[2] + "','" + theReader[3] + "','" + theReader[4] + "'";

                     

                        OleDbCommand oleCommand = new OleDbCommand();
                        string sqlCommand = "INSERT INTO 	[DetailedViewReady$] (InStoreDate, StoreNumber, Provider, CDC, Route) VALUES (" + values + ")";
                        oleCommand.CommandText = sqlCommand;
                        oleCommand.Connection = oleConnection;
                        oleCommand.ExecuteNonQuery();
                    }

                    theResponse.statusCode = 0;
                    theResponse.statusDescription = newFilePath;
                }*/

                //theReader.Close();

                //oleConnection.Close();



                ExcelWorksheet sheetSummary = workBook.Worksheets["Summary"];

                sheetSummary.Cells["A3:C3"].Merge = true;
                sheetSummary.Cells[3, 1].Value = "All Data";

                if (dvprvpData != null && dvprvpData.Count > 0)
                {
                    int currentRowIndex = 4;

                    for (int i = 0, l = dvprvpData.Count; i < l; i++)
                    {
                        DVPRVPSummary thisRowsData = dvprvpData[i];

                        thisRowsData.percentageStoresReady = Math.Round((100 - ((double)thisRowsData.deliveriesWithIssues / (double)thisRowsData.deliveries) * 100.0), 2);

                        sheetSummary.Cells[currentRowIndex, 1].Value = thisRowsData.dvpName;
                        sheetSummary.Cells[currentRowIndex, 2].Value = thisRowsData.rvpName;
                        sheetSummary.Cells[currentRowIndex, 3].Value = thisRowsData.percentageStoresReady.ToString() + " %";
                        sheetSummary.Cells[currentRowIndex, 4].Value = thisRowsData.leftoutUnits;
                        sheetSummary.Cells[currentRowIndex, 5].Value = thisRowsData.dairyBackhaulUnits;
                        sheetSummary.Cells[currentRowIndex, 6].Value = thisRowsData.deliveries;
                        sheetSummary.Cells[currentRowIndex, 7].Value = thisRowsData.deliveriesWithIssues;
                        sheetSummary.Cells[currentRowIndex, 8].Value = thisRowsData.totalReadinessIssues;

                        currentRowIndex++;
                    }

                    theResponse.statusCode = 0;
                    theResponse.statusDescription = extractFilename(newFilePath);
                }
                else
                {
                    theResponse.statusCode = 1;
                    theResponse.statusDescription = "There is no data logged between the dates that were selected";
                }



                FileStream fileSave = new FileStream(newFilePath, FileMode.Create);
                //theWorkbook.Write(fileSave);
                pck.SaveAs(fileSave);
                fileSave.Close();

                closeDataConnection();
            }
            catch (Exception _exception)
            {
                theResponse.statusCode = 6;
                theResponse.statusDescription = _exception.Message + " ::" + newFilePath + "::" + _exception.StackTrace;
            }

            return theResponse;
        }

        public Response ReportStoreReadinessForCDC()
        {

            string newFilePath = "";
            Response theResponse = new Response();

            openDataConnection();

            /* Summary Data */

            SqlCommand cmdDVPRVPDeliveriesWithoutIssues = new SqlCommand("ReportNumberOfDeliveriesWithoutIssuesForSSC", theConnection);
            cmdDVPRVPDeliveriesWithoutIssues.CommandType = System.Data.CommandType.StoredProcedure;
            cmdDVPRVPDeliveriesWithoutIssues.CommandTimeout = 1200;

            theReader = cmdDVPRVPDeliveriesWithoutIssues.ExecuteReader();

            List<DVPRVPSummary> dvprvpData = new List<DVPRVPSummary>();

            if (theReader.HasRows)
            {
                while (theReader.Read())
                {
                    DVPRVPSummary thisRow = new DVPRVPSummary();

                    thisRow.dvpName = theReader["ProviderName"].ToString();
                    thisRow.rvpName = theReader["CDCName"].ToString();
                    thisRow.deliveries = (int)theReader["NumberOfDeliveriesWithoutIssues"];

                    dvprvpData.Add(thisRow);
                }
            }

            theReader.Close();

            SqlCommand cmdDVPRVPDeliveriesWithIssues = new SqlCommand("ReportNumberOfDeliveriesWithIssuesForSSC", theConnection);
            cmdDVPRVPDeliveriesWithIssues.CommandType = System.Data.CommandType.StoredProcedure;
            cmdDVPRVPDeliveriesWithIssues.CommandTimeout = 1200;

            theReader = cmdDVPRVPDeliveriesWithIssues.ExecuteReader();

            if (theReader.HasRows)
            {
                while (theReader.Read())
                {
                    string thisRowsDVP = theReader["ProviderName"].ToString();
                    string thisRowsRVP = theReader["CDCName"].ToString();

                    bool found = false;

                    for (int i = 0, l = dvprvpData.Count; i < l; i++)
                    {
                        DVPRVPSummary thisRowData = dvprvpData[i];

                        if (thisRowsDVP.Equals(thisRowData.dvpName) && thisRowsRVP.Equals(thisRowData.rvpName))
                        {
                            found = true;

                            int deliveriesWithIssues = (int)theReader["NumberOfDeliveriesWithIssues"];

                            thisRowData.deliveriesWithIssues = deliveriesWithIssues;
                            thisRowData.deliveries += thisRowData.deliveriesWithIssues;
                        }
                    }

                    if (!found)
                    {
                        DVPRVPSummary thisRow = new DVPRVPSummary();

                        thisRow.dvpName = thisRowsDVP;
                        thisRow.rvpName = thisRowsRVP;
                        thisRow.deliveriesWithIssues = (int)theReader["NumberOfDeliveriesWithIssues"];
                        thisRow.deliveries += thisRow.deliveriesWithIssues;

                        dvprvpData.Add(thisRow);

                        found = false;
                    }
                }
            }

            theReader.Close();

            SqlCommand cmdDVPRVPNumberOfIssues = new SqlCommand("ReportNumberOfIssuesForSSC", theConnection);
            cmdDVPRVPNumberOfIssues.CommandType = System.Data.CommandType.StoredProcedure;
            cmdDVPRVPNumberOfIssues.CommandTimeout = 1200;

            theReader = cmdDVPRVPNumberOfIssues.ExecuteReader();

            if (theReader.HasRows)
            {
                while (theReader.Read())
                {
                    string thisRowsDVP = theReader["ProviderName"].ToString();
                    string thisRowsRVP = theReader["CDCName"].ToString();

                    bool found = false;

                    for (int i = 0, l = dvprvpData.Count; i < l; i++)
                    {
                        DVPRVPSummary thisRowData = dvprvpData[i];

                        if (thisRowsDVP.Equals(thisRowData.dvpName) && thisRowsRVP.Equals(thisRowData.rvpName))
                        {
                            found = true;

                            int numberOfIssues = (int)theReader["NumberOfIssues"];

                            thisRowData.totalReadinessIssues = numberOfIssues;
                        }
                    }

                    if (!found)
                    {
                        DVPRVPSummary thisRow = new DVPRVPSummary();

                        thisRow.dvpName = thisRowsDVP;
                        thisRow.rvpName = thisRowsRVP;
                        thisRow.totalReadinessIssues = (int)theReader["NumberOfIssues"];

                        dvprvpData.Add(thisRow);

                        found = false;
                    }
                }
            }

            theReader.Close();

            SqlCommand cmdUnitsLeftout = new SqlCommand("ReportUnitsLeftoutForSSC", theConnection);
            cmdUnitsLeftout.CommandType = System.Data.CommandType.StoredProcedure;
            cmdUnitsLeftout.CommandTimeout = 1200;

            theReader = cmdUnitsLeftout.ExecuteReader();

            if (theReader.HasRows)
            {
                while (theReader.Read())
                {
                    string thisRowsDVP = theReader["ProviderName"].ToString();
                    string thisRowsRVP = theReader["CDCName"].ToString();

                    bool found = false;

                    for (int i = 0, l = dvprvpData.Count; i < l; i++)
                    {
                        DVPRVPSummary thisRowData = dvprvpData[i];

                        if (thisRowsDVP.Equals(thisRowData.dvpName) && thisRowsRVP.Equals(thisRowData.rvpName))
                        {
                            found = true;

                            int unitsLeftout = (int)theReader["UnitsLeftout"];

                            thisRowData.leftoutUnits = unitsLeftout;
                        }
                    }

                    if (!found)
                    {
                        DVPRVPSummary thisRow = new DVPRVPSummary();

                        thisRow.dvpName = thisRowsDVP;
                        thisRow.rvpName = thisRowsRVP;
                        thisRow.leftoutUnits = (int)theReader["UnitsLeftout"];

                        dvprvpData.Add(thisRow);

                        found = false;
                    }
                }
            }

            theReader.Close();

            SqlCommand cmdDVPRVPUnitsBackhauled = new SqlCommand("ReportUnitsBackhauledForSSC", theConnection);
            cmdDVPRVPUnitsBackhauled.CommandType = System.Data.CommandType.StoredProcedure;
            cmdDVPRVPUnitsBackhauled.CommandTimeout = 1200;

            theReader = cmdDVPRVPUnitsBackhauled.ExecuteReader();

            if (theReader.HasRows)
            {
                while (theReader.Read())
                {
                    string thisRowsDVP = theReader["ProviderName"].ToString();
                    string thisRowsRVP = theReader["CDCName"].ToString();

                    bool found = false;

                    for (int i = 0, l = dvprvpData.Count; i < l; i++)
                    {
                        DVPRVPSummary thisRowData = dvprvpData[i];

                        if (thisRowsDVP.Equals(thisRowData.dvpName) && thisRowsRVP.Equals(thisRowData.rvpName))
                        {
                            found = true;

                            int unitsBackhauled = (int)theReader["UnitsBackhauled"];

                            thisRowData.dairyBackhaulUnits = unitsBackhauled;
                        }
                    }

                    if (!found)
                    {
                        DVPRVPSummary thisRow = new DVPRVPSummary();

                        thisRow.dvpName = thisRowsDVP;
                        thisRow.rvpName = thisRowsRVP;
                        thisRow.dairyBackhaulUnits = (int)theReader["UnitsBackhauled"];

                        dvprvpData.Add(thisRow);

                        found = false;
                    }
                }
            }

            theReader.Close();

            SqlCommand cmdDVPRVPUnitsBackhauledCost = new SqlCommand("ReportUnitsBackhauledCostForSSC", theConnection);
            cmdDVPRVPUnitsBackhauledCost.CommandType = System.Data.CommandType.StoredProcedure;
            cmdDVPRVPUnitsBackhauledCost.CommandTimeout = 1200;

            theReader = cmdDVPRVPUnitsBackhauledCost.ExecuteReader();

            if (theReader.HasRows)
            {
                while (theReader.Read())
                {
                    string thisRowsDVP = theReader["ProviderName"].ToString();
                    string thisRowsRVP = theReader["CDCName"].ToString();

                    bool found = false;

                    for (int i = 0, l = dvprvpData.Count; i < l; i++)
                    {
                        DVPRVPSummary thisRowData = dvprvpData[i];

                        if (thisRowsDVP.Equals(thisRowData.dvpName) && thisRowsRVP.Equals(thisRowData.rvpName))
                        {
                            found = true;

                            if (theReader["BackhaulCost"] != DBNull.Value)
                            {
                                double backhaulCost = (double)theReader["BackhaulCost"];

                                thisRowData.dairyBackhaulCOGS = backhaulCost;
                            }
                        }
                    }

                    if (!found)
                    {
                        DVPRVPSummary thisRow = new DVPRVPSummary();

                        thisRow.dvpName = thisRowsDVP;
                        thisRow.rvpName = thisRowsRVP;
                        thisRow.dairyBackhaulCOGS = (double)theReader["BackhaulCost"];

                        dvprvpData.Add(thisRow);

                        found = false;
                    }
                }
            }

            theReader.Close();

            closeDataConnection();

            try
            {
                newFilePath = CopyReportTemplate("cdc");

                /*string oleConnectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + newFilePath + ";Extended Properties=\"Excel 8.0;HDR=Yes;ReadOnly=False\"";
                OleDbConnection oleConnection = new OleDbConnection(oleConnectionString);
                oleConnection.Open();*/

                openDataConnection();
                SqlCommand cmdReport = new SqlCommand("ReportStoresNotReady", theConnection);
                cmdReport.CommandType = System.Data.CommandType.StoredProcedure;
                cmdReport.CommandTimeout = 1200;

                theReader = cmdReport.ExecuteReader();

                FileStream template = new FileStream(newFilePath, FileMode.Open, FileAccess.Read);
                //FileStream fileRead = new FileStream(destFile, FileMode.Create, FileAccess.ReadWrite);
                //IWorkbook theWorkbook = new XSSFWorkbook(fileRead);
                //Create a stream of .xlsx file contained within my project using reflection


                //EPPlusTest = Namespace/Project
                //templates = folder
                //VendorTemplate.xlsx = file

                //ExcelPackage has a constructor that only requires a stream.
                ExcelPackage pck = new OfficeOpenXml.ExcelPackage(template);
                ExcelWorkbook workBook = pck.Workbook;
                //fileRead.Close();
                template.Close();

                if (theReader.HasRows)
                {
                    int rowIndex = 2;
                    //ISheet notReadySheet = theWorkbook.GetSheet("DetailedViewNotReady");
                    ExcelWorksheet notReadySheet = workBook.Worksheets["DetailedViewNotReady"];
                    //notReadySheet.ForceFormulaRecalculation = true;
                    string photoVal = "";
                    //IRow crow = null;

                    while (theReader.Read())
                    {
                        rowIndex++;
                        //crow = notReadySheet.CreateRow(rowIndex);
                        DateTime todaysDateTime = DateTime.Today;
                        DateTime thisEntrysDateTime = Convert.ToDateTime(theReader[0].ToString());
                        TimeSpan timeDifference = todaysDateTime - thisEntrysDateTime;
                        int differenceInDays = timeDifference.Days;
                        photoVal = "";
                        notReadySheet.Cells[rowIndex, 1].Value = theReader[0].ToString();
                        notReadySheet.Cells[rowIndex, 2].Value = theReader[1].ToString();
                        notReadySheet.Cells[rowIndex, 3].Value = theReader[2].ToString();
                        notReadySheet.Cells[rowIndex, 4].Value = theReader[3].ToString();
                        notReadySheet.Cells[rowIndex, 5].Value = theReader[4].ToString();
                        notReadySheet.Cells[rowIndex, 6].Value = theReader[9].ToString();
                        notReadySheet.Cells[rowIndex, 7].Value = theReader[10].ToString();
                        notReadySheet.Cells[rowIndex, 8].Value = theReader[11].ToString();
                        notReadySheet.Cells[rowIndex, 9].Value = getCellFriendlyText(theReader[12].ToString());
                        if (differenceInDays <= 21 && theReader[13].ToString() != null && !theReader[13].ToString().Equals(""))
                        {
                            photoVal += ",'" + baseURLForPhotoLink + theReader[13].ToString() + "'";
                        }
                        else
                        {
                            photoVal += ",'No Photos Available'";
                        }
                        notReadySheet.Cells[rowIndex, 10].Value = photoVal;

                    }
                    theResponse.statusCode = 0;
                    theResponse.statusDescription = extractFilename(newFilePath);
                }
                else
                {
                    theResponse.statusCode = 1;
                    theResponse.statusDescription = "There is no data logged between the dates that were selected";
                }
                theReader.Close();



                SqlCommand cmdReportNotReady = new SqlCommand("ReportStoresReady", theConnection);
                cmdReportNotReady.CommandType = System.Data.CommandType.StoredProcedure;
                cmdReportNotReady.CommandTimeout = 1200;

                theReader = cmdReportNotReady.ExecuteReader();
                if (theReader.HasRows)
                {
                    int rowIndex = 2;
                    ExcelWorksheet readySheet = workBook.Worksheets["DetailedViewReady"];

                    while (theReader.Read())
                    {
                        rowIndex++;

                        readySheet.Cells[rowIndex, 1].Value = theReader[0].ToString();
                        readySheet.Cells[rowIndex, 2].Value = theReader[1].ToString();
                        readySheet.Cells[rowIndex, 3].Value = theReader[2].ToString();
                        readySheet.Cells[rowIndex, 4].Value = theReader[3].ToString();
                        readySheet.Cells[rowIndex, 5].Value = theReader[4].ToString();

                    }
                    theResponse.statusCode = 0;
                    theResponse.statusDescription = extractFilename(newFilePath);
                }
                else
                {
                    theResponse.statusCode = 1;
                    theResponse.statusDescription = "There is no data logged between the dates that were selected";
                }
                theReader.Close();

                /*if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        DateTime todaysDateTime = DateTime.Today;
                        DateTime thisEntrysDateTime = Convert.ToDateTime(theReader[0].ToString());
                        TimeSpan timeDifference = todaysDateTime - thisEntrysDateTime;
                        int differenceInDays = timeDifference.Days;

                        string values = "'" + theReader[0] + "','" + theReader[1] + "','" + theReader[2] + "','" + theReader[3] + "','" + theReader[4] + "','" + theReader[9] + "','" + theReader[10] + "','" + theReader[11] + "','" + getCellFriendlyText(theReader[12].ToString()) + "'";

                        if (differenceInDays <= 21 && theReader[13].ToString() != null && !theReader[13].ToString().Equals(""))
                        {
                            values += ",'" + baseURLForPhotoLink + theReader[13].ToString() + "'";
                        }
                        else
                        {
                            values += ",'No Photos Available'";
                        }

                        OleDbCommand oleCommand = new OleDbCommand();
                        string sqlCommand = "INSERT INTO 	[DetailedViewNotReady$] (InStoreDate, StoreNumber, Provider, CDC, Route, ReasonCode, Photos, UnitsBackhauled, Comments, PhotoLink) VALUES (" + values + ")";
                        oleCommand.CommandText = sqlCommand;
                        oleCommand.Connection = oleConnection;
                        oleCommand.ExecuteNonQuery();
                    }

                    theResponse.statusCode = 0;
                    theResponse.statusDescription = newFilePath;
                }

                theReader.Close();

                SqlCommand cmdReportNotReady = new SqlCommand("ReportStoresReady", theConnection);
                cmdReportNotReady.CommandType = System.Data.CommandType.StoredProcedure;

                theReader = cmdReportNotReady.ExecuteReader();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        //DateTime todaysDateTime = DateTime.Today;
                        //DateTime thisEntrysDateTime = Convert.ToDateTime(theReader[0].ToString());
                        //TimeSpan timeDifference = todaysDateTime - thisEntrysDateTime;
                        //int differenceInDays = timeDifference.Days;

                        string values = "'" + theReader[0] + "','" + theReader[1] + "','" + theReader[2] + "','" + theReader[3] + "','" + theReader[4] + "'";



                        OleDbCommand oleCommand = new OleDbCommand();
                        string sqlCommand = "INSERT INTO 	[DetailedViewReady$] (InStoreDate, StoreNumber, Provider, CDC, Route) VALUES (" + values + ")";
                        oleCommand.CommandText = sqlCommand;
                        oleCommand.Connection = oleConnection;
                        oleCommand.ExecuteNonQuery();
                    }

                    theResponse.statusCode = 0;
                    theResponse.statusDescription = newFilePath;
                }

                theReader.Close();

                oleConnection.Close();*/

                ExcelWorksheet sheetSummary = workBook.Worksheets["Summary"];

                sheetSummary.Cells["A3:C3"].Merge = true;
                sheetSummary.Cells[3, 1].Value = "All Data";

                if (dvprvpData != null && dvprvpData.Count > 0)
                {
                    int currentRowIndex = 4;

                    for (int i = 0, l = dvprvpData.Count; i < l; i++)
                    {
                        DVPRVPSummary thisRowsData = dvprvpData[i];

                        thisRowsData.percentageStoresReady = Math.Round((100 - ((double)thisRowsData.deliveriesWithIssues / (double)thisRowsData.deliveries) * 100.0), 2);

                        sheetSummary.Cells[currentRowIndex, 1].Value = thisRowsData.dvpName;
                        sheetSummary.Cells[currentRowIndex, 2].Value = thisRowsData.rvpName;
                        sheetSummary.Cells[currentRowIndex, 3].Value = thisRowsData.percentageStoresReady.ToString() + " %";
                        sheetSummary.Cells[currentRowIndex, 4].Value = thisRowsData.leftoutUnits;
                        sheetSummary.Cells[currentRowIndex, 5].Value = thisRowsData.dairyBackhaulUnits;
                        sheetSummary.Cells[currentRowIndex, 6].Value = thisRowsData.deliveries;
                        sheetSummary.Cells[currentRowIndex, 7].Value = thisRowsData.deliveriesWithIssues;
                        sheetSummary.Cells[currentRowIndex, 8].Value = thisRowsData.totalReadinessIssues;

                        currentRowIndex++;
                    }

                    theResponse.statusCode = 0;
                    theResponse.statusDescription = extractFilename(newFilePath);
                }
                else
                {
                    theResponse.statusCode = 1;
                    theResponse.statusDescription = "There is no data logged between the dates that were selected";
                }



                FileStream fileSave = new FileStream(newFilePath, FileMode.Create);
                //theWorkbook.Write(fileSave);
                pck.SaveAs(fileSave);
                fileSave.Close();

                closeDataConnection();
            }
            catch (Exception _exception)
            {
                theResponse.statusCode = 6;
                theResponse.statusDescription = _exception.Message + " " + _exception.StackTrace;
            }

            return theResponse;
        }


        public Response ReportStoreReadinessForCDCForProvider(string providerID)
        {
            Response theResponse = new Response();

            openDataConnection();

            /* Summary Data */

            SqlCommand cmdDVPRVPDeliveriesWithoutIssues = new SqlCommand("ReportNumberOfDeliveriesWithoutIssuesForCDCWithProviderID", theConnection);
            cmdDVPRVPDeliveriesWithoutIssues.Parameters.AddWithValue("@providerID", Int32.Parse(providerID));
            cmdDVPRVPDeliveriesWithoutIssues.CommandType = System.Data.CommandType.StoredProcedure;
            cmdDVPRVPDeliveriesWithoutIssues.CommandTimeout = 1200;

            theReader = cmdDVPRVPDeliveriesWithoutIssues.ExecuteReader();

            List<DVPRVPSummary> dvprvpData = new List<DVPRVPSummary>();

            if (theReader.HasRows)
            {
                while (theReader.Read())
                {
                    DVPRVPSummary thisRow = new DVPRVPSummary();

                    thisRow.dvpName = theReader["ProviderName"].ToString();
                    thisRow.rvpName = theReader["CDCName"].ToString();
                    thisRow.deliveries = (int)theReader["NumberOfDeliveriesWithoutIssues"];

                    dvprvpData.Add(thisRow);
                }
            }

            theReader.Close();

            SqlCommand cmdDVPRVPDeliveriesWithIssues = new SqlCommand("ReportNumberOfDeliveriesWithIssuesForCDCWithProviderID", theConnection);
            cmdDVPRVPDeliveriesWithIssues.Parameters.AddWithValue("@providerID", Int32.Parse(providerID));
            cmdDVPRVPDeliveriesWithIssues.CommandType = System.Data.CommandType.StoredProcedure;
            cmdDVPRVPDeliveriesWithIssues.CommandTimeout = 1200;

            theReader = cmdDVPRVPDeliveriesWithIssues.ExecuteReader();

            if (theReader.HasRows)
            {
                while (theReader.Read())
                {
                    string thisRowsDVP = theReader["ProviderName"].ToString();
                    string thisRowsRVP = theReader["CDCName"].ToString();

                    bool found = false;

                    for (int i = 0, l = dvprvpData.Count; i < l; i++)
                    {
                        DVPRVPSummary thisRowData = dvprvpData[i];

                        if (thisRowsDVP.Equals(thisRowData.dvpName) && thisRowsRVP.Equals(thisRowData.rvpName))
                        {
                            found = true;

                            int deliveriesWithIssues = (int)theReader["NumberOfDeliveriesWithIssues"];

                            thisRowData.deliveriesWithIssues = deliveriesWithIssues;
                            thisRowData.deliveries += thisRowData.deliveriesWithIssues;
                        }
                    }

                    if (!found)
                    {
                        DVPRVPSummary thisRow = new DVPRVPSummary();

                        thisRow.dvpName = thisRowsDVP;
                        thisRow.rvpName = thisRowsRVP;
                        thisRow.deliveriesWithIssues = (int)theReader["NumberOfDeliveriesWithIssues"];
                        thisRow.deliveries += thisRow.deliveriesWithIssues;

                        dvprvpData.Add(thisRow);

                        found = false;
                    }
                }
            }

            theReader.Close();

            SqlCommand cmdDVPRVPNumberOfIssues = new SqlCommand("ReportNumberOfIssuesForCDCWithProviderID", theConnection);
            cmdDVPRVPNumberOfIssues.Parameters.AddWithValue("@providerID", Int32.Parse(providerID));
            cmdDVPRVPNumberOfIssues.CommandType = System.Data.CommandType.StoredProcedure;
            cmdDVPRVPNumberOfIssues.CommandTimeout = 1200;

            theReader = cmdDVPRVPNumberOfIssues.ExecuteReader();

            if (theReader.HasRows)
            {
                while (theReader.Read())
                {
                    string thisRowsDVP = theReader["ProviderName"].ToString();
                    string thisRowsRVP = theReader["CDCName"].ToString();

                    bool found = false;

                    for (int i = 0, l = dvprvpData.Count; i < l; i++)
                    {
                        DVPRVPSummary thisRowData = dvprvpData[i];

                        if (thisRowsDVP.Equals(thisRowData.dvpName) && thisRowsRVP.Equals(thisRowData.rvpName))
                        {
                            found = true;

                            int numberOfIssues = (int)theReader["NumberOfIssues"];

                            thisRowData.totalReadinessIssues = numberOfIssues;
                        }
                    }

                    if (!found)
                    {
                        DVPRVPSummary thisRow = new DVPRVPSummary();

                        thisRow.dvpName = thisRowsDVP;
                        thisRow.rvpName = thisRowsRVP;
                        thisRow.totalReadinessIssues = (int)theReader["NumberOfIssues"];

                        dvprvpData.Add(thisRow);

                        found = false;
                    }
                }
            }

            theReader.Close();

            SqlCommand cmdUnitsLeftout = new SqlCommand("ReportUnitsLeftoutForCDCWithProviderID", theConnection);
            cmdUnitsLeftout.Parameters.AddWithValue("@providerID", Int32.Parse(providerID));
            cmdUnitsLeftout.CommandType = System.Data.CommandType.StoredProcedure;
            cmdUnitsLeftout.CommandTimeout = 1200;

            theReader = cmdUnitsLeftout.ExecuteReader();

            if (theReader.HasRows)
            {
                while (theReader.Read())
                {
                    string thisRowsDVP = theReader["ProviderName"].ToString();
                    string thisRowsRVP = theReader["CDCName"].ToString();


                    bool found = false;

                    for (int i = 0, l = dvprvpData.Count; i < l; i++)
                    {
                        DVPRVPSummary thisRowData = dvprvpData[i];

                        if (thisRowsDVP.Equals(thisRowData.dvpName) && thisRowsRVP.Equals(thisRowData.rvpName))
                        {
                            found = true;

                            int unitsLeftout = (int)theReader["UnitsLeftout"];

                            thisRowData.leftoutUnits = unitsLeftout;
                        }
                    }

                    if (!found)
                    {
                        DVPRVPSummary thisRow = new DVPRVPSummary();

                        thisRow.dvpName = thisRowsDVP;
                        thisRow.rvpName = thisRowsRVP;
                        thisRow.leftoutUnits = (int)theReader["UnitsLeftout"];

                        dvprvpData.Add(thisRow);

                        found = false;
                    }
                }
            }

            theReader.Close();

            SqlCommand cmdDVPRVPUnitsBackhauled = new SqlCommand("ReportUnitsBackhauledForCDCWithProviderID", theConnection);
            cmdDVPRVPUnitsBackhauled.Parameters.AddWithValue("@providerID", Int32.Parse(providerID));
            cmdDVPRVPUnitsBackhauled.CommandType = System.Data.CommandType.StoredProcedure;
            cmdDVPRVPUnitsBackhauled.CommandTimeout = 1200;

            theReader = cmdDVPRVPUnitsBackhauled.ExecuteReader();

            if (theReader.HasRows)
            {
                while (theReader.Read())
                {
                    string thisRowsDVP = theReader["ProviderName"].ToString();
                    string thisRowsRVP = theReader["CDCName"].ToString();


                    bool found = false;

                    for (int i = 0, l = dvprvpData.Count; i < l; i++)
                    {
                        DVPRVPSummary thisRowData = dvprvpData[i];

                        if (thisRowsDVP.Equals(thisRowData.dvpName) && thisRowsRVP.Equals(thisRowData.rvpName))
                        {
                            found = true;

                            int unitsBackhauled = (int)theReader["UnitsBackhauled"];

                            thisRowData.dairyBackhaulUnits = unitsBackhauled;
                        }
                    }

                    if (!found)
                    {
                        DVPRVPSummary thisRow = new DVPRVPSummary();

                        thisRow.dvpName = thisRowsDVP;
                        thisRow.rvpName = thisRowsRVP;
                        thisRow.dairyBackhaulUnits = (int)theReader["UnitsBackhauled"];

                        dvprvpData.Add(thisRow);

                        found = false;
                    }
                }
            }

            theReader.Close();

            SqlCommand cmdDVPRVPUnitsBackhauledCost = new SqlCommand("ReportUnitsBackhauledCostForCDCWithProviderID", theConnection);
            cmdDVPRVPUnitsBackhauledCost.Parameters.AddWithValue("@providerID", Int32.Parse(providerID));
            cmdDVPRVPUnitsBackhauledCost.CommandType = System.Data.CommandType.StoredProcedure;
            cmdDVPRVPUnitsBackhauledCost.CommandTimeout = 1200;

            theReader = cmdDVPRVPUnitsBackhauledCost.ExecuteReader();

            if (theReader.HasRows)
            {
                while (theReader.Read())
                {
                    string thisRowsDVP = theReader["ProviderName"].ToString();
                    string thisRowsRVP = theReader["CDCName"].ToString();

                    bool found = false;

                    for (int i = 0, l = dvprvpData.Count; i < l; i++)
                    {
                        DVPRVPSummary thisRowData = dvprvpData[i];

                        if (thisRowsDVP.Equals(thisRowData.dvpName) && thisRowsRVP.Equals(thisRowData.rvpName))
                        {
                            found = true;

                            if (theReader["BackhaulCost"] != DBNull.Value)
                            {
                                double backhaulCost = (double)theReader["BackhaulCost"];

                                thisRowData.dairyBackhaulCOGS = backhaulCost;
                            }
                        }
                    }

                    if (!found)
                    {
                        DVPRVPSummary thisRow = new DVPRVPSummary();

                        thisRow.dvpName = thisRowsDVP;
                        thisRow.rvpName = thisRowsRVP;
                        thisRow.dairyBackhaulCOGS = (double)theReader["BackhaulCost"];

                        dvprvpData.Add(thisRow);

                        found = false;
                    }
                }
            }

            theReader.Close();

            closeDataConnection();

            try
            {
                int wantedProviderID = Int32.Parse(providerID);

                string wantedProviderName = getProviderNameFromID(wantedProviderID);

                string newFilePath = CopyReportTemplate("cdc");


                /*string oleConnectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + newFilePath + ";Extended Properties=\"Excel 8.0;\"";
                OleDbConnection oleConnection = new OleDbConnection(oleConnectionString);
                oleConnection.Open();*/

                openDataConnection();
                SqlCommand cmdReport = new SqlCommand("ReportStoresNotReadyWithProviderID", theConnection);
                cmdReport.Parameters.AddWithValue("@providerID", Int32.Parse(providerID));
                cmdReport.CommandType = System.Data.CommandType.StoredProcedure;
                cmdReport.CommandTimeout = 1200;

                theReader = cmdReport.ExecuteReader();

                FileStream template = new FileStream(newFilePath, FileMode.Open, FileAccess.Read);
                //FileStream fileRead = new FileStream(destFile, FileMode.Create, FileAccess.ReadWrite);
                //IWorkbook theWorkbook = new XSSFWorkbook(fileRead);
                //Create a stream of .xlsx file contained within my project using reflection


                //EPPlusTest = Namespace/Project
                //templates = folder
                //VendorTemplate.xlsx = file

                //ExcelPackage has a constructor that only requires a stream.
                ExcelPackage pck = new OfficeOpenXml.ExcelPackage(template);
                ExcelWorkbook workBook = pck.Workbook;
                //fileRead.Close();
                template.Close();

                if (theReader.HasRows)
                {
                    int rowIndex = 2;
                    //ISheet notReadySheet = theWorkbook.GetSheet("DetailedViewNotReady");
                    ExcelWorksheet notReadySheet = workBook.Worksheets["DetailedViewNotReady"];
                    //notReadySheet.ForceFormulaRecalculation = true;
                    string photoVal = "";
                    //IRow crow = null;

                    while (theReader.Read())
                    {
                        rowIndex++;
                        //crow = notReadySheet.CreateRow(rowIndex);
                        DateTime todaysDateTime = DateTime.Today;
                        DateTime thisEntrysDateTime = Convert.ToDateTime(theReader[0].ToString());
                        TimeSpan timeDifference = todaysDateTime - thisEntrysDateTime;
                        int differenceInDays = timeDifference.Days;
                        photoVal = "";
                        notReadySheet.Cells[rowIndex, 1].Value = theReader[0].ToString();
                        notReadySheet.Cells[rowIndex, 2].Value = theReader[1].ToString();
                        notReadySheet.Cells[rowIndex, 3].Value = theReader[2].ToString();
                        notReadySheet.Cells[rowIndex, 4].Value = theReader[3].ToString();
                        notReadySheet.Cells[rowIndex, 5].Value = theReader[4].ToString();
                        notReadySheet.Cells[rowIndex, 6].Value = theReader[9].ToString();
                        notReadySheet.Cells[rowIndex, 7].Value = theReader[10].ToString();
                        notReadySheet.Cells[rowIndex, 8].Value = theReader[11].ToString();
                        notReadySheet.Cells[rowIndex, 9].Value = getCellFriendlyText(theReader[12].ToString());
                        if (differenceInDays <= 21 && theReader[13].ToString() != null && !theReader[13].ToString().Equals(""))
                        {
                            photoVal += ",'" + baseURLForPhotoLink + theReader[13].ToString() + "'";
                        }
                        else
                        {
                            photoVal += ",'No Photos Available'";
                        }
                        notReadySheet.Cells[rowIndex, 10].Value = photoVal;

                    }
                    theResponse.statusCode = 0;
                    theResponse.statusDescription = extractFilename(newFilePath);
                }
                else
                {
                    theResponse.statusCode = 1;
                    theResponse.statusDescription = "There is no data logged between the dates that were selected";
                }
                theReader.Close();



                SqlCommand cmdReportNotReady = new SqlCommand("ReportStoresReadyWithProviderID", theConnection);
                cmdReportNotReady.Parameters.AddWithValue("@providerID", Int32.Parse(providerID));
                cmdReportNotReady.CommandType = System.Data.CommandType.StoredProcedure;
                cmdReportNotReady.CommandTimeout = 1200;

                theReader = cmdReportNotReady.ExecuteReader();
                if (theReader.HasRows)
                {
                    int rowIndex = 2;
                    ExcelWorksheet readySheet = workBook.Worksheets["DetailedViewReady"];

                    while (theReader.Read())
                    {
                        rowIndex++;

                        readySheet.Cells[rowIndex, 1].Value = theReader[0].ToString();
                        readySheet.Cells[rowIndex, 2].Value = theReader[1].ToString();
                        readySheet.Cells[rowIndex, 3].Value = theReader[2].ToString();
                        readySheet.Cells[rowIndex, 4].Value = theReader[3].ToString();
                        readySheet.Cells[rowIndex, 5].Value = theReader[4].ToString();

                    }
                    theResponse.statusCode = 0;
                    theResponse.statusDescription = extractFilename(newFilePath);
                }
                else
                {
                    theResponse.statusCode = 1;
                    theResponse.statusDescription = "There is no data logged between the dates that were selected";
                }
                theReader.Close();

                /*if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        DateTime todaysDateTime = DateTime.Today;
                        DateTime thisEntrysDateTime = Convert.ToDateTime(theReader[0].ToString());
                        TimeSpan timeDifference = todaysDateTime - thisEntrysDateTime;
                        int differenceInDays = timeDifference.Days;

                        string values = "'" + theReader[0] + "','" + theReader[1] + "','" + theReader[2] + "','" + theReader[3] + "','" + theReader[4] + "','" + theReader[9] + "','" + theReader[10] + "','" + theReader[11] + "','" + getCellFriendlyText(theReader[12].ToString()) + "'";

                        if (differenceInDays <= 21 && theReader[13].ToString() != null && !theReader[13].ToString().Equals(""))
                        {
                            values += ",'" + baseURLForPhotoLink + theReader[13].ToString() + "'";
                        }
                        else
                        {
                            values += ",'No Photos Available'";
                        }

                        OleDbCommand oleCommand = new OleDbCommand();
                        string sqlCommand = "INSERT INTO [DetailedViewNotReady$] (InStoreDate, StoreNumber, Provider, CDC, Route, ReasonCode, Photos, UnitsBackhauled, Comments, PhotoLink) VALUES (" + values + ")";
                        oleCommand.CommandText = sqlCommand;
                        oleCommand.Connection = oleConnection;
                        oleCommand.ExecuteNonQuery();
                    }

                    theResponse.statusCode = 0;
                    theResponse.statusDescription = newFilePath;
                }

                theReader.Close();

                SqlCommand cmdReportNotReady = new SqlCommand("ReportStoresReadyWithProviderID", theConnection);
                cmdReportNotReady.Parameters.AddWithValue("@providerID", Int32.Parse(providerID));
                cmdReportNotReady.CommandType = System.Data.CommandType.StoredProcedure;

                theReader = cmdReportNotReady.ExecuteReader();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                       /* DateTime todaysDateTime = DateTime.Today;
                        DateTime thisEntrysDateTime = Convert.ToDateTime(theReader[0].ToString());
                        TimeSpan timeDifference = todaysDateTime - thisEntrysDateTime;
                        int differenceInDays = timeDifference.Days;

                        string values = "'" + theReader[0] + "','" + theReader[1] + "','" + theReader[2] + "','" + theReader[3] + "','" + theReader[4] + "'";



                        OleDbCommand oleCommand = new OleDbCommand();
                        string sqlCommand = "INSERT INTO 	[DetailedViewReady$] (InStoreDate, StoreNumber, Provider, CDC, Route) VALUES (" + values + ")";
                        oleCommand.CommandText = sqlCommand;
                        oleCommand.Connection = oleConnection;
                        oleCommand.ExecuteNonQuery();
                    }

                    theResponse.statusCode = 0;
                    theResponse.statusDescription = newFilePath;
                }

                theReader.Close();

                oleConnection.Close();*/



                ExcelWorksheet sheetSummary = workBook.Worksheets["Summary"];

                sheetSummary.Cells["A3:C3"].Merge = true;
                sheetSummary.Cells[3, 1].Value = "All Data";

                if (dvprvpData != null && dvprvpData.Count > 0)
                {
                    int currentRowIndex = 4;

                    for (int i = 0, l = dvprvpData.Count; i < l; i++)
                    {
                        DVPRVPSummary thisRowsData = dvprvpData[i];

                        thisRowsData.percentageStoresReady = Math.Round((100 - ((double)thisRowsData.deliveriesWithIssues / (double)thisRowsData.deliveries) * 100.0), 2);

                        sheetSummary.Cells[currentRowIndex, 1].Value = thisRowsData.dvpName;
                        sheetSummary.Cells[currentRowIndex, 2].Value = thisRowsData.rvpName;
                        sheetSummary.Cells[currentRowIndex, 3].Value = thisRowsData.percentageStoresReady.ToString() + " %";
                        sheetSummary.Cells[currentRowIndex, 4].Value = thisRowsData.leftoutUnits;
                        sheetSummary.Cells[currentRowIndex, 5].Value = thisRowsData.dairyBackhaulUnits;
                        sheetSummary.Cells[currentRowIndex, 6].Value = thisRowsData.deliveries;
                        sheetSummary.Cells[currentRowIndex, 7].Value = thisRowsData.deliveriesWithIssues;
                        sheetSummary.Cells[currentRowIndex, 8].Value = thisRowsData.totalReadinessIssues;

                        currentRowIndex++;
                    }

                    theResponse.statusCode = 0;
                    theResponse.statusDescription = extractFilename(newFilePath);
                }
                else
                {
                    theResponse.statusCode = 1;
                    theResponse.statusDescription = "There is no data logged between the dates that were selected";
                }



                FileStream fileSave = new FileStream(newFilePath, FileMode.Create);
                //theWorkbook.Write(fileSave);
                pck.SaveAs(fileSave);
                fileSave.Close();

                closeDataConnection();
            }
            catch (Exception _exception)
            {
                theResponse.statusCode = 6;
                theResponse.statusDescription = _exception.Message + " " + _exception.StackTrace;
            }

            return theResponse;
        }


        public Response ReportStoreReadinessForCDCForProviderWithInterval(string providerID, string startDate, string endDate)
        {
            if (validateDate(startDate) && validateDate(endDate))
            {
                if (endDate.Length <= 10)
                {
                    endDate += " 23:59:59";
                }

                Response theResponse = new Response();

                openDataConnection();

                /* Summary Data */

                SqlCommand cmdDVPRVPDeliveriesWithoutIssues = new SqlCommand("ReportNumberOfDeliveriesWithoutIssuesWithIntervalForCDCWithProviderID", theConnection);
                cmdDVPRVPDeliveriesWithoutIssues.Parameters.AddWithValue("@dateStarted", startDate);
                cmdDVPRVPDeliveriesWithoutIssues.Parameters.AddWithValue("@dateEnded", endDate);
                cmdDVPRVPDeliveriesWithoutIssues.Parameters.AddWithValue("@providerID", Int32.Parse(providerID));
                cmdDVPRVPDeliveriesWithoutIssues.CommandType = System.Data.CommandType.StoredProcedure;
                cmdDVPRVPDeliveriesWithoutIssues.CommandTimeout = 1200;

                theReader = cmdDVPRVPDeliveriesWithoutIssues.ExecuteReader();

                List<DVPRVPSummary> dvprvpData = new List<DVPRVPSummary>();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        DVPRVPSummary thisRow = new DVPRVPSummary();

                        thisRow.dvpName = theReader["ProviderName"].ToString();
                        thisRow.rvpName = theReader["CDCName"].ToString();
                        thisRow.deliveries = (int)theReader["NumberOfDeliveriesWithoutIssues"];

                        dvprvpData.Add(thisRow);
                    }
                }

                theReader.Close();

                SqlCommand cmdDVPRVPDeliveriesWithIssues = new SqlCommand("ReportNumberOfDeliveriesWithIssuesWithIntervalForCDCWithProviderID", theConnection);
                cmdDVPRVPDeliveriesWithIssues.Parameters.AddWithValue("@dateStarted", startDate);
                cmdDVPRVPDeliveriesWithIssues.Parameters.AddWithValue("@dateEnded", endDate);
                cmdDVPRVPDeliveriesWithIssues.Parameters.AddWithValue("@providerID", Int32.Parse(providerID));
                cmdDVPRVPDeliveriesWithIssues.CommandType = System.Data.CommandType.StoredProcedure;
                cmdDVPRVPDeliveriesWithIssues.CommandTimeout = 1200;

                theReader = cmdDVPRVPDeliveriesWithIssues.ExecuteReader();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        string thisRowsDVP = theReader["ProviderName"].ToString();
                        string thisRowsRVP = theReader["CDCName"].ToString();

                        bool found = false;

                        for (int i = 0, l = dvprvpData.Count; i < l; i++)
                        {
                            DVPRVPSummary thisRowData = dvprvpData[i];

                            if (thisRowsDVP.Equals(thisRowData.dvpName) && thisRowsRVP.Equals(thisRowData.rvpName))
                            {
                                found = true;

                                int deliveriesWithIssues = (int)theReader["NumberOfDeliveriesWithIssues"];

                                thisRowData.deliveriesWithIssues = deliveriesWithIssues;
                                thisRowData.deliveries += thisRowData.deliveriesWithIssues;
                            }
                        }

                        if (!found)
                        {
                            DVPRVPSummary thisRow = new DVPRVPSummary();

                            thisRow.dvpName = thisRowsDVP;
                            thisRow.rvpName = thisRowsRVP;
                            thisRow.deliveriesWithIssues = (int)theReader["NumberOfDeliveriesWithIssues"];
                            thisRow.deliveries += thisRow.deliveriesWithIssues;

                            dvprvpData.Add(thisRow);

                            found = false;
                        }
                    }
                }

                theReader.Close();

                SqlCommand cmdDVPRVPNumberOfIssues = new SqlCommand("ReportNumberOfIssuesWithIntervalForCDCWithProviderID", theConnection);
                cmdDVPRVPNumberOfIssues.Parameters.AddWithValue("@dateStarted", startDate);
                cmdDVPRVPNumberOfIssues.Parameters.AddWithValue("@dateEnded", endDate);
                cmdDVPRVPNumberOfIssues.Parameters.AddWithValue("@providerID", Int32.Parse(providerID));
                cmdDVPRVPNumberOfIssues.CommandType = System.Data.CommandType.StoredProcedure;
                cmdDVPRVPNumberOfIssues.CommandTimeout = 1200;

                theReader = cmdDVPRVPNumberOfIssues.ExecuteReader();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        string thisRowsDVP = theReader["ProviderName"].ToString();
                        string thisRowsRVP = theReader["CDCName"].ToString();

                        bool found = false;

                        for (int i = 0, l = dvprvpData.Count; i < l; i++)
                        {
                            DVPRVPSummary thisRowData = dvprvpData[i];

                            if (thisRowsDVP.Equals(thisRowData.dvpName) && thisRowsRVP.Equals(thisRowData.rvpName))
                            {
                                found = true;

                                int numberOfIssues = (int)theReader["NumberOfIssues"];

                                thisRowData.totalReadinessIssues = numberOfIssues;
                            }
                        }

                        if (!found)
                        {
                            DVPRVPSummary thisRow = new DVPRVPSummary();

                            thisRow.dvpName = thisRowsDVP;
                            thisRow.rvpName = thisRowsRVP;
                            thisRow.totalReadinessIssues = (int)theReader["NumberOfIssues"];

                            dvprvpData.Add(thisRow);

                            found = false;
                        }
                    }
                }

                theReader.Close();

                SqlCommand cmdUnitsLeftout = new SqlCommand("ReportUnitsLeftoutWithIntervalForCDCWithProviderID", theConnection);
                cmdUnitsLeftout.Parameters.AddWithValue("@dateStarted", startDate);
                cmdUnitsLeftout.Parameters.AddWithValue("@dateEnded", endDate);
                cmdUnitsLeftout.Parameters.AddWithValue("@providerID", Int32.Parse(providerID));
                cmdUnitsLeftout.CommandType = System.Data.CommandType.StoredProcedure;
                cmdUnitsLeftout.CommandTimeout = 1200;

                theReader = cmdUnitsLeftout.ExecuteReader();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        string thisRowsDVP = theReader["ProviderName"].ToString();
                        string thisRowsRVP = theReader["CDCName"].ToString();

                        bool found = false;

                        for (int i = 0, l = dvprvpData.Count; i < l; i++)
                        {
                            DVPRVPSummary thisRowData = dvprvpData[i];

                            if (thisRowsDVP.Equals(thisRowData.dvpName) && thisRowsRVP.Equals(thisRowData.rvpName))
                            {
                                found = true;

                                int unitsLeftout = (int)theReader["UnitsLeftout"];

                                thisRowData.leftoutUnits = unitsLeftout;
                            }
                        }

                        if (!found)
                        {
                            DVPRVPSummary thisRow = new DVPRVPSummary();

                            thisRow.dvpName = thisRowsDVP;
                            thisRow.rvpName = thisRowsRVP;
                            thisRow.leftoutUnits = (int)theReader["UnitsLeftout"];

                            dvprvpData.Add(thisRow);

                            found = false;
                        }
                    }
                }

                theReader.Close();

                SqlCommand cmdDVPRVPUnitsBackhauled = new SqlCommand("ReportUnitsBackhauledWithIntervalForCDCWithProviderID", theConnection);
                cmdDVPRVPUnitsBackhauled.Parameters.AddWithValue("@dateStarted", startDate);
                cmdDVPRVPUnitsBackhauled.Parameters.AddWithValue("@dateEnded", endDate);
                cmdDVPRVPUnitsBackhauled.Parameters.AddWithValue("@providerID", Int32.Parse(providerID));
                cmdDVPRVPUnitsBackhauled.CommandType = System.Data.CommandType.StoredProcedure;
                cmdDVPRVPUnitsBackhauled.CommandTimeout = 1200;

                theReader = cmdDVPRVPUnitsBackhauled.ExecuteReader();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        string thisRowsDVP = theReader["ProviderName"].ToString();
                        string thisRowsRVP = theReader["CDCName"].ToString();

                        bool found = false;

                        for (int i = 0, l = dvprvpData.Count; i < l; i++)
                        {
                            DVPRVPSummary thisRowData = dvprvpData[i];

                            if (thisRowsDVP.Equals(thisRowData.dvpName) && thisRowsRVP.Equals(thisRowData.rvpName))
                            {
                                found = true;

                                int unitsBackhauled = (int)theReader["UnitsBackhauled"];

                                thisRowData.dairyBackhaulUnits = unitsBackhauled;
                            }
                        }

                        if (!found)
                        {
                            DVPRVPSummary thisRow = new DVPRVPSummary();

                            thisRow.dvpName = thisRowsDVP;
                            thisRow.rvpName = thisRowsRVP;
                            thisRow.dairyBackhaulUnits = (int)theReader["UnitsBackhauled"];

                            dvprvpData.Add(thisRow);

                            found = false;
                        }
                    }
                }

                theReader.Close();

                SqlCommand cmdDVPRVPUnitsBackhauledCost = new SqlCommand("ReportUnitsBackhauledCostWithIntervalForCDCWithProviderID", theConnection);
                cmdDVPRVPUnitsBackhauledCost.Parameters.AddWithValue("@dateStarted", startDate);
                cmdDVPRVPUnitsBackhauledCost.Parameters.AddWithValue("@dateEnded", endDate);
                cmdDVPRVPUnitsBackhauledCost.Parameters.AddWithValue("@providerID", Int32.Parse(providerID));
                cmdDVPRVPUnitsBackhauledCost.CommandType = System.Data.CommandType.StoredProcedure;
                cmdDVPRVPUnitsBackhauledCost.CommandTimeout = 1200;

                theReader = cmdDVPRVPUnitsBackhauledCost.ExecuteReader();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        string thisRowsDVP = theReader["ProviderName"].ToString();
                        string thisRowsRVP = theReader["CDCName"].ToString();

                        bool found = false;

                        for (int i = 0, l = dvprvpData.Count; i < l; i++)
                        {
                            DVPRVPSummary thisRowData = dvprvpData[i];

                            if (thisRowsDVP.Equals(thisRowData.dvpName) && thisRowsRVP.Equals(thisRowData.rvpName))
                            {
                                found = true;

                                if (theReader["BackhaulCost"] != DBNull.Value)
                                {
                                    double backhaulCost = (double)theReader["BackhaulCost"];

                                    thisRowData.dairyBackhaulCOGS = backhaulCost;
                                }
                            }
                        }

                        if (!found)
                        {
                            DVPRVPSummary thisRow = new DVPRVPSummary();

                            thisRow.dvpName = thisRowsDVP;
                            thisRow.rvpName = thisRowsRVP;
                            thisRow.dairyBackhaulCOGS = (double)theReader["BackhaulCost"];

                            dvprvpData.Add(thisRow);

                            found = false;
                        }
                    }
                }

                theReader.Close();

                closeDataConnection();

                try
                {
                    int wantedProviderID = Int32.Parse(providerID);

                    string wantedProviderName = getProviderNameFromID(wantedProviderID);

                    string newFilePath = CopyReportTemplate("cdc");


                    /*string oleConnectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + newFilePath + ";Extended Properties=\"Excel 8.0;\"";
                    OleDbConnection oleConnection = new OleDbConnection(oleConnectionString);
                    oleConnection.Open();*/

                    openDataConnection();
                    SqlCommand cmdReport = new SqlCommand("ReportStoresNotReadyWithProviderIDWithInterval", theConnection);
                    cmdReport.Parameters.AddWithValue("@dateStarted", startDate);
                    cmdReport.Parameters.AddWithValue("@dateEnded", endDate);
                    cmdReport.Parameters.AddWithValue("@providerID", Int32.Parse(providerID));
                    cmdReport.CommandType = System.Data.CommandType.StoredProcedure;
                    cmdReport.CommandTimeout = 1200;

                    theReader = cmdReport.ExecuteReader();

                    FileStream template = new FileStream(newFilePath, FileMode.Open, FileAccess.Read);
                    //FileStream fileRead = new FileStream(destFile, FileMode.Create, FileAccess.ReadWrite);
                    //IWorkbook theWorkbook = new XSSFWorkbook(fileRead);
                    //Create a stream of .xlsx file contained within my project using reflection


                    //EPPlusTest = Namespace/Project
                    //templates = folder
                    //VendorTemplate.xlsx = file

                    //ExcelPackage has a constructor that only requires a stream.
                    ExcelPackage pck = new OfficeOpenXml.ExcelPackage(template);
                    ExcelWorkbook workBook = pck.Workbook;
                    //fileRead.Close();
                    template.Close();

                    if (theReader.HasRows)
                    {
                        int rowIndex = 1;
                        //ISheet notReadySheet = theWorkbook.GetSheet("DetailedViewNotReady");
                        ExcelWorksheet notReadySheet = workBook.Worksheets["DetailedViewNotReady"];
                        //notReadySheet.ForceFormulaRecalculation = true;
                        string photoVal = "";
                        //IRow crow = null;

                        while (theReader.Read())
                        {
                            rowIndex++;
                            //crow = notReadySheet.CreateRow(rowIndex);
                            //DateTime todaysDateTime = DateTime.Today;
                            //DateTime thisEntrysDateTime = Convert.ToDateTime(theReader[0].ToString());
                            //TimeSpan timeDifference = todaysDateTime - thisEntrysDateTime;
                            //int differenceInDays = timeDifference.Days;
                            photoVal = "";
                            notReadySheet.Cells[rowIndex, 1].Value = theReader[0].ToString();
                            notReadySheet.Cells[rowIndex, 2].Value = theReader[1].ToString();
                            notReadySheet.Cells[rowIndex, 3].Value = theReader[2].ToString();
                            notReadySheet.Cells[rowIndex, 4].Value = theReader[3].ToString();
                            notReadySheet.Cells[rowIndex, 5].Value = theReader[4].ToString();
                            notReadySheet.Cells[rowIndex, 6].Value = theReader[9].ToString();
                            notReadySheet.Cells[rowIndex, 7].Value = theReader[10].ToString();
                            notReadySheet.Cells[rowIndex, 8].Value = theReader[11].ToString();
                            notReadySheet.Cells[rowIndex, 9].Value = getCellFriendlyText(theReader[12].ToString());
                            if (theReader[13].ToString() != null && !theReader[13].ToString().Equals(""))
                            {
                                photoVal += "" + baseURLForPhotoLink + theReader[13].ToString() + "";
                            }
                            else
                            {
                                photoVal += "No Photos Available";
                            }
                            notReadySheet.Cells[rowIndex, 10].Value = photoVal;

                        }
                        theResponse.statusCode = 0;
                        theResponse.statusDescription = extractFilename(newFilePath);
                    }
                    else
                    {
                        theResponse.statusCode = 1;
                        theResponse.statusDescription = "There is no data logged between the dates that were selected";
                    }
                    theReader.Close();



                    SqlCommand cmdReportNotReady = new SqlCommand("ReportStoresReadyWithProviderIDWithInterval", theConnection);
                    cmdReportNotReady.Parameters.AddWithValue("@dateStarted", startDate);
                    cmdReportNotReady.Parameters.AddWithValue("@dateEnded", endDate);
                    cmdReportNotReady.Parameters.AddWithValue("@providerID", Int32.Parse(providerID));
                    cmdReportNotReady.CommandType = System.Data.CommandType.StoredProcedure;
                    cmdReportNotReady.CommandTimeout = 1200;

                    theReader = cmdReportNotReady.ExecuteReader();
                    if (theReader.HasRows)
                    {
                        int rowIndex = 1;
                        ExcelWorksheet readySheet = workBook.Worksheets["DetailedViewReady"];

                        while (theReader.Read())
                        {
                            rowIndex++;

                            readySheet.Cells[rowIndex, 1].Value = theReader[0].ToString();
                            readySheet.Cells[rowIndex, 2].Value = theReader[1].ToString();
                            readySheet.Cells[rowIndex, 3].Value = theReader[2].ToString();
                            readySheet.Cells[rowIndex, 4].Value = theReader[3].ToString();
                            readySheet.Cells[rowIndex, 5].Value = theReader[4].ToString();

                        }
                        theResponse.statusCode = 0;
                        theResponse.statusDescription = extractFilename(newFilePath);
                    }
                    else
                    {
                        theResponse.statusCode = 1;
                        theResponse.statusDescription = "There is no data logged between the dates that were selected";
                    }
                    theReader.Close();


                    /* if (theReader.HasRows)
                     {
                         while (theReader.Read())
                         {
                             DateTime todaysDateTime = DateTime.Today;
                             DateTime thisEntrysDateTime = Convert.ToDateTime(theReader[0].ToString());
                             TimeSpan timeDifference = todaysDateTime - thisEntrysDateTime;
                             int differenceInDays = timeDifference.Days;

                             string values = "'" + theReader[0] + "','" + theReader[1] + "','" + theReader[2] + "','" + theReader[3] + "','" + theReader[4] + "','" + theReader[9] + "','" + theReader[10] + "','" + theReader[11] + "','" + getCellFriendlyText(theReader[12].ToString()) + "'";

                             if (differenceInDays <= 21 && theReader[13].ToString() != null && !theReader[13].ToString().Equals(""))
                             {
                                 values += ",'" + baseURLForPhotoLink + theReader[13].ToString() + "'";
                             }
                             else
                             {
                                 values += ",'No Photos Available'";
                             }

                             OleDbCommand oleCommand = new OleDbCommand();
                             string sqlCommand = "INSERT INTO [DetailedViewNotReady$] (InStoreDate, StoreNumber, Provider, CDC, Route, ReasonCode, Photos, UnitsBackhauled, Comments, PhotoLink) VALUES (" + values + ")";
                             oleCommand.CommandText = sqlCommand;
                             oleCommand.Connection = oleConnection;
                             oleCommand.ExecuteNonQuery();
                         }

                         theResponse.statusCode = 0;
                         theResponse.statusDescription = newFilePath;
                     }
                     else
                     {
                         theResponse.statusCode = 1;
                         theResponse.statusDescription = "There is no data logged between the dates that were selected";
                     }

                     theReader.Close();

                     SqlCommand cmdReportReady = new SqlCommand("ReportStoresReadyWithProviderIDWithInterval", theConnection);
                     cmdReportReady.Parameters.AddWithValue("@dateStarted", startDate);
                     cmdReportReady.Parameters.AddWithValue("@dateEnded", endDate);
                     cmdReportReady.Parameters.AddWithValue("@providerID", Int32.Parse(providerID));
                     cmdReportReady.CommandType = System.Data.CommandType.StoredProcedure;

                     theReader = cmdReportReady.ExecuteReader();

                     if (theReader.HasRows)
                     {
                         while (theReader.Read())
                         {
                            /* DateTime todaysDateTime = DateTime.Today;
                             DateTime thisEntrysDateTime = Convert.ToDateTime(theReader[0].ToString());
                             TimeSpan timeDifference = todaysDateTime - thisEntrysDateTime;
                             int differenceInDays = timeDifference.Days;

                             string values = "'" + theReader[0] + "','" + theReader[1] + "','" + theReader[2] + "','" + theReader[3] + "','" + theReader[4] + "'";



                             OleDbCommand oleCommand = new OleDbCommand();
                             string sqlCommand = "INSERT INTO 	[DetailedViewReady$] (InStoreDate, StoreNumber, Provider, CDC, Route) VALUES (" + values + ")";
                             oleCommand.CommandText = sqlCommand;
                             oleCommand.Connection = oleConnection;
                             oleCommand.ExecuteNonQuery();
                         }

                         theResponse.statusCode = 0;
                         theResponse.statusDescription = newFilePath;
                     }

                     theReader.Close();

                     oleConnection.Close();*/



                    ExcelWorksheet sheetSummary = workBook.Worksheets["Summary"];

                    sheetSummary.Cells["A2:C2"].Merge = true;
                    sheetSummary.Cells[2, 1].Value = "All Data";

                    if (dvprvpData != null && dvprvpData.Count > 0)
                    {
                        int currentRowIndex = 3;

                        for (int i = 0, l = dvprvpData.Count; i < l; i++)
                        {
                            DVPRVPSummary thisRowsData = dvprvpData[i];

                            thisRowsData.percentageStoresReady = Math.Round((100 - ((double)thisRowsData.deliveriesWithIssues / (double)thisRowsData.deliveries) * 100.0), 2);

                            sheetSummary.Cells[currentRowIndex, 1].Value = thisRowsData.dvpName;
                            sheetSummary.Cells[currentRowIndex, 2].Value = thisRowsData.rvpName;
                            sheetSummary.Cells[currentRowIndex, 3].Value = thisRowsData.percentageStoresReady.ToString() + " %";
                            //           sheetSummary.Cells[currentRowIndex, 4].Value = thisRowsData.leftoutUnits;
                            //           sheetSummary.Cells[currentRowIndex, 5].Value = thisRowsData.dairyBackhaulUnits;
                            sheetSummary.Cells[currentRowIndex, 4].Value = thisRowsData.deliveries;
                            sheetSummary.Cells[currentRowIndex, 5].Value = thisRowsData.deliveriesWithIssues;
                            sheetSummary.Cells[currentRowIndex, 6].Value = thisRowsData.totalReadinessIssues;

                            currentRowIndex++;
                        }

                        theResponse.statusCode = 0;
                        theResponse.statusDescription = extractFilename(newFilePath);
                    }
                    else
                    {
                        theResponse.statusCode = 1;
                        theResponse.statusDescription = "There is no data logged between the dates that were selected";
                    }



                    FileStream fileSave = new FileStream(newFilePath, FileMode.Create);
                    //theWorkbook.Write(fileSave);
                    pck.SaveAs(fileSave);
                    fileSave.Close();

                    closeDataConnection();
                }
                catch (Exception _exception)
                {
                    theResponse.statusCode = 6;
                    theResponse.statusDescription = _exception.Message + " " + _exception.StackTrace;
                }

                return theResponse;
            }
            else
            {
                return ReportStoreReadinessForCDC();
            }
        }


        public Response ReportFieldReadiness()
        {
            Response theResponse = new Response();

            openDataConnection();

            /* DVP - RVP Data */

            SqlCommand cmdDVPRVPDeliveriesWithoutIssues = new SqlCommand("ReportDVPRVPNumberOfDeliveriesWithoutIssues", theConnection);
            cmdDVPRVPDeliveriesWithoutIssues.CommandType = System.Data.CommandType.StoredProcedure;
            cmdDVPRVPDeliveriesWithoutIssues.CommandTimeout = 1200;

            theReader = cmdDVPRVPDeliveriesWithoutIssues.ExecuteReader();

            List<DVPRVPSummary> dvprvpData = new List<DVPRVPSummary>();

            if (theReader.HasRows)
            {
                while (theReader.Read())
                {
                    DVPRVPSummary thisRow = new DVPRVPSummary();

                    thisRow.dvpName = theReader["DVPOutlookName"].ToString();
                    thisRow.rvpName = theReader["RVPOutlookName"].ToString();
                    thisRow.deliveries = (int)theReader["NumberOfDeliveriesWithoutIssues"];

                    dvprvpData.Add(thisRow);
                }
            }

            theReader.Close();

            SqlCommand cmdDVPRVPDeliveriesWithIssues = new SqlCommand("ReportDVPRVPNumberOfDeliveriesWithIssues", theConnection);
            cmdDVPRVPDeliveriesWithIssues.CommandType = System.Data.CommandType.StoredProcedure;
            cmdDVPRVPDeliveriesWithIssues.CommandTimeout = 1200;

            theReader = cmdDVPRVPDeliveriesWithIssues.ExecuteReader();

            if (theReader.HasRows)
            {
                while (theReader.Read())
                {
                    string thisRowsDVP = theReader["DVPOutlookName"].ToString();
                    string thisRowsRVP = theReader["RVPOutlookName"].ToString();

                    bool found = false;

                    for (int i = 0, l = dvprvpData.Count; i < l; i++)
                    {
                        DVPRVPSummary thisRowData = dvprvpData[i];

                        if (thisRowsDVP.Equals(thisRowData.dvpName) && thisRowsRVP.Equals(thisRowData.rvpName))
                        {
                            found = true;

                            int deliveriesWithIssues = (int)theReader["NumberOfDeliveriesWithIssues"];

                            thisRowData.deliveriesWithIssues = deliveriesWithIssues;
                            thisRowData.deliveries += thisRowData.deliveriesWithIssues;
                        }
                    }

                    if (!found)
                    {
                        DVPRVPSummary thisRow = new DVPRVPSummary();

                        thisRow.dvpName = thisRowsDVP;
                        thisRow.rvpName = thisRowsRVP;
                        thisRow.deliveriesWithIssues = (int)theReader["NumberOfDeliveriesWithIssues"];
                        thisRow.deliveries += thisRow.deliveriesWithIssues;

                        dvprvpData.Add(thisRow);

                        found = false;
                    }
                }
            }

            theReader.Close();

            SqlCommand cmdDVPRVPNumberOfIssues = new SqlCommand("ReportDVPRVPNumberOfIssues", theConnection);
            cmdDVPRVPNumberOfIssues.CommandType = System.Data.CommandType.StoredProcedure;
            cmdDVPRVPNumberOfIssues.CommandTimeout = 1200;

            theReader = cmdDVPRVPNumberOfIssues.ExecuteReader();

            if (theReader.HasRows)
            {
                while (theReader.Read())
                {
                    string thisRowsDVP = theReader["DVPOutlookName"].ToString();
                    string thisRowsRVP = theReader["RVPOutlookName"].ToString();

                    bool found = false;

                    for (int i = 0, l = dvprvpData.Count; i < l; i++)
                    {
                        DVPRVPSummary thisRowData = dvprvpData[i];

                        if (thisRowsDVP.Equals(thisRowData.dvpName) && thisRowsRVP.Equals(thisRowData.rvpName))
                        {
                            found = true;

                            int numberOfIssues = (int)theReader["NumberOfIssues"];

                            thisRowData.totalReadinessIssues = numberOfIssues;
                        }
                    }

                    if (!found)
                    {
                        DVPRVPSummary thisRow = new DVPRVPSummary();

                        thisRow.dvpName = thisRowsDVP;
                        thisRow.rvpName = thisRowsRVP;
                        thisRow.totalReadinessIssues = (int)theReader["NumberOfIssues"];

                        dvprvpData.Add(thisRow);

                        found = false;
                    }
                }
            }

            theReader.Close();

            SqlCommand cmdDVPRVPUnitsLeftout = new SqlCommand("ReportDVPRVPUnitsLeftout", theConnection);
            cmdDVPRVPUnitsLeftout.CommandType = System.Data.CommandType.StoredProcedure;
            cmdDVPRVPUnitsLeftout.CommandTimeout = 1200;

            theReader = cmdDVPRVPUnitsLeftout.ExecuteReader();

            if (theReader.HasRows)
            {
                while (theReader.Read())
                {
                    string thisRowsDVP = theReader["DVPOutlookName"].ToString();
                    string thisRowsRVP = theReader["RVPOutlookName"].ToString();

                    bool found = false;

                    for (int i = 0, l = dvprvpData.Count; i < l; i++)
                    {
                        DVPRVPSummary thisRowData = dvprvpData[i];

                        if (thisRowsDVP.Equals(thisRowData.dvpName) && thisRowsRVP.Equals(thisRowData.rvpName))
                        {
                            found = true;

                            int unitsLeftout = (int)theReader["UnitsLeftout"];

                            thisRowData.leftoutUnits = unitsLeftout;
                        }
                    }

                    if (!found)
                    {
                        DVPRVPSummary thisRow = new DVPRVPSummary();

                        thisRow.dvpName = thisRowsDVP;
                        thisRow.rvpName = thisRowsRVP;
                        thisRow.leftoutUnits = (int)theReader["UnitsLeftout"];

                        dvprvpData.Add(thisRow);

                        found = false;
                    }
                }
            }

            theReader.Close();

            SqlCommand cmdDVPRVPUnitsLeftoutCost = new SqlCommand("ReportDVPRVPUnitsLeftoutCost", theConnection);
            cmdDVPRVPUnitsLeftoutCost.CommandType = System.Data.CommandType.StoredProcedure;
            cmdDVPRVPUnitsLeftoutCost.CommandTimeout = 1200;

            theReader = cmdDVPRVPUnitsLeftoutCost.ExecuteReader();

            if (theReader.HasRows)
            {
                while (theReader.Read())
                {
                    string thisRowsDVP = theReader["DVPOutlookName"].ToString();
                    string thisRowsRVP = theReader["RVPOutlookName"].ToString();

                    bool found = false;

                    for (int i = 0, l = dvprvpData.Count; i < l; i++)
                    {
                        DVPRVPSummary thisRowData = dvprvpData[i];

                        if (thisRowsDVP.Equals(thisRowData.dvpName) && thisRowsRVP.Equals(thisRowData.rvpName))
                        {
                            found = true;

                            if (theReader["LeftoutCost"] != DBNull.Value)
                            {
                                double leftoutCost = (double)theReader["LeftoutCost"];

                                thisRowData.leftoutCOGS = leftoutCost;
                            }
                        }
                    }

                    if (!found)
                    {
                        DVPRVPSummary thisRow = new DVPRVPSummary();

                        thisRow.dvpName = thisRowsDVP;
                        thisRow.rvpName = thisRowsRVP;
                        thisRow.leftoutCOGS = (double)theReader["LeftoutCost"];

                        dvprvpData.Add(thisRow);

                        found = false;
                    }
                }
            }

            theReader.Close();

            SqlCommand cmdDVPRVPUnitsBackhauled = new SqlCommand("ReportDVPRVPUnitsBackhauled", theConnection);
            cmdDVPRVPUnitsBackhauled.CommandType = System.Data.CommandType.StoredProcedure;
            cmdDVPRVPUnitsBackhauled.CommandTimeout = 1200;

            theReader = cmdDVPRVPUnitsBackhauled.ExecuteReader();

            if (theReader.HasRows)
            {
                while (theReader.Read())
                {
                    string thisRowsDVP = theReader["DVPOutlookName"].ToString();
                    string thisRowsRVP = theReader["RVPOutlookName"].ToString();

                    bool found = false;

                    for (int i = 0, l = dvprvpData.Count; i < l; i++)
                    {
                        DVPRVPSummary thisRowData = dvprvpData[i];

                        if (thisRowsDVP.Equals(thisRowData.dvpName) && thisRowsRVP.Equals(thisRowData.rvpName))
                        {
                            found = true;

                            int unitsBackhauled = (int)theReader["UnitsBackhauled"];

                            thisRowData.dairyBackhaulUnits = unitsBackhauled;
                        }
                    }

                    if (!found)
                    {
                        DVPRVPSummary thisRow = new DVPRVPSummary();

                        thisRow.dvpName = thisRowsDVP;
                        thisRow.rvpName = thisRowsRVP;
                        thisRow.dairyBackhaulUnits = (int)theReader["UnitsBackhauled"];

                        dvprvpData.Add(thisRow);

                        found = false;
                    }
                }
            }

            theReader.Close();

            SqlCommand cmdDVPRVPUnitsBackhauledCost = new SqlCommand("ReportDVPRVPUnitsBackhauledCost", theConnection);
            cmdDVPRVPUnitsBackhauledCost.CommandType = System.Data.CommandType.StoredProcedure;
            cmdDVPRVPUnitsBackhauledCost.CommandTimeout = 1200;

            theReader = cmdDVPRVPUnitsBackhauledCost.ExecuteReader();

            if (theReader.HasRows)
            {
                while (theReader.Read())
                {
                    string thisRowsDVP = theReader["DVPOutlookName"].ToString();
                    string thisRowsRVP = theReader["RVPOutlookName"].ToString();

                    bool found = false;

                    for (int i = 0, l = dvprvpData.Count; i < l; i++)
                    {
                        DVPRVPSummary thisRowData = dvprvpData[i];

                        if (thisRowsDVP.Equals(thisRowData.dvpName) && thisRowsRVP.Equals(thisRowData.rvpName))
                        {
                            found = true;

                            if (theReader["BackhaulCost"] != DBNull.Value)
                            {
                                double backhaulCost = (double)theReader["BackhaulCost"];

                                thisRowData.dairyBackhaulCOGS = backhaulCost;
                            }
                        }
                    }

                    if (!found)
                    {
                        DVPRVPSummary thisRow = new DVPRVPSummary();

                        thisRow.dvpName = thisRowsDVP;
                        thisRow.rvpName = thisRowsRVP;
                        thisRow.dairyBackhaulCOGS = (double)theReader["BackhaulCost"];

                        dvprvpData.Add(thisRow);

                        found = false;
                    }
                }
            }

            theReader.Close();

            /* RD-DM Data */

            SqlCommand cmdRDDMDeliveriesWithoutIssues = new SqlCommand("ReportRDDMNumberOfDeliveriesWithoutIssues", theConnection);
            cmdRDDMDeliveriesWithoutIssues.CommandType = System.Data.CommandType.StoredProcedure;
            cmdRDDMDeliveriesWithoutIssues.CommandTimeout = 1200;

            theReader = cmdRDDMDeliveriesWithoutIssues.ExecuteReader();

            List<RDDMSummary> rddmData = new List<RDDMSummary>();

            if (theReader.HasRows)
            {
                while (theReader.Read())
                {
                    RDDMSummary thisRow = new RDDMSummary();

                    thisRow.rdName = theReader["RDOutlookName"].ToString();
                    thisRow.dmName = theReader["DMOutlookName"].ToString();
                    thisRow.deliveries = (int)theReader["NumberOfDeliveriesWithoutIssues"];

                    rddmData.Add(thisRow);
                }
            }

            theReader.Close();

            SqlCommand cmdRDDMDeliveriesWithIssues = new SqlCommand("ReportRDDMNumberOfDeliveriesWithIssues", theConnection);
            cmdRDDMDeliveriesWithIssues.CommandType = System.Data.CommandType.StoredProcedure;
            cmdRDDMDeliveriesWithIssues.CommandTimeout = 1200;

            theReader = cmdRDDMDeliveriesWithIssues.ExecuteReader();

            if (theReader.HasRows)
            {
                while (theReader.Read())
                {
                    string thisRowsRD = theReader["RDOutlookName"].ToString();
                    string thisRowsDM = theReader["DMOutlookName"].ToString();

                    bool found = false;

                    for (int i = 0, l = rddmData.Count; i < l; i++)
                    {
                        RDDMSummary thisRowData = rddmData[i];

                        if (thisRowsRD.Equals(thisRowData.rdName) && thisRowsDM.Equals(thisRowData.dmName))
                        {
                            found = true;

                            int deliveriesWithIssues = (int)theReader["NumberOfDeliveriesWithIssues"];

                            thisRowData.deliveriesWithIssues = deliveriesWithIssues;
                            thisRowData.deliveries += thisRowData.deliveriesWithIssues;
                        }
                    }

                    if (!found)
                    {
                        RDDMSummary thisRow = new RDDMSummary();

                        thisRow.rdName = theReader["RDOutlookName"].ToString();
                        thisRow.dmName = theReader["DMOutlookName"].ToString();
                        thisRow.deliveriesWithIssues = (int)theReader["NumberOfDeliveriesWithIssues"];
                        thisRow.deliveries += thisRow.deliveriesWithIssues;

                        rddmData.Add(thisRow);

                        found = false;
                    }
                }
            }

            theReader.Close();

            SqlCommand cmdRDDMNumberOfIssues = new SqlCommand("ReportRDDMNumberOfIssues", theConnection);
            cmdRDDMNumberOfIssues.CommandType = System.Data.CommandType.StoredProcedure;
            cmdRDDMNumberOfIssues.CommandTimeout = 1200;

            theReader = cmdRDDMNumberOfIssues.ExecuteReader();

            if (theReader.HasRows)
            {
                while (theReader.Read())
                {
                    string thisRowsRD = theReader["RDOutlookName"].ToString();
                    string thisRowsDM = theReader["DMOutlookName"].ToString();

                    bool found = false;

                    for (int i = 0, l = rddmData.Count; i < l; i++)
                    {
                        RDDMSummary thisRowData = rddmData[i];

                        if (thisRowsRD.Equals(thisRowData.rdName) && thisRowsDM.Equals(thisRowData.dmName))
                        {
                            found = true;

                            int numberOfIssues = (int)theReader["NumberOfIssues"];

                            thisRowData.totalReadinessIssues = numberOfIssues;
                        }
                    }

                    if (!found)
                    {
                        RDDMSummary thisRow = new RDDMSummary();

                        thisRow.rdName = theReader["RDOutlookName"].ToString();
                        thisRow.dmName = theReader["DMOutlookName"].ToString();
                        thisRow.totalReadinessIssues = (int)theReader["NumberOfIssues"];

                        rddmData.Add(thisRow);

                        found = false;
                    }
                }
            }

            theReader.Close();

            SqlCommand cmdRDDMUnitsBackhauled = new SqlCommand("ReportRDDMUnitsBackhauled", theConnection);
            cmdRDDMUnitsBackhauled.CommandType = System.Data.CommandType.StoredProcedure;
            cmdRDDMUnitsBackhauled.CommandTimeout = 1200;

            theReader = cmdRDDMUnitsBackhauled.ExecuteReader();

            if (theReader.HasRows)
            {
                while (theReader.Read())
                {
                    string thisRowsRD = theReader["RDOutlookName"].ToString();
                    string thisRowsDM = theReader["DMOutlookName"].ToString();

                    bool found = false;

                    for (int i = 0, l = rddmData.Count; i < l; i++)
                    {
                        RDDMSummary thisRowData = rddmData[i];

                        if (thisRowsRD.Equals(thisRowData.rdName) && thisRowsDM.Equals(thisRowData.dmName))
                        {
                            found = true;

                            int unitsBackhauled = (int)theReader["UnitsBackhauled"];

                            thisRowData.dairyBackhaulUnits = unitsBackhauled;
                        }
                    }

                    if (!found)
                    {
                        RDDMSummary thisRow = new RDDMSummary();

                        thisRow.rdName = theReader["RDOutlookName"].ToString();
                        thisRow.dmName = theReader["DMOutlookName"].ToString();
                        thisRow.dairyBackhaulUnits = (int)theReader["UnitsBackhauled"];

                        rddmData.Add(thisRow);

                        found = false;
                    }
                }
            }

            theReader.Close();

            SqlCommand cmdRDDMUnitsBackhauledCost = new SqlCommand("ReportRDDMUnitsBackhauledCost", theConnection);
            cmdRDDMUnitsBackhauledCost.CommandType = System.Data.CommandType.StoredProcedure;
            cmdRDDMUnitsBackhauledCost.CommandTimeout = 1200;

            theReader = cmdRDDMUnitsBackhauledCost.ExecuteReader();

            if (theReader.HasRows)
            {
                while (theReader.Read())
                {
                    string thisRowsRD = theReader["RDOutlookName"].ToString();
                    string thisRowsDM = theReader["DMOutlookName"].ToString();

                    bool found = false;

                    for (int i = 0, l = rddmData.Count; i < l; i++)
                    {
                        RDDMSummary thisRowData = rddmData[i];

                        if (thisRowsRD.Equals(thisRowData.rdName) && thisRowsDM.Equals(thisRowData.dmName))
                        {
                            found = true;

                            if (theReader["BackhaulCost"] != DBNull.Value)
                            {
                                double backhaulCost = (double)theReader["BackhaulCost"];

                                thisRowData.dairyBackhaulCOGS = backhaulCost;
                            }
                        }
                    }

                    if (!found)
                    {
                        RDDMSummary thisRow = new RDDMSummary();

                        thisRow.rdName = theReader["RDOutlookName"].ToString();
                        thisRow.dmName = theReader["DMOutlookName"].ToString();
                        thisRow.dairyBackhaulCOGS = (double)theReader["BackhaulCost"];

                        rddmData.Add(thisRow);

                        found = false;
                    }
                }
            }

            theReader.Close();

            SqlCommand cmdRDDMNumberOfIssuesOne = new SqlCommand("ReportRDDMGroupOneIssues", theConnection);
            cmdRDDMNumberOfIssuesOne.CommandType = System.Data.CommandType.StoredProcedure;
            cmdRDDMNumberOfIssuesOne.CommandTimeout = 1200;

            theReader = cmdRDDMNumberOfIssuesOne.ExecuteReader();

            if (theReader.HasRows)
            {
                while (theReader.Read())
                {
                    string thisRowsRD = theReader["RDOutlookName"].ToString();
                    string thisRowsDM = theReader["DMOutlookName"].ToString();

                    bool found = false;

                    for (int i = 0, l = rddmData.Count; i < l; i++)
                    {
                        RDDMSummary thisRowData = rddmData[i];

                        if (thisRowsRD.Equals(thisRowData.rdName) && thisRowsDM.Equals(thisRowData.dmName))
                        {
                            found = true;

                            int numberOfIssues = (int)theReader["NumberOfIssues"];

                            thisRowData.totalSecurityFacilityIssues = numberOfIssues;
                        }
                    }

                    if (!found)
                    {
                        RDDMSummary thisRow = new RDDMSummary();

                        thisRow.rdName = theReader["RDOutlookName"].ToString();
                        thisRow.dmName = theReader["DMOutlookName"].ToString();
                        thisRow.totalSecurityFacilityIssues = (int)theReader["NumberOfIssues"];

                        rddmData.Add(thisRow);

                        found = false;
                    }
                }
            }

            theReader.Close();

            SqlCommand cmdRDDMNumberOfIssuesTwo = new SqlCommand("ReportRDDMGroupTwoIssues", theConnection);
            cmdRDDMNumberOfIssuesTwo.CommandType = System.Data.CommandType.StoredProcedure;
            cmdRDDMNumberOfIssuesTwo.CommandTimeout = 1200;

            theReader = cmdRDDMNumberOfIssuesTwo.ExecuteReader();

            if (theReader.HasRows)
            {
                while (theReader.Read())
                {
                    string thisRowsRD = theReader["RDOutlookName"].ToString();
                    string thisRowsDM = theReader["DMOutlookName"].ToString();

                    bool found = false;

                    for (int i = 0, l = rddmData.Count; i < l; i++)
                    {
                        RDDMSummary thisRowData = rddmData[i];

                        if (thisRowsRD.Equals(thisRowData.rdName) && thisRowsDM.Equals(thisRowData.dmName))
                        {
                            found = true;

                            int numberOfIssues = (int)theReader["NumberOfIssues"];

                            thisRowData.totalCapacityIssues = numberOfIssues;
                        }
                    }

                    if (!found)
                    {
                        RDDMSummary thisRow = new RDDMSummary();

                        thisRow.rdName = theReader["RDOutlookName"].ToString();
                        thisRow.dmName = theReader["DMOutlookName"].ToString();
                        thisRow.totalCapacityIssues = (int)theReader["NumberOfIssues"];

                        rddmData.Add(thisRow);

                        found = false;
                    }
                }
            }

            theReader.Close();

            SqlCommand cmdRDDMNumberOfIssuesThree = new SqlCommand("ReportRDDMGroupThreeIssues", theConnection);
            cmdRDDMNumberOfIssuesThree.CommandType = System.Data.CommandType.StoredProcedure;
            cmdRDDMNumberOfIssuesThree.CommandTimeout = 1200;

            theReader = cmdRDDMNumberOfIssuesThree.ExecuteReader();

            if (theReader.HasRows)
            {
                while (theReader.Read())
                {
                    string thisRowsRD = theReader["RDOutlookName"].ToString();
                    string thisRowsDM = theReader["DMOutlookName"].ToString();

                    bool found = false;

                    for (int i = 0, l = rddmData.Count; i < l; i++)
                    {
                        RDDMSummary thisRowData = rddmData[i];

                        if (thisRowsRD.Equals(thisRowData.rdName) && thisRowsDM.Equals(thisRowData.dmName))
                        {
                            found = true;

                            int numberOfIssues = (int)theReader["NumberOfIssues"];

                            thisRowData.totalProductivityIssues = numberOfIssues;
                        }
                    }

                    if (!found)
                    {
                        RDDMSummary thisRow = new RDDMSummary();

                        thisRow.rdName = theReader["RDOutlookName"].ToString();
                        thisRow.dmName = theReader["DMOutlookName"].ToString();
                        thisRow.totalProductivityIssues = (int)theReader["NumberOfIssues"];

                        rddmData.Add(thisRow);

                        found = false;
                    }
                }
            }

            theReader.Close();

            closeDataConnection();

            try
            {
                string newFilePath = CopyReportTemplate("field");


                /*string oleConnectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + newFilePath + ";Extended Properties=\"Excel 8.0;\"";
                OleDbConnection oleConnection = new OleDbConnection(oleConnectionString);
                oleConnection.Open();*/

                openDataConnection();
                SqlCommand cmdReport = new SqlCommand("ReportStoresNotReady", theConnection);
                cmdReport.CommandType = System.Data.CommandType.StoredProcedure;
                cmdReport.CommandTimeout = 1200;

                theReader = cmdReport.ExecuteReader();

                FileStream template = new FileStream(newFilePath, FileMode.Open, FileAccess.Read);
                //FileStream fileRead = new FileStream(destFile, FileMode.Create, FileAccess.ReadWrite);
                //IWorkbook theWorkbook = new XSSFWorkbook(fileRead);
                //Create a stream of .xlsx file contained within my project using reflection


                //EPPlusTest = Namespace/Project
                //templates = folder
                //VendorTemplate.xlsx = file

                //ExcelPackage has a constructor that only requires a stream.
                ExcelPackage pck = new OfficeOpenXml.ExcelPackage(template);
                ExcelWorkbook workBook = pck.Workbook;
                //fileRead.Close();
                template.Close();

                if (theReader.HasRows)
                {
                    int rowIndex = 2;
                    //ISheet notReadySheet = theWorkbook.GetSheet("DetailedViewNotReady");
                    ExcelWorksheet notReadySheet = workBook.Worksheets["DetailedViewNotReady"];
                    //notReadySheet.ForceFormulaRecalculation = true;
                    string photoVal = "";
                    //IRow crow = null;

                    while (theReader.Read())
                    {
                        rowIndex++;
                        //crow = notReadySheet.CreateRow(rowIndex);
                        DateTime todaysDateTime = DateTime.Today;
                        DateTime thisEntrysDateTime = Convert.ToDateTime(theReader[0].ToString());
                        TimeSpan timeDifference = todaysDateTime - thisEntrysDateTime;
                        int differenceInDays = timeDifference.Days;
                        photoVal = "";
                        notReadySheet.Cells[rowIndex, 1].Value = theReader[0].ToString();
                        notReadySheet.Cells[rowIndex, 2].Value = theReader[1].ToString();
                        notReadySheet.Cells[rowIndex, 3].Value = theReader[2].ToString();
                        notReadySheet.Cells[rowIndex, 4].Value = theReader[3].ToString();
                        notReadySheet.Cells[rowIndex, 5].Value = theReader[4].ToString();
                        notReadySheet.Cells[rowIndex, 6].Value = theReader[5].ToString();
                        notReadySheet.Cells[rowIndex, 7].Value = theReader[6].ToString();
                        notReadySheet.Cells[rowIndex, 8].Value = theReader[7].ToString();
                        notReadySheet.Cells[rowIndex, 9].Value = theReader[8].ToString();
                        notReadySheet.Cells[rowIndex, 10].Value = theReader[9].ToString();
                        notReadySheet.Cells[rowIndex, 11].Value = theReader[10].ToString();
                        notReadySheet.Cells[rowIndex, 12].Value = theReader[11].ToString();
                        notReadySheet.Cells[rowIndex, 13].Value = getCellFriendlyText(theReader[12].ToString());
                        if (differenceInDays <= 21 && theReader[13].ToString() != null && !theReader[13].ToString().Equals(""))
                        {
                            photoVal += ",'" + baseURLForPhotoLink + theReader[13].ToString() + "'";
                        }
                        else
                        {
                            photoVal += ",'No Photos Available'";
                        }
                        notReadySheet.Cells[rowIndex, 14].Value = photoVal;

                    }
                    theResponse.statusCode = 0;
                    theResponse.statusDescription = extractFilename(newFilePath);
                }
                else
                {
                    theResponse.statusCode = 1;
                    theResponse.statusDescription = "There is no data logged between the dates that were selected";
                }
                theReader.Close();



                SqlCommand cmdReportNotReady = new SqlCommand("ReportStoresReady", theConnection);
                cmdReportNotReady.CommandType = System.Data.CommandType.StoredProcedure;
                cmdReportNotReady.CommandTimeout = 1200;

                theReader = cmdReportNotReady.ExecuteReader();
                if (theReader.HasRows)
                {
                    int rowIndex = 2;
                    ExcelWorksheet readySheet = workBook.Worksheets["DetailedViewReady"];

                    while (theReader.Read())
                    {
                        rowIndex++;

                        readySheet.Cells[rowIndex, 1].Value = theReader[0].ToString();
                        readySheet.Cells[rowIndex, 2].Value = theReader[1].ToString();
                        readySheet.Cells[rowIndex, 3].Value = theReader[2].ToString();
                        readySheet.Cells[rowIndex, 4].Value = theReader[3].ToString();
                        readySheet.Cells[rowIndex, 5].Value = theReader[4].ToString();

                    }
                    theResponse.statusCode = 0;
                    theResponse.statusDescription = extractFilename(newFilePath);
                }
                else
                {
                    theResponse.statusCode = 1;
                    theResponse.statusDescription = "There is no data logged between the dates that were selected";
                }
                theReader.Close();

                /*if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        DateTime todaysDateTime = DateTime.Today;
                        DateTime thisEntrysDateTime = Convert.ToDateTime(theReader[0].ToString());
                        TimeSpan timeDifference = todaysDateTime - thisEntrysDateTime;
                        int differenceInDays = timeDifference.Days;

                        string values = "'" + theReader[0] + "','" + theReader[1] + "','" + theReader[2] + "','" + theReader[3] + "','" + theReader[4] + "','" + theReader[5] + "','" + theReader[6] + "','" + theReader[7] + "','" + theReader[8] + "','" + theReader[9] + "','" + theReader[10] + "','" + theReader[11] + "','" + getCellFriendlyText(theReader[12].ToString()) + "'";

                        if (differenceInDays <= 21 && theReader[13].ToString() != null && !theReader[13].ToString().Equals(""))
                        {
                            values += ",'" + baseURLForPhotoLink + theReader[13].ToString() + "'";
                        }
                        else
                        {
                            values += ",'No Photos Available'";
                        }

                        OleDbCommand oleCommand = new OleDbCommand();
                        string sqlCommand = "INSERT INTO [DetailedViewNotReady$] (InStoreDate, StoreNumber, Provider, CDC, Route, DSVP, RVP, RD, DM, ReasonCode, Photos, UnitsBackhauled, Comments, PhotoLink) VALUES (" + values + ")";
                        oleCommand.CommandText = sqlCommand;
                        oleCommand.Connection = oleConnection;
                        oleCommand.ExecuteNonQuery();
                    }

                    theResponse.statusCode = 0;
                    theResponse.statusDescription = newFilePath;
                }
                else
                {
                    theResponse.statusCode = 1;
                    theResponse.statusDescription = "There is no data logged between the dates that were selected";
                }

                theReader.Close();

                SqlCommand cmdReportNotReady = new SqlCommand("ReportStoresReady", theConnection);
                
                cmdReportNotReady.CommandType = System.Data.CommandType.StoredProcedure;

                theReader = cmdReportNotReady.ExecuteReader();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        /*DateTime todaysDateTime = DateTime.Today;
                        DateTime thisEntrysDateTime = Convert.ToDateTime(theReader[0].ToString());
                        TimeSpan timeDifference = todaysDateTime - thisEntrysDateTime;
                        int differenceInDays = timeDifference.Days;

                        string values = "'" + theReader[0] + "','" + theReader[1] + "','" + theReader[2] + "','" + theReader[3] + "','" + theReader[4] + "'";



                        OleDbCommand oleCommand = new OleDbCommand();
                        string sqlCommand = "INSERT INTO 	[DetailedViewReady$] (InStoreDate, StoreNumber, Provider, CDC, Route) VALUES (" + values + ")";
                        oleCommand.CommandText = sqlCommand;
                        oleCommand.Connection = oleConnection;
                        oleCommand.ExecuteNonQuery();
                    }

                    theResponse.statusCode = 0;
                    theResponse.statusDescription = newFilePath;
                }

                theReader.Close();

                oleConnection.Close();*/

                ExcelWorksheet sheetSummary = workBook.Worksheets["DVPRVPSummary"];

                sheetSummary.Cells["A3:C3"].Merge = true;
                sheetSummary.Cells[3, 1].Value = "All Data";

                if (dvprvpData != null && dvprvpData.Count > 0)
                {
                    int currentRowIndex = 4;


                    for (int i = 0, l = dvprvpData.Count; i < l; i++)
                    {
                        DVPRVPSummary thisRowsData = dvprvpData[i];

                        thisRowsData.percentageStoresReady = Math.Round((100 - ((double)thisRowsData.deliveriesWithIssues / (double)thisRowsData.deliveries) * 100.0), 2);
                        thisRowsData.leftoutCOGS = Math.Round(thisRowsData.leftoutCOGS, 2);
                        thisRowsData.dairyBackhaulCOGS = Math.Round(thisRowsData.dairyBackhaulCOGS, 2);


                        sheetSummary.Cells[currentRowIndex, 1].Value = thisRowsData.dvpName;
                        sheetSummary.Cells[currentRowIndex, 2].Value = thisRowsData.rvpName;
                        sheetSummary.Cells[currentRowIndex, 3].Value = thisRowsData.percentageStoresReady.ToString() + " %";
                        sheetSummary.Cells[currentRowIndex, 4].Value = thisRowsData.leftoutUnits;
                        sheetSummary.Cells[currentRowIndex, 5].Value = "$ " + thisRowsData.leftoutCOGS.ToString();
                        sheetSummary.Cells[currentRowIndex, 6].Value = thisRowsData.dairyBackhaulUnits;
                        sheetSummary.Cells[currentRowIndex, 7].Value = "$ " + thisRowsData.dairyBackhaulCOGS.ToString();
                        sheetSummary.Cells[currentRowIndex, 8].Value = thisRowsData.deliveries;
                        sheetSummary.Cells[currentRowIndex, 9].Value = thisRowsData.deliveriesWithIssues;
                        sheetSummary.Cells[currentRowIndex, 10].Value = thisRowsData.totalReadinessIssues;

                        currentRowIndex++;
                    }

                    theResponse.statusCode = 0;
                    theResponse.statusDescription = extractFilename(newFilePath);
                }
                else
                {
                    theResponse.statusCode = 1;
                    theResponse.statusDescription = "There is no data logged between the dates that were selected";
                }


                ExcelWorksheet sheetRDDMSummary = workBook.Worksheets["RDDMSummary"];

                sheetRDDMSummary.Cells["A3:C3"].Merge = true;
                sheetRDDMSummary.Cells[3, 1].Value = "All Data";

                if (rddmData != null && rddmData.Count > 0)
                {
                    int currentRowIndex = 4;

                    //IRow currentRow = null;

                    for (int i = 0, l = rddmData.Count; i < l; i++)
                    {
                        RDDMSummary thisRowsData = rddmData[i];

                        thisRowsData.percentageStoresReady = Math.Round((100 - ((double)thisRowsData.deliveriesWithIssues / (double)thisRowsData.deliveries) * 100.0), 2);

                        thisRowsData.dairyBackhaulCOGS = Math.Round(thisRowsData.dairyBackhaulCOGS, 2);
                        // currentRow = sheetRDDMSummary.CreateRow(currentRowIndex);

                        sheetRDDMSummary.Cells[currentRowIndex, 1].Value = thisRowsData.rdName;
                        sheetRDDMSummary.Cells[currentRowIndex, 2].Value = thisRowsData.dmName;
                        sheetRDDMSummary.Cells[currentRowIndex, 3].Value = thisRowsData.percentageStoresReady.ToString() + " %";
                        sheetRDDMSummary.Cells[currentRowIndex, 4].Value = thisRowsData.dairyBackhaulUnits;
                        sheetRDDMSummary.Cells[currentRowIndex, 5].Value = "$ " + thisRowsData.dairyBackhaulCOGS.ToString();
                        sheetRDDMSummary.Cells[currentRowIndex, 6].Value = thisRowsData.deliveries;
                        sheetRDDMSummary.Cells[currentRowIndex, 7].Value = thisRowsData.deliveriesWithIssues;
                        sheetRDDMSummary.Cells[currentRowIndex, 8].Value = thisRowsData.totalReadinessIssues;
                        sheetRDDMSummary.Cells[currentRowIndex, 9].Value = thisRowsData.totalSecurityFacilityIssues;
                        sheetRDDMSummary.Cells[currentRowIndex, 10].Value = thisRowsData.totalCapacityIssues;
                        sheetRDDMSummary.Cells[currentRowIndex, 11].Value = thisRowsData.totalProductivityIssues;

                        currentRowIndex++;
                    }

                    theResponse.statusCode = 0;
                    theResponse.statusDescription = extractFilename(newFilePath);
                }
                else
                {
                    theResponse.statusCode = 1;
                    theResponse.statusDescription = "There is no data logged between the dates that were selected";
                }

                FileStream fileSave = new FileStream(newFilePath, FileMode.Create);
                //theWorkbook.Write(fileSave);
                pck.SaveAs(fileSave);
                fileSave.Close();

                closeDataConnection();
            }
            catch (Exception _exception)
            {
                theResponse.statusCode = 6;
                theResponse.statusDescription = _exception.Message + " " + _exception.StackTrace;
            }

            return theResponse;
        }

        private string extractFilename(string filePath)
        {
            if (filePath == null || filePath.Equals(""))
            {
                return "";
            }

            string[] path = filePath.Split('\\');

            List<string> parts = new List<string>();

            foreach (string aPart in path)
            {
                parts.Add(aPart);
            }

            return parts[parts.Count - 1];
        }

        private ResponseFailureList GetFailuresForStop(int stopID)
        {
            ResponseFailureList theResponse = new ResponseFailureList();

            openDataConnection();

            SqlCommand cmdGet = new SqlCommand("GetFailuresForStop", theConnection);
            cmdGet.Parameters.AddWithValue("@stopID", stopID);
            cmdGet.CommandType = System.Data.CommandType.StoredProcedure;

            try
            {
                theReader = cmdGet.ExecuteReader();

                if (theReader.HasRows)
                {
                    theResponse.failures = new List<FailureWithReason>();

                    while (theReader.Read())
                    {
                        FailureWithReason thisFailure = new FailureWithReason();

                        thisFailure.failureID = (int)theReader["FailureID"];
                        thisFailure.stopID = (int)theReader["StopID"];
                        thisFailure.parentReasonCode = (int)theReader["ReasonID"];
                        thisFailure.childReasonCode = (int)theReader["ChildReasonID"];
                        thisFailure.valueEntered = (int)theReader["ValueEntered"];
                        thisFailure.reason = new ReasonChildWithParent();
                        thisFailure.reason.childReasonCode = thisFailure.childReasonCode;
                        thisFailure.reason.childReasonName = theReader["ChildReasonName"].ToString();
                        thisFailure.reason.escalation = (bool)theReader["Escalation"];
                        thisFailure.reason.parentReason = new Reason();
                        thisFailure.reason.parentReason.reasonCode = thisFailure.parentReasonCode;
                        thisFailure.reason.parentReason.reasonName = theReader["ParentReasonName"].ToString();
                        thisFailure.reason.childReasonExplanation = theReader["ChildReasonExplanation"].ToString();

                        theResponse.failures.Add(thisFailure);
                    }

                    theResponse.statusCode = 0;
                    theResponse.statusDescription = "";
                }
                else
                {
                    theResponse.statusCode = 4;
                    theResponse.statusDescription = "No failures found";
                }
            }
            catch (Exception _exception)
            {
                theResponse.statusCode = 6;
                theResponse.statusDescription = _exception.Message;
            }

            theReader.Close();

            closeDataConnection();

            return theResponse;
        }

        public Response SendTestEmail()
        {
            Response theResponse = new Response();

            bool emailSent = false;

            try
            {
                SmtpClient client = new SmtpClient();
                client.Port = 587;
                client.Host = "smtp.gmail.com";
                client.EnableSsl = true;
                client.Timeout = 10000;
                client.DeliveryMethod = SmtpDeliveryMethod.Network;
                client.UseDefaultCredentials = false;
                client.Credentials = new System.Net.NetworkCredential("storereadiness@gmail.com", "sbuxreadyy");

                string textBody = "<p>Test Email</p>";

                string attachmentPath = HttpContext.Current.Server.MapPath(".") + "\\photos\\6039.jpg";

                MailMessage mm = new MailMessage(new MailAddress("storereadiness@gmail.com", "Store Readiness"), new MailAddress("pawail@gmail.com", "Pawail Qaisar"));

                mm.Subject = "Store Readiness Issues";
                mm.Body = textBody;
                mm.BodyEncoding = UTF8Encoding.UTF8;
                mm.IsBodyHtml = true;
                mm.DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure;

                int contentID = 1;

                Attachment inline = new Attachment(attachmentPath);
                inline.ContentDisposition.Inline = true;
                inline.ContentDisposition.DispositionType = DispositionTypeNames.Inline;
                inline.ContentId = contentID.ToString();
                inline.ContentType.MediaType = "image/jpeg";
                inline.ContentType.Name = Path.GetFileName(attachmentPath);

                mm.Attachments.Add(inline);

                client.Send(mm);

                emailSent = true;

                theResponse.statusCode = 0;
                theResponse.statusDescription = "";
            }
            catch (Exception _exception)
            {
                emailSent = false;

                theResponse.statusCode = 6;
                theResponse.statusDescription = _exception.Message + " / " + _exception.StackTrace;
            }

            return theResponse;
        }

        private bool SendEmail(string storeManagerName, string storeNumber, string cdcName, string storeManagerEmail, List<string> copyEmailAddresses, List<FailureWithReason> failures, List<Photo> photos)
        {
            bool emailSent = false;

            try
            {
                SmtpClient client = new SmtpClient();
                client.Port = 587;
                client.Host = "smtp.gmail.com";
                client.EnableSsl = true;
                client.Timeout = 10000;
                client.DeliveryMethod = SmtpDeliveryMethod.Network;
                client.UseDefaultCredentials = false;
                client.Credentials = new System.Net.NetworkCredential("storereadiness@gmail.com", "sbuxreadyy");
                //client.Credentials = new System.Net.NetworkCredential("admin@vwci.com", "starbux");

                string textBody = "<p>" + storeManagerName + ",<br /><br />";
                textBody += "<p>This email is being sent to notify you that a delivery readiness issue was reported by the driver.</p>";

                if (failures != null && failures.Count > 0)
                {
                    foreach (FailureWithReason aFailure in failures)
                    {
                        textBody += "<p><span style=\"font-weight:bold\">Store Readiness Issue</span> " + aFailure.reason.parentReason.reasonName + " : " + aFailure.reason.childReasonName + "</p>";
                        textBody += "<p><span style=\"font-weight:bold\">Description: </span>" + aFailure.reason.childReasonExplanation + (aFailure.valueEntered > 0 ? " - " + aFailure.valueEntered + " Items Impacted" : "") + "</p>";
                    }

                    if (photos != null && photos.Count > 0)
                    {
                        textBody += "<p><span style=\"font-weight:bold\">Photos:</span></p>";

                        textBody += "<ul>";

                        int counter = 1;
                        foreach (Photo aPhoto in photos)
                        {
                            textBody += "<li><a href=\"" + baseURLForPhotoLink + aPhoto.photoID + "\">Photo " + counter.ToString() + "</a></li>";
                            counter++;
                        }

                        textBody += "</ul>";
                    }
                }

                textBody += "<p>Please reference the <a href=\"http://rspnaadmin.starbucks.net/Docs/Resource Manuals/Inventory Management/Tools/Inventory Management � Tool Kit.pdf\">Inventory Management Toolkit</a> in the Retail Store Portal &gt; Resource Manuals &gt; Inventory Management &gt; Tools for how you can be more prepared for your deliveries.</p>";
                textBody += "<p>Thank you for your partnership in helping us deliver what you ordered.</p>";
                textBody += "<p><span style=\"font-weight:bold\">Questions, please contact your LSR.</span></p>";

                MailMessage mm = new MailMessage(new MailAddress("storereadiness@gmail.com", "Store Readiness"), new MailAddress(storeManagerEmail, storeManagerName));

                foreach (string copyAddress in copyEmailAddresses)
                {
                    mm.CC.Add(new MailAddress(copyAddress));
                }

                //        mm.Subject = "Store Readiness Issues";
                mm.Subject = "Store Readiness Issues - Store " + storeNumber + " ; " + cdcName;

                mm.Body = textBody;
                mm.BodyEncoding = UTF8Encoding.UTF8;
                mm.IsBodyHtml = true;
                mm.DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure;

                if (photos != null && photos.Count > 0)
                {
                    int contentID = 1;

                    foreach (Photo aPhoto in photos)
                    {
                        savePhotoToDisk(aPhoto.imageData, aPhoto.photoID);

                        string attachmentPath = HttpContext.Current.Server.MapPath(".") + "\\photos\\" + aPhoto.photoID + ".jpg";

                        Attachment inline = new Attachment(attachmentPath);
                        inline.ContentDisposition.Inline = true;
                        inline.ContentDisposition.DispositionType = DispositionTypeNames.Inline;
                        inline.ContentId = contentID.ToString();
                        inline.ContentType.MediaType = "image/jpeg";
                        inline.ContentType.Name = Path.GetFileName(attachmentPath);

                        mm.Attachments.Add(inline);

                        contentID++;
                    }
                }

                client.Send(mm);

                emailSent = true;
            }
            catch (Exception _exception)
            {
                emailSent = false;
            }

            return emailSent;
        }

        private bool SendEmailToCDC(string cdcName, string cdcEmail, string routeName, DateTime theTime)
        {
            bool emailSent = false;

            try
            {
                SmtpClient client = new SmtpClient();
                client.Port = 587;
                client.Host = "smtp.gmail.com";
                client.EnableSsl = true;
                client.Timeout = 10000;
                client.DeliveryMethod = SmtpDeliveryMethod.Network;
                client.UseDefaultCredentials = false;
                client.Credentials = new System.Net.NetworkCredential("storereadiness@gmail.com", "sbuxreadyy");

                string textBody = "<p>" + cdcName + ",<br />";
                textBody += "<p>Delivery route " + routeName + " has submitted completed trip in Store Readiness application at " + theTime.ToLongDateString() + " " + theTime.ToLongTimeString() + ". Please notify driver that all route information has been processed.";
                textBody += "<p>Thank you,</p>";
                textBody += "<p>The Starbucks Delivery Team</p>";

                List<MailAddress> ccList = new List<MailAddress>();

                string cdcEmailTo = replaceCommasWithSemiColons(cdcEmail);
                string[] cdcEmails = cdcEmailTo.Split(';');
                if (cdcEmails.Count() > 0)
                {
                    cdcEmailTo = cdcEmails[0];

                    for (int i = 1, l = cdcEmails.Count(); i < l; i++)
                    {
                        ccList.Add(new MailAddress(cdcEmails[i]));
                    }
                }
                //ccList.Add(new MailAddress("pawail@gmail.com"));

                MailMessage mm = new MailMessage(new MailAddress("storereadiness@gmail.com", "Store Readiness"), new MailAddress(cdcEmailTo, cdcName));

                for (int i = 0, l = ccList.Count(); i < l; i++)
                {
                    mm.CC.Add(ccList[i]);
                }

                mm.Subject = cdcName + " - " + routeName + " - Trip Completed";
                mm.Body = textBody;
                mm.BodyEncoding = UTF8Encoding.UTF8;
                mm.IsBodyHtml = true;
                mm.DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure;

                client.Send(mm);

                emailSent = true;
            }
            catch (Exception _exception)
            {
                emailSent = false;
            }

            return emailSent;
        }

        public Response ConsolidateEmails(string stopID)
        {
            Response theResponse = new Response();

            try
            {
                ResponsePhoto photosResponse = GetPhotosForStop(stopID);
                ResponseFailureList failuresResponse = GetFailuresForStop(Int32.Parse(stopID));

                Store thisStore = GetStoreForStop(Int32.Parse(stopID));

                CDC thisCDC = GetCDCForStop(Int32.Parse(stopID));

                bool escalationRequired = false;
                string escalationToName = "";
                string escalationToEmail = "";

                foreach (FailureWithReason aFailure in failuresResponse.failures)
                {
                    if (aFailure.reason.escalation)
                    {
                        escalationRequired = true;
                    }
                }

                if (escalationRequired)
                {
                    if (thisStore != null)
                    {
                        ResponseOpList opList = GetOpsForStoreID(thisStore.storeID.ToString());

                        if (opList.statusCode == 0)
                        {
                            if (opList.ops != null && opList.ops.Count > 0)
                            {
                                escalationToName = opList.ops[0].dmOutlookName;
                                escalationToEmail = opList.ops[0].dmEmailAddress;
                            }
                        }
                    }
                }

                List<string> listCC = new List<string>();
                string storeEmailAddressTo = thisStore.storeEmailAddress;
                storeEmailAddressTo = replaceCommasWithSemiColons(storeEmailAddressTo);

                string[] storeEmailAddressesSeparatedBySemiColon = storeEmailAddressTo.Split(';');
                if (storeEmailAddressesSeparatedBySemiColon.Count() > 1)
                {
                    storeEmailAddressTo = storeEmailAddressesSeparatedBySemiColon[0];

                    for (int i = 1, l = storeEmailAddressesSeparatedBySemiColon.Count(); i < l; i++)
                    {
                        listCC.Add(storeEmailAddressesSeparatedBySemiColon[i]);
                    }
                }

                //listCC.Add("kmotwani@starbucks.com");
                //listCC.Add("regularrex@gmail.com");

                //    SendEmail(thisStore.storeManagerName + " - Store Number " + thisStore.storeNumber, storeEmailAddressTo, listCC, failuresResponse.failures, photosResponse.photos);

                SendEmail(thisStore.storeManagerName + " - Store Number " + thisStore.storeNumber, thisStore.storeNumber, thisCDC.name,
                            storeEmailAddressTo, listCC, failuresResponse.failures, photosResponse.photos);


                if (escalationRequired && !escalationToEmail.Equals("") && !escalationToName.Equals(""))
                {
                    //         SendEmail(escalationToName, escalationToEmail, listCC, failuresResponse.failures, photosResponse.photos);
                    SendEmail(escalationToName, thisStore.storeNumber, thisCDC.name, escalationToEmail, listCC, failuresResponse.failures, photosResponse.photos);
                }

                theResponse.statusCode = 0;
                theResponse.statusDescription = "Email(s) sent to " + thisStore.storeEmailAddress + (escalationRequired ? ". Emails were also sent to " + thisStore.storeEmailAddress + " for escalation." : "");

                return theResponse;
            }
            catch (Exception e)
            {
                SendErrorEmail(e.Message + " / " + e.StackTrace);
                return theResponse;
            }
        }

        public void SendErrorEmail(string message)
        {
            try
            {
                SmtpClient client = new SmtpClient();
                client.Port = 3388;
                client.Host = "108.60.221.2";
                client.EnableSsl = false;
                client.Timeout = 10000;
                client.DeliveryMethod = SmtpDeliveryMethod.Network;
                client.UseDefaultCredentials = false;
                client.Credentials = new System.Net.NetworkCredential("admin@vwci.com", "starbux");

                string textBody = "<p>Error Email</p><br /> <br />" + message;
                MailMessage mm = new MailMessage(new MailAddress("regularrex@gmail.com", "Web Error"), new MailAddress("regularrex@gmail.com", "regularrex@gmail.com"));

                mm.Subject = "Web Error";
                mm.Body = textBody;
                mm.BodyEncoding = UTF8Encoding.UTF8;
                mm.IsBodyHtml = true;
                mm.DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure;

                client.Send(mm);

            }
            catch (Exception _exception)
            {
                return;
            }

            return;
        }

        public Store GetStoreForStop(int stopID)
        {
            Store thisStore = null;

            openDataConnection();

            SqlCommand getStore = new SqlCommand("GetStoreForStop", theConnection);
            getStore.Parameters.AddWithValue("@stopID", stopID);
            getStore.CommandType = System.Data.CommandType.StoredProcedure;

            try
            {
                theReader = getStore.ExecuteReader();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        thisStore = new Store();

                        thisStore.storeID = (int)theReader["StoreID"];
                        thisStore.storeName = theReader["StoreName"].ToString();
                        thisStore.storeAddress = theReader["StoreAddress"].ToString();
                        thisStore.storeCity = theReader["StoreCity"].ToString();
                        thisStore.storeZip = theReader["StoreZip"].ToString();
                        thisStore.storeState = theReader["StoreState"].ToString();
                        thisStore.storePhone = theReader["StorePhone"].ToString();
                        thisStore.storeManagerName = theReader["StoreManagerName"].ToString();
                        thisStore.storeEmailAddress = theReader["StoreEmail"].ToString();
                        thisStore.storeNumber = theReader["StoreNumber"].ToString();
                        thisStore.storeOwnershipType = theReader["StoreOwnershipType"].ToString();
                    }
                }
            }
            catch (Exception _exception)
            {

            }

            closeDataConnection();

            return thisStore;
        }

        public CDC GetCDCForStop(int stopID)
        {
            CDC thisCDC = null;

            openDataConnection();

            SqlCommand getCDC = new SqlCommand("GetCDCForStop", theConnection);
            getCDC.Parameters.AddWithValue("@stopID", stopID);
            getCDC.CommandType = System.Data.CommandType.StoredProcedure;

            try
            {
                theReader = getCDC.ExecuteReader();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        thisCDC = new CDC();

                        thisCDC.name = theReader["CDCName"].ToString();

                    }
                }
            }
            catch (Exception _exception)
            {

            }

            closeDataConnection();

            return thisCDC;
        }

        private bool savePhotoToDisk(string encodedImage, int photoID)
        {
            byte[] contents = Convert.FromBase64String(encodedImage);

            string currentPath = HttpContext.Current.Server.MapPath(".");
            string fileName = photoID + ".jpg";
            string finalPath = currentPath + "\\photos\\" + fileName;

            System.IO.File.WriteAllBytes(finalPath, contents);

            return true;
        }

        private bool validateDate(string aDate)
        {
            bool isDateValid = false;

            if (aDate != null && !aDate.Equals(""))
            {
                DateTime thisDate = Convert.ToDateTime(aDate);

                if (thisDate != null)
                {
                    isDateValid = true;
                }
            }

            return isDateValid;
        }


        public Response ReportStoreReadinessForSSCWithInterval(string startDate, string endDate)
        {
            if (validateDate(startDate) && validateDate(endDate))
            {
                if (endDate.Length <= 10)
                {
                    endDate += " 23:59:59";
                }

                Response theResponse = new Response();

                openDataConnection();

                /* Summary Data */

                SqlCommand cmdDVPRVPDeliveriesWithoutIssues = new SqlCommand("ReportNumberOfDeliveriesWithoutIssuesWithIntervalForSSC", theConnection);
                cmdDVPRVPDeliveriesWithoutIssues.Parameters.AddWithValue("@dateStarted", startDate);
                cmdDVPRVPDeliveriesWithoutIssues.Parameters.AddWithValue("@dateEnded", endDate);
                cmdDVPRVPDeliveriesWithoutIssues.CommandType = System.Data.CommandType.StoredProcedure;
                cmdDVPRVPDeliveriesWithoutIssues.CommandTimeout = 1200;

                theReader = cmdDVPRVPDeliveriesWithoutIssues.ExecuteReader();

                List<DVPRVPSummary> dvprvpData = new List<DVPRVPSummary>();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        DVPRVPSummary thisRow = new DVPRVPSummary();

                        thisRow.dvpName = theReader["ProviderName"].ToString();
                        thisRow.rvpName = theReader["CDCName"].ToString();
                        thisRow.deliveries = (int)theReader["NumberOfDeliveriesWithoutIssues"];

                        dvprvpData.Add(thisRow);
                    }
                }

                theReader.Close();

                SqlCommand cmdDVPRVPDeliveriesWithIssues = new SqlCommand("ReportNumberOfDeliveriesWithIssuesWithIntervalForSSC", theConnection);
                cmdDVPRVPDeliveriesWithIssues.Parameters.AddWithValue("@dateStarted", startDate);
                cmdDVPRVPDeliveriesWithIssues.Parameters.AddWithValue("@dateEnded", endDate);
                cmdDVPRVPDeliveriesWithIssues.CommandType = System.Data.CommandType.StoredProcedure;
                cmdDVPRVPDeliveriesWithIssues.CommandTimeout = 1200;

                theReader = cmdDVPRVPDeliveriesWithIssues.ExecuteReader();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        string thisRowsDVP = theReader["ProviderName"].ToString();
                        string thisRowsRVP = theReader["CDCName"].ToString();

                        bool found = false;

                        for (int i = 0, l = dvprvpData.Count; i < l; i++)
                        {
                            DVPRVPSummary thisRowData = dvprvpData[i];

                            if (thisRowsDVP.Equals(thisRowData.dvpName) && thisRowsRVP.Equals(thisRowData.rvpName))
                            {
                                found = true;

                                int deliveriesWithIssues = (int)theReader["NumberOfDeliveriesWithIssues"];

                                thisRowData.deliveriesWithIssues = deliveriesWithIssues;
                                thisRowData.deliveries += thisRowData.deliveriesWithIssues;
                            }
                        }

                        if (!found)
                        {
                            DVPRVPSummary thisRow = new DVPRVPSummary();

                            thisRow.dvpName = thisRowsDVP;
                            thisRow.rvpName = thisRowsRVP;
                            thisRow.deliveriesWithIssues = (int)theReader["NumberOfDeliveriesWithIssues"];
                            thisRow.deliveries += thisRow.deliveriesWithIssues;

                            dvprvpData.Add(thisRow);

                            found = false;
                        }
                    }
                }

                theReader.Close();

                SqlCommand cmdDVPRVPNumberOfIssues = new SqlCommand("ReportNumberOfIssuesWithIntervalForSSC", theConnection);
                cmdDVPRVPNumberOfIssues.Parameters.AddWithValue("@dateStarted", startDate);
                cmdDVPRVPNumberOfIssues.Parameters.AddWithValue("@dateEnded", endDate);
                cmdDVPRVPNumberOfIssues.CommandType = System.Data.CommandType.StoredProcedure;
                cmdDVPRVPNumberOfIssues.CommandTimeout = 1200;

                theReader = cmdDVPRVPNumberOfIssues.ExecuteReader();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        string thisRowsDVP = theReader["ProviderName"].ToString();
                        string thisRowsRVP = theReader["CDCName"].ToString();

                        bool found = false;

                        for (int i = 0, l = dvprvpData.Count; i < l; i++)
                        {
                            DVPRVPSummary thisRowData = dvprvpData[i];

                            if (thisRowsDVP.Equals(thisRowData.dvpName) && thisRowsRVP.Equals(thisRowData.rvpName))
                            {
                                found = true;

                                int numberOfIssues = (int)theReader["NumberOfIssues"];

                                thisRowData.totalReadinessIssues = numberOfIssues;
                            }
                        }

                        if (!found)
                        {
                            DVPRVPSummary thisRow = new DVPRVPSummary();

                            thisRow.dvpName = thisRowsDVP;
                            thisRow.rvpName = thisRowsRVP;
                            thisRow.totalReadinessIssues = (int)theReader["NumberOfIssues"];

                            dvprvpData.Add(thisRow);

                            found = false;
                        }
                    }
                }

                theReader.Close();

                SqlCommand cmdUnitsLeftout = new SqlCommand("ReportUnitsLeftoutWithIntervalForSSC", theConnection);
                cmdUnitsLeftout.Parameters.AddWithValue("@dateStarted", startDate);
                cmdUnitsLeftout.Parameters.AddWithValue("@dateEnded", endDate);
                cmdUnitsLeftout.CommandType = System.Data.CommandType.StoredProcedure;
                cmdUnitsLeftout.CommandTimeout = 1200;

                theReader = cmdUnitsLeftout.ExecuteReader();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        string thisRowsDVP = theReader["ProviderName"].ToString();
                        string thisRowsRVP = theReader["CDCName"].ToString();

                        bool found = false;

                        for (int i = 0, l = dvprvpData.Count; i < l; i++)
                        {
                            DVPRVPSummary thisRowData = dvprvpData[i];

                            if (thisRowsDVP.Equals(thisRowData.dvpName) && thisRowsRVP.Equals(thisRowData.rvpName))
                            {
                                found = true;

                                int unitsLeftout = (int)theReader["UnitsLeftout"];

                                thisRowData.leftoutUnits = unitsLeftout;
                            }
                        }

                        if (!found)
                        {
                            DVPRVPSummary thisRow = new DVPRVPSummary();

                            thisRow.dvpName = thisRowsDVP;
                            thisRow.rvpName = thisRowsRVP;
                            thisRow.leftoutUnits = (int)theReader["UnitsLeftout"];

                            dvprvpData.Add(thisRow);

                            found = false;
                        }
                    }
                }

                theReader.Close();

                SqlCommand cmdDVPRVPUnitsBackhauled = new SqlCommand("ReportUnitsBackhauledWithIntervalForSSC", theConnection);
                cmdDVPRVPUnitsBackhauled.Parameters.AddWithValue("@dateStarted", startDate);
                cmdDVPRVPUnitsBackhauled.Parameters.AddWithValue("@dateEnded", endDate);
                cmdDVPRVPUnitsBackhauled.CommandType = System.Data.CommandType.StoredProcedure;
                cmdDVPRVPUnitsBackhauled.CommandTimeout = 1200;

                theReader = cmdDVPRVPUnitsBackhauled.ExecuteReader();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        string thisRowsDVP = theReader["ProviderName"].ToString();
                        string thisRowsRVP = theReader["CDCName"].ToString();

                        bool found = false;

                        for (int i = 0, l = dvprvpData.Count; i < l; i++)
                        {
                            DVPRVPSummary thisRowData = dvprvpData[i];

                            if (thisRowsDVP.Equals(thisRowData.dvpName) && thisRowsRVP.Equals(thisRowData.rvpName))
                            {
                                found = true;

                                int unitsBackhauled = (int)theReader["UnitsBackhauled"];

                                thisRowData.dairyBackhaulUnits = unitsBackhauled;
                            }
                        }

                        if (!found)
                        {
                            DVPRVPSummary thisRow = new DVPRVPSummary();

                            thisRow.dvpName = thisRowsDVP;
                            thisRow.rvpName = thisRowsRVP;
                            thisRow.dairyBackhaulUnits = (int)theReader["UnitsBackhauled"];

                            dvprvpData.Add(thisRow);

                            found = false;
                        }
                    }
                }

                theReader.Close();

                SqlCommand cmdDVPRVPUnitsBackhauledCost = new SqlCommand("ReportUnitsBackhauledCostWithIntervalForSSC", theConnection);
                cmdDVPRVPUnitsBackhauledCost.Parameters.AddWithValue("@dateStarted", startDate);
                cmdDVPRVPUnitsBackhauledCost.Parameters.AddWithValue("@dateEnded", endDate);
                cmdDVPRVPUnitsBackhauledCost.CommandType = System.Data.CommandType.StoredProcedure;
                cmdDVPRVPUnitsBackhauledCost.CommandTimeout = 1200;

                theReader = cmdDVPRVPUnitsBackhauledCost.ExecuteReader();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        string thisRowsDVP = theReader["ProviderName"].ToString();
                        string thisRowsRVP = theReader["CDCName"].ToString();

                        bool found = false;

                        for (int i = 0, l = dvprvpData.Count; i < l; i++)
                        {
                            DVPRVPSummary thisRowData = dvprvpData[i];

                            if (thisRowsDVP.Equals(thisRowData.dvpName) && thisRowsRVP.Equals(thisRowData.rvpName))
                            {
                                found = true;

                                if (theReader["BackhaulCost"] != DBNull.Value)
                                {
                                    double backhaulCost = (double)theReader["BackhaulCost"];

                                    thisRowData.dairyBackhaulCOGS = backhaulCost;
                                }
                            }
                        }

                        if (!found)
                        {
                            DVPRVPSummary thisRow = new DVPRVPSummary();

                            thisRow.dvpName = thisRowsDVP;
                            thisRow.rvpName = thisRowsRVP;
                            thisRow.dairyBackhaulCOGS = (double)theReader["BackhaulCost"];

                            dvprvpData.Add(thisRow);

                            found = false;
                        }
                    }
                }

                theReader.Close();

                closeDataConnection();

                // try
                //{
                string newFilePath = CopyReportTemplate("ssc");

                string currentPath = HttpContext.Current.Server.MapPath(".");
                long currentTime = DateTime.Now.ToFileTimeUtc();

                string sourceFilename = "ssc.xlsx";
                string targetFilename;
                string targetPath;
                string sourcePath = currentPath + "\\templates\\";

                targetFilename = "report_ssc_" + currentTime + ".xlsx";
                targetPath = currentPath + "\\downloads\\";

                string sourceFile = System.IO.Path.Combine(sourcePath, sourceFilename);
                string destFile = System.IO.Path.Combine(targetPath, targetFilename);

                /* string oleConnectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + newFilePath + ";Extended Properties=\"Excel 8.0;\"";
                 OleDbConnection oleConnection = new OleDbConnection(oleConnectionString);
                 oleConnection.Open();*/

                openDataConnection();
                SqlCommand cmdReport = new SqlCommand("ReportStoresNotReadyWithInterval", theConnection);
                cmdReport.Parameters.AddWithValue("@dateStarted", startDate);
                cmdReport.Parameters.AddWithValue("@dateEnded", endDate);
                cmdReport.CommandType = System.Data.CommandType.StoredProcedure;
                cmdReport.CommandTimeout = 1200;

                theReader = cmdReport.ExecuteReader();

                FileStream template = new FileStream(newFilePath, FileMode.Open, FileAccess.Read);
                //FileStream fileRead = new FileStream(destFile, FileMode.Create, FileAccess.ReadWrite);
                //IWorkbook theWorkbook = new XSSFWorkbook(fileRead);
                //Create a stream of .xlsx file contained within my project using reflection


                //EPPlusTest = Namespace/Project
                //templates = folder
                //VendorTemplate.xlsx = file

                //ExcelPackage has a constructor that only requires a stream.
                ExcelPackage pck = new OfficeOpenXml.ExcelPackage(template);
                ExcelWorkbook workBook = pck.Workbook;
                //fileRead.Close();
                template.Close();

                if (theReader.HasRows)
                {
                    int rowIndex = 1;
                    //ISheet notReadySheet = theWorkbook.GetSheet("DetailedViewNotReady");
                    ExcelWorksheet notReadySheet = workBook.Worksheets["DetailedViewNotReady"];
                    //notReadySheet.ForceFormulaRecalculation = true;
                    string photoVal = "";
                    //IRow crow = null;

                    while (theReader.Read())
                    {
                        rowIndex++;
                        //crow = notReadySheet.CreateRow(rowIndex);
                        //DateTime todaysDateTime = DateTime.Today;
                        //DateTime thisEntrysDateTime = Convert.ToDateTime(theReader[0].ToString());
                        //TimeSpan timeDifference = todaysDateTime - thisEntrysDateTime;
                        //int differenceInDays = timeDifference.Days;
                        photoVal = "";
                        notReadySheet.Cells[rowIndex, 1].Value = theReader[0].ToString();
                        notReadySheet.Cells[rowIndex, 2].Value = theReader[1].ToString();
                        notReadySheet.Cells[rowIndex, 3].Value = theReader[2].ToString();
                        notReadySheet.Cells[rowIndex, 4].Value = theReader[3].ToString();
                        notReadySheet.Cells[rowIndex, 5].Value = theReader[4].ToString();
                        notReadySheet.Cells[rowIndex, 6].Value = theReader[9].ToString();
                        notReadySheet.Cells[rowIndex, 7].Value = theReader[10].ToString();
                        /*         notReadySheet.Cells[rowIndex, 8].Value = theReader[11].ToString();
                                 notReadySheet.Cells[rowIndex, 9].Value = getCellFriendlyText(theReader[12].ToString());
                                 if (differenceInDays <= 21 && theReader[13].ToString() != null && !theReader[13].ToString().Equals(""))
                                 {
                                     photoVal += ",'" + baseURLForPhotoLink + theReader[13].ToString() + "'";
                                 }
                                 else
                                 {
                                     photoVal += ",'No Photos Available'";
                                 }
                                 notReadySheet.Cells[rowIndex, 10].Value = photoVal;
                */
                        notReadySheet.Cells[rowIndex, 8].Value = theReader[11].ToString();
                        notReadySheet.Cells[rowIndex, 9].Value = Math.Round(Convert.ToDecimal(theReader[14].ToString()), 2).ToString();
                        notReadySheet.Cells[rowIndex, 10].Value = getCellFriendlyText(theReader[12].ToString());
                        if (theReader[13].ToString() != null && !theReader[13].ToString().Equals(""))
                        {
                            photoVal += "" + baseURLForPhotoLink + theReader[13].ToString() + "";
                        }
                        else
                        {
                            photoVal += "No Photos Available";
                        }
                        notReadySheet.Cells[rowIndex, 11].Value = photoVal;


                    }
                    theResponse.statusCode = 0;
                    theResponse.statusDescription = extractFilename(newFilePath);
                }
                else
                {
                    theResponse.statusCode = 1;
                    theResponse.statusDescription = "There is no data logged between the dates that were selected";
                }
                theReader.Close();

                SqlCommand cmdReportNotReady = new SqlCommand("ReportStoresReadyWithInterval", theConnection);
                cmdReportNotReady.Parameters.AddWithValue("@dateStarted", startDate);
                cmdReportNotReady.Parameters.AddWithValue("@dateEnded", endDate);
                cmdReportNotReady.CommandType = System.Data.CommandType.StoredProcedure;
                cmdReportNotReady.CommandTimeout = 1200;

                theReader = cmdReportNotReady.ExecuteReader();
                if (theReader.HasRows)
                {
                    int rowIndex = 1;
                    ExcelWorksheet readySheet = workBook.Worksheets["DetailedViewReady"];

                    while (theReader.Read())
                    {
                        rowIndex++;

                        readySheet.Cells[rowIndex, 1].Value = theReader[0].ToString();
                        readySheet.Cells[rowIndex, 2].Value = theReader[1].ToString();
                        readySheet.Cells[rowIndex, 3].Value = theReader[2].ToString();
                        readySheet.Cells[rowIndex, 4].Value = theReader[3].ToString();
                        readySheet.Cells[rowIndex, 5].Value = theReader[4].ToString();

                    }
                    theResponse.statusCode = 0;
                    theResponse.statusDescription = extractFilename(newFilePath);
                }
                else
                {
                    theResponse.statusCode = 1;
                    theResponse.statusDescription = "There is no data logged between the dates that were selected";
                }
                theReader.Close();
                /* if (theReader.HasRows)
                 {
                     while (theReader.Read())
                     {
                         //DateTime todaysDateTime = DateTime.Today;
                         //DateTime thisEntrysDateTime = Convert.ToDateTime(theReader[0].ToString());
                        // TimeSpan timeDifference = todaysDateTime - thisEntrysDateTime;
                         //int differenceInDays = timeDifference.Days;

                         string values = "'" + theReader[0] + "','" + theReader[1] + "','" + theReader[2] + "','" + theReader[3] + "','" + theReader[4] + "'";



                         OleDbCommand oleCommand = new OleDbCommand();
                         string sqlCommand = "INSERT INTO 	[DetailedViewReady$] (InStoreDate, StoreNumber, Provider, CDC, Route) VALUES (" + values + ")";
                         oleCommand.CommandText = sqlCommand;
                         oleCommand.Connection = oleConnection;
                         oleCommand.ExecuteNonQuery();
                     }

                     theResponse.statusCode = 0;
                     theResponse.statusDescription = newFilePath;
                 }
                 */
                // theReader.Close();

                //oleConnection.Close();


                ExcelWorksheet sheetSummary = workBook.Worksheets["Summary"];

                sheetSummary.Cells["A2:C2"].Merge = true;
                sheetSummary.Cells[2, 1].Value = startDate + " to " + endDate;

                if (dvprvpData != null && dvprvpData.Count > 0)
                {
                    int currentRowIndex = 3;

                    for (int i = 0, l = dvprvpData.Count; i < l; i++)
                    {
                        DVPRVPSummary thisRowsData = dvprvpData[i];

                        thisRowsData.percentageStoresReady = Math.Round((100 - ((double)thisRowsData.deliveriesWithIssues / (double)thisRowsData.deliveries) * 100.0), 2);

                        sheetSummary.Cells[currentRowIndex, 1].Value = thisRowsData.dvpName;
                        sheetSummary.Cells[currentRowIndex, 2].Value = thisRowsData.rvpName;
                        sheetSummary.Cells[currentRowIndex, 3].Value = thisRowsData.percentageStoresReady.ToString() + " %";
                        //              sheetSummary.Cells[currentRowIndex, 4].Value = thisRowsData.leftoutUnits;
                        //               sheetSummary.Cells[currentRowIndex, 5].Value = thisRowsData.dairyBackhaulUnits;
                        sheetSummary.Cells[currentRowIndex, 4].Value = thisRowsData.deliveries;
                        sheetSummary.Cells[currentRowIndex, 5].Value = thisRowsData.deliveriesWithIssues;
                        sheetSummary.Cells[currentRowIndex, 6].Value = thisRowsData.totalReadinessIssues;

                        currentRowIndex++;
                    }

                    theResponse.statusCode = 0;
                    theResponse.statusDescription = extractFilename(newFilePath);
                }
                else
                {
                    theResponse.statusCode = 1;
                    theResponse.statusDescription = "There is no data logged between the dates that were selected";
                }



                FileStream fileSave = new FileStream(newFilePath, FileMode.Create);
                //theWorkbook.Write(fileSave);
                pck.SaveAs(fileSave);
                fileSave.Close();

                closeDataConnection();
                /* }
                 catch (Exception _exception)
                 {
                     theResponse.statusCode = 6;
                     theResponse.statusDescription = _exception.Message + " " + _exception.StackTrace;
                 }*/

                return theResponse;
            }
            else
            {
                return ReportStoreReadinessForSSC();
            }
        }


        public Response ReportStoreReadinessForCDCWithInterval(string startDate, string endDate)
        {
            if (validateDate(startDate) && validateDate(endDate))
            {
                if (endDate.Length <= 10)
                {
                    endDate += " 23:59:59";
                }

                Response theResponse = new Response();

                openDataConnection();

                /* Summary Data */

                SqlCommand cmdDVPRVPDeliveriesWithoutIssues = new SqlCommand("ReportNumberOfDeliveriesWithoutIssuesWithIntervalForSSC", theConnection);
                cmdDVPRVPDeliveriesWithoutIssues.Parameters.AddWithValue("@dateStarted", startDate);
                cmdDVPRVPDeliveriesWithoutIssues.Parameters.AddWithValue("@dateEnded", endDate);
                cmdDVPRVPDeliveriesWithoutIssues.CommandType = System.Data.CommandType.StoredProcedure;
                cmdDVPRVPDeliveriesWithoutIssues.CommandTimeout = 1200;

                theReader = cmdDVPRVPDeliveriesWithoutIssues.ExecuteReader();

                List<DVPRVPSummary> dvprvpData = new List<DVPRVPSummary>();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        DVPRVPSummary thisRow = new DVPRVPSummary();

                        thisRow.dvpName = theReader["ProviderName"].ToString();
                        thisRow.rvpName = theReader["CDCName"].ToString();
                        thisRow.deliveries = (int)theReader["NumberOfDeliveriesWithoutIssues"];

                        dvprvpData.Add(thisRow);
                    }
                }

                theReader.Close();

                SqlCommand cmdDVPRVPDeliveriesWithIssues = new SqlCommand("ReportNumberOfDeliveriesWithIssuesWithIntervalForSSC", theConnection);
                cmdDVPRVPDeliveriesWithIssues.Parameters.AddWithValue("@dateStarted", startDate);
                cmdDVPRVPDeliveriesWithIssues.Parameters.AddWithValue("@dateEnded", endDate);
                cmdDVPRVPDeliveriesWithIssues.CommandType = System.Data.CommandType.StoredProcedure;
                cmdDVPRVPDeliveriesWithIssues.CommandTimeout = 1200;

                theReader = cmdDVPRVPDeliveriesWithIssues.ExecuteReader();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        string thisRowsDVP = theReader["ProviderName"].ToString();
                        string thisRowsRVP = theReader["CDCName"].ToString();

                        bool found = false;

                        for (int i = 0, l = dvprvpData.Count; i < l; i++)
                        {
                            DVPRVPSummary thisRowData = dvprvpData[i];

                            if (thisRowsDVP.Equals(thisRowData.dvpName) && thisRowsRVP.Equals(thisRowData.rvpName))
                            {
                                found = true;

                                int deliveriesWithIssues = (int)theReader["NumberOfDeliveriesWithIssues"];

                                thisRowData.deliveriesWithIssues = deliveriesWithIssues;
                                thisRowData.deliveries += thisRowData.deliveriesWithIssues;
                            }
                        }

                        if (!found)
                        {
                            DVPRVPSummary thisRow = new DVPRVPSummary();

                            thisRow.dvpName = thisRowsDVP;
                            thisRow.rvpName = thisRowsRVP;
                            thisRow.deliveriesWithIssues = (int)theReader["NumberOfDeliveriesWithIssues"];
                            thisRow.deliveries += thisRow.deliveriesWithIssues;

                            dvprvpData.Add(thisRow);

                            found = false;
                        }
                    }
                }

                theReader.Close();

                SqlCommand cmdDVPRVPNumberOfIssues = new SqlCommand("ReportNumberOfIssuesWithIntervalForSSC", theConnection);
                cmdDVPRVPNumberOfIssues.Parameters.AddWithValue("@dateStarted", startDate);
                cmdDVPRVPNumberOfIssues.Parameters.AddWithValue("@dateEnded", endDate);
                cmdDVPRVPNumberOfIssues.CommandType = System.Data.CommandType.StoredProcedure;
                cmdDVPRVPNumberOfIssues.CommandTimeout = 1200;

                theReader = cmdDVPRVPNumberOfIssues.ExecuteReader();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        string thisRowsDVP = theReader["ProviderName"].ToString();
                        string thisRowsRVP = theReader["CDCName"].ToString();

                        bool found = false;

                        for (int i = 0, l = dvprvpData.Count; i < l; i++)
                        {
                            DVPRVPSummary thisRowData = dvprvpData[i];

                            if (thisRowsDVP.Equals(thisRowData.dvpName) && thisRowsRVP.Equals(thisRowData.rvpName))
                            {
                                found = true;

                                int numberOfIssues = (int)theReader["NumberOfIssues"];

                                thisRowData.totalReadinessIssues = numberOfIssues;
                            }
                        }

                        if (!found)
                        {
                            DVPRVPSummary thisRow = new DVPRVPSummary();

                            thisRow.dvpName = thisRowsDVP;
                            thisRow.rvpName = thisRowsRVP;
                            thisRow.totalReadinessIssues = (int)theReader["NumberOfIssues"];

                            dvprvpData.Add(thisRow);

                            found = false;
                        }
                    }
                }

                theReader.Close();

                SqlCommand cmdUnitsLeftout = new SqlCommand("ReportUnitsLeftoutWithIntervalForSSC", theConnection);
                cmdUnitsLeftout.Parameters.AddWithValue("@dateStarted", startDate);
                cmdUnitsLeftout.Parameters.AddWithValue("@dateEnded", endDate);
                cmdUnitsLeftout.CommandType = System.Data.CommandType.StoredProcedure;
                cmdUnitsLeftout.CommandTimeout = 1200;

                theReader = cmdUnitsLeftout.ExecuteReader();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        string thisRowsDVP = theReader["ProviderName"].ToString();
                        string thisRowsRVP = theReader["CDCName"].ToString();

                        bool found = false;

                        for (int i = 0, l = dvprvpData.Count; i < l; i++)
                        {
                            DVPRVPSummary thisRowData = dvprvpData[i];

                            if (thisRowsDVP.Equals(thisRowData.dvpName) && thisRowsRVP.Equals(thisRowData.rvpName))
                            {
                                found = true;

                                int unitsLeftout = (int)theReader["UnitsLeftout"];

                                thisRowData.leftoutUnits = unitsLeftout;
                            }
                        }

                        if (!found)
                        {
                            DVPRVPSummary thisRow = new DVPRVPSummary();

                            thisRow.dvpName = thisRowsDVP;
                            thisRow.rvpName = thisRowsRVP;
                            thisRow.leftoutUnits = (int)theReader["UnitsLeftout"];

                            dvprvpData.Add(thisRow);

                            found = false;
                        }
                    }
                }

                theReader.Close();

                SqlCommand cmdDVPRVPUnitsBackhauled = new SqlCommand("ReportUnitsBackhauledWithIntervalForSSC", theConnection);
                cmdDVPRVPUnitsBackhauled.Parameters.AddWithValue("@dateStarted", startDate);
                cmdDVPRVPUnitsBackhauled.Parameters.AddWithValue("@dateEnded", endDate);
                cmdDVPRVPUnitsBackhauled.CommandType = System.Data.CommandType.StoredProcedure;
                cmdDVPRVPUnitsBackhauled.CommandTimeout = 1200;

                theReader = cmdDVPRVPUnitsBackhauled.ExecuteReader();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        string thisRowsDVP = theReader["ProviderName"].ToString();
                        string thisRowsRVP = theReader["CDCName"].ToString();

                        bool found = false;

                        for (int i = 0, l = dvprvpData.Count; i < l; i++)
                        {
                            DVPRVPSummary thisRowData = dvprvpData[i];

                            if (thisRowsDVP.Equals(thisRowData.dvpName) && thisRowsRVP.Equals(thisRowData.rvpName))
                            {
                                found = true;

                                int unitsBackhauled = (int)theReader["UnitsBackhauled"];

                                thisRowData.dairyBackhaulUnits = unitsBackhauled;
                            }
                        }

                        if (!found)
                        {
                            DVPRVPSummary thisRow = new DVPRVPSummary();

                            thisRow.dvpName = thisRowsDVP;
                            thisRow.rvpName = thisRowsRVP;
                            thisRow.dairyBackhaulUnits = (int)theReader["UnitsBackhauled"];

                            dvprvpData.Add(thisRow);

                            found = false;
                        }
                    }
                }

                theReader.Close();

                SqlCommand cmdDVPRVPUnitsBackhauledCost = new SqlCommand("ReportUnitsBackhauledCostWithIntervalForSSC", theConnection);
                cmdDVPRVPUnitsBackhauledCost.Parameters.AddWithValue("@dateStarted", startDate);
                cmdDVPRVPUnitsBackhauledCost.Parameters.AddWithValue("@dateEnded", endDate);
                cmdDVPRVPUnitsBackhauledCost.CommandType = System.Data.CommandType.StoredProcedure;
                cmdDVPRVPUnitsBackhauledCost.CommandTimeout = 1200;

                theReader = cmdDVPRVPUnitsBackhauledCost.ExecuteReader();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        string thisRowsDVP = theReader["ProviderName"].ToString();
                        string thisRowsRVP = theReader["CDCName"].ToString();

                        bool found = false;

                        for (int i = 0, l = dvprvpData.Count; i < l; i++)
                        {
                            DVPRVPSummary thisRowData = dvprvpData[i];

                            if (thisRowsDVP.Equals(thisRowData.dvpName) && thisRowsRVP.Equals(thisRowData.rvpName))
                            {
                                found = true;

                                if (theReader["BackhaulCost"] != DBNull.Value)
                                {
                                    double backhaulCost = (double)theReader["BackhaulCost"];

                                    thisRowData.dairyBackhaulCOGS = backhaulCost;
                                }
                            }
                        }

                        if (!found)
                        {
                            DVPRVPSummary thisRow = new DVPRVPSummary();

                            thisRow.dvpName = thisRowsDVP;
                            thisRow.rvpName = thisRowsRVP;
                            thisRow.dairyBackhaulCOGS = (double)theReader["BackhaulCost"];

                            dvprvpData.Add(thisRow);

                            found = false;
                        }
                    }
                }

                theReader.Close();

                closeDataConnection();

                try
                {
                    string newFilePath = CopyReportTemplate("cdc");

                    /* string oleConnectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + newFilePath + ";Extended Properties=\"Excel 8.0;\"";
                     OleDbConnection oleConnection = new OleDbConnection(oleConnectionString);
                     oleConnection.Open();*/

                    openDataConnection();
                    SqlCommand cmdReport = new SqlCommand("ReportStoresNotReadyWithInterval", theConnection);
                    cmdReport.Parameters.AddWithValue("@dateStarted", startDate);
                    cmdReport.Parameters.AddWithValue("@dateEnded", endDate);
                    cmdReport.CommandType = System.Data.CommandType.StoredProcedure;
                    cmdReport.CommandTimeout = 1200;

                    theReader = cmdReport.ExecuteReader();

                    FileStream template = new FileStream(newFilePath, FileMode.Open, FileAccess.Read);
                    //FileStream fileRead = new FileStream(destFile, FileMode.Create, FileAccess.ReadWrite);
                    //IWorkbook theWorkbook = new XSSFWorkbook(fileRead);
                    //Create a stream of .xlsx file contained within my project using reflection


                    //EPPlusTest = Namespace/Project
                    //templates = folder
                    //VendorTemplate.xlsx = file

                    //ExcelPackage has a constructor that only requires a stream.
                    ExcelPackage pck = new OfficeOpenXml.ExcelPackage(template);
                    ExcelWorkbook workBook = pck.Workbook;
                    //fileRead.Close();
                    template.Close();

                    if (theReader.HasRows)
                    {
                        int rowIndex = 1;
                        //ISheet notReadySheet = theWorkbook.GetSheet("DetailedViewNotReady");
                        ExcelWorksheet notReadySheet = workBook.Worksheets["DetailedViewNotReady"];
                        //notReadySheet.ForceFormulaRecalculation = true;
                        string photoVal = "";
                        //IRow crow = null;

                        while (theReader.Read())
                        {
                            rowIndex++;
                            //crow = notReadySheet.CreateRow(rowIndex);
                            //DateTime todaysDateTime = DateTime.Today;
                            //DateTime thisEntrysDateTime = Convert.ToDateTime(theReader[0].ToString());
                            //TimeSpan timeDifference = todaysDateTime - thisEntrysDateTime;
                            //int differenceInDays = timeDifference.Days;
                            photoVal = "";

                            notReadySheet.Cells[rowIndex, 1].Value = theReader[0].ToString();
                            notReadySheet.Cells[rowIndex, 2].Value = theReader[1].ToString();
                            notReadySheet.Cells[rowIndex, 3].Value = theReader[2].ToString();
                            notReadySheet.Cells[rowIndex, 4].Value = theReader[3].ToString();
                            notReadySheet.Cells[rowIndex, 5].Value = theReader[4].ToString();
                            notReadySheet.Cells[rowIndex, 6].Value = theReader[9].ToString();
                            notReadySheet.Cells[rowIndex, 7].Value = theReader[10].ToString();
                            notReadySheet.Cells[rowIndex, 8].Value = theReader[11].ToString();
                            notReadySheet.Cells[rowIndex, 9].Value = getCellFriendlyText(theReader[12].ToString());
                            if (theReader[13].ToString() != null && !theReader[13].ToString().Equals(""))
                            {
                                photoVal += "" + baseURLForPhotoLink + theReader[13].ToString() + "";
                            }
                            else
                            {
                                photoVal += "No Photos Available";
                            }
                            notReadySheet.Cells[rowIndex, 10].Value = photoVal;

                        }
                        theResponse.statusCode = 0;
                        theResponse.statusDescription = extractFilename(newFilePath);
                    }
                    else
                    {
                        theResponse.statusCode = 1;
                        theResponse.statusDescription = "There is no data logged between the dates that were selected";
                    }
                    theReader.Close();

                    SqlCommand cmdReportNotReady = new SqlCommand("ReportStoresReadyWithInterval", theConnection);
                    cmdReportNotReady.Parameters.AddWithValue("@dateStarted", startDate);
                    cmdReportNotReady.Parameters.AddWithValue("@dateEnded", endDate);
                    cmdReportNotReady.CommandType = System.Data.CommandType.StoredProcedure;
                    cmdReportNotReady.CommandTimeout = 1200;

                    theReader = cmdReportNotReady.ExecuteReader();
                    if (theReader.HasRows)
                    {
                        int rowIndex = 1;
                        ExcelWorksheet readySheet = workBook.Worksheets["DetailedViewReady"];

                        while (theReader.Read())
                        {
                            rowIndex++;

                            readySheet.Cells[rowIndex, 1].Value = theReader[0].ToString();
                            readySheet.Cells[rowIndex, 2].Value = theReader[1].ToString();
                            readySheet.Cells[rowIndex, 3].Value = theReader[2].ToString();
                            readySheet.Cells[rowIndex, 4].Value = theReader[3].ToString();
                            readySheet.Cells[rowIndex, 5].Value = theReader[4].ToString();

                        }
                        theResponse.statusCode = 0;
                        theResponse.statusDescription = extractFilename(newFilePath);
                    }
                    else
                    {
                        theResponse.statusCode = 1;
                        theResponse.statusDescription = "There is no data logged between the dates that were selected";
                    }
                    theReader.Close();

                    /* if (theReader.HasRows)
                     {
                         while (theReader.Read())
                         {
                             DateTime todaysDateTime = DateTime.Today;
                             DateTime thisEntrysDateTime = Convert.ToDateTime(theReader[0].ToString());
                             TimeSpan timeDifference = todaysDateTime - thisEntrysDateTime;
                             int differenceInDays = timeDifference.Days;

                             string values = "'" + theReader[0] + "','" + theReader[1] + "','" + theReader[2] + "','" + theReader[3] + "','" + theReader[4] + "','" + theReader[9] + "','" + theReader[10] + "','" + theReader[11] + "','" + getCellFriendlyText(theReader[12].ToString()) + "'";

                             if (differenceInDays <= 21 && theReader[13].ToString() != null && !theReader[13].ToString().Equals(""))
                             {
                                 values += ",'" + baseURLForPhotoLink + theReader[13].ToString() + "'";
                             }
                             else
                             {
                                 values += ",'No Photos Available'";
                             }

                             OleDbCommand oleCommand = new OleDbCommand();
                             string sqlCommand = "INSERT INTO [DetailedViewNotReady$] (InStoreDate, StoreNumber, Provider, CDC, Route, ReasonCode, Photos, UnitsBackhauled, Comments, PhotoLink) VALUES (" + values + ")";
                             oleCommand.CommandText = sqlCommand;
                             oleCommand.Connection = oleConnection;
                             oleCommand.ExecuteNonQuery();
                         }

                         theResponse.statusCode = 0;
                         theResponse.statusDescription = newFilePath;
                     }
                     else
                     {
                         theResponse.statusCode = 1;
                         theResponse.statusDescription = "There is no data logged between the dates that were selected";
                     }

                     theReader.Close();

                     SqlCommand cmdReportNotReady = new SqlCommand("ReportStoresReadyWithInterval", theConnection);
                     cmdReportNotReady.Parameters.AddWithValue("@dateStarted", startDate);
                     cmdReportNotReady.Parameters.AddWithValue("@dateEnded", endDate);
                     cmdReportNotReady.CommandType = System.Data.CommandType.StoredProcedure;
             cmdReportNotReady.CommandTimeout = 1200;

                     theReader = cmdReportNotReady.ExecuteReader();

                     if (theReader.HasRows)
                     {
                         while (theReader.Read())
                         {
                             //DateTime todaysDateTime = DateTime.Today;
                             //DateTime thisEntrysDateTime = Convert.ToDateTime(theReader[0].ToString());
                             //TimeSpan timeDifference = todaysDateTime - thisEntrysDateTime;
                             //int differenceInDays = timeDifference.Days;

                             string values = "'" + theReader[0] + "','" + theReader[1] + "','" + theReader[2] + "','" + theReader[3] + "','" + theReader[4] + "'";



                             OleDbCommand oleCommand = new OleDbCommand();
                             string sqlCommand = "INSERT INTO 	[DetailedViewReady$] (InStoreDate, StoreNumber, Provider, CDC, Route) VALUES (" + values + ")";
                             oleCommand.CommandText = sqlCommand;
                             oleCommand.Connection = oleConnection;
                             oleCommand.ExecuteNonQuery();
                         }

                         theResponse.statusCode = 0;
                         theResponse.statusDescription = newFilePath;
                     }

                     theReader.Close();

                     oleConnection.Close();*/



                    ExcelWorksheet sheetSummary = workBook.Worksheets["Summary"];

                    sheetSummary.Cells["A2:C2"].Merge = true;
                    sheetSummary.Cells[2, 1].Value = startDate + " to " + endDate;

                    if (dvprvpData != null && dvprvpData.Count > 0)
                    {
                        int currentRowIndex = 3;

                        for (int i = 0, l = dvprvpData.Count; i < l; i++)
                        {
                            DVPRVPSummary thisRowsData = dvprvpData[i];

                            thisRowsData.percentageStoresReady = Math.Round((100 - ((double)thisRowsData.deliveriesWithIssues / (double)thisRowsData.deliveries) * 100.0), 2);

                            sheetSummary.Cells[currentRowIndex, 1].Value = thisRowsData.dvpName;
                            sheetSummary.Cells[currentRowIndex, 2].Value = thisRowsData.rvpName;
                            sheetSummary.Cells[currentRowIndex, 3].Value = thisRowsData.percentageStoresReady.ToString() + " %";
                            //           sheetSummary.Cells[currentRowIndex, 4].Value = thisRowsData.leftoutUnits;
                            //           sheetSummary.Cells[currentRowIndex, 5].Value = thisRowsData.dairyBackhaulUnits;
                            sheetSummary.Cells[currentRowIndex, 4].Value = thisRowsData.deliveries;
                            sheetSummary.Cells[currentRowIndex, 5].Value = thisRowsData.deliveriesWithIssues;
                            sheetSummary.Cells[currentRowIndex, 6].Value = thisRowsData.totalReadinessIssues;

                            currentRowIndex++;
                        }

                        theResponse.statusCode = 0;
                        theResponse.statusDescription = extractFilename(newFilePath);
                    }
                    else
                    {
                        theResponse.statusCode = 1;
                        theResponse.statusDescription = "There is no data logged between the dates that were selected";
                    }



                    FileStream fileSave = new FileStream(newFilePath, FileMode.Create);
                    //theWorkbook.Write(fileSave);
                    pck.SaveAs(fileSave);
                    fileSave.Close();

                    closeDataConnection();
                }
                catch (Exception _exception)
                {
                    theResponse.statusCode = 6;
                    theResponse.statusDescription = _exception.Message;
                }

                return theResponse;
            }
            else
            {
                return ReportStoreReadinessForCDC();
            }
        }

        public Response ReportFieldReadinessWithInterval(string startDate, string endDate)
        {
            if (validateDate(startDate) && validateDate(endDate))
            {
                if (endDate.Length <= 10)
                {
                    endDate += " 23:59:59";
                }

                Response theResponse = new Response();

                openDataConnection();

                /* DVP - RVP Data */

                SqlCommand cmdDVPRVPDeliveriesWithoutIssues = new SqlCommand("ReportDVPRVPNumberOfDeliveriesWithoutIssuesWithInterval", theConnection);
                cmdDVPRVPDeliveriesWithoutIssues.Parameters.AddWithValue("@dateStarted", startDate);
                cmdDVPRVPDeliveriesWithoutIssues.Parameters.AddWithValue("@dateEnded", endDate);
                cmdDVPRVPDeliveriesWithoutIssues.CommandType = System.Data.CommandType.StoredProcedure;
                cmdDVPRVPDeliveriesWithoutIssues.CommandTimeout = 1200;

                theReader = cmdDVPRVPDeliveriesWithoutIssues.ExecuteReader();

                List<DVPRVPSummary> dvprvpData = new List<DVPRVPSummary>();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        DVPRVPSummary thisRow = new DVPRVPSummary();

                        thisRow.dvpName = theReader["DVPOutlookName"].ToString();
                        thisRow.rvpName = theReader["RVPOutlookName"].ToString();
                        thisRow.deliveries = (int)theReader["NumberOfDeliveriesWithoutIssues"];

                        dvprvpData.Add(thisRow);
                    }
                }

                theReader.Close();

                SqlCommand cmdDVPRVPDeliveriesWithIssues = new SqlCommand("ReportDVPRVPNumberOfDeliveriesWithIssuesWithInterval", theConnection);
                cmdDVPRVPDeliveriesWithIssues.Parameters.AddWithValue("@dateStarted", startDate);
                cmdDVPRVPDeliveriesWithIssues.Parameters.AddWithValue("@dateEnded", endDate);
                cmdDVPRVPDeliveriesWithIssues.CommandType = System.Data.CommandType.StoredProcedure;
                cmdDVPRVPDeliveriesWithIssues.CommandTimeout = 1200;

                theReader = cmdDVPRVPDeliveriesWithIssues.ExecuteReader();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        string thisRowsDVP = theReader["DVPOutlookName"].ToString();
                        string thisRowsRVP = theReader["RVPOutlookName"].ToString();

                        bool found = false;

                        for (int i = 0, l = dvprvpData.Count; i < l; i++)
                        {
                            DVPRVPSummary thisRowData = dvprvpData[i];

                            if (thisRowsDVP.Equals(thisRowData.dvpName) && thisRowsRVP.Equals(thisRowData.rvpName))
                            {
                                found = true;

                                int deliveriesWithIssues = (int)theReader["NumberOfDeliveriesWithIssues"];

                                thisRowData.deliveriesWithIssues = deliveriesWithIssues;
                                thisRowData.deliveries += thisRowData.deliveriesWithIssues;
                            }
                        }

                        if (!found)
                        {
                            DVPRVPSummary thisRow = new DVPRVPSummary();

                            thisRow.dvpName = thisRowsDVP;
                            thisRow.rvpName = thisRowsRVP;
                            thisRow.deliveriesWithIssues = (int)theReader["NumberOfDeliveriesWithIssues"];
                            thisRow.deliveries += thisRow.deliveriesWithIssues;

                            dvprvpData.Add(thisRow);

                            found = false;
                        }
                    }
                }

                theReader.Close();

                SqlCommand cmdDVPRVPNumberOfIssues = new SqlCommand("ReportDVPRVPNumberOfIssuesWithInterval", theConnection);
                cmdDVPRVPNumberOfIssues.Parameters.AddWithValue("@dateStarted", startDate);
                cmdDVPRVPNumberOfIssues.Parameters.AddWithValue("@dateEnded", endDate);
                cmdDVPRVPNumberOfIssues.CommandType = System.Data.CommandType.StoredProcedure;
                cmdDVPRVPNumberOfIssues.CommandTimeout = 1200;

                theReader = cmdDVPRVPNumberOfIssues.ExecuteReader();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        string thisRowsDVP = theReader["DVPOutlookName"].ToString();
                        string thisRowsRVP = theReader["RVPOutlookName"].ToString();

                        bool found = false;

                        for (int i = 0, l = dvprvpData.Count; i < l; i++)
                        {
                            DVPRVPSummary thisRowData = dvprvpData[i];

                            if (thisRowsDVP.Equals(thisRowData.dvpName) && thisRowsRVP.Equals(thisRowData.rvpName))
                            {
                                found = true;

                                int numberOfIssues = (int)theReader["NumberOfIssues"];

                                thisRowData.totalReadinessIssues = numberOfIssues;
                            }
                        }

                        if (!found)
                        {
                            DVPRVPSummary thisRow = new DVPRVPSummary();

                            thisRow.dvpName = thisRowsDVP;
                            thisRow.rvpName = thisRowsRVP;
                            thisRow.totalReadinessIssues = (int)theReader["NumberOfIssues"];

                            dvprvpData.Add(thisRow);

                            found = false;
                        }
                    }
                }

                theReader.Close();

                SqlCommand cmdDVPRVPUnitsLeftout = new SqlCommand("ReportDVPRVPUnitsLeftoutWithInterval", theConnection);
                cmdDVPRVPUnitsLeftout.Parameters.AddWithValue("@dateStarted", startDate);
                cmdDVPRVPUnitsLeftout.Parameters.AddWithValue("@dateEnded", endDate);
                cmdDVPRVPUnitsLeftout.CommandType = System.Data.CommandType.StoredProcedure;
                cmdDVPRVPUnitsLeftout.CommandTimeout = 1200;

                theReader = cmdDVPRVPUnitsLeftout.ExecuteReader();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        string thisRowsDVP = theReader["DVPOutlookName"].ToString();
                        string thisRowsRVP = theReader["RVPOutlookName"].ToString();

                        bool found = false;

                        for (int i = 0, l = dvprvpData.Count; i < l; i++)
                        {
                            DVPRVPSummary thisRowData = dvprvpData[i];

                            if (thisRowsDVP.Equals(thisRowData.dvpName) && thisRowsRVP.Equals(thisRowData.rvpName))
                            {
                                found = true;

                                int unitsLeftout = (int)theReader["UnitsLeftout"];

                                thisRowData.leftoutUnits = unitsLeftout;
                            }
                        }

                        if (!found)
                        {
                            DVPRVPSummary thisRow = new DVPRVPSummary();

                            thisRow.dvpName = thisRowsDVP;
                            thisRow.rvpName = thisRowsRVP;
                            thisRow.leftoutUnits = (int)theReader["UnitsLeftout"];

                            dvprvpData.Add(thisRow);

                            found = false;
                        }
                    }
                }

                theReader.Close();

                SqlCommand cmdDVPRVPUnitsLeftoutCost = new SqlCommand("ReportDVPRVPUnitsLeftoutCostWithInterval", theConnection);
                cmdDVPRVPUnitsLeftoutCost.Parameters.AddWithValue("@dateStarted", startDate);
                cmdDVPRVPUnitsLeftoutCost.Parameters.AddWithValue("@dateEnded", endDate);
                cmdDVPRVPUnitsLeftoutCost.CommandType = System.Data.CommandType.StoredProcedure;
                cmdDVPRVPUnitsLeftoutCost.CommandTimeout = 1200;

                theReader = cmdDVPRVPUnitsLeftoutCost.ExecuteReader();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        string thisRowsDVP = theReader["DVPOutlookName"].ToString();
                        string thisRowsRVP = theReader["RVPOutlookName"].ToString();

                        bool found = false;

                        for (int i = 0, l = dvprvpData.Count; i < l; i++)
                        {
                            DVPRVPSummary thisRowData = dvprvpData[i];

                            if (thisRowsDVP.Equals(thisRowData.dvpName) && thisRowsRVP.Equals(thisRowData.rvpName))
                            {
                                found = true;

                                if (theReader["LeftoutCost"] != DBNull.Value)
                                {
                                    double leftoutCost = (double)theReader["LeftoutCost"];

                                    thisRowData.leftoutCOGS = leftoutCost;
                                }
                            }
                        }

                        if (!found)
                        {
                            DVPRVPSummary thisRow = new DVPRVPSummary();

                            thisRow.dvpName = thisRowsDVP;
                            thisRow.rvpName = thisRowsRVP;
                            thisRow.leftoutCOGS = (double)theReader["LeftoutCost"];

                            dvprvpData.Add(thisRow);

                            found = false;
                        }
                    }
                }

                theReader.Close();

                SqlCommand cmdDVPRVPUnitsBackhauled = new SqlCommand("ReportDVPRVPUnitsBackhauledWithInterval", theConnection);
                cmdDVPRVPUnitsBackhauled.Parameters.AddWithValue("@dateStarted", startDate);
                cmdDVPRVPUnitsBackhauled.Parameters.AddWithValue("@dateEnded", endDate);
                cmdDVPRVPUnitsBackhauled.CommandType = System.Data.CommandType.StoredProcedure;
                cmdDVPRVPUnitsBackhauled.CommandTimeout = 1200;

                theReader = cmdDVPRVPUnitsBackhauled.ExecuteReader();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        string thisRowsDVP = theReader["DVPOutlookName"].ToString();
                        string thisRowsRVP = theReader["RVPOutlookName"].ToString();

                        bool found = false;

                        for (int i = 0, l = dvprvpData.Count; i < l; i++)
                        {
                            DVPRVPSummary thisRowData = dvprvpData[i];

                            if (thisRowsDVP.Equals(thisRowData.dvpName) && thisRowsRVP.Equals(thisRowData.rvpName))
                            {
                                found = true;

                                int unitsBackhauled = (int)theReader["UnitsBackhauled"];

                                thisRowData.dairyBackhaulUnits = unitsBackhauled;
                            }
                        }

                        if (!found)
                        {
                            DVPRVPSummary thisRow = new DVPRVPSummary();

                            thisRow.dvpName = thisRowsDVP;
                            thisRow.rvpName = thisRowsRVP;
                            thisRow.dairyBackhaulUnits = (int)theReader["UnitsBackhauled"];

                            dvprvpData.Add(thisRow);

                            found = false;
                        }
                    }
                }

                theReader.Close();

                SqlCommand cmdDVPRVPUnitsBackhauledCost = new SqlCommand("ReportDVPRVPUnitsBackhauledCostWithInterval", theConnection);
                cmdDVPRVPUnitsBackhauledCost.Parameters.AddWithValue("@dateStarted", startDate);
                cmdDVPRVPUnitsBackhauledCost.Parameters.AddWithValue("@dateEnded", endDate);
                cmdDVPRVPUnitsBackhauledCost.CommandType = System.Data.CommandType.StoredProcedure;
                cmdDVPRVPUnitsBackhauledCost.CommandTimeout = 1200;

                theReader = cmdDVPRVPUnitsBackhauledCost.ExecuteReader();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        string thisRowsDVP = theReader["DVPOutlookName"].ToString();
                        string thisRowsRVP = theReader["RVPOutlookName"].ToString();

                        bool found = false;

                        for (int i = 0, l = dvprvpData.Count; i < l; i++)
                        {
                            DVPRVPSummary thisRowData = dvprvpData[i];

                            if (thisRowsDVP.Equals(thisRowData.dvpName) && thisRowsRVP.Equals(thisRowData.rvpName))
                            {
                                found = true;

                                if (theReader["BackhaulCost"] != DBNull.Value)
                                {
                                    double backhaulCost = (double)theReader["BackhaulCost"];

                                    thisRowData.dairyBackhaulCOGS = backhaulCost;
                                }
                            }
                        }

                        if (!found)
                        {
                            DVPRVPSummary thisRow = new DVPRVPSummary();

                            thisRow.dvpName = thisRowsDVP;
                            thisRow.rvpName = thisRowsRVP;
                            thisRow.dairyBackhaulCOGS = (double)theReader["BackhaulCost"];

                            dvprvpData.Add(thisRow);

                            found = false;
                        }
                    }
                }

                theReader.Close();

                /* RD-DM Data */

                SqlCommand cmdRDDMDeliveriesWithoutIssues = new SqlCommand("ReportRDDMNumberOfDeliveriesWithoutIssuesWithInterval", theConnection);
                cmdRDDMDeliveriesWithoutIssues.Parameters.AddWithValue("@dateStarted", startDate);
                cmdRDDMDeliveriesWithoutIssues.Parameters.AddWithValue("@dateEnded", endDate);
                cmdRDDMDeliveriesWithoutIssues.CommandType = System.Data.CommandType.StoredProcedure;
                cmdRDDMDeliveriesWithoutIssues.CommandTimeout = 1200;

                theReader = cmdRDDMDeliveriesWithoutIssues.ExecuteReader();

                List<RDDMSummary> rddmData = new List<RDDMSummary>();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        RDDMSummary thisRow = new RDDMSummary();

                        thisRow.rdName = theReader["RDOutlookName"].ToString();
                        thisRow.dmName = theReader["DMOutlookName"].ToString();
                        thisRow.deliveries = (int)theReader["NumberOfDeliveriesWithoutIssues"];

                        rddmData.Add(thisRow);
                    }
                }

                theReader.Close();

                SqlCommand cmdRDDMDeliveriesWithIssues = new SqlCommand("ReportRDDMNumberOfDeliveriesWithIssuesWithInterval", theConnection);
                cmdRDDMDeliveriesWithIssues.Parameters.AddWithValue("@dateStarted", startDate);
                cmdRDDMDeliveriesWithIssues.Parameters.AddWithValue("@dateEnded", endDate);
                cmdRDDMDeliveriesWithIssues.CommandType = System.Data.CommandType.StoredProcedure;
                cmdRDDMDeliveriesWithIssues.CommandTimeout = 1200;

                theReader = cmdRDDMDeliveriesWithIssues.ExecuteReader();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        string thisRowsRD = theReader["RDOutlookName"].ToString();
                        string thisRowsDM = theReader["DMOutlookName"].ToString();

                        bool found = false;

                        for (int i = 0, l = rddmData.Count; i < l; i++)
                        {
                            RDDMSummary thisRowData = rddmData[i];

                            if (thisRowsRD.Equals(thisRowData.rdName) && thisRowsDM.Equals(thisRowData.dmName))
                            {
                                found = true;

                                int deliveriesWithIssues = (int)theReader["NumberOfDeliveriesWithIssues"];

                                thisRowData.deliveriesWithIssues = deliveriesWithIssues;
                                thisRowData.deliveries += thisRowData.deliveriesWithIssues;
                            }
                        }

                        if (!found)
                        {
                            RDDMSummary thisRow = new RDDMSummary();

                            thisRow.rdName = theReader["RDOutlookName"].ToString();
                            thisRow.dmName = theReader["DMOutlookName"].ToString();
                            thisRow.deliveriesWithIssues = (int)theReader["NumberOfDeliveriesWithIssues"];
                            thisRow.deliveries += thisRow.deliveriesWithIssues;

                            rddmData.Add(thisRow);

                            found = false;
                        }
                    }
                }

                theReader.Close();

                SqlCommand cmdRDDMNumberOfIssues = new SqlCommand("ReportRDDMNumberOfIssuesWithInterval", theConnection);
                cmdRDDMNumberOfIssues.Parameters.AddWithValue("@dateStarted", startDate);
                cmdRDDMNumberOfIssues.Parameters.AddWithValue("@dateEnded", endDate);
                cmdRDDMNumberOfIssues.CommandType = System.Data.CommandType.StoredProcedure;
                cmdRDDMNumberOfIssues.CommandTimeout = 1200;

                theReader = cmdRDDMNumberOfIssues.ExecuteReader();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        string thisRowsRD = theReader["RDOutlookName"].ToString();
                        string thisRowsDM = theReader["DMOutlookName"].ToString();

                        bool found = false;

                        for (int i = 0, l = rddmData.Count; i < l; i++)
                        {
                            RDDMSummary thisRowData = rddmData[i];

                            if (thisRowsRD.Equals(thisRowData.rdName) && thisRowsDM.Equals(thisRowData.dmName))
                            {
                                found = true;

                                int numberOfIssues = (int)theReader["NumberOfIssues"];

                                thisRowData.totalReadinessIssues = numberOfIssues;
                            }
                        }

                        if (!found)
                        {
                            RDDMSummary thisRow = new RDDMSummary();

                            thisRow.rdName = theReader["RDOutlookName"].ToString();
                            thisRow.dmName = theReader["DMOutlookName"].ToString();
                            thisRow.totalReadinessIssues = (int)theReader["NumberOfIssues"];

                            rddmData.Add(thisRow);

                            found = false;
                        }
                    }
                }

                theReader.Close();

                SqlCommand cmdRDDMUnitsBackhauled = new SqlCommand("ReportRDDMUnitsBackhauledWithInterval", theConnection);
                cmdRDDMUnitsBackhauled.Parameters.AddWithValue("@dateStarted", startDate);
                cmdRDDMUnitsBackhauled.Parameters.AddWithValue("@dateEnded", endDate);
                cmdRDDMUnitsBackhauled.CommandType = System.Data.CommandType.StoredProcedure;
                cmdRDDMUnitsBackhauled.CommandTimeout = 1200;

                theReader = cmdRDDMUnitsBackhauled.ExecuteReader();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        string thisRowsRD = theReader["RDOutlookName"].ToString();
                        string thisRowsDM = theReader["DMOutlookName"].ToString();

                        bool found = false;

                        for (int i = 0, l = rddmData.Count; i < l; i++)
                        {
                            RDDMSummary thisRowData = rddmData[i];

                            if (thisRowsRD.Equals(thisRowData.rdName) && thisRowsDM.Equals(thisRowData.dmName))
                            {
                                found = true;

                                int unitsBackhauled = (int)theReader["UnitsBackhauled"];

                                thisRowData.dairyBackhaulUnits = unitsBackhauled;
                            }
                        }

                        if (!found)
                        {
                            RDDMSummary thisRow = new RDDMSummary();

                            thisRow.rdName = theReader["RDOutlookName"].ToString();
                            thisRow.dmName = theReader["DMOutlookName"].ToString();
                            thisRow.dairyBackhaulUnits = (int)theReader["UnitsBackhauled"];

                            rddmData.Add(thisRow);

                            found = false;
                        }
                    }
                }

                theReader.Close();

                SqlCommand cmdRDDMUnitsBackhauledCost = new SqlCommand("ReportRDDMUnitsBackhauledCostWithInterval", theConnection);
                cmdRDDMUnitsBackhauledCost.Parameters.AddWithValue("@dateStarted", startDate);
                cmdRDDMUnitsBackhauledCost.Parameters.AddWithValue("@dateEnded", endDate);
                cmdRDDMUnitsBackhauledCost.CommandType = System.Data.CommandType.StoredProcedure;
                cmdRDDMUnitsBackhauledCost.CommandTimeout = 1200;

                theReader = cmdRDDMUnitsBackhauledCost.ExecuteReader();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        string thisRowsRD = theReader["RDOutlookName"].ToString();
                        string thisRowsDM = theReader["DMOutlookName"].ToString();

                        bool found = false;

                        for (int i = 0, l = rddmData.Count; i < l; i++)
                        {
                            RDDMSummary thisRowData = rddmData[i];

                            if (thisRowsRD.Equals(thisRowData.rdName) && thisRowsDM.Equals(thisRowData.dmName))
                            {
                                found = true;

                                if (theReader["BackhaulCost"] != DBNull.Value)
                                {
                                    double backhaulCost = (double)theReader["BackhaulCost"];

                                    thisRowData.dairyBackhaulCOGS = backhaulCost;
                                }
                            }
                        }

                        if (!found)
                        {
                            RDDMSummary thisRow = new RDDMSummary();

                            thisRow.rdName = theReader["RDOutlookName"].ToString();
                            thisRow.dmName = theReader["DMOutlookName"].ToString();
                            thisRow.dairyBackhaulCOGS = (double)theReader["BackhaulCost"];

                            rddmData.Add(thisRow);

                            found = false;
                        }
                    }
                }

                theReader.Close();

                SqlCommand cmdRDDMNumberOfIssuesOne = new SqlCommand("ReportRDDMGroupOneIssuesWithInterval", theConnection);
                cmdRDDMNumberOfIssuesOne.Parameters.AddWithValue("@dateStarted", startDate);
                cmdRDDMNumberOfIssuesOne.Parameters.AddWithValue("@dateEnded", endDate);
                cmdRDDMNumberOfIssuesOne.CommandType = System.Data.CommandType.StoredProcedure;
                cmdRDDMNumberOfIssuesOne.CommandTimeout = 1200;

                theReader = cmdRDDMNumberOfIssues.ExecuteReader();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        string thisRowsRD = theReader["RDOutlookName"].ToString();
                        string thisRowsDM = theReader["DMOutlookName"].ToString();

                        bool found = false;

                        for (int i = 0, l = rddmData.Count; i < l; i++)
                        {
                            RDDMSummary thisRowData = rddmData[i];

                            if (thisRowsRD.Equals(thisRowData.rdName) && thisRowsDM.Equals(thisRowData.dmName))
                            {
                                found = true;

                                int numberOfIssues = (int)theReader["NumberOfIssues"];

                                thisRowData.totalSecurityFacilityIssues = numberOfIssues;
                            }
                        }

                        if (!found)
                        {
                            RDDMSummary thisRow = new RDDMSummary();

                            thisRow.rdName = theReader["RDOutlookName"].ToString();
                            thisRow.dmName = theReader["DMOutlookName"].ToString();
                            thisRow.totalSecurityFacilityIssues = (int)theReader["NumberOfIssues"];

                            rddmData.Add(thisRow);

                            found = false;
                        }
                    }
                }

                theReader.Close();

                SqlCommand cmdRDDMNumberOfIssuesTwo = new SqlCommand("ReportRDDMGroupTwoIssuesWithInterval", theConnection);
                cmdRDDMNumberOfIssuesTwo.Parameters.AddWithValue("@dateStarted", startDate);
                cmdRDDMNumberOfIssuesTwo.Parameters.AddWithValue("@dateEnded", endDate);
                cmdRDDMNumberOfIssuesTwo.CommandType = System.Data.CommandType.StoredProcedure;
                cmdRDDMNumberOfIssuesTwo.CommandTimeout = 1200;

                theReader = cmdRDDMNumberOfIssuesTwo.ExecuteReader();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        string thisRowsRD = theReader["RDOutlookName"].ToString();
                        string thisRowsDM = theReader["DMOutlookName"].ToString();

                        bool found = false;

                        for (int i = 0, l = rddmData.Count; i < l; i++)
                        {
                            RDDMSummary thisRowData = rddmData[i];

                            if (thisRowsRD.Equals(thisRowData.rdName) && thisRowsDM.Equals(thisRowData.dmName))
                            {
                                found = true;

                                int numberOfIssues = (int)theReader["NumberOfIssues"];

                                thisRowData.totalCapacityIssues = numberOfIssues;
                            }
                        }

                        if (!found)
                        {
                            RDDMSummary thisRow = new RDDMSummary();

                            thisRow.rdName = theReader["RDOutlookName"].ToString();
                            thisRow.dmName = theReader["DMOutlookName"].ToString();
                            thisRow.totalCapacityIssues = (int)theReader["NumberOfIssues"];

                            rddmData.Add(thisRow);

                            found = false;
                        }
                    }
                }

                theReader.Close();

                SqlCommand cmdRDDMNumberOfIssuesThree = new SqlCommand("ReportRDDMGroupThreeIssuesWithInterval", theConnection);
                cmdRDDMNumberOfIssuesThree.Parameters.AddWithValue("@dateStarted", startDate);
                cmdRDDMNumberOfIssuesThree.Parameters.AddWithValue("@dateEnded", endDate);
                cmdRDDMNumberOfIssuesThree.CommandType = System.Data.CommandType.StoredProcedure;
                cmdRDDMNumberOfIssuesThree.CommandTimeout = 1200;

                theReader = cmdRDDMNumberOfIssuesThree.ExecuteReader();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        string thisRowsRD = theReader["RDOutlookName"].ToString();
                        string thisRowsDM = theReader["DMOutlookName"].ToString();

                        bool found = false;

                        for (int i = 0, l = rddmData.Count; i < l; i++)
                        {
                            RDDMSummary thisRowData = rddmData[i];

                            if (thisRowsRD.Equals(thisRowData.rdName) && thisRowsDM.Equals(thisRowData.dmName))
                            {
                                found = true;

                                int numberOfIssues = (int)theReader["NumberOfIssues"];

                                thisRowData.totalProductivityIssues = numberOfIssues;
                            }
                        }

                        if (!found)
                        {
                            RDDMSummary thisRow = new RDDMSummary();

                            thisRow.rdName = theReader["RDOutlookName"].ToString();
                            thisRow.dmName = theReader["DMOutlookName"].ToString();
                            thisRow.totalProductivityIssues = (int)theReader["NumberOfIssues"];

                            rddmData.Add(thisRow);

                            found = false;
                        }
                    }
                }

                theReader.Close();

                closeDataConnection();

                try
                {
                    string newFilePath = CopyReportTemplate("field");


                    /*string oleConnectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + newFilePath + ";Extended Properties=\"Excel 8.0;\"";
                    OleDbConnection oleConnection = new OleDbConnection(oleConnectionString);
                    oleConnection.Open();*/

                    openDataConnection();
                    SqlCommand cmdReport = new SqlCommand("ReportStoresNotReadyWithInterval", theConnection);
                    cmdReport.Parameters.AddWithValue("@dateStarted", startDate);
                    cmdReport.Parameters.AddWithValue("@dateEnded", endDate);
                    cmdReport.CommandType = System.Data.CommandType.StoredProcedure;
                    cmdReport.CommandTimeout = 1200;

                    theReader = cmdReport.ExecuteReader();

                    FileStream template = new FileStream(newFilePath, FileMode.Open, FileAccess.Read);
                    //FileStream fileRead = new FileStream(destFile, FileMode.Create, FileAccess.ReadWrite);
                    //IWorkbook theWorkbook = new XSSFWorkbook(fileRead);
                    //Create a stream of .xlsx file contained within my project using reflection


                    //EPPlusTest = Namespace/Project
                    //templates = folder
                    //VendorTemplate.xlsx = file

                    //ExcelPackage has a constructor that only requires a stream.
                    ExcelPackage pck = new OfficeOpenXml.ExcelPackage(template);
                    ExcelWorkbook workBook = pck.Workbook;
                    //fileRead.Close();
                    template.Close();

                    if (theReader.HasRows)
                    {
                        int rowIndex = 1;
                        //ISheet notReadySheet = theWorkbook.GetSheet("DetailedViewNotReady");
                        ExcelWorksheet notReadySheet = workBook.Worksheets["DetailedViewNotReady"];
                        //notReadySheet.ForceFormulaRecalculation = true;
                        string photoVal = "";
                        //IRow crow = null;

                        while (theReader.Read())
                        {
                            rowIndex++;
                            //crow = notReadySheet.CreateRow(rowIndex);
                            //DateTime todaysDateTime = DateTime.Today;
                            //DateTime thisEntrysDateTime = Convert.ToDateTime(theReader[0].ToString());
                            //TimeSpan timeDifference = todaysDateTime - thisEntrysDateTime;
                            //int differenceInDays = timeDifference.Days;
                            photoVal = "";
                            notReadySheet.Cells[rowIndex, 1].Value = theReader[0].ToString();
                            notReadySheet.Cells[rowIndex, 2].Value = theReader[1].ToString();
                            notReadySheet.Cells[rowIndex, 3].Value = theReader[2].ToString();
                            notReadySheet.Cells[rowIndex, 4].Value = theReader[3].ToString();
                            notReadySheet.Cells[rowIndex, 5].Value = theReader[4].ToString();
                            notReadySheet.Cells[rowIndex, 6].Value = theReader[5].ToString();
                            notReadySheet.Cells[rowIndex, 7].Value = theReader[6].ToString();
                            notReadySheet.Cells[rowIndex, 8].Value = theReader[7].ToString();
                            notReadySheet.Cells[rowIndex, 9].Value = theReader[8].ToString();
                            notReadySheet.Cells[rowIndex, 10].Value = theReader[9].ToString();
                            notReadySheet.Cells[rowIndex, 11].Value = theReader[10].ToString();
                            /*           notReadySheet.Cells[rowIndex, 12].Value = theReader[11].ToString();
                                       notReadySheet.Cells[rowIndex, 13].Value = getCellFriendlyText(theReader[12].ToString());
                                       if (differenceInDays <= 21 && theReader[13].ToString() != null && !theReader[13].ToString().Equals(""))
                                       {
                                           photoVal += ",'" + baseURLForPhotoLink + theReader[13].ToString() + "'";
                                       }
                                       else
                                       {
                                           photoVal += ",'No Photos Available'";
                                       }
                                       notReadySheet.Cells[rowIndex, 14].Value = photoVal;
                   */

                            notReadySheet.Cells[rowIndex, 12].Value = theReader[11].ToString();
                            notReadySheet.Cells[rowIndex, 13].Value = Math.Round(Convert.ToDecimal(theReader[14].ToString()), 2).ToString();
                            notReadySheet.Cells[rowIndex, 14].Value = getCellFriendlyText(theReader[12].ToString());
                            if (theReader[13].ToString() != null && !theReader[13].ToString().Equals(""))
                            {
                                photoVal += "" + baseURLForPhotoLink + theReader[13].ToString() + "";
                            }
                            else
                            {
                                photoVal += "No Photos Available";
                            }
                            notReadySheet.Cells[rowIndex, 15].Value = photoVal;


                        }
                        theResponse.statusCode = 0;
                        theResponse.statusDescription = extractFilename(newFilePath);
                    }
                    else
                    {
                        theResponse.statusCode = 1;
                        theResponse.statusDescription = "There is no data logged between the dates that were selected";
                    }
                    theReader.Close();



                    SqlCommand cmdReportNotReady = new SqlCommand("ReportStoresReadyWithInterval", theConnection);
                    cmdReportNotReady.Parameters.AddWithValue("@dateStarted", startDate);
                    cmdReportNotReady.Parameters.AddWithValue("@dateEnded", endDate);
                    cmdReportNotReady.CommandType = System.Data.CommandType.StoredProcedure;
                    cmdReportNotReady.CommandTimeout = 1200;

                    theReader = cmdReportNotReady.ExecuteReader();
                    if (theReader.HasRows)
                    {
                        int rowIndex = 1;
                        ExcelWorksheet readySheet = workBook.Worksheets["DetailedViewReady"];

                        while (theReader.Read())
                        {
                            rowIndex++;

                            readySheet.Cells[rowIndex, 1].Value = theReader[0].ToString();
                            readySheet.Cells[rowIndex, 2].Value = theReader[1].ToString();
                            readySheet.Cells[rowIndex, 3].Value = theReader[2].ToString();
                            readySheet.Cells[rowIndex, 4].Value = theReader[3].ToString();
                            readySheet.Cells[rowIndex, 5].Value = theReader[4].ToString();

                        }
                        theResponse.statusCode = 0;
                        theResponse.statusDescription = extractFilename(newFilePath);
                    }
                    else
                    {
                        theResponse.statusCode = 1;
                        theResponse.statusDescription = "There is no data logged between the dates that were selected";
                    }
                    theReader.Close();

                    /*if (theReader.HasRows)
                    {
                        while (theReader.Read())
                        {
                            DateTime todaysDateTime = DateTime.Today;
                            DateTime thisEntrysDateTime = Convert.ToDateTime(theReader[0].ToString());
                            TimeSpan timeDifference = todaysDateTime - thisEntrysDateTime;
                            int differenceInDays = timeDifference.Days;

                            string values = "'" + theReader[0] + "','" + theReader[1] + "','" + theReader[2] + "','" + theReader[3] + "','" + theReader[4] + "','" + theReader[5] + "','" + theReader[6] + "','" + theReader[7] + "','" + theReader[8] + "','" + theReader[9] + "','" + theReader[10] + "','" + theReader[11] + "','" + getCellFriendlyText(theReader[12].ToString()) + "'";

                            if (differenceInDays <= 21 && theReader[13].ToString() != null && !theReader[13].ToString().Equals(""))
                            {
                                values += ",'" + baseURLForPhotoLink + theReader[13].ToString() + "'";
                            }
                            else
                            {
                                values += ",'No Photos Available'";
                            }

                            OleDbCommand oleCommand = new OleDbCommand();
                            string sqlCommand = "INSERT INTO [DetailedViewNotReady$] (InStoreDate, StoreNumber, Provider, CDC, Route, DSVP, RVP, RD, DM, ReasonCode, Photos, UnitsBackhauled, Comments, PhotoLink) VALUES (" + values + ")";
                            oleCommand.CommandText = sqlCommand;
                            oleCommand.Connection = oleConnection;
                            oleCommand.ExecuteNonQuery();
                        }

                        theResponse.statusCode = 0;
                        theResponse.statusDescription = newFilePath;
                    }
                    else
                    {
                        theResponse.statusCode = 1;
                        theResponse.statusDescription = "There is no data logged between the dates that were selected";
                    }

                    theReader.Close();

                    SqlCommand cmdReportNotReady = new SqlCommand("ReportStoresReadyWithInterval", theConnection);
                    cmdReportNotReady.Parameters.AddWithValue("@dateStarted", startDate);
                    cmdReportNotReady.Parameters.AddWithValue("@dateEnded", endDate);
                    cmdReportNotReady.CommandType = System.Data.CommandType.StoredProcedure;

                    theReader = cmdReportNotReady.ExecuteReader();

                    if (theReader.HasRows)
                    {
                        while (theReader.Read())
                        {
                           /* DateTime todaysDateTime = DateTime.Today;
                            DateTime thisEntrysDateTime = Convert.ToDateTime(theReader[0].ToString());
                            TimeSpan timeDifference = todaysDateTime - thisEntrysDateTime;
                            int differenceInDays = timeDifference.Days;

                            string values = "'" + theReader[0] + "','" + theReader[1] + "','" + theReader[2] + "','" + theReader[3] + "','" + theReader[4] + "'";



                            OleDbCommand oleCommand = new OleDbCommand();
                            string sqlCommand = "INSERT INTO 	[DetailedViewReady$] (InStoreDate, StoreNumber, Provider, CDC, Route) VALUES (" + values + ")";
                            oleCommand.CommandText = sqlCommand;
                            oleCommand.Connection = oleConnection;
                            oleCommand.ExecuteNonQuery();
                        }

                        theResponse.statusCode = 0;
                        theResponse.statusDescription = newFilePath;
                    }

                    theReader.Close();

                    oleConnection.Close();*/



                    ExcelWorksheet sheetSummary = workBook.Worksheets["DVPRVPSummary"];

                    sheetSummary.Cells["A2:C2"].Merge = true;
                    sheetSummary.Cells[2, 1].Value = "All Data";

                    if (dvprvpData != null && dvprvpData.Count > 0)
                    {
                        int currentRowIndex = 3;


                        for (int i = 0, l = dvprvpData.Count; i < l; i++)
                        {
                            DVPRVPSummary thisRowsData = dvprvpData[i];

                            thisRowsData.percentageStoresReady = Math.Round((100 - ((double)thisRowsData.deliveriesWithIssues / (double)thisRowsData.deliveries) * 100.0), 2);
                            thisRowsData.leftoutCOGS = Math.Round(thisRowsData.leftoutCOGS, 2);
                            thisRowsData.dairyBackhaulCOGS = Math.Round(thisRowsData.dairyBackhaulCOGS, 2);


                            sheetSummary.Cells[currentRowIndex, 1].Value = thisRowsData.dvpName;
                            sheetSummary.Cells[currentRowIndex, 2].Value = thisRowsData.rvpName;
                            sheetSummary.Cells[currentRowIndex, 3].Value = thisRowsData.percentageStoresReady.ToString() + " %";
                            //         sheetSummary.Cells[currentRowIndex, 4].Value = thisRowsData.leftoutUnits;
                            //         sheetSummary.Cells[currentRowIndex, 5].Value = "$ " + thisRowsData.leftoutCOGS.ToString();
                            //         sheetSummary.Cells[currentRowIndex, 6].Value = thisRowsData.dairyBackhaulUnits;
                            sheetSummary.Cells[currentRowIndex, 4].Value = "$ " + thisRowsData.dairyBackhaulCOGS.ToString();
                            sheetSummary.Cells[currentRowIndex, 5].Value = thisRowsData.deliveries;
                            sheetSummary.Cells[currentRowIndex, 6].Value = thisRowsData.deliveriesWithIssues;
                            sheetSummary.Cells[currentRowIndex, 7].Value = thisRowsData.totalReadinessIssues;

                            currentRowIndex++;
                        }

                        theResponse.statusCode = 0;
                        theResponse.statusDescription = extractFilename(newFilePath);
                    }
                    else
                    {
                        theResponse.statusCode = 1;
                        theResponse.statusDescription = "There is no data logged between the dates that were selected";
                    }


                    ExcelWorksheet sheetRDDMSummary = workBook.Worksheets["RDDMSummary"];

                    sheetRDDMSummary.Cells["A2:C2"].Merge = true;
                    sheetRDDMSummary.Cells[2, 1].Value = "All Data";

                    if (rddmData != null && rddmData.Count > 0)
                    {
                        int currentRowIndex = 3;

                        //IRow currentRow = null;

                        for (int i = 0, l = rddmData.Count; i < l; i++)
                        {
                            RDDMSummary thisRowsData = rddmData[i];

                            thisRowsData.percentageStoresReady = Math.Round((100 - ((double)thisRowsData.deliveriesWithIssues / (double)thisRowsData.deliveries) * 100.0), 2);

                            thisRowsData.dairyBackhaulCOGS = Math.Round(thisRowsData.dairyBackhaulCOGS, 2);
                            // currentRow = sheetRDDMSummary.CreateRow(currentRowIndex);

                            sheetRDDMSummary.Cells[currentRowIndex, 1].Value = thisRowsData.rdName;
                            sheetRDDMSummary.Cells[currentRowIndex, 2].Value = thisRowsData.dmName;
                            sheetRDDMSummary.Cells[currentRowIndex, 3].Value = thisRowsData.percentageStoresReady.ToString() + " %";
                            //        sheetRDDMSummary.Cells[currentRowIndex, 4].Value = thisRowsData.dairyBackhaulUnits;
                            sheetRDDMSummary.Cells[currentRowIndex, 4].Value = "$ " + thisRowsData.dairyBackhaulCOGS.ToString();
                            sheetRDDMSummary.Cells[currentRowIndex, 5].Value = thisRowsData.deliveries;
                            sheetRDDMSummary.Cells[currentRowIndex, 6].Value = thisRowsData.deliveriesWithIssues;
                            sheetRDDMSummary.Cells[currentRowIndex, 7].Value = thisRowsData.totalReadinessIssues;
                            sheetRDDMSummary.Cells[currentRowIndex, 8].Value = thisRowsData.totalSecurityFacilityIssues;
                            sheetRDDMSummary.Cells[currentRowIndex, 9].Value = thisRowsData.totalCapacityIssues;
                            sheetRDDMSummary.Cells[currentRowIndex, 10].Value = thisRowsData.totalProductivityIssues;

                            currentRowIndex++;
                        }

                        theResponse.statusCode = 0;
                        theResponse.statusDescription = extractFilename(newFilePath);
                    }
                    else
                    {
                        theResponse.statusCode = 1;
                        theResponse.statusDescription = "There is no data logged between the dates that were selected";
                    }

                    FileStream fileSave = new FileStream(newFilePath, FileMode.Create);
                    //theWorkbook.Write(fileSave);
                    pck.SaveAs(fileSave);
                    fileSave.Close();

                    closeDataConnection();
                }
                catch (Exception _exception)
                {
                    theResponse.statusCode = 6;
                    theResponse.statusDescription = _exception.Message + " " + _exception.StackTrace;
                }

                return theResponse;
            }
            else
            {
                return ReportFieldReadiness();
            }
        }
        private string getFilteredText(string someText)
        {
            Regex wordFilter = new Regex("(damn|shit|fuck|fucking|assfuck|motherfucker|crap|idiot|ass|asshole|jackass|bitch|bitchy|bullshit|bastard|jerk|cock|cunt|queer|whore|witch)");
            return wordFilter.Replace(someText, "");
        }

        private string getCellFriendlyText(string someText)
        {
            Regex wordFilter = new Regex("(')");

            string filteredString = wordFilter.Replace(someText, "");

            string shortenedString = filteredString;

            if (filteredString.Length > 255)
            {
                shortenedString = filteredString.Substring(0, 255);
            }

            return shortenedString;
        }

        private string replaceCommasWithSemiColons(string someText)
        {
            Regex wordFilter = new Regex("(,)");
            return wordFilter.Replace(someText, ";");
        }

        public Stream ViewPhotos(string photoIDs)
        {
            string[] photoIDsArray = photoIDs.Split(',');

            string theResponse = "";

            openDataConnection();

            for (int i = 0, l = photoIDsArray.Count(); i < l; i++)
            {
                SqlCommand cmdGetPhoto = new SqlCommand("GetPhotoByID", theConnection);
                cmdGetPhoto.Parameters.AddWithValue("@photoID", Int32.Parse(photoIDsArray[i]));
                cmdGetPhoto.CommandType = System.Data.CommandType.StoredProcedure;

                theReader = cmdGetPhoto.ExecuteReader();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        theResponse += "<p><img src=\"data:image/png;base64," + theReader["Photo"].ToString() + "\" alt=\"" + photoIDsArray[i] + "\" /></p>";
                    }
                }
                else
                {
                    theResponse += "<p>No Photos Found</p>";
                }

                theReader.Close();
            }

            closeDataConnection();

            byte[] theResponseInBytes = Encoding.UTF8.GetBytes(theResponse);

            WebOperationContext.Current.OutgoingResponse.ContentType = "text/html";

            return new MemoryStream(theResponseInBytes);
        }

        public Response ReportStoreReadinessForSSCWithIntervalHours(string startDate, string endDate, string startHour, string endHour)
        {
            string startDateWithHour = startDate + " " + startHour + ":00:00";
            string endDateWithHour = endDate + " " + endHour + ":00:00";

            return ReportStoreReadinessForSSCWithInterval(startDateWithHour, endDateWithHour);
        }

        public Response ReportStoreReadinessForCDCWithIntervalHours(string startDate, string endDate, string startHour, string endHour)
        {
            string startDateWithHour = startDate + " " + startHour + ":00:00";
            string endDateWithHour = endDate + " " + endHour + ":00:00";

            return ReportStoreReadinessForCDCWithInterval(startDateWithHour, endDateWithHour);
        }

        public Response ReportStoreReadinessForCDCForProviderWithIntervalHours(string providerID, string startDate, string endDate, string startHour, string endHour)
        {
            string startDateWithHour = startDate + " " + startHour + ":00:00";
            string endDateWithHour = endDate + " " + endHour + ":00:00";

            return ReportStoreReadinessForCDCForProviderWithInterval(providerID, startDateWithHour, endDateWithHour);
        }

        public Response ReportFieldReadinessWithIntervalHours(string startDate, string endDate, string startHour, string endHour)
        {
            string startDateWithHour = startDate + " " + startHour + ":00:00";
            string endDateWithHour = endDate + " " + endHour + ":00:00";

            return ReportFieldReadinessWithInterval(startDateWithHour, endDateWithHour);
        }

        const int _COLUMN_BASE = 26;
        const int _DIGIT_MAX = 7;
        const string _DIGITS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        private static string getColumnName(int index)
        {
            if (index <= 0)
                throw new IndexOutOfRangeException("index must be a positive number");

            if (index <= _COLUMN_BASE)
                return _DIGITS[index - 1].ToString();

            var sb = new StringBuilder().Append(' ', _DIGIT_MAX);
            var current = index;
            var offset = _DIGIT_MAX;
            while (current > 0)
            {
                sb[--offset] = _DIGITS[--current % _COLUMN_BASE];
                current /= _COLUMN_BASE;
            }
            return sb.ToString(offset, _DIGIT_MAX - offset);
        }

        private int getGoodDeliveriesByProviderAndDate(string providerName, string date, string startHour, string endHour)
        {
            int numGoodDeliveries = 0;

            openDataConnection();

            string startTime = "";
            string endTime;

            string startDate = date;
            string endDate = date;

            if (startHour.Equals(""))
            {
                startTime = "00:00:00";
            }
            else
            {
                startTime = startHour + ":00:00";
            }

            if (endHour.Equals(""))
            {
                endTime = "23:59:59";
            }
            else
            {
                endTime = endHour + ":00:00";
            }

            startDate += " " + startTime;
            endDate += " " + endTime;

            SqlCommand cmdGet = new SqlCommand("ReportCompletedDeliveriesByProviderAndDate", theConnection);
            cmdGet.Parameters.AddWithValue("@providerName", providerName);
            cmdGet.Parameters.AddWithValue("@dateStarted", startDate);
            cmdGet.Parameters.AddWithValue("@dateEnded", endDate);
            cmdGet.CommandType = System.Data.CommandType.StoredProcedure;

            theReader = cmdGet.ExecuteReader();

            if (theReader.HasRows)
            {
                while (theReader.Read())
                {
                    numGoodDeliveries = (int)theReader[0];
                }
            }

            theReader.Close();

            closeDataConnection();

            return numGoodDeliveries;
        }

        private int getGoodDeliveriesByCDCAndDate(string cdcName, string date, string startHour, string endHour)
        {
            int numGoodDeliveries = 0;

            openDataConnection();

            string startTime = "";
            string endTime;

            string startDate = date;
            string endDate = date;

            if (startHour.Equals(""))
            {
                startTime = "00:00:00";
            }
            else
            {
                startTime = startHour + ":00:00";
            }

            if (endHour.Equals(""))
            {
                endTime = "23:59:59";
            }
            else
            {
                endTime = endHour + ":00:00";
            }

            startDate += " " + startTime;
            endDate += " " + endTime;

            SqlCommand cmdGet = new SqlCommand("ReportCompletedDeliveriesByCDCAndDate", theConnection);
            cmdGet.Parameters.AddWithValue("@cdcName", cdcName);
            cmdGet.Parameters.AddWithValue("@dateStarted", startDate);
            cmdGet.Parameters.AddWithValue("@dateEnded", endDate);
            cmdGet.CommandType = System.Data.CommandType.StoredProcedure;

            theReader = cmdGet.ExecuteReader();

            if (theReader.HasRows)
            {
                while (theReader.Read())
                {
                    numGoodDeliveries = (int)theReader[0];
                }
            }

            theReader.Close();

            closeDataConnection();

            return numGoodDeliveries;
        }

        private bool SendEmailForUploadErrors(string moduleName, string username, string errors, string cdcName)
        {
            bool emailSent = false;

            try
            {
                SmtpClient client = new SmtpClient();
                client.Port = 587;
                client.Host = "smtp.gmail.com";
                client.EnableSsl = true;
                client.Timeout = 10000;
                client.DeliveryMethod = SmtpDeliveryMethod.Network;
                client.UseDefaultCredentials = false;

                string userType = "";
                if (cdcName == "")
                    userType = GetUserCDCByUserName(username);
                else
                    userType = cdcName;

                client.Credentials = new System.Net.NetworkCredential("storereadiness@gmail.com", "sbuxreadyy");

                string textBody = "<p>Hello Administrator,<br />";
                textBody += "<p>User " + username + " " + userType + " was trying to batch upload " + moduleName + " and ran into the following errors:";
                textBody += "<p>" + errors + "</p>";
                textBody += "<p>Sbux Ready Admin Panel</p>";

                List<MailAddress> ccList = new List<MailAddress>();

                string cdcEmailTo = replaceCommasWithSemiColons("sdreadiness@starbucks.com");
                string[] cdcEmails = cdcEmailTo.Split(';');
                if (cdcEmails.Count() > 0)
                {
                    cdcEmailTo = cdcEmails[0];

                    for (int i = 1, l = cdcEmails.Count(); i < l; i++)
                    {
                        ccList.Add(new MailAddress(cdcEmails[i]));
                    }
                }

                MailMessage mm = new MailMessage(new MailAddress("storereadiness@gmail.com", "Store Readiness"), new MailAddress(cdcEmailTo, "Sbux Ready Admin"));

                for (int i = 0, l = ccList.Count(); i < l; i++)
                {
                    mm.CC.Add(ccList[i]);
                }

                mm.Subject = "Admin Panel Batch Upload Error";
                mm.Body = textBody;
                mm.BodyEncoding = UTF8Encoding.UTF8;
                mm.IsBodyHtml = true;
                mm.DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure;

                client.Send(mm);

                emailSent = true;
            }
            catch (Exception _exception)
            {
                emailSent = false;
            }

            return emailSent;
        }

        private string GetUserCDCByUserName(string username)
        {
            openDataConnection();
            int associatedId = 0;
            SqlCommand cmdGet = new SqlCommand("select * from UserAssociation where Username = @username", theConnection);
            cmdGet.Parameters.AddWithValue("@username", username);
            cmdGet.CommandType = System.Data.CommandType.Text;

            theReader = cmdGet.ExecuteReader();

            if (theReader.HasRows)
            {
                while (theReader.Read())
                {
                    associatedId = int.Parse(theReader[1].ToString());
                    break;
                }
            }

            theReader.Close();

            cmdGet = new SqlCommand("SELECT * from cdc where cdcid = @cdcid", theConnection);
            cmdGet.Parameters.AddWithValue("@cdcid", associatedId);
            cmdGet.CommandType = System.Data.CommandType.Text;

            theReader = cmdGet.ExecuteReader();
            string response = "";
            if (theReader.HasRows)
            {
                while (theReader.Read())
                {
                    response += " " + (theReader[1].ToString());
                    //associatedId = int.Parse(theReader[9].ToString());
                    break;
                }
            }


            theReader.Close();

            closeDataConnection();
            return response;

        }

        // Export photos after search
        public String ExportPhotoSearch(DataTable dtPhotoSearch)
        {
            try
            {
                string newFilePath = CopyReportTemplate("PhotoSearch");

                FileStream template = new FileStream(newFilePath, FileMode.Open, FileAccess.Read);

                //ExcelPackage has a constructor that only requires a stream.
                ExcelPackage pck = new OfficeOpenXml.ExcelPackage(template);
                ExcelWorkbook workBook = pck.Workbook;

                template.Close();

                if (dtPhotoSearch.Rows.Count > 0)
                {
                    int rowIndex = 2;
                    ExcelWorksheet photoSearchSheet = workBook.Worksheets["PhotoSearch"];

                    for (int i = 0; i < dtPhotoSearch.Rows.Count; i++)
                    {
                        rowIndex++;

                        photoSearchSheet.Cells[rowIndex, 1].Value = dtPhotoSearch.Rows[i]["DeliveryCode"].ToString();
                        if (String.IsNullOrEmpty(dtPhotoSearch.Rows[i]["CompletedDate"].ToString()))
                            photoSearchSheet.Cells[rowIndex, 2].Value = dtPhotoSearch.Rows[i]["DateAdded"].ToString();
                        else
                            photoSearchSheet.Cells[rowIndex, 2].Value = dtPhotoSearch.Rows[i]["CompletedDate"].ToString();
                        photoSearchSheet.Cells[rowIndex, 3].Value = dtPhotoSearch.Rows[i]["StoreNumber"].ToString();
                        photoSearchSheet.Cells[rowIndex, 4].Value = dtPhotoSearch.Rows[i]["StoreName"].ToString();
                        photoSearchSheet.Cells[rowIndex, 5].Value = dtPhotoSearch.Rows[i]["StoreOwnershipType"].ToString();
                        photoSearchSheet.Cells[rowIndex, 6].Value = dtPhotoSearch.Rows[i]["ProviderName"].ToString();
                        photoSearchSheet.Cells[rowIndex, 7].Value = dtPhotoSearch.Rows[i]["CDCName"].ToString();
                        photoSearchSheet.Cells[rowIndex, 8].Value = dtPhotoSearch.Rows[i]["RouteName"].ToString();
                        photoSearchSheet.Cells[rowIndex, 9].Value = dtPhotoSearch.Rows[i]["ChildReasonName"].ToString();
                        photoSearchSheet.Cells[rowIndex, 10].Value = dtPhotoSearch.Rows[i]["UserName"].ToString();
                        if (!String.IsNullOrEmpty(dtPhotoSearch.Rows[i]["PhotoId"].ToString()))
                            photoSearchSheet.Cells[rowIndex, 11].Value = baseWebURL + "/photos/" + dtPhotoSearch.Rows[i]["PhotoId"].ToString() + ".jpg";

                    }

                }

                FileStream fileSave = new FileStream(newFilePath, FileMode.Create);

                pck.SaveAs(fileSave);
                fileSave.Close();
                return extractFilename(newFilePath);

            }
            catch (Exception)
            {
                return String.Empty;
            }
        }

        // Export Routes
        public String ExportRoutes(DataTable dtRoutes)
        {
            try
            {

                string newFilePath = CopyReportTemplate("Routes");
                //WriteToFile(newFilePath);
                FileStream template = new FileStream(newFilePath, FileMode.Open, FileAccess.Read);

                //ExcelPackage has a constructor that only requires a stream.
                ExcelPackage pck = new OfficeOpenXml.ExcelPackage(template);
                ExcelWorkbook workBook = pck.Workbook;

                template.Close();

                if (dtRoutes.Rows.Count > 0)
                {
                    int rowIndex = 2;
                    ExcelWorksheet routesSheet = workBook.Worksheets["Routes"];

                    for (int i = 0; i < dtRoutes.Rows.Count; i++)
                    {
                        rowIndex++;

                        routesSheet.Cells[rowIndex, 2].Value = dtRoutes.Rows[i]["CDCName"].ToString();
                        routesSheet.Cells[rowIndex, 3].Value = dtRoutes.Rows[i]["RouteName"].ToString();
                        for (int j = 4; j < 39; j++)
                            routesSheet.Cells[rowIndex, j].Value = dtRoutes.Rows[i][j - 1].ToString();

                    }

                }

                FileStream fileSave = new FileStream(newFilePath, FileMode.Create);

                pck.SaveAs(fileSave);
                fileSave.Close();
                //WriteToFile(extractFilename(newFilePath));
                return extractFilename(newFilePath);

            }
            catch (Exception)
            {
                return String.Empty;
            }
        }

        // Export Stores
        public String ExportStores(DataTable dtStores)
        {
            try
            {
                string newFilePath = CopyReportTemplate("Stores");

                FileStream template = new FileStream(newFilePath, FileMode.Open, FileAccess.Read);

                //ExcelPackage has a constructor that only requires a stream.
                ExcelPackage pck = new OfficeOpenXml.ExcelPackage(template);
                ExcelWorkbook workBook = pck.Workbook;

                template.Close();

                if (dtStores.Rows.Count > 0)
                {
                    int rowIndex = 1;
                    ExcelWorksheet storesSheet = workBook.Worksheets["Stores"];

                    for (int i = 0; i < dtStores.Rows.Count; i++)
                    {
                        rowIndex++;

                        storesSheet.Cells[rowIndex, 1].Value = dtStores.Rows[i]["StoreNumber"].ToString();
                        storesSheet.Cells[rowIndex, 2].Value = dtStores.Rows[i]["StoreName"].ToString();
                        storesSheet.Cells[rowIndex, 3].Value = dtStores.Rows[i]["StoreAddress"].ToString();
                        storesSheet.Cells[rowIndex, 4].Value = dtStores.Rows[i]["StoreCity"].ToString();
                        storesSheet.Cells[rowIndex, 5].Value = dtStores.Rows[i]["StoreZip"].ToString();
                        storesSheet.Cells[rowIndex, 6].Value = dtStores.Rows[i]["StoreState"].ToString();
                        storesSheet.Cells[rowIndex, 7].Value = dtStores.Rows[i]["StorePhone"].ToString();
                        storesSheet.Cells[rowIndex, 8].Value = dtStores.Rows[i]["StoreManagerName"].ToString();
                        storesSheet.Cells[rowIndex, 9].Value = dtStores.Rows[i]["StoreEmail"].ToString();
                        storesSheet.Cells[rowIndex, 10].Value = dtStores.Rows[i]["StoreOwnershipType"].ToString();
                        storesSheet.Cells[rowIndex, 11].Value = dtStores.Rows[i]["PODRequired"].ToString();
                    }
                }

                FileStream fileSave = new FileStream(newFilePath, FileMode.Create);

                pck.SaveAs(fileSave);
                fileSave.Close();
                return extractFilename(newFilePath);

            }
            catch (Exception)
            {
                return String.Empty;
            }
        }

        // Export Ops
        public String ExportOps(DataTable dtStores)
        {
            try
            {
                string newFilePath = CopyReportTemplate("Ops");

                FileStream template = new FileStream(newFilePath, FileMode.Open, FileAccess.Read);

                //ExcelPackage has a constructor that only requires a stream.
                ExcelPackage pck = new OfficeOpenXml.ExcelPackage(template);
                ExcelWorkbook workBook = pck.Workbook;

                template.Close();

                if (dtStores.Rows.Count > 0)
                {
                    int rowIndex = 1;
                    ExcelWorksheet storesSheet = workBook.Worksheets["Ops"];

                    for (int i = 0; i < dtStores.Rows.Count; i++)
                    {
                        rowIndex++;

                        storesSheet.Cells[rowIndex, 2].Value = dtStores.Rows[i]["Division"].ToString();
                        storesSheet.Cells[rowIndex, 3].Value = dtStores.Rows[i]["DivisionName"].ToString();
                        storesSheet.Cells[rowIndex, 4].Value = dtStores.Rows[i]["DVPOutlookName"].ToString();
                        storesSheet.Cells[rowIndex, 5].Value = dtStores.Rows[i]["DVPEmailAddress"].ToString();
                        storesSheet.Cells[rowIndex, 6].Value = dtStores.Rows[i]["Region"].ToString();
                        storesSheet.Cells[rowIndex, 7].Value = dtStores.Rows[i]["RegionName"].ToString();
                        storesSheet.Cells[rowIndex, 8].Value = dtStores.Rows[i]["RVPOutlookName"].ToString();
                        storesSheet.Cells[rowIndex, 9].Value = dtStores.Rows[i]["RVPEmailAddress"].ToString();
                        storesSheet.Cells[rowIndex, 10].Value = dtStores.Rows[i]["Area"].ToString();
                        storesSheet.Cells[rowIndex, 11].Value = dtStores.Rows[i]["AreaName"].ToString();
                        storesSheet.Cells[rowIndex, 12].Value = dtStores.Rows[i]["RDOutlookName"].ToString();
                        storesSheet.Cells[rowIndex, 13].Value = dtStores.Rows[i]["RDEmailAddress"].ToString();
                        storesSheet.Cells[rowIndex, 14].Value = dtStores.Rows[i]["District"].ToString();
                        storesSheet.Cells[rowIndex, 15].Value = dtStores.Rows[i]["DistrictName"].ToString();
                        storesSheet.Cells[rowIndex, 16].Value = dtStores.Rows[i]["DMOutlookName"].ToString();
                        storesSheet.Cells[rowIndex, 17].Value = dtStores.Rows[i]["DMEmailAddress"].ToString();
                        storesSheet.Cells[rowIndex, 18].Value = dtStores.Rows[i]["StoreNumber"].ToString();

                    }
                }

                FileStream fileSave = new FileStream(newFilePath, FileMode.Create);

                pck.SaveAs(fileSave);
                fileSave.Close();
                return extractFilename(newFilePath);

            }
            catch (Exception)
            {
                return String.Empty;
            }
        }

        public Response CommitCachedData(TripForSync aTripModel)
        {
            Response theResponse = new Response();

            int totalFailuresCommitted = 0;
            int totalImagesCommitted = 0;
            int totalCommentsCommitted = 0;

            int totalFailureErrors = 0;
            int totalImageErrors = 0;
            int totalCommentErrors = 0;

            if (aTripModel != null)
            {
                if (aTripModel.id > 0)
                {
                    openDataConnection();

                    SqlCommand cmdCheckTripID = new SqlCommand("SELECT TripID FROM Trip WHERE TripID = " + aTripModel.id, theConnection);

                    theReader = cmdCheckTripID.ExecuteReader();

                    if (theReader.HasRows)
                    {
                        theReader.Close();

                        if (aTripModel.stops != null && aTripModel.stops.Count > 0)
                        {
                            foreach (StopWithStoreAndFailure aStop in aTripModel.stops)
                            {
                                if (aStop.committed)
                                {
                                    continue;
                                }

                                SqlCommand cmdCheckStopID = new SqlCommand("SELECT StopID FROM Stop WHERE StopID = " + aStop.id, theConnection);
                                openDataConnection();
                                theReader = cmdCheckStopID.ExecuteReader();

                                if (theReader.HasRows)
                                {
                                    theReader.Close();

                                    if (aStop.failure != null && aStop.failure.Count > 0)
                                    {
                                        foreach (Failure aFailure in aStop.failure)
                                        //foreach (FailureAndPhoto aFailure in aStop.failureImages)
                                        {
                                            ResponseFailure addFailureResponse = AddFailure(aFailure);

                                            if (addFailureResponse.statusCode == 0)
                                            {
                                                totalFailuresCommitted++;
                                            }
                                            else
                                            {
                                                totalFailureErrors++;
                                            }
                                            //}
                                            //}

                                            //if (aStop.images != null && aStop.images.Count > 0)
                                            if (aFailure.photos != null && aFailure.photos.Count > 0)
                                            {
                                                //foreach (string anImage in aStop.images)
                                                foreach (Photo aPhoto in aFailure.photos)
                                                {
                                                    Photo thisPhoto = new Photo();
                                                    thisPhoto.imageData = aPhoto.imageData;
                                                    thisPhoto.stopID = aStop.id;
                                                    thisPhoto.failureID = addFailureResponse.failure.failureID;

                                                    Response addPhotoResponse = AddPhotoToStop(thisPhoto);

                                                    if (addPhotoResponse.statusCode == 0)
                                                    {
                                                        totalImagesCommitted++;
                                                    }
                                                    else
                                                    {
                                                        totalImageErrors++;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    //if (aStop.comment != null && aStop.comment.Length > 0)
                                    //{
                                    //    Comment thisComment = new Comment();
                                    //    thisComment.comment = aStop.comment;
                                    //    thisComment.stopID = aStop.id;

                                    //    Response addCommentResponse = AddCommentToStop(thisComment);

                                    //    if (addCommentResponse.statusCode == 0)
                                    //    {
                                    //        totalCommentsCommitted++;
                                    //    }
                                    //    else
                                    //    {
                                    //        totalCommentErrors++;
                                    //    }
                                    //}

                                    if (aStop.completed)
                                    {
                                        Response addCompletedResponse = CompleteStop(aStop.id.ToString());
                                    }
                                }
                                else
                                {
                                    theReader.Close();

                                    theResponse.statusCode = 6;
                                    theResponse.statusDescription = "The Stop ID " + aStop.id + " does not exist";
                                }
                            }

                            if (totalFailureErrors == 0 && totalImageErrors == 0 && totalCommentErrors == 0)
                            {
                                theResponse.statusCode = 0;
                                theResponse.statusDescription = "All data has been synchronized";
                            }
                            else
                            {
                                if (totalFailuresCommitted > 0 || totalImagesCommitted > 0 || totalCommentsCommitted > 0)
                                {
                                    theResponse.statusCode = 6;
                                    theResponse.statusDescription = "Some of the data has been synchronized. " + totalFailureErrors + " Failures, " + totalImageErrors + " Images and " + totalCommentErrors + " Comments could not be synchronized";
                                }
                            }
                        }
                        else
                        {
                            theResponse.statusCode = 6;
                            theResponse.statusDescription = "No stops were supplied with the trip model. No data was synchronized.";
                        }
                    }
                    else
                    {
                        theReader.Close();

                        theResponse.statusCode = 4;
                        theResponse.statusDescription = "Trip ID was invalid";
                    }

                    closeDataConnection();
                }
            }
            else
            {
                theResponse.statusCode = 6;
                theResponse.statusDescription = "Trip Model not supplied";
            }

            return theResponse;
        }

        public Response CommitIssues(StopWithStoreAndFailure aStop)
        {
            Response theResponse = new Response();
            int totalFailureErrors = 0, totalImageErrors = 0;

            try
            {
                openDataConnection();
                theTrans = theConnection.BeginTransaction();

                if (aStop.failure != null && aStop.failure.Count > 0)
                {
                    foreach (Failure aFailure in aStop.failure)
                    {
                        SqlCommand cmdCheckUniqueID = new SqlCommand("SELECT UniqueID FROM Failure WHERE UniqueID = '" + aFailure.uniqueID + "'", theConnection);
                        cmdCheckUniqueID.Transaction = theTrans;

                        object uniqueId = cmdCheckUniqueID.ExecuteScalar();

                        if (uniqueId != null && !string.IsNullOrEmpty(uniqueId.ToString()))
                        {
                            continue;
                        }
                        ResponseFailure addFailureResponse = AddFailureTransaction(aFailure);

                        if (addFailureResponse.statusCode != 0)
                        {
                            totalFailureErrors++;
                        }


                        if (aFailure.photos != null && aFailure.photos.Count > 0)
                        {
                            foreach (Photo aPhoto in aFailure.photos)
                            {
                                Photo thisPhoto = new Photo();
                                thisPhoto.imageData = aPhoto.imageData;
                                thisPhoto.stopID = aFailure.stopID;
                                thisPhoto.failureID = addFailureResponse.failure.failureID;

                                Response addPhotoResponse = AddPhotoToStopTransaction(thisPhoto);

                                if (addPhotoResponse.statusCode != 0)
                                {
                                    totalImageErrors++;
                                }

                            }
                        }
                    }
                }

                if (totalFailureErrors > 0 || totalImageErrors > 0)
                {
                    theTrans.Rollback();
                    theResponse.statusCode = 6;
                    theResponse.statusDescription = "Unable to update data";
                }
                else
                {
                    theTrans.Commit();
                    theResponse.statusCode = 0;
                    theResponse.statusDescription = "All data has been synchronized";
                    ConsolidateEmails(aStop.id.ToString());
                }
            }
            catch (Exception ex)
            {
                theTrans.Rollback();
                theResponse.statusCode = 6;
                theResponse.statusDescription = "Unable to update data";
            }
            finally
            {
                theTrans = null;
                closeDataConnection();

            }
            return theResponse;
        }

        public Response CommitIssuesV7(StopWithStoreAndFailure aStop)
        {

            //string text = "";
            //text = " Online Start time :" + DateTime.Now + "\n";
            //WriteToFile(text);


            Response theResponse = new Response();
            int totalFailureErrors = 0, totalImageErrors = 0, totalDeliveryErrors = 0;

            try
            {
                openDataConnection();
                theTrans = theConnection.BeginTransaction();

                if (aStop.failure != null && aStop.failure.Count > 0)
                {
                    foreach (Failure aFailure in aStop.failure)
                    {
                        SqlCommand cmdCheckUniqueID = new SqlCommand("SELECT UniqueID FROM Failure WHERE UniqueID = '" + aFailure.uniqueID + "'", theConnection);
                        cmdCheckUniqueID.Transaction = theTrans;

                        object uniqueId = cmdCheckUniqueID.ExecuteScalar();

                        if (uniqueId != null && !string.IsNullOrEmpty(uniqueId.ToString()))
                        {
                            continue;
                        }
                        ResponseFailure addFailureResponse = AddFailureTransaction(aFailure);

                        if (addFailureResponse.statusCode != 0)
                        {
                            totalFailureErrors++;
                        }


                        if (aFailure.photos != null && aFailure.photos.Count > 0)
                        {
                            foreach (Photo aPhoto in aFailure.photos)
                            {
                                Photo thisPhoto = new Photo();
                                thisPhoto.imageData = aPhoto.imageData;
                                thisPhoto.stopID = aFailure.stopID;
                                thisPhoto.failureID = addFailureResponse.failure.failureID;

                                Response addPhotoResponse = AddPhotoToStopTransaction(thisPhoto);

                                if (addPhotoResponse.statusCode != 0)
                                {
                                    totalImageErrors++;
                                }

                            }
                        }
                        if (aFailure.deliveryCodes != null && aFailure.deliveryCodes.Count > 0)
                        {
                            foreach (Delivery aDelivery in aFailure.deliveryCodes)
                            {
                                Delivery thisDelivery = new Delivery();
                                thisDelivery.deliveryCode = aDelivery.deliveryCode;
                                thisDelivery.stopID = aStop.id;
                                thisDelivery.failureID = addFailureResponse.failure.failureID;
                                //    thisDelivery.dateAdded = aDelivery.dateAdded;

                                Response addDeliveryResponse = AddDeliveryTransaction(thisDelivery);

                                if (addDeliveryResponse.statusCode != 0)
                                {
                                    totalDeliveryErrors++;
                                }

                            }
                        }
                    }
                }

                Response addStopCompletedDateResponse = AddStopCompletedDate(aStop.id.ToString(), aStop.completedDate);

                if (totalFailureErrors > 0 || totalImageErrors > 0 || totalDeliveryErrors > 0)
                {
                    theTrans.Rollback();
                    theResponse.statusCode = 6;
                    theResponse.statusDescription = "Unable to update data";
                }
                else
                {
                    theTrans.Commit();
                    theResponse.statusCode = 0;
                    theResponse.statusDescription = "All data has been synchronized";
                    ConsolidateEmails(aStop.id.ToString());
                }
            }
            catch (Exception ex)
            {
                //text = "Exception :" + ex.Message + "\n";
                //WriteToFile(text);

                theTrans.Rollback();
                theResponse.statusCode = 6;
                theResponse.statusDescription = "Unable to update data";
            }
            finally
            {
                theTrans = null;
                closeDataConnection();

            }
            //text = " End time   :" + DateTime.Now + "\n";
            //WriteToFile(text);

            return theResponse;
        }

        public Response CommitCachedDataV5(TripForSync aTripModel)
        {
            Response theResponse = new Response();

            int totalFailuresCommitted = 0;
            int totalImagesCommitted = 0;
            int totalDeliveryCodesCommitted = 0;
            int totalCommentsCommitted = 0;

            int totalFailureErrors = 0;
            int totalImageErrors = 0;
            int totalDeliveryCodeErrors = 0;
            int totalCommentErrors = 0;

            if (aTripModel != null)
            {
                if (aTripModel.id > 0)
                {
                    openDataConnection();

                    SqlCommand cmdCheckTripID = new SqlCommand("SELECT TripID FROM Trip WHERE TripID = " + aTripModel.id, theConnection);

                    theReader = cmdCheckTripID.ExecuteReader();

                    if (theReader.HasRows)
                    {
                        theReader.Close();

                        if (aTripModel.stops != null && aTripModel.stops.Count > 0)
                        {
                            foreach (StopWithStoreAndFailure aStop in aTripModel.stops)
                            {
                                if (aStop.committed)
                                {
                                    continue;
                                }

                                SqlCommand cmdCheckStopID = new SqlCommand("SELECT StopID FROM Stop WHERE StopID = " + aStop.id, theConnection);
                                openDataConnection();
                                theReader = cmdCheckStopID.ExecuteReader();

                                if (theReader.HasRows)
                                {
                                    theReader.Close();

                                    if (aStop.failure != null && aStop.failure.Count > 0)
                                    {
                                        foreach (Failure aFailure in aStop.failure)
                                        {
                                            if (aFailure.committed)
                                            {
                                                continue;
                                            }

                                            ResponseFailure addFailureResponse = AddFailure(aFailure);

                                            if (addFailureResponse.statusCode == 0)
                                            {
                                                totalFailuresCommitted++;
                                            }
                                            else
                                            {
                                                totalFailureErrors++;
                                            }

                                            if (aFailure.photos != null && aFailure.photos.Count > 0)
                                            {
                                                foreach (Photo aPhoto in aFailure.photos)
                                                {
                                                    Photo thisPhoto = new Photo();
                                                    thisPhoto.imageData = aPhoto.imageData;
                                                    thisPhoto.stopID = aStop.id;
                                                    thisPhoto.failureID = addFailureResponse.failure.failureID;

                                                    Response addPhotoResponse = AddPhotoToStop(thisPhoto);

                                                    if (addPhotoResponse.statusCode == 0)
                                                    {
                                                        totalImagesCommitted++;
                                                    }
                                                    else
                                                    {
                                                        totalImageErrors++;
                                                    }
                                                }
                                            }
                                            //if (aFailure.deliveryCodes != null && aFailure.deliveryCodes.Count > 0)
                                            //{
                                            //    foreach (Delivery aDelivery in aFailure.deliveryCodes)
                                            //    {
                                            //        Delivery thisDelivery = new Delivery();
                                            //        thisDelivery.deliveryCode = aDelivery.deliveryCode;
                                            //        thisDelivery.stopID = aStop.id;
                                            //        thisDelivery.failureID = addFailureResponse.failure.failureID;

                                            //        Response addDeliveryResponse = AddDelivery(thisDelivery);

                                            //        if (addDeliveryResponse.statusCode == 0)
                                            //        {
                                            //            totalDeliveryCodesCommitted++;
                                            //        }
                                            //        else
                                            //        {
                                            //            totalDeliveryCodeErrors++;
                                            //        }
                                            //    }
                                            //}
                                        }
                                    }

                                    if (aStop.completed)
                                    {
                                        Response addCompletedResponse = CompleteStop(aStop.id.ToString());
                                    }
                                    ConsolidateEmails(aStop.id.ToString());
                                }
                                else
                                {
                                    theReader.Close();

                                    theResponse.statusCode = 6;
                                    theResponse.statusDescription = "The Stop ID " + aStop.id + " does not exist";
                                }
                            }

                            if (totalFailureErrors == 0 && totalImageErrors == 0 && totalDeliveryCodeErrors == 0 && totalCommentErrors == 0)
                            {
                                theResponse.statusCode = 0;
                                theResponse.statusDescription = "All data has been synchronized";
                            }
                            else
                            {
                                if (totalFailuresCommitted > 0 || totalImagesCommitted > 0 || totalDeliveryCodeErrors > 0 || totalCommentsCommitted > 0)
                                {
                                    theResponse.statusCode = 6;
                                    theResponse.statusDescription = "Some of the data has been synchronized. " + totalFailureErrors + " Failures, " + totalDeliveryCodeErrors + " DeliveryCodes, " + totalImageErrors + " Images and " + totalCommentErrors + " Comments could not be synchronized";
                                }
                            }
                        }
                        else
                        {
                            theResponse.statusCode = 6;
                            theResponse.statusDescription = "No stops were supplied with the trip model. No data was synchronized.";
                        }
                    }
                    else
                    {
                        theReader.Close();

                        theResponse.statusCode = 4;
                        theResponse.statusDescription = "Trip ID was invalid";
                    }

                    closeDataConnection();
                }
            }
            else
            {
                theResponse.statusCode = 6;
                theResponse.statusDescription = "Trip Model not supplied";
            }

            return theResponse;
        }

        public Response CommitCachedDataV6(TripForSync aTripModel)
        {
            Response theResponse = new Response();
            List<int> lstStopIds = new List<int>();

            int totalFailuresCommitted = 0;
            int totalImagesCommitted = 0;
            int totalCommentsCommitted = 0;

            int totalFailureErrors = 0;
            int totalImageErrors = 0;
            int totalDeliveryCodeErrors = 0;
            int totalCommentErrors = 0;

            try
            {
                openDataConnection();
                theTrans = theConnection.BeginTransaction();

                if (aTripModel != null)
                {
                    if (aTripModel.id > 0)
                    {
                        SqlCommand cmdCheckTripID = new SqlCommand("SELECT TripID FROM Trip WHERE TripID = " + aTripModel.id, theConnection);
                        cmdCheckTripID.Transaction = theTrans;

                        theReader = cmdCheckTripID.ExecuteReader();

                        if (theReader.HasRows)
                        {
                            theReader.Close();

                            if (aTripModel.stops != null && aTripModel.stops.Count > 0)
                            {
                                foreach (StopWithStoreAndFailure aStop in aTripModel.stops)
                                {
                                    if (aStop.committed)
                                    {
                                        continue;
                                    }

                                    SqlCommand cmdCheckStopID = new SqlCommand("SELECT StopID FROM Stop WHERE StopID = " + aStop.id, theConnection);
                                    cmdCheckStopID.Transaction = theTrans;
                                    theReader = cmdCheckStopID.ExecuteReader();

                                    if (theReader.HasRows)
                                    {
                                        theReader.Close();

                                        if (aStop.failure != null && aStop.failure.Count > 0)
                                        {
                                            foreach (Failure aFailure in aStop.failure)
                                            {
                                                SqlCommand cmdCheckUniqueID = new SqlCommand("SELECT UniqueID FROM Failure WHERE UniqueID = '" + aFailure.uniqueID + "'", theConnection);
                                                cmdCheckUniqueID.Transaction = theTrans;

                                                object uniqueId = cmdCheckUniqueID.ExecuteScalar();

                                                if (uniqueId != null && !string.IsNullOrEmpty(uniqueId.ToString()))
                                                {
                                                    continue;
                                                }

                                                ResponseFailure addFailureResponse = AddFailureTransaction(aFailure);

                                                if (addFailureResponse.statusCode == 0)
                                                {
                                                    totalFailuresCommitted++;
                                                }
                                                else
                                                {
                                                    totalFailureErrors++;
                                                }

                                                if (aFailure.photos != null && aFailure.photos.Count > 0)
                                                {
                                                    foreach (Photo aPhoto in aFailure.photos)
                                                    {
                                                        Photo thisPhoto = new Photo();
                                                        thisPhoto.imageData = aPhoto.imageData;
                                                        thisPhoto.stopID = aStop.id;
                                                        thisPhoto.failureID = addFailureResponse.failure.failureID;

                                                        Response addPhotoResponse = AddPhotoToStopTransaction(thisPhoto);

                                                        if (addPhotoResponse.statusCode == 0)
                                                        {
                                                            totalImagesCommitted++;
                                                        }
                                                        else
                                                        {
                                                            totalImageErrors++;
                                                        }
                                                    }
                                                }
                                            }
                                        }

                                        if (aStop.completed)
                                        {
                                            Response addCompletedResponse = CompleteStopTransaction(aStop.id.ToString());
                                        }
                                        lstStopIds.Add(aStop.id);
                                        //ConsolidateEmails(aStop.id.ToString());
                                    }
                                    else
                                    {
                                        theReader.Close();

                                        theResponse.statusCode = 6;
                                        theResponse.statusDescription = "The Stop ID " + aStop.id + " does not exist";
                                    }
                                }

                                if (totalFailureErrors == 0 && totalImageErrors == 0 && totalDeliveryCodeErrors == 0 && totalCommentErrors == 0)
                                {
                                    theTrans.Commit();
                                    theResponse.statusCode = 0;
                                    theResponse.statusDescription = "All data has been synchronized";
                                    foreach (int id in lstStopIds)
                                        ConsolidateEmails(id.ToString());
                                }
                                else
                                {
                                    if (totalFailuresCommitted > 0 || totalImagesCommitted > 0 || totalDeliveryCodeErrors > 0 || totalCommentsCommitted > 0)
                                    {
                                        theResponse.statusCode = 6;
                                        theResponse.statusDescription = "Some of the data has been synchronized. " + totalFailureErrors + " Failures, " + totalDeliveryCodeErrors + " DeliveryCodes, " + totalImageErrors + " Images and " + totalCommentErrors + " Comments could not be synchronized";
                                    }
                                }
                            }
                            else
                            {
                                theResponse.statusCode = 6;
                                theResponse.statusDescription = "No stops were supplied with the trip model. No data was synchronized.";
                            }
                        }
                        else
                        {
                            theReader.Close();

                            theResponse.statusCode = 4;
                            theResponse.statusDescription = "Trip ID was invalid";
                        }

                    }
                }
                else
                {
                    theResponse.statusCode = 6;
                    theResponse.statusDescription = "Trip Model not supplied";
                }
            }
            catch (Exception ex)
            {
                theTrans.Rollback();
                theResponse.statusCode = 6;
                theResponse.statusDescription = "Unable to update data";
            }
            finally
            {
                theTrans = null;
                closeDataConnection();
            }
            return theResponse;
        }

        public Response CommitCachedDataV7(TripForSync aTripModel)
        {

            //string text = "";
            //text = " Offline Start time :" + DateTime.Now + "\n";
            //WriteToFile(text);


            Response theResponse = new Response();
            List<int> lstStopIds = new List<int>();

            int totalFailuresCommitted = 0;
            int totalImagesCommitted = 0;
            int totalDeliveryCodesCommitted = 0;
            int totalCommentsCommitted = 0;

            int totalFailureErrors = 0;
            int totalImageErrors = 0;
            int totalDeliveryCodeErrors = 0;
            int totalCommentErrors = 0;

            try
            {
                openDataConnection();
                theTrans = theConnection.BeginTransaction();

                if (aTripModel != null)
                {
                    if (aTripModel.id > 0)
                    {
                        SqlCommand cmdCheckTripID = new SqlCommand("SELECT TripID FROM Trip WHERE TripID = " + aTripModel.id, theConnection);
                        cmdCheckTripID.Transaction = theTrans;

                        theReader = cmdCheckTripID.ExecuteReader();

                        if (theReader.HasRows)
                        {
                            theReader.Close();

                            if (aTripModel.stops != null && aTripModel.stops.Count > 0)
                            {
                                foreach (StopWithStoreAndFailure aStop in aTripModel.stops)
                                {
                                    if (aStop.committed)
                                    {
                                        continue;
                                    }

                                    SqlCommand cmdCheckStopID = new SqlCommand("SELECT StopID FROM Stop WHERE StopID = " + aStop.id, theConnection);
                                    cmdCheckStopID.Transaction = theTrans;
                                    theReader = cmdCheckStopID.ExecuteReader();

                                    if (theReader.HasRows)
                                    {
                                        theReader.Close();

                                        if (aStop.failure != null && aStop.failure.Count > 0)
                                        {
                                            foreach (Failure aFailure in aStop.failure)
                                            {
                                                SqlCommand cmdCheckUniqueID = new SqlCommand("SELECT UniqueID FROM Failure WHERE UniqueID = '" + aFailure.uniqueID + "'", theConnection);
                                                cmdCheckUniqueID.Transaction = theTrans;

                                                object uniqueId = cmdCheckUniqueID.ExecuteScalar();

                                                if (uniqueId != null && !string.IsNullOrEmpty(uniqueId.ToString()))
                                                {
                                                    continue;
                                                }

                                                ResponseFailure addFailureResponse = AddFailureTransaction(aFailure);

                                                if (addFailureResponse.statusCode == 0)
                                                {
                                                    totalFailuresCommitted++;
                                                }
                                                else
                                                {
                                                    totalFailureErrors++;
                                                }

                                                if (aFailure.photos != null && aFailure.photos.Count > 0)
                                                {
                                                    foreach (Photo aPhoto in aFailure.photos)
                                                    {
                                                        Photo thisPhoto = new Photo();
                                                        thisPhoto.imageData = aPhoto.imageData;
                                                        thisPhoto.stopID = aStop.id;
                                                        thisPhoto.failureID = addFailureResponse.failure.failureID;

                                                        Response addPhotoResponse = AddPhotoToStopTransaction(thisPhoto);

                                                        if (addPhotoResponse.statusCode == 0)
                                                        {
                                                            totalImagesCommitted++;
                                                        }
                                                        else
                                                        {
                                                            totalImageErrors++;
                                                        }
                                                    }
                                                }
                                                if (aFailure.deliveryCodes != null && aFailure.deliveryCodes.Count > 0)
                                                {
                                                    foreach (Delivery aDelivery in aFailure.deliveryCodes)
                                                    {
                                                        if (aDelivery.deliveryCode == 0)
                                                        {
                                                            continue;
                                                        }

                                                        Delivery thisDelivery = new Delivery();
                                                        thisDelivery.deliveryCode = aDelivery.deliveryCode;
                                                        thisDelivery.stopID = aStop.id;
                                                        thisDelivery.failureID = addFailureResponse.failure.failureID;
                                                        //thisDelivery.dateAdded = aDelivery.dateAdded;

                                                        Response addDeliveryResponse = AddDeliveryTransaction(thisDelivery);

                                                        if (addDeliveryResponse.statusCode == 0)
                                                        {
                                                            totalDeliveryCodesCommitted++;
                                                        }
                                                        else
                                                        {
                                                            totalDeliveryCodeErrors++;
                                                        }
                                                    }
                                                }
                                            }
                                        }

                                        Response addStopCompletedDateResponse = AddStopCompletedDate(aStop.id.ToString(), aStop.completedDate);

                                        if (aStop.completed)
                                        {
                                            Response addCompletedResponse = CompleteStopTransaction(aStop.id.ToString());
                                        }
                                        lstStopIds.Add(aStop.id);
                                        //ConsolidateEmails(aStop.id.ToString());
                                    }
                                    else
                                    {
                                        theReader.Close();

                                        theResponse.statusCode = 6;
                                        theResponse.statusDescription = "The Stop ID " + aStop.id + " does not exist";
                                    }
                                }

                                if (totalFailureErrors == 0 && totalImageErrors == 0 && totalDeliveryCodeErrors == 0 && totalCommentErrors == 0)
                                {
                                    theTrans.Commit();
                                    theResponse.statusCode = 0;
                                    theResponse.statusDescription = "All data has been synchronized";
                                    //for(int i = 0; i<lstStopIds.Count;i++)
                                    foreach (int id in lstStopIds)
                                        ConsolidateEmails(id.ToString());
                                    //ConsolidateEmails(aStop.id.ToString());                                    
                                }
                                else
                                {
                                    if (totalFailuresCommitted > 0 || totalImagesCommitted > 0 || totalDeliveryCodeErrors > 0 || totalCommentsCommitted > 0)
                                    {
                                        theTrans.Commit();
                                        theResponse.statusCode = 6;
                                        theResponse.statusDescription = "Some of the data has been synchronized. " + totalFailureErrors + " Failures, " + totalDeliveryCodeErrors + " DeliveryCodes, " + totalImageErrors + " Images and " + totalCommentErrors + " Comments could not be synchronized";
                                    }
                                }
                            }
                            else
                            {
                                theResponse.statusCode = 6;
                                theResponse.statusDescription = "No stops were supplied with the trip model. No data was synchronized.";
                            }
                        }
                        else
                        {
                            theReader.Close();

                            theResponse.statusCode = 4;
                            theResponse.statusDescription = "Trip ID was invalid";
                        }

                    }
                }
                else
                {
                    theResponse.statusCode = 6;
                    theResponse.statusDescription = "Trip Model not supplied";
                }
            }
            catch (Exception ex)
            {
                //text = "Exception :" + ex.Message + "\n";
                //WriteToFile(text);

                theTrans.Rollback();
                theResponse.statusCode = 6;
                theResponse.statusDescription = "Unable to update data";
            }
            finally
            {
                theTrans = null;
                closeDataConnection();
            }

            //text = " End time   :" + DateTime.Now + "\n";
            //WriteToFile(text);

            return theResponse;
        }

        public Response DotNetReportStoreReadinessForSSC(string startDate, string endDate, string startHour, string endHour)
        {
            startDate = startDate + " " + startHour + ":00:00";
            endDate = endDate + " " + endHour + ":00:00";

            if (validateDate(startDate) && validateDate(endDate))
            {
                if (endDate.Length <= 10)
                {
                    endDate += " 23:59:59";
                }

                Response theResponse = new Response();

                openDataConnection();

                /* Summary Data */

                SqlCommand cmdDVPRVPDeliveriesWithoutIssues = new SqlCommand("ReportNumberOfDeliveriesWithoutIssuesWithIntervalForSSC", theConnection);
                cmdDVPRVPDeliveriesWithoutIssues.Parameters.AddWithValue("@dateStarted", startDate);
                cmdDVPRVPDeliveriesWithoutIssues.Parameters.AddWithValue("@dateEnded", endDate);
                cmdDVPRVPDeliveriesWithoutIssues.CommandType = System.Data.CommandType.StoredProcedure;
                cmdDVPRVPDeliveriesWithoutIssues.CommandTimeout = 1200;

                theReader = cmdDVPRVPDeliveriesWithoutIssues.ExecuteReader();

                List<DVPRVPSummary> dvprvpData = new List<DVPRVPSummary>();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        DVPRVPSummary thisRow = new DVPRVPSummary();

                        thisRow.dvpName = theReader["ProviderName"].ToString();
                        thisRow.rvpName = theReader["CDCName"].ToString();
                        thisRow.deliveries = (int)theReader["NumberOfDeliveriesWithoutIssues"];

                        dvprvpData.Add(thisRow);
                    }
                }

                theReader.Close();

                SqlCommand cmdDVPRVPDeliveriesWithIssues = new SqlCommand("ReportNumberOfDeliveriesWithIssuesWithIntervalForSSC", theConnection);
                cmdDVPRVPDeliveriesWithIssues.Parameters.AddWithValue("@dateStarted", startDate);
                cmdDVPRVPDeliveriesWithIssues.Parameters.AddWithValue("@dateEnded", endDate);
                cmdDVPRVPDeliveriesWithIssues.CommandType = System.Data.CommandType.StoredProcedure;
                cmdDVPRVPDeliveriesWithIssues.CommandTimeout = 1200;

                theReader = cmdDVPRVPDeliveriesWithIssues.ExecuteReader();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        string thisRowsDVP = theReader["ProviderName"].ToString();
                        string thisRowsRVP = theReader["CDCName"].ToString();

                        bool found = false;

                        for (int i = 0, l = dvprvpData.Count; i < l; i++)
                        {
                            DVPRVPSummary thisRowData = dvprvpData[i];

                            if (thisRowsDVP.Equals(thisRowData.dvpName) && thisRowsRVP.Equals(thisRowData.rvpName))
                            {
                                found = true;

                                int deliveriesWithIssues = (int)theReader["NumberOfDeliveriesWithIssues"];

                                thisRowData.deliveriesWithIssues = deliveriesWithIssues;
                                thisRowData.deliveries += thisRowData.deliveriesWithIssues;
                            }
                        }

                        if (!found)
                        {
                            DVPRVPSummary thisRow = new DVPRVPSummary();

                            thisRow.dvpName = thisRowsDVP;
                            thisRow.rvpName = thisRowsRVP;
                            thisRow.deliveriesWithIssues = (int)theReader["NumberOfDeliveriesWithIssues"];
                            thisRow.deliveries += thisRow.deliveriesWithIssues;

                            dvprvpData.Add(thisRow);

                            found = false;
                        }
                    }
                }

                theReader.Close();

                SqlCommand cmdDVPRVPNumberOfIssues = new SqlCommand("ReportNumberOfIssuesWithIntervalForSSC", theConnection);
                cmdDVPRVPNumberOfIssues.Parameters.AddWithValue("@dateStarted", startDate);
                cmdDVPRVPNumberOfIssues.Parameters.AddWithValue("@dateEnded", endDate);
                cmdDVPRVPNumberOfIssues.CommandType = System.Data.CommandType.StoredProcedure;
                cmdDVPRVPNumberOfIssues.CommandTimeout = 1200;

                theReader = cmdDVPRVPNumberOfIssues.ExecuteReader();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        string thisRowsDVP = theReader["ProviderName"].ToString();
                        string thisRowsRVP = theReader["CDCName"].ToString();

                        bool found = false;

                        for (int i = 0, l = dvprvpData.Count; i < l; i++)
                        {
                            DVPRVPSummary thisRowData = dvprvpData[i];

                            if (thisRowsDVP.Equals(thisRowData.dvpName) && thisRowsRVP.Equals(thisRowData.rvpName))
                            {
                                found = true;

                                int numberOfIssues = (int)theReader["NumberOfIssues"];

                                thisRowData.totalReadinessIssues = numberOfIssues;
                            }
                        }

                        if (!found)
                        {
                            DVPRVPSummary thisRow = new DVPRVPSummary();

                            thisRow.dvpName = thisRowsDVP;
                            thisRow.rvpName = thisRowsRVP;
                            thisRow.totalReadinessIssues = (int)theReader["NumberOfIssues"];

                            dvprvpData.Add(thisRow);

                            found = false;
                        }
                    }
                }

                theReader.Close();

                SqlCommand cmdUnitsLeftout = new SqlCommand("ReportUnitsLeftoutWithIntervalForSSC", theConnection);
                cmdUnitsLeftout.Parameters.AddWithValue("@dateStarted", startDate);
                cmdUnitsLeftout.Parameters.AddWithValue("@dateEnded", endDate);
                cmdUnitsLeftout.CommandType = System.Data.CommandType.StoredProcedure;
                cmdUnitsLeftout.CommandTimeout = 1200;

                theReader = cmdUnitsLeftout.ExecuteReader();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        string thisRowsDVP = theReader["ProviderName"].ToString();
                        string thisRowsRVP = theReader["CDCName"].ToString();

                        bool found = false;

                        for (int i = 0, l = dvprvpData.Count; i < l; i++)
                        {
                            DVPRVPSummary thisRowData = dvprvpData[i];

                            if (thisRowsDVP.Equals(thisRowData.dvpName) && thisRowsRVP.Equals(thisRowData.rvpName))
                            {
                                found = true;

                                int unitsLeftout = (int)theReader["UnitsLeftout"];

                                thisRowData.leftoutUnits = unitsLeftout;
                            }
                        }

                        if (!found)
                        {
                            DVPRVPSummary thisRow = new DVPRVPSummary();

                            thisRow.dvpName = thisRowsDVP;
                            thisRow.rvpName = thisRowsRVP;
                            thisRow.leftoutUnits = (int)theReader["UnitsLeftout"];

                            dvprvpData.Add(thisRow);

                            found = false;
                        }
                    }
                }

                theReader.Close();

                SqlCommand cmdDVPRVPUnitsBackhauled = new SqlCommand("ReportUnitsBackhauledWithIntervalForSSC", theConnection);
                cmdDVPRVPUnitsBackhauled.Parameters.AddWithValue("@dateStarted", startDate);
                cmdDVPRVPUnitsBackhauled.Parameters.AddWithValue("@dateEnded", endDate);
                cmdDVPRVPUnitsBackhauled.CommandType = System.Data.CommandType.StoredProcedure;
                cmdDVPRVPUnitsBackhauled.CommandTimeout = 1200;

                theReader = cmdDVPRVPUnitsBackhauled.ExecuteReader();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        string thisRowsDVP = theReader["ProviderName"].ToString();
                        string thisRowsRVP = theReader["CDCName"].ToString();

                        bool found = false;

                        for (int i = 0, l = dvprvpData.Count; i < l; i++)
                        {
                            DVPRVPSummary thisRowData = dvprvpData[i];

                            if (thisRowsDVP.Equals(thisRowData.dvpName) && thisRowsRVP.Equals(thisRowData.rvpName))
                            {
                                found = true;

                                int unitsBackhauled = (int)theReader["UnitsBackhauled"];

                                thisRowData.dairyBackhaulUnits = unitsBackhauled;
                            }
                        }

                        if (!found)
                        {
                            DVPRVPSummary thisRow = new DVPRVPSummary();

                            thisRow.dvpName = thisRowsDVP;
                            thisRow.rvpName = thisRowsRVP;
                            thisRow.dairyBackhaulUnits = (int)theReader["UnitsBackhauled"];

                            dvprvpData.Add(thisRow);

                            found = false;
                        }
                    }
                }

                theReader.Close();

                SqlCommand cmdDVPRVPUnitsBackhauledCost = new SqlCommand("ReportUnitsBackhauledCostWithIntervalForSSC", theConnection);
                cmdDVPRVPUnitsBackhauledCost.Parameters.AddWithValue("@dateStarted", startDate);
                cmdDVPRVPUnitsBackhauledCost.Parameters.AddWithValue("@dateEnded", endDate);
                cmdDVPRVPUnitsBackhauledCost.CommandType = System.Data.CommandType.StoredProcedure;
                cmdDVPRVPUnitsBackhauledCost.CommandTimeout = 1200;

                theReader = cmdDVPRVPUnitsBackhauledCost.ExecuteReader();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        string thisRowsDVP = theReader["ProviderName"].ToString();
                        string thisRowsRVP = theReader["CDCName"].ToString();

                        bool found = false;

                        for (int i = 0, l = dvprvpData.Count; i < l; i++)
                        {
                            DVPRVPSummary thisRowData = dvprvpData[i];

                            if (thisRowsDVP.Equals(thisRowData.dvpName) && thisRowsRVP.Equals(thisRowData.rvpName))
                            {
                                found = true;

                                if (theReader["BackhaulCost"] != DBNull.Value)
                                {
                                    double backhaulCost = (double)theReader["BackhaulCost"];

                                    thisRowData.dairyBackhaulCOGS = backhaulCost;
                                }
                            }
                        }

                        if (!found)
                        {
                            DVPRVPSummary thisRow = new DVPRVPSummary();

                            thisRow.dvpName = thisRowsDVP;
                            thisRow.rvpName = thisRowsRVP;
                            thisRow.dairyBackhaulCOGS = (double)theReader["BackhaulCost"];

                            dvprvpData.Add(thisRow);

                            found = false;
                        }
                    }
                }

                theReader.Close();

                closeDataConnection();

                try
                {

                    string newFilePath = CopyReportTemplate("ssc");

                    string currentPath = HttpContext.Current.Server.MapPath(".");
                    long currentTime = DateTime.Now.ToFileTimeUtc();

                    string sourceFilename = "ssc.xlsx";
                    string targetFilename;
                    string targetPath;
                    string sourcePath = currentPath + "\\templates\\";

                    targetFilename = "report_ssc_" + currentTime + ".xlsx";
                    targetPath = currentPath + "\\downloads\\";

                    string sourceFile = System.IO.Path.Combine(sourcePath, sourceFilename);
                    string destFile = System.IO.Path.Combine(targetPath, targetFilename);

                    openDataConnection();
                    SqlCommand cmdReport = new SqlCommand("ReportStoresNotReadyWithInterval", theConnection);
                    cmdReport.Parameters.AddWithValue("@dateStarted", startDate);
                    cmdReport.Parameters.AddWithValue("@dateEnded", endDate);
                    cmdReport.CommandType = System.Data.CommandType.StoredProcedure;
                    cmdReport.CommandTimeout = 1200;

                    theReader = cmdReport.ExecuteReader();

                    FileStream template = new FileStream(newFilePath, FileMode.Open, FileAccess.Read);
                    ExcelPackage pck = new OfficeOpenXml.ExcelPackage(template);
                    ExcelWorkbook workBook = pck.Workbook;

                    template.Close();

                    if (theReader.HasRows)
                    {
                        int rowIndex = 1;
                        string[] photoIds;
                        int colIndex;

                        ExcelWorksheet notReadySheet = workBook.Worksheets["DetailedViewNotReady"];

                        while (theReader.Read())
                        {
                            rowIndex++;

                            notReadySheet.Cells[rowIndex, 1].Value = theReader[0].ToString();
                            notReadySheet.Cells[rowIndex, 2].Value = theReader[1].ToString();
                            notReadySheet.Cells[rowIndex, 3].Value = theReader[2].ToString();
                            notReadySheet.Cells[rowIndex, 4].Value = theReader[3].ToString();
                            notReadySheet.Cells[rowIndex, 5].Value = theReader[4].ToString();
                            notReadySheet.Cells[rowIndex, 6].Value = theReader[9].ToString();
                            notReadySheet.Cells[rowIndex, 7].Value = theReader[10].ToString();
                            notReadySheet.Cells[rowIndex, 8].Value = theReader[11].ToString();
                            notReadySheet.Cells[rowIndex, 9].Value = Math.Round(Convert.ToDecimal(theReader[14].ToString()), 2).ToString();
                            notReadySheet.Cells[rowIndex, 10].Value = getCellFriendlyText(theReader[12].ToString());
                            if (String.IsNullOrEmpty(theReader[13].ToString()))
                            {
                                notReadySheet.Cells[rowIndex, 11].Value = "No Photos Available";
                            }
                            else
                            {
                                photoIds = theReader[13].ToString().Split(',');
                                for (colIndex = 0; colIndex < Int32.Parse(theReader[10].ToString()); colIndex++)
                                    notReadySheet.Cells[rowIndex, colIndex + 11].Value = baseWebURL + "/photos/" + photoIds[colIndex] + ".jpg";

                            }
                            notReadySheet.Cells.AutoFitColumns();

                        }
                        theResponse.statusCode = 0;
                        theResponse.statusDescription = extractFilename(newFilePath);
                    }
                    else
                    {
                        theResponse.statusCode = 1;
                        theResponse.statusDescription = "There is no data logged between the dates that were selected";
                    }
                    theReader.Close();

                    SqlCommand cmdReportNotReady = new SqlCommand("ReportStoresReadyWithInterval", theConnection);
                    cmdReportNotReady.Parameters.AddWithValue("@dateStarted", startDate);
                    cmdReportNotReady.Parameters.AddWithValue("@dateEnded", endDate);
                    cmdReportNotReady.CommandType = System.Data.CommandType.StoredProcedure;
                    cmdReportNotReady.CommandTimeout = 1200;

                    theReader = cmdReportNotReady.ExecuteReader();
                    if (theReader.HasRows)
                    {
                        int rowIndex = 1;
                        ExcelWorksheet readySheet = workBook.Worksheets["DetailedViewReady"];

                        while (theReader.Read())
                        {
                            rowIndex++;

                            readySheet.Cells[rowIndex, 1].Value = theReader[0].ToString();
                            readySheet.Cells[rowIndex, 2].Value = theReader[1].ToString();
                            readySheet.Cells[rowIndex, 3].Value = theReader[2].ToString();
                            readySheet.Cells[rowIndex, 4].Value = theReader[3].ToString();
                            readySheet.Cells[rowIndex, 5].Value = theReader[4].ToString();

                        }
                        theResponse.statusCode = 0;
                        theResponse.statusDescription = extractFilename(newFilePath);
                    }
                    else
                    {
                        theResponse.statusCode = 1;
                        theResponse.statusDescription = "There is no data logged between the dates that were selected";
                    }
                    theReader.Close();

                    ExcelWorksheet sheetSummary = workBook.Worksheets["Summary"];

                    sheetSummary.Cells["A2:C2"].Merge = true;
                    sheetSummary.Cells[2, 1].Value = startDate + " to " + endDate;

                    if (dvprvpData != null && dvprvpData.Count > 0)
                    {
                        int currentRowIndex = 3;

                        for (int i = 0, l = dvprvpData.Count; i < l; i++)
                        {
                            DVPRVPSummary thisRowsData = dvprvpData[i];

                            thisRowsData.percentageStoresReady = Math.Round((100 - ((double)thisRowsData.deliveriesWithIssues / (double)thisRowsData.deliveries) * 100.0), 2);

                            sheetSummary.Cells[currentRowIndex, 1].Value = thisRowsData.dvpName;
                            sheetSummary.Cells[currentRowIndex, 2].Value = thisRowsData.rvpName;
                            sheetSummary.Cells[currentRowIndex, 3].Value = thisRowsData.percentageStoresReady.ToString() + " %";
                            sheetSummary.Cells[currentRowIndex, 4].Value = thisRowsData.deliveries;
                            sheetSummary.Cells[currentRowIndex, 5].Value = thisRowsData.deliveriesWithIssues;
                            sheetSummary.Cells[currentRowIndex, 6].Value = thisRowsData.totalReadinessIssues;

                            currentRowIndex++;
                        }

                        theResponse.statusCode = 0;
                        theResponse.statusDescription = extractFilename(newFilePath);
                    }
                    else
                    {
                        theResponse.statusCode = 1;
                        theResponse.statusDescription = "There is no data logged between the dates that were selected";
                    }



                    FileStream fileSave = new FileStream(newFilePath, FileMode.Create);

                    pck.SaveAs(fileSave);
                    fileSave.Close();

                    closeDataConnection();
                }
                catch (Exception _exception)
                {
                    theResponse.statusCode = 6;
                    theResponse.statusDescription = _exception.Message;
                }

                return theResponse;
            }
            else
            {
                //return ReportStoreReadinessForSSC();
                return null;
            }

        }

        public Response DotNetReportStoreReadinessForCDC(string startDate, string endDate, string startHour, string endHour)
        {
            startDate = startDate + " " + startHour + ":00:00";
            endDate = endDate + " " + endHour + ":00:00";

            if (validateDate(startDate) && validateDate(endDate))
            {
                if (endDate.Length <= 10)
                {
                    endDate += " 23:59:59";
                }

                Response theResponse = new Response();

                openDataConnection();

                /* Summary Data */

                SqlCommand cmdDVPRVPDeliveriesWithoutIssues = new SqlCommand("ReportNumberOfDeliveriesWithoutIssuesWithIntervalForSSC", theConnection);
                cmdDVPRVPDeliveriesWithoutIssues.Parameters.AddWithValue("@dateStarted", startDate);
                cmdDVPRVPDeliveriesWithoutIssues.Parameters.AddWithValue("@dateEnded", endDate);
                cmdDVPRVPDeliveriesWithoutIssues.CommandType = System.Data.CommandType.StoredProcedure;
                cmdDVPRVPDeliveriesWithoutIssues.CommandTimeout = 1200;

                theReader = cmdDVPRVPDeliveriesWithoutIssues.ExecuteReader();

                List<DVPRVPSummary> dvprvpData = new List<DVPRVPSummary>();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        DVPRVPSummary thisRow = new DVPRVPSummary();

                        thisRow.dvpName = theReader["ProviderName"].ToString();
                        thisRow.rvpName = theReader["CDCName"].ToString();
                        thisRow.deliveries = (int)theReader["NumberOfDeliveriesWithoutIssues"];

                        dvprvpData.Add(thisRow);
                    }
                }

                theReader.Close();

                SqlCommand cmdDVPRVPDeliveriesWithIssues = new SqlCommand("ReportNumberOfDeliveriesWithIssuesWithIntervalForSSC", theConnection);
                cmdDVPRVPDeliveriesWithIssues.Parameters.AddWithValue("@dateStarted", startDate);
                cmdDVPRVPDeliveriesWithIssues.Parameters.AddWithValue("@dateEnded", endDate);
                cmdDVPRVPDeliveriesWithIssues.CommandType = System.Data.CommandType.StoredProcedure;
                cmdDVPRVPDeliveriesWithIssues.CommandTimeout = 1200;

                theReader = cmdDVPRVPDeliveriesWithIssues.ExecuteReader();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        string thisRowsDVP = theReader["ProviderName"].ToString();
                        string thisRowsRVP = theReader["CDCName"].ToString();

                        bool found = false;

                        for (int i = 0, l = dvprvpData.Count; i < l; i++)
                        {
                            DVPRVPSummary thisRowData = dvprvpData[i];

                            if (thisRowsDVP.Equals(thisRowData.dvpName) && thisRowsRVP.Equals(thisRowData.rvpName))
                            {
                                found = true;

                                int deliveriesWithIssues = (int)theReader["NumberOfDeliveriesWithIssues"];

                                thisRowData.deliveriesWithIssues = deliveriesWithIssues;
                                thisRowData.deliveries += thisRowData.deliveriesWithIssues;
                            }
                        }

                        if (!found)
                        {
                            DVPRVPSummary thisRow = new DVPRVPSummary();

                            thisRow.dvpName = thisRowsDVP;
                            thisRow.rvpName = thisRowsRVP;
                            thisRow.deliveriesWithIssues = (int)theReader["NumberOfDeliveriesWithIssues"];
                            thisRow.deliveries += thisRow.deliveriesWithIssues;

                            dvprvpData.Add(thisRow);

                            found = false;
                        }
                    }
                }

                theReader.Close();

                SqlCommand cmdDVPRVPNumberOfIssues = new SqlCommand("ReportNumberOfIssuesWithIntervalForSSC", theConnection);
                cmdDVPRVPNumberOfIssues.Parameters.AddWithValue("@dateStarted", startDate);
                cmdDVPRVPNumberOfIssues.Parameters.AddWithValue("@dateEnded", endDate);
                cmdDVPRVPNumberOfIssues.CommandType = System.Data.CommandType.StoredProcedure;
                cmdDVPRVPNumberOfIssues.CommandTimeout = 1200;

                theReader = cmdDVPRVPNumberOfIssues.ExecuteReader();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        string thisRowsDVP = theReader["ProviderName"].ToString();
                        string thisRowsRVP = theReader["CDCName"].ToString();

                        bool found = false;

                        for (int i = 0, l = dvprvpData.Count; i < l; i++)
                        {
                            DVPRVPSummary thisRowData = dvprvpData[i];

                            if (thisRowsDVP.Equals(thisRowData.dvpName) && thisRowsRVP.Equals(thisRowData.rvpName))
                            {
                                found = true;

                                int numberOfIssues = (int)theReader["NumberOfIssues"];

                                thisRowData.totalReadinessIssues = numberOfIssues;
                            }
                        }

                        if (!found)
                        {
                            DVPRVPSummary thisRow = new DVPRVPSummary();

                            thisRow.dvpName = thisRowsDVP;
                            thisRow.rvpName = thisRowsRVP;
                            thisRow.totalReadinessIssues = (int)theReader["NumberOfIssues"];

                            dvprvpData.Add(thisRow);

                            found = false;
                        }
                    }
                }

                theReader.Close();

                SqlCommand cmdUnitsLeftout = new SqlCommand("ReportUnitsLeftoutWithIntervalForSSC", theConnection);
                cmdUnitsLeftout.Parameters.AddWithValue("@dateStarted", startDate);
                cmdUnitsLeftout.Parameters.AddWithValue("@dateEnded", endDate);
                cmdUnitsLeftout.CommandType = System.Data.CommandType.StoredProcedure;
                cmdUnitsLeftout.CommandTimeout = 1200;

                theReader = cmdUnitsLeftout.ExecuteReader();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        string thisRowsDVP = theReader["ProviderName"].ToString();
                        string thisRowsRVP = theReader["CDCName"].ToString();

                        bool found = false;

                        for (int i = 0, l = dvprvpData.Count; i < l; i++)
                        {
                            DVPRVPSummary thisRowData = dvprvpData[i];

                            if (thisRowsDVP.Equals(thisRowData.dvpName) && thisRowsRVP.Equals(thisRowData.rvpName))
                            {
                                found = true;

                                int unitsLeftout = (int)theReader["UnitsLeftout"];

                                thisRowData.leftoutUnits = unitsLeftout;
                            }
                        }

                        if (!found)
                        {
                            DVPRVPSummary thisRow = new DVPRVPSummary();

                            thisRow.dvpName = thisRowsDVP;
                            thisRow.rvpName = thisRowsRVP;
                            thisRow.leftoutUnits = (int)theReader["UnitsLeftout"];

                            dvprvpData.Add(thisRow);

                            found = false;
                        }
                    }
                }

                theReader.Close();

                SqlCommand cmdDVPRVPUnitsBackhauled = new SqlCommand("ReportUnitsBackhauledWithIntervalForSSC", theConnection);
                cmdDVPRVPUnitsBackhauled.Parameters.AddWithValue("@dateStarted", startDate);
                cmdDVPRVPUnitsBackhauled.Parameters.AddWithValue("@dateEnded", endDate);
                cmdDVPRVPUnitsBackhauled.CommandType = System.Data.CommandType.StoredProcedure;
                cmdDVPRVPUnitsBackhauled.CommandTimeout = 1200;

                theReader = cmdDVPRVPUnitsBackhauled.ExecuteReader();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        string thisRowsDVP = theReader["ProviderName"].ToString();
                        string thisRowsRVP = theReader["CDCName"].ToString();

                        bool found = false;

                        for (int i = 0, l = dvprvpData.Count; i < l; i++)
                        {
                            DVPRVPSummary thisRowData = dvprvpData[i];

                            if (thisRowsDVP.Equals(thisRowData.dvpName) && thisRowsRVP.Equals(thisRowData.rvpName))
                            {
                                found = true;

                                int unitsBackhauled = (int)theReader["UnitsBackhauled"];

                                thisRowData.dairyBackhaulUnits = unitsBackhauled;
                            }
                        }

                        if (!found)
                        {
                            DVPRVPSummary thisRow = new DVPRVPSummary();

                            thisRow.dvpName = thisRowsDVP;
                            thisRow.rvpName = thisRowsRVP;
                            thisRow.dairyBackhaulUnits = (int)theReader["UnitsBackhauled"];

                            dvprvpData.Add(thisRow);

                            found = false;
                        }
                    }
                }

                theReader.Close();

                SqlCommand cmdDVPRVPUnitsBackhauledCost = new SqlCommand("ReportUnitsBackhauledCostWithIntervalForSSC", theConnection);
                cmdDVPRVPUnitsBackhauledCost.Parameters.AddWithValue("@dateStarted", startDate);
                cmdDVPRVPUnitsBackhauledCost.Parameters.AddWithValue("@dateEnded", endDate);
                cmdDVPRVPUnitsBackhauledCost.CommandType = System.Data.CommandType.StoredProcedure;
                cmdDVPRVPUnitsBackhauledCost.CommandTimeout = 1200;

                theReader = cmdDVPRVPUnitsBackhauledCost.ExecuteReader();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        string thisRowsDVP = theReader["ProviderName"].ToString();
                        string thisRowsRVP = theReader["CDCName"].ToString();

                        bool found = false;

                        for (int i = 0, l = dvprvpData.Count; i < l; i++)
                        {
                            DVPRVPSummary thisRowData = dvprvpData[i];

                            if (thisRowsDVP.Equals(thisRowData.dvpName) && thisRowsRVP.Equals(thisRowData.rvpName))
                            {
                                found = true;

                                if (theReader["BackhaulCost"] != DBNull.Value)
                                {
                                    double backhaulCost = (double)theReader["BackhaulCost"];

                                    thisRowData.dairyBackhaulCOGS = backhaulCost;
                                }
                            }
                        }

                        if (!found)
                        {
                            DVPRVPSummary thisRow = new DVPRVPSummary();

                            thisRow.dvpName = thisRowsDVP;
                            thisRow.rvpName = thisRowsRVP;
                            thisRow.dairyBackhaulCOGS = (double)theReader["BackhaulCost"];

                            dvprvpData.Add(thisRow);

                            found = false;
                        }
                    }
                }

                theReader.Close();

                closeDataConnection();

                try
                {
                    string newFilePath = CopyReportTemplate("cdc");

                    openDataConnection();
                    SqlCommand cmdReport = new SqlCommand("ReportStoresNotReadyWithInterval", theConnection);
                    cmdReport.Parameters.AddWithValue("@dateStarted", startDate);
                    cmdReport.Parameters.AddWithValue("@dateEnded", endDate);
                    cmdReport.CommandType = System.Data.CommandType.StoredProcedure;
                    cmdReport.CommandTimeout = 1200;

                    theReader = cmdReport.ExecuteReader();

                    FileStream template = new FileStream(newFilePath, FileMode.Open, FileAccess.Read);
                    ExcelPackage pck = new OfficeOpenXml.ExcelPackage(template);
                    ExcelWorkbook workBook = pck.Workbook;

                    template.Close();

                    if (theReader.HasRows)
                    {
                        int rowIndex = 1;

                        ExcelWorksheet notReadySheet = workBook.Worksheets["DetailedViewNotReady"];

                        while (theReader.Read())
                        {
                            rowIndex++;

                            notReadySheet.Cells[rowIndex, 1].Value = theReader[0].ToString();
                            notReadySheet.Cells[rowIndex, 2].Value = theReader[1].ToString();
                            notReadySheet.Cells[rowIndex, 3].Value = theReader[2].ToString();
                            notReadySheet.Cells[rowIndex, 4].Value = theReader[3].ToString();
                            notReadySheet.Cells[rowIndex, 5].Value = theReader[4].ToString();
                            notReadySheet.Cells[rowIndex, 6].Value = theReader[9].ToString();
                            notReadySheet.Cells[rowIndex, 7].Value = theReader[10].ToString();
                            notReadySheet.Cells[rowIndex, 8].Value = theReader[11].ToString();
                            notReadySheet.Cells[rowIndex, 9].Value = getCellFriendlyText(theReader[12].ToString());

                            if (String.IsNullOrEmpty(theReader[13].ToString()))
                            {
                                notReadySheet.Cells[rowIndex, 10].Value = "No Photos Available";
                            }
                            else
                            {
                                string[] photoIds = theReader[13].ToString().Split(',');
                                for (int j = 0; j < Int32.Parse(theReader[10].ToString()); j++)
                                    notReadySheet.Cells[rowIndex, j + 10].Value = baseWebURL + "/photos/" + photoIds[j] + ".jpg";
                            }
                            notReadySheet.Cells.AutoFitColumns();
                        }
                        theResponse.statusCode = 0;
                        theResponse.statusDescription = extractFilename(newFilePath);
                    }
                    else
                    {
                        theResponse.statusCode = 1;
                        theResponse.statusDescription = "There is no data logged between the dates that were selected";
                    }
                    theReader.Close();

                    SqlCommand cmdReportNotReady = new SqlCommand("ReportStoresReadyWithInterval", theConnection);
                    cmdReportNotReady.Parameters.AddWithValue("@dateStarted", startDate);
                    cmdReportNotReady.Parameters.AddWithValue("@dateEnded", endDate);
                    cmdReportNotReady.CommandType = System.Data.CommandType.StoredProcedure;
                    cmdReportNotReady.CommandTimeout = 1200;

                    theReader = cmdReportNotReady.ExecuteReader();
                    if (theReader.HasRows)
                    {
                        int rowIndex = 1;
                        ExcelWorksheet readySheet = workBook.Worksheets["DetailedViewReady"];

                        while (theReader.Read())
                        {
                            rowIndex++;

                            readySheet.Cells[rowIndex, 1].Value = theReader[0].ToString();
                            readySheet.Cells[rowIndex, 2].Value = theReader[1].ToString();
                            readySheet.Cells[rowIndex, 3].Value = theReader[2].ToString();
                            readySheet.Cells[rowIndex, 4].Value = theReader[3].ToString();
                            readySheet.Cells[rowIndex, 5].Value = theReader[4].ToString();

                        }
                        theResponse.statusCode = 0;
                        theResponse.statusDescription = extractFilename(newFilePath);
                    }
                    else
                    {
                        theResponse.statusCode = 1;
                        theResponse.statusDescription = "There is no data logged between the dates that were selected";
                    }
                    theReader.Close();

                    ExcelWorksheet sheetSummary = workBook.Worksheets["Summary"];

                    sheetSummary.Cells["A2:C2"].Merge = true;
                    sheetSummary.Cells[2, 1].Value = startDate + " to " + endDate;

                    if (dvprvpData != null && dvprvpData.Count > 0)
                    {
                        int currentRowIndex = 3;

                        for (int i = 0, l = dvprvpData.Count; i < l; i++)
                        {
                            DVPRVPSummary thisRowsData = dvprvpData[i];

                            thisRowsData.percentageStoresReady = Math.Round((100 - ((double)thisRowsData.deliveriesWithIssues / (double)thisRowsData.deliveries) * 100.0), 2);

                            sheetSummary.Cells[currentRowIndex, 1].Value = thisRowsData.dvpName;
                            sheetSummary.Cells[currentRowIndex, 2].Value = thisRowsData.rvpName;
                            sheetSummary.Cells[currentRowIndex, 3].Value = thisRowsData.percentageStoresReady.ToString() + " %";
                            sheetSummary.Cells[currentRowIndex, 4].Value = thisRowsData.deliveries;
                            sheetSummary.Cells[currentRowIndex, 5].Value = thisRowsData.deliveriesWithIssues;
                            sheetSummary.Cells[currentRowIndex, 6].Value = thisRowsData.totalReadinessIssues;

                            currentRowIndex++;
                        }

                        theResponse.statusCode = 0;
                        theResponse.statusDescription = extractFilename(newFilePath);
                    }
                    else
                    {
                        theResponse.statusCode = 1;
                        theResponse.statusDescription = "There is no data logged between the dates that were selected";
                    }

                    FileStream fileSave = new FileStream(newFilePath, FileMode.Create);

                    pck.SaveAs(fileSave);
                    fileSave.Close();

                    closeDataConnection();
                }
                catch (Exception _exception)
                {
                    theResponse.statusCode = 6;
                    theResponse.statusDescription = _exception.Message;
                }

                return theResponse;
            }
            else
            {
                //return ReportStoreReadinessForCDC();
                return null;
            }
        }

        public Response DotNetReportStoreReadinessForCDCForProvider(string providerID, string startDate, string endDate, string startHour, string endHour)
        {
            startDate = startDate + " " + startHour + ":00:00";
            endDate = endDate + " " + endHour + ":00:00";

            if (validateDate(startDate) && validateDate(endDate))
            {
                if (endDate.Length <= 10)
                {
                    endDate += " 23:59:59";
                }

                Response theResponse = new Response();

                openDataConnection();

                /* Summary Data */

                SqlCommand cmdDVPRVPDeliveriesWithoutIssues = new SqlCommand("ReportNumberOfDeliveriesWithoutIssuesWithIntervalForCDCWithProviderID", theConnection);
                cmdDVPRVPDeliveriesWithoutIssues.Parameters.AddWithValue("@dateStarted", startDate);
                cmdDVPRVPDeliveriesWithoutIssues.Parameters.AddWithValue("@dateEnded", endDate);
                cmdDVPRVPDeliveriesWithoutIssues.Parameters.AddWithValue("@providerID", Int32.Parse(providerID));
                cmdDVPRVPDeliveriesWithoutIssues.CommandType = System.Data.CommandType.StoredProcedure;
                cmdDVPRVPDeliveriesWithoutIssues.CommandTimeout = 1200;

                theReader = cmdDVPRVPDeliveriesWithoutIssues.ExecuteReader();

                List<DVPRVPSummary> dvprvpData = new List<DVPRVPSummary>();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        DVPRVPSummary thisRow = new DVPRVPSummary();

                        thisRow.dvpName = theReader["ProviderName"].ToString();
                        thisRow.rvpName = theReader["CDCName"].ToString();
                        thisRow.deliveries = (int)theReader["NumberOfDeliveriesWithoutIssues"];

                        dvprvpData.Add(thisRow);
                    }
                }

                theReader.Close();

                SqlCommand cmdDVPRVPDeliveriesWithIssues = new SqlCommand("ReportNumberOfDeliveriesWithIssuesWithIntervalForCDCWithProviderID", theConnection);
                cmdDVPRVPDeliveriesWithIssues.Parameters.AddWithValue("@dateStarted", startDate);
                cmdDVPRVPDeliveriesWithIssues.Parameters.AddWithValue("@dateEnded", endDate);
                cmdDVPRVPDeliveriesWithIssues.Parameters.AddWithValue("@providerID", Int32.Parse(providerID));
                cmdDVPRVPDeliveriesWithIssues.CommandType = System.Data.CommandType.StoredProcedure;
                cmdDVPRVPDeliveriesWithIssues.CommandTimeout = 1200;

                theReader = cmdDVPRVPDeliveriesWithIssues.ExecuteReader();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        string thisRowsDVP = theReader["ProviderName"].ToString();
                        string thisRowsRVP = theReader["CDCName"].ToString();

                        bool found = false;

                        for (int i = 0, l = dvprvpData.Count; i < l; i++)
                        {
                            DVPRVPSummary thisRowData = dvprvpData[i];

                            if (thisRowsDVP.Equals(thisRowData.dvpName) && thisRowsRVP.Equals(thisRowData.rvpName))
                            {
                                found = true;

                                int deliveriesWithIssues = (int)theReader["NumberOfDeliveriesWithIssues"];

                                thisRowData.deliveriesWithIssues = deliveriesWithIssues;
                                thisRowData.deliveries += thisRowData.deliveriesWithIssues;
                            }
                        }

                        if (!found)
                        {
                            DVPRVPSummary thisRow = new DVPRVPSummary();

                            thisRow.dvpName = thisRowsDVP;
                            thisRow.rvpName = thisRowsRVP;
                            thisRow.deliveriesWithIssues = (int)theReader["NumberOfDeliveriesWithIssues"];
                            thisRow.deliveries += thisRow.deliveriesWithIssues;

                            dvprvpData.Add(thisRow);

                            found = false;
                        }
                    }
                }

                theReader.Close();

                SqlCommand cmdDVPRVPNumberOfIssues = new SqlCommand("ReportNumberOfIssuesWithIntervalForCDCWithProviderID", theConnection);
                cmdDVPRVPNumberOfIssues.Parameters.AddWithValue("@dateStarted", startDate);
                cmdDVPRVPNumberOfIssues.Parameters.AddWithValue("@dateEnded", endDate);
                cmdDVPRVPNumberOfIssues.Parameters.AddWithValue("@providerID", Int32.Parse(providerID));
                cmdDVPRVPNumberOfIssues.CommandType = System.Data.CommandType.StoredProcedure;
                cmdDVPRVPNumberOfIssues.CommandTimeout = 1200;

                theReader = cmdDVPRVPNumberOfIssues.ExecuteReader();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        string thisRowsDVP = theReader["ProviderName"].ToString();
                        string thisRowsRVP = theReader["CDCName"].ToString();

                        bool found = false;

                        for (int i = 0, l = dvprvpData.Count; i < l; i++)
                        {
                            DVPRVPSummary thisRowData = dvprvpData[i];

                            if (thisRowsDVP.Equals(thisRowData.dvpName) && thisRowsRVP.Equals(thisRowData.rvpName))
                            {
                                found = true;

                                int numberOfIssues = (int)theReader["NumberOfIssues"];

                                thisRowData.totalReadinessIssues = numberOfIssues;
                            }
                        }

                        if (!found)
                        {
                            DVPRVPSummary thisRow = new DVPRVPSummary();

                            thisRow.dvpName = thisRowsDVP;
                            thisRow.rvpName = thisRowsRVP;
                            thisRow.totalReadinessIssues = (int)theReader["NumberOfIssues"];

                            dvprvpData.Add(thisRow);

                            found = false;
                        }
                    }
                }

                theReader.Close();

                SqlCommand cmdUnitsLeftout = new SqlCommand("ReportUnitsLeftoutWithIntervalForCDCWithProviderID", theConnection);
                cmdUnitsLeftout.Parameters.AddWithValue("@dateStarted", startDate);
                cmdUnitsLeftout.Parameters.AddWithValue("@dateEnded", endDate);
                cmdUnitsLeftout.Parameters.AddWithValue("@providerID", Int32.Parse(providerID));
                cmdUnitsLeftout.CommandType = System.Data.CommandType.StoredProcedure;
                cmdUnitsLeftout.CommandTimeout = 1200;

                theReader = cmdUnitsLeftout.ExecuteReader();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        string thisRowsDVP = theReader["ProviderName"].ToString();
                        string thisRowsRVP = theReader["CDCName"].ToString();

                        bool found = false;

                        for (int i = 0, l = dvprvpData.Count; i < l; i++)
                        {
                            DVPRVPSummary thisRowData = dvprvpData[i];

                            if (thisRowsDVP.Equals(thisRowData.dvpName) && thisRowsRVP.Equals(thisRowData.rvpName))
                            {
                                found = true;

                                int unitsLeftout = (int)theReader["UnitsLeftout"];

                                thisRowData.leftoutUnits = unitsLeftout;
                            }
                        }

                        if (!found)
                        {
                            DVPRVPSummary thisRow = new DVPRVPSummary();

                            thisRow.dvpName = thisRowsDVP;
                            thisRow.rvpName = thisRowsRVP;
                            thisRow.leftoutUnits = (int)theReader["UnitsLeftout"];

                            dvprvpData.Add(thisRow);

                            found = false;
                        }
                    }
                }

                theReader.Close();

                SqlCommand cmdDVPRVPUnitsBackhauled = new SqlCommand("ReportUnitsBackhauledWithIntervalForCDCWithProviderID", theConnection);
                cmdDVPRVPUnitsBackhauled.Parameters.AddWithValue("@dateStarted", startDate);
                cmdDVPRVPUnitsBackhauled.Parameters.AddWithValue("@dateEnded", endDate);
                cmdDVPRVPUnitsBackhauled.Parameters.AddWithValue("@providerID", Int32.Parse(providerID));
                cmdDVPRVPUnitsBackhauled.CommandType = System.Data.CommandType.StoredProcedure;
                cmdDVPRVPUnitsBackhauled.CommandTimeout = 1200;

                theReader = cmdDVPRVPUnitsBackhauled.ExecuteReader();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        string thisRowsDVP = theReader["ProviderName"].ToString();
                        string thisRowsRVP = theReader["CDCName"].ToString();

                        bool found = false;

                        for (int i = 0, l = dvprvpData.Count; i < l; i++)
                        {
                            DVPRVPSummary thisRowData = dvprvpData[i];

                            if (thisRowsDVP.Equals(thisRowData.dvpName) && thisRowsRVP.Equals(thisRowData.rvpName))
                            {
                                found = true;

                                int unitsBackhauled = (int)theReader["UnitsBackhauled"];

                                thisRowData.dairyBackhaulUnits = unitsBackhauled;
                            }
                        }

                        if (!found)
                        {
                            DVPRVPSummary thisRow = new DVPRVPSummary();

                            thisRow.dvpName = thisRowsDVP;
                            thisRow.rvpName = thisRowsRVP;
                            thisRow.dairyBackhaulUnits = (int)theReader["UnitsBackhauled"];

                            dvprvpData.Add(thisRow);

                            found = false;
                        }
                    }
                }

                theReader.Close();

                SqlCommand cmdDVPRVPUnitsBackhauledCost = new SqlCommand("ReportUnitsBackhauledCostWithIntervalForCDCWithProviderID", theConnection);
                cmdDVPRVPUnitsBackhauledCost.Parameters.AddWithValue("@dateStarted", startDate);
                cmdDVPRVPUnitsBackhauledCost.Parameters.AddWithValue("@dateEnded", endDate);
                cmdDVPRVPUnitsBackhauledCost.Parameters.AddWithValue("@providerID", Int32.Parse(providerID));
                cmdDVPRVPUnitsBackhauledCost.CommandType = System.Data.CommandType.StoredProcedure;
                cmdDVPRVPUnitsBackhauledCost.CommandTimeout = 1200;

                theReader = cmdDVPRVPUnitsBackhauledCost.ExecuteReader();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        string thisRowsDVP = theReader["ProviderName"].ToString();
                        string thisRowsRVP = theReader["CDCName"].ToString();

                        bool found = false;

                        for (int i = 0, l = dvprvpData.Count; i < l; i++)
                        {
                            DVPRVPSummary thisRowData = dvprvpData[i];

                            if (thisRowsDVP.Equals(thisRowData.dvpName) && thisRowsRVP.Equals(thisRowData.rvpName))
                            {
                                found = true;

                                if (theReader["BackhaulCost"] != DBNull.Value)
                                {
                                    double backhaulCost = (double)theReader["BackhaulCost"];

                                    thisRowData.dairyBackhaulCOGS = backhaulCost;
                                }
                            }
                        }

                        if (!found)
                        {
                            DVPRVPSummary thisRow = new DVPRVPSummary();

                            thisRow.dvpName = thisRowsDVP;
                            thisRow.rvpName = thisRowsRVP;
                            thisRow.dairyBackhaulCOGS = (double)theReader["BackhaulCost"];

                            dvprvpData.Add(thisRow);

                            found = false;
                        }
                    }
                }

                theReader.Close();

                closeDataConnection();

                try
                {
                    int wantedProviderID = Int32.Parse(providerID);

                    string wantedProviderName = getProviderNameFromID(wantedProviderID);

                    string newFilePath = CopyReportTemplate("cdc");

                    openDataConnection();
                    SqlCommand cmdReport = new SqlCommand("ReportStoresNotReadyWithProviderIDWithInterval", theConnection);
                    cmdReport.Parameters.AddWithValue("@dateStarted", startDate);
                    cmdReport.Parameters.AddWithValue("@dateEnded", endDate);
                    cmdReport.Parameters.AddWithValue("@providerID", Int32.Parse(providerID));
                    cmdReport.CommandType = System.Data.CommandType.StoredProcedure;
                    cmdReport.CommandTimeout = 1200;

                    theReader = cmdReport.ExecuteReader();

                    FileStream template = new FileStream(newFilePath, FileMode.Open, FileAccess.Read);

                    ExcelPackage pck = new OfficeOpenXml.ExcelPackage(template);
                    ExcelWorkbook workBook = pck.Workbook;

                    template.Close();

                    if (theReader.HasRows)
                    {
                        int rowIndex = 1;

                        ExcelWorksheet notReadySheet = workBook.Worksheets["DetailedViewNotReady"];

                        while (theReader.Read())
                        {
                            rowIndex++;

                            notReadySheet.Cells[rowIndex, 1].Value = theReader[0].ToString();
                            notReadySheet.Cells[rowIndex, 2].Value = theReader[1].ToString();
                            notReadySheet.Cells[rowIndex, 3].Value = theReader[2].ToString();
                            notReadySheet.Cells[rowIndex, 4].Value = theReader[3].ToString();
                            notReadySheet.Cells[rowIndex, 5].Value = theReader[4].ToString();
                            notReadySheet.Cells[rowIndex, 6].Value = theReader[9].ToString();
                            notReadySheet.Cells[rowIndex, 7].Value = theReader[10].ToString();
                            notReadySheet.Cells[rowIndex, 8].Value = theReader[11].ToString();
                            notReadySheet.Cells[rowIndex, 9].Value = getCellFriendlyText(theReader[12].ToString());
                            if (String.IsNullOrEmpty(theReader[13].ToString()))
                            {
                                notReadySheet.Cells[rowIndex, 10].Value = "No Photos Available";
                            }
                            else
                            {
                                string[] photoIds = theReader[13].ToString().Split(',');
                                for (int j = 0; j < Int32.Parse(theReader[10].ToString()); j++)
                                    notReadySheet.Cells[rowIndex, j + 10].Value = baseWebURL + "/photos/" + photoIds[j] + ".jpg";
                            }
                            notReadySheet.Cells.AutoFitColumns();

                        }
                        theResponse.statusCode = 0;
                        theResponse.statusDescription = extractFilename(newFilePath);
                    }
                    else
                    {
                        theResponse.statusCode = 1;
                        theResponse.statusDescription = "There is no data logged between the dates that were selected";
                    }
                    theReader.Close();



                    SqlCommand cmdReportNotReady = new SqlCommand("ReportStoresReadyWithProviderIDWithInterval", theConnection);
                    cmdReportNotReady.Parameters.AddWithValue("@dateStarted", startDate);
                    cmdReportNotReady.Parameters.AddWithValue("@dateEnded", endDate);
                    cmdReportNotReady.Parameters.AddWithValue("@providerID", Int32.Parse(providerID));
                    cmdReportNotReady.CommandType = System.Data.CommandType.StoredProcedure;
                    cmdReportNotReady.CommandTimeout = 1200;

                    theReader = cmdReportNotReady.ExecuteReader();
                    if (theReader.HasRows)
                    {
                        int rowIndex = 1;
                        ExcelWorksheet readySheet = workBook.Worksheets["DetailedViewReady"];

                        while (theReader.Read())
                        {
                            rowIndex++;

                            readySheet.Cells[rowIndex, 1].Value = theReader[0].ToString();
                            readySheet.Cells[rowIndex, 2].Value = theReader[1].ToString();
                            readySheet.Cells[rowIndex, 3].Value = theReader[2].ToString();
                            readySheet.Cells[rowIndex, 4].Value = theReader[3].ToString();
                            readySheet.Cells[rowIndex, 5].Value = theReader[4].ToString();

                        }
                        theResponse.statusCode = 0;
                        theResponse.statusDescription = extractFilename(newFilePath);
                    }
                    else
                    {
                        theResponse.statusCode = 1;
                        theResponse.statusDescription = "There is no data logged between the dates that were selected";
                    }
                    theReader.Close();

                    ExcelWorksheet sheetSummary = workBook.Worksheets["Summary"];

                    sheetSummary.Cells["A2:C2"].Merge = true;
                    sheetSummary.Cells[2, 1].Value = "All Data";

                    if (dvprvpData != null && dvprvpData.Count > 0)
                    {
                        int currentRowIndex = 3;

                        for (int i = 0, l = dvprvpData.Count; i < l; i++)
                        {
                            DVPRVPSummary thisRowsData = dvprvpData[i];

                            thisRowsData.percentageStoresReady = Math.Round((100 - ((double)thisRowsData.deliveriesWithIssues / (double)thisRowsData.deliveries) * 100.0), 2);

                            sheetSummary.Cells[currentRowIndex, 1].Value = thisRowsData.dvpName;
                            sheetSummary.Cells[currentRowIndex, 2].Value = thisRowsData.rvpName;
                            sheetSummary.Cells[currentRowIndex, 3].Value = thisRowsData.percentageStoresReady.ToString() + " %";
                            sheetSummary.Cells[currentRowIndex, 4].Value = thisRowsData.deliveries;
                            sheetSummary.Cells[currentRowIndex, 5].Value = thisRowsData.deliveriesWithIssues;
                            sheetSummary.Cells[currentRowIndex, 6].Value = thisRowsData.totalReadinessIssues;

                            currentRowIndex++;
                        }

                        theResponse.statusCode = 0;
                        theResponse.statusDescription = extractFilename(newFilePath);
                    }
                    else
                    {
                        theResponse.statusCode = 1;
                        theResponse.statusDescription = "There is no data logged between the dates that were selected";
                    }



                    FileStream fileSave = new FileStream(newFilePath, FileMode.Create);

                    pck.SaveAs(fileSave);
                    fileSave.Close();

                    closeDataConnection();
                }
                catch (Exception _exception)
                {
                    theResponse.statusCode = 6;
                    theResponse.statusDescription = _exception.Message + " " + _exception.StackTrace;
                }

                return theResponse;
            }
            else
            {
                //return ReportStoreReadinessForCDC();
                return null;
            }
        }

        public Response DotNetReportFieldReadiness(string startDate, string endDate, string startHour, string endHour)
        {
            startDate = startDate + " " + startHour + ":00:00";
            endDate = endDate + " " + endHour + ":00:00";

            if (validateDate(startDate) && validateDate(endDate))
            {
                if (endDate.Length <= 10)
                {
                    endDate += " 23:59:59";
                }

                Response theResponse = new Response();

                openDataConnection();

                /* DVP - RVP Data */

                SqlCommand cmdDVPRVPDeliveriesWithoutIssues = new SqlCommand("ReportDVPRVPNumberOfDeliveriesWithoutIssuesWithInterval", theConnection);
                cmdDVPRVPDeliveriesWithoutIssues.Parameters.AddWithValue("@dateStarted", startDate);
                cmdDVPRVPDeliveriesWithoutIssues.Parameters.AddWithValue("@dateEnded", endDate);
                cmdDVPRVPDeliveriesWithoutIssues.CommandType = System.Data.CommandType.StoredProcedure;
                cmdDVPRVPDeliveriesWithoutIssues.CommandTimeout = 1200;

                theReader = cmdDVPRVPDeliveriesWithoutIssues.ExecuteReader();

                List<DVPRVPSummary> dvprvpData = new List<DVPRVPSummary>();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        DVPRVPSummary thisRow = new DVPRVPSummary();

                        thisRow.dvpName = theReader["DVPOutlookName"].ToString();
                        thisRow.rvpName = theReader["RVPOutlookName"].ToString();
                        thisRow.deliveries = (int)theReader["NumberOfDeliveriesWithoutIssues"];

                        dvprvpData.Add(thisRow);
                    }
                }

                theReader.Close();

                SqlCommand cmdDVPRVPDeliveriesWithIssues = new SqlCommand("ReportDVPRVPNumberOfDeliveriesWithIssuesWithInterval", theConnection);
                cmdDVPRVPDeliveriesWithIssues.Parameters.AddWithValue("@dateStarted", startDate);
                cmdDVPRVPDeliveriesWithIssues.Parameters.AddWithValue("@dateEnded", endDate);
                cmdDVPRVPDeliveriesWithIssues.CommandType = System.Data.CommandType.StoredProcedure;
                cmdDVPRVPDeliveriesWithIssues.CommandTimeout = 1200;

                theReader = cmdDVPRVPDeliveriesWithIssues.ExecuteReader();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        string thisRowsDVP = theReader["DVPOutlookName"].ToString();
                        string thisRowsRVP = theReader["RVPOutlookName"].ToString();

                        bool found = false;

                        for (int i = 0, l = dvprvpData.Count; i < l; i++)
                        {
                            DVPRVPSummary thisRowData = dvprvpData[i];

                            if (thisRowsDVP.Equals(thisRowData.dvpName) && thisRowsRVP.Equals(thisRowData.rvpName))
                            {
                                found = true;

                                int deliveriesWithIssues = (int)theReader["NumberOfDeliveriesWithIssues"];

                                thisRowData.deliveriesWithIssues = deliveriesWithIssues;
                                thisRowData.deliveries += thisRowData.deliveriesWithIssues;
                            }
                        }

                        if (!found)
                        {
                            DVPRVPSummary thisRow = new DVPRVPSummary();

                            thisRow.dvpName = thisRowsDVP;
                            thisRow.rvpName = thisRowsRVP;
                            thisRow.deliveriesWithIssues = (int)theReader["NumberOfDeliveriesWithIssues"];
                            thisRow.deliveries += thisRow.deliveriesWithIssues;

                            dvprvpData.Add(thisRow);

                            found = false;
                        }
                    }
                }

                theReader.Close();

                SqlCommand cmdDVPRVPNumberOfIssues = new SqlCommand("ReportDVPRVPNumberOfIssuesWithInterval", theConnection);
                cmdDVPRVPNumberOfIssues.Parameters.AddWithValue("@dateStarted", startDate);
                cmdDVPRVPNumberOfIssues.Parameters.AddWithValue("@dateEnded", endDate);
                cmdDVPRVPNumberOfIssues.CommandType = System.Data.CommandType.StoredProcedure;
                cmdDVPRVPNumberOfIssues.CommandTimeout = 1200;

                theReader = cmdDVPRVPNumberOfIssues.ExecuteReader();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        string thisRowsDVP = theReader["DVPOutlookName"].ToString();
                        string thisRowsRVP = theReader["RVPOutlookName"].ToString();

                        bool found = false;

                        for (int i = 0, l = dvprvpData.Count; i < l; i++)
                        {
                            DVPRVPSummary thisRowData = dvprvpData[i];

                            if (thisRowsDVP.Equals(thisRowData.dvpName) && thisRowsRVP.Equals(thisRowData.rvpName))
                            {
                                found = true;

                                int numberOfIssues = (int)theReader["NumberOfIssues"];

                                thisRowData.totalReadinessIssues = numberOfIssues;
                            }
                        }

                        if (!found)
                        {
                            DVPRVPSummary thisRow = new DVPRVPSummary();

                            thisRow.dvpName = thisRowsDVP;
                            thisRow.rvpName = thisRowsRVP;
                            thisRow.totalReadinessIssues = (int)theReader["NumberOfIssues"];

                            dvprvpData.Add(thisRow);

                            found = false;
                        }
                    }
                }

                theReader.Close();

                SqlCommand cmdDVPRVPUnitsLeftout = new SqlCommand("ReportDVPRVPUnitsLeftoutWithInterval", theConnection);
                cmdDVPRVPUnitsLeftout.Parameters.AddWithValue("@dateStarted", startDate);
                cmdDVPRVPUnitsLeftout.Parameters.AddWithValue("@dateEnded", endDate);
                cmdDVPRVPUnitsLeftout.CommandType = System.Data.CommandType.StoredProcedure;
                cmdDVPRVPUnitsLeftout.CommandTimeout = 1200;

                theReader = cmdDVPRVPUnitsLeftout.ExecuteReader();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        string thisRowsDVP = theReader["DVPOutlookName"].ToString();
                        string thisRowsRVP = theReader["RVPOutlookName"].ToString();

                        bool found = false;

                        for (int i = 0, l = dvprvpData.Count; i < l; i++)
                        {
                            DVPRVPSummary thisRowData = dvprvpData[i];

                            if (thisRowsDVP.Equals(thisRowData.dvpName) && thisRowsRVP.Equals(thisRowData.rvpName))
                            {
                                found = true;

                                int unitsLeftout = (int)theReader["UnitsLeftout"];

                                thisRowData.leftoutUnits = unitsLeftout;
                            }
                        }

                        if (!found)
                        {
                            DVPRVPSummary thisRow = new DVPRVPSummary();

                            thisRow.dvpName = thisRowsDVP;
                            thisRow.rvpName = thisRowsRVP;
                            thisRow.leftoutUnits = (int)theReader["UnitsLeftout"];

                            dvprvpData.Add(thisRow);

                            found = false;
                        }
                    }
                }

                theReader.Close();

                SqlCommand cmdDVPRVPUnitsLeftoutCost = new SqlCommand("ReportDVPRVPUnitsLeftoutCostWithInterval", theConnection);
                cmdDVPRVPUnitsLeftoutCost.Parameters.AddWithValue("@dateStarted", startDate);
                cmdDVPRVPUnitsLeftoutCost.Parameters.AddWithValue("@dateEnded", endDate);
                cmdDVPRVPUnitsLeftoutCost.CommandType = System.Data.CommandType.StoredProcedure;
                cmdDVPRVPUnitsLeftoutCost.CommandTimeout = 1200;

                theReader = cmdDVPRVPUnitsLeftoutCost.ExecuteReader();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        string thisRowsDVP = theReader["DVPOutlookName"].ToString();
                        string thisRowsRVP = theReader["RVPOutlookName"].ToString();

                        bool found = false;

                        for (int i = 0, l = dvprvpData.Count; i < l; i++)
                        {
                            DVPRVPSummary thisRowData = dvprvpData[i];

                            if (thisRowsDVP.Equals(thisRowData.dvpName) && thisRowsRVP.Equals(thisRowData.rvpName))
                            {
                                found = true;

                                if (theReader["LeftoutCost"] != DBNull.Value)
                                {
                                    double leftoutCost = (double)theReader["LeftoutCost"];

                                    thisRowData.leftoutCOGS = leftoutCost;
                                }
                            }
                        }

                        if (!found)
                        {
                            DVPRVPSummary thisRow = new DVPRVPSummary();

                            thisRow.dvpName = thisRowsDVP;
                            thisRow.rvpName = thisRowsRVP;
                            thisRow.leftoutCOGS = (double)theReader["LeftoutCost"];

                            dvprvpData.Add(thisRow);

                            found = false;
                        }
                    }
                }

                theReader.Close();

                SqlCommand cmdDVPRVPUnitsBackhauled = new SqlCommand("ReportDVPRVPUnitsBackhauledWithInterval", theConnection);
                cmdDVPRVPUnitsBackhauled.Parameters.AddWithValue("@dateStarted", startDate);
                cmdDVPRVPUnitsBackhauled.Parameters.AddWithValue("@dateEnded", endDate);
                cmdDVPRVPUnitsBackhauled.CommandType = System.Data.CommandType.StoredProcedure;
                cmdDVPRVPUnitsBackhauled.CommandTimeout = 1200;

                theReader = cmdDVPRVPUnitsBackhauled.ExecuteReader();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        string thisRowsDVP = theReader["DVPOutlookName"].ToString();
                        string thisRowsRVP = theReader["RVPOutlookName"].ToString();

                        bool found = false;

                        for (int i = 0, l = dvprvpData.Count; i < l; i++)
                        {
                            DVPRVPSummary thisRowData = dvprvpData[i];

                            if (thisRowsDVP.Equals(thisRowData.dvpName) && thisRowsRVP.Equals(thisRowData.rvpName))
                            {
                                found = true;

                                int unitsBackhauled = (int)theReader["UnitsBackhauled"];

                                thisRowData.dairyBackhaulUnits = unitsBackhauled;
                            }
                        }

                        if (!found)
                        {
                            DVPRVPSummary thisRow = new DVPRVPSummary();

                            thisRow.dvpName = thisRowsDVP;
                            thisRow.rvpName = thisRowsRVP;
                            thisRow.dairyBackhaulUnits = (int)theReader["UnitsBackhauled"];

                            dvprvpData.Add(thisRow);

                            found = false;
                        }
                    }
                }

                theReader.Close();

                SqlCommand cmdDVPRVPUnitsBackhauledCost = new SqlCommand("ReportDVPRVPUnitsBackhauledCostWithInterval", theConnection);
                cmdDVPRVPUnitsBackhauledCost.Parameters.AddWithValue("@dateStarted", startDate);
                cmdDVPRVPUnitsBackhauledCost.Parameters.AddWithValue("@dateEnded", endDate);
                cmdDVPRVPUnitsBackhauledCost.CommandType = System.Data.CommandType.StoredProcedure;
                cmdDVPRVPUnitsBackhauledCost.CommandTimeout = 1200;

                theReader = cmdDVPRVPUnitsBackhauledCost.ExecuteReader();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        string thisRowsDVP = theReader["DVPOutlookName"].ToString();
                        string thisRowsRVP = theReader["RVPOutlookName"].ToString();

                        bool found = false;

                        for (int i = 0, l = dvprvpData.Count; i < l; i++)
                        {
                            DVPRVPSummary thisRowData = dvprvpData[i];

                            if (thisRowsDVP.Equals(thisRowData.dvpName) && thisRowsRVP.Equals(thisRowData.rvpName))
                            {
                                found = true;

                                if (theReader["BackhaulCost"] != DBNull.Value)
                                {
                                    double backhaulCost = (double)theReader["BackhaulCost"];

                                    thisRowData.dairyBackhaulCOGS = backhaulCost;
                                }
                            }
                        }

                        if (!found)
                        {
                            DVPRVPSummary thisRow = new DVPRVPSummary();

                            thisRow.dvpName = thisRowsDVP;
                            thisRow.rvpName = thisRowsRVP;
                            thisRow.dairyBackhaulCOGS = (double)theReader["BackhaulCost"];

                            dvprvpData.Add(thisRow);

                            found = false;
                        }
                    }
                }

                theReader.Close();

                /* RD-DM Data */

                SqlCommand cmdRDDMDeliveriesWithoutIssues = new SqlCommand("ReportRDDMNumberOfDeliveriesWithoutIssuesWithInterval", theConnection);
                cmdRDDMDeliveriesWithoutIssues.Parameters.AddWithValue("@dateStarted", startDate);
                cmdRDDMDeliveriesWithoutIssues.Parameters.AddWithValue("@dateEnded", endDate);
                cmdRDDMDeliveriesWithoutIssues.CommandType = System.Data.CommandType.StoredProcedure;
                cmdRDDMDeliveriesWithoutIssues.CommandTimeout = 1200;

                theReader = cmdRDDMDeliveriesWithoutIssues.ExecuteReader();

                List<RDDMSummary> rddmData = new List<RDDMSummary>();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        RDDMSummary thisRow = new RDDMSummary();

                        thisRow.rdName = theReader["RDOutlookName"].ToString();
                        thisRow.dmName = theReader["DMOutlookName"].ToString();
                        thisRow.deliveries = (int)theReader["NumberOfDeliveriesWithoutIssues"];

                        rddmData.Add(thisRow);
                    }
                }

                theReader.Close();

                SqlCommand cmdRDDMDeliveriesWithIssues = new SqlCommand("ReportRDDMNumberOfDeliveriesWithIssuesWithInterval", theConnection);
                cmdRDDMDeliveriesWithIssues.Parameters.AddWithValue("@dateStarted", startDate);
                cmdRDDMDeliveriesWithIssues.Parameters.AddWithValue("@dateEnded", endDate);
                cmdRDDMDeliveriesWithIssues.CommandType = System.Data.CommandType.StoredProcedure;
                cmdRDDMDeliveriesWithIssues.CommandTimeout = 1200;

                theReader = cmdRDDMDeliveriesWithIssues.ExecuteReader();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        string thisRowsRD = theReader["RDOutlookName"].ToString();
                        string thisRowsDM = theReader["DMOutlookName"].ToString();

                        bool found = false;

                        for (int i = 0, l = rddmData.Count; i < l; i++)
                        {
                            RDDMSummary thisRowData = rddmData[i];

                            if (thisRowsRD.Equals(thisRowData.rdName) && thisRowsDM.Equals(thisRowData.dmName))
                            {
                                found = true;

                                int deliveriesWithIssues = (int)theReader["NumberOfDeliveriesWithIssues"];

                                thisRowData.deliveriesWithIssues = deliveriesWithIssues;
                                thisRowData.deliveries += thisRowData.deliveriesWithIssues;
                            }
                        }

                        if (!found)
                        {
                            RDDMSummary thisRow = new RDDMSummary();

                            thisRow.rdName = theReader["RDOutlookName"].ToString();
                            thisRow.dmName = theReader["DMOutlookName"].ToString();
                            thisRow.deliveriesWithIssues = (int)theReader["NumberOfDeliveriesWithIssues"];
                            thisRow.deliveries += thisRow.deliveriesWithIssues;

                            rddmData.Add(thisRow);

                            found = false;
                        }
                    }
                }

                theReader.Close();

                SqlCommand cmdRDDMNumberOfIssues = new SqlCommand("ReportRDDMNumberOfIssuesWithInterval", theConnection);
                cmdRDDMNumberOfIssues.Parameters.AddWithValue("@dateStarted", startDate);
                cmdRDDMNumberOfIssues.Parameters.AddWithValue("@dateEnded", endDate);
                cmdRDDMNumberOfIssues.CommandType = System.Data.CommandType.StoredProcedure;
                cmdRDDMNumberOfIssues.CommandTimeout = 1200;

                theReader = cmdRDDMNumberOfIssues.ExecuteReader();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        string thisRowsRD = theReader["RDOutlookName"].ToString();
                        string thisRowsDM = theReader["DMOutlookName"].ToString();

                        bool found = false;

                        for (int i = 0, l = rddmData.Count; i < l; i++)
                        {
                            RDDMSummary thisRowData = rddmData[i];

                            if (thisRowsRD.Equals(thisRowData.rdName) && thisRowsDM.Equals(thisRowData.dmName))
                            {
                                found = true;

                                int numberOfIssues = (int)theReader["NumberOfIssues"];

                                thisRowData.totalReadinessIssues = numberOfIssues;
                            }
                        }

                        if (!found)
                        {
                            RDDMSummary thisRow = new RDDMSummary();

                            thisRow.rdName = theReader["RDOutlookName"].ToString();
                            thisRow.dmName = theReader["DMOutlookName"].ToString();
                            thisRow.totalReadinessIssues = (int)theReader["NumberOfIssues"];

                            rddmData.Add(thisRow);

                            found = false;
                        }
                    }
                }

                theReader.Close();

                SqlCommand cmdRDDMUnitsBackhauled = new SqlCommand("ReportRDDMUnitsBackhauledWithInterval", theConnection);
                cmdRDDMUnitsBackhauled.Parameters.AddWithValue("@dateStarted", startDate);
                cmdRDDMUnitsBackhauled.Parameters.AddWithValue("@dateEnded", endDate);
                cmdRDDMUnitsBackhauled.CommandType = System.Data.CommandType.StoredProcedure;
                cmdRDDMUnitsBackhauled.CommandTimeout = 1200;

                theReader = cmdRDDMUnitsBackhauled.ExecuteReader();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        string thisRowsRD = theReader["RDOutlookName"].ToString();
                        string thisRowsDM = theReader["DMOutlookName"].ToString();

                        bool found = false;

                        for (int i = 0, l = rddmData.Count; i < l; i++)
                        {
                            RDDMSummary thisRowData = rddmData[i];

                            if (thisRowsRD.Equals(thisRowData.rdName) && thisRowsDM.Equals(thisRowData.dmName))
                            {
                                found = true;

                                int unitsBackhauled = (int)theReader["UnitsBackhauled"];

                                thisRowData.dairyBackhaulUnits = unitsBackhauled;
                            }
                        }

                        if (!found)
                        {
                            RDDMSummary thisRow = new RDDMSummary();

                            thisRow.rdName = theReader["RDOutlookName"].ToString();
                            thisRow.dmName = theReader["DMOutlookName"].ToString();
                            thisRow.dairyBackhaulUnits = (int)theReader["UnitsBackhauled"];

                            rddmData.Add(thisRow);

                            found = false;
                        }
                    }
                }

                theReader.Close();

                SqlCommand cmdRDDMUnitsBackhauledCost = new SqlCommand("ReportRDDMUnitsBackhauledCostWithInterval", theConnection);
                cmdRDDMUnitsBackhauledCost.Parameters.AddWithValue("@dateStarted", startDate);
                cmdRDDMUnitsBackhauledCost.Parameters.AddWithValue("@dateEnded", endDate);
                cmdRDDMUnitsBackhauledCost.CommandType = System.Data.CommandType.StoredProcedure;
                cmdRDDMUnitsBackhauledCost.CommandTimeout = 1200;

                theReader = cmdRDDMUnitsBackhauledCost.ExecuteReader();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        string thisRowsRD = theReader["RDOutlookName"].ToString();
                        string thisRowsDM = theReader["DMOutlookName"].ToString();

                        bool found = false;

                        for (int i = 0, l = rddmData.Count; i < l; i++)
                        {
                            RDDMSummary thisRowData = rddmData[i];

                            if (thisRowsRD.Equals(thisRowData.rdName) && thisRowsDM.Equals(thisRowData.dmName))
                            {
                                found = true;

                                if (theReader["BackhaulCost"] != DBNull.Value)
                                {
                                    double backhaulCost = (double)theReader["BackhaulCost"];

                                    thisRowData.dairyBackhaulCOGS = backhaulCost;
                                }
                            }
                        }

                        if (!found)
                        {
                            RDDMSummary thisRow = new RDDMSummary();

                            thisRow.rdName = theReader["RDOutlookName"].ToString();
                            thisRow.dmName = theReader["DMOutlookName"].ToString();
                            thisRow.dairyBackhaulCOGS = (double)theReader["BackhaulCost"];

                            rddmData.Add(thisRow);

                            found = false;
                        }
                    }
                }

                theReader.Close();

                SqlCommand cmdRDDMNumberOfIssuesOne = new SqlCommand("ReportRDDMGroupOneIssuesWithInterval", theConnection);
                cmdRDDMNumberOfIssuesOne.Parameters.AddWithValue("@dateStarted", startDate);
                cmdRDDMNumberOfIssuesOne.Parameters.AddWithValue("@dateEnded", endDate);
                cmdRDDMNumberOfIssuesOne.CommandType = System.Data.CommandType.StoredProcedure;
                cmdRDDMNumberOfIssuesOne.CommandTimeout = 1200;

                theReader = cmdRDDMNumberOfIssues.ExecuteReader();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        string thisRowsRD = theReader["RDOutlookName"].ToString();
                        string thisRowsDM = theReader["DMOutlookName"].ToString();

                        bool found = false;

                        for (int i = 0, l = rddmData.Count; i < l; i++)
                        {
                            RDDMSummary thisRowData = rddmData[i];

                            if (thisRowsRD.Equals(thisRowData.rdName) && thisRowsDM.Equals(thisRowData.dmName))
                            {
                                found = true;

                                int numberOfIssues = (int)theReader["NumberOfIssues"];

                                thisRowData.totalSecurityFacilityIssues = numberOfIssues;
                            }
                        }

                        if (!found)
                        {
                            RDDMSummary thisRow = new RDDMSummary();

                            thisRow.rdName = theReader["RDOutlookName"].ToString();
                            thisRow.dmName = theReader["DMOutlookName"].ToString();
                            thisRow.totalSecurityFacilityIssues = (int)theReader["NumberOfIssues"];

                            rddmData.Add(thisRow);

                            found = false;
                        }
                    }
                }

                theReader.Close();

                SqlCommand cmdRDDMNumberOfIssuesTwo = new SqlCommand("ReportRDDMGroupTwoIssuesWithInterval", theConnection);
                cmdRDDMNumberOfIssuesTwo.Parameters.AddWithValue("@dateStarted", startDate);
                cmdRDDMNumberOfIssuesTwo.Parameters.AddWithValue("@dateEnded", endDate);
                cmdRDDMNumberOfIssuesTwo.CommandType = System.Data.CommandType.StoredProcedure;
                cmdRDDMNumberOfIssuesTwo.CommandTimeout = 1200;

                theReader = cmdRDDMNumberOfIssuesTwo.ExecuteReader();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        string thisRowsRD = theReader["RDOutlookName"].ToString();
                        string thisRowsDM = theReader["DMOutlookName"].ToString();

                        bool found = false;

                        for (int i = 0, l = rddmData.Count; i < l; i++)
                        {
                            RDDMSummary thisRowData = rddmData[i];

                            if (thisRowsRD.Equals(thisRowData.rdName) && thisRowsDM.Equals(thisRowData.dmName))
                            {
                                found = true;

                                int numberOfIssues = (int)theReader["NumberOfIssues"];

                                thisRowData.totalCapacityIssues = numberOfIssues;
                            }
                        }

                        if (!found)
                        {
                            RDDMSummary thisRow = new RDDMSummary();

                            thisRow.rdName = theReader["RDOutlookName"].ToString();
                            thisRow.dmName = theReader["DMOutlookName"].ToString();
                            thisRow.totalCapacityIssues = (int)theReader["NumberOfIssues"];

                            rddmData.Add(thisRow);

                            found = false;
                        }
                    }
                }

                theReader.Close();

                SqlCommand cmdRDDMNumberOfIssuesThree = new SqlCommand("ReportRDDMGroupThreeIssuesWithInterval", theConnection);
                cmdRDDMNumberOfIssuesThree.Parameters.AddWithValue("@dateStarted", startDate);
                cmdRDDMNumberOfIssuesThree.Parameters.AddWithValue("@dateEnded", endDate);
                cmdRDDMNumberOfIssuesThree.CommandType = System.Data.CommandType.StoredProcedure;
                cmdRDDMNumberOfIssuesThree.CommandTimeout = 1200;

                theReader = cmdRDDMNumberOfIssuesThree.ExecuteReader();

                if (theReader.HasRows)
                {
                    while (theReader.Read())
                    {
                        string thisRowsRD = theReader["RDOutlookName"].ToString();
                        string thisRowsDM = theReader["DMOutlookName"].ToString();

                        bool found = false;

                        for (int i = 0, l = rddmData.Count; i < l; i++)
                        {
                            RDDMSummary thisRowData = rddmData[i];

                            if (thisRowsRD.Equals(thisRowData.rdName) && thisRowsDM.Equals(thisRowData.dmName))
                            {
                                found = true;

                                int numberOfIssues = (int)theReader["NumberOfIssues"];

                                thisRowData.totalProductivityIssues = numberOfIssues;
                            }
                        }

                        if (!found)
                        {
                            RDDMSummary thisRow = new RDDMSummary();

                            thisRow.rdName = theReader["RDOutlookName"].ToString();
                            thisRow.dmName = theReader["DMOutlookName"].ToString();
                            thisRow.totalProductivityIssues = (int)theReader["NumberOfIssues"];

                            rddmData.Add(thisRow);

                            found = false;
                        }
                    }
                }

                theReader.Close();

                closeDataConnection();

                try
                {
                    string newFilePath = CopyReportTemplate("field");

                    openDataConnection();
                    SqlCommand cmdReport = new SqlCommand("ReportStoresNotReadyWithInterval", theConnection);
                    cmdReport.Parameters.AddWithValue("@dateStarted", startDate);
                    cmdReport.Parameters.AddWithValue("@dateEnded", endDate);
                    cmdReport.CommandType = System.Data.CommandType.StoredProcedure;
                    cmdReport.CommandTimeout = 1200;

                    theReader = cmdReport.ExecuteReader();

                    FileStream template = new FileStream(newFilePath, FileMode.Open, FileAccess.Read);
                    ExcelPackage pck = new OfficeOpenXml.ExcelPackage(template);
                    ExcelWorkbook workBook = pck.Workbook;

                    template.Close();

                    if (theReader.HasRows)
                    {
                        int rowIndex = 1;

                        ExcelWorksheet notReadySheet = workBook.Worksheets["DetailedViewNotReady"];

                        while (theReader.Read())
                        {
                            rowIndex++;

                            notReadySheet.Cells[rowIndex, 1].Value = theReader[0].ToString();
                            notReadySheet.Cells[rowIndex, 2].Value = theReader[1].ToString();
                            notReadySheet.Cells[rowIndex, 3].Value = theReader[2].ToString();
                            notReadySheet.Cells[rowIndex, 4].Value = theReader[3].ToString();
                            notReadySheet.Cells[rowIndex, 5].Value = theReader[4].ToString();
                            notReadySheet.Cells[rowIndex, 6].Value = theReader[5].ToString();
                            notReadySheet.Cells[rowIndex, 7].Value = theReader[6].ToString();
                            notReadySheet.Cells[rowIndex, 8].Value = theReader[7].ToString();
                            notReadySheet.Cells[rowIndex, 9].Value = theReader[8].ToString();
                            notReadySheet.Cells[rowIndex, 10].Value = theReader[9].ToString();
                            notReadySheet.Cells[rowIndex, 11].Value = theReader[10].ToString();
                            notReadySheet.Cells[rowIndex, 12].Value = theReader[11].ToString();
                            notReadySheet.Cells[rowIndex, 13].Value = Math.Round(Convert.ToDecimal(theReader[14].ToString()), 2).ToString();
                            notReadySheet.Cells[rowIndex, 14].Value = getCellFriendlyText(theReader[12].ToString());
                            if (String.IsNullOrEmpty(theReader[13].ToString()))
                            {
                                notReadySheet.Cells[rowIndex, 15].Value = "No Photos Available";
                            }
                            else
                            {
                                string[] photoIds = theReader[13].ToString().Split(',');
                                for (int j = 0; j < Int32.Parse(theReader[10].ToString()); j++)
                                    notReadySheet.Cells[rowIndex, j + 15].Value = baseWebURL + "/photos/" + photoIds[j] + ".jpg";
                            }
                            notReadySheet.Cells.AutoFitColumns();

                        }
                        theResponse.statusCode = 0;
                        theResponse.statusDescription = extractFilename(newFilePath);
                    }
                    else
                    {
                        theResponse.statusCode = 1;
                        theResponse.statusDescription = "There is no data logged between the dates that were selected";
                    }
                    theReader.Close();



                    SqlCommand cmdReportNotReady = new SqlCommand("ReportStoresReadyWithInterval", theConnection);
                    cmdReportNotReady.Parameters.AddWithValue("@dateStarted", startDate);
                    cmdReportNotReady.Parameters.AddWithValue("@dateEnded", endDate);
                    cmdReportNotReady.CommandType = System.Data.CommandType.StoredProcedure;
                    cmdReportNotReady.CommandTimeout = 1200;

                    theReader = cmdReportNotReady.ExecuteReader();
                    if (theReader.HasRows)
                    {
                        int rowIndex = 1;
                        ExcelWorksheet readySheet = workBook.Worksheets["DetailedViewReady"];

                        while (theReader.Read())
                        {
                            rowIndex++;

                            readySheet.Cells[rowIndex, 1].Value = theReader[0].ToString();
                            readySheet.Cells[rowIndex, 2].Value = theReader[1].ToString();
                            readySheet.Cells[rowIndex, 3].Value = theReader[2].ToString();
                            readySheet.Cells[rowIndex, 4].Value = theReader[3].ToString();
                            readySheet.Cells[rowIndex, 5].Value = theReader[4].ToString();

                        }
                        theResponse.statusCode = 0;
                        theResponse.statusDescription = extractFilename(newFilePath);
                    }
                    else
                    {
                        theResponse.statusCode = 1;
                        theResponse.statusDescription = "There is no data logged between the dates that were selected";
                    }
                    theReader.Close();

                    ExcelWorksheet sheetSummary = workBook.Worksheets["DVPRVPSummary"];

                    sheetSummary.Cells["A2:C2"].Merge = true;
                    sheetSummary.Cells[2, 1].Value = "All Data";

                    if (dvprvpData != null && dvprvpData.Count > 0)
                    {
                        int currentRowIndex = 3;


                        for (int i = 0, l = dvprvpData.Count; i < l; i++)
                        {
                            DVPRVPSummary thisRowsData = dvprvpData[i];

                            thisRowsData.percentageStoresReady = Math.Round((100 - ((double)thisRowsData.deliveriesWithIssues / (double)thisRowsData.deliveries) * 100.0), 2);
                            thisRowsData.leftoutCOGS = Math.Round(thisRowsData.leftoutCOGS, 2);
                            thisRowsData.dairyBackhaulCOGS = Math.Round(thisRowsData.dairyBackhaulCOGS, 2);


                            sheetSummary.Cells[currentRowIndex, 1].Value = thisRowsData.dvpName;
                            sheetSummary.Cells[currentRowIndex, 2].Value = thisRowsData.rvpName;
                            sheetSummary.Cells[currentRowIndex, 3].Value = thisRowsData.percentageStoresReady.ToString() + " %";
                            sheetSummary.Cells[currentRowIndex, 4].Value = "$ " + thisRowsData.dairyBackhaulCOGS.ToString();
                            sheetSummary.Cells[currentRowIndex, 5].Value = thisRowsData.deliveries;
                            sheetSummary.Cells[currentRowIndex, 6].Value = thisRowsData.deliveriesWithIssues;
                            sheetSummary.Cells[currentRowIndex, 7].Value = thisRowsData.totalReadinessIssues;

                            currentRowIndex++;
                        }

                        theResponse.statusCode = 0;
                        theResponse.statusDescription = extractFilename(newFilePath);
                    }
                    else
                    {
                        theResponse.statusCode = 1;
                        theResponse.statusDescription = "There is no data logged between the dates that were selected";
                    }


                    ExcelWorksheet sheetRDDMSummary = workBook.Worksheets["RDDMSummary"];

                    sheetRDDMSummary.Cells["A2:C2"].Merge = true;
                    sheetRDDMSummary.Cells[2, 1].Value = "All Data";

                    if (rddmData != null && rddmData.Count > 0)
                    {
                        int currentRowIndex = 3;

                        for (int i = 0, l = rddmData.Count; i < l; i++)
                        {
                            RDDMSummary thisRowsData = rddmData[i];

                            thisRowsData.percentageStoresReady = Math.Round((100 - ((double)thisRowsData.deliveriesWithIssues / (double)thisRowsData.deliveries) * 100.0), 2);

                            thisRowsData.dairyBackhaulCOGS = Math.Round(thisRowsData.dairyBackhaulCOGS, 2);

                            sheetRDDMSummary.Cells[currentRowIndex, 1].Value = thisRowsData.rdName;
                            sheetRDDMSummary.Cells[currentRowIndex, 2].Value = thisRowsData.dmName;
                            sheetRDDMSummary.Cells[currentRowIndex, 3].Value = thisRowsData.percentageStoresReady.ToString() + " %";
                            sheetRDDMSummary.Cells[currentRowIndex, 4].Value = "$ " + thisRowsData.dairyBackhaulCOGS.ToString();
                            sheetRDDMSummary.Cells[currentRowIndex, 5].Value = thisRowsData.deliveries;
                            sheetRDDMSummary.Cells[currentRowIndex, 6].Value = thisRowsData.deliveriesWithIssues;
                            sheetRDDMSummary.Cells[currentRowIndex, 7].Value = thisRowsData.totalReadinessIssues;
                            sheetRDDMSummary.Cells[currentRowIndex, 8].Value = thisRowsData.totalSecurityFacilityIssues;
                            sheetRDDMSummary.Cells[currentRowIndex, 9].Value = thisRowsData.totalCapacityIssues;
                            sheetRDDMSummary.Cells[currentRowIndex, 10].Value = thisRowsData.totalProductivityIssues;

                            currentRowIndex++;
                        }

                        theResponse.statusCode = 0;
                        theResponse.statusDescription = extractFilename(newFilePath);
                    }
                    else
                    {
                        theResponse.statusCode = 1;
                        theResponse.statusDescription = "There is no data logged between the dates that were selected";
                    }

                    FileStream fileSave = new FileStream(newFilePath, FileMode.Create);

                    pck.SaveAs(fileSave);
                    fileSave.Close();

                    closeDataConnection();
                }
                catch (Exception _exception)
                {
                    theResponse.statusCode = 6;
                    theResponse.statusDescription = _exception.Message + " " + _exception.StackTrace;
                }

                return theResponse;
            }
            else
            {
                //return ReportFieldReadiness();
                return null;
            }
        }

        public void WriteToFile(string text)
        {
            System.IO.StreamWriter file = new System.IO.StreamWriter("C:\\Temp\\Log.txt", true);
            file.WriteLine(text);

            file.Close();
        }

    }
}