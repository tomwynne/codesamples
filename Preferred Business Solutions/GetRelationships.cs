public List<Relationship> GetRelationships(int customerID) {

	var relationships = new List<Relationship>();
	var rsGuarantor = db.OpenRecordset($"SELECT * FROM Guarantors WHERE [Customer ID]={customerID} ORDER BY [Document Reference]", DAO.RecordsetTypeEnum.dbOpenSnapshot);
	while(!rsGuarantor.EOF) {
		var relationship = new Relationship();
		relationship.relationshipType = "Principal";
		relationship.person.firstName = modUtil.GetField(rsGuarantor, "FirstName");
		relationship.person.lastName = modUtil.GetField(rsGuarantor, "LastName");
		relationship.person.ssn = modUtil.GetField(rsGuarantor, "SSN").Replace("-", "");
		relationship.person.title = modUtil.GetField(rsGuarantor, "Title");
		relationship.person.percentageOwnership = modUtil.GetFieldInt(rsGuarantor, "Ownership");
		relationship.person.mailingStreet = modUtil.GetField(rsGuarantor, "Street Number") + (" " & modUtil.GetField(rsGuarantor, "Street Direction")).TrimEnd() + (" " & modUtil.GetField(rsGuarantor, "Street Name")).TrimEnd() + (" " + modUtil.GetField(rsGuarantor, "Street Type")).TrimEnd() + (" " & modUtil.GetField(rsGuarantor, "Post Direction")).TrimEnd() + (" " & modUtil.GetField(rsGuarantor, "Apartment Number")).TrimEnd();
		relationship.person.mailingCity = modUtil.GetField(rsGuarantor, "City");
		relationship.person.mailingStateCode = modUtil.GetField(rsGuarantor, "State");
		relationship.person.mailingPostalCode = modUtil.GetField(rsGuarantor, "Zip").Replace("-", "");
		relationship.person.mailingCountryCode = "US";
		relationship.person.homePhone = modUtil.GetField(rsGuarantor, "Phone").Replace("-", "");
		relationship.person.mobilePhone = modUtil.GetField(rsGuarantor, "Mobile").Replace("-", "");
		relationship.person.email = modUtil.GetField(rsGuarantor, "EMail");
		relationships.Add(relationship);
		rsGuarantor.MoveNext();
	}
	return relationships;
}