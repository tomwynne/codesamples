// Get Active Directory 
//
// Pass the server name, user id, password, and account name and what field name to return
//

public string GetADField(string ADServerName, string ADUserID, string ADPassword, string AccountName, string FieldName)
{
	OleDbConnection objConnection;
	OleDbCommand objCommand = new OleDbCommand();
	OleDbDataReader objRecordset;

	objConnection = new OleDbConnection("Provider=ADsDSOObject;User ID=" + ADUserID + ";Password=" + ADPassword + ";Encrypt Password=True;ADSI Flag=1");
	objCommand.Connection = objConnection;

	//objCommand.Properties("Page Size") = 1000

	objCommand.CommandText = "<LDAP://" + ADServerName + ">;(&(objectCategory=User)(SAMAccountName=" + AccountName + "));" + FieldName + ";Subtree";
	objRecordset = objCommand.ExecuteReader();
	if (objRecordset.Read())
		return objRecordset[0].ToString();
	return "";
}