Imports System.IO
Imports Telerik.Web.UI
Imports System

Partial Class integrationFactures
    Inherits System.Web.UI.Page

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Load
        rwFormulaireCorrespondance.VisibleOnPageLoad = False
        rwIntegrationResult.VisibleOnPageLoad = False

        If Not IsPostBack Then
            ChargerHistorique()
        End If
        ' Toujours vérifier les factures au rechargement de la page au cas où le Refresh JS échoue
        ServiceReintegration.VerifierFacturesDematParLot()
        ' pour la facture demat c'est déclanché dans la fonction rgFacturesDemat_NeedDataSource via la déclaration dans le tableau radGrid OnNeedDataSource="rgFacturesDemat_NeedDataSource"
    End Sub


    ''' Charge l'historique des factures dans le RadGrid
    Private Sub ChargerHistorique()
        Try
            rgHistoriqueFactures.DataSource = GestionnaireBddFacture.ObtenirHistoriqueFactures()
            rgHistoriqueFactures.DataBind()
        Catch ex As Exception
            GestionnaireLog.Error("Erreur lors du chargement de l'historique : " & ex.Message)
        End Try
    End Sub


    ''' Événement déclenché lors du clic sur le bouton d'intégration
    Protected Async Sub rb_validerRep_Click(sender As Object, e As EventArgs) Handles rb_validerRep.Click
        ' Désactiver le bouton pendant le traitement
        rb_validerRep.Enabled = False
        lbl_result.Text = "Traitement en cours..."

        If rtb_repertoire.UploadedFiles.Count = 0 Then
            lbl_result.Text = "Aucun fichier uploadé."
            rb_validerRep.Enabled = True
            Return
        End If

        Dim tempFolder As String = Server.MapPath("~/TempFiles/")
        Dim resultatsGlobaux As New List(Of ResultatTraitementImportFacture)()

        Try
            If Not Directory.Exists(tempFolder) Then
                Directory.CreateDirectory(tempFolder)
            End If

            Dim sourceImport As String = User.Identity.Name ' À remplacer par l'utilisateur connecté

            For Each uploadedFile As UploadedFile In rtb_repertoire.UploadedFiles
                Dim tempFilePath As String = Path.Combine(tempFolder, uploadedFile.GetName())
                uploadedFile.SaveAs(tempFilePath)

                Try
                    Dim resultat = Await ServiceImportFacture.integrerFacture(tempFilePath, sourceImport)
                    resultatsGlobaux.Add(resultat)

                Catch ex As Exception
                    ' Créer un résultat d'échec
                    Dim resultatEchec As New ResultatTraitementImportFacture() With {
                        .NomFichier = uploadedFile.GetName(),
                        .Statut = "ERREUR_CRITIQUE",
                        .Message = ex.Message
                    }
                    resultatsGlobaux.Add(resultatEchec)
                    GestionnaireLog.Error("Erreur critique sur fichier " & uploadedFile.GetName() & " : " & ex.Message)
                End Try
            Next

            ' Afficher un résumé
            Dim nbSucces As Integer = 0
            For Each resultat In resultatsGlobaux
                If resultat.Statut = "SUCCES" Then
                    nbSucces += 1
                End If
            Next
            Dim nbEchecs As Integer = resultatsGlobaux.Count - nbSucces
            lbl_result.Text = "Traitement terminé : " & nbSucces.ToString() & " succès, " & nbEchecs.ToString() & " échec(s)"

            ' Rafraîchir la grille
            ChargerHistorique()

        Catch ex As Exception
            lbl_result.Text = "Erreur critique lors du traitement"
            GestionnaireLog.Error("Erreur globale : " & ex.ToString())
        Finally
            ' Nettoyage
            If Directory.Exists(tempFolder) Then
                Try
                    Directory.Delete(tempFolder, recursive:=True)
                Catch
                    GestionnaireLog.Warn("Impossible de supprimer le répertoire temporaire")
                End Try
            End If

            rb_validerRep.Enabled = True
        End Try
    End Sub


    ''' Événement déclenché lors du chargement des détails (lignes de facture)
    Protected Sub rgHistoriqueFactures_DetailTableDataBind(sender As Object, e As GridDetailTableDataBindEventArgs) Handles rgHistoriqueFactures.DetailTableDataBind
        Try
            Dim dataItem As GridDataItem = CType(e.DetailTableView.ParentItem, GridDataItem)

            Dim numOR As String = dataItem.GetDataKeyValue("NumOR").ToString().Trim()
            Dim numFacture As String = dataItem.GetDataKeyValue("NumFacture").ToString().Trim()

            e.DetailTableView.DataSource = GestionnaireBddFacture.ObtenirLignesFacture(numOR, numFacture)
        Catch ex As Exception
            GestionnaireLog.Error("Erreur lors du chargement des détails : " & ex.Message)
        End Try
    End Sub

    ''' <summary>
    ''' Événement déclenché lors de la création de chaque ligne (principale ET détails)
    ''' </summary>
    Protected Sub rgHistoriqueFactures_ItemDataBound(sender As Object, e As GridItemEventArgs) Handles rgHistoriqueFactures.ItemDataBound
        If TypeOf e.Item Is GridDataItem Then
            Dim dataItem As GridDataItem = CType(e.Item, GridDataItem)

            ' ===== GESTION TABLE PRINCIPALE =====
            If e.Item.OwnerTableView.Name = "MasterTableView" OrElse e.Item.OwnerTableView Is rgHistoriqueFactures.MasterTableView Then
                ' Si la ligne est développée, ajouter la classe CSS
                If dataItem.Expanded Then
                    dataItem.CssClass = dataItem.CssClass & " rgExpandedRow"
                Else
                    dataItem.CssClass = dataItem.CssClass.Replace("rgExpandedRow", "").Trim()
                End If

                Dim lblStatut As Label = CType(dataItem.FindControl("lblStatut"), Label)
                Dim statut As String = lblStatut.Text.Trim()

                GererEditionFournisseur(dataItem, statut)
                GererEditionImmat(dataItem, statut)
            End If

            ' ===== GESTION TABLE DE DÉTAILS (LIGNES) =====
            If e.Item.OwnerTableView.Name = "LignesFacture" Then
                ' Récupérer le RadLabel du code prestation LocPro
                Dim lblCodePrestaLP As RadLabel = CType(dataItem.FindControl("lblCodePrestaLP"), RadLabel)
                Dim btnAjouterRegle As RadButton = CType(dataItem.FindControl("btnAjouterRegle"), RadButton)

                If lblCodePrestaLP IsNot Nothing AndAlso btnAjouterRegle IsNot Nothing Then
                    Dim codePrestaLP As String = lblCodePrestaLP.Text.Trim()

                    ' Nettoyer les caractères HTML
                    codePrestaLP = codePrestaLP.Replace("&nbsp;", "").Trim()

                    ' Afficher le bouton uniquement si CodePrestaLP est vide
                    If String.IsNullOrEmpty(codePrestaLP) Then
                        btnAjouterRegle.Visible = True
                    Else
                        btnAjouterRegle.Visible = False
                    End If
                End If
            End If
        End If
    End Sub


    ''' Événement déclenché lors d'une commande sur le RadGrid
    Protected Sub rgHistoriqueFactures_ItemCommand(sender As Object, e As GridCommandEventArgs) Handles rgHistoriqueFactures.ItemCommand
        Select Case e.CommandName
            Case "Refresh"
                ' Recharger les données
                ChargerHistorique()

            Case "AjouterRegleLigne"
                ' Récupérer les paramètres depuis CommandArgument
                ' Format : NumOR|NumFacture|CodePrestaFournisseur|Descr
                Dim args As String() = e.CommandArgument.ToString().Split("|"c)

                If args.Length >= 4 Then
                    Dim numOR As String = args(0)
                    Dim numFacture As String = args(1)
                    Dim codePrestaFournisseur As String = args(2)
                    Dim descr As String = args(3)

                    ' Récupérer le code fournisseur via GestionnaireBddFacture
                    Dim codeFournisseur As String = GestionnaireBddFacture.GetCodeFournisseur(numOR, numFacture)

                    If Not String.IsNullOrEmpty(codeFournisseur) Then
                        ' Construire l'URL avec les paramètres
                        Dim url As String = "FormulaireCorrespondancePopup.aspx?" &
                                           "codeFour=" & Server.UrlEncode(codeFournisseur) &
                                           "&refFour=" & Server.UrlEncode(codePrestaFournisseur) &
                                           "&libelle=" & Server.UrlEncode(descr) &
                                           "&numOR=" & Server.UrlEncode(numOR) &
                                           "&numFac=" & Server.UrlEncode(numFacture) &
                                           "&gridID=" & rgFacturesDemat.ClientID

                        ' Ouvrir la RadWindow
                        rwFormulaireCorrespondance.NavigateUrl = url
                        rwFormulaireCorrespondance.VisibleOnPageLoad = True
                    Else
                        GestionnaireLog.Error("Code fournisseur introuvable pour " & numFacture)
                    End If
                End If

            Case "ValidateSiret"
                Dim args As String() = e.CommandArgument.ToString().Split("|"c)
                If args.Length >= 2 Then
                    Dim numOR As String = args(0)
                    Dim numFacture As String = args(1)

                    Dim dataItem As GridDataItem = CType(e.Item, GridDataItem)
                    Dim txtSiret As TextBox = CType(dataItem.FindControl("txtSiret"), TextBox)

                    If txtSiret IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(txtSiret.Text) Then
                        Dim nouveauSiret As String = txtSiret.Text.Trim()

                        ' Mettre à jour en base
                        GestionnaireBddFacture.UpdateSiret(numOR, numFacture, nouveauSiret)

                        ' Retraiter
                        Dim errorMessage As String = ""
                        Dim succes As Boolean = ServiceReintegration.RetraiterFactureSiret(numOR, numFacture, errorMessage)

                        If succes Then
                            AfficherResultatAction("SIRET validé avec succès.", True, lblResultatReintegration)
                        Else
                            AfficherResultatAction(errorMessage, False, lblResultatReintegration)
                        End If

                        ' Recharger
                        ChargerHistorique()
                        ScriptManager.RegisterStartupScript(Me, Me.GetType(), "RefreshGrid", "setTimeout(function() { refreshRadGrid(); }, 500);", True)
                    End If
                End If

        End Select
    End Sub


    ''' <summary>
    ''' Ré-intégration par lot de toutes les factures EN_ATTENTE
    ''' </summary>
    Protected Sub btnReintegrerTout_Click(sender As Object, e As EventArgs) Handles btnReintegrerTout.Click
        Try
            ' Masquer le résultat précédent
            lblResultatReintegration.Visible = False

            ' Lancer le traitement
            Dim resultat As ServiceReintegration.ResultatLot = ServiceReintegration.ReintegrerFacturesParLot()

            ' Afficher le résultat
            AfficherResultatReintegration(resultat, lblResultatReintegration)
            ChargerHistorique()
            rgFacturesDemat.Rebind()
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "RefreshGrid", "setTimeout(function() { refreshRadGrid(); }, 500);", True)
        Catch ex As Exception
            GestionnaireLog.Error("Erreur bouton ré-intégrer tout : " & ex.ToString())
            lblResultatReintegration.Text = "&#10060; Erreur critique : " & ex.Message
            lblResultatReintegration.ForeColor = System.Drawing.Color.Red
            lblResultatReintegration.Visible = True
        End Try
    End Sub



    ''' <summary>
    ''' Affiche le résultat du traitement par lot
    ''' </summary>
    Private Sub AfficherResultatReintegration(resultat As ServiceReintegration.ResultatLot, targetLabel As Label)
        Dim sb As New StringBuilder()

        ' Titre
        sb.Append("<div style='font-size: 16px; font-weight: bold; margin-bottom: 10px;'>")
        sb.Append("R&Eacute;SULTAT DU TRAITEMENT")
        sb.Append("</div>")

        ' Statistiques
        sb.Append("<div style='margin-bottom: 10px;'>")
        sb.Append("&#9989; Factures int&eacute;gr&eacute;es : <strong>" & resultat.NbSucces & "</strong><br/>")
        sb.Append("&#10060; &Eacute;checs : <strong>" & resultat.NbEchecs & "</strong>")
        sb.Append("</div>")

        ' Details des echecs
        If resultat.NbEchecs > 0 AndAlso resultat.DetailsEchecs.Count > 0 Then
            sb.Append("<div style='margin-top: 15px;'>")
            sb.Append("<strong>D&eacute;tails des &eacute;checs :</strong>")
            sb.Append("<ul style='margin-top: 5px;'>")

            For Each detail As String In resultat.DetailsEchecs
                sb.Append("<li>" & detail & "</li>")
            Next

            sb.Append("</ul>")
            sb.Append("</div>")
        End If

        targetLabel.Text = sb.ToString()
        targetLabel.Visible = True
    End Sub

    ''' <summary>
    ''' Affiche le résultat d'une action unitaire (ex: Validation SIRET)
    ''' </summary>
    Private Sub AfficherResultatAction(message As String, isSuccess As Boolean, targetLabel As Label)
        Dim sb As New StringBuilder()

        ' Titre
        sb.Append("<div style='font-size: 16px; font-weight: bold; margin-bottom: 10px;'>")
        sb.Append("R&Eacute;SULTAT DE L'ACTION")
        sb.Append("</div>")

        ' Message
        sb.Append("<div style='margin-bottom: 10px;'>")
        If isSuccess Then
            sb.Append("&#9989; <strong style='color: green;'>" & message & "</strong>")
        Else
            sb.Append("&#10060; <strong style='color: red;'>Erreur :</strong> <span style='color: red;'>" & message & "</span>")
        End If
        sb.Append("</div>")

        targetLabel.Text = sb.ToString()
        targetLabel.ForeColor = System.Drawing.Color.Empty
        targetLabel.Visible = True
    End Sub


    ''' <summary>
    ''' Affiche l'édition SIRET ou le nom du fournisseur selon le statut
    ''' </summary>
    Private Sub GererEditionFournisseur(item As GridDataItem, statut As String)
        Dim pnlFournisseur As Panel = CType(item.FindControl("pnlFournisseur"), Panel)
        If pnlFournisseur Is Nothing Then Return

        Dim txtSiret As TextBox = CType(item.FindControl("txtSiret"), TextBox)
        Dim btnValiderSiret As RadButton = CType(item.FindControl("btnValiderSiret"), RadButton)
        Dim lblFournisseur As Label = CType(item.FindControl("lblFournisseur"), Label)

        If txtSiret Is Nothing OrElse btnValiderSiret Is Nothing OrElse lblFournisseur Is Nothing Then
            Return
        End If

        ' Statuts qui nécessitent l'édition du SIRET
        Dim statutsEditables As String() = {"SIRET_NON_LU", "FOURNISSEUR_INCONNU", "FOURNISSEUR_INTROUVABLE", "SUPPLIER_NOT_FOUND", "FOURNISSEUR_INEXISTANT"}

        If statutsEditables.Contains(statut.ToUpper()) Then
            txtSiret.Visible = True
            btnValiderSiret.Visible = True
            lblFournisseur.Visible = False
            lblFournisseur.Text = ""
        Else
            txtSiret.Visible = False
            btnValiderSiret.Visible = False
            lblFournisseur.Visible = True
        End If
    End Sub


    ''' <summary>
    ''' Affiche TextBox + Bouton ou Label selon le statut (Immat)
    ''' </summary>
    Private Sub GererEditionImmat(item As GridDataItem, statut As String)
        ' Récupérer les contrôles
        Dim txtImmat As TextBox = CType(item.FindControl("txtImmat"), TextBox)
        Dim btnValiderImmat As RadButton = CType(item.FindControl("btnValiderImmat"), RadButton)
        Dim lblImmat As Label = CType(item.FindControl("lblImmat"), Label)

        If txtImmat Is Nothing OrElse btnValiderImmat Is Nothing OrElse lblImmat Is Nothing Then
            Return
        End If

        ' Statuts qui nécessitent l'édition de l'Immat
        Dim statutsEditables As String() = {"PARC_INTROUVABLE"}

        If statutsEditables.Contains(statut) Then
            ' Mode édition
            txtImmat.Visible = True
            btnValiderImmat.Visible = True
            lblImmat.Visible = False
        Else
            ' Mode lecture seule
            txtImmat.Visible = False
            btnValiderImmat.Visible = False
            lblImmat.Visible = True
        End If
    End Sub


    ''' <summary>
    ''' Trouve une ligne du grid par NumOR et NumFacture
    ''' </summary>
    ''' <param name="numOR">Numéro OR</param>
    ''' <param name="numFacture">Numéro facture</param>
    ''' <returns>GridDataItem ou Nothing si non trouvé</returns>
    Private Function TrouverLigneGrid(numOR As String, numFacture As String) As GridDataItem
        For Each item As GridDataItem In rgHistoriqueFactures.Items
            Dim numORGrid As String = item("NumOR").Text.Trim()
            Dim numFactureGrid As String = item("NumFacture").Text.Trim()

            If numORGrid = numOR AndAlso numFactureGrid = numFacture Then
                Return item
            End If
        Next

        Return Nothing
    End Function

    ''' Événement déclenché lors du chargement des données des factures dématérialisées (En cours)
    Protected Sub rgFacturesDemat_NeedDataSource(sender As Object, e As GridNeedDataSourceEventArgs) Handles rgFacturesDemat.NeedDataSource
        Try
            Dim dt As System.Data.DataTable = GestionnaireBddFacture.getFactureDemat()
            If dt IsNot Nothing Then
                Dim dv As System.Data.DataView = dt.DefaultView
                ' Exclure les factures terminées (PAID = Encaissée)
                dv.RowFilter = "ISNULL(StatutCycleDeVie, '') NOT IN ('PAID', 'paid', 'Encaissée', 'ENCAISSÉE')"
                rgFacturesDemat.DataSource = dv
            Else
                rgFacturesDemat.DataSource = dt
            End If
        Catch ex As Exception
            GestionnaireLog.Error("Erreur lors du chargement des factures dématérialisées (En cours) : " & ex.Message)
        End Try
    End Sub

    ''' Événement déclenché lors du chargement des données des factures dématérialisées (Historique)
    Protected Sub rgFacturesDematHistorique_NeedDataSource(sender As Object, e As GridNeedDataSourceEventArgs) Handles rgFacturesDematHistorique.NeedDataSource
        Try
            Dim dt As System.Data.DataTable = GestionnaireBddFacture.getFactureDemat()
            If dt IsNot Nothing Then
                Dim dv As System.Data.DataView = dt.DefaultView
                ' Inclure uniquement les factures terminées (PAID = Encaissée)
                Dim filterExpression As String = "StatutCycleDeVie IN ('PAID', 'paid', 'Encaissée', 'ENCAISSÉE')"

                ' Appliquer les filtres personnalisés
                If txtFiltreFournisseur IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(txtFiltreFournisseur.Text) Then
                    filterExpression &= " AND RaisonSociale LIKE '%" & txtFiltreFournisseur.Text.Trim().Replace("'", "''") & "%'"
                End If

                If txtFiltreNumFacture IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(txtFiltreNumFacture.Text) Then
                    filterExpression &= " AND NumFacture LIKE '%" & txtFiltreNumFacture.Text.Trim().Replace("'", "''") & "%'"
                End If

                If dpFiltreDateDebut IsNot Nothing AndAlso dpFiltreDateDebut.SelectedDate.HasValue Then
                    Dim dateDebutStr As String = dpFiltreDateDebut.SelectedDate.Value.ToString("MM/dd/yyyy", System.Globalization.CultureInfo.InvariantCulture)
                    filterExpression &= " AND DateFacture >= #" & dateDebutStr & "#"
                End If

                If dpFiltreDateFin IsNot Nothing AndAlso dpFiltreDateFin.SelectedDate.HasValue Then
                    ' On ajoute 1 jour pour inclure la fin de la journée sélectionnée
                    Dim dateFinStr As String = dpFiltreDateFin.SelectedDate.Value.AddDays(1).ToString("MM/dd/yyyy", System.Globalization.CultureInfo.InvariantCulture)
                    filterExpression &= " AND DateFacture < #" & dateFinStr & "#"
                End If

                dv.RowFilter = filterExpression
                rgFacturesDematHistorique.DataSource = dv
            Else
                rgFacturesDematHistorique.DataSource = dt
            End If
        Catch ex As Exception
            GestionnaireLog.Error("Erreur lors du chargement des factures dématérialisées (Historique) : " & ex.Message)
        End Try
    End Sub

    Protected Sub btnFiltrerHistorique_Click(sender As Object, e As EventArgs)
        rgFacturesDematHistorique.Rebind()
    End Sub

    Protected Sub btnReinitialiserFiltres_Click(sender As Object, e As EventArgs)
        If txtFiltreFournisseur IsNot Nothing Then txtFiltreFournisseur.Text = ""
        If txtFiltreNumFacture IsNot Nothing Then txtFiltreNumFacture.Text = ""
        If dpFiltreDateDebut IsNot Nothing Then dpFiltreDateDebut.Clear()
        If dpFiltreDateFin IsNot Nothing Then dpFiltreDateFin.Clear()
        rgFacturesDematHistorique.Rebind()
    End Sub

    ''' Événement déclenché lors du chargement des détails (lignes) des factures dématérialisées dans la vue partagée
    Protected Sub rgLignesInternes_NeedDataSource(sender As Object, e As GridNeedDataSourceEventArgs)
        Try
            Dim innerGrid As RadGrid = CType(sender, RadGrid)
            Dim parentItem As GridNestedViewItem = CType(innerGrid.NamingContainer, GridNestedViewItem)
            Dim idFacture As String = parentItem.ParentItem.GetDataKeyValue("IdFacture").ToString()
            innerGrid.DataSource = GestionnaireBddFacture.getDematFacturesLignes(idFacture)
        Catch ex As Exception
            GestionnaireLog.Error("Erreur lors du chargement des lignes des factures dématérialisées (NestedView) : " & ex.Message)
        End Try
    End Sub

    ''' <summary>
    ''' Événement déclenché ligne par ligne (pour chaque facture dématérialisée) juste avant son affichage à l'écran.
    ''' Il permet de personnaliser dynamiquement l'interface (ex: afficher/masquer la zone de saisie du SIRET)
    ''' en fonction du statut actuel de la facture contenue dans la ligne.
    ''' </summary>
    Protected Sub rgFacturesDemat_ItemDataBound(sender As Object, e As GridItemEventArgs) Handles rgFacturesDemat.ItemDataBound
        If TypeOf e.Item Is GridDataItem Then
            Dim dataItem As GridDataItem = CType(e.Item, GridDataItem)

            If e.Item.OwnerTableView.Name = "MasterTableView" OrElse e.Item.OwnerTableView Is rgFacturesDemat.MasterTableView Then
                Dim lblStatut As Label = CType(dataItem.FindControl("lblStatutDemat"), Label)
                If lblStatut IsNot Nothing Then
                    Dim statut As String = lblStatut.Text.Trim()
                    GererEditionFournisseurDemat(dataItem, statut)
                End If

                ' Filtrer la liste des statuts Cycle de Vie disponibles
                FiltrerStatutsCycleDeVie(dataItem)

            End If

        End If
    End Sub

    ''' <summary>
    ''' Filtre dynamiquement les statuts du cycle de vie en fonction du statut actuel pour ne permettre que certaines transitions.
    ''' </summary>
    Private Sub FiltrerStatutsCycleDeVie(dataItem As GridDataItem)
        Dim ddlStatutCycleDeVie As Telerik.Web.UI.RadDropDownList = CType(dataItem.FindControl("ddlStatutCycleDeVie"), Telerik.Web.UI.RadDropDownList)
        If ddlStatutCycleDeVie IsNot Nothing Then

            Dim currentStatut As String = "IN_PROCESS"
            If dataItem.DataItem IsNot Nothing Then
                Dim drv As System.Data.DataRowView = TryCast(dataItem.DataItem, System.Data.DataRowView)
                If drv IsNot Nothing AndAlso drv.Row.Table.Columns.Contains("StatutCycleDeVie") AndAlso Not IsDBNull(drv("StatutCycleDeVie")) Then
                    Dim dbStatut As String = drv("StatutCycleDeVie").ToString().Trim().ToUpper().Replace(" ", "_").Replace("SUSPENDED", "ON_HOLD")
                    If Not String.IsNullOrEmpty(dbStatut) Then
                        currentStatut = dbStatut
                    End If
                End If
            End If

            Dim itemsToDisable As New List(Of Telerik.Web.UI.DropDownListItem)()

            For Each item As Telerik.Web.UI.DropDownListItem In ddlStatutCycleDeVie.Items
                If item.Value = currentStatut Then Continue For

                Select Case currentStatut
                    Case "IN_PROCESS", "ACKNOWLEDGE"
                        ' Garder tout actif pour IN_PROCESS et ACKNOWLEDGE
                    Case Else
                        ' Pour ON_HOLD, REFUSED, ACCEPTED, c'est "one way"
                        itemsToDisable.Add(item)
                End Select
            Next

            For Each itemToDisable In itemsToDisable
                itemToDisable.Enabled = False
            Next

            ' Si le statut n'est pas IN_PROCESS ou ACKNOWLEDGE, on désactive aussi le contrôle entier pour éviter le clic
            If currentStatut <> "IN_PROCESS" AndAlso currentStatut <> "ACKNOWLEDGE" Then
                ddlStatutCycleDeVie.Enabled = False

                ' Ajouter une infobulle explicative spécifique au statut
                Select Case currentStatut
                    Case "ON_HOLD"
                        ddlStatutCycleDeVie.ToolTip = "La facture est en attente d'une action, d'un avoir ou d'une correction de la part du fournisseur."
                    Case "REFUSED"
                        ddlStatutCycleDeVie.ToolTip = "La facture a été refusée manuellement. En attente d'un avoir et/ou d'une nouvelle facture."
                    Case "ACCEPTED"
                        ddlStatutCycleDeVie.ToolTip = "La facture a été approuvée, son paiement est validé. Elle pourra ensuite être comptabilisée."
                    Case Else
                        ddlStatutCycleDeVie.ToolTip = "Ce statut est définitif et ne peut plus être modifié manuellement."
                End Select
            End If
        End If
    End Sub

    ''' <summary>
    ''' Événement déclenché pour chaque ligne de détail (prestation) du sous-tableau d'une facture dématérialisée.
    ''' Il vérifie si le code prestation LocPro est manquant et affiche dynamiquement le bouton "+ Créer une règle" 
    ''' pour permettre à l'utilisateur de lier l'article du fournisseur à un code interne.
    ''' </summary>
    Protected Sub rgLignesInternes_ItemDataBound(sender As Object, e As GridItemEventArgs)
        If TypeOf e.Item Is GridDataItem Then
            Dim dataItem As GridDataItem = CType(e.Item, GridDataItem)
            Dim lblCodePrestaLP As RadLabel = CType(dataItem.FindControl("lblCodePrestaLPDemat"), RadLabel)
            Dim btnAjouterRegle As RadButton = CType(dataItem.FindControl("btnAjouterRegleDemat"), RadButton)

            ' --- Gestion Réf. Fournisseur ---
            Dim txtRefFournisseur As TextBox = CType(dataItem.FindControl("txtRefFournisseur"), TextBox)
            Dim btnValiderRefFournisseur As RadButton = CType(dataItem.FindControl("btnValiderRefFournisseur"), RadButton)
            Dim lblRefFournisseur As Label = CType(dataItem.FindControl("lblRefFournisseur"), Label)

            Dim refFournisseur As String = ""
            If lblRefFournisseur IsNot Nothing Then
                refFournisseur = lblRefFournisseur.Text.Trim()
            End If

            Dim editControlsContainer As HtmlGenericControl = CType(dataItem.FindControl("editControlsContainer"), HtmlGenericControl)

            If String.IsNullOrEmpty(refFournisseur) Then
                If editControlsContainer IsNot Nothing Then editControlsContainer.Style("display") = "inline-block"
                If lblRefFournisseur IsNot Nothing Then lblRefFournisseur.Style("display") = "none"
            Else
                If editControlsContainer IsNot Nothing Then editControlsContainer.Style("display") = "none"
                If lblRefFournisseur IsNot Nothing Then lblRefFournisseur.Style("display") = "inline-block"
            End If
            ' -------------------------------

            If lblCodePrestaLP IsNot Nothing AndAlso btnAjouterRegle IsNot Nothing Then
                Dim codePrestaLP As String = lblCodePrestaLP.Text.Trim()
                codePrestaLP = codePrestaLP.Replace("&nbsp;", "").Trim()

                If String.IsNullOrEmpty(codePrestaLP) AndAlso dataItem.DataItem IsNot Nothing Then
                    Try
                        Dim drv As System.Data.DataRowView = CType(dataItem.DataItem, System.Data.DataRowView)
                        Dim numOR As String = If(IsDBNull(drv("NumOR")), "", drv("NumOR").ToString())
                        Dim numFacture As String = If(IsDBNull(drv("NumFacture")), "", drv("NumFacture").ToString())
                        Dim codePrestaFournisseur As String = ""
                        Dim codeFournisseur As String = ""

                        ' PRIORITE 1: Lecture depuis l'interface (Label parent) pour garantir la cohérence
                        Try
                            Dim parentItem As GridDataItem = CType(dataItem.OwnerTableView.ParentItem, GridDataItem)
                            If parentItem IsNot Nothing Then
                                Dim lblCodeFournisseurParent As Label = CType(parentItem.FindControl("lblCodeFournisseur"), Label)
                                If lblCodeFournisseurParent IsNot Nothing AndAlso Not String.IsNullOrEmpty(lblCodeFournisseurParent.Text.Trim()) Then
                                    codeFournisseur = lblCodeFournisseurParent.Text.Trim()
                                End If
                            End If
                        Catch ex As Exception
                            ' En cas d'erreur, on fallback sur la BDD
                        End Try

                        ' Fallback BDD si l'interface est vide
                        If String.IsNullOrEmpty(codeFournisseur) Then
                            If drv.Row.Table.Columns.Contains("CodeFournisseurLocpro") AndAlso Not IsDBNull(drv("CodeFournisseurLocpro")) Then
                                codeFournisseur = drv("CodeFournisseurLocpro").ToString().Trim()
                            ElseIf drv.Row.Table.Columns.Contains("CodeFournisseur") AndAlso Not IsDBNull(drv("CodeFournisseur")) Then
                                codeFournisseur = drv("CodeFournisseur").ToString().Trim()
                            End If

                            If String.IsNullOrEmpty(codeFournisseur) Then
                                codeFournisseur = GestionnaireBddFacture.GetCodeFournisseur(numOR, numFacture)
                            End If
                        End If

                        ' Utilisation de la référence affichée dans l'interface si elle existe, sinon BDD
                        If Not String.IsNullOrEmpty(refFournisseur) Then
                            codePrestaFournisseur = refFournisseur
                        Else
                            codePrestaFournisseur = If(IsDBNull(drv("CodePrestaFournisseur")), "", drv("CodePrestaFournisseur").ToString())
                        End If

                        ' Injecter le code fournisseur dans le CommandArgument pour le JS (openPopupFromBtn)
                        If btnAjouterRegle IsNot Nothing Then
                            Dim descr As String = If(IsDBNull(drv("Descr")), "", drv("Descr").ToString())
                            btnAjouterRegle.CommandArgument = numOR & "|" & numFacture & "|" & codePrestaFournisseur & "|" & codeFournisseur & "|" & descr
                        End If

                        If Not String.IsNullOrEmpty(codeFournisseur) Then
                            Dim regleJson As Newtonsoft.Json.Linq.JObject = GestionnaireBddFacture.GetRegleCorrespondance(codeFournisseur, codePrestaFournisseur)
                            If regleJson IsNot Nothing Then
                                If regleJson("prestations") IsNot Nothing Then
                                    Dim codes As New List(Of String)()
                                    For Each p In regleJson("prestations")
                                        codes.Add(p("code").ToString())
                                    Next
                                    codePrestaLP = String.Join(", ", codes)
                                    lblCodePrestaLP.Text = codePrestaLP
                                End If
                            Else
                                GestionnaireLog.Error("GetRegleCorrespondance returned Nothing for CodeFournisseur=" & codeFournisseur & " CodePrestaFournisseur=" & codePrestaFournisseur)
                            End If
                        Else
                            GestionnaireLog.Error("GetCodeFournisseur returned empty for numOR=" & numOR & " numFacture=" & numFacture)
                        End If
                    Catch ex As Exception
                        GestionnaireLog.Error("Erreur dans ItemDataBound Demat (lignes) : " & ex.Message & " - " & ex.StackTrace)
                    End Try
                End If

                If String.IsNullOrEmpty(codePrestaLP) Then
                    btnAjouterRegle.Visible = True
                Else
                    btnAjouterRegle.Visible = False
                End If
            End If
        End If
    End Sub

    Protected Sub rgLignesInternes_ItemCommand(sender As Object, e As GridCommandEventArgs)
        Select Case e.CommandName
            Case "ValidateRefFournisseur"
                Dim argsRef As String() = e.CommandArgument.ToString().Split("|"c)
                If argsRef.Length >= 2 Then
                    Dim idFacture As String = argsRef(0)
                    Dim numLig As String = argsRef(1)

                    Dim dataItem As GridDataItem = CType(e.Item, GridDataItem)
                    Dim txtRefFournisseur As TextBox = CType(dataItem.FindControl("txtRefFournisseur"), TextBox)

                    If txtRefFournisseur IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(txtRefFournisseur.Text) Then
                        Dim nouvelleRef As String = txtRefFournisseur.Text.Trim()

                        GestionnaireBddFacture.UpdateRefFournisseurLigne(idFacture, numLig, nouvelleRef)

                        ' RE-VÉRIFIER pour mettre à jour le statut de la facture en direct !
                        ServiceReintegration.VerifierFacturesDematParLot()

                        Dim lblResultat As Label = CType(Me.FindControl("lblResultatReintegrationDemat"), Label)
                        If lblResultat IsNot Nothing Then
                            AfficherResultatAction("Référence mise à jour avec succès.", True, lblResultat)
                        End If

                        ChargerHistorique()

                        ' Mettre à jour l'affichage de cette ligne spécifiquement SANS faire de Rebind global
                        Dim lblRefFournisseur As Label = CType(dataItem.FindControl("lblRefFournisseur"), Label)
                        Dim editControlsContainer As HtmlGenericControl = CType(dataItem.FindControl("editControlsContainer"), HtmlGenericControl)

                        If lblRefFournisseur IsNot Nothing Then
                            lblRefFournisseur.Text = nouvelleRef
                            lblRefFournisseur.Style("display") = "inline-block"
                        End If
                        If editControlsContainer IsNot Nothing Then
                            editControlsContainer.Style("display") = "none"
                        End If

                        ' Mise à jour de la correspondance LocPro en direct
                        Dim lblCodePrestaLP As Telerik.Web.UI.RadLabel = CType(dataItem.FindControl("lblCodePrestaLP"), Telerik.Web.UI.RadLabel)
                        If lblCodePrestaLP Is Nothing Then
                            lblCodePrestaLP = CType(dataItem.FindControl("lblCodePrestaLPDemat"), Telerik.Web.UI.RadLabel)
                        End If

                        Dim btnAjouterRegle As Telerik.Web.UI.RadButton = CType(dataItem.FindControl("btnAjouterRegle"), Telerik.Web.UI.RadButton)
                        If btnAjouterRegle Is Nothing Then
                            btnAjouterRegle = CType(dataItem.FindControl("btnAjouterRegleDemat"), Telerik.Web.UI.RadButton)
                        End If

                        If lblCodePrestaLP IsNot Nothing AndAlso btnAjouterRegle IsNot Nothing Then
                            Dim codeFournisseur As String = ""
                            Dim numOR As String = ""
                            Dim numFacture As String = ""
                            Dim descr As String = ""

                            If dataItem("Descr") IsNot Nothing Then
                                descr = dataItem("Descr").Text.Replace("&nbsp;", "").Trim()
                            End If

                            ' Extraction fiable des clés via DataKeyValues (définis dans DataKeyNames="NumOR,NumFacture,NumLig")
                            If dataItem.OwnerTableView.DataKeyNames.Contains("NumOR") Then
                                numOR = dataItem.GetDataKeyValue("NumOR").ToString()
                            End If
                            If dataItem.OwnerTableView.DataKeyNames.Contains("NumFacture") Then
                                numFacture = dataItem.GetDataKeyValue("NumFacture").ToString()
                            End If

                            ' Récupération du code fournisseur depuis la base de données (Historique)
                            codeFournisseur = GestionnaireBddFacture.GetCodeFournisseur(numOR, numFacture)

                            ' Si introuvable (ex: facture Demat), récupération via SIRET/SIREN
                            If String.IsNullOrEmpty(codeFournisseur) Then
                                Dim dtFact = GestionnaireBddFacture.GetFactureDematById(idFacture)
                                If dtFact IsNot Nothing AndAlso dtFact.Rows.Count > 0 Then
                                    Dim siret As String = dtFact.Rows(0)("Siret_Vend").ToString().Replace(" ", "").Trim()
                                    Dim siren As String = dtFact.Rows(0)("Siren_Vend").ToString().Replace(" ", "").Trim()

                                    Dim dtFourn = GestionnaireBddFacture.RechercherFournisseurParSiretOuSiren(siret, "")
                                    If dtFourn IsNot Nothing AndAlso dtFourn.Rows.Count > 0 Then
                                        codeFournisseur = dtFourn.Rows(0)("F050KY").ToString().Trim()
                                    Else
                                        dtFourn = GestionnaireBddFacture.RechercherFournisseurParSiretOuSiren("", siren)
                                        If dtFourn IsNot Nothing AndAlso dtFourn.Rows.Count > 0 Then
                                            codeFournisseur = dtFourn.Rows(0)("F050KY").ToString().Trim()
                                        End If
                                    End If
                                End If
                            End If

                            btnAjouterRegle.CommandArgument = numOR & "|" & numFacture & "|" & nouvelleRef & "|" & codeFournisseur & "|" & descr

                            Dim codePrestaLP As String = ""
                            If Not String.IsNullOrEmpty(codeFournisseur) Then
                                Dim regleJson As Newtonsoft.Json.Linq.JObject = GestionnaireBddFacture.GetRegleCorrespondance(codeFournisseur, nouvelleRef)
                                If regleJson IsNot Nothing AndAlso regleJson("prestations") IsNot Nothing Then
                                    Dim codes As New List(Of String)()
                                    For Each p In regleJson("prestations")
                                        codes.Add(p("code").ToString())
                                    Next
                                    codePrestaLP = String.Join(", ", codes)
                                End If
                            End If

                            lblCodePrestaLP.Text = codePrestaLP
                            If String.IsNullOrEmpty(codePrestaLP) Then
                                btnAjouterRegle.Visible = True
                            Else
                                btnAjouterRegle.Visible = False
                            End If
                        End If

                    End If
                End If

            Case "AjouterRegleLigne"
                Dim args As String() = e.CommandArgument.ToString().Split("|"c)

                If args.Length >= 4 Then
                    Dim numOR As String = args(0)
                    Dim numFacture As String = args(1)
                    Dim codePrestaFournisseur As String = args(2)
                    Dim codeFournisseur As String = args(3)
                    Dim descr As String = ""
                    If args.Length >= 5 Then
                        descr = String.Join("|", args.Skip(4).ToArray())
                    End If

                    If String.IsNullOrEmpty(codeFournisseur) Then
                        Dim siret As String = ""
                        Try
                            Dim innerGrid As RadGrid = CType(sender, RadGrid)
                            Dim parentItem As GridNestedViewItem = CType(innerGrid.NamingContainer, GridNestedViewItem)
                            Dim idFacture As String = parentItem.ParentItem.GetDataKeyValue("IdFacture").ToString()

                            Dim dtFacture = GestionnaireBddFacture.getFactureDemat()
                            Dim rows = dtFacture.Select("IdFacture = '" & idFacture.Replace("'", "''") & "'")
                            If rows.Length > 0 Then
                                siret = rows(0)("NumeroTVA_Vend").ToString().Trim()
                            End If

                            If Not String.IsNullOrEmpty(siret) Then
                                Dim infosFour = ServiceOR.retournerInfosFournisseur(siret)
                                If infosFour IsNot Nothing Then
                                    codeFournisseur = infosFour.CodeFournisseur
                                End If
                            End If
                        Catch ex As Exception
                            GestionnaireLog.Error("Erreur récupération SIRET (ItemCommand) : " & ex.Message)
                        End Try
                    End If

                    If String.IsNullOrEmpty(codeFournisseur) Then
                        codeFournisseur = GestionnaireBddFacture.GetCodeFournisseur(numOR, numFacture)
                    End If

                    Dim url As String = "FormulaireCorrespondancePopup.aspx?" &
                                       "codeFour=" & Server.UrlEncode(codeFournisseur) &
                                       "&refFour=" & Server.UrlEncode(codePrestaFournisseur) &
                                       "&libelle=" & Server.UrlEncode(descr) &
                                       "&numOR=" & Server.UrlEncode(numOR) &
                                       "&numFac=" & Server.UrlEncode(numFacture) &
                                       "&gridID=" & rgFacturesDemat.ClientID

                    rwFormulaireCorrespondance.NavigateUrl = url
                    rwFormulaireCorrespondance.VisibleOnPageLoad = True
                End If
        End Select
    End Sub

    ' Pour vérifier la validité du siret saisie
    Protected Sub rgFacturesDemat_ItemCommand(sender As Object, e As GridCommandEventArgs) Handles rgFacturesDemat.ItemCommand, rgFacturesDematHistorique.ItemCommand
        Select Case e.CommandName
            Case "Refresh"
                ' Lancé typiquement par la fermeture du popup pour réévaluer les prestations/fournisseurs
                ServiceReintegration.VerifierFacturesDematParLot()
                CType(sender, RadGrid).Rebind()

            Case "Comptabiliser"
                Dim idFacture As String = e.CommandArgument.ToString()
                Dim dt As System.Data.DataTable = GestionnaireBddFacture.GetFactureDematById(idFacture)

                If dt IsNot Nothing AndAlso dt.Rows.Count > 0 Then
                    Dim statutActuel As String = ""
                    If Not IsDBNull(dt.Rows(0)("Statut")) Then
                        statutActuel = dt.Rows(0)("Statut").ToString().ToUpper()
                    End If

                    Dim lblIntegrationResult As Label = CType(rwIntegrationResult.ContentContainer.FindControl("lblIntegrationResult"), Label)
                    Dim ctrl As Control = CType(sender, Control)
                    Dim scriptOpen As String = "setTimeout(function(){ var w = $find('" & rwIntegrationResult.ClientID & "'); if(w) w.show(); }, 100);"

                    ' Anti-refresh F5 : on récupère le token envoyé par le client
                    Dim currentToken As String = ""
                    For Each key As String In Request.Form.AllKeys
                        If key IsNot Nothing AndAlso key.EndsWith("hfActionToken") Then
                            currentToken = Request.Form(key)
                            Exit For
                        End If
                    Next

                    Dim sessionKeyToken As String = "ActionToken_" & idFacture
                    Dim isRealClick As Boolean = (Session(sessionKeyToken) Is Nothing OrElse Session(sessionKeyToken).ToString() <> currentToken)

                    ' Si la facture est déjà intégrée
                    If statutActuel = "SUCCES" OrElse statutActuel = "INTEGREE" Then
                        If Not isRealClick Then
                            ' F5 ou rafraîchissement silencieux : on ignore
                            CType(sender, RadGrid).Rebind()
                            Exit Select
                        Else
                            ' Vrai clic : on affiche le message
                            Session(sessionKeyToken) = currentToken
                            lblIntegrationResult.Text = "<span style='color: #FF9800;'>Cette facture a déjà été intégrée dans LocPro.</span>"
                            ScriptManager.RegisterStartupScript(ctrl, ctrl.GetType(), "showIntResult", scriptOpen, True)
                            CType(sender, RadGrid).Rebind()
                            Exit Select
                        End If
                    End If

                    Session(sessionKeyToken) = currentToken

                    Dim resultat = ServiceReintegration.ReintegrerFactureDemat(dt.Rows(0))

                    If resultat.Succes Then
                        lblIntegrationResult.Text = "<span style='color: #4CAF50;'>La facture a été intégrée avec succès dans LocPro.</span>"
                    Else
                        lblIntegrationResult.Text = "<span style='color: #F44336;'>Erreur lors de l'intégration : " & resultat.Message.Replace("'", "&apos;") & "</span>"
                    End If
                    ScriptManager.RegisterStartupScript(ctrl, ctrl.GetType(), "showIntResult", scriptOpen, True)
                    CType(sender, RadGrid).Rebind()
                End If

            Case "ValidateSiren"
                Dim idFacture As String = e.CommandArgument.ToString()

                Dim dataItem As GridDataItem = CType(e.Item, GridDataItem)
                Dim txtSiren As TextBox = CType(dataItem.FindControl("txtSirenDemat"), TextBox)

                If txtSiren IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(txtSiren.Text) Then
                    Dim nouveauSiren As String = txtSiren.Text.Trim()

                    ' Validation : 9 ou 14 chiffres uniquement
                    If Not IsNumeric(nouveauSiren) OrElse (nouveauSiren.Length <> 9 AndAlso nouveauSiren.Length <> 14) Then
                        Dim lblSiretMsgErr As Label = CType(dataItem.FindControl("lblSiretMsg"), Label)
                        If lblSiretMsgErr IsNot Nothing Then
                            lblSiretMsgErr.Text = "Erreur: 9 ou 14 chiffres requis."
                            lblSiretMsgErr.ForeColor = System.Drawing.Color.Red
                            lblSiretMsgErr.Visible = True
                        End If
                        txtSiren.Style("border") = "2px solid red"
                        Exit Select
                    End If

                    ' Retraiter et vérifier dans LocPro
                    Dim errorMessage As String = ""
                    Dim succes As Boolean = ServiceReintegration.RetraiterFactureSiretDemat(idFacture, nouveauSiren, errorMessage)

                    If succes Then
                        ' Mettre à jour en base UNIQUEMENT si le fournisseur existe dans LocPro
                        GestionnaireBddFacture.UpdateSirenDemat(idFacture, nouveauSiren)

                        txtSiren.Style("border") = ""

                        Dim lblSiretMsgSuccess As Label = CType(dataItem.FindControl("lblSiretMsg"), Label)
                        If lblSiretMsgSuccess IsNot Nothing Then
                            lblSiretMsgSuccess.Visible = False
                            lblSiretMsgSuccess.Text = ""
                        End If

                        ' Mettre à jour l'affichage en ligne SANS Rebind
                        Dim lblSirenText As Label = CType(dataItem.FindControl("lblSirenText"), Label)
                        If lblSirenText Is Nothing Then
                            lblSirenText = CType(dataItem.FindControl("lblSirenTextDemat"), Label)
                        End If
                        Dim editSirenContainer As HtmlGenericControl = CType(dataItem.FindControl("editSirenContainer"), HtmlGenericControl)
                        If editSirenContainer Is Nothing Then
                            editSirenContainer = CType(dataItem.FindControl("editSirenContainerDemat"), HtmlGenericControl)
                        End If

                        If lblSirenText IsNot Nothing Then
                            lblSirenText.Text = nouveauSiren
                            lblSirenText.Style("display") = "inline-block"
                        End If
                        If editSirenContainer IsNot Nothing Then
                            editSirenContainer.Style("display") = "none"
                        End If

                        ' Mettre à jour le nom et code fournisseur (s'ils existent dans la ligne)
                        Dim dtFourn = GestionnaireBddFacture.RechercherFournisseurParSiretOuSiren(nouveauSiren, nouveauSiren)
                        If dtFourn IsNot Nothing AndAlso dtFourn.Rows.Count > 0 Then
                            Dim nomLocPro As String = dtFourn.Rows(0)("F050NOM").ToString().Trim()
                            Dim codeLocPro As String = dtFourn.Rows(0)("F050KY").ToString().Trim()

                            Dim cellFournisseur As TableCell = dataItem("ColRaisonSociale")
                            If cellFournisseur IsNot Nothing Then
                                Dim lblFournisseur As Label = CType(cellFournisseur.FindControl("lblFournisseur"), Label)
                                If lblFournisseur IsNot Nothing Then
                                    lblFournisseur.Text = nomLocPro
                                    lblFournisseur.Visible = True
                                End If
                            End If

                            Dim cellCodeFournisseur As TableCell = Nothing
                            If dataItem.OwnerTableView.Columns.FindByUniqueNameSafe("ColCodeFournisseur") IsNot Nothing Then
                                cellCodeFournisseur = dataItem("ColCodeFournisseur")
                            End If
                            If cellCodeFournisseur IsNot Nothing Then
                                Dim lblCodeFournisseur As Label = CType(cellCodeFournisseur.FindControl("lblCodeFournisseur"), Label)
                                If lblCodeFournisseur IsNot Nothing Then
                                    lblCodeFournisseur.Text = codeLocPro
                                    lblCodeFournisseur.Visible = True
                                End If
                            End If
                        End If

                        ' Recharger les lignes internes pour propager le nouveau code fournisseur (pour le popup etc.)
                        Dim nestedViewItem As GridNestedViewItem = CType(dataItem.ChildItem, GridNestedViewItem)
                        If nestedViewItem IsNot Nothing Then
                            Dim rgLignesInternes As RadGrid = CType(nestedViewItem.FindControl("rgLignesInternes"), RadGrid)
                            If rgLignesInternes IsNot Nothing Then
                                rgLignesInternes.Rebind()
                            End If
                        End If

                        ' Mettre à jour le statut de la facture sur l'UI
                        Dim dtFacture = GestionnaireBddFacture.GetFactureDematById(idFacture)
                        If dtFacture IsNot Nothing AndAlso dtFacture.Rows.Count > 0 Then
                            Dim newStatut As String = dtFacture.Rows(0)("Statut").ToString()
                            Dim lblStatut As Label = CType(dataItem.FindControl("lblStatutDemat"), Label)
                            If lblStatut Is Nothing Then
                                lblStatut = CType(dataItem.FindControl("lblStatut"), Label)
                            End If
                            If lblStatut IsNot Nothing Then
                                lblStatut.Text = newStatut
                                lblStatut.CssClass = "statut-badge statut-" & newStatut.ToLower()
                            End If
                        End If
                    Else
                        Dim lblSiretMsg As Label = CType(dataItem.FindControl("lblSiretMsg"), Label)
                        If lblSiretMsg IsNot Nothing Then
                            lblSiretMsg.Text = "Erreur: " & errorMessage
                            lblSiretMsg.ForeColor = System.Drawing.Color.Red
                            lblSiretMsg.Visible = True
                        End If
                        txtSiren.Style("border") = "2px solid red"
                    End If
                End If

        End Select
    End Sub

    ''' <summary>
    ''' Affiche l'édition SIREN et/ou le nom du fournisseur selon la résolution dans LocPro pour Demat
    ''' </summary>
    Private Sub GererEditionFournisseurDemat(item As GridDataItem, statut As String)
        Try
            Dim pnlFournisseur As Panel = CType(item.FindControl("pnlFournisseur"), Panel)
            If pnlFournisseur Is Nothing Then Return

            Dim txtSiren As TextBox = CType(item.FindControl("txtSirenDemat"), TextBox)
            Dim btnValiderSiren As RadButton = CType(item.FindControl("btnValiderSirenDemat"), RadButton)

            Dim lblFournisseur As Label = Nothing
            Dim cellFournisseur As TableCell = item("ColRaisonSociale")
            If cellFournisseur IsNot Nothing Then
                lblFournisseur = CType(cellFournisseur.FindControl("lblFournisseur"), Label)
            End If

            Dim lblCodeFournisseur As Label = Nothing
            Dim cellCodeFournisseur As TableCell = Nothing
            If item.OwnerTableView.Columns.FindByUniqueNameSafe("ColCodeFournisseur") IsNot Nothing Then
                cellCodeFournisseur = item("ColCodeFournisseur")
            End If
            If cellCodeFournisseur IsNot Nothing Then
                lblCodeFournisseur = CType(cellCodeFournisseur.FindControl("lblCodeFournisseur"), Label)
            End If

            If txtSiren Is Nothing OrElse btnValiderSiren Is Nothing Then
                Return
            End If

            Dim sirenBase As String = ""
            Dim siretBase As String = ""

            If item.DataItem IsNot Nothing Then
                Dim drv As System.Data.DataRowView = TryCast(item.DataItem, System.Data.DataRowView)
                If drv IsNot Nothing Then
                    If drv.Row.Table.Columns.Contains("Siren") AndAlso Not IsDBNull(drv("Siren")) Then
                        sirenBase = drv("Siren").ToString().Replace(" ", "").Trim()
                    End If
                    If drv.Row.Table.Columns.Contains("Siret_Vend") AndAlso Not IsDBNull(drv("Siret_Vend")) Then
                        siretBase = drv("Siret_Vend").ToString().Replace(" ", "").Trim()
                    End If
                End If
            End If

            ' On vérifie si le fournisseur existe dans LocPro en testant d'abord le SIRET puis le SIREN
            Dim existeDansLocPro As Boolean = False
            Dim nomFournisseurLocPro As String = ""
            Dim codeFournisseurLocPro As String = ""
            Dim valeurAffichage As String = ""

            Dim dtFourn = GestionnaireBddFacture.RechercherFournisseurParSiretOuSiren(siretBase, "")
            If dtFourn IsNot Nothing AndAlso dtFourn.Rows.Count > 0 Then
                existeDansLocPro = True
                valeurAffichage = siretBase
                codeFournisseurLocPro = dtFourn.Rows(0)("F050KY").ToString().Trim()
                nomFournisseurLocPro = dtFourn.Rows(0)("F050NOM").ToString().Trim()
            Else
                dtFourn = GestionnaireBddFacture.RechercherFournisseurParSiretOuSiren("", sirenBase)
                If dtFourn IsNot Nothing AndAlso dtFourn.Rows.Count > 0 Then
                    existeDansLocPro = True
                    valeurAffichage = sirenBase
                    codeFournisseurLocPro = dtFourn.Rows(0)("F050KY").ToString().Trim()
                    nomFournisseurLocPro = dtFourn.Rows(0)("F050NOM").ToString().Trim()
                End If
            End If

            If Not existeDansLocPro Then
                ' Aucun ne correspond, on affiche le SIRET en priorité, ou le SIREN
                valeurAffichage = siretBase
                If String.IsNullOrEmpty(valeurAffichage) OrElse valeurAffichage.ToLower() = "null" Then
                    valeurAffichage = sirenBase
                End If
            End If

            If String.IsNullOrEmpty(valeurAffichage) Then
                txtSiren.Text = ""
            Else
                txtSiren.Text = valeurAffichage
            End If

            Dim lblSirenText As Label = CType(item.FindControl("lblSirenText"), Label)
            If lblSirenText Is Nothing Then
                lblSirenText = CType(item.FindControl("lblSirenTextDemat"), Label)
            End If

            Dim editSirenContainer As HtmlGenericControl = CType(item.FindControl("editSirenContainer"), HtmlGenericControl)
            If editSirenContainer Is Nothing Then
                editSirenContainer = CType(item.FindControl("editSirenContainerDemat"), HtmlGenericControl)
            End If

            If existeDansLocPro Then
                ' Fournisseur trouvé : On affiche le SIREN/SIRET en texte plat et on cache l'input
                If lblSirenText IsNot Nothing Then
                    lblSirenText.Text = valeurAffichage
                    lblSirenText.Style("display") = "inline-block"
                End If
                If editSirenContainer IsNot Nothing Then editSirenContainer.Style("display") = "none"

                ' La colonne NOM FOURNISSEUR affiche Nom Fournisseur Locpro
                If lblFournisseur IsNot Nothing Then
                    lblFournisseur.Text = nomFournisseurLocPro
                    lblFournisseur.Visible = True
                End If

                ' La colonne CODE FRN affiche le Code Fournisseur Locpro
                If lblCodeFournisseur IsNot Nothing Then
                    lblCodeFournisseur.Text = codeFournisseurLocPro
                    lblCodeFournisseur.Visible = True
                End If
            Else
                ' Fournisseur introuvable : On affiche l'input avec le SIREN dedans, et on VIDE le nom et le code
                If lblSirenText IsNot Nothing Then
                    lblSirenText.Style("display") = "none"
                End If
                If editSirenContainer IsNot Nothing Then editSirenContainer.Style("display") = "inline-block"

                If lblFournisseur IsNot Nothing Then
                    lblFournisseur.Visible = False
                    lblFournisseur.Text = ""
                End If

                If lblCodeFournisseur IsNot Nothing Then
                    lblCodeFournisseur.Visible = False
                    lblCodeFournisseur.Text = ""
                End If

                ' Vider la cellule FOURNISSEUR elle-même si jamais le text est directement sur la cellule
                Dim cellF As TableCell = item("ColRaisonSociale")
                If cellF IsNot Nothing Then
                    cellF.Text = "&nbsp;"
                End If

                If cellCodeFournisseur IsNot Nothing Then
                    cellCodeFournisseur.Text = "&nbsp;"
                End If
            End If
        Catch ex As Exception
            Dim txtSirenErr As TextBox = CType(item.FindControl("txtSirenDemat"), TextBox)
            If txtSirenErr IsNot Nothing Then
                txtSirenErr.Text = "ERR"
                txtSirenErr.ToolTip = ex.Message
            End If
        End Try
    End Sub

    Protected Sub ddlStatutCycleDeVie_SelectedIndexChanged(sender As Object, e As DropDownListEventArgs)
        Dim ddl As RadDropDownList = CType(sender, RadDropDownList)
        Dim item As GridDataItem = CType(ddl.NamingContainer, GridDataItem)

        Dim hdnIdFactureCycle As HiddenField = CType(item.FindControl("hdnIdFactureCycle"), HiddenField)
        If hdnIdFactureCycle IsNot Nothing Then
            Dim idFacture As String = hdnIdFactureCycle.Value
            Dim nouveauStatut As String = ddl.SelectedValue

            ' Préparer la popup
            hdnPopupIdFacture.Value = idFacture
            hdnPopupNouveauStatut.Value = nouveauStatut

            txtStatutNote.Text = ""
            txtStatutRejectionMessage.Text = ""
            ddlStatutRejectionCode.SelectedIndex = 0
            ddlStatutExpectedAction.SelectedIndex = 0

            ' Afficher ou masquer les détails de rejet en fonction du statut
            If nouveauStatut = "REFUSED" OrElse nouveauStatut = "UNDER_QUERY" OrElse nouveauStatut = "ON_HOLD" Then
                divRejectionDetails.Visible = True
            Else
                divRejectionDetails.Visible = False
            End If

            ' Ouvrir la popup
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "OpenStatutPopup", "openStatutPopup();", True)
        End If
    End Sub

    Protected Async Sub btnValiderChangementStatut_Click(sender As Object, e As EventArgs)
        Dim idFacture As String = hdnPopupIdFacture.Value.Trim()
        Dim nouveauStatut As String = hdnPopupNouveauStatut.Value.Trim()
        Dim note As String = txtStatutNote.Text.Trim()

        Dim rejectionCode As String = ""
        Dim rejectionMessage As String = ""
        Dim expectedAction As String = ""

        If divRejectionDetails.Visible Then
            rejectionCode = ddlStatutRejectionCode.SelectedValue
            rejectionMessage = txtStatutRejectionMessage.Text.Trim()
            expectedAction = ddlStatutExpectedAction.SelectedValue
        End If

        Try
            ' 1. Appel API Maileva pour mettre à jour le statut
            Dim mailevaService As New Services.MailevaApiService()
            Await mailevaService.MettreAJourStatutCycleDeVieAsync(idFacture, nouveauStatut, note, rejectionCode, rejectionMessage, expectedAction)

            ' 2. Mise à jour dans la base de données D_invoice
            GestionnaireBddFacture.UpdateStatutCycleDeVie(idFacture, nouveauStatut)

            ' Message de succès global
            lbl_result.Text = "Statut Maileva mis à jour avec succès en " & nouveauStatut & "."
            lbl_result.ForeColor = System.Drawing.Color.Green

            Dim messageJS As String = "alert('Statut Maileva mis à jour avec succès en " & nouveauStatut & "');"
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "alertSuccess", messageJS, True)

            ' Rafraichir la grille pour refléter l'état
            rgFacturesDemat.Rebind()

            ' Fermer la popup
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "CloseStatutPopup", "closeStatutPopup();", True)

        Catch ex As Exception
            GestionnaireLog.Error("Erreur lors de la mise à jour du statut cycle de vie pour la facture " & idFacture & " : " & ex.Message)

            Dim safeErrorMsg As String = ex.Message
            If safeErrorMsg.Contains("<html") OrElse safeErrorMsg.Contains("503") OrElse safeErrorMsg.Contains("502") Then
                safeErrorMsg = "Service indisponible (Erreur serveur API Maileva)."
            ElseIf safeErrorMsg.Length > 150 Then
                safeErrorMsg = safeErrorMsg.Substring(0, 150) & "..."
            End If

            lbl_result.Text = "Erreur lors de la mise à jour du statut Maileva : " & Server.HtmlEncode(safeErrorMsg)
            lbl_result.ForeColor = System.Drawing.Color.Red

            Dim errorJS As String = "alert('Erreur: " & safeErrorMsg.Replace("'", "\'") & "');"
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "alertError", errorJS, True)
        End Try
    End Sub


End Class
