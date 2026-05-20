Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Text
Imports System.Web

Public Class FaceMatchHandler
    Implements IHttpHandler

    Private Const DefaultThreshold As Double = 0.5R

    Public ReadOnly Property IsReusable As Boolean Implements IHttpHandler.IsReusable
        Get
            Return True
        End Get
    End Property

    Public Sub ProcessRequest(context As HttpContext) Implements IHttpHandler.ProcessRequest
        context.Response.ContentEncoding = Encoding.UTF8
        context.Response.ContentType = "application/json"
        context.Response.Cache.SetCacheability(HttpCacheability.NoCache)
        context.Response.Cache.SetNoStore()
        context.Response.TrySkipIisCustomErrors = True

        Try
            If String.Equals(context.Request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase) Then
                WriteJson(context, FaceDescriptorStore.GetSummary(context))
                Return
            End If

            If Not String.Equals(context.Request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase) Then
                context.Response.StatusCode = 405
                context.Response.AppendHeader("Allow", "GET, POST")
                WriteJson(context, New ApiErrorResponse With {
                    .message = "Method not allowed",
                    .success = False
                })
                Return
            End If

            Dim matchRequest As FaceMatchRequest = ParseRequest(context)
            Dim requestedThreshold As Double = DefaultThreshold

            If matchRequest.threshold.HasValue Then
                requestedThreshold = matchRequest.threshold.Value
            End If

            Dim descriptorRequests As List(Of Double()) = ParseDescriptorRequests(matchRequest.descriptors)
            Dim matchResponse As FaceMatchResponse = FaceDescriptorStore.FindBestMatches(context, descriptorRequests, requestedThreshold)
            WriteJson(context, matchResponse)
        Catch ex As ApplicationException
            context.Response.StatusCode = 400
            WriteJson(context, New ApiErrorResponse With {
                .message = ex.Message,
                .success = False
            })
        Catch ex As Exception
            context.Response.StatusCode = 500
            WriteJson(context, New ApiErrorResponse With {
                .message = "เกิดข้อผิดพลาดระหว่างเทียบใบหน้า",
                .success = False
            })
        End Try
    End Sub

    Private Function ParseRequest(context As HttpContext) As FaceMatchRequest
        Using requestReader As New StreamReader(context.Request.InputStream, Encoding.UTF8)
            Dim rawJson As String = requestReader.ReadToEnd()

            If String.IsNullOrWhiteSpace(rawJson) Then
                Throw New ApplicationException("ไม่พบข้อมูล descriptor สำหรับเทียบใบหน้า")
            End If

            Try
                Dim serializer = FaceDescriptorStore.CreateSerializer()
                Dim matchRequest As FaceMatchRequest = serializer.Deserialize(Of FaceMatchRequest)(rawJson)

                If matchRequest Is Nothing OrElse matchRequest.descriptors Is Nothing OrElse matchRequest.descriptors.Count = 0 Then
                    Throw New ApplicationException("ไม่พบข้อมูล descriptor สำหรับเทียบใบหน้า")
                End If

                Return matchRequest
            Catch ex As ApplicationException
                Throw
            Catch ex As Exception
                Throw New ApplicationException("รูปแบบข้อมูล descriptor ที่ส่งมาไม่ถูกต้อง", ex)
            End Try
        End Using
    End Function

    Private Function ParseDescriptorRequests(rawDescriptors As List(Of List(Of Double))) As List(Of Double())
        If rawDescriptors Is Nothing OrElse rawDescriptors.Count = 0 Then
            Throw New ApplicationException("ไม่พบข้อมูล descriptor สำหรับเทียบใบหน้า")
        End If

        Dim descriptorRequests As New List(Of Double())()

        For Each rawDescriptor As List(Of Double) In rawDescriptors
            If rawDescriptor Is Nothing OrElse rawDescriptor.Count <> FaceDescriptorStore.DescriptorLength Then
                Throw New ApplicationException("descriptor ที่ส่งมาไม่ครบ 128 ค่า")
            End If

            For Each descriptorValue As Double In rawDescriptor
                If Double.IsNaN(descriptorValue) OrElse Double.IsInfinity(descriptorValue) Then
                    Throw New ApplicationException("descriptor ที่ส่งมามีค่าที่ไม่ถูกต้อง")
                End If
            Next

            descriptorRequests.Add(rawDescriptor.ToArray())
        Next

        Return descriptorRequests
    End Function

    Private Sub WriteJson(context As HttpContext, payload As Object)
        Dim serializer = FaceDescriptorStore.CreateSerializer()
        context.Response.Write(serializer.Serialize(payload))
    End Sub

    Private Class FaceMatchRequest
        Public Property descriptors As List(Of List(Of Double))
        Public Property threshold As Nullable(Of Double)
    End Class

    Private Class ApiErrorResponse
        Public Property success As Boolean
        Public Property message As String
    End Class
End Class
