public string GetPageTypeForPageName(string PageName)
{
	string sPageType = "";
	SqlConnection cn = new SqlConnection(DSN);
	try
	{
		cn.Open();
		SqlCommand cmd = new SqlCommand("SELECT Description FROM PageTypes WHERE PageName=@PageName", cn);
		cmd.CommandTimeout = Constants.GCOMMANDTIMEOUT;
		cmd.Parameters.AddWithValue("@PageName", PageName);
		SqlDataReader rs = cmd.ExecuteReader(CommandBehavior.CloseConnection);

		if (rs.Read())
		{
			sPageType = rs["Description"].ToString();
		}
		return sPageType;
	}
	catch (Exception ex)
	{
		LastError = ex.Message;
		return "";
	}
	finally
	{
		if (cn.State == ConnectionState.Open)
			cn.Close();
	}
}