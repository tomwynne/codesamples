public string lastError = "";

public bool IsValidApprovalRequest(ApprovalRequest approvalRequest)
{
        if(approvalRequest.originatorSalesRepId == "") {
            lastError = "Missing Sales Rep Originator Id.  Check the originator sales rep list.";
            return false;
        }
        if(approvalRequest.creditRequested == 0.0) {
            lastError = "Credit Requested must be greater than 0.";
            return false;
        }
        if(approvalRequest.desiredTermInMonths == 0) {
            lastError = "Desired Term must be selected.";
            return false;
        }
        if(approvalRequest.borrower.billingStreet == "") {
            lastError = "Billing street is required.";
            return false;
        }
        if(approvalRequest.borrower.billingCity == "") {
            lastError = "Billing city is required.";
            return false;
        }
        if(approvalRequest.borrower.billingStateCode == "") {
            lastError = "Billing state is required.";
            return false;
        }
        if(approvalRequest.borrower.billingPostalCode == "") {
            lastError = "Billing postal code is required.";
            return false;
        }
        if(approvalRequest.borrower.verificationMethod == "") {
            lastError = "Verification Method is required.";
            return false;
        }
        if(approvalRequest.borrower.businessStartDate == "") {
            lastError = "Busines Start Date is required.";
            return false;
        }
        if(approvalRequest.borrower.fedTaxId.Length > 9) {
            lastError = "Borrower Federal Tax ID cannot be more than 9 digits.";
            return false;
        }
        if(approvalRequest.relationships Is Nothing) {
            lastError = "No guarantors for this borrower.";
            return false;
        }
        foreach(Relationship relationship in approvalRequest.relationships) 
		{
            if(relationship.person.firstName == "") {
                lastError = "Principal First Name is required.";
                return false;
            }
            if(relationship.person.lastName == "") {
                lastError = "Principal Last Name is required.";
                return false;
            }
            if(relationship.person.ssn == "") {
                lastError = "Principal SSN is required.";
                return false;
            }
            if(relationship.person.ssn.Length > 9) {
                lastError = "Principal SSN cannot be more than 9 digits.";
                return false;
            }
            if(relationship.person.percentageOwnership == 0) {
                lastError = "Percentage Ownership is required.";
                return false;
            }
            if(relationship.person.mailingStateCode == "") {
                lastError = "Principal Mailing State is required.";
                return false;
            }
            if(relationship.person.mailingPostalCode == "") {
                lastError = "Principal Postal Code is required.";
                return false;
            }
            if(relationship.person.mailingCountryCode == "") {
                lastError = "Principal Country Code is required.";
                return false;
            }
        }
        if(approvalRequest.equipmentList == null) {
            lastError = "No equipment exists for this borrower.";
            return false;
        }
        foreach(Equipment equipment in approvalRequest.equipmentList)
		{
            if(equipment.equipmentCostPerUnit == 0.0) {
                lastError = "Equipment Cost Per Unit is required.";
                return false;
            }
            if(equipment.equipmentQuantity == 0) {
                lastError = "Equipment Quantity is required.";
                return false;
            }
            if(equipment.description == "") {
                lastError = "Equipment Description is required.";
                return false;
            }
            if(equipment.typeOfTransaction == "") {
                lastError = "Equipment Type of Transaction is required.";
                return false;
            }
            if(equipment.condition == "") {
                lastError = "Equipment Condition is required.";
                return false;
            }
            if(equipment.equipmentType == "") {
                lastError = "Equipment Type is required.";
                return false;
            }
		}
	}
	return true;
}
