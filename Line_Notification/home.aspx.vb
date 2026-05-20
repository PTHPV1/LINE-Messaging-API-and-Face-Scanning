Imports System.Configuration
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Drawing.Imaging
Imports System.IO
Imports System.Net
Imports System.Text
Imports System.Web
Imports System.Web.Script.Serialization

Public Class test
    Inherits System.Web.UI.Page

    Private Const BroadcastEndpoint As String = "https://api.line.me/v2/bot/message/broadcast"
    Private Const DefaultAccessToken As String = "<AccessToken>"
    Private Const DefaultBroadcastMessageText As String = "ส่งหลายคน"
    Private Const DefaultRecognizedFaceNotificationText As String = "แจ้งเตือนตรวจพบบุคคลจาก Face Scanner"
    Private Const UploadFolderVirtualPath As String = "~/Uploads/LineImages/"
    Private Const KnownFaceFolderVirtualPath As String = "~/Uploads/KnownFaces/"
    Private Const RecognizedFaceFolderVirtualPath As String = "~/Uploads/RecognizedFaces/"
    Private Const KnownFaceDescriptorDatabaseVirtualPath As String = "~/App_Data/known-face-descriptors.json"
    Private Const KnownFaceDisplayNameFileName As String = "_display-name.txt"
    Private Const PreviewSuffix As String = "_preview.jpg"
    Private Const MaxLineImageBytes As Integer = 10 * 1024 * 1024
    Private Const MaxPreviewBytes As Integer = 1024 * 1024
    Private Const SavedImagesPageSize As Integer = 24
    Private Const KnownFaceSamplesPerPerson As Integer = 5
    Private Const KnownFaceDisplayPageSize As Integer = 30
    Private Const RecognizedFaceDisplayPageSize As Integer = 30
    Private Const FaceDescriptorLength As Integer = 128
    Private Shared ReadOnly AllowedImageExtensions As String() = {".jpg", ".jpeg", ".png"}
    Private Shared ReadOnly PreviewQualityLevels As Long() = {85L, 75L, 65L, 55L, 45L}
    Private Shared ReadOnly DescriptorDatabaseSyncRoot As New Object()

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        ConfigureUploadControls()

        If Not IsPostBack Then
            txtMessage.Text = DefaultBroadcastMessageText
            btnSaveFaceSnapshot.Enabled = False
            hfResumeFaceScanner.Value = String.Empty
            RefreshPageData()
        End If
    End Sub

    Private Sub RefreshPageData()
        BindSavedImages()
        BindKnownFaces()
        BindRecognizedFaces()
    End Sub

    Private Function CreateJsonSerializer() As JavaScriptSerializer
        Return New JavaScriptSerializer() With {
            .MaxJsonLength = Integer.MaxValue,
            .RecursionLimit = 256
        }
    End Function

    Protected Sub btnSend_Click(sender As Object, e As EventArgs) Handles btnSend.Click
        Try
            litImageInfo.Text = String.Empty
            Dim responseMessage As String = SendLineMessages()
            RefreshPageData()
            RenderStatus(True, responseMessage)
        Catch ex As Exception
            RefreshPageData()
            RenderStatus(False, ex.Message)
        End Try
    End Sub

    Protected Sub btnSaveOnly_Click(sender As Object, e As EventArgs) Handles btnSaveOnly.Click
        Try
            litImageInfo.Text = String.Empty

            Dim savedImage As UploadedImageInfo = SaveImageForStorage()
            Dim publicOriginalUrl As String = String.Empty
            Dim publicPreviewUrl As String = String.Empty

            If TryGetPublicImageUrls(savedImage, txtPublicBaseUrl.Text.Trim(), publicOriginalUrl, publicPreviewUrl) Then
                txtImageUrl.Text = publicOriginalUrl
                txtPreviewImageUrl.Text = publicPreviewUrl
            End If

            litImageInfo.Text = BuildSavedImageInfoHtml(savedImage, publicOriginalUrl, publicPreviewUrl)
            RefreshPageData()
            RenderStatus(True, "บันทึกรูปภาพเรียบร้อยแล้ว")
        Catch ex As Exception
            RefreshPageData()
            RenderStatus(False, ex.Message)
        End Try
    End Sub

    Protected Sub btnRefreshSaved_Click(sender As Object, e As EventArgs) Handles btnRefreshSaved.Click
        RefreshPageData()
        RenderStatus(True, "สแกนรายการรูปภาพที่บันทึกไว้เรียบร้อยแล้ว")
    End Sub

    Protected Sub btnSaveFaceSnapshot_Click(sender As Object, e As EventArgs) Handles btnSaveFaceSnapshot.Click
        Try
            litImageInfo.Text = String.Empty

            Dim savedImage As UploadedImageInfo = SaveFaceSnapshotData(hfFaceSnapshotData.Value)
            Dim publicOriginalUrl As String = String.Empty
            Dim publicPreviewUrl As String = String.Empty

            If TryGetPublicImageUrls(savedImage, txtPublicBaseUrl.Text.Trim(), publicOriginalUrl, publicPreviewUrl) Then
                txtImageUrl.Text = publicOriginalUrl
                txtPreviewImageUrl.Text = publicPreviewUrl
            End If

            hfFaceSnapshotData.Value = String.Empty
            litImageInfo.Text = BuildSavedImageInfoHtml(savedImage, publicOriginalUrl, publicPreviewUrl)
            RefreshPageData()
            RenderStatus(True, "บันทึกภาพสแกนใบหน้าเรียบร้อยแล้ว")
        Catch ex As Exception
            RefreshPageData()
            RenderStatus(False, ex.Message)
        End Try
    End Sub

    Protected Sub btnRegisterKnownFace_Click(sender As Object, e As EventArgs) Handles btnRegisterKnownFace.Click
        Dim savedImage As UploadedImageInfo = Nothing

        Try
            litImageInfo.Text = String.Empty

            Dim personName As String = NormalizePersonDisplayName(txtKnownFaceName.Text)
            Dim descriptorValues As Double() = ParseKnownFaceDescriptorValues(hfKnownFaceDescriptorData.Value)
            savedImage = SaveKnownFaceRegistration(personName)
            SaveKnownFaceDescriptorRecord(personName, savedImage, descriptorValues)

            hfFaceSnapshotData.Value = String.Empty
            hfKnownFaceDescriptorData.Value = String.Empty
            litImageInfo.Text = BuildKnownFaceInfoHtml(personName, savedImage)
            RefreshPageData()
            RenderStatus(True, "ลงทะเบียนใบหน้าของ " & personName & " เรียบร้อยแล้ว")
        Catch ex As Exception
            If savedImage IsNot Nothing Then
                DeleteSavedImageFiles(savedImage)
            End If

            RefreshPageData()
            RenderStatus(False, ex.Message)
        End Try
    End Sub

    Protected Sub btnRefreshKnownFaces_Click(sender As Object, e As EventArgs) Handles btnRefreshKnownFaces.Click
        RefreshPageData()
        RenderStatus(True, "รีเฟรชฐานข้อมูลใบหน้าเรียบร้อยแล้ว")
    End Sub

    Protected Sub btnSaveRecognizedFace_Click(sender As Object, e As EventArgs) Handles btnSaveRecognizedFace.Click
        Try
            litImageInfo.Text = String.Empty

            Dim personName As String = NormalizePersonDisplayName(hfRecognizedPersonName.Value)
            Dim savedImage As UploadedImageInfo = SaveRecognizedFaceCapture(personName, hfRecognizedSnapshotData.Value)
            Dim lineNotificationResult As LineSendAttemptResult = SendRecognizedFaceLineNotification(personName, savedImage)

            hfRecognizedSnapshotData.Value = String.Empty
            hfRecognizedPersonName.Value = String.Empty
            hfResumeFaceScanner.Value = "1"
            litImageInfo.Text = BuildRecognizedFaceInfoHtml(
                personName,
                savedImage,
                lineNotificationResult.PublicOriginalUrl,
                lineNotificationResult.PublicPreviewUrl,
                lineNotificationResult)

            RefreshPageData()
            Dim statusMessage As String = "บันทึกการตรวจพบใบหน้าของ " & personName & " เรียบร้อยแล้ว"

            If lineNotificationResult.IsSuccess Then
                statusMessage &= " และส่ง LINE เรียบร้อยแล้ว"
            ElseIf Not String.IsNullOrWhiteSpace(lineNotificationResult.Message) Then
                statusMessage &= " แต่ส่ง LINE ไม่สำเร็จ: " & lineNotificationResult.Message
            End If

            RenderStatus(True, statusMessage)
        Catch ex As Exception
            hfResumeFaceScanner.Value = "1"
            RefreshPageData()
            RenderStatus(False, ex.Message)
        End Try
    End Sub

    Private Sub ConfigureUploadControls()
        fuImage.Attributes("accept") = ".jpg,.jpeg,.png,image/jpeg,image/png"
        fuScanImage.Attributes("accept") = "image/*"
        fuScanImage.Attributes("capture") = "environment"
    End Sub

    Private Function SaveKnownFaceRegistration(personName As String) As UploadedImageInfo
        If String.IsNullOrWhiteSpace(personName) Then
            Throw New ApplicationException("กรุณาระบุชื่อบุคคลก่อนลงทะเบียนใบหน้า")
        End If

        Dim personFolderVirtualPath As String = GetKnownFacePersonFolderVirtualPath(personName)
        EnsurePersonDisplayName(personFolderVirtualPath, personName)

        If Not String.IsNullOrWhiteSpace(hfFaceSnapshotData.Value) Then
            Return SaveFaceSnapshotData(hfFaceSnapshotData.Value, personFolderVirtualPath)
        End If

        Dim selectedUpload As FileUpload = GetSelectedUpload()

        If selectedUpload IsNot Nothing Then
            Return SaveUploadedImage(selectedUpload, personFolderVirtualPath)
        End If

        If Not String.IsNullOrWhiteSpace(txtImageUrl.Text.Trim()) Then
            Return SaveImageFromRemoteUrl(txtImageUrl.Text.Trim(), personFolderVirtualPath)
        End If

        Throw New ApplicationException("กรุณาจับภาพจาก face scanner, อัปโหลดรูป หรือกรอก URL รูปภาพก่อนลงทะเบียนใบหน้า")
    End Function

    Private Function ParseKnownFaceDescriptorValues(rawDescriptorJson As String) As Double()
        If String.IsNullOrWhiteSpace(rawDescriptorJson) Then
            Throw New ApplicationException("ไม่พบตัวเลขอ้างอิงใบหน้า กรุณาลองลงทะเบียนใหม่อีกครั้ง")
        End If

        Dim serializer As JavaScriptSerializer = CreateJsonSerializer()
        Dim descriptorValues As List(Of Double) = Nothing

        Try
            descriptorValues = serializer.Deserialize(Of List(Of Double))(rawDescriptorJson)
        Catch ex As Exception
            Throw New ApplicationException("ตัวเลขอ้างอิงใบหน้าที่ส่งมาลงทะเบียนไม่ถูกต้อง", ex)
        End Try

        If descriptorValues Is Nothing OrElse descriptorValues.Count <> FaceDescriptorLength Then
            Throw New ApplicationException("ตัวเลขอ้างอิงใบหน้ามีจำนวนไม่ถูกต้อง")
        End If

        For Each descriptorValue As Double In descriptorValues
            If Double.IsNaN(descriptorValue) OrElse Double.IsInfinity(descriptorValue) Then
                Throw New ApplicationException("ตัวเลขอ้างอิงใบหน้ามีค่าที่ไม่รองรับ")
            End If
        Next

        Return descriptorValues.ToArray()
    End Function

    Private Sub SaveKnownFaceDescriptorRecord(personName As String, savedImage As UploadedImageInfo, descriptorValues As Double())
        Dim normalizedPersonName As String = NormalizePersonDisplayName(personName)

        If String.IsNullOrWhiteSpace(normalizedPersonName) Then
            Throw New ApplicationException("ไม่พบชื่อบุคคลสำหรับบันทึกตัวเลขอ้างอิงใบหน้า")
        End If

        If savedImage Is Nothing OrElse String.IsNullOrWhiteSpace(savedImage.OriginalFileName) Then
            Throw New ApplicationException("ไม่พบไฟล์รูปภาพสำหรับบันทึกตัวเลขอ้างอิงใบหน้า")
        End If

        If descriptorValues Is Nothing OrElse descriptorValues.Length <> FaceDescriptorLength Then
            Throw New ApplicationException("ไม่พบตัวเลขอ้างอิงใบหน้าที่ครบถ้วน")
        End If

        FaceDescriptorStore.SaveDescriptor(Context, normalizedPersonName, savedImage.OriginalFileName, descriptorValues)
    End Sub

    Private Function SaveRecognizedFaceCapture(personName As String, snapshotDataUri As String) As UploadedImageInfo
        If String.IsNullOrWhiteSpace(personName) Then
            Throw New ApplicationException("ไม่พบชื่อบุคคลที่ตรวจพบจาก face scanner")
        End If

        If String.IsNullOrWhiteSpace(snapshotDataUri) Then
            Throw New ApplicationException("ไม่พบภาพใบหน้าที่ตรวจพบสำหรับใช้บันทึก")
        End If

        Dim personFolderVirtualPath As String = GetRecognizedFacePersonFolderVirtualPath(personName)
        EnsurePersonDisplayName(personFolderVirtualPath, personName)

        Return SaveFaceSnapshotData(snapshotDataUri, personFolderVirtualPath)
    End Function

    Private Function SendLineMessages() As String
        Dim messageText As String = txtMessage.Text.Trim()
        Dim originalImageUrl As String = txtImageUrl.Text.Trim()
        Dim previewImageUrl As String = txtPreviewImageUrl.Text.Trim()
        Dim selectedUpload As FileUpload = GetSelectedUpload()

        If selectedUpload IsNot Nothing Then
            Dim uploadedImage As UploadedImageInfo = SaveUploadedImage(selectedUpload)
            originalImageUrl = BuildAbsoluteImageUrl(uploadedImage.OriginalVirtualPath, txtPublicBaseUrl.Text.Trim())
            previewImageUrl = BuildAbsoluteImageUrl(uploadedImage.PreviewVirtualPath, txtPublicBaseUrl.Text.Trim())
            txtImageUrl.Text = originalImageUrl
            txtPreviewImageUrl.Text = previewImageUrl
            litImageInfo.Text = BuildSavedImageInfoHtml(uploadedImage, originalImageUrl, previewImageUrl)
        ElseIf Not String.IsNullOrWhiteSpace(originalImageUrl) AndAlso String.IsNullOrWhiteSpace(previewImageUrl) Then
            previewImageUrl = originalImageUrl
        End If

        If String.IsNullOrWhiteSpace(messageText) AndAlso String.IsNullOrWhiteSpace(originalImageUrl) Then
            Throw New ApplicationException("กรุณาระบุข้อความ หรือรูปภาพอย่างน้อย 1 อย่าง")
        End If

        Return SendLineBroadcastMessage(messageText, originalImageUrl, previewImageUrl)
    End Function

    Private Function SendRecognizedFaceLineNotification(personName As String, savedImage As UploadedImageInfo) As LineSendAttemptResult
        Dim result As New LineSendAttemptResult()

        If savedImage Is Nothing Then
            result.Message = "ไม่พบรูปภาพที่ตรวจพบสำหรับใช้ส่ง LINE"
            Return result
        End If

        Try
            result.PublicOriginalUrl = BuildAbsoluteImageUrl(savedImage.OriginalVirtualPath, txtPublicBaseUrl.Text.Trim())
            result.PublicPreviewUrl = BuildAbsoluteImageUrl(savedImage.PreviewVirtualPath, txtPublicBaseUrl.Text.Trim())
            txtImageUrl.Text = result.PublicOriginalUrl
            txtPreviewImageUrl.Text = result.PublicPreviewUrl
            result.WasAttempted = True
            result.Message = SendLineBroadcastMessage(
                BuildRecognizedFaceLineMessage(personName),
                result.PublicOriginalUrl,
                result.PublicPreviewUrl)
            result.IsSuccess = True
        Catch ex As Exception
            result.Message = ex.Message
        End Try

        Return result
    End Function

    Private Function BuildRecognizedFaceLineMessage(personName As String) As String
        Dim configuredMessage As String = txtMessage.Text.Trim()

        If configuredMessage.Equals(DefaultBroadcastMessageText, StringComparison.Ordinal) Then
            configuredMessage = String.Empty
        End If

        Dim messageBuilder As New StringBuilder()

        If Not String.IsNullOrWhiteSpace(configuredMessage) Then
            messageBuilder.AppendLine(configuredMessage)
        Else
            messageBuilder.AppendLine(DefaultRecognizedFaceNotificationText)
        End If

        messageBuilder.AppendLine("ตรวจพบบุคคล: " & personName)
        messageBuilder.Append("เวลา: " & DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"))
        Return messageBuilder.ToString().Trim()
    End Function

    Private Function SendLineBroadcastMessage(messageText As String, originalImageUrl As String, previewImageUrl As String) As String
        EnsureLineTlsConfiguration()
        Dim accessToken As String = GetAccessToken()
        Dim messages As New List(Of LineMessagePayload)()

        If Not String.IsNullOrWhiteSpace(messageText) Then
            messages.Add(New LineMessagePayload With {
                .type = "text",
                .text = messageText
            })
        End If

        If Not String.IsNullOrWhiteSpace(originalImageUrl) Then
            ValidateLineImageUrl(originalImageUrl, "Original Image URL")
            ValidateLineImageUrl(previewImageUrl, "Preview Image URL")

            messages.Add(New LineMessagePayload With {
                .type = "image",
                .originalContentUrl = originalImageUrl,
                .previewImageUrl = previewImageUrl
            })
        End If

        Dim payload As New LineBroadcastRequest With {.messages = messages}
        Dim serializer As New JavaScriptSerializer()
        Dim jsonPayload As String = serializer.Serialize(payload)
        Dim data As Byte() = Encoding.UTF8.GetBytes(jsonPayload)

        Dim request = CType(HttpWebRequest.Create(BroadcastEndpoint), HttpWebRequest)
        With request
            .Method = "POST"
            .ContentType = "application/json"
            .ContentLength = data.Length
            .Headers.Add("Authorization", "Bearer " & accessToken)
            .KeepAlive = False
            .ProtocolVersion = HttpVersion.Version11
        End With

        Try
            Using stream As Stream = request.GetRequestStream()
                stream.Write(data, 0, data.Length)
            End Using

            Using response As HttpWebResponse = CType(request.GetResponse(), HttpWebResponse)
                Using reader As New StreamReader(response.GetResponseStream())
                    Dim responseText As String = reader.ReadToEnd()
                    Return String.Format("ส่งสำเร็จ HTTP {0}{1}", CInt(response.StatusCode), BuildResponseSuffix(responseText))
                End Using
            End Using
        Catch ex As WebException
            Dim apiError As String = ReadResponseBody(ex.Response)
            Throw New ApplicationException("เกิดข้อผิดพลาดจาก LINE API: " & ex.Message & BuildResponseSuffix(apiError), ex)
        End Try
    End Function

    Private Sub EnsureLineTlsConfiguration()
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12
        ServicePointManager.Expect100Continue = False
    End Sub

    Private Function SaveImageForStorage() As UploadedImageInfo
        Dim selectedUpload As FileUpload = GetSelectedUpload()

        If selectedUpload IsNot Nothing Then
            Return SaveUploadedImage(selectedUpload)
        End If

        If Not String.IsNullOrWhiteSpace(txtImageUrl.Text.Trim()) Then
            Return SaveImageFromRemoteUrl(txtImageUrl.Text.Trim())
        End If

        Throw New ApplicationException("กรุณาเลือกรูปจากไฟล์, กล้อง หรือกรอก URL รูปภาพก่อนบันทึก")
    End Function

    Private Function GetSelectedUpload() As FileUpload
        If fuScanImage IsNot Nothing AndAlso fuScanImage.HasFile Then
            Return fuScanImage
        End If

        If fuImage IsNot Nothing AndAlso fuImage.HasFile Then
            Return fuImage
        End If

        Return Nothing
    End Function

    Private Function GetAccessToken() As String
        Dim configuredToken As String = ConfigurationManager.AppSettings("LineChannelAccessToken")

        If Not String.IsNullOrWhiteSpace(configuredToken) Then
            Return configuredToken.Trim()
        End If

        If Not String.IsNullOrWhiteSpace(DefaultAccessToken) Then
            Return DefaultAccessToken.Trim()
        End If

        Throw New ApplicationException("ไม่พบ Channel Access Token")
    End Function

    Private Function SaveUploadedImage(fileUpload As FileUpload, Optional folderVirtualPath As String = UploadFolderVirtualPath) As UploadedImageInfo
        Dim extension As String = NormalizeImageExtension(Path.GetExtension(fileUpload.FileName))

        If String.IsNullOrWhiteSpace(extension) Then
            Throw New ApplicationException("รองรับเฉพาะไฟล์ .jpg, .jpeg และ .png เท่านั้น")
        End If

        If fileUpload.PostedFile.ContentLength <= 0 Then
            Throw New ApplicationException("ไม่พบข้อมูลรูปภาพที่อัปโหลด")
        End If

        If fileUpload.PostedFile.ContentLength > MaxLineImageBytes Then
            Throw New ApplicationException("รูปต้นฉบับต้องมีขนาดไม่เกิน 10 MB ตามข้อกำหนดของ LINE")
        End If

        Dim imageInfo As UploadedImageInfo = CreateImageStoragePaths(extension, folderVirtualPath)
        fileUpload.SaveAs(imageInfo.OriginalPhysicalPath)
        CreatePreviewImage(imageInfo.OriginalPhysicalPath, imageInfo.PreviewPhysicalPath)

        Return imageInfo
    End Function

    Private Function SaveImageFromRemoteUrl(imageUrl As String, Optional folderVirtualPath As String = UploadFolderVirtualPath) As UploadedImageInfo
        Dim sourceUri As Uri = Nothing

        If Not Uri.TryCreate(imageUrl, UriKind.Absolute, sourceUri) Then
            Throw New ApplicationException("Image URL ไม่ถูกต้อง")
        End If

        Dim request = CType(HttpWebRequest.Create(sourceUri), HttpWebRequest)
        request.Method = "GET"

        Using response As HttpWebResponse = CType(request.GetResponse(), HttpWebResponse)
            Dim extension As String = ResolveExtensionFromResponse(sourceUri, response.ContentType)
            Dim imageInfo As UploadedImageInfo = CreateImageStoragePaths(extension, folderVirtualPath)

            Using responseStream As Stream = response.GetResponseStream()
                If responseStream Is Nothing Then
                    Throw New ApplicationException("ไม่สามารถอ่านข้อมูลรูปภาพจาก URL ได้")
                End If

                Using outputStream As New FileStream(imageInfo.OriginalPhysicalPath, FileMode.Create, FileAccess.Write)
                    responseStream.CopyTo(outputStream)
                End Using
            End Using

            Dim fileSize As Long = New FileInfo(imageInfo.OriginalPhysicalPath).Length

            If fileSize <= 0 Then
                Throw New ApplicationException("รูปภาพจาก URL ไม่มีข้อมูล")
            End If

            If fileSize > MaxLineImageBytes Then
                File.Delete(imageInfo.OriginalPhysicalPath)
                Throw New ApplicationException("รูปภาพจาก URL ต้องมีขนาดไม่เกิน 10 MB")
            End If

            CreatePreviewImage(imageInfo.OriginalPhysicalPath, imageInfo.PreviewPhysicalPath)
            Return imageInfo
        End Using
    End Function

    Private Function SaveFaceSnapshotData(snapshotDataUri As String, Optional folderVirtualPath As String = UploadFolderVirtualPath) As UploadedImageInfo
        If String.IsNullOrWhiteSpace(snapshotDataUri) Then
            Throw New ApplicationException("ยังไม่มี snapshot จาก face scanner กรุณาจับภาพก่อนบันทึก")
        End If

        Dim base64Payload As String = ExtractBase64Payload(snapshotDataUri)
        Dim imageBytes As Byte()

        Try
            imageBytes = Convert.FromBase64String(base64Payload)
        Catch ex As FormatException
            Throw New ApplicationException("ข้อมูล snapshot ของ face scanner ไม่ถูกต้อง", ex)
        End Try

        If imageBytes Is Nothing OrElse imageBytes.Length = 0 Then
            Throw New ApplicationException("snapshot ของ face scanner ไม่มีข้อมูลรูปภาพ")
        End If

        If imageBytes.Length > MaxLineImageBytes Then
            Throw New ApplicationException("snapshot ของ face scanner ต้องมีขนาดไม่เกิน 10 MB")
        End If

        Dim imageInfo As UploadedImageInfo = CreateImageStoragePaths(".jpg", folderVirtualPath)
        File.WriteAllBytes(imageInfo.OriginalPhysicalPath, imageBytes)
        CreatePreviewImage(imageInfo.OriginalPhysicalPath, imageInfo.PreviewPhysicalPath)

        Return imageInfo
    End Function

    Private Function ExtractBase64Payload(dataUri As String) As String
        Dim trimmedValue As String = dataUri.Trim()

        If Not trimmedValue.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase) Then
            Throw New ApplicationException("รูปภาพจาก face scanner ไม่ได้อยู่ในรูปแบบ data URI ที่รองรับ")
        End If

        Dim marker As String = "base64,"
        Dim markerIndex As Integer = trimmedValue.IndexOf(marker, StringComparison.OrdinalIgnoreCase)

        If markerIndex < 0 Then
            Throw New ApplicationException("รูปภาพจาก face scanner ไม่มีข้อมูล base64")
        End If

        Return trimmedValue.Substring(markerIndex + marker.Length)
    End Function

    Private Function CreateImageStoragePaths(extension As String, Optional folderVirtualPath As String = UploadFolderVirtualPath) As UploadedImageInfo
        Dim normalizedExtension As String = NormalizeImageExtension(extension)

        If String.IsNullOrWhiteSpace(normalizedExtension) Then
            Throw New ApplicationException("ไม่พบชนิดไฟล์รูปภาพที่รองรับ")
        End If

        Dim uploadFolderPhysicalPath As String = Server.MapPath(folderVirtualPath)
        Directory.CreateDirectory(uploadFolderPhysicalPath)

        Dim fileToken As String = Guid.NewGuid().ToString("N")
        Dim originalFileName As String = fileToken & normalizedExtension
        Dim previewFileName As String = fileToken & PreviewSuffix
        Dim originalVirtualPath As String = folderVirtualPath & originalFileName
        Dim previewVirtualPath As String = folderVirtualPath & previewFileName

        Return New UploadedImageInfo With {
            .OriginalFileName = originalFileName,
            .PreviewFileName = previewFileName,
            .OriginalVirtualPath = originalVirtualPath,
            .PreviewVirtualPath = previewVirtualPath,
            .OriginalRelativeUrl = ResolveUrl(originalVirtualPath),
            .PreviewRelativeUrl = ResolveUrl(previewVirtualPath),
            .OriginalPhysicalPath = Path.Combine(uploadFolderPhysicalPath, originalFileName),
            .PreviewPhysicalPath = Path.Combine(uploadFolderPhysicalPath, previewFileName)
        }
    End Function

    Private Function NormalizePersonDisplayName(personName As String) As String
        Dim trimmedName As String = If(personName, String.Empty).Trim()

        If String.IsNullOrWhiteSpace(trimmedName) Then
            Return String.Empty
        End If

        Dim builder As New StringBuilder()
        Dim hasWhitespace As Boolean = False

        For Each currentChar As Char In trimmedName
            If Char.IsWhiteSpace(currentChar) Then
                If Not hasWhitespace Then
                    builder.Append(" "c)
                    hasWhitespace = True
                End If
            Else
                builder.Append(currentChar)
                hasWhitespace = False
            End If
        Next

        Return builder.ToString().Trim()
    End Function

    Private Function BuildKnownFaceFolderKey(personName As String) As String
        Dim normalizedName As String = NormalizePersonDisplayName(personName)

        If String.IsNullOrWhiteSpace(normalizedName) Then
            Throw New ApplicationException("ไม่สามารถสร้างชื่อโฟลเดอร์สำหรับใบหน้าได้")
        End If

        Dim invalidChars As Char() = Path.GetInvalidFileNameChars()
        Dim builder As New StringBuilder()

        For Each currentChar As Char In normalizedName
            If invalidChars.Contains(currentChar) Then
                builder.Append("_"c)
            ElseIf Char.IsWhiteSpace(currentChar) Then
                builder.Append("_"c)
            Else
                builder.Append(currentChar)
            End If
        Next

        Dim folderKey As String = builder.ToString().Trim("_"c)

        If String.IsNullOrWhiteSpace(folderKey) Then
            folderKey = "known-face"
        End If

        Return folderKey
    End Function

    Private Function GetPersonFolderVirtualPath(rootVirtualPath As String, personName As String) As String
        Dim folderKey As String = BuildKnownFaceFolderKey(personName)
        Return rootVirtualPath & folderKey & "/"
    End Function

    Private Function GetKnownFacePersonFolderVirtualPath(personName As String) As String
        Return GetPersonFolderVirtualPath(KnownFaceFolderVirtualPath, personName)
    End Function

    Private Function GetRecognizedFacePersonFolderVirtualPath(personName As String) As String
        Return GetPersonFolderVirtualPath(RecognizedFaceFolderVirtualPath, personName)
    End Function

    Private Sub EnsurePersonDisplayName(personFolderVirtualPath As String, personName As String)
        Dim normalizedName As String = NormalizePersonDisplayName(personName)

        If String.IsNullOrWhiteSpace(normalizedName) Then
            Throw New ApplicationException("กรุณาระบุชื่อบุคคลก่อนลงทะเบียนใบหน้า")
        End If

        Dim personFolderPhysicalPath As String = Server.MapPath(personFolderVirtualPath)
        Directory.CreateDirectory(personFolderPhysicalPath)

        Dim displayNameFilePath As String = Path.Combine(personFolderPhysicalPath, KnownFaceDisplayNameFileName)
        File.WriteAllText(displayNameFilePath, normalizedName, Encoding.UTF8)
    End Sub

    Private Function ReadPersonDisplayName(personDirectory As DirectoryInfo) As String
        If personDirectory Is Nothing Then
            Return String.Empty
        End If

        Dim displayNameFilePath As String = Path.Combine(personDirectory.FullName, KnownFaceDisplayNameFileName)

        If File.Exists(displayNameFilePath) Then
            Dim displayName As String = NormalizePersonDisplayName(File.ReadAllText(displayNameFilePath, Encoding.UTF8))

            If Not String.IsNullOrWhiteSpace(displayName) Then
                Return displayName
            End If
        End If

        Return personDirectory.Name.Replace("_", " ").Trim()
    End Function

    Private Function BuildKnownFaceDescriptorKey(personName As String, fileName As String) As String
        Return NormalizePersonDisplayName(personName).ToLowerInvariant() & "|" & If(fileName, String.Empty).Trim().ToLowerInvariant()
    End Function

    Private Function GetKnownFaceDescriptorDatabasePhysicalPath() As String
        Dim databasePhysicalPath As String = Server.MapPath(KnownFaceDescriptorDatabaseVirtualPath)
        Dim databaseDirectory As String = Path.GetDirectoryName(databasePhysicalPath)

        If Not String.IsNullOrWhiteSpace(databaseDirectory) Then
            Directory.CreateDirectory(databaseDirectory)
        End If

        Return databasePhysicalPath
    End Function

    Private Function LoadKnownFaceDescriptorDatabase() As KnownFaceDescriptorDatabase
        Dim databasePhysicalPath As String = GetKnownFaceDescriptorDatabasePhysicalPath()

        If Not File.Exists(databasePhysicalPath) Then
            Return New KnownFaceDescriptorDatabase With {
                .samples = New List(Of KnownFaceDescriptorSample)(),
                .version = 1
            }
        End If

        Dim rawJson As String = File.ReadAllText(databasePhysicalPath, Encoding.UTF8)

        If String.IsNullOrWhiteSpace(rawJson) Then
            Return New KnownFaceDescriptorDatabase With {
                .samples = New List(Of KnownFaceDescriptorSample)(),
                .version = 1
            }
        End If

        Dim serializer As JavaScriptSerializer = CreateJsonSerializer()
        Dim descriptorDatabase As KnownFaceDescriptorDatabase = serializer.Deserialize(Of KnownFaceDescriptorDatabase)(rawJson)

        If descriptorDatabase Is Nothing Then
            descriptorDatabase = New KnownFaceDescriptorDatabase()
        End If

        If descriptorDatabase.samples Is Nothing Then
            descriptorDatabase.samples = New List(Of KnownFaceDescriptorSample)()
        End If

        If descriptorDatabase.version <= 0 Then
            descriptorDatabase.version = 1
        End If

        Return descriptorDatabase
    End Function

    Private Sub SaveKnownFaceDescriptorDatabase(descriptorDatabase As KnownFaceDescriptorDatabase)
        If descriptorDatabase Is Nothing Then
            Throw New ApplicationException("ไม่พบฐานข้อมูลตัวเลขอ้างอิงใบหน้า")
        End If

        If descriptorDatabase.samples Is Nothing Then
            descriptorDatabase.samples = New List(Of KnownFaceDescriptorSample)()
        End If

        descriptorDatabase.version = 1

        Dim serializer As JavaScriptSerializer = CreateJsonSerializer()
        Dim databasePhysicalPath As String = GetKnownFaceDescriptorDatabasePhysicalPath()
        Dim rawJson As String = serializer.Serialize(descriptorDatabase)

        File.WriteAllText(databasePhysicalPath, rawJson, Encoding.UTF8)
    End Sub

    Private Function GetKnownFaceRecognitionEntries(knownFaceReferences As List(Of KnownFaceReference)) As List(Of KnownFaceRecognitionEntry)
        Dim recognitionEntries As New List(Of KnownFaceRecognitionEntry)()
        Dim descriptorLookup As New Dictionary(Of String, KnownFaceDescriptorSample)(StringComparer.OrdinalIgnoreCase)

        SyncLock DescriptorDatabaseSyncRoot
            Dim descriptorDatabase As KnownFaceDescriptorDatabase = LoadKnownFaceDescriptorDatabase()

            For Each descriptorSample As KnownFaceDescriptorSample In descriptorDatabase.samples
                If descriptorSample Is Nothing OrElse String.IsNullOrWhiteSpace(descriptorSample.label) OrElse String.IsNullOrWhiteSpace(descriptorSample.fileName) Then
                    Continue For
                End If

                descriptorLookup(BuildKnownFaceDescriptorKey(descriptorSample.label, descriptorSample.fileName)) = descriptorSample
            Next
        End SyncLock

        For Each knownFaceReference As KnownFaceReference In knownFaceReferences
            Dim descriptorSample As KnownFaceDescriptorSample = Nothing
            descriptorLookup.TryGetValue(BuildKnownFaceDescriptorKey(knownFaceReference.label, knownFaceReference.fileName), descriptorSample)

            Dim descriptorValues As Double() = Nothing

            If descriptorSample IsNot Nothing AndAlso descriptorSample.descriptor IsNot Nothing AndAlso descriptorSample.descriptor.Count = FaceDescriptorLength Then
                descriptorValues = descriptorSample.descriptor.ToArray()
            End If

            recognitionEntries.Add(New KnownFaceRecognitionEntry With {
                .descriptor = descriptorValues,
                .fileName = knownFaceReference.fileName,
                .imageUrl = knownFaceReference.imageUrl,
                .label = knownFaceReference.label,
                .previewUrl = knownFaceReference.previewUrl
            })
        Next

        Return recognitionEntries
    End Function

    Private Sub DeleteSavedImageFiles(savedImage As UploadedImageInfo)
        If savedImage Is Nothing Then
            Return
        End If

        Try
            If Not String.IsNullOrWhiteSpace(savedImage.OriginalPhysicalPath) AndAlso File.Exists(savedImage.OriginalPhysicalPath) Then
                File.Delete(savedImage.OriginalPhysicalPath)
            End If
        Catch
        End Try

        Try
            If Not String.IsNullOrWhiteSpace(savedImage.PreviewPhysicalPath) AndAlso File.Exists(savedImage.PreviewPhysicalPath) Then
                File.Delete(savedImage.PreviewPhysicalPath)
            End If
        Catch
        End Try
    End Sub

    Private Sub CreatePreviewImage(originalPhysicalPath As String, previewPhysicalPath As String)
        Using originalImage As Image = Image.FromFile(originalPhysicalPath)
            Dim longestSide As Integer = Math.Max(originalImage.Width, originalImage.Height)
            Dim currentLongEdge As Integer = Math.Min(1024, longestSide)

            If currentLongEdge <= 0 Then
                Throw New ApplicationException("ไม่สามารถสร้าง preview image ได้")
            End If

            Do While currentLongEdge > 0
                For Each quality As Long In PreviewQualityLevels
                    Using bitmap As Bitmap = ResizeImage(originalImage, currentLongEdge)
                        If SaveJpegWithinSize(bitmap, previewPhysicalPath, quality, MaxPreviewBytes) Then
                            Exit Sub
                        End If
                    End Using
                Next

                If currentLongEdge = 1 Then
                    Exit Do
                End If

                Dim nextLongEdge As Integer = CInt(Math.Floor(currentLongEdge * 0.8))

                If nextLongEdge >= currentLongEdge Then
                    nextLongEdge = currentLongEdge - 1
                End If

                currentLongEdge = Math.Max(1, nextLongEdge)
            Loop
        End Using

        Throw New ApplicationException("ไม่สามารถย่อรูป preview ให้ต่ำกว่า 1 MB ได้")
    End Sub

    Private Function ResizeImage(sourceImage As Image, maxLongEdge As Integer) As Bitmap
        Dim sourceWidth As Integer = sourceImage.Width
        Dim sourceHeight As Integer = sourceImage.Height
        Dim longestSide As Integer = Math.Max(sourceWidth, sourceHeight)
        Dim scale As Double = 1

        If longestSide > maxLongEdge Then
            scale = CDbl(maxLongEdge) / CDbl(longestSide)
        End If

        Dim targetWidth As Integer = Math.Max(1, CInt(Math.Round(sourceWidth * scale)))
        Dim targetHeight As Integer = Math.Max(1, CInt(Math.Round(sourceHeight * scale)))
        Dim resizedImage As New Bitmap(targetWidth, targetHeight)

        resizedImage.SetResolution(sourceImage.HorizontalResolution, sourceImage.VerticalResolution)

        Using graphics As Graphics = Graphics.FromImage(resizedImage)
            graphics.CompositingQuality = CompositingQuality.HighQuality
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic
            graphics.SmoothingMode = SmoothingMode.HighQuality
            graphics.DrawImage(sourceImage, 0, 0, targetWidth, targetHeight)
        End Using

        Return resizedImage
    End Function

    Private Function SaveJpegWithinSize(image As Bitmap, outputPath As String, quality As Long, maxBytes As Integer) As Boolean
        Dim jpegCodec As ImageCodecInfo = ImageCodecInfo.GetImageEncoders().FirstOrDefault(Function(codec) codec.MimeType = "image/jpeg")

        If jpegCodec Is Nothing Then
            Throw New ApplicationException("ไม่พบ JPEG encoder สำหรับสร้าง preview image")
        End If

        Using stream As New MemoryStream()
            Using encoderParameters As New EncoderParameters(1)
                encoderParameters.Param(0) = New EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality)
                image.Save(stream, jpegCodec, encoderParameters)
            End Using

            If stream.Length > maxBytes Then
                Return False
            End If

            File.WriteAllBytes(outputPath, stream.ToArray())
        End Using

        Return True
    End Function

    Private Function NormalizeImageExtension(extension As String) As String
        If String.IsNullOrWhiteSpace(extension) Then
            Return String.Empty
        End If

        Dim normalizedExtension As String = extension.Trim().ToLowerInvariant()

        If normalizedExtension = ".jpg" OrElse normalizedExtension = ".jpeg" OrElse normalizedExtension = ".png" Then
            Return normalizedExtension
        End If

        Return String.Empty
    End Function

    Private Function ResolveExtensionFromResponse(sourceUri As Uri, contentType As String) As String
        Dim extension As String = NormalizeImageExtension(Path.GetExtension(sourceUri.AbsolutePath))

        If Not String.IsNullOrWhiteSpace(extension) Then
            Return extension
        End If

        Dim normalizedContentType As String = If(contentType, String.Empty).ToLowerInvariant()

        If normalizedContentType.Contains("png") Then
            Return ".png"
        End If

        If normalizedContentType.Contains("jpeg") OrElse normalizedContentType.Contains("jpg") Then
            Return ".jpg"
        End If

        Throw New ApplicationException("ไม่สามารถระบุชนิดไฟล์รูปภาพจาก URL ได้ รองรับเฉพาะ JPG / JPEG / PNG")
    End Function

    Private Function BuildAbsoluteImageUrl(virtualPath As String, publicBaseUrl As String) As String
        Dim baseUri As Uri = ResolveBaseUri(publicBaseUrl)
        Dim applicationRelativePath As String = ResolveUrl(virtualPath)
        Dim absoluteImageUri As New Uri(baseUri, applicationRelativePath)

        If absoluteImageUri.Scheme <> Uri.UriSchemeHttps Then
            Throw New ApplicationException("LINE รับเฉพาะ URL รูปภาพแบบ HTTPS เท่านั้น")
        End If

        If absoluteImageUri.IsLoopback OrElse absoluteImageUri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) Then
            Throw New ApplicationException("URL รูปภาพต้องเข้าถึงได้จากภายนอก LINE จึงไม่สามารถใช้ localhost ได้ กรุณาระบุ Public Base URL ที่เป็น HTTPS")
        End If

        Return absoluteImageUri.AbsoluteUri
    End Function

    Private Function ResolveBaseUri(publicBaseUrl As String) As Uri
        If Not String.IsNullOrWhiteSpace(publicBaseUrl) Then
            Dim normalizedBaseUrl As String = publicBaseUrl.Trim()

            If Not normalizedBaseUrl.EndsWith("/", StringComparison.Ordinal) Then
                normalizedBaseUrl &= "/"
            End If

            Dim parsedBaseUri As Uri = Nothing

            If Not Uri.TryCreate(normalizedBaseUrl, UriKind.Absolute, parsedBaseUri) Then
                Throw New ApplicationException("Public Base URL ไม่ถูกต้อง")
            End If

            Return parsedBaseUri
        End If

        Return New Uri(Request.Url.GetLeftPart(UriPartial.Authority))
    End Function

    Private Function TryGetPublicImageUrls(savedImage As UploadedImageInfo, publicBaseUrl As String, ByRef originalUrl As String, ByRef previewUrl As String) As Boolean
        originalUrl = String.Empty
        previewUrl = String.Empty

        Try
            originalUrl = BuildAbsoluteImageUrl(savedImage.OriginalVirtualPath, publicBaseUrl)
            previewUrl = BuildAbsoluteImageUrl(savedImage.PreviewVirtualPath, publicBaseUrl)
            Return True
        Catch
            Return False
        End Try
    End Function

    Private Function BuildSavedImageInfoHtml(savedImage As UploadedImageInfo, publicOriginalUrl As String, publicPreviewUrl As String) As String
        Dim html As New StringBuilder()
        html.Append("<div style='margin-top: 16px; padding: 14px 16px; border-radius: 12px; background-color: #eff6ff; color: #1d4ed8;'>")
        html.Append("<div><strong>Saved Original Path:</strong> " & Server.HtmlEncode(savedImage.OriginalRelativeUrl) & "</div>")
        html.Append("<div style='margin-top: 4px;'><strong>Saved Preview Path:</strong> " & Server.HtmlEncode(savedImage.PreviewRelativeUrl) & "</div>")

        If Not String.IsNullOrWhiteSpace(publicOriginalUrl) AndAlso Not String.IsNullOrWhiteSpace(publicPreviewUrl) Then
            html.Append("<div style='margin-top: 8px;'><strong>Original URL:</strong> " & Server.HtmlEncode(publicOriginalUrl) & "</div>")
            html.Append("<div style='margin-top: 4px;'><strong>Preview URL:</strong> " & Server.HtmlEncode(publicPreviewUrl) & "</div>")
        Else
            html.Append("<div style='margin-top: 8px; color: #1e40af;'>บันทึกรูปไว้แล้ว แต่ยังไม่มี Public HTTPS URL สำหรับใช้ส่งผ่าน LINE</div>")
        End If

        html.Append("<div style='margin-top: 12px;'><img src='" & HttpUtility.HtmlAttributeEncode(savedImage.PreviewRelativeUrl) & "' alt='preview' style='max-width: 240px; width: 100%; border-radius: 10px; border: 1px solid #bfdbfe;' /></div>")
        html.Append("</div>")
        Return html.ToString()
    End Function

    Private Function BuildKnownFaceInfoHtml(personName As String, savedImage As UploadedImageInfo) As String
        Dim html As New StringBuilder()
        html.Append("<div style='margin-top: 16px; padding: 14px 16px; border-radius: 12px; background-color: #ecfeff; color: #155e75;'>")
        html.Append("<div><strong>ลงทะเบียนใบหน้า:</strong> " & Server.HtmlEncode(personName) & "</div>")
        html.Append("<div style='margin-top: 4px;'><strong>Saved Original Path:</strong> " & Server.HtmlEncode(savedImage.OriginalRelativeUrl) & "</div>")
        html.Append("<div style='margin-top: 4px;'><strong>Saved Preview Path:</strong> " & Server.HtmlEncode(savedImage.PreviewRelativeUrl) & "</div>")
        html.Append("<div style='margin-top: 12px;'><img src='" & HttpUtility.HtmlAttributeEncode(savedImage.PreviewRelativeUrl) & "' alt='known face preview' style='max-width: 240px; width: 100%; border-radius: 10px; border: 1px solid #a5f3fc;' /></div>")
        html.Append("</div>")
        Return html.ToString()
    End Function

    Private Function BuildRecognizedFaceInfoHtml(
        personName As String,
        savedImage As UploadedImageInfo,
        publicOriginalUrl As String,
        publicPreviewUrl As String,
        lineNotificationResult As LineSendAttemptResult) As String
        Dim html As New StringBuilder()
        html.Append("<div style='margin-top: 16px; padding: 14px 16px; border-radius: 12px; background-color: #f0fdf4; color: #166534;'>")
        html.Append("<div><strong>ตรวจพบบุคคล:</strong> " & Server.HtmlEncode(personName) & "</div>")
        html.Append("<div style='margin-top: 4px;'><strong>Saved Original Path:</strong> " & Server.HtmlEncode(savedImage.OriginalRelativeUrl) & "</div>")
        html.Append("<div style='margin-top: 4px;'><strong>Saved Preview Path:</strong> " & Server.HtmlEncode(savedImage.PreviewRelativeUrl) & "</div>")

        If Not String.IsNullOrWhiteSpace(publicOriginalUrl) AndAlso Not String.IsNullOrWhiteSpace(publicPreviewUrl) Then
            html.Append("<div style='margin-top: 8px;'><strong>Original URL:</strong> " & Server.HtmlEncode(publicOriginalUrl) & "</div>")
            html.Append("<div style='margin-top: 4px;'><strong>Preview URL:</strong> " & Server.HtmlEncode(publicPreviewUrl) & "</div>")
        Else
            html.Append("<div style='margin-top: 8px; color: #1e40af;'>บันทึกรูปไว้แล้ว แต่ยังไม่มี Public HTTPS URL สำหรับใช้ส่งผ่าน LINE</div>")
        End If

        If lineNotificationResult IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(lineNotificationResult.Message) Then
            If lineNotificationResult.IsSuccess Then
                html.Append("<div style='margin-top: 8px; color: #166534;'><strong>LINE:</strong> " & Server.HtmlEncode(lineNotificationResult.Message) & "</div>")
            Else
                html.Append("<div style='margin-top: 8px; color: #9a3412;'><strong>LINE:</strong> " & Server.HtmlEncode(lineNotificationResult.Message) & "</div>")
            End If
        End If

        html.Append("<div style='margin-top: 12px;'><img src='" & HttpUtility.HtmlAttributeEncode(savedImage.PreviewRelativeUrl) & "' alt='recognized face preview' style='max-width: 240px; width: 100%; border-radius: 10px; border: 1px solid #86efac;' /></div>")
        html.Append("</div>")
        Return html.ToString()
    End Function

    Private Sub BindKnownFaces()
        Dim knownFaceReferences As List(Of KnownFaceReference) = GetKnownFaceReferences()
        Dim serializer As JavaScriptSerializer = CreateJsonSerializer()
        hfKnownFacesCatalog.Value = serializer.Serialize(FaceDescriptorStore.GetSummary(Context))

        If knownFaceReferences.Count = 0 Then
            litKnownFaces.Text = "<div style='padding: 16px; border: 1px dashed #cbd5e1; border-radius: 12px; color: #64748b;'>ยังไม่มีใบหน้าที่ลงทะเบียนไว้</div>"
            Return
        End If

        Dim html As New StringBuilder()
        html.Append("<div style='display: grid; grid-template-columns: repeat(auto-fit, minmax(220px, 1fr)); gap: 16px;'>")

        For Each knownFace As KnownFaceReference In knownFaceReferences.Take(KnownFaceDisplayPageSize)
            html.Append("<div style='border: 1px solid #dbe4f0; border-radius: 14px; overflow: hidden; background-color: #ffffff;'>")
            html.Append("<div style='height: 180px; background-color: #e0f2fe; display: flex; align-items: center; justify-content: center;'>")
            html.Append("<img src='" & HttpUtility.HtmlAttributeEncode(knownFace.previewUrl) & "' alt='" & HttpUtility.HtmlAttributeEncode(knownFace.label) & "' style='max-width: 100%; max-height: 100%; object-fit: contain;' />")
            html.Append("</div>")
            html.Append("<div style='padding: 12px 14px;'>")
            html.Append("<div style='font-weight: 600; color: #111827; word-break: break-word;'>" & Server.HtmlEncode(knownFace.label) & "</div>")
            html.Append("<div style='margin-top: 6px; font-size: 13px; color: #6b7280;'>")
            html.Append("อ้างอิงไฟล์ " & Server.HtmlEncode(knownFace.fileName))
            html.Append("<br />อัปเดต " & Server.HtmlEncode(knownFace.lastUpdatedText))
            html.Append("</div>")
            html.Append("<div style='margin-top: 10px; font-size: 13px;'>")
            html.Append("<a href='" & HttpUtility.HtmlAttributeEncode(knownFace.imageUrl) & "' target='_blank' style='color: #2563eb; text-decoration: none;'>เปิดรูปอ้างอิง</a>")
            html.Append("</div>")
            html.Append("</div>")
            html.Append("</div>")
        Next

        html.Append("</div>")
        litKnownFaces.Text = html.ToString()
    End Sub

    Private Sub BindRecognizedFaces()
        Dim recognizedFaceReferences As List(Of RecognizedFaceReference) = GetRecognizedFaceReferences()

        If recognizedFaceReferences.Count = 0 Then
            litRecognizedFaces.Text = "<div style='padding: 16px; border: 1px dashed #cbd5e1; border-radius: 12px; color: #64748b;'>ยังไม่มีประวัติการตรวจพบใบหน้าอัตโนมัติ</div>"
            Return
        End If

        Dim html As New StringBuilder()
        html.Append("<div style='display: grid; grid-template-columns: repeat(auto-fit, minmax(220px, 1fr)); gap: 16px;'>")

        For Each recognizedFace As RecognizedFaceReference In recognizedFaceReferences.Take(RecognizedFaceDisplayPageSize)
            html.Append("<div style='border: 1px solid #dbe4f0; border-radius: 14px; overflow: hidden; background-color: #ffffff;'>")
            html.Append("<div style='height: 180px; background-color: #dcfce7; display: flex; align-items: center; justify-content: center;'>")
            html.Append("<img src='" & HttpUtility.HtmlAttributeEncode(recognizedFace.previewUrl) & "' alt='" & HttpUtility.HtmlAttributeEncode(recognizedFace.label) & "' style='max-width: 100%; max-height: 100%; object-fit: contain;' />")
            html.Append("</div>")
            html.Append("<div style='padding: 12px 14px;'>")
            html.Append("<div style='font-weight: 600; color: #111827; word-break: break-word;'>" & Server.HtmlEncode(recognizedFace.label) & "</div>")
            html.Append("<div style='margin-top: 6px; font-size: 13px; color: #6b7280;'>")
            html.Append("ตรวจพบเมื่อ " & Server.HtmlEncode(recognizedFace.lastUpdatedText))
            html.Append("<br />อ้างอิงไฟล์ " & Server.HtmlEncode(recognizedFace.fileName))
            html.Append("</div>")
            html.Append("<div style='margin-top: 10px; font-size: 13px;'>")
            html.Append("<a href='" & HttpUtility.HtmlAttributeEncode(recognizedFace.imageUrl) & "' target='_blank' style='color: #16a34a; text-decoration: none;'>เปิดภาพที่บันทึก</a>")
            html.Append("</div>")
            html.Append("</div>")
            html.Append("</div>")
        Next

        html.Append("</div>")
        litRecognizedFaces.Text = html.ToString()
    End Sub

    Private Function GetKnownFaceReferences() As List(Of KnownFaceReference)
        Dim knownFaceRootPhysicalPath As String = Server.MapPath(KnownFaceFolderVirtualPath)
        Dim knownFaceReferences As New List(Of KnownFaceReference)()

        If Not Directory.Exists(knownFaceRootPhysicalPath) Then
            Return knownFaceReferences
        End If

        Dim personDirectories As List(Of DirectoryInfo) =
            New DirectoryInfo(knownFaceRootPhysicalPath).
            GetDirectories().
            OrderByDescending(Function(directory) directory.LastWriteTime).
            ToList()

        For Each personDirectory As DirectoryInfo In personDirectories
            Dim displayName As String = ReadPersonDisplayName(personDirectory)
            Dim personFolderVirtualPath As String = KnownFaceFolderVirtualPath & personDirectory.Name & "/"
            Dim personSamples As List(Of FileInfo) =
                personDirectory.GetFiles().
                Where(Function(file) IsSavedOriginalImage(file)).
                OrderByDescending(Function(file) file.LastWriteTime).
                Take(KnownFaceSamplesPerPerson).
                ToList()

            For Each sampleFile As FileInfo In personSamples
                Dim previewFileName As String = Path.GetFileNameWithoutExtension(sampleFile.Name) & PreviewSuffix
                Dim previewPhysicalPath As String = Path.Combine(personDirectory.FullName, previewFileName)
                Dim previewVirtualPath As String = personFolderVirtualPath & previewFileName
                Dim originalVirtualPath As String = personFolderVirtualPath & sampleFile.Name

                knownFaceReferences.Add(New KnownFaceReference With {
                    .label = displayName,
                    .imageUrl = ResolveUrl(originalVirtualPath),
                    .previewUrl = If(File.Exists(previewPhysicalPath), ResolveUrl(previewVirtualPath), ResolveUrl(originalVirtualPath)),
                    .fileName = sampleFile.Name,
                    .lastUpdatedText = sampleFile.LastWriteTime.ToString("dd/MM/yyyy HH:mm")
                })
            Next
        Next

        Return knownFaceReferences
    End Function

    Private Function GetRecognizedFaceReferences() As List(Of RecognizedFaceReference)
        Dim recognizedFaceRootPhysicalPath As String = Server.MapPath(RecognizedFaceFolderVirtualPath)
        Dim recognizedFaceReferences As New List(Of RecognizedFaceReference)()

        If Not Directory.Exists(recognizedFaceRootPhysicalPath) Then
            Return recognizedFaceReferences
        End If

        Dim personDirectories As List(Of DirectoryInfo) =
            New DirectoryInfo(recognizedFaceRootPhysicalPath).
            GetDirectories().
            OrderByDescending(Function(directory) directory.LastWriteTime).
            ToList()

        For Each personDirectory As DirectoryInfo In personDirectories
            Dim displayName As String = ReadPersonDisplayName(personDirectory)
            Dim personFolderVirtualPath As String = RecognizedFaceFolderVirtualPath & personDirectory.Name & "/"
            Dim savedFaces As List(Of FileInfo) =
                personDirectory.GetFiles().
                Where(Function(file) IsSavedOriginalImage(file)).
                OrderByDescending(Function(file) file.LastWriteTime).
                ToList()

            For Each savedFace As FileInfo In savedFaces
                Dim previewFileName As String = Path.GetFileNameWithoutExtension(savedFace.Name) & PreviewSuffix
                Dim previewPhysicalPath As String = Path.Combine(personDirectory.FullName, previewFileName)
                Dim previewVirtualPath As String = personFolderVirtualPath & previewFileName
                Dim originalVirtualPath As String = personFolderVirtualPath & savedFace.Name

                recognizedFaceReferences.Add(New RecognizedFaceReference With {
                    .label = displayName,
                    .imageUrl = ResolveUrl(originalVirtualPath),
                    .previewUrl = If(File.Exists(previewPhysicalPath), ResolveUrl(previewVirtualPath), ResolveUrl(originalVirtualPath)),
                    .fileName = savedFace.Name,
                    .lastUpdatedText = savedFace.LastWriteTime.ToString("dd/MM/yyyy HH:mm:ss"),
                    .sortTicks = savedFace.LastWriteTime.Ticks
                })
            Next
        Next

        Return recognizedFaceReferences.
            OrderByDescending(Function(item) item.sortTicks).
            ToList()
    End Function

    Private Sub BindSavedImages()
        Dim uploadFolderPhysicalPath As String = Server.MapPath(UploadFolderVirtualPath)

        If Not Directory.Exists(uploadFolderPhysicalPath) Then
            litSavedImages.Text = "<div style='padding: 16px; border: 1px dashed #cbd5e1; border-radius: 12px; color: #64748b;'>ยังไม่มีรูปภาพที่บันทึกไว้</div>"
            Return
        End If

        Dim directoryInfo As New DirectoryInfo(uploadFolderPhysicalPath)
        Dim savedFiles As List(Of FileInfo) =
            directoryInfo.GetFiles().
            Where(Function(file) IsSavedOriginalImage(file)).
            OrderByDescending(Function(file) file.LastWriteTime).
            Take(SavedImagesPageSize).
            ToList()

        If savedFiles.Count = 0 Then
            litSavedImages.Text = "<div style='padding: 16px; border: 1px dashed #cbd5e1; border-radius: 12px; color: #64748b;'>ยังไม่มีรูปภาพที่บันทึกไว้</div>"
            Return
        End If

        Dim html As New StringBuilder()
        html.Append("<div style='display: grid; grid-template-columns: repeat(auto-fit, minmax(220px, 1fr)); gap: 16px;'>")

        For Each savedFile As FileInfo In savedFiles
            Dim previewFileName As String = Path.GetFileNameWithoutExtension(savedFile.Name) & PreviewSuffix
            Dim previewPhysicalPath As String = Path.Combine(uploadFolderPhysicalPath, previewFileName)
            Dim previewRelativeUrl As String = ResolveUrl(UploadFolderVirtualPath & previewFileName)
            Dim originalRelativeUrl As String = ResolveUrl(UploadFolderVirtualPath & savedFile.Name)
            Dim imageUrl As String = If(File.Exists(previewPhysicalPath), previewRelativeUrl, originalRelativeUrl)

            html.Append("<div style='border: 1px solid #dbe4f0; border-radius: 14px; overflow: hidden; background-color: #ffffff;'>")
            html.Append("<div style='height: 180px; background-color: #e5e7eb; display: flex; align-items: center; justify-content: center;'>")
            html.Append("<img src='" & HttpUtility.HtmlAttributeEncode(imageUrl) & "' alt='" & HttpUtility.HtmlAttributeEncode(savedFile.Name) & "' style='max-width: 100%; max-height: 100%; object-fit: contain;' />")
            html.Append("</div>")
            html.Append("<div style='padding: 12px 14px;'>")
            html.Append("<div style='font-weight: 600; color: #111827; word-break: break-word;'>" & Server.HtmlEncode(savedFile.Name) & "</div>")
            html.Append("<div style='margin-top: 6px; font-size: 13px; color: #6b7280;'>")
            html.Append("แก้ไขล่าสุด " & savedFile.LastWriteTime.ToString("dd/MM/yyyy HH:mm"))
            html.Append("<br />ขนาด " & FormatFileSize(savedFile.Length))
            html.Append("</div>")
            html.Append("<div style='margin-top: 10px; font-size: 13px;'>")
            html.Append("<a href='" & HttpUtility.HtmlAttributeEncode(originalRelativeUrl) & "' target='_blank' style='color: #2563eb; text-decoration: none;'>เปิดรูปต้นฉบับ</a>")
            html.Append("</div>")
            html.Append("</div>")
            html.Append("</div>")
        Next

        html.Append("</div>")
        litSavedImages.Text = html.ToString()
    End Sub

    Private Function IsSavedOriginalImage(file As FileInfo) As Boolean
        Dim extension As String = NormalizeImageExtension(file.Extension)

        If String.IsNullOrWhiteSpace(extension) Then
            Return False
        End If

        Return Not file.Name.EndsWith(PreviewSuffix, StringComparison.OrdinalIgnoreCase)
    End Function

    Private Function FormatFileSize(fileSizeInBytes As Long) As String
        If fileSizeInBytes >= 1024 * 1024 Then
            Return (fileSizeInBytes / 1024.0 / 1024.0).ToString("0.00") & " MB"
        End If

        Return (fileSizeInBytes / 1024.0).ToString("0.00") & " KB"
    End Function

    Private Sub ValidateLineImageUrl(imageUrl As String, fieldName As String)
        Dim parsedUri As Uri = Nothing

        If String.IsNullOrWhiteSpace(imageUrl) Then
            Throw New ApplicationException(fieldName & " ห้ามเป็นค่าว่าง")
        End If

        If Not Uri.TryCreate(imageUrl, UriKind.Absolute, parsedUri) Then
            Throw New ApplicationException(fieldName & " ไม่ใช่ URL ที่ถูกต้อง")
        End If

        If parsedUri.Scheme <> Uri.UriSchemeHttps Then
            Throw New ApplicationException(fieldName & " ต้องเป็น HTTPS")
        End If

        If parsedUri.IsLoopback OrElse parsedUri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) Then
            Throw New ApplicationException(fieldName & " ต้องเข้าถึงได้จากภายนอก จึงไม่สามารถใช้ localhost ได้")
        End If
    End Sub

    Private Function ReadResponseBody(response As WebResponse) As String
        If response Is Nothing Then
            Return String.Empty
        End If

        Using responseStream As Stream = response.GetResponseStream()
            If responseStream Is Nothing Then
                Return String.Empty
            End If

            Using reader As New StreamReader(responseStream)
                Return reader.ReadToEnd()
            End Using
        End Using
    End Function

    Private Function BuildResponseSuffix(responseText As String) As String
        If String.IsNullOrWhiteSpace(responseText) Then
            Return String.Empty
        End If

        Return ": " & responseText
    End Function

    Private Sub RenderStatus(isSuccess As Boolean, message As String)
        Dim backgroundColor As String = If(isSuccess, "#ecfdf5", "#fef2f2")
        Dim borderColor As String = If(isSuccess, "#86efac", "#fca5a5")
        Dim textColor As String = If(isSuccess, "#166534", "#991b1b")
        Dim titleText As String = If(isSuccess, "สำเร็จ", "เกิดข้อผิดพลาด")

        litStatus.Text =
            "<div style='margin-top: 16px; padding: 14px 16px; border: 1px solid " & borderColor & "; border-radius: 8px; background-color: " & backgroundColor & "; color: " & textColor & ";'>" &
            "<strong>" & titleText & ":</strong> " & Server.HtmlEncode(message) &
            "</div>"
    End Sub

    Private Class LineBroadcastRequest
        Public Property messages As List(Of LineMessagePayload)
    End Class

    Private Class LineMessagePayload
        Public Property type As String
        Public Property text As String
        Public Property originalContentUrl As String
        Public Property previewImageUrl As String
    End Class

    Private Class LineSendAttemptResult
        Public Property IsSuccess As Boolean
        Public Property Message As String
        Public Property PublicOriginalUrl As String
        Public Property PublicPreviewUrl As String
        Public Property WasAttempted As Boolean
    End Class

    Private Class KnownFaceReference
        Public Property label As String
        Public Property imageUrl As String
        Public Property previewUrl As String
        Public Property fileName As String
        Public Property lastUpdatedText As String
    End Class

    Private Class KnownFaceRecognitionEntry
        Public Property label As String
        Public Property fileName As String
        Public Property imageUrl As String
        Public Property previewUrl As String
        Public Property descriptor As Double()
    End Class

    Private Class KnownFaceDescriptorDatabase
        Public Property version As Integer
        Public Property samples As List(Of KnownFaceDescriptorSample)
    End Class

    Private Class KnownFaceDescriptorSample
        Public Property label As String
        Public Property fileName As String
        Public Property descriptor As List(Of Double)
        Public Property updatedAtUtc As String
    End Class

    Private Class RecognizedFaceReference
        Public Property label As String
        Public Property imageUrl As String
        Public Property previewUrl As String
        Public Property fileName As String
        Public Property lastUpdatedText As String
        Public Property sortTicks As Long
    End Class

    Private Class UploadedImageInfo
        Public Property OriginalFileName As String
        Public Property PreviewFileName As String
        Public Property OriginalVirtualPath As String
        Public Property PreviewVirtualPath As String
        Public Property OriginalRelativeUrl As String
        Public Property PreviewRelativeUrl As String
        Public Property OriginalPhysicalPath As String
        Public Property PreviewPhysicalPath As String
    End Class
End Class
