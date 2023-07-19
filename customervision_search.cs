public string MakeSearchSQL(string ValidViewLevels, long UserID, long CategoryID, string KeyPhrase,
                                string LogicOperator, string OrderBy,
                                bool Priority, string Criteria,
                                bool bUseFullTextSearch)
    {
        //bool bIncludeSubCategoriesInSearch = false;
        //long lKBID = 0;
        string[] Keywords;
        string sSQLPhrase = "";
        string sSQLKeyPhrase = "";
        //Dim arrKeywords
        string sStatusPhrase = "";
        //Dim oCategory As New Category
        bool isPrivate = false;
        //Dim sCriteria As String

        if (ValidViewLevels == "")
            ValidViewLevels = new Levels(DSN).GetLowestLevel().ToString();
        KeyPhrase = KeyPhrase.Trim().Replace("'", "''");
        OrderBy = OrderBy.Trim();
        LogicOperator = LogicOperator.Trim();
        //bIncludeSubCategoriesInSearch = YesNoValue(oConfig.GetValue("IncludeSubcategoriesInSearch"))
        Criteria = Criteria.Trim().ToLower();

        if (KeyPhrase != "")
        {
            if (LogicOperator.ToUpper() == "PHRASE")
            {
                switch (Criteria)
                {
                    case "title":
                        sSQLKeyPhrase += "(title LIKE '%" + KeyPhrase + "%')";
                        break;
                    case "content":
                        sSQLKeyPhrase += "(content LIKE '%" + KeyPhrase + "%')";
                        break;
                    case "summary":
                        sSQLKeyPhrase += "(summary LIKE '%" + KeyPhrase + "%')";
                        break;
                    case "keywords":
                        sSQLKeyPhrase += "(keywords LIKE '%" + KeyPhrase + "%')";
                        break;
                    default:
                        //sSQLKeyPhrase = sSQLKeyPhrase & "(title LIKE '%" & sKeyPhrase & "%' OR contentsearch LIKE '%" & sKeyPhrase & "%' OR keywords LIKE '%" & sKeyPhrase & "%')"    ' include summary?
                        //sSQLKeyPhrase = sSQLKeyPhrase & "(contentsearch LIKE '%" & sKeyPhrase & "%')"    ' include summary?
                        if (bUseFullTextSearch)
                            sSQLKeyPhrase += " CONTAINS(ContentSearch,' \"" + KeyPhrase + "\" ')";
                        else
                            sSQLKeyPhrase += "(contentsearch LIKE '%" + KeyPhrase + "%')";    // include summary?
                        break;
                }
            }
            else
            {
                Keywords = KeyPhrase.Split(' ');

                if (Keywords.GetUpperBound(0) > -1) sSQLKeyPhrase += "(";

                for (int i = Keywords.GetLowerBound(0); i <= Keywords.GetUpperBound(0); i++)
                {
                    switch (Criteria)
                    {
                        case "title":
                            sSQLKeyPhrase += "(title LIKE '%" + Keywords[i] + "%')";
                            break;
                        case "content":
                            sSQLKeyPhrase += "(content LIKE '%" + Keywords[i] + "%')";
                            break;
                        case "summary":
                            sSQLKeyPhrase += "(summary LIKE '%" + Keywords[i] + "%')";
                            break;
                        case "keywords":
                            sSQLKeyPhrase += "(keywords LIKE '%" + Keywords[i] + "%')";
                            break;
                        default:
                            //sSQLKeyPhrase = sSQLKeyPhrase & "(title LIKE '%" & arrKeywords(i) & "%' OR contentsearch LIKE '%" & arrKeywords(i) & "%' OR keywords LIKE '%" & arrKeywords(i) & "%')"
                            //sSQLKeyPhrase = sSQLKeyPhrase & "(contentsearch LIKE '%" & arrKeywords(i) & "%')"
                            if (bUseFullTextSearch)
                                sSQLKeyPhrase += " CONTAINS(ContentSearch,'" + Keywords[i] + "')";
                            else
                                sSQLKeyPhrase += "(contentsearch LIKE '%" + Keywords[i] + "%')";
                            break;
                    }
                    if (LogicOperator.ToUpper() == "AND" && i != Keywords.GetUpperBound(0))
                        sSQLKeyPhrase += " AND ";
                    else if (LogicOperator.ToUpper() == "OR" && i != Keywords.GetUpperBound(0))
                        sSQLKeyPhrase += " OR ";
                }

                if (Keywords.GetUpperBound(0) > -1) sSQLKeyPhrase += ")";

            }
        }

        if (UserID == 0)
            sStatusPhrase = "(KnowledgeBase.statusID=" + Constants.KBSTATUS_PUBLIC + ")";
        else
            sStatusPhrase = "((KnowledgeBase.statusID=" + Constants.KBSTATUS_PUBLIC + ") OR (KnowledgeBase.createdBy=" + UserID + " AND KnowledgeBase.statusID=" + Constants.KBSTATUS_DRAFT + "))";

        sSQLKeyPhrase = sSQLKeyPhrase.Trim();
        sSQLPhrase = sSQLKeyPhrase;
        if (sSQLPhrase != "" && sStatusPhrase != "") sSQLPhrase += " AND ";
        sSQLPhrase += sStatusPhrase;
        if (sSQLKeyPhrase == "") sSQLKeyPhrase = " ";
        if (!isPrivate)  // only filter by date for public searches
        {
            if (sSQLPhrase == "")
                sSQLPhrase = "WHERE (dateAvailable Is null or dateAvailable<=getdate()) AND " +
                                "(dateExpire is null or dateExpire>=getdate())";
            else
                sSQLPhrase = "WHERE (dateAvailable Is null or dateAvailable<=getdate()) AND " +
                        "(dateExpire is null or dateExpire>=getdate()) AND " +
                        "(" + sSQLPhrase + ")";
        }
        else
            sSQLPhrase = "WHERE (" + sSQLPhrase + ")";

        // filter results by valid level ids
        if (ValidViewLevels != "all" && ValidViewLevels != "")
            sSQLPhrase += " AND KnowledgeBase.LevelID IN (" + ValidViewLevels + ")";

        // filter results by category id
        if (CategoryID != 0)
        {
            //If bIncludeSubCategoriesInSearch Then
            //  oCategory.DSN = mvarDSN
            //  sSQLPhrase = sSQLPhrase & " AND KnowledgeBase.CategoryID IN (" & oCategory.GetSubCategories(sValidViewLevels, iCategoryID) & ")"
            //Else
            sSQLPhrase += " AND KnowledgeBase.CategoryID=" + CategoryID;
            //End If
        }
        sSQLPhrase += " ORDER BY ";
        if (Priority)
            sSQLPhrase += " Priority DESC ";

        if (OrderBy != "")
        {
            if (Priority) sSQLPhrase += ", ";
            sSQLPhrase += OrderBy;
        }
        if (OrderBy.ToLower() != "categoryname" && OrderBy.ToLower() != "title" && OrderBy != "")
            sSQLPhrase += " DESC";

        if (OrderBy != "" || Priority)
            sSQLPhrase += ", ";

        sSQLPhrase += " kbid";
        if (OrderBy == "categoryname")
        {
            sSQLPhrase = "SELECT kbid, KnowledgeBase.CategoryID, Category.Category AS CategoryName, Title, Author, DateModified, Summary " +
                            "FROM KnowledgeBase LEFT JOIN Category ON KnowledgeBase.CategoryID=Category.categoryID " +
                            sSQLPhrase;
        }
        else
        {
            sSQLPhrase = "SELECT kbid, CategoryID, Title, Author, DateModified, Summary " +
                            "FROM KnowledgeBase " +
                        sSQLPhrase;
        }
        //Debug.Print sSQLPhrase
        return Util.CheckInjection(sSQLPhrase);
    }
