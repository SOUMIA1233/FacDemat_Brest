Imports System.IO
Imports Telerik.Web.UI
Imports System

Partial Class integrationFactures
    Inherits System.Web.UI.Page

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Load
        rwFormulaireCorrespondance.VisibleOnPageLoad = False

        If Not IsPostBack Then
            ChargerHistorique()
        End If
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

            Dim sourceImport As String = "TestLocal" ' À remplacer par l'utilisateur connecté

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
                
                ' Filtrer la liste des statuts Cycle de Vie disponibles
                FiltrerStatutsCycleDeVie(dataItem)
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
                                           "&numFac=" & Server.UrlEncode(numFacture)

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

    Protected Sub btnReintegrerToutDemat_Click(sender As Object, e As EventArgs) Handles btnReintegrerToutDemat.Click
        Try
            ' Masquer le résultat précédent
            lblResultatReintegrationDemat.Visible = False

            ' Lancer le traitement
            Dim resultat As ServiceReintegration.ResultatLot = ServiceReintegration.ReintegrerFacturesDematParLot()

            ' Afficher le résultat
            AfficherResultatReintegration(resultat, lblResultatReintegrationDemat)
            ChargerHistorique()
            rgFacturesDemat.Rebind()
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "RefreshGridDemat", "setTimeout(function() { refreshRadGrid(); }, 500);", True)
        Catch ex As Exception
            GestionnaireLog.Error("Erreur bouton ré-intégrer tout Demat : " & ex.ToString())
            lblResultatReintegrationDemat.Text = "&#10060; Erreur critique : " & ex.Message
            lblResultatReintegrationDemat.ForeColor = System.Drawing.Color.Red
            lblResultatReintegrationDemat.Visible = True
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


    'rajouter l'item command pour cliquer sur le bouton


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
                ' Exclure les factures terminées (PAID et REFUSED)
                dv.RowFilter = "ISNULL(StatutCycleDeVie, '') NOT IN ('PAID', 'REFUSED', 'paid', 'refused', 'Paiement Transmis', 'Refusée', 'PAIEMENT TRANSMIS', 'REFUSÉE')"
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
                ' Inclure uniquement les factures terminées (PAID et REFUSED)
                dv.RowFilter = "StatutCycleDeVie IN ('PAID', 'REFUSED', 'paid', 'refused', 'Paiement Transmis', 'Refusée', 'PAIEMENT TRANSMIS', 'REFUSÉE')"
                rgFacturesDematHistorique.DataSource = dv
            Else
                rgFacturesDematHistorique.DataSource = dt
            End If
        Catch ex As Exception
            GestionnaireLog.Error("Erreur lors du chargement des factures dématérialisées (Historique) : " & ex.Message)
        End Try
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
                    Case "IN_PROCESS"
                        ' Garder tout actif pour IN_PROCESS
                    Case Else
                        ' Pour SUSPENDED(ON_HOLD), UNDER_QUERY, PAID, etc., c'est "one way"
                        itemsToDisable.Add(item)
                End Select
            Next
            
            For Each itemToDisable In itemsToDisable
                itemToDisable.Enabled = False
            Next
            
            ' Si le statut n'est pas IN_PROCESS, on désactive aussi le contrôle entier pour éviter le clic
            If currentStatut <> "IN_PROCESS" Then
                ddlStatutCycleDeVie.Enabled = False
                
                ' Ajouter une infobulle explicative spécifique au statut
                Select Case currentStatut
                    Case "ON_HOLD"
                        ddlStatutCycleDeVie.ToolTip = "Une erreur venant du fournisseur a été signalée. En attente de sa correction avant de pouvoir reprendre la facture."
                    Case "UNDER_QUERY"
                        ddlStatutCycleDeVie.ToolTip = "Facture en litige (désaccord). En attente d'une action ou d'un avoir."
                    Case "REFUSED"
                        ddlStatutCycleDeVie.ToolTip = "Facture définitivement rejetée ."
                    Case "PAID"
                        ddlStatutCycleDeVie.ToolTip = "Facture intégrée avec succès, le paiement est acté."
                    Case "CONDITIONNALY_ACCEPTED"
                        ddlStatutCycleDeVie.ToolTip = "Facture intégrée mais avec des réserves."
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

            If lblCodePrestaLP IsNot Nothing AndAlso btnAjouterRegle IsNot Nothing Then
                Dim codePrestaLP As String = lblCodePrestaLP.Text.Trim()
                codePrestaLP = codePrestaLP.Replace("&nbsp;", "").Trim()

                If String.IsNullOrEmpty(codePrestaLP) AndAlso dataItem.DataItem IsNot Nothing Then
                    Try
                        Dim drv As System.Data.DataRowView = CType(dataItem.DataItem, System.Data.DataRowView)
                        Dim numOR As String = If(IsDBNull(drv("NumOR")), "", drv("NumOR").ToString())
                        Dim numFacture As String = If(IsDBNull(drv("NumFacture")), "", drv("NumFacture").ToString())
                        Dim codePrestaFournisseur As String = If(IsDBNull(drv("CodePrestaFournisseur")), "", drv("CodePrestaFournisseur").ToString())
                        Dim codeFournisseur As String = ""
                        Dim siret As String = If(IsDBNull(drv("Siret")), "", drv("Siret").ToString())

                        If Not String.IsNullOrEmpty(siret) Then
                            Try
                                Dim infosFour = ServiceOR.retournerInfosFournisseur(siret)
                                If infosFour IsNot Nothing Then
                                    codeFournisseur = infosFour.CodeFournisseur
                                End If
                            Catch exSiret As Exception
                                GestionnaireLog.Error("Erreur récupération InfosFournisseur : " & exSiret.Message)
                            End Try
                        End If

                        ' Fallback sur la méthode historique si non trouvé
                        If String.IsNullOrEmpty(codeFournisseur) Then
                            codeFournisseur = GestionnaireBddFacture.GetCodeFournisseur(numOR, numFacture)
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
            Case "AjouterRegleLigne"
                Dim args As String() = e.CommandArgument.ToString().Split("|"c)

                If args.Length >= 4 Then
                    Dim numOR As String = args(0)
                    Dim numFacture As String = args(1)
                    Dim codePrestaFournisseur As String = args(2)
                    Dim descr As String = args(3)
                    Dim codeFournisseur As String = ""

                    If args.Length >= 5 Then
                        codeFournisseur = args(4)
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
                                       "&numFac=" & Server.UrlEncode(numFacture)

                    rwFormulaireCorrespondance.NavigateUrl = url
                    rwFormulaireCorrespondance.VisibleOnPageLoad = True
                End If
        End Select
    End Sub

    ' Pour vérifier la validité du siret saisie
    Protected Sub rgFacturesDemat_ItemCommand(sender As Object, e As GridCommandEventArgs) Handles rgFacturesDemat.ItemCommand
        Select Case e.CommandName

            Case "ValidateSiret"
                Dim idFacture As String = e.CommandArgument.ToString()

                Dim dataItem As GridDataItem = CType(e.Item, GridDataItem)
                Dim txtSiret As TextBox = CType(dataItem.FindControl("txtSiretDemat"), TextBox)

                If txtSiret IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(txtSiret.Text) Then
                    Dim nouveauSiret As String = txtSiret.Text.Trim()

                    ' Mettre à jour en base
                    GestionnaireBddFacture.UpdateSiretDemat(idFacture, nouveauSiret)

                    ' Retraiter
                    Dim errorMessage As String = ""
                    Dim succes As Boolean = ServiceReintegration.RetraiterFactureSiretDemat(idFacture, nouveauSiret, errorMessage)

                    If succes Then
                        ' AfficherResultatAction("SIRET validé avec succès.", True, lblResultatReintegrationDemat)
                        
                        Dim lblSiretMsg As Label = CType(dataItem.FindControl("lblSiretMsg"), Label)
                        If lblSiretMsg IsNot Nothing Then
                            lblSiretMsg.Text = "Validé"
                            lblSiretMsg.ForeColor = System.Drawing.Color.Green
                            lblSiretMsg.Visible = True
                        End If
                        txtSiret.Style("border") = ""

                        ' Recharger
                        rgFacturesDemat.Rebind()
                        ScriptManager.RegisterStartupScript(Me, Me.GetType(), "RefreshGridDemat", "setTimeout(function() { refreshRadGrid(); }, 500);", True)
                    Else
                        ' AfficherResultatAction(errorMessage, False, lblResultatReintegrationDemat)
                        
                        Dim lblSiretMsg As Label = CType(dataItem.FindControl("lblSiretMsg"), Label)
                        If lblSiretMsg IsNot Nothing Then
                            lblSiretMsg.Text = "Erreur: " & errorMessage
                            lblSiretMsg.ForeColor = System.Drawing.Color.Red
                            lblSiretMsg.Visible = True
                        End If
                        txtSiret.Style("border") = "2px solid red"
                        ' On ne fait pas de Rebind ici pour conserver l'affichage de l'erreur dans la ligne
                    End If
                End If

        End Select
    End Sub

    ''' <summary>
    ''' Affiche l'édition SIRET ou le nom du fournisseur selon le statut pour Demat
    ''' </summary>
    Private Sub GererEditionFournisseurDemat(item As GridDataItem, statut As String)
        Dim pnlFournisseur As Panel = CType(item.FindControl("pnlFournisseurDemat"), Panel)
        If pnlFournisseur Is Nothing Then Return

        Dim txtSiret As TextBox = CType(item.FindControl("txtSiretDemat"), TextBox)
        Dim btnValiderSiret As RadButton = CType(item.FindControl("btnValiderSiretDemat"), RadButton)
        Dim lblFournisseur As Label = CType(item.FindControl("lblFournisseurDemat"), Label)

        If txtSiret Is Nothing OrElse btnValiderSiret Is Nothing OrElse lblFournisseur Is Nothing Then
            Return
        End If

        ' Statuts qui nécessitent l'édition du SIRET
        Dim statutsEditables As String() = {"SIRET_NON_LU", "FOURNISSEUR_INCONNU", "FOURNISSEUR_INTROUVABLE", "SUPPLIER_NOT_FOUND", "FOURNISSEUR_INEXISTANT"}

        If statutsEditables.Contains(statut.ToUpper()) Then
            txtSiret.Visible = True
            btnValiderSiret.Visible = True
            lblFournisseur.Visible = False
        Else
            txtSiret.Visible = False
            btnValiderSiret.Visible = False
            lblFournisseur.Visible = True
        End If
    End Sub

    Protected Async Sub ddlStatutCycleDeVie_SelectedIndexChanged(sender As Object, e As DropDownListEventArgs)
        Dim ddl As RadDropDownList = CType(sender, RadDropDownList)
        Dim item As GridDataItem = CType(ddl.NamingContainer, GridDataItem)

        Dim hdnIdFactureCycle As HiddenField = CType(item.FindControl("hdnIdFactureCycle"), HiddenField)
        If hdnIdFactureCycle IsNot Nothing Then
            Dim idFacture As String = hdnIdFactureCycle.Value
            Dim nouveauStatut As String = ddl.SelectedValue

            Try
                ' 1. Appel API Maileva pour mettre à jour le statut
                Dim mailevaService As New Services.MailevaApiService()
                Await mailevaService.MettreAJourStatutCycleDeVieAsync(idFacture, nouveauStatut)

                ' 2. Mise à jour dans la base de données D_invoice
                GestionnaireBddFacture.UpdateStatutCycleDeVie(idFacture, nouveauStatut)

                ' Message de succès global et local
                lbl_result.Text = "Statut Maileva mis à jour avec succès en " & nouveauStatut & "."
                lbl_result.ForeColor = System.Drawing.Color.Green

                Dim lblCycleMsg As Label = CType(item.FindControl("lblCycleMsg"), Label)
                If lblCycleMsg IsNot Nothing Then
                    lblCycleMsg.Text = "Mis à jour"
                    lblCycleMsg.ForeColor = System.Drawing.Color.Green
                    lblCycleMsg.Visible = True
                End If
                ddl.Style("border") = ""

                Dim messageJS As String = "alert('Statut Maileva mis à jour avec succès en " & nouveauStatut & "');"
                ScriptManager.RegisterStartupScript(Me, Me.GetType(), "alertSuccess", messageJS, True)

                ' Rafraichir la grille pour refléter l'état
                rgFacturesDemat.Rebind()

            Catch ex As Exception
                GestionnaireLog.Error("Erreur lors de la mise à jour du statut cycle de vie pour la facture " & idFacture & " : " & ex.Message)
                
                Dim safeErrorMsg As String = ex.Message
                If safeErrorMsg.Contains("<html") OrElse safeErrorMsg.Contains("503") OrElse safeErrorMsg.Contains("502") Then
                    safeErrorMsg = "Service indisponible (Erreur serveur API Maileva)."
                ElseIf safeErrorMsg.Length > 150 Then
                    safeErrorMsg = safeErrorMsg.Substring(0, 150) & "..."
                End If

                Dim lblCycleMsg As Label = CType(item.FindControl("lblCycleMsg"), Label)
                If lblCycleMsg IsNot Nothing Then
                    lblCycleMsg.Text = "Erreur: " & Server.HtmlEncode(safeErrorMsg)
                    lblCycleMsg.ForeColor = System.Drawing.Color.Red
                    lblCycleMsg.Visible = True
                End If
                ddl.Style("border") = "2px solid red"

                ' Commenté pour ne pas perdre l'affichage de l'erreur dans la ligne
                ' rgFacturesDemat.Rebind()
            End Try
        End If
    End Sub


End Class
