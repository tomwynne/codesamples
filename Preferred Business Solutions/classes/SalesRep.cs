Public Class SalesRep
    Public ID As Integer = 0
    Public SalesRepName As String = ""
    Public OriginatorSalesRepId As String = ""
    Public Property LastResult As String

    Public Function Load(_ID As Integer) As Boolean
        Try
            Dim sSQL As String = "SELECT AdvantageValue AS Name, FinPacValue AS OriginatorSalesRepId FROM FinPacTranslation " +
                                 "WHERE ID=" & _ID
            Dim rs As DAO.Recordset

            rs = db.OpenRecordset(sSQL, DAO.RecordsetTypeEnum.dbOpenSnapshot)
            If rs.EOF Then
                rs.Close()
                Return False
            End If
            ID = _ID
            SalesRepName = GetField(rs, "Name")
            OriginatorSalesRepId = GetField(rs, "OriginatorSalesRepId")
            rs.Close()
            Return True
        Catch ex As Exception
            LastResult = ex.Message
            Return False
        End Try
    End Function
    Public Function Validate() As Boolean

        If SalesRepName = "" Then
            LastResult = "Name is required."
            Return False
        End If
        If OriginatorSalesRepId = "" Then
            LastResult = "Originator Sales Rep Id is required."
            Return False
        End If
        ' check for duplicates
        ' if adding ID=0 but name or salesrep id could be duplicate
        ' if updating ID=# but name or salerep id could be duplicate in other record

        Dim sSQL As String = "SELECT AdvantageValue AS Name, FinPacValue AS OriginatorSalesRepId FROM FinPacTranslation " +
                                 "WHERE FieldName='OriginatorSalesRepId' " +
                                 "AND (AdvantageValue='" & modUtil.DoubleUpQuotes(SalesRepName) & "' OR FinPacValue='" & OriginatorSalesRepId & "') "
        If ID <> 0 Then
            sSQL &= "AND ID<>" & ID
        End If
        Dim rs As DAO.Recordset
        rs = db.OpenRecordset(sSQL, DAO.RecordsetTypeEnum.dbOpenSnapshot)
        If Not rs.EOF Then
            rs.Close()
            LastResult = "Cannot save because of a duplicate name or originator sales rep Id."
            Return False
        End If
        Return True

    End Function
    Public Function Save() As Boolean

        If Not Validate() Then
            Return False
        End If
        Try
            Dim rs As DAO.Recordset
            If ID = 0 Then      ' adding
                rs = db.OpenRecordset("SELECT ID, FieldName AS Name, AdvantageValue, FinPacValue AS OriginatorSalesRepID FROM FinPacTranslation " +
                                 "WHERE FieldName='OriginatorSalesRepId'", DAO.RecordsetTypeEnum.dbOpenDynaset)
                rs.AddNew()
            Else
                rs = db.OpenRecordset("SELECT ID, FieldName AS Name, AdvantageValue, FinPacValue AS OriginatorSalesRepID FROM FinPacTranslation " +
                                 "WHERE ID=" & ID, DAO.RecordsetTypeEnum.dbOpenDynaset)
                If rs.EOF() Then
                    rs.Close()
                    Return False
                End If
                rs.Edit()
            End If
            rs("Name").Value = "OriginatorSalesRepId"
            rs("AdvantageValue").Value = SalesRepName
            rs("OriginatorSalesRepID").Value = OriginatorSalesRepId
            rs.Update()
            ID = rs("ID").Value
            rs.Close()
            WaitFor(1)
            Return True

        Catch ex As Exception
            LastResult = ex.Message
            Return False
        End Try
    End Function

    Public Function Delete() As Boolean
        Return Delete(ID)
    End Function

    Public Function Delete(_ID As Integer) As Boolean
        Try
            Dim sSQL As String = "DELETE * FROM FinPacTranslation " +
                                 "WHERE ID=" & _ID.ToString()
            db.Execute(sSQL)
            WaitFor(1)
            Return True
        Catch ex As Exception
            LastResult = ex.Message
            Return False
        End Try
    End Function
End Class
