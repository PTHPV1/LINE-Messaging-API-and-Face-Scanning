Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Linq
Imports System.Text
Imports System.Web
Imports System.Web.Script.Serialization

Public NotInheritable Class FaceDescriptorStore
    Public Const DescriptorLength As Integer = 128
    Private Const DefaultThreshold As Double = 0.5R
    Private Const KnownFaceDescriptorDatabaseVirtualPath As String = "~/App_Data/known-face-descriptors.json"
    Private Shared ReadOnly SyncRoot As New Object()

    Private Sub New()
    End Sub

    Public Shared Function CreateSerializer() As JavaScriptSerializer
        Return New JavaScriptSerializer() With {
            .MaxJsonLength = Integer.MaxValue,
            .RecursionLimit = 256
        }
    End Function

    Public Shared Function GetSummary(context As HttpContext) As KnownFaceCatalogSummary
        SyncLock SyncRoot
            Return BuildSummary(LoadDatabase(context))
        End SyncLock
    End Function

    Public Shared Sub SaveDescriptor(context As HttpContext, personName As String, fileName As String, descriptorValues As IEnumerable(Of Double))
        Dim normalizedPersonName As String = NormalizeLabel(personName)
        Dim normalizedFileName As String = NormalizeFileName(fileName)
        Dim normalizedDescriptor As List(Of Double) = NormalizeDescriptorValues(descriptorValues)

        If String.IsNullOrWhiteSpace(normalizedPersonName) Then
            Throw New ApplicationException("ไม่พบชื่อบุคคลสำหรับบันทึกตัวเลขอ้างอิงใบหน้า")
        End If

        If String.IsNullOrWhiteSpace(normalizedFileName) Then
            Throw New ApplicationException("ไม่พบไฟล์รูปภาพสำหรับบันทึกตัวเลขอ้างอิงใบหน้า")
        End If

        SyncLock SyncRoot
            Dim descriptorDatabase As KnownFaceDescriptorDatabase = LoadDatabase(context)
            Dim descriptorKey As String = BuildDescriptorKey(normalizedPersonName, normalizedFileName)

            descriptorDatabase.samples.RemoveAll(Function(sample) BuildDescriptorKey(sample.label, sample.fileName) = descriptorKey)
            descriptorDatabase.samples.Add(New KnownFaceDescriptorSample With {
                .label = normalizedPersonName,
                .fileName = normalizedFileName,
                .descriptor = normalizedDescriptor,
                .updatedAtUtc = DateTime.UtcNow.ToString("o")
            })

            SaveDatabase(context, descriptorDatabase)
        End SyncLock
    End Sub

    Public Shared Function FindBestMatches(context As HttpContext, descriptorSets As IEnumerable(Of Double()), threshold As Double) As FaceMatchResponse
        If descriptorSets Is Nothing Then
            Throw New ApplicationException("ไม่พบข้อมูล descriptor สำหรับเทียบใบหน้า")
        End If

        Dim descriptorQueries As List(Of Double()) = descriptorSets.
            Select(Function(values) NormalizeDescriptorArray(values)).
            ToList()
        Dim descriptorDatabase As KnownFaceDescriptorDatabase

        SyncLock SyncRoot
            descriptorDatabase = LoadDatabase(context)
        End SyncLock

        Dim groupedSamples As Dictionary(Of String, List(Of KnownFaceDescriptorVector)) = BuildSampleLookup(descriptorDatabase)
        Dim summary As KnownFaceCatalogSummary = BuildSummary(descriptorDatabase)
        Dim response As New FaceMatchResponse With {
            .matches = New List(Of FaceMatchResult)(),
            .personCount = summary.personCount,
            .sampleCount = summary.sampleCount,
            .threshold = NormalizeThreshold(threshold)
        }

        For Each descriptorQuery As Double() In descriptorQueries
            response.matches.Add(FindBestMatch(groupedSamples, descriptorQuery, response.threshold))
        Next

        Return response
    End Function

    Private Shared Function FindBestMatch(groupedSamples As Dictionary(Of String, List(Of KnownFaceDescriptorVector)), descriptorQuery As Double(), threshold As Double) As FaceMatchResult
        If groupedSamples Is Nothing OrElse groupedSamples.Count = 0 Then
            Return New FaceMatchResult With {
                .distance = 1R,
                .fileName = String.Empty,
                .isMatch = False,
                .label = "unknown"
            }
        End If

        Dim bestLabel As String = String.Empty
        Dim bestFileName As String = String.Empty
        Dim bestDistance As Double = Double.MaxValue

        For Each groupedSample As KeyValuePair(Of String, List(Of KnownFaceDescriptorVector)) In groupedSamples
            Dim descriptorCount As Integer = 0
            Dim totalDistance As Double = 0R
            Dim closestFileName As String = String.Empty
            Dim closestDistance As Double = Double.MaxValue

            For Each descriptorSample As KnownFaceDescriptorVector In groupedSample.Value
                Dim sampleDistance As Double = ComputeEuclideanDistance(descriptorQuery, descriptorSample.descriptor)
                totalDistance += sampleDistance
                descriptorCount += 1

                If sampleDistance < closestDistance Then
                    closestDistance = sampleDistance
                    closestFileName = descriptorSample.fileName
                End If
            Next

            If descriptorCount = 0 Then
                Continue For
            End If

            Dim meanDistance As Double = totalDistance / descriptorCount

            If meanDistance < bestDistance Then
                bestDistance = meanDistance
                bestFileName = closestFileName
                bestLabel = groupedSample.Key
            End If
        Next

        Dim normalizedDistance As Double = If(Double.IsInfinity(bestDistance) OrElse Double.IsNaN(bestDistance), 1R, Math.Round(bestDistance, 6))
        Dim isMatch As Boolean = Not String.IsNullOrWhiteSpace(bestLabel) AndAlso normalizedDistance <= threshold

        Return New FaceMatchResult With {
            .distance = normalizedDistance,
            .fileName = If(isMatch, bestFileName, String.Empty),
            .isMatch = isMatch,
            .label = If(isMatch, bestLabel, "unknown")
        }
    End Function

    Private Shared Function BuildSampleLookup(descriptorDatabase As KnownFaceDescriptorDatabase) As Dictionary(Of String, List(Of KnownFaceDescriptorVector))
        Dim groupedSamples As New Dictionary(Of String, List(Of KnownFaceDescriptorVector))(StringComparer.OrdinalIgnoreCase)

        For Each descriptorSample As KnownFaceDescriptorSample In descriptorDatabase.samples
            If descriptorSample Is Nothing Then
                Continue For
            End If

            Dim label As String = NormalizeLabel(descriptorSample.label)
            Dim fileName As String = NormalizeFileName(descriptorSample.fileName)
            Dim descriptorValues As Double() = TryNormalizeStoredDescriptor(descriptorSample.descriptor)

            If String.IsNullOrWhiteSpace(label) OrElse String.IsNullOrWhiteSpace(fileName) OrElse descriptorValues Is Nothing Then
                Continue For
            End If

            If Not groupedSamples.ContainsKey(label) Then
                groupedSamples(label) = New List(Of KnownFaceDescriptorVector)()
            End If

            groupedSamples(label).Add(New KnownFaceDescriptorVector With {
                .descriptor = descriptorValues,
                .fileName = fileName
            })
        Next

        Return groupedSamples
    End Function

    Private Shared Function BuildSummary(descriptorDatabase As KnownFaceDescriptorDatabase) As KnownFaceCatalogSummary
        Dim uniqueLabels As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        Dim sampleCount As Integer = 0

        For Each descriptorSample As KnownFaceDescriptorSample In descriptorDatabase.samples
            If descriptorSample Is Nothing Then
                Continue For
            End If

            Dim label As String = NormalizeLabel(descriptorSample.label)
            Dim descriptorValues As Double() = TryNormalizeStoredDescriptor(descriptorSample.descriptor)

            If String.IsNullOrWhiteSpace(label) OrElse descriptorValues Is Nothing Then
                Continue For
            End If

            uniqueLabels.Add(label)
            sampleCount += 1
        Next

        Return New KnownFaceCatalogSummary With {
            .personCount = uniqueLabels.Count,
            .sampleCount = sampleCount
        }
    End Function

    Private Shared Function NormalizeDescriptorValues(descriptorValues As IEnumerable(Of Double)) As List(Of Double)
        Dim normalizedValues As Double() = NormalizeDescriptorArray(If(descriptorValues, Enumerable.Empty(Of Double)()).ToArray())
        Return normalizedValues.ToList()
    End Function

    Private Shared Function NormalizeDescriptorArray(descriptorValues As IEnumerable(Of Double)) As Double()
        If descriptorValues Is Nothing Then
            Throw New ApplicationException("ไม่พบตัวเลขอ้างอิงใบหน้าที่ครบถ้วน")
        End If

        Dim normalizedValues As Double() = descriptorValues.ToArray()

        If normalizedValues.Length <> DescriptorLength Then
            Throw New ApplicationException("ตัวเลขอ้างอิงใบหน้ามีจำนวนไม่ถูกต้อง")
        End If

        For Each descriptorValue As Double In normalizedValues
            If Double.IsNaN(descriptorValue) OrElse Double.IsInfinity(descriptorValue) Then
                Throw New ApplicationException("ตัวเลขอ้างอิงใบหน้ามีค่าที่ไม่ถูกต้อง")
            End If
        Next

        Return normalizedValues
    End Function

    Private Shared Function TryNormalizeStoredDescriptor(descriptorValues As IEnumerable(Of Double)) As Double()
        Try
            Return NormalizeDescriptorArray(descriptorValues)
        Catch
            Return Nothing
        End Try
    End Function

    Private Shared Function ComputeEuclideanDistance(leftDescriptor As Double(), rightDescriptor As Double()) As Double
        Dim squaredDistance As Double = 0R

        For index As Integer = 0 To DescriptorLength - 1
            Dim delta As Double = leftDescriptor(index) - rightDescriptor(index)
            squaredDistance += delta * delta
        Next

        Return Math.Sqrt(squaredDistance)
    End Function

    Private Shared Function NormalizeThreshold(threshold As Double) As Double
        If Double.IsNaN(threshold) OrElse Double.IsInfinity(threshold) Then
            Return DefaultThreshold
        End If

        Return Math.Max(0R, Math.Min(2R, threshold))
    End Function

    Private Shared Function LoadDatabase(context As HttpContext) As KnownFaceDescriptorDatabase
        Dim databasePhysicalPath As String = GetKnownFaceDescriptorDatabasePhysicalPath(context)

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

        Dim serializer As JavaScriptSerializer = CreateSerializer()
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

    Private Shared Sub SaveDatabase(context As HttpContext, descriptorDatabase As KnownFaceDescriptorDatabase)
        If descriptorDatabase Is Nothing Then
            Throw New ApplicationException("ไม่พบฐานข้อมูลตัวเลขอ้างอิงใบหน้า")
        End If

        If descriptorDatabase.samples Is Nothing Then
            descriptorDatabase.samples = New List(Of KnownFaceDescriptorSample)()
        End If

        descriptorDatabase.version = 1

        Dim serializer As JavaScriptSerializer = CreateSerializer()
        Dim databasePhysicalPath As String = GetKnownFaceDescriptorDatabasePhysicalPath(context)
        Dim rawJson As String = serializer.Serialize(descriptorDatabase)
        File.WriteAllText(databasePhysicalPath, rawJson, Encoding.UTF8)
    End Sub

    Private Shared Function GetKnownFaceDescriptorDatabasePhysicalPath(context As HttpContext) As String
        Dim httpContext As HttpContext = If(context, HttpContext.Current)

        If httpContext Is Nothing Then
            Throw New ApplicationException("ไม่พบ HttpContext สำหรับใช้งานฐานข้อมูลใบหน้า")
        End If

        Dim databasePhysicalPath As String = httpContext.Server.MapPath(KnownFaceDescriptorDatabaseVirtualPath)
        Dim databaseDirectory As String = Path.GetDirectoryName(databasePhysicalPath)

        If Not String.IsNullOrWhiteSpace(databaseDirectory) Then
            Directory.CreateDirectory(databaseDirectory)
        End If

        Return databasePhysicalPath
    End Function

    Private Shared Function BuildDescriptorKey(personName As String, fileName As String) As String
        Return NormalizeLabel(personName).ToLowerInvariant() & "|" & NormalizeFileName(fileName).ToLowerInvariant()
    End Function

    Private Shared Function NormalizeLabel(value As String) As String
        Return If(value, String.Empty).Trim()
    End Function

    Private Shared Function NormalizeFileName(value As String) As String
        Return Path.GetFileName(If(value, String.Empty)).Trim()
    End Function

    Private Class KnownFaceDescriptorVector
        Public Property descriptor As Double()
        Public Property fileName As String
    End Class
End Class

Public Class KnownFaceCatalogSummary
    Public Property personCount As Integer
    Public Property sampleCount As Integer
End Class

Public Class KnownFaceDescriptorDatabase
    Public Property version As Integer
    Public Property samples As List(Of KnownFaceDescriptorSample)
End Class

Public Class KnownFaceDescriptorSample
    Public Property label As String
    Public Property fileName As String
    Public Property descriptor As List(Of Double)
    Public Property updatedAtUtc As String
End Class

Public Class FaceMatchResponse
    Public Property personCount As Integer
    Public Property sampleCount As Integer
    Public Property threshold As Double
    Public Property matches As List(Of FaceMatchResult)
End Class

Public Class FaceMatchResult
    Public Property label As String
    Public Property fileName As String
    Public Property distance As Double
    Public Property isMatch As Boolean
End Class
