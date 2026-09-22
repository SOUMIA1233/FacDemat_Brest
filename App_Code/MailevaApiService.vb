Imports System
Imports System.Collections.Generic
Imports System.Configuration
Imports System.Net.Http
Imports System.Net.Http.Headers
Imports System.Text
Imports System.Threading.Tasks
Imports iTextSharp.text.pdf.events.IndexEvents
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq

Namespace Services
    Public Class MailevaApiService
        Private ReadOnly _httpClient As HttpClient

        Public Sub New()
            _httpClient = New HttpClient()
        End Sub

        ''' <summary>
        ''' Récupère un token d'authentification Bearer (OAuth2) depuis Maileva
        ''' </summary>
        Public Async Function GetAccessTokenAsync() As Task(Of String)
            Dim authUrl As String = ConfigurationManager.AppSettings("Maileva_AuthUrl")
            Dim clientId As String = ConfigurationManager.AppSettings("Maileva_ClientId")
            Dim clientSecret As String = ConfigurationManager.AppSettings("Maileva_ClientSecret")
            Dim username As String = ConfigurationManager.AppSettings("Maileva_Username")
            Dim password As String = ConfigurationManager.AppSettings("Maileva_Password")

            Dim request As New HttpRequestMessage(HttpMethod.Post, authUrl)

            Dim formData As New List(Of KeyValuePair(Of String, String))() From {
                New KeyValuePair(Of String, String)("client_id", clientId),
                New KeyValuePair(Of String, String)("client_secret", clientSecret),
                New KeyValuePair(Of String, String)("grant_type", "password"),
                New KeyValuePair(Of String, String)("username", username),
                New KeyValuePair(Of String, String)("password", password)
            }

            request.Content = New FormUrlEncodedContent(formData)

            Dim response = Await _httpClient.SendAsync(request)
            If response.IsSuccessStatusCode Then
                Dim jsonStr = Await response.Content.ReadAsStringAsync()
                Dim json = JObject.Parse(jsonStr)
                Return json("access_token").ToString()
            Else
                Dim errorMsg = Await response.Content.ReadAsStringAsync()
                Throw New Exception("Erreur lors de l'authentification Maileva (Code " & response.StatusCode.ToString() & "): " & errorMsg)
            End If
        End Function

        ''' <summary>
        ''' Crée un fichier CDAR et le soumet pour mettre à jour le statut de cycle de vie
        ''' </summary>
        Public Async Function MettreAJourStatutCycleDeVieAsync(incomingInvoiceId As String, nouveauStatut As String) As Task
            Dim token As String = Await GetAccessTokenAsync()
            Dim baseUrl As String = ConfigurationManager.AppSettings("Maileva_LifecycleBaseUrl")

            _httpClient.DefaultRequestHeaders.Authorization = New AuthenticationHeaderValue("Bearer", token)
            _httpClient.DefaultRequestHeaders.Accept.Clear()
            _httpClient.DefaultRequestHeaders.Accept.Add(New MediaTypeWithQualityHeaderValue("application/json"))

            ' 1. Création du fichier CDAR
            Dim cdarPayload As New JObject()
            cdarPayload.Add("name", "Mise a jour manuelle du statut")
            cdarPayload.Add("role", "BY")
            cdarPayload.Add("lifecycle_status", nouveauStatut)
            cdarPayload.Add("note", "")

            Dim jsonPayload As String = cdarPayload.ToString(Formatting.None)
            Dim content As New StringContent(jsonPayload, Encoding.UTF8, "application/json")

            Dim createUrl As String = baseUrl & "/incoming_invoices/" & incomingInvoiceId & "/cdar_files"
            Dim createResponse = Await _httpClient.PostAsync(createUrl, content)

            Dim cdarFileId As String = ""
            If createResponse.IsSuccessStatusCode Then
                Dim responseStr = Await createResponse.Content.ReadAsStringAsync()
                If Not String.IsNullOrEmpty(responseStr) Then
                    Try
                        Dim json = JObject.Parse(responseStr)
                        If json("id") IsNot Nothing Then
                            cdarFileId = json("id").ToString()
                        End If
                    Catch
                    End Try
                End If

                If String.IsNullOrEmpty(cdarFileId) AndAlso createResponse.Headers.Location IsNot Nothing Then
                    Dim segments = createResponse.Headers.Location.Segments
                    cdarFileId = segments(segments.Length - 1).Trim("/"c)
                End If
            Else
                Dim errorMsg = Await createResponse.Content.ReadAsStringAsync()
                Throw New Exception("Erreur lors de la création du CDAR (Code " & createResponse.StatusCode.ToString() & "): " & errorMsg)
            End If

            If String.IsNullOrEmpty(cdarFileId) Then
                Throw New Exception("Impossible de récupérer l'ID du fichier CDAR créé.")
            End If

            ' 2. Soumission du fichier CDAR
            Dim submitUrl As String = baseUrl & "/cdar_files/" & cdarFileId & "/submit"
            Dim submitContent As New StringContent("", Encoding.UTF8, "application/json")

            Dim submitResponse = Await _httpClient.PostAsync(submitUrl, submitContent)
            If Not submitResponse.IsSuccessStatusCode Then
                Dim errorMsg = Await submitResponse.Content.ReadAsStringAsync()
                Throw New Exception("Erreur lors de la soumission du CDAR (Code " & submitResponse.StatusCode.ToString() & "): " & errorMsg)
            End If
        End Function

        ''' <summary>
        ''' Télécharge l'archive de la facture et extrait le fichier PDF
        ''' </summary>
        Public Async Function DownloadFacturePdfAsync(incomingInvoiceId As String) As Task(Of Byte())
            Dim token As String = Await GetAccessTokenAsync()

            ' Utiliser l'URL de base pour les factures entrantes (par défaut)
            Dim baseUrl As String = ConfigurationManager.AppSettings("Maileva_IncomingInvoicesBaseUrl")
            If String.IsNullOrEmpty(baseUrl) Then
                baseUrl = "https://api.maileva.com/incoming_invoices/v1"
            End If

            _httpClient.DefaultRequestHeaders.Authorization = New AuthenticationHeaderValue("Bearer", token)
            _httpClient.DefaultRequestHeaders.Accept.Clear()
            _httpClient.DefaultRequestHeaders.Accept.Add(New MediaTypeWithQualityHeaderValue("*/*"))

            Dim downloadUrl As String = baseUrl & "/incoming_invoices/" & incomingInvoiceId & "/download_archive"
            Dim response = Await _httpClient.GetAsync(downloadUrl)

            If response.IsSuccessStatusCode Then
                ' Dim zipBytes As Byte() = Await response.Content.ReadAsByteArrayAsync()

                ' ' Extraction du PDF depuis le ZIP en mémoire
                ' Using ms As New System.IO.MemoryStream(zipBytes)
                    ' Using archive As New System.IO.Compression.ZipArchive(ms, System.IO.Compression.ZipArchiveMode.Read)
                        ' For Each entry In archive.Entries
                            ' If entry.FullName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) Then
                                ' Using entryStream = entry.Open()
                                    ' Using pdfMs As New System.IO.MemoryStream()
                                        ' entryStream.CopyTo(pdfMs)
                                        ' Return pdfMs.ToArray()
                                    ' End Using
                                ' End Using
                            ' End If
                        ' Next
                    ' End Using
                ' End Using
				
				Dim pdfBytes As Byte() = Await response.Content.ReadAsByteArrayAsync()
				Return pdfBytes

                Throw New Exception("Aucun fichier PDF trouvé dans l'archive téléchargée.")
            Else
                Dim errorMsg = Await response.Content.ReadAsStringAsync()
                Throw New Exception("Erreur lors du téléchargement de la facture (Code " & response.StatusCode.ToString() & "): " & errorMsg)
            End If
        End Function

    End Class
End Namespace
