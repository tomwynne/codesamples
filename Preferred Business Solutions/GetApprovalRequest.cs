public string lastError = "";

private bool GetApprovalRequest(int customerID, int contractTerm, string originatorSalesRepName, string originatorId, string brokerComments, string residual, ApprovalRequest approvalRequest) {
	try {
		var rsCustomer = db.OpenRecordset($"SELECT * FROM Customers WHERE ID={customerID}", DAO.RecordsetTypeEnum.dbOpenSnapshot);
		if(rsCustomer.EOF) {
			lastError = $"Customer {customerID} not found.";
			rsCustomer.Close();
			return false;
		}

		approvalRequest.desiredTermInMonths = contractTerm;

		var salesRepOriginatorId = new SalesReps().GetOriginatorSalesRepIdForName(originatorSalesRepName);
		approvalRequest.originatorSalesRepId = salesRepOriginatorId;
		approvalRequest.originatorId = originatorId;
		approvalRequest.subBrokerName = "";
		approvalRequest.splitTransaction = "NO";
		approvalRequest.comments = brokerComments;
		approvalRequest.creditRequested = modUtil.GetFieldDouble(rsCustomer, "Total Financed");
		approvalRequest.residual = residual;
		approvalRequest.programCode = "";
		approvalRequest.borrower = GetBorrower(customerID);
		approvalRequest.relationships = GetRelationships(customerID);
		approvalRequest.equipmentList = GetEquipmentList(customerID);
		rsCustomer.Close();
		return true;
	}
	catch(exception ex) {
		lastError = ex.Message;
		return false;
	}
}