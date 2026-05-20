Imports System.Data.SqlClient
Public Class MyClassExecuteData


    Dim connectString As String = ConfigurationManager.ConnectionStrings.Item(("face_scanner")).ConnectionString
    Public Function ExecuteNonQuery(ByVal prmSQL As String) As Boolean
        Dim tag As New StringBuilder
        Dim query As String = prmSQL.ToString

        Using conn As New SqlConnection(connectString)
            conn.Open()
            Dim cmd As New SqlCommand(query, conn)
            cmd.CommandType = CommandType.Text
            cmd.CommandTimeout = 1024
            cmd.ExecuteNonQuery()
            conn.Close()
        End Using
        Return True
    End Function
    Public Function ExecuteNonQuery_1(ByVal prmSQL As String, connectString_1 As String) As Boolean
        Dim tag As New StringBuilder
        Dim query As String = prmSQL.ToString

        Using conn As New SqlConnection(connectString_1)
            conn.Open()
            Dim cmd As New SqlCommand(query, conn)
            cmd.CommandType = CommandType.Text
            cmd.CommandTimeout = 1024
            cmd.ExecuteNonQuery()
            conn.Close()
        End Using
        Return True
    End Function

    '    For Each row As DataRow In obj.GetDataTable(sql).Rows
    '       xxx = row("xxxx").ToString()
    '   Next

    Public Function GetData_row(SQL As String) As Boolean
        Dim tag As New StringBuilder
        Dim query As String = SQL.ToString
        Dim t1 As New DataTable()
        Dim chk As Boolean
        Using conn As New SqlConnection(connectString)
            conn.Open()
            Dim cmd As New SqlCommand(query, conn)

            Dim ddd As String = cmd.Container.ToString
            'Using a As New SqlDataAdapter(cmd)
            '    a.Fill(t1)
            'End Using
            conn.Close()
        End Using

        Return True
    End Function

    Public Function GetDataTable(SQL As String) As DataTable
        Dim tag As New StringBuilder
        Dim query As String = SQL.ToString
        Dim t1 As New DataTable()
        Using conn As New SqlConnection(connectString)
            conn.Open()
            Dim cmd As New SqlCommand(query, conn)
            Using a As New SqlDataAdapter(cmd)
                a.Fill(t1)
            End Using
            conn.Close()
        End Using

        Return t1
    End Function
    Public Function GetDataTable_1(SQL As String, connectString_1 As String) As DataTable
        Dim tag As New StringBuilder
        Dim query As String = SQL.ToString
        Dim t1 As New DataTable()
        Using conn As New SqlConnection(connectString_1)
            conn.Open()
            Dim cmd As New SqlCommand(query, conn)
            Using a As New SqlDataAdapter(cmd)
                a.Fill(t1)
            End Using
            conn.Close()
        End Using

        Return t1
    End Function
    Public Function GetDataTable(SQL As String, Repeater1 As Repeater) As DataTable
        Dim tag As New StringBuilder
        Dim query As String = SQL.ToString
        Dim t1 As New DataTable()
        Using conn As New SqlConnection(connectString)
            conn.Open()
            Dim cmd As New SqlCommand(query, conn)
            Using a As New SqlDataAdapter(cmd)
                a.Fill(t1)
            End Using
            conn.Close()
        End Using

        Repeater1.DataSource = t1
        Repeater1.DataBind()

        Return t1
    End Function
    Public Function GetDataTable(SQLROW_NUMBER As String, start As String, LimitRow As Integer, prmRepeater As Repeater) As Integer

        Dim sql As New StringBuilder
        Dim paging As Integer = Val(start)
        Dim c1 As String = IIf(paging = 0, 1, (LimitRow * (paging - 1)) + 1)
        Dim c2 As String = IIf(paging = 0, LimitRow, LimitRow * paging) ' 10 * paging
        Dim RowCount As Integer

        sql.Append("SELECT *  FROM (")

        sql.Append(SQLROW_NUMBER)

        sql.Append(") AS showData  WHERE  rownumber >= (" & c1 & ") AND rownumber <= (" & c2 & ")")

        Using conn As New SqlConnection(connectString)
            conn.Open()
            Dim query As String = sql.ToString
            Dim cmd As New SqlCommand(query, conn)
            Dim t1 As New DataTable()
            Using a As New SqlDataAdapter(cmd)
                a.Fill(t1)

            End Using

            prmRepeater.DataSource = t1
            prmRepeater.DataBind()
            'RowCount = Val(t1.Rows.Count)

            '**** Row Count *****
            Dim sqlCount As String
            sqlCount = Mid(SQLROW_NUMBER, 1, InStr(SQLROW_NUMBER, "FROM") - 1)
            If sqlCount = "" Then sqlCount = Mid(SQLROW_NUMBER, 1, InStr(SQLROW_NUMBER, "from") - 1)
            SQLROW_NUMBER = SQLROW_NUMBER.Replace(sqlCount, "SELECT COUNT(*) ")

            Dim comm As New SqlCommand(SQLROW_NUMBER, conn)
            Dim count As Int32 = DirectCast(comm.ExecuteScalar(), Int32)
            RowCount = count
            '**** END  Row Count *****
            conn.Close()
        End Using

        Return RowCount

    End Function

    Public Function GetDataTable(SQLROW_NUMBER As String, start As Integer, LimitRow As Integer, prmRepeater As Repeater, ByRef t1 As DataTable) As Integer

        Dim sql As New StringBuilder
        Dim paging As Integer = start
        Dim c1 As String = IIf(paging = 0, 1, (LimitRow * (paging - 1)) + 1)
        Dim c2 As String = IIf(paging = 0, LimitRow, LimitRow * paging) ' 10 * paging
        Dim RowCount As Integer

        sql.Append("SELECT *  FROM (")

        sql.Append(SQLROW_NUMBER)

        sql.Append(") AS showData  WHERE  rownumber >= (" & c1 & ") AND rownumber <= (" & c2 & ")")

        Using conn As New SqlConnection(connectString)
            conn.Open()
            Dim query As String = sql.ToString
            Dim cmd As New SqlCommand(query, conn)
            'Dim t1 As New DataTable()
            Using a As New SqlDataAdapter(cmd)
                a.Fill(t1)

            End Using

            prmRepeater.DataSource = t1
            prmRepeater.DataBind()
            'RowCount = Val(t1.Rows.Count)

            '**** Row Count *****
            Dim sqlCount As String
            sqlCount = Mid(SQLROW_NUMBER, 1, InStr(SQLROW_NUMBER, "FROM") - 1)
            If sqlCount = "" Then sqlCount = Mid(SQLROW_NUMBER, 1, InStr(SQLROW_NUMBER, "from") - 1)
            SQLROW_NUMBER = SQLROW_NUMBER.Replace(sqlCount, "SELECT COUNT(*) ")

            Dim comm As New SqlCommand(SQLROW_NUMBER, conn)
            Dim count As Int32 = DirectCast(comm.ExecuteScalar(), Int32)
            RowCount = count
            '**** END  Row Count *****
            conn.Close()
        End Using

        Return RowCount

    End Function

    Public Function GetDataSingleField(sql As String, Field As String) As String
        Dim strField As String
        Dim query As String = sql.ToString
        Dim dtb As New DataTable
        Using conn As New SqlConnection(connectString)
            conn.Open()
            Dim cmd As New SqlCommand(query, conn)
            Dim t1 As New DataTable()

            For Each row As DataRow In t1.Rows
                strField = row(Field).ToString()
            Next

            conn.Close()
        End Using
        Return strField

    End Function

    Public Function EXISTS_DATA(sql As String) As Boolean
        If GetDataTable(sql).Rows.Count >= 1 Then
            Return True
        Else
            Return False
        End If
    End Function

    Public Function GetDataArrayField(sql As String) As StringBuilder
        Dim tag As New StringBuilder
        Dim query As String = sql.ToString
        Dim dtb As New DataTable
        Using conn As New SqlConnection(connectString)
            conn.Open()
            Dim cmd As New SqlCommand(query, conn)
            Dim t1 As New DataTable()
            Using a As New SqlDataAdapter(cmd)
                a.Fill(t1)
            End Using

            For Each row As DataRow In t1.Rows
                For Each column As DataColumn In t1.Columns
                    tag.Append("<" & column.ColumnName & ">" & row(column) & "</" & column.ColumnName & ">")

                Next column
            Next row


            conn.Close()
        End Using
        Return tag

    End Function

#Region "**** เพื่อการแบ่งหน้า ****"

    Public Function GetCountRow(SQLROW_NUMBER As String) As String
        Dim count As Int32
        Using conn As New SqlConnection(connectString)
            conn.Open()
            Dim sql As String
            sql = Mid(SQLROW_NUMBER, 1, InStr(SQLROW_NUMBER, "FROM") - 1)
            If sql = "" Then sql = Mid(SQLROW_NUMBER, 1, InStr(SQLROW_NUMBER, "from") - 1)
            SQLROW_NUMBER = SQLROW_NUMBER.Replace(sql, "SELECT COUNT(*) ")


            Dim comm As New SqlCommand(SQLROW_NUMBER, conn)
            count = DirectCast(comm.ExecuteScalar(), Int32)
            conn.Close()
        End Using
        Return count
    End Function

    Function genPaging(rowCount As Integer, pagingButton As Integer, intStart As Integer, strPage As String, LimitRow As Integer, ctrGenPaging As HtmlGenericControl, ctrCount_All As HtmlGenericControl) As String
        Dim c1, c2 As Integer

        If rowCount <= LimitRow Then
            c1 = 1
            c2 = 1
            ctrCount_All.InnerHtml = "พบ <strong>1</strong>-<strong>" & FormatNumber(rowCount, 0) & "</strong> จาก <strong>" & FormatNumber(rowCount, 0) & "</strong>"
            ctrGenPaging.InnerHtml = getPaging(strPage, intStart, c1, c2, rowCount, LimitRow)
            Exit Function
        ElseIf intStart < pagingButton Then
            c1 = 1
            c2 = pagingButton
        ElseIf intStart >= pagingButton Then
            c1 = intStart - (pagingButton / 2)
            c2 = intStart + (pagingButton / 2) - 1
        End If

        ctrCount_All.InnerHtml = "พบ <strong>" & FormatNumber(Val(IIf(intStart = 0, 1, (LimitRow * (intStart - 1)) + 1)), 0) & "</strong>-<strong>" & FormatNumber(Val(IIf(intStart = 0, LimitRow, LimitRow * intStart)), 0) & "</strong> จาก <strong>" & FormatNumber(rowCount, 0) & "</strong>"
        ctrGenPaging.InnerHtml = getPaging(strPage, intStart, c1, c2, rowCount, LimitRow)

    End Function

    Private Function getPaging(QueryString_Page As String, start As String, c1 As Integer, c2 As Integer, pageAll As Integer, row As Integer) As String
        Dim url As String = HttpContext.Current.Request.Url.AbsoluteUri '& "?start="
        If InStr(url, "start") = 0 Then
            If QueryString_Page = "" Then
                url = url & "?start="
            Else
                url = url & "&start="
            End If
        Else
            If QueryString_Page = "" Then
                url = Left(url, InStr(url, "start") - 2) & "?start="
            Else
                url = Left(url, InStr(url, "start") - 2) & "&start="
            End If
        End If


        Dim i As Integer
        Dim str As New StringBuilder
        start = Val(start)
        Dim li_left = <li><a href="{url}" id="li_left" onclick="return processClose(this.id);"><i class="fa fa-arrow-left"></i></a></li>.ToString.Replace("{url}", url & 1)
        Dim li = <li><a href="{url}" id="{id}" onclick="return processClose(this.id);">{1}</a></li>
        Dim li_active = <li class="active" id="{id}" onclick="return processClose(this.id);"><a href="javascript:void(0)">{1}</a></li>

        Dim li_right = <li><a href="{url}" id="li_right" onclick="return processClose(this.id);"><i class="fa fa-arrow-right"></i></a></li>.ToString.Replace("{url}", url & pageAll / row)

        str.Append("<ul class='pagination'>")
        str.Append(li_left)

        If start = 0 Then start = 1
        For i = c1 To c2 'pageAll / row
            If i = start Then
                str.Append(li_active.ToString.Replace("{1}", i).Replace("{url}", url & i).Replace("{id}", "paging_" & i))
            Else
                str.Append(li.ToString.Replace("{1}", i).Replace("{url}", url & i).Replace("{id}", "paging_" & i))
            End If

        Next
        str.Append(li_right)
        str.Append("</ul>")
        Return str.ToString
    End Function

    Function genPaging(_url As String,
                      rowCount As Integer,
                      pagingButton As Integer,
                      intStart As Integer,
                      strPage As String,
                      LimitRow As Integer,
                      ctrGenPaging As HtmlGenericControl,
                      ctrCount_All As HtmlGenericControl) As String
        Dim c1, c2 As Integer
        Dim rowCountLimit As Double
        rowCountLimit = (rowCount / LimitRow)

        If pagingButton >= rowCountLimit Then pagingButton = rowCountLimit
        If InStr(rowCountLimit.ToString, "0") >= 1 Then pagingButton = pagingButton + 1


        If rowCount <= LimitRow Then
            c1 = 1
            c2 = 1
            ctrCount_All.InnerHtml = "พบ 1-" & FormatNumber(rowCount, 0) & " จาก " & FormatNumber(rowCount, 0) & ""
            ctrGenPaging.InnerHtml = getPaging(_url, strPage, intStart, c1, c2, rowCount, LimitRow)
            Exit Function
        ElseIf intStart < pagingButton Then
            c1 = 1
            c2 = pagingButton
        ElseIf intStart >= pagingButton Then
            c1 = intStart - (pagingButton / 2)
            c2 = intStart + (pagingButton / 2) - 1
        End If

        ctrCount_All.InnerHtml = "พบ " & FormatNumber(Val(IIf(intStart = 0, 1, (LimitRow * (intStart - 1)) + 1)), 0) & "-" & FormatNumber(Val(IIf(intStart = 0, LimitRow, LimitRow * intStart)), 0) & " จาก " & FormatNumber(rowCount, 0) & ""
        ctrGenPaging.InnerHtml = getPaging(_url, strPage, intStart, c1, c2, rowCount, LimitRow)

    End Function

    Function genPaging(_url As String,
                      rowCount As Integer,
                      pagingButton As Integer,
                      intStart As Integer,
                      strPage As String,
                      LimitRow As Integer,
                      ltr_GenPaging As Literal,
                      ctrCount_All As HtmlGenericControl) As String
        Dim c1, c2 As Integer
        Dim rowCountLimit As Double
        rowCountLimit = (rowCount / LimitRow)

        If pagingButton >= rowCountLimit Then pagingButton = rowCountLimit
        If InStr(rowCountLimit.ToString, "0") >= 1 Then pagingButton = pagingButton + 1


        If rowCount <= LimitRow Then
            c1 = 1
            c2 = 1
            'ctrCount_All.InnerHtml = "พบ 1-" & FormatNumber(rowCount, 0) & " จาก " & FormatNumber(rowCount, 0) & ""
            'ltr_GenPaging.Text = getPaging(_url, strPage, intStart, c1, c2, rowCount, LimitRow)
            Exit Function
        ElseIf intStart < pagingButton Then
            c1 = 1
            c2 = pagingButton
        ElseIf intStart >= pagingButton Then
            c1 = intStart - (pagingButton / 2)
            c2 = intStart + (pagingButton / 2) - 1
        End If

        ctrCount_All.InnerHtml = "พบ " & FormatNumber(Val(IIf(intStart = 0, 1, (LimitRow * (intStart - 1)) + 1)), 0) & "-" & FormatNumber(Val(IIf(intStart = 0, LimitRow, LimitRow * intStart)), 0) & " จาก " & FormatNumber(rowCount, 0) & " รายการ"
        ltr_GenPaging.Text = getPaging(_url, strPage, intStart, c1, c2, rowCount, LimitRow)

    End Function

    Private Function getPaging(_url As String, QueryString_Page As String, start As String, c1 As Integer, c2 As Integer, pageAll As Integer, row As Integer) As String
        Dim url As String = _url  '& "/" 'HttpContext.Current.Request.Url.AbsoluteUri '& "?start="
        Dim i As Integer
        Dim str As New StringBuilder
        start = Val(start)
        Dim li_left = <a href="{url}" id="li_left" onclick="return processClosePagin(this.id);"><i class="fa fa-arrow-left"></i></a>.ToString.Replace("{url}", url.Replace("{page}", 1))
        Dim li = <a href="{url}" id="{id}" onclick="return processClosePagin(this.id);"><span>{1}</span></a>
        Dim li_active = <a href="javascript:void(0)" class="active"><span>{1}</span></a>

        Dim li_right = <a href="{url}" id="li_right" onclick="return processClosePagin(this.id);"><i class="fa fa-arrow-right"></i></a>.ToString.Replace("{url}", url.Replace("{page}", CInt(pageAll / row))) 'url & CInt(pageAll / row))

        str.Append("<nav aria-label='...'><ul class='pagination pg-secondary mb-0'>")
        'str.Append(li_left)

        If start = 0 Then start = 1
        Dim _countPage As Integer
        _countPage = pageAll / row
        If (_countPage * row) < (pageAll) Then _countPage = _countPage + 1
        For i = 1 To _countPage ' c2
            If Not i = start Then
                'str.Append("<li class='page-item'><a id='{id}' onclick='return processClosePagin(this.id);' class='page-link' href='{url}'>{i} <span class='sr-only'>(current)</span></a></li>".Replace("{i}", i).Replace("{url}", url.Replace("{page}", i)).Replace("{id}", "paging_" & i)) 'li_active.ToString.Replace("{1}", i).Replace("{url}", url.Replace("{page}", i)).Replace("{id}", "paging_" & i))
                str.Append("<li class='page-item '><a id='{id}' class='page-link' onclick='return processClosePagin(this.id);' href='{url}'>{i}</a><span class='sr-only'>(current)</span></li>".Replace("{i}", i).Replace("{url}", url.Replace("{page}", i)).Replace("{id}", "paging_" & i))
            Else
                str.Append("<li class='page-item active disabled'><span class='page-link'>{i}<span class='sr-only'>(current)</span></span></li>".Replace("{i}", i).Replace("{url}", url.Replace("{page}", i)).Replace("{id}", "paging_" & i))
                'str.Append(li.ToString.Replace("{1}", i).Replace("{url}", url.Replace("{page}", i)).Replace("{id}", "paging_" & i))
            End If

        Next
        'str.Append(li_right)
        str.Append("</ul></nav>")
        Return str.ToString
    End Function

    Private Function getPaging2222(_url As String, QueryString_Page As String, start As String, c1 As Integer, c2 As Integer, pageAll As Integer, row As Integer) As String
        Dim url As String = _url  '& "/" 'HttpContext.Current.Request.Url.AbsoluteUri '& "?start="
        Dim i As Integer
        Dim str As New StringBuilder
        start = Val(start)
        Dim li_left = <a href="{url}" id="li_left" onclick="return processClosePagin(this.id);"><i class="fa fa-arrow-left"></i></a>.ToString.Replace("{url}", url.Replace("{page}", 1))
        Dim li = <a href="{url}" id="{id}" onclick="return processClosePagin(this.id);"><span>{1}</span></a>
        Dim li_active = <a href="javascript:void(0)" class="active"><span>{1}</span></a>

        Dim li_right = <a href="{url}" id="li_right" onclick="return processClosePagin(this.id);"><i class="fa fa-arrow-right"></i></a>.ToString.Replace("{url}", url.Replace("{page}", CInt(pageAll / row))) 'url & CInt(pageAll / row))

        str.Append("<div class='pagination2 text-center '>")
        'str.Append(li_left)

        If start = 0 Then start = 1
        Dim _countPage As Integer
        _countPage = pageAll / row
        If (_countPage * row) < (pageAll) Then _countPage = _countPage + 1
        For i = 1 To _countPage ' c2
            If i = start Then
                str.Append(li_active.ToString.Replace("{1}", i).Replace("{url}", url.Replace("{page}", i)).Replace("{id}", "paging_" & i))
            Else
                str.Append(li.ToString.Replace("{1}", i).Replace("{url}", url.Replace("{page}", i)).Replace("{id}", "paging_" & i))
            End If

        Next
        'str.Append(li_right)
        str.Append("</div>")
        Return str.ToString
    End Function

#End Region


End Class
