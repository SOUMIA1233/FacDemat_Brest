Imports System.Data
Imports System.Globalization

Partial Class ControlesUtilisateur_FormulaireCorrespondance
    Inherits System.Web.UI.UserControl

#Region "PROPRIÉTÉS PUBLIQUES"

    ''' <summary>Code du fournisseur</summary>
    Public Property CodeFournisseur As String
        Get
            Return txtCodeFournisseur.Text
        End Get
        Set(value As String)
            txtCodeFournisseur.Text = value
        End Set
    End Property


    ''' <summary>Code de la prestation fournisseur</summary>
    Public Property CodePrestaFournisseur As String
        Get
            Return txtRefFournisseur.Text
        End Get
        Set(value As String)
            txtRefFournisseur.Text = value
        End Set
    End Property


    ''' <summary>Libellé de la prestation fournisseur</summary>
    Public Property LibelleFournisseur As String
        Get
            Return txtLibelle.Text
        End Get
        Set(value As String)
            txtLibelle.Text = value
        End Set
    End Property


    ''' <summary>Numéro d'OR de la facture</summary>
    Public Property NumOR As String
        Get
            Return If(TryCast(ViewState("NumOR"), String), "")
        End Get
        Set(value As String)
            ViewState("NumOR") = value
        End Set
    End Property


    ''' <summary>Numéro de facture</summary>
    Public Property NumFacture As String
        Get
            Return If(TryCast(ViewState("NumFacture"), String), "")
        End Get
        Set(value As String)
            ViewState("NumFacture") = value
        End Set
    End Property

#End Region

#Region "PROPRIÉTÉS PRIVÉES - DATATABLE"

    ''' <summary>Clé de session unique pour la DataTable</summary>
    Private ReadOnly Property SessionKeyPrestations As String
        Get
            Return "DT_PRESTAS_" & Me.UniqueID
        End Get
    End Property


    ''' <summary>DataTable contenant les prestations saisies</summary>
    Private Property DtPrestations As DataTable
        Get
            Dim dt = TryCast(Session(SessionKeyPrestations), DataTable)
            If dt Is Nothing Then
                dt = CreerStructureDataTable()
                ' Ajouter une ligne par défaut
                dt.Rows.Add(Guid.NewGuid().ToString("N"), "", "TOTAL", 0D)
                Session(SessionKeyPrestations) = dt
            End If
            Return dt
        End Get
        Set(value As DataTable)
            Session(SessionKeyPrestations) = value
        End Set
    End Property

#End Region

#Region "CYCLE DE VIE DE LA PAGE"

    ''' <summary>Initialisation du contrôle</summary>
    Protected Sub Page_Init(sender As Object, e As EventArgs) Handles Me.Init
        BindRepeater()
    End Sub


    ''' <summary>Chargement de la page</summary>
    Protected Sub Page_Load(sender As Object, e As EventArgs) Handles Me.Load
        ' Le binding est géré dans Page_Init
    End Sub

#End Region

#Region "GESTIONNAIRES D'ÉVÉNEMENTS - BOUTONS"

    ''' <summary>Ajouter une nouvelle prestation</summary>
    Protected Sub btnAjouterPrestation_Click(sender As Object, e As EventArgs) Handles btnAjouterPrestation.Click
        SyncRepeaterToDataTable()
        DtPrestations.Rows.Add(Guid.NewGuid().ToString("N"), "", "TOTAL", 0D)
        BindRepeater()
    End Sub


    ''' <summary>Valider le formulaire et afficher le récapitulatif</summary>
    Protected Sub btnValider_Click(sender As Object, e As EventArgs) Handles btnValider.Click
        SyncRepeaterToDataTable()
        lblMessage.Visible = False

        If Not ValiderFormulaire() Then
            Return
        End If

        If Not VerifierExistencePrestationsLocPro() Then
            Return
        End If

        AfficherRecapitulatif()
    End Sub


    ''' <summary>Retour du récapitulatif vers le formulaire</summary>
    Protected Sub btnRetourRecap_Click(sender As Object, e As EventArgs) Handles btnRetourRecap.Click
        MasquerRecapitulatif()
        AfficherFormulaire()
    End Sub


    ''' <summary>Confirmer la création de la règle</summary>
    Protected Sub btnConfirmerCreation_Click(sender As Object, e As EventArgs) Handles btnConfirmerCreation.Click
        CreerRegleEnBDD()
    End Sub

#End Region

#Region "GESTIONNAIRES D'ÉVÉNEMENTS - REPEATER"

    ''' <summary>Binding des données du Repeater</summary>
    Protected Sub rptPrestations_ItemDataBound(sender As Object, e As RepeaterItemEventArgs)
        If e.Item.ItemType <> ListItemType.Item AndAlso e.Item.ItemType <> ListItemType.AlternatingItem Then
            Return
        End If

        Dim drv = TryCast(e.Item.DataItem, DataRowView)
        If drv Is Nothing Then
            Return
        End If

        Dim cboCodeLocPro = CType(e.Item.FindControl("cboCodeLocPro"), Telerik.Web.UI.RadComboBox)
        If cboCodeLocPro IsNot Nothing Then
            cboCodeLocPro.DataSource = GestionnaireBddFacture.GetListePrestationsLocPro()
            cboCodeLocPro.DataTextField = "F100KY"
            cboCodeLocPro.DataValueField = "F100KY"
            cboCodeLocPro.DataBind()
        End If

        ' Remplir les contrôles
        RemplirControlesPrestation(e.Item, drv)
    End Sub


    ''' <summary>Commande du Repeater (suppression)</summary>
    Protected Sub rptPrestations_ItemCommand(source As Object, e As RepeaterCommandEventArgs)
        If e.CommandName = "SUPPRIMER" Then
            SupprimerPrestation(e.CommandArgument.ToString())
        End If
    End Sub

#End Region

#Region "MÉTHODES PRIVÉES - DATATABLE"

    ''' <summary>Crée la structure de la DataTable</summary>
    Private Function CreerStructureDataTable() As DataTable
        Dim dt As New DataTable()
        dt.Columns.Add("RowId", GetType(String))
        dt.Columns.Add("CodeLocPro", GetType(String))
        dt.Columns.Add("Mode", GetType(String))
        dt.Columns.Add("Valeur", GetType(Decimal))
        Return dt
    End Function


    ''' <summary>Synchronise les données du Repeater vers la DataTable</summary>
    Private Sub SyncRepeaterToDataTable()
        For Each item As RepeaterItem In rptPrestations.Items
            Dim hidRowId = CType(item.FindControl("hidRowId"), HiddenField)
            Dim row As DataRow = TrouverLigneParRowId(hidRowId.Value)

            If row IsNot Nothing Then
                MettreAJourLigneDepuisControles(row, item)
            End If
        Next
    End Sub


    ''' <summary>Trouve une ligne dans la DataTable par son RowId</summary>
    Private Function TrouverLigneParRowId(rowId As String) As DataRow
        For Each r As DataRow In DtPrestations.Rows
            If String.Equals(r("RowId").ToString(), rowId, StringComparison.OrdinalIgnoreCase) Then
                Return r
            End If
        Next
        Return Nothing
    End Function


    ''' <summary>Met à jour une ligne de la DataTable depuis les contrôles</summary>
    Private Sub MettreAJourLigneDepuisControles(row As DataRow, item As RepeaterItem)
        ' Code LocPro
        Dim cboCode = CType(item.FindControl("cboCodeLocPro"), Telerik.Web.UI.RadComboBox)
        row("CodeLocPro") = cboCode.SelectedValue.Trim()

        ' Radio buttons
        Dim rbTotal = CType(item.FindControl("rbTotal"), RadioButton)
        Dim rbPourcent = CType(item.FindControl("rbPourcent"), RadioButton)
        Dim rbMontant = CType(item.FindControl("rbMontant"), RadioButton)

        ' Déterminer le mode
        Dim mode As String = DeterminerMode(rbTotal, rbPourcent, rbMontant)
        row("Mode") = mode

        ' Récupérer la valeur selon le mode
        Dim valeur As Decimal = RecupererValeurSelonMode(item, mode)
        row("Valeur") = valeur
    End Sub


    ''' <summary>Détermine le mode de répartition sélectionné</summary>
    Private Function DeterminerMode(rbTotal As RadioButton, rbPourcent As RadioButton, rbMontant As RadioButton) As String
        If rbPourcent.Checked Then
            Return "POURCENT"
        ElseIf rbMontant.Checked Then
            Return "MONTANT"
        ElseIf rbTotal.Checked Then
            Return "TOTAL"
        Else
            Return "INCLUS"
        End If
    End Function


    ''' <summary>Récupère la valeur selon le mode de répartition</summary>
    Private Function RecupererValeurSelonMode(item As RepeaterItem, mode As String) As Decimal
        Dim valeur As Decimal = 0D

        If mode = "POURCENT" Then
            Dim txtPourcent = CType(item.FindControl("txtPourcent"), TextBox)
            Decimal.TryParse(txtPourcent.Text.Replace(","c, "."c), NumberStyles.Any, CultureInfo.InvariantCulture, valeur)
        ElseIf mode = "MONTANT" Then
            Dim txtMontant = CType(item.FindControl("txtMontant"), TextBox)
            Decimal.TryParse(txtMontant.Text.Replace(","c, "."c), NumberStyles.Any, CultureInfo.InvariantCulture, valeur)
        End If

        Return valeur
    End Function


    ''' <summary>Supprime une prestation de la DataTable</summary>
    Private Sub SupprimerPrestation(rowId As String)
        SyncRepeaterToDataTable()

        For i As Integer = DtPrestations.Rows.Count - 1 To 0 Step -1
            If String.Equals(DtPrestations.Rows(i)("RowId").ToString(), rowId, StringComparison.OrdinalIgnoreCase) Then
                DtPrestations.Rows.RemoveAt(i)
                Exit For
            End If
        Next

        ' Garder au moins une ligne
        If DtPrestations.Rows.Count = 0 Then
            DtPrestations.Rows.Add(Guid.NewGuid().ToString("N"), "", "TOTAL", 0D)
        End If

        BindRepeater()
    End Sub

#End Region

#Region "MÉTHODES PRIVÉES - REPEATER"

    ''' <summary>Lie les données au Repeater</summary>
    Private Sub BindRepeater()
        rptPrestations.DataSource = DtPrestations
        rptPrestations.DataBind()
    End Sub


    ''' <summary>Remplit les contrôles d'une prestation</summary>
    Private Sub RemplirControlesPrestation(item As RepeaterItem, drv As DataRowView)
        ' Code LocPro
        Dim cboCode = CType(item.FindControl("cboCodeLocPro"), Telerik.Web.UI.RadComboBox)
        Dim codeLocPro As String = drv("CodeLocPro").ToString()
        If cboCode.Items.FindItemByValue(codeLocPro) IsNot Nothing Then
            cboCode.SelectedValue = codeLocPro
        End If

        ' Mode et valeur
        Dim mode As String = drv("Mode").ToString()
        Dim valeur As Decimal = If(IsDBNull(drv("Valeur")), 0D, Convert.ToDecimal(drv("Valeur")))

        ' Radio buttons
        Dim rbTotal = CType(item.FindControl("rbTotal"), RadioButton)
        Dim rbPourcent = CType(item.FindControl("rbPourcent"), RadioButton)
        Dim rbMontant = CType(item.FindControl("rbMontant"), RadioButton)

        rbTotal.Checked = (mode = "TOTAL")
        rbPourcent.Checked = (mode = "POURCENT")
        rbMontant.Checked = (mode = "MONTANT")

        ' TextBoxes
        Dim txtPourcent = CType(item.FindControl("txtPourcent"), TextBox)
        Dim txtMontant = CType(item.FindControl("txtMontant"), TextBox)

        txtPourcent.Text = If(mode = "POURCENT", valeur.ToString(CultureInfo.InvariantCulture), "")
        txtMontant.Text = If(mode = "MONTANT", valeur.ToString(CultureInfo.InvariantCulture), "")
    End Sub

#End Region

#Region "MÉTHODES PRIVÉES - VALIDATION"

    ''' <summary>Valide le formulaire côté serveur</summary>
    Private Function ValiderFormulaire() As Boolean
        ' Au moins une prestation
        If DtPrestations.Rows.Count = 0 Then
            AfficherErreur("Vous devez ajouter au moins une prestation LocPro.")
            Return False
        End If

        ' Tous les codes renseignés
        For Each dr As DataRow In DtPrestations.Rows
            If String.IsNullOrEmpty(dr("CodeLocPro").ToString().Trim()) Then
                AfficherErreur("Tous les codes LocPro doivent être renseignés.")
                Return False
            End If
        Next

        ' Validation des valeurs selon le mode
        For Each dr As DataRow In DtPrestations.Rows
            If Not ValiderValeurPrestation(dr) Then
                Return False
            End If
        Next

        Return True
    End Function


    ''' <summary>Valide la valeur d'une prestation selon son mode</summary>
    Private Function ValiderValeurPrestation(dr As DataRow) As Boolean
        Dim code As String = dr("CodeLocPro").ToString().Trim()
        Dim mode As String = dr("Mode").ToString()
        Dim valeur As Decimal = If(IsDBNull(dr("Valeur")), 0, Convert.ToDecimal(dr("Valeur")))

        Select Case mode
            Case "POURCENT"
                If valeur < 0 OrElse valeur > 100 Then
                    AfficherErreur("Le pourcentage pour " & code & " doit être entre 0 et 100.")
                    Return False
                End If

            Case "MONTANT"
                If valeur <= 0 Then
                    AfficherErreur("Le montant fixe pour " & code & " doit être supérieur à 0.")
                    Return False
                End If
        End Select

        Return True
    End Function


    ''' <summary>Vérifie que toutes les prestations existent dans LocPro</summary>
    Private Function VerifierExistencePrestationsLocPro() As Boolean
        Dim codesPrestations As New List(Of String)()

        For Each dr As DataRow In DtPrestations.Rows
            Dim code As String = dr("CodeLocPro").ToString().Trim()
            If Not String.IsNullOrEmpty(code) Then
                codesPrestations.Add(code)
            End If
        Next

        Dim codesInvalides As List(Of String) = GestionnaireBddFacture.VerifierPrestationsMultiples(codesPrestations)

        If codesInvalides.Count > 0 Then
            Dim message As String = "Les codes de prestation suivants n'existent pas dans LocPro : " & String.Join(", ", codesInvalides)
            AfficherErreur(message)
            GestionnaireLog.Warn("Prestations invalides : " & String.Join(", ", codesInvalides))
            Return False
        End If

        Return True
    End Function

#End Region

#Region "MÉTHODES PRIVÉES - RÉCAPITULATIF"

    ''' <summary>Affiche le récapitulatif avec simulation des montants</summary>
    Private Sub AfficherRecapitulatif()
        Dim sb As New StringBuilder()
        Dim montantLigneHT As Decimal = ObtenirMontantLigneFacture()

        ' Section informations générales
        GenererSectionInformations(sb, montantLigneHT)

        ' Section prestations LocPro
        GenererSectionPrestations(sb, montantLigneHT)

        ' Afficher le récap
        litRecapitulatif.Text = sb.ToString()
        MasquerFormulaire()
        AfficherPanelRecapitulatif()
    End Sub


    ''' <summary>Génère la section informations générales du récap</summary>
    Private Sub GenererSectionInformations(sb As StringBuilder, montantLigneHT As Decimal)
        sb.Append("<div class='recap-section'>")
        sb.Append("<div class='recap-section-title'>Informations générales</div>")

        AjouterLigneInfo(sb, "Fournisseur :", Server.HtmlEncode(CodeFournisseur.Trim()))
        AjouterLigneInfo(sb, "Code prestation fournisseur :", Server.HtmlEncode(CodePrestaFournisseur.Trim()))
        AjouterLigneInfo(sb, "Libellé :", Server.HtmlEncode(LibelleFournisseur.Trim()))

        If montantLigneHT > 0 Then
            AjouterLigneInfo(sb, "Montant ligne facture (HT) :", "<strong>" & montantLigneHT.ToString("N2") & " €</strong>")
        End If

        sb.Append("</div>")
    End Sub


    ''' <summary>Génère la section prestations LocPro du récap</summary>
    Private Sub GenererSectionPrestations(sb As StringBuilder, montantLigneHT As Decimal)
        sb.Append("<div class='recap-section'>")
        sb.Append("<div class='recap-section-title'>Prestations LocPro créées</div>")
        sb.Append("<div class='recap-prestations-list'>")

        Dim totalSimule As Decimal = 0

        For Each dr As DataRow In DtPrestations.Rows
            Dim code As String = dr("CodeLocPro").ToString().Trim()
            Dim mode As String = dr("Mode").ToString()
            Dim valeur As Decimal = If(IsDBNull(dr("Valeur")), 0, Convert.ToDecimal(dr("Valeur")))
            Dim montantSimule As Decimal = CalculateurMontants.CalculerMontant(mode, valeur, montantLigneHT)

            totalSimule += montantSimule

            GenererItemPrestation(sb, code, mode, valeur, montantSimule)
        Next

        sb.Append("</div></div>")

        ' Total
        sb.Append("<div class='recap-total'>")
        sb.Append("<span class='recap-total-label'>Total simulé :</span>")
        sb.Append("<span class='recap-total-montant'>" & totalSimule.ToString("N2") & " €</span>")
        sb.Append("</div>")
    End Sub


    ''' <summary>Génère un item de prestation dans le récap</summary>
    Private Sub GenererItemPrestation(sb As StringBuilder, code As String, mode As String, valeur As Decimal, montantSimule As Decimal)
        sb.Append("<div class='recap-prestation-item'>")
        sb.Append("<div class='recap-prestation-header'>• " & Server.HtmlEncode(code) & "</div>")
        sb.Append("<div class='recap-prestation-detail'>")
        sb.Append("<span class='recap-prestation-repartition'>")

        Select Case mode
            Case "TOTAL"
                sb.Append("100% du montant HT")
            Case "POURCENT"
                sb.Append(valeur.ToString("N2") & "% du montant HT")
            Case "MONTANT"
                sb.Append("Montant fixe")
            Case Else
                sb.Append("Prestation incluse (0€)")
        End Select

        sb.Append("</span>")
        sb.Append("<span class='recap-prestation-montant'>→ " & montantSimule.ToString("N2") & " €</span>")
        sb.Append("</div></div>")
    End Sub


    ''' <summary>Ajoute une ligne d'information au récap</summary>
    Private Sub AjouterLigneInfo(sb As StringBuilder, label As String, valeur As String)
        sb.Append("<div class='recap-info-line'>")
        sb.Append("<span class='recap-info-label'>" & label & "</span>")
        sb.Append("<span class='recap-info-value'>" & valeur & "</span>")
        sb.Append("</div>")
    End Sub


    ''' <summary>Récupère le montant HT de la ligne de facture</summary>
    Private Function ObtenirMontantLigneFacture() As Decimal
        Try
            Return GestionnaireBddFacture.ObtenirMontantLigneFacture(NumOR, NumFacture, CodePrestaFournisseur)
        Catch ex As Exception
            GestionnaireLog.Error("Erreur récupération montant : " & ex.Message)
            Return 0
        End Try
    End Function

#End Region

#Region "MÉTHODES PRIVÉES - CRÉATION RÈGLE"

    ''' <summary>Crée la règle de correspondance en BDD</summary>
    Private Sub CreerRegleEnBDD()
        Try
            ' Vérifier doublon
            If GestionnaireBddFacture.RegleCorrespondanceExiste(CodeFournisseur, CodePrestaFournisseur) Then
                AfficherAvertissement("Une règle existe déjà pour cette prestation fournisseur.")
                GestionnaireLog.Warn("Règle existante : " & CodeFournisseur & " - " & CodePrestaFournisseur)
                Return
            End If

            ' Générer JSON
            Dim jsonRegle As String = GestionnaireReglesJSON.GenererJSON(DtPrestations)
            GestionnaireLog.Info("JSON généré : " & jsonRegle)

            ' Enregistrer
            Dim succes As Boolean = GestionnaireBddFacture.CreerRegleCorrespondance(CodeFournisseur, CodePrestaFournisseur, txtLibelle.Text.Trim(), jsonRegle)

            If succes Then
                GestionnaireLog.Info("Règle créée avec succès")

                ' RE-MATCHAGE AUTOMATIQUE
                Dim nbFacturesMaj As Integer = ServiceReintegration.RematcherFactures(CodeFournisseur, CodePrestaFournisseur)

                ' Message de succès avec compteur
                If nbFacturesMaj > 0 Then
                    Dim message As String = "✅ Règle créée avec succès ! " & nbFacturesMaj & " facture(s) peuvent maintenant être intégrées."
                    GestionnaireLog.Info(message)
                Else
                    GestionnaireLog.Info("✅ Règle créée avec succès ! Aucune autre facture concernée.")
                End If

                FermerPopupEtRafraichirGrid()
            Else
                AfficherErreur("Erreur lors de la création de la règle.")
            End If


        Catch ex As Exception
            AfficherErreur("Erreur critique : " & ex.Message)
            GestionnaireLog.Error("Erreur création règle : " & ex.ToString())
        End Try
    End Sub

#End Region

#Region "MÉTHODES PRIVÉES - AFFICHAGE UI"

    ''' <summary>Affiche un message d'erreur</summary>
    Private Sub AfficherErreur(message As String)
        lblMessage.Text = "❌ " & message
        lblMessage.ForeColor = System.Drawing.Color.Red
        lblMessage.Visible = True
    End Sub


    ''' <summary>Affiche un message d'avertissement</summary>
    Private Sub AfficherAvertissement(message As String)
        lblMessage.Text = "⚠️ " & message
        lblMessage.ForeColor = System.Drawing.Color.Orange
        lblMessage.Visible = True
    End Sub


    ''' <summary>Masque le formulaire de saisie</summary>
    Private Sub MasquerFormulaire()
        btnValider.Visible = False
        rptPrestations.Visible = False
        btnAjouterPrestation.Visible = False
    End Sub


    ''' <summary>Affiche le formulaire de saisie</summary>
    Private Sub AfficherFormulaire()
        btnValider.Visible = True
        rptPrestations.Visible = True
        btnAjouterPrestation.Visible = True
        lblMessage.Visible = False
    End Sub


    ''' <summary>Affiche le panel récapitulatif</summary>
    Private Sub AfficherPanelRecapitulatif()
        pnlRecapitulatif.Visible = True
    End Sub


    ''' <summary>Masque le panel récapitulatif</summary>
    Private Sub MasquerRecapitulatif()
        pnlRecapitulatif.Visible = False
    End Sub


    ''' <summary>
    ''' Ferme la popup RadWindow et rafraîchit le grid parent
    ''' </summary>
    ''' <summary>
    Private Sub FermerPopupEtRafraichirGrid()
        ' Récupérer le ClientID du grid depuis l'URL
        Dim gridClientID As String = If(Request.QueryString("gridID"), "")

        ' Script pour fermer la popup PUIS rafraîchir le grid parent (dans cet ordre)
        Dim script As String = "function closeAndRefresh() { " &
                              "var oWnd = GetRadWindow(); " &
                              "if (oWnd) { " &
                              "var opener = oWnd.get_browserWindow(); " &
                              "oWnd.close(); " &
                              "if (opener) { " &
                              "setTimeout(function() { " &
                              "try { " &
                              "var grid = opener.$find('" & gridClientID & "'); " &
                              "if (grid) { " &
                              "grid.get_masterTableView().fireCommand('Refresh', ''); " &
                              "console.log('Grid rafraîchi avec succès'); " &
                              "} else { " &
                              "console.log('Grid non trouvé, rechargement de la page...'); " &
                              "opener.location.reload(); " &
                              "} " &
                              "} catch(e) { " &
                              "console.error('Erreur rafraîchissement:', e); " &
                              "opener.location.reload(); " &
                              "} " &
                              "}, 300); " &
                              "} " &
                              "} " &
                              "} closeAndRefresh();"

        Page.ClientScript.RegisterStartupScript(Me.GetType(), "CloseAndRefresh", script, True)
    End Sub

#End Region

End Class