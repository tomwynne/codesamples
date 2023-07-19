public class ApprovalRequest
{
    public int desiredTermInMonths;
    public string originatorSalesRepId;
    public string originatorId As String
    public string subBrokerName As String
    public string splitTransaction As String
    public string comments As String
    public string creditRequested As Double
    public string residual As String
    public string programCode As String
    public string borrower As Borrower
    public string relationships As List(Of Relationship)
    public string equipmentList As List(Of Equipment)

    public void ApprovalRequest()
	{
        this.relationships = new List<Relationship>();
        this.equipmentList = new List<Equipment>();
    }

}
