public string lastError = "";

public string GetApplicationData() {
	lastError = "";
	try {
		ApprovalRequest approvalRequest = new ApprovalRequest();
		if(GetApprovalRequest(approvalRequest)) {
			if(IsValidApprovalRequest(approvalRequest)) {
				return JsonConvert.SerializeObject(approvalRequest);
			}
		}
		return "";
	}
	catch(exception ex) {
		lastError = ex.Message;
		Return "";
	}
}