// CVBATCH53
//
// Runs on a schedule to do routine maintenance on the data for every customer in the system
//


using System;
using System.Configuration;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Diagnostics;

namespace CVBATCH53
{

    class Program
    {
        static bool TESTMODE = false;
        static string LastError = "";
        static int VersionNumber = 53;

        static void Main(string[] args)
        {
            LogMessage("Starting batch process.");
            
            RunBatch();

            LogMessage("Backup complete.");
        }

        static void RunBatch()
        {
            Customer oCust = new Customer(ConfigurationManager.ConnectionStrings["DSN_CVMASTER"].ConnectionString);
            List<Customer> oCustomers = oCust.GetCustomers(VersionNumber);
            foreach (Customer oCustomer in oCustomers)
            {
                BatchCustomer(oCustomer);
            }

            LogMessage("Terminating batch process. Packing up.");

        }

        static void BatchCustomer(Customer oCustomer)
        {
            //string DSN = oCustomer.DSN.Replace("cvmaster", oCustomer.DBName);
            LogMessage("Processing Customer: " + oCustomer.Name);  // + " " + oCustomer.DBDSN);

            // Do processes
            //UpdateTest(oCustomer);
            ProcessAlertQueue(oCustomer, oCustomer.DBDSN);
            Aggregate(oCustomer, oCustomer.DBDSN);
            AlertStaleIncidents(oCustomer, oCustomer.DBDSN);
            AlertExpiredArticles(oCustomer, oCustomer.DBDSN);
            UpdateSearch(oCustomer, oCustomer.DBDSN);
            FixLevelIDs(oCustomer, oCustomer.DBDSN);
        }

        static void Aggregate(Customer oCustomer, string DSN)
        {
            DateTime dteDate = DateTime.Now;
            string sPopFreq = "";

            LogMessage("Beginning aggregation..");

            sPopFreq = new Config(DSN).GetValue("PopularityFrequency");

            switch (sPopFreq)
            {
                case "week":
                    dteDate = DateTime.Now.Subtract(new TimeSpan(-7, 0, 0, 0));        //  DateAdd("ww", -1, Now)
                    break;
                case "month":
                    dteDate = DateTime.Now.AddMonths(-1);       // DateAdd("m", -1, Now)
                    break;
                case "quarter":
                    dteDate = DateTime.Now.AddMonths(-3);       // DateAdd("q", -1, Now) ?????
                    break;
                case "year":
                    dteDate = DateTime.Now.AddYears(-1);        // DateAdd("yyyy", -1, Now)
                    break;
                case "lifetime":
                    dteDate = DateTime.Now.AddYears(-10);       // DateAdd("yyyy", -10, Now)
                    break;
                default:
                    LogMessage("Invalid frequency: " + sPopFreq);
                    break;
            }

            SqlConnection cn = new SqlConnection(DSN);
            SqlCommand cmd;
            try
            {
                cn.Open();
                cmd = new SqlCommand("sp_update_kbviews", cn);
                cmd.CommandTimeout = Constants.GCOMMANDTIMEOUT;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@frequency", dteDate);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                LogMessage("Error during Aggregation: " + ex.Message);
            }
            finally
            {
                if (cn.State == ConnectionState.Open)
                    cn.Close();
                LogMessage("Aggregation completed");
            }
        }


        static void AlertStaleIncidents(Customer oCustomer, string DSN)
        {
            string sSQL = "";
            long StaleIncidentDays = 0;
            long StaleIncidentUser = 0;
            long IncidentID = 0;
            DateTime LastActivityDate;
            string sSubject = "";
            string sTextBody = "";
            bool OkToSend = false;
            long nCount = 0;
            List<User> oUsers;
            User oUser;
            //Dim oPerson As Object
            //string sEmails = "";
            //long l = 0;
            Main oMain = new Main(DSN);
            if (oMain.oConfig.GetBool("StaleIncidentAlert") == false)
                return;
            LogMessage("Begin processing Alert for Stale Messages");
            StaleIncidentDays = oMain.oConfig.GetLong("StaleIncidentDays");
            if (StaleIncidentDays < 0) return;
            StaleIncidentUser = oMain.oConfig.GetLong("StaleIncidentExpertUser");
            SqlConnection cn = new SqlConnection(DSN);
            sSQL = "SELECT     TOP 100 PERCENT dbo.Incident.incidentID, MAX(dbo.IncidentHistory.CreateDate) AS LastActivityDate " +
                    "FROM       dbo.Incident INNER JOIN " +
                    "           dbo.IncidentHistory ON dbo.Incident.incidentID = dbo.IncidentHistory.IncidentID " +
                    "WHERE      (dbo.Incident.statusID = 1) " +
                    "GROUP BY   dbo.Incident.incidentID " +
                    "ORDER BY   dbo.Incident.incidentID";
            try
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand(sSQL, cn);
                cmd.CommandTimeout = Constants.GCOMMANDTIMEOUT;
                SqlDataReader rs = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                nCount = 0;
                while (rs.Read())
                {
                    LastActivityDate = Convert.ToDateTime(rs["LastActivityDate"].ToString());
                    if (StaleIncidentDays == 0)
                    {
                        OkToSend = true;
                    }
                    else
                    {
                        if (DateTime.Now.Date.Subtract(LastActivityDate).Days > StaleIncidentDays)
                            OkToSend = true;
                    }
                    if (OkToSend)
                    {
                        nCount++;
                        IncidentID = Util.Val64(rs["incidentID"].ToString());
                        sTextBody += "Message #" + IncidentID + " had no activity since " + LastActivityDate.ToShortDateString() + " - Link: " + oMain.GetSiteURL() + "solve.aspx?messageid=" + IncidentID + "\r\n";
                    }
                }
                if (OkToSend)
                {
                    sSubject = "There are " + nCount + " messages with no activity for at least " + StaleIncidentDays + " days.";
                    oUser = new User(DSN);
                    oUsers = oUser.GetUsers("UserID");
                    foreach (User oPerson in oUsers)
                    {
                        if (!oPerson.Suspended)
                        {
                            if (oPerson.IncidentSupervision)
                            {
                                if (StaleIncidentUser == 0 || StaleIncidentUser == oPerson.UserID)
                                {
                                    if (oPerson.EMail.Trim() != "")
                                    {
                                        if (!TESTMODE)
                                        {
                                            if (oMain.SendEMail(oMain.oConfig.GetValue("FromEMailAddress"), oPerson.EMail, "", sSubject, sTextBody))
                                            {
                                                LogMessage("AlertStaleIncidents: E-Mail sent. From: " + oMain.oConfig.GetValue("FromEMailAddress") + " To: " + oPerson.EMail);
                                            }
                                            else
                                            {
                                                LogMessage("AlertStaleIncidents: ERROR sending e-mail: " + oMain.LastError);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                LogMessage("End processing Alert for Stale Messages");
            }
            catch (Exception ex)
            {
                LogMessage("AlertStaleIncidents Error! " + ex.Message);
            }
            finally
            {
                if (cn.State == ConnectionState.Open)
                    cn.Close();
            }

        }

        static void LogMessage(string Message)
        {
            string sLogFile = Util.MergePathAndFile(Environment.CurrentDirectory, "batch.log");
            Message = DateTime.Now.ToString() + "\t" + Message.Replace("\r\n", "|") + "\r\n";
            File.AppendAllText(sLogFile, Message);
            Debug.WriteLine(Message);

        }

        // Alert for articles about to expire
        static void AlertExpiredArticles(Customer oCustomer, string DSN)
        {
            string sSQL = "";
            string sFromEmailAddress = "";
            string sSubject = "";
            string sTextBody = "";
            long nCount = 0;
            //Dim oPerson As Object
            string sEmail = "";
            //long l = 0;
            long lKBID = 0;
            long lCategoryID = 0;
            long lCreatedByID = 0;
            DateTime dateExpire = DateTime.Now;

            Main oMain = new Main(DSN);
            LogMessage("Begin processing Alert for Pages about to expire");
            sFromEmailAddress = oMain.oConfig.GetValue("FromEmailAddress");
            SqlConnection cn = new SqlConnection(DSN);
            sSQL = "SELECT     KBID, categoryID, title, createdBy, dateExpire " +
                   "FROM       KnowledgeBase " +
                   "WHERE      (statusID = 1 or statusID = 2) AND dateExpire IS NOT NULL";
            try
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand(sSQL, cn);
                cmd.CommandTimeout = Constants.GCOMMANDTIMEOUT;
                SqlDataReader rs = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                nCount = 0;
                while (rs.Read())
                {
                    lKBID = Util.Val64(rs["KBID"].ToString());
                    lCategoryID = Util.Val64(rs["categoryID"].ToString());
                    lCreatedByID = Util.Val64(rs["createdBy"].ToString());
                    dateExpire = Convert.ToDateTime(rs["dateExpire"].ToString());
                    if (dateExpire >= DateTime.Now.Date.AddDays(1) && dateExpire <= DateTime.Now.Date.AddDays(7))
                    {
                        nCount++;
                        sSubject = "Page " + lKBID + " is about to expire.";
                        sTextBody = "Page '" + rs["title"].ToString() + "' is about to expire.\r\n";
                        sTextBody += "Link: " + oMain.GetSiteURL() + "kbedit.aspx?articleid=" + lKBID + "\r\n";
                        sEmail = GetCreatorEmail(DSN, lCreatedByID, lCategoryID);
                        if (sEmail != "")
                        {
                            if (!TESTMODE)
                            {
                                if(oMain.SendEMail(sFromEmailAddress, sEmail, "", sSubject, sTextBody))
                                {
                                    LogMessage("AlertExpiredArticles: E-Mail sent. From: " + sFromEmailAddress + " To: " + sEmail);
                                }
                                else
                                {
                                    LogMessage("AlertExpiredArticles: ERROR sending e-mail: " + oMain.LastError);
                                }
                            }
                        }
                        //SendMailToExperts cnCustomer, oConfig.GetValue("FromEMailAddress"), iCreatedByID, iCategoryID, sSubject, sTextBody
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage("AlertExpiredArticles Error! " + ex.Message);
            }
            finally
            {
                if (cn.State == ConnectionState.Open)
                    cn.Close();
                LogMessage("End processing Alert for Pages about to expire");
            }
        }

        static string GetCreatorEmail(string DSN, long CreatedByID, long CategoryID)
        {
            string sEmail = "";

            sEmail = GetEMailForUser(DSN, CreatedByID);
            if (sEmail == "")
            {
                sEmail = GetEMailForCategoryExpert(DSN, CategoryID);
                if (sEmail == "")
                {
                    sEmail = GetEMailForUser(DSN, Util.Val64(new Config(DSN).GetValue("UnassignedExpertUser")));
                }
            }
            return sEmail;
        }

        // Get Email for user
        // if suspended, returns blank email
        static string GetEMailForUser(string DSN, long UserID)
        {
            User oUser = new User(DSN);
            if (oUser.Load(UserID))
            {
                if (!oUser.Suspended)
                {
                    return oUser.EMail;
                }
            }
            return "";
        }

        // Get Email for category expert for a category (first one only)
        // if suspended, returns blank email
        static string GetEMailForCategoryExpert(string DSN, long CategoryID)
        {
            string sSQL = "SELECT Users.UserID, Users.Suspended, Email FROM Users " +
                          "INNER JOIN CategoryExperts ON Users.UserID=CategoryExperts.UserID " +
                          "WHERE CategoryExperts.CategoryID = @CategoryID";

            SqlConnection cn = new SqlConnection(DSN);
            try
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand(sSQL, cn);
                cmd.CommandTimeout = Constants.GCOMMANDTIMEOUT;
                cmd.Parameters.AddWithValue("@CategoryID", CategoryID);
                SqlDataReader rs = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                if (!rs.Read())
                {
                    if (!Util.ValBool(rs["Suspended"].ToString()))
                    {
                        return rs["EMail"].ToString();
                    }
                }
                return "";
            }
            catch (Exception ex)
            {
                return "";
            }
            finally
            {
                if (cn.State == ConnectionState.Open)
                    cn.Close();
            }
        }

        static void ProcessAlertQueue(Customer oCustomer, string DSN)
        {
            //string sFromEmailAddress = "";
            //string sTextBody = "";
            //long nCount = 0;
            //Dim oPerson As Object
            //string sEmail = "";
            //long l = 0;
            //long lKBID = 0;
            //long lCategoryID = 0;
            //long lCreatedByID = 0;
            DateTime dateExpire = DateTime.Now;
            long lCurrentUserID = 0;
            string sCurrentUserEmail = "";
            string sCurrentUserFirstName = "";
            string sCurrentUserLastName = "";
            string sViewLevelIDs = "";

            Main oMain = new Main(DSN);

            LogMessage("Begin processing Alert Queue");
            string sSiteURL = oMain.GetSiteURL();
            string sSupportEmail = oMain.oConfig.GetValue("FromEmailAddress");   //"support@customervision.com"
            string sSubject = "Content updates from " + oMain.oConfig.GetValue("CompanyName") + ".";
            Category oCats = new Category(DSN);
            Category oRoot = oCats.GetCategoryTree("all", true);

            SqlConnection cn = new SqlConnection(DSN);
            string sSQL = "SELECT * FROM Users WHERE Suspended<>1 ORDER BY UserID";
            try
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand(sSQL, cn);
                cmd.CommandTimeout = Constants.GCOMMANDTIMEOUT;
                SqlDataReader rs = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                //nCount = 0;
                while (rs.Read())
                {
                    lCurrentUserID = Util.Val64(rs["UserID"].ToString());
                    sCurrentUserEmail = rs["email"].ToString();
                    sCurrentUserFirstName = rs["firstname"].ToString();
                    sCurrentUserLastName = rs["lastname"].ToString();
                    sViewLevelIDs = rs["ViewLevelIDs"].ToString();
                    if (sViewLevelIDs != "")         // trim trailing ',' if there
                    {
                        if (sViewLevelIDs.EndsWith(","))
                        {
                            sViewLevelIDs = sViewLevelIDs.TrimEnd(',');
                            //sViewLevelIDs = Left(sViewLevelIDs, Len(sViewLevelIDs) - 1)
                        }
                    }

                    //sCurrentUserGroupName = rsUser("groupname")
                    //Debug.Print "Processing user: " & iCurrentUserID, sCurrentUserEmail, sCurrentUserFirstName, sCurrentUserLastName
                    string sArticleAlerts = GetArticleAlerts(DSN, sSiteURL, lCurrentUserID, sViewLevelIDs);
                    string sCategoryAlerts = GetCategoryAlerts(DSN, sSiteURL, lCurrentUserID, sViewLevelIDs, oRoot);
                    string sDiscussionAlerts = GetDiscussionAlerts(DSN, sSiteURL, lCurrentUserID, sViewLevelIDs);
                    if (sArticleAlerts != "" || sCategoryAlerts != "" || sDiscussionAlerts != "")
                    {
                        string sBody = "At any time you may change the pages in our content bank on " +
                                       "which you want to receive alerts, by using the My Alerts section of the " + oMain.oConfig.GetValue("companyname") + " portal.\r\n" +
                                       sSiteURL + "maintainalerts.aspx\r\n";
                        if (sArticleAlerts != "")
                        {
                            sBody += "\r\n" + sArticleAlerts;
                        }
                        if (sCategoryAlerts != "")
                        {
                            sBody += "\r\n" + sCategoryAlerts;
                        }
                        if (sDiscussionAlerts != "")
                        {
                            sBody += "\r\n" + sDiscussionAlerts;
                        }

                        //Send Email
                        if (!TESTMODE)
                        {
                            if(oMain.SendEMail(sSupportEmail, sCurrentUserEmail, "", sSubject, sBody))
                            {
                                LogMessage("ProcessAlertQueue: E-Mail sent. From: " + sSupportEmail + " To: " + sCurrentUserEmail);
                            }
                            else
                            {
                                LogMessage("ProcessAlertQueue: ERROR sending e-mail: " + oMain.LastError);
                            }
                        }
                    }
                }
                if (!TESTMODE)
                    ClearAlertQueue(DSN);

            }
            catch (Exception ex)
            {
                LogMessage("ProcessAlertQueue Error! " + ex.Message);
            }
            finally
            {
                if (cn.State == ConnectionState.Open)
                    cn.Close();
                LogMessage("End processing Alert Queue");
            }
        }

        static string GetArticleAlerts(string DSN, string SiteURL, long UserID, string ViewLevelIDs)
        {
            string sConsumerKBLink = SiteURL + "article.aspx?articleid=";
            string sLevelFilter = "";

            if (ViewLevelIDs == "" || ViewLevelIDs == "all")
                sLevelFilter = "";
            else
                sLevelFilter = " AND (KnowledgeBase.levelID IN (" + ViewLevelIDs + "))";

            string sSQL = "SELECT DISTINCT KnowledgeBase.KBID AS KBID, KnowledgeBase.Title " +
                          "FROM   KBAlerts INNER JOIN " +
                          "       AlertQueue ON KBAlerts.KBID = AlertQueue.KBID INNER JOIN " +
                          "       KnowledgeBase ON KBAlerts.KBID = KnowledgeBase.KBID " +
                          "WHERE  (AlertQueue.Type=1 OR AlertQueue.Type=2) AND " +
                          "       (KBAlerts.UserID = " + UserID + ") AND (AlertQueue.UserID <> " + UserID + ") AND " +
                          "       (KnowledgeBase.statusID = 2) AND " +
                          "       (KnowledgeBase.dateAvailable IS NULL OR KnowledgeBase.dateAvailable <= GETDATE()) AND " +
                          "       (KnowledgeBase.dateExpire IS NULL OR KnowledgeBase.dateExpire >= GETDATE()) " +
                          sLevelFilter + " " +
                          "ORDER BY KnowledgeBase.Title";
            SqlConnection cn = new SqlConnection(DSN);
            try
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand(sSQL, cn);
                cmd.CommandTimeout = Constants.GCOMMANDTIMEOUT;
                SqlDataReader rs = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                string sBody = "";
                while (rs.Read())
                {
                    string sTitle = rs["Title"].ToString();
                    if (sTitle.Length > 100) sTitle = sTitle.Substring(100) + "...";
                    sBody += "Title: " + sTitle + "\r\n";
                    sBody += sConsumerKBLink + rs["KBID"].ToString() + "\r\n\r\n";
                }
                if (sBody != "")
                    sBody = "One or more items you have an alert on has been changed:\r\n\r\n" + sBody;

                return sBody;
            }
            catch (Exception ex)
            {
                LogMessage("GetArticleAlerts Error! " + ex.Message);
                return "";
            }
            finally
            {
                if (cn.State == ConnectionState.Open)
                    cn.Close();
            }

        }

        static string GetCategoryAlerts(string DSN, string SiteURL, long UserID, string ViewLevelIDs, Category oRoot)
        {

            string sConsumerCategoryLink = SiteURL + "searchredir.aspx?categoryid=";
            string sLevelFilter = "";

            if (ViewLevelIDs == "" || ViewLevelIDs == "all")
                sLevelFilter = "";
            else
                sLevelFilter = " AND (Category.levelID IN (" + ViewLevelIDs + "))";
            string sSQL = "SELECT AlertQueue.categoryID AS CategoryID, AlertQueue.Type, KBID " +
                      "FROM   CategoryAlerts " +
                      "INNER JOIN AlertQueue ON CategoryAlerts.CategoryID = AlertQueue.CategoryID " +
                      "INNER JOIN Category ON AlertQueue.CategoryID = Category.CategoryID " +
                      "WHERE  (AlertQueue.Type IN (3,4,5,6)) AND " +
                      "       (CategoryAlerts.UserID = " + UserID + ") AND " +
                      "       (AlertQueue.UserID <> " + UserID + ") " +
                              sLevelFilter + " " +
                      "ORDER BY AlertQueue.categoryID, AlertQueue.Type, KBID";
            //cnCustomer.BeginTrans
            SqlConnection cn = new SqlConnection(DSN);
            try
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand(sSQL, cn);
                cmd.CommandTimeout = Constants.GCOMMANDTIMEOUT;
                SqlDataReader rs = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                string sBody = "";
                long lPreviousCategory = 0;
                long iPreviousGroup = 0;
                while (rs.Read())
                {
                    int iAlertType = Util.Val32(rs["type"].ToString());
                    long lCategory = Util.Val64(rs["CategoryID"].ToString());
                    int iGroup = 0;
                    switch (iAlertType)
                    {
                        case 3:
                        case 5:
                        case 6:      // kb added/kb changed/kb promoted
                            iGroup = 1;
                            break;
                        case 4:      // category structure changed
                            iGroup = 2;
                            break;
                        case 7:      // discussion
                            iGroup = 3;
                            break;
                    }
                    if (lCategory != lPreviousCategory)
                    {
                        sBody += "\r\n";
                        sBody += "Items have been added or changed within a category you have an alert on:\r\n";
                        sBody += "Category: " + GetBreadCrumbs(oRoot, Util.Val64(rs["CategoryID"].ToString())) + "\r\n";
                        sBody += sConsumerCategoryLink + rs["CategoryID"].ToString() + "\r\n";
                    }
                    if (lCategory != lPreviousCategory || iGroup != iPreviousGroup)
                    {
                        switch (iGroup)
                        {
                            case 1:
                                sBody += "\r\nThe following pages have been changed:\r\n";
                                break;
                            case 2:
                                sBody += "\r\nOne or more subcategories containing items has been added:\r\n";
                                break;
                            case 3:
                                sBody += "\r\nThe following discussion items have been added or changed:\r\n";
                                break;
                        }
                    }
                    switch (iGroup)
                    {
                        case 1:
                            sBody += SiteURL + "article.aspx?articleid=" + rs["KBID"].ToString() + "\r\n";
                            break;
                        case 2:
                            sBody += "Category: " + GetBreadCrumbs(oRoot, Util.Val64(rs["CategoryID"].ToString())) + "\r\n";
                            sBody += sConsumerCategoryLink + rs["CategoryID"].ToString() + "\r\n\r\n";
                            break;
                        case 3:
                            sBody += SiteURL + "discuss.aspx?articleid=" + rs["KBID"].ToString() + "#discussion\r\n";
                            break;
                    }

                    lPreviousCategory = lCategory;
                    iPreviousGroup = iGroup;
                }
                return sBody;
            }
            catch (Exception ex)
            {
                LogMessage("GetCategoryAlerts Error! " + ex.Message);
                return "";
            }
            finally
            {
                if (cn.State == ConnectionState.Open)
                    cn.Close();
            }

        }

        static string GetDiscussionAlerts(string DSN, string SiteURL, long UserID, string ViewLevelIDs)
        {
            string sConsumerDiscussionLink = SiteURL + "discuss.aspx?articleid=";
            string sLevelFilter = "";
            if (ViewLevelIDs == "" || ViewLevelIDs == "all")
                sLevelFilter = "";
            else
                sLevelFilter = " AND (KnowledgeBase.levelID IN (" + ViewLevelIDs + "))";
            string sSQL = "SELECT DISTINCT KnowledgeBase.KBID AS KBID, KnowledgeBase.Title " +
                          "FROM   KBAlerts INNER JOIN " +
                          "       AlertQueue ON KBAlerts.KBID = AlertQueue.KBID INNER JOIN " +
                          "       KnowledgeBase ON KBAlerts.KBID = KnowledgeBase.KBID " +
                          "WHERE  (AlertQueue.Type=7) AND " +
                          "       (KBAlerts.UserID = " + UserID + ") AND (AlertQueue.UserID <> " + UserID + ") AND (KnowledgeBase.statusID = 2) AND " +
                          "       (KnowledgeBase.dateAvailable IS NULL OR KnowledgeBase.dateAvailable <= GETDATE()) AND " +
                          "       (KnowledgeBase.dateExpire IS NULL OR KnowledgeBase.dateExpire >= GETDATE()) " +
                                  sLevelFilter + " " +
                          "ORDER BY KnowledgeBase.Title";
            SqlConnection cn = new SqlConnection(DSN);
            try
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand(sSQL, cn);
                cmd.CommandTimeout = Constants.GCOMMANDTIMEOUT;
                SqlDataReader rs = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                string sBody = "";
                while (rs.Read())
                {
                    string sTitle = rs["Title"].ToString();
                    if (sTitle.Length > 100) sTitle = sTitle.Substring(100) + "...";
                    sBody += "Title: " + sTitle + "\r\n";
                    sBody += sConsumerDiscussionLink + rs["KBID"].ToString() + "\r\n";
                }
                if (sBody != "")
                    sBody = "One or more discussion items have been added to a page you have an alert on:\r\n\r\n" + sBody;

                return sBody;
            }
            catch (Exception ex)
            {
                LogMessage("GetDiscussionAlerts Error! " + ex.Message);
                return "";
            }
            finally
            {
                if (cn.State == ConnectionState.Open)
                    cn.Close();
            }

        }

        static string GetBreadCrumbs(Category oRoot, long lCatID)
        {
            string sBreadCrumbs = "";
            Category oCategory = oRoot.GetNodeByID(lCatID);

            while (oCategory.Parent != null)
            {
                sBreadCrumbs = " > " + oCategory.CategoryName + sBreadCrumbs;
                oCategory = oCategory.Parent;
            }

            sBreadCrumbs = sBreadCrumbs.Substring(2);

            return sBreadCrumbs;
        }

        static void SendMailToExperts(Customer oCustomer, string FromEMailAddress, long CategoryID, string Subject, string TextBody)
        {
            string DSN = oCustomer.DSN.Replace("cvmaster", oCustomer.DBName);
            Main oMain = new Main(DSN);
            string DBName = oCustomer.DBName;
            string sSQL = "SELECT Users.UserID, Email FROM Users " +
                          "INNER JOIN CategoryExperts ON Users.UserID=CategoryExperts.UserID " +
                          "WHERE CategoryExperts.CategoryID = @CategoryID";
            SqlConnection cn = new SqlConnection(DSN);
            try
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand(sSQL, cn);
                cmd.CommandTimeout = Constants.GCOMMANDTIMEOUT;
                cmd.Parameters.AddWithValue("@CategoryID", CategoryID);
                SqlDataReader rs = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                while (rs.Read())
                {
                    string sEmail = rs["Email"].ToString();
                    if (sEmail != "")
                    {
                        if (!TESTMODE)
                        {
                            if(oMain.SendEMail(FromEMailAddress, sEmail, "", Subject, TextBody))
                            {
                                LogMessage("SendMailToExperts: E-Mail sent. From: " + FromEMailAddress + " To: " + sEmail);
                            }
                            else
                            {
                                LogMessage("SendMailToExperts: ERROR sending e-mail: " + oMain.LastError);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage("SendMailToExperts Error! " + ex.Message);
            }
            finally
            {
                if (cn.State == ConnectionState.Open)
                    cn.Close();
            }

        }

        static bool ArticleIsPrivate(string DSN, long KBID)
        {
            KB oKB = new KB(DSN);
            if (oKB.Load(KBID))
            {
                return (oKB.StatusID == Constants.KBSTATUS_DRAFT);
            }
            return false;
        }

        static void UpdateSearch(Customer oCustomer, string DSN)
        {
            LogMessage("Updating Search for [" + oCustomer.DBName + "]...");
            string sSQL = "SELECT KBID,Title,Summary,Content,Keywords,ContentSearch FROM KnowledgeBase WITH (NOLOCK) WHERE statusID=2 OR statusID=6 ORDER BY KBID";
            SqlConnection cn = new SqlConnection(DSN);
            SqlCommand cmd;
            try
            {
                cn.Open();
                cmd = new SqlCommand(sSQL, cn);
                cmd.CommandTimeout = Constants.GCOMMANDTIMEOUT;
                SqlDataReader rs = cmd.ExecuteReader(CommandBehavior.Default);
                while (rs.Read())
                {
                    long lKBID = Util.Val64(rs["KBID"].ToString());
                    string sTitle = rs["Title"].ToString();
                    string sSummary = rs["Summary"].ToString();
                    string sContent = rs["Content"].ToString();
                    string sKeywords = rs["Keywords"].ToString();
                    string sContentSearch = sTitle + "\r\n" + Util.StripHTML(sSummary) + "\r\n" + Util.StripHTML(sContent) + "\r\n" + sKeywords;
                    if (rs["ContentSearch"].ToString() != sContentSearch)
                    {
                        //Debug.Print "Updating search: KBID=" & rs("KBID")
                        if (!UpdateContentSearch(DSN, lKBID, sContentSearch))
                        {
                            LogMessage("UpdateContentSearch Error! " + LastError);
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage("UpdateSearch Error! " + ex.Message);
            }
            finally
            {
                if (cn.State == ConnectionState.Open)
                    cn.Close();
                LogMessage("Search update complete.");
            }
        }

        static bool UpdateContentSearch(string DSN, long KBID, string ContentSearch)
        {
            SqlConnection cn = new SqlConnection(DSN);
            SqlCommand cmd;
            try
            {
                cn.Open();
                cmd = new SqlCommand("UPDATE KnowledgeBase SET ContentSearch=@ContentSearch WHERE KBID=@KBID", cn);
                cmd.CommandTimeout = Constants.GCOMMANDTIMEOUT;
                cmd.Parameters.AddWithValue("@ContentSearch", ContentSearch);
                cmd.Parameters.AddWithValue("@KBID", KBID);
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return false;
            }
            finally
            {
                if (cn.State == ConnectionState.Open)
                    cn.Close();
            }

        }

        static void ClearAlertQueue(string DSN)
        {
            SqlConnection cn = new SqlConnection(DSN);
            try
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("sp_delete_allalertqueue", cn);
                cmd.CommandTimeout = Constants.GCOMMANDTIMEOUT;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                LogMessage("Error executing sp_delete_allalertqueue. " + ex.Message);
            }
            finally
            {
                if (cn.State == ConnectionState.Open)
                    cn.Close();
                LogMessage("Alert Queue Cleared.");
            }
        }
                
        static bool ArticleModifiedByUser(string DSN, long KBID, long AlertUserID)
        {
            KB oKB = new KB(DSN);
            if (oKB.Load(KBID))
            {
                return (oKB.ModifiedByID == AlertUserID);
            }
            return false;
        }

        static void FixLevelIDs(Customer oCustomer, string DSN)
        {
            LogMessage("Begin Fixing Level IDs");
            string sSQL = "SELECT * FROM Users WITH (NOLOCK) ORDER BY UserID";
            SqlConnection cn = new SqlConnection(DSN);
            try
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand(sSQL, cn);
                cmd.CommandTimeout = Constants.GCOMMANDTIMEOUT;
                SqlDataReader rs = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                while (rs.Read())
                {
                    long lUSERID = Util.Val64(rs["UserID"].ToString());
                    string sViewLevelIDs = rs["ViewLevelIDs"].ToString();
                    if (sViewLevelIDs != "")   // trim trailing ',' if there
                    {
                        if (sViewLevelIDs.EndsWith(","))
                        {
                            //Debug.Print "Fixing level id for user: " & rsUser("UserID")
                            sViewLevelIDs = sViewLevelIDs.TrimEnd(',');
                            User oUser = new User(DSN);
                            if (oUser.Load(lUSERID))
                            {
                                oUser.ViewLevelIDs = sViewLevelIDs;
                                oUser.Save();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage("Error fixing Level IDs! " + ex.Message);
            }
            finally
            {
                if (cn.State == ConnectionState.Open)
                    cn.Close();
                LogMessage("End Fixing Level IDs.");
            }
        }


        //static void UpdateTest(Customer oCustomer)
        //{
        //    string DSN = oCustomer.DSN.Replace("cvmaster", oCustomer.DBName);
        //    KB oKB = new KB(DSN);
        //    if (oKB.Load(5947))
        //    {
        //        oKB.Content = "test";
        //        oKB.Save(true);
        //    }

        //}
    }

}
