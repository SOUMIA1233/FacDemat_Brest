<%@ WebHandler Language="VB" Class="DownloadPdf" %>

Imports System
Imports System.Web
Imports System.Threading.Tasks
Imports Services

Public Class DownloadPdf
    Inherits HttpTaskAsyncHandler

    Public Overrides Async Function ProcessRequestAsync(context As HttpContext) As Task
        Dim idFacture As String = context.Request.QueryString("id")
        Dim numFacture As String = context.Request.QueryString("num")
        
        If String.IsNullOrEmpty(idFacture) AndAlso String.IsNullOrEmpty(numFacture) Then
            context.Response.StatusCode = 400
            context.Response.Write("ID Facture ou NumFacture manquant.")
            Return
        End If

        Dim pdfBytes As Byte() = Nothing

        ' Tentative 1 : Maileva
        If Not String.IsNullOrEmpty(idFacture) Then
            Try
                Dim apiService As New MailevaApiService()
                pdfBytes = Await apiService.DownloadFacturePdfAsync(idFacture)
            Catch ex As Exception
                ' Ignorer l'erreur Maileva et essayer la BDD
                GestionnaireLog.Error("Echec récupération PDF Maileva pour " & idFacture & ". On va tenter la base de données. Erreur: " & ex.Message)
            End Try
        End If

        ' Tentative 2 : Base de donnÃ©es (si non trouvÃ© dans Maileva)
        If pdfBytes Is Nothing OrElse pdfBytes.Length = 0 Then
            If Not String.IsNullOrEmpty(numFacture) Then
                Try
                    pdfBytes = GestionnaireBddFacture.GetFacturePdfFromDb(numFacture)
                Catch ex As Exception
                    GestionnaireLog.Error("Echec récupération PDF BDD pour " & numFacture & ". Erreur: " & ex.Message)
                End Try
            End If
        End If

        ' Tentative 3 : Fallback d'exemple (pour dÃ©veloppement)
        If pdfBytes Is Nothing OrElse pdfBytes.Length = 0 Then
            Dim localPdfPath As String = context.Server.MapPath("~/exemple_facturx.pdf")
            If System.IO.File.Exists(localPdfPath) Then
                pdfBytes = System.IO.File.ReadAllBytes(localPdfPath)
            End If
        End If

        If pdfBytes IsNot Nothing AndAlso pdfBytes.Length > 0 Then
            Try
                context.Response.Clear()
                context.Response.ContentType = "application/pdf"
                Dim filename As String = If(Not String.IsNullOrEmpty(numFacture), numFacture, idFacture)
                context.Response.AddHeader("Content-Disposition", "inline; filename=facture_" & filename & ".pdf")
                context.Response.BinaryWrite(pdfBytes)
                context.Response.Flush()
                context.ApplicationInstance.CompleteRequest()
            Catch ex As Exception
                context.Response.StatusCode = 500
                context.Response.Write("Erreur lors de l'envoi du PDF: " & ex.Message)
            End Try
        Else
            context.Response.StatusCode = 404
            context.Response.Write("PDF introuvable ni dans Maileva ni dans la base de données.")
        End If
    End Function
End Class

