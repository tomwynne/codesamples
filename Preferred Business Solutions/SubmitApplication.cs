public string lastError = "";

private async bool SubmitApplication()
{
	PrintStatus("Getting application data...");
	string applicationData = GetApplicationData();
	if(applicationData == "") {
		if(lastError != "") {
			PrintStatus("Application data could not be created.");
			PrintStatus("There may be a problem with the data:");
			PrintStatus(lastError);
			lastError = "Application data could not be created.\r\n" + lastError;
		} else {
			lastError = "Application cancelled";
		}
		return false;
	}
	PrintStatus("Application Data OK...");
	
	PrintStatus("Submitting application...");

	try {
		// Do a post, get the app system ID, append to url, and post again
		PrintStatus("Creating Application.  Please wait...");
		string url;
		switch(TestMode) {
			case "STAGE":
				url = URL_STAGE;
				break;
			case "PREPROD":
				url = URL_PREPROD;
				break;
			case "PROD":
				url = URL_PROD;
				break;
			default:
				url = URL_STAGE;
				break;
		}
		string JSONResult = await HttpClientPost(url, applicationData);
		if(JSONResult == "") {
			PrintStatus(lastResult);
			return false;
		}
		ApprovalResponse approvalResponse = new ApprovalResponse();
		approvalResponse = JsonConvert.DeserializeObject<ApprovalResponse>(JSONResult);
		if(approvalResponse.appSystemId == "") {
			PrintStatus("Application could not be submitted.");
			PrintStatus(approvalResponse.message);
			lastError = "Application could not be submitted.\r\n" + approvalResponse.message;
			return false;
		}

		PrintStatus("Submitting Application...");
		string JSONResult = await HttpClientPut(url + "/" + approvalResponse.appSystemId, applicationData);
		if(JSONResult == "") {
			PrintStatus(lastResult);
			return false;
		}
		approvalResponse = JsonConvert.DeserializeObject<ApprovalResponse>(JSONResult);
		if(approvalResponse.status == "APPROVED" Then) {
			PrintStatus("Application was APPROVED");
			PrintStatus(approvalResponse.message);
			PrintStatus("Pricing=" + approvalResponse.pricing);
			AddToPhoneLog("FinPac Application APPROVED", "Pricing=" + approvalResponse.pricing);
			return true;
		}
		PrintStatus("Application was " + approvalResponse.status);
		PrintStatus(approvalResponse.rejectReason);
		AddToPhoneLog("FinPac Application REJECTED", approvalResponse.rejectReason);
		return false;
	}
	catch(Exception ex) {
		var sMessage = "An error occurred submitting application.\r\n\r\n" +
							 "Error: " + ex.Message;
		if(ex.InnerException != null)
			sMessage += "\r\n" + ex.InnerException.Message;            
		return false;
	}

}
