Partial Class formulaireCorrespondancePopup
    Inherits System.Web.UI.Page

    ''' <summary>
    ''' Chargement de la page popup
    ''' </summary>
    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        If Not IsPostBack Then
            ' Récupérer les paramètres de l'URL
            Dim codeFour As String = Request.QueryString("codeFour")
            Dim refFour As String = Request.QueryString("refFour")
            Dim libelle As String = Request.QueryString("libelle")
            Dim numOR As String = Request.QueryString("numOR")
            Dim numFac As String = Request.QueryString("numFac")

            ' Initialiser le formulaire
            formulaire1.CodeFournisseur = codeFour
            formulaire1.CodePrestaFournisseur = refFour
            formulaire1.LibelleFournisseur = libelle
            formulaire1.NumOR = numOR
            formulaire1.NumFacture = numFac
        End If
    End Sub
End Class
