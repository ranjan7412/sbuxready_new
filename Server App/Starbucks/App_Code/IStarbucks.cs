using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace Starbucks
{
    [ServiceContract]
    public interface IStarbucks
    {
        /* Login and Session Management */

        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "/Security/Login/{aUsername}/{aPassword}", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        LoginResponse LoginForAdminPanel(string aUsername, string aPassword);

        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "/Security/Login/{aUsername}/{aPassword}/{aRouteNumber}", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        LoginResponse LoginForDevice(string aUsername, string aPassword, string aRouteNumber);

        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "/Security/Logout/{aUsername}/{aSessionID}", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Response Logout(string aUsername, string aSessionID);

        /* User Management */

        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "/Security/User/Detail/{aUsername}", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        ResponseUserList GetUserDetail(string aUsername);

        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "/Security/User/All", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        ResponseUserList GetAllUsers();

        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "/Security/User/All/{userType}", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        ResponseUserList GetAllUsersByType(string userType);

        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "/Security/User/All/{userType}/{providerId}", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        ResponseUserList GetAllUsersByTypeAndProvider(string userType, string providerId);

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/Security/User/Create", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Response CreateUser(StarbucksUser aUserModel);

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/Security/User/Update", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Response UpdateUser(StarbucksUser aUserModel);

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/Security/User/Update/Password", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Response UpdateUserPassword(StarbucksUser aUserModel);

        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "/Security/User/Activate/{aUsername}", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Response ActivateUser(string aUsername);

        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "/Security/User/Deactivate/{aUsername}", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Response DeactivateUser(string aUsername);

        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "/Security/User/Delete/{aUsername}", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Response DeleteUser(string aUsername);

        /* Store Management */

        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "/Store/CDC/{aCDCID}", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        ResponseStoreList GetStoresForCDC(string aCDCID);

        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "/Store/Detail/{aStoreID}", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        ResponseStoreList GetStoreDetail(string aStoreID);

        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "/Store/All", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        ResponseStoreList GetAllStores();

        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "/Store/All?start={startingIndex}&limit={endingIndex}", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        ResponseStoreList GetAllStoresWithRange(string startingIndex, string endingIndex);
        
        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/Store/Create", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Response CreateStore(Store aStoreModel);

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/Store/Update", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Response UpdateStore(Store aStoreModel);

        /* Route Management */

        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "/Route/All", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        ResponseRouteList GetAllRouteMappings();

        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "/Route/All?start={startingIndex}&limit={endingIndex}", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        ResponseRouteList GetAllRouteMappingsWithRange(string startingIndex, string endingIndex);

        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "/Route/All/{providerID}", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        ResponseRouteList GetAllRouteMappingsForProvider(string providerID);

        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "/Route/All/{providerID}?start={startingIndex}&limit={endingIndex}", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        ResponseRouteList GetAllRouteMappingsForProviderWithRange(string providerID, string startingIndex, string endingIndex);

        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "/Route/Detail/{routeName}", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        ResponseRouteList GetRouteDetail(string routeName);

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/Route/Create", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Response CreateRoute(Route routeModel);

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/Route/AddStore/{routeName}/{storeID}", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Response AddStoreToRoute(string routeName, string storeID);

        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "/Route/Activate/{routeId}", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Response UpdateRouteStatusToActive(string routeId);

        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "/Route/Deactivate/{routeId}", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Response UpdateRouteStatusToDeactive(string routeId);

        /* Reason Code Management */

        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "/Reason/All", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        ResponseReasonList GetAllParentReasons();

        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "/Reason/AllWithChildren", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        ResponseReasonWithChildrenList GetAllParentReasonsWithChildren();

        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "/Reason/Children/{reasonCode}", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        ResponseReasonWithChildrenList GetChildrenOfParentReason(string reasonCode);

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/Reason/Create", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Response CreateReason(Reason aReasonModel);

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/Reason/Update", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Response UpdateReason(Reason aReasonModel);

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/Reason/Child/Create", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Response CreateReasonChild(ReasonChildWithParent aChildReasonModel);

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/Reason/Child/Update", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Response UpdateReasonChild(ReasonChildWithParent aChildReasonModel);

        /* CDC Management */

        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "/CDC/All", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        ResponseCDCList GetAllCDCs();

        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "/CDC/All/{providerID}", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        ResponseCDCList GetAllCDCsForProvider(string providerID);

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/CDC/Create", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Response CreateCDC(CDC aCDCModel);

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/CDC/Update", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Response UpdateCDC(CDC aCDCModel);

        /* Trip Management */

        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "/Trip/AllOpen", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        ResponseTripList GetAllOpenTrips();

        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "/Trip/AllOpenForProvider/{aProviderID}", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        ResponseTripList GetAllOpenTripsForProvider(string aProviderID);

        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "/Trip/AllOpenForCDC/{aCDCID}", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        ResponseTripList GetAllOpenTripsForCDC(string aCDCID);

        //[OperationContract]
        //[WebInvoke(Method = "GET", UriTemplate = "/Trip/Setup/{aRouteName}/{aUsername}", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        //Response SetupTrip(string aRouteName, string aUsername);

        //[OperationContract]
        //[WebInvoke(Method = "GET", UriTemplate = "/Trip/Setup/{aRouteName}/{aUsername}/{aGMTOffset}", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        //Response SetupTripV7(string aRouteName, string aUsername, string aGmtOffset);

        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "/Trip/Close/{aTripID}", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Response CloseTrip(string aTripID);

        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "/Trip/GetOpenTrip/{aRouteName}/{aUsername}", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        ResponseTrip GetOpenTripForRouteNameAndUser(string aRouteName, string aUsername);

        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "/Trip/GetOpenTripV7/{aRouteName}/{aUsername}/{aGMTOffset}", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        ResponseTrip GetOpenTripForRouteNameAndUserV7(string aRouteName, string aUsername, string aGMTOffset);

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/Trip/Comment/Add", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Response AddCommentToStop(Comment aComment);

        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "/Trip/Photo/All/{aStopID}", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        ResponsePhoto GetPhotosForStop(string aStopID);

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/Trip/Photo/Add", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Response AddPhotoToStop(Photo aPhoto);

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/Trip/Delivery/Add", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Response AddDeliveryTransaction(Delivery aDelivery);

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/Trip/Failure/Add", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        ResponseFailure AddFailure(Failure aFailure);

        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "/Trip/Stop/Complete/{aStopID}", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Response CompleteStop(string aStopID);

        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "/Trip/Stop/CompleteV7/{aStopID}/{aStopCompletedDate}", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Response CompleteStopV7(string aStopID, string aStopCompletedDate);

        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "/Trip/Stop/DeleteAllIssues/{aStopID}", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Response DeleteAllIssues(string aStopID);

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/Trip/Geo/Set", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Response SetGeoPosition(GeoPosition aPosition);

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/Trip/Issues/Commit", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Response CommitIssues(StopWithStoreAndFailure aStop);

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/Trip/Issues/CommitV7", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Response CommitIssuesV7(StopWithStoreAndFailure aStop);

        //[OperationContract]
        //[WebInvoke(Method = "POST", UriTemplate = "/Trip/Offline/Commit", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        //Response CommitCachedData(TripForSync aTripModel);

        //[OperationContract]
        //[WebInvoke(Method = "POST", UriTemplate = "/Trip/Offline/CommitV5", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        //Response CommitCachedDataV5(TripForSync aTripModel);

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/Trip/Offline/CommitV6", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Response CommitCachedDataV6(TripForSync aTripModel);

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/Trip/Offline/CommitV7", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Response CommitCachedDataV7(TripForSync aTripModel);

        /* Ops Hierarchy Management */

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/Op/Add", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Response AddOp(Op anOp);

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/Op/Update", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Response UpdateOp(Op anOp);

        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "/Op/All", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        ResponseOpList GetAllOps();

        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "/Op/All?start={startingIndex}&limit={endingIndex}", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        ResponseOpList GetAllOpsWithRange(string startingIndex, string endingIndex);

        /* Batch Upload */

        //[OperationContract]
        //[WebInvoke(Method = "POST", UriTemplate = "/Upload/Stores/{username}", BodyStyle = WebMessageBodyStyle.WrappedRequest, ResponseFormat = WebMessageFormat.Json)]
        //MyResponse UploadStores(Stream fileStream, string username);

        //[OperationContract]
        //[WebInvoke(Method = "POST", UriTemplate = "/Upload/Routes/{username}", BodyStyle = WebMessageBodyStyle.WrappedRequest, ResponseFormat = WebMessageFormat.Json)]
        //Response UploadRoutes(Stream fileStream, string username);

        //[OperationContract]
        //[WebInvoke(Method = "POST", UriTemplate = "/Upload/Ops/{username}", BodyStyle = WebMessageBodyStyle.WrappedRequest, ResponseFormat = WebMessageFormat.Json)]
        //Response UploadOps(Stream fileStream, string username);

        /* Provider Management */

        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "/Provider/All", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        ResponseProviderList GetAllProviders();

        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "/Provider/AllWithCDC", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        ResponseProviderWithCDCList GetAllProvidersWithCDCs();

        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "/Provider/{providerID}/CDC", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        ResponseProviderWithCDCList GetCDCsForProvider(string providerID);

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/Provider/Create", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Response CreateProvider(Provider aProviderModel);

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/Provider/Update", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Response UpdateProvider(Provider aProviderModel);

        /* Photo Gallery */

        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "/Photos/{aStoreID}", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        ResponsePhotoList GetPhotosForStore(string aStoreID);

        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "/Photos/View/{photoIDs}", BodyStyle = WebMessageBodyStyle.Bare)]
        Stream ViewPhotos(string photoIDs);

        /* Reports */

        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "/Reports/StoreReadiness/SSC", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Response ReportStoreReadinessForSSC();

        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "/Reports/StoreReadiness/SSC/{startDate}/{endDate}", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Response ReportStoreReadinessForSSCWithInterval(string startDate, string endDate);

        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "/Reports/StoreReadiness/SSC/{startDate}/{endDate}/{startHour}/{endHour}", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Response ReportStoreReadinessForSSCWithIntervalHours(string startDate, string endDate, string startHour, string endHour);
        
        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "/Reports/StoreReadiness/CDC", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Response ReportStoreReadinessForCDC();

        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "/Reports/StoreReadiness/CDC/{startDate}/{endDate}", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Response ReportStoreReadinessForCDCWithInterval(string startDate, string endDate);

        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "/Reports/StoreReadiness/CDC/{startDate}/{endDate}/{startHour}/{endHour}", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Response ReportStoreReadinessForCDCWithIntervalHours(string startDate, string endDate, string startHour, string endHour);

        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "/Reports/StoreReadiness/CDC/Provider/{providerID}", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Response ReportStoreReadinessForCDCForProvider(string providerID);

        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "/Reports/StoreReadiness/CDC/Provider/{providerID}/{startDate}/{endDate}", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Response ReportStoreReadinessForCDCForProviderWithInterval(string providerID, string startDate, string endDate);

        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "/Reports/StoreReadiness/CDC/Provider/{providerID}/{startDate}/{endDate}/{startHour}/{endHour}", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Response ReportStoreReadinessForCDCForProviderWithIntervalHours(string providerID, string startDate, string endDate, string startHour, string endHour);

        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "/Reports/FieldReadiness", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Response ReportFieldReadiness();

        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "/Reports/FieldReadiness/{startDate}/{endDate}", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Response ReportFieldReadinessWithInterval(string startDate, string endDate);

        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "/Reports/FieldReadiness/{startDate}/{endDate}/{startHour}/{endHour}", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Response ReportFieldReadinessWithIntervalHours(string startDate, string endDate, string startHour, string endHour);

        /* Email */

        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "/Email/Consolidate/{stopID}", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Response ConsolidateEmails(string stopID);

        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "/Email/Test", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Response SendTestEmail();

        /* New UI Methods  */

        [OperationContract]
        //[WebInvoke(Method = "GET", UriTemplate = "/Photo?condition={condition}&startIndex={startIndex}&maxRows={maxRows}", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        DataTable GetPhotos(string condition, int startIndex, int maxRows);

        [OperationContract]
        //[WebInvoke(Method = "GET", UriTemplate = "/testing/Testing", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        String ExportPhotoSearch(DataTable dtPhotoSearch);

        [OperationContract]
        ResponseRouteList DotNetGetAllRouteMappings(int startIndex, int maxRows, string providerId);

        [OperationContract]
        ResponseRouteList DotNetGetAllFilteredRouteMappings(string filterText, int startIndex, int maxRows, string providerId);

        [OperationContract]
        String ExportRoutes(DataTable dtRoutes);

        [OperationContract]
        Response UploadRoutesDotNet(string fileName, string username);


    }
}
