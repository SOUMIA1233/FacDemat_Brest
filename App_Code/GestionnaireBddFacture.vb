Imports System
Imports System.Collections.Generic
Imports System.Configuration
Imports System.Data
Imports System.Data.SqlClient
Imports Newtonsoft.Json.Linq
Imports SiteWeb

Public Class GestionnaireBddFacture
    Const BaseDeDonnees As String = "BASE_PROD"
    Const BaseDeDonneesTest As String = "BASE_TEST"
    Const BaseDeDonneesLP As String = "LocPro"
    Const BaseDeDonneesLPTest As String = "LocPro_Test"

    Public Shared Function retournerInfosVeh(immat As String) As DataTable
        Dim sql As String = "SELECT code_parc, code_modele FROM parc_importe where immat = '" & immat & "';"
        Using acd As New AccesDonnees()
            Return acd.creation_datatable(sql, BaseDeDonnees)
        End Using
    End Function


    Public Shared Function retournerInfosMouvement(codeParc As String, dateFac As Date) As DataTable
        Dim sql As String = "SELECT top 1 * FROM F570MVT WHERE f570dtdep <= '" & dateFac & " 23:59:59' AND K570T58POS='ENTRETIEN' AND K570090UNI='" & codeParc & "' order by f570dtdep DESC;"
        Using acd As New AccesDonnees()
            Return acd.creation_datatable(sql, BaseDeDonneesLP)
        End Using
    End Function


    Public Shared Function retournerFournisseur(siret As String) As DataTable
        'refaire la requete en rajoutant un lien pour avoir le code fournisseur dans la table F050TIERS
        ' ne pas oublier de modifier la méthode appelante qui mets les informations de la table dans l'objet "InfosFournisseur"
        Dim sql As String = "SELECT F050KY FROM F050TIERS inner join F020ADR on F050TIERS.K050020ADR = F020ADR.f020ky WHERE F020SIRET = '" & siret.Replace(" ", "") & "';"
        Using acd As New AccesDonnees()
            Return acd.creation_datatable(sql, BaseDeDonneesLP)
        End Using
    End Function

    Public Shared Function retournerFournisseurSiren(siren As String) As DataTable
        Dim sirenClean As String = siren.Trim().Replace(" ", "")
        Dim sql As String = "SELECT F050KY FROM F050TIERS WHERE F050SIREN = '" & sirenClean & "';"
        Using acd As New AccesDonnees()
            Dim dt As DataTable = acd.creation_datatable(sql, BaseDeDonneesLP)
            If dt IsNot Nothing AndAlso dt.Rows.Count > 0 Then
                Return dt
            Else
                Dim sqlFallback As String = "SELECT F050KY FROM F050TIERS inner join F020ADR on F050TIERS.K050020ADR = F020ADR.f020ky WHERE F020SIRET LIKE '" & sirenClean & "%';"
                Return acd.creation_datatable(sqlFallback, BaseDeDonneesLP)
            End If
        End Using
    End Function


    Public Shared Function GetRegleCorrespondance(codeFournisseur As String, codePresta As String) As JObject
        Dim sql As String = "SELECT RegleLP FROM CorrespondancePrestaFournisseur WHERE CodeFournisseur = '" & codeFournisseur & "'AND Actif = 1 AND CodePrestaFournisseur = '" & codePresta & "';"

        Dim dt As DataTable
        Using acd As New AccesDonnees()
            dt = acd.creation_datatable(sql, BaseDeDonnees)
        End Using

        If dt Is Nothing OrElse dt.Rows.Count = 0 Then Return Nothing

        Dim jsonString As String = dt.Rows(0)("RegleLP").ToString()
        Return JObject.Parse(jsonString)
    End Function


    ''' Vérifie si une facture existe déjà dans la base
    Public Shared Function FactureExiste(numOR As String, numFacture As String) As Boolean
        Dim sql As String = "SELECT COUNT(*) FROM HistoFacFournisseur WHERE NumOR = '" & numOR & "' AND NumFacture = '" & numFacture & "';"
        Dim dt As DataTable
        Using acd As New AccesDonnees()
            dt = acd.creation_datatable(sql, BaseDeDonnees)
        End Using

        If dt IsNot Nothing AndAlso dt.Rows.Count > 0 Then
            Return CInt(dt.Rows(0)(0)) > 0
        End If

        Return False
    End Function


    ''' Insère l'en-tête d'une facture fournisseur
    Public Shared Sub InsererFactureFournisseur(numOR As String, numFacture As String, nomFichier As String, dateFacture As Date,
                                                siret As String, codeFournisseur As String, raisonSociale As String, immat As String,
                                                codeParc As String, codeModele As String, totalHT As Double, totalTTC As Double,
                                                remiseHT As Double, totalTVA As Double, statut As String, message As String, utilisateur As String,
                                                dateDebMouv As DateTime, dateFinMouv As DateTime, compteurKm As Integer)

        Dim sql As String = "INSERT INTO HistoFacFournisseur " _
            & "(NumOR, NumFacture, NomFichier, DateFacture, Siret, CodeFournisseur, RaisonSociale, Immat, CodeParc, CodeModele, " _
            & "TotalHT, TotalTTC, RemiseHT, TotalTVA, Statut, Message, Utilisateur, DateImport, DateDebMouv, DateFinMouv, CompteurKm) VALUES " _
            & "('" & numOR & "', '" & numFacture & "', '" & nomFichier.Replace("'", "''") & "', '" & dateFacture.ToString("yyyy-MM-dd") & "', " _
            & " '" & siret.Replace(" ", "") & "', '" & codeFournisseur & "', '" & raisonSociale.Replace("'", "''") & "', '" & immat & "', '" & codeParc & "', " _
            & "'" & codeModele & "', " & Replace(totalHT.ToString().Replace(",", "."), ",", ".") & ", " & Replace(totalTTC.ToString().Replace(",", "."), ",", ".") & ", " _
            & Replace(remiseHT.ToString().Replace(",", "."), ",", ".") & "," & Replace(totalTVA.ToString().Replace(",", "."), ",", ".") & ", " _
            & "'" & statut & "', '" & message.Replace("'", "''") & "', '" & utilisateur & "', GETDATE(), '" & dateDebMouv & "', '" & dateFinMouv & "', " & compteurKm & ");"

        Using acd As New AccesDonnees()
            acd.ExecuterCommande(sql, BaseDeDonnees)
        End Using
    End Sub


    ''' Insère une ligne de facture fournisseur
    Public Shared Sub InsererLigneFactureFournisseur(numOR As String, numFacture As String, codePrestaFournisseur As String,
                                                     codePrestaLP As String, numLig As Integer, descr As String, qte As Integer,
                                                     prixBareme As Double, prixUnitHT As Double, prixUnitNetHT As Double,
                                                     tauxRemise As Double, montantNetHT As Double, tva As Double)

        Dim sql As String = "INSERT INTO HistoLigFacFournisseur " _
            & "(NumOR, NumFacture, CodePrestaFournisseur, CodePrestaLP, NumLig, Descr, Qte, PrixBareme, PrixUnitHT, PrixUnitNetHT, TauxRemise, MontantNetHT, TVA) " _
            & "VALUES ('" & numOR & "', '" & numFacture & "', '" & codePrestaFournisseur.Replace("'", "''") & "', '" & codePrestaLP & "', " & numLig _
            & ",'" & descr.Replace("'", "''") & "'," & qte & "," & Replace(prixBareme.ToString().Replace(",", "."), ",", ".") _
            & "," & Replace(prixUnitHT.ToString().Replace(",", "."), ",", ".") & "," & Replace(prixUnitNetHT.ToString().Replace(",", "."), ",", ".") _
            & "," & Replace(tauxRemise.ToString().Replace(",", "."), ",", ".") & "," & Replace(montantNetHT.ToString().Replace(",", "."), ",", ".") _
            & "," & Replace(tva.ToString().Replace(",", "."), ",", ".") & ");"

        Using acd As New AccesDonnees()
            acd.ExecuterCommande(sql, BaseDeDonnees)
        End Using
    End Sub


    ''' Met à jour le statut d'une facture
    Public Shared Sub MettreAJourStatutFacture(numOR As String, numFacture As String, statut As String, message As String)
        Dim sql As String = "UPDATE HistoFacFournisseur " _
            & "SET Statut = '" & statut & "', Message = '" & message.Replace("'", "''") & "' " _
            & "WHERE NumOR = '" & numOR & "' AND NumFacture = '" & numFacture & "';"

        Using acd As New AccesDonnees()
            acd.ExecuterCommande(sql, BaseDeDonnees)
        End Using
    End Sub


    ''' Récupère l'historique des factures depuis la BDD
    Public Shared Function ObtenirHistoriqueFactures() As DataTable
        Dim sql As String = "SELECT " &
            "NumOR, NumFacture, NomFichier, DateFacture, " &
            "Siret, CodeFournisseur, RaisonSociale, Immat, " &
            "CodeParc, CodeModele, TotalHT, TotalTTC, " &
            "RemiseHT, TotalTVA, Statut, Message, " &
            "Utilisateur, DateImport " &
            "FROM HistoFacFournisseur " &
            "ORDER BY DateImport DESC"

        Using acd As New AccesDonnees()
            Return acd.creation_datatable(sql, BaseDeDonnees)
        End Using
    End Function


    ''' Récupère les lignes d'une facture
    Public Shared Function ObtenirLignesFacture(numOR As String, numFacture As String) As DataTable
        Dim sql As String = "SELECT " &
            "NumOR, NumFacture, CodePrestaFournisseur, CodePrestaLP, " &
            "NumLig, Descr, Qte, PrixBareme, PrixUnitHT, " &
            "PrixUnitNetHT, TauxRemise, MontantNetHT, TVA " &
            "FROM HistoLigFacFournisseur " &
            "WHERE NumOR = '" & numOR & "' " &
            "AND NumFacture = '" & numFacture.Replace("'", "''") & "' " &
            "ORDER BY NumLig"

        Using acd As New AccesDonnees()
            Return acd.creation_datatable(sql, BaseDeDonnees)
        End Using
    End Function


    ''' <summary>
    ''' Récupère le code fournisseur d'une facture
    ''' </summary>
    Public Shared Function GetCodeFournisseur(numOR As String, numFacture As String) As String
        Try
            Dim sql As String = "SELECT CodeFournisseur " &
                               "FROM HistoFacFournisseur " &
                               "WHERE NumOR = '" & numOR & "' " &
                               "AND NumFacture = '" & numFacture.Replace("'", "''") & "'"

            Dim dt As DataTable
            Using acd As New AccesDonnees()
                dt = acd.creation_datatable(sql, BaseDeDonnees)
            End Using

            If dt IsNot Nothing AndAlso dt.Rows.Count > 0 Then
                Return dt.Rows(0)("CodeFournisseur").ToString()
            End If

            Return ""

        Catch ex As Exception
            GestionnaireLog.Error("Erreur GetCodeFournisseur : " & ex.Message)
            Return ""
        End Try
    End Function


    ''' <summary>
    ''' Vérifie si une prestation LocPro existe dans la base de données
    ''' </summary>
    ''' <param name="codePrestation">Code de la prestation à vérifier (ex: VID, FL, MO)</param>
    ''' <returns>True si la prestation existe, False sinon</returns>
    Public Shared Function PrestationLocProExiste(codePrestation As String) As Boolean
        If String.IsNullOrEmpty(codePrestation) Then
            Return False
        End If

        Try
            Dim sql As String = "SELECT COUNT(*) FROM F100PRO WHERE F100KY = '" & codePrestation.Trim() & "';"

            Using acd As New AccesDonnees()
                Using dr As SqlDataReader = acd.RetournerDataReader(sql, BaseDeDonneesLP)
                    If dr.Read() Then
                        Dim count As Integer = Convert.ToInt32(dr(0))
                        Return count > 0
                    End If
                End Using
            End Using

            Return False

        Catch ex As Exception
            GestionnaireLog.Error("Erreur lors de la vérification de la prestation " & codePrestation & " : " & ex.Message)
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Vérifie une liste de codes prestations et retourne ceux qui n'existent pas
    ''' </summary>
    ''' <param name="codesPrestations">Liste des codes à vérifier</param>
    ''' <returns>Liste des codes invalides</returns>
    Public Shared Function VerifierPrestationsMultiples(codesPrestations As List(Of String)) As List(Of String)
        Dim codesInvalides As New List(Of String)()

        If codesPrestations Is Nothing OrElse codesPrestations.Count = 0 Then
            Return codesInvalides
        End If

        For Each code As String In codesPrestations
            If Not String.IsNullOrEmpty(code) AndAlso Not PrestationLocProExiste(code) Then
                codesInvalides.Add(code)
            End If
        Next

        Return codesInvalides
    End Function


    ''' <summary>
    ''' Récupère le montant HT d'une ligne de facture fournisseur
    ''' </summary>
    ''' <param name="numOR">Numéro d'OR</param>
    ''' <param name="numFacture">Numéro de facture</param>
    ''' <param name="codePrestaFournisseur">Code prestation fournisseur</param>
    ''' <returns>Montant HT de la ligne, ou 0 si non trouvé</returns>
    Public Shared Function ObtenirMontantLigneFacture(numOR As String, numFacture As String, codePrestaFournisseur As String) As Decimal
        If String.IsNullOrEmpty(numOR) OrElse String.IsNullOrEmpty(numFacture) OrElse String.IsNullOrEmpty(codePrestaFournisseur) Then
            Return 0
        End If

        Try
            Dim sql As String = "SELECT TOP 1 MontantNetHT FROM HistoLigFacFournisseur " &
                               "WHERE NumOR = '" & numOR.Trim().Replace("'", "''") & "' " &
                               "AND NumFacture = '" & numFacture.Trim().Replace("'", "''") & "' " &
                               "AND CodePrestaFournisseur = '" & codePrestaFournisseur.Trim().Replace("'", "''") & "';"

            Using acd As New AccesDonnees()
                Using dr As SqlDataReader = acd.RetournerDataReader(sql, BaseDeDonnees)
                    If dr.Read() AndAlso Not IsDBNull(dr("MontantNetHT")) Then
                        Return Convert.ToDecimal(dr("MontantNetHT"))
                    End If
                End Using
            End Using

            Return 0

        Catch ex As Exception
            GestionnaireLog.Error("Erreur lors de la récupération du montant de la ligne facture " & numFacture & " : " & ex.Message)
            Return 0
        End Try
    End Function


    ''' <summary>
    ''' Crée une nouvelle règle de correspondance prestation fournisseur/LocPro
    ''' </summary>
    ''' <param name="codeFournisseur">Code fournisseur</param>
    ''' <param name="codePrestaFournisseur">Code prestation fournisseur</param>
    ''' <param name="libelleFournisseur">Libellé de la prestation fournisseur</param>
    ''' <param name="regleJSON">Règle au format JSON</param>
    ''' <returns>True si insertion réussie, False sinon</returns>
    Public Shared Function CreerRegleCorrespondance(codeFournisseur As String, codePrestaFournisseur As String, libelleFournisseur As String, regleJSON As String) As Boolean
        If String.IsNullOrEmpty(codeFournisseur) OrElse String.IsNullOrEmpty(codePrestaFournisseur) OrElse String.IsNullOrEmpty(regleJSON) Then
            GestionnaireLog.Error("Paramètres invalides pour la création de règle")
            Return False
        End If

        Try
            Dim sql As String = "INSERT INTO CorrespondancePrestaFournisseur (CodeFournisseur, CodePrestaFournisseur, LibelleFournisseur, Actif, RegleLP) " &
                               "VALUES ('" & codeFournisseur.Trim().Replace("'", "''") & "', " &
                               "'" & codePrestaFournisseur.Trim().Replace("'", "''") & "', " &
                               "'" & libelleFournisseur.Trim().Replace("'", "''") & "', " &
                               "1, " &
                               "'" & regleJSON.Replace("'", "''") & "');"

            Dim nbLignes As Integer
            Using acd As New AccesDonnees()
                nbLignes = acd.ExecuterCommande(sql, BaseDeDonnees)
            End Using

            If nbLignes > 0 Then
                GestionnaireLog.Info("Règle de correspondance créée : " & codeFournisseur & " - " & codePrestaFournisseur)
                Return True
            Else
                GestionnaireLog.Warn("Aucune ligne insérée pour la règle " & codeFournisseur & " - " & codePrestaFournisseur)
                Return False
            End If

        Catch ex As Exception
            GestionnaireLog.Error("Erreur lors de la création de la règle de correspondance : " & ex.Message)
            Return False
        End Try
    End Function



    ''' <summary>
    ''' Vérifie si une règle de correspondance existe déjà
    ''' </summary>
    ''' <param name="codeFournisseur">Code fournisseur</param>
    ''' <param name="codePrestaFournisseur">Code prestation fournisseur</param>
    ''' <returns>True si la règle existe, False sinon</returns>
    Public Shared Function RegleCorrespondanceExiste(codeFournisseur As String, codePrestaFournisseur As String) As Boolean
        If String.IsNullOrEmpty(codeFournisseur) OrElse String.IsNullOrEmpty(codePrestaFournisseur) Then
            Return False
        End If

        Try
            Dim sql As String = "SELECT COUNT(*) FROM CorrespondancePrestaFournisseur " &
                               "WHERE CodeFournisseur = '" & codeFournisseur.Trim().Replace("'", "''") & "' " &
                               "AND CodePrestaFournisseur = '" & codePrestaFournisseur.Trim().Replace("'", "''") & "' " &
                               "AND Actif = 1;"

            Using acd As New AccesDonnees()
                Using dr As SqlDataReader = acd.RetournerDataReader(sql, BaseDeDonnees)
                    If dr.Read() Then
                        Dim count As Integer = Convert.ToInt32(dr(0))
                        Return count > 0
                    End If
                End Using
            End Using

            Return False

        Catch ex As Exception
            GestionnaireLog.Error("Erreur lors de la vérification de l'existence de la règle : " & ex.Message)
            Return False
        End Try
    End Function


    ''' <summary>
    ''' Récupère les codes des prestations LocPro depuis le JSON d'une règle de correspondance
    ''' </summary>
    ''' <param name="codeFournisseur">Code du fournisseur</param>
    ''' <param name="codePrestaFournisseur">Code de la prestation fournisseur</param>
    ''' <returns>Liste des codes prestations LocPro, ou liste vide si pas de règle</returns>
    Public Shared Function GetCodesLocPro(codeFournisseur As String, codePrestaFournisseur As String) As List(Of String)
        Dim codes As New List(Of String)()

        If String.IsNullOrEmpty(codeFournisseur) OrElse String.IsNullOrEmpty(codePrestaFournisseur) Then
            Return codes
        End If

        Try
            Dim sql As String = "SELECT RegleLP FROM CorrespondancePrestaFournisseur " &
                               "WHERE CodeFournisseur = '" & codeFournisseur.Trim().Replace("'", "''") & "' " &
                               "AND CodePrestaFournisseur = '" & codePrestaFournisseur.Trim().Replace("'", "''") & "' " &
                               "AND Actif = 1;"

            Using acd As New AccesDonnees()
                Using dr As SqlDataReader = acd.RetournerDataReader(sql, BaseDeDonnees)
                    If dr.Read() AndAlso Not IsDBNull(dr("RegleLP")) Then
                        Dim jsonRegle As String = dr("RegleLP").ToString()

                        ' Parser le JSON pour extraire les codes
                        Dim jRoot As JObject = JObject.Parse(jsonRegle)
                        Dim jPrestations As JArray = CType(jRoot("prestations"), JArray)

                        For Each jPresta As JObject In jPrestations
                            Dim code As String = jPresta("code").ToString()
                            If Not String.IsNullOrEmpty(code) Then
                                codes.Add(code)
                            End If
                        Next
                    End If
                End Using
            End Using

        Catch ex As Exception
            GestionnaireLog.Error("Erreur extraction codes LocPro depuis règle : " & ex.Message)
        End Try

        Return codes
    End Function


    ''' <summary>
    ''' Récupère toutes les factures contenant une prestation donnée avec le statut NON_MATCHED_SERVICES
    ''' </summary>
    ''' <param name="codeFournisseur">Code du fournisseur</param>
    ''' <param name="codePrestaFournisseur">Code de la prestation fournisseur</param>
    ''' <returns>DataTable avec NumOR, NumFacture, CodeFournisseur</returns>
    Public Shared Function GetFacturesNonMatcheesAvecPrestation(codeFournisseur As String, codePrestaFournisseur As String) As DataTable
        If String.IsNullOrEmpty(codeFournisseur) OrElse String.IsNullOrEmpty(codePrestaFournisseur) Then
            Return New DataTable()
        End If

        Try
            Dim sql As String = "SELECT DISTINCT f.NumOR, f.NumFacture, f.CodeFournisseur " &
                               "FROM HistoFacFournisseur f " &
                               "INNER JOIN HistoLigFacFournisseur l ON f.NumOR = l.NumOR AND f.NumFacture = l.NumFacture " &
                               "WHERE f.CodeFournisseur = '" & codeFournisseur.Trim().Replace("'", "''") & "' " &
                               "AND l.CodePrestaFournisseur = '" & codePrestaFournisseur.Trim().Replace("'", "''") & "' " &
                               "AND f.Statut = 'PRESTATION_INEXISTANTE';"

            Using acd As New AccesDonnees()
                Return acd.creation_datatable(sql, BaseDeDonnees)
            End Using

        Catch ex As Exception
            GestionnaireLog.Error("Erreur récupération factures non matchées : " & ex.Message)
            Return New DataTable()
        End Try
    End Function


    ''' <summary>
    ''' Met à jour le statut d'une facture vers EN_ATTENTE
    ''' </summary>
    ''' <param name="numOR">Numéro d'OR</param>
    ''' <param name="numFacture">Numéro de facture</param>
    ''' <returns>True si mise à jour réussie</returns>
    Public Shared Function MettreAJourStatutFactureEnAttente(numOR As String, numFacture As String) As Boolean
        If String.IsNullOrEmpty(numOR) OrElse String.IsNullOrEmpty(numFacture) Then
            Return False
        End If

        Try
            Dim sql As String = "UPDATE HistoFacFournisseur " &
                               "SET Statut = 'EN_ATTENTE', " &
                               "Message = 'Toutes les prestations sont maintenant identifiées' " &
                               "WHERE NumOR = '" & numOR.Trim().Replace("'", "''") & "' " &
                               "AND NumFacture = '" & numFacture.Trim().Replace("'", "''") & "';"

            Dim nbLignes As Integer
            Using acd As New AccesDonnees()
                nbLignes = acd.ExecuterCommande(sql, BaseDeDonnees)
            End Using

            If nbLignes > 0 Then
                GestionnaireLog.Info("Facture " & numFacture & " mise à jour vers EN_ATTENTE")
                Return True
            End If

            Return False

        Catch ex As Exception
            GestionnaireLog.Error("Erreur mise à jour statut facture " & numFacture & " : " & ex.Message)
            Return False
        End Try
    End Function


    ''' <summary>
    ''' Met à jour la colonne CodePrestaLP d'une ligne de facture avec les codes LocPro de la règle
    ''' </summary>
    ''' <param name="numOR">Numéro d'OR</param>
    ''' <param name="numFacture">Numéro de facture</param>
    ''' <param name="codePrestaFournisseur">Code prestation fournisseur</param>
    ''' <param name="codesLocPro">Liste des codes prestations LocPro (séparés par virgule)</param>
    ''' <returns>True si mise à jour réussie</returns>
    Public Shared Function MettreAJourCodesLocProLigne(numOR As String, numFacture As String, codePrestaFournisseur As String, codesLocPro As String) As Boolean
        If String.IsNullOrEmpty(numOR) OrElse String.IsNullOrEmpty(numFacture) OrElse String.IsNullOrEmpty(codePrestaFournisseur) Then
            Return False
        End If

        Try
            Dim sql As String = "UPDATE HistoLigFacFournisseur " &
                               "SET CodePrestaLP = '" & codesLocPro.Trim().Replace("'", "''") & "' " &
                               "WHERE NumOR = '" & numOR.Trim().Replace("'", "''") & "' " &
                               "AND NumFacture = '" & numFacture.Trim().Replace("'", "''") & "' " &
                               "AND CodePrestaFournisseur = '" & codePrestaFournisseur.Trim().Replace("'", "''") & "';"

            Dim nbLignes As Integer
            Using acd As New AccesDonnees()
                nbLignes = acd.ExecuterCommande(sql, BaseDeDonnees)
            End Using

            If nbLignes > 0 Then
                GestionnaireLog.Info("Ligne(s) de facture " & numFacture & " mise(s) à jour avec codes LocPro : " & codesLocPro)
                Return True
            End If

            Return False

        Catch ex As Exception
            GestionnaireLog.Error("Erreur mise à jour codes LocPro ligne facture : " & ex.Message)
            Return False
        End Try
    End Function


    ''' <summary>
    ''' Récupère toutes les factures avec le statut EN_ATTENTE
    ''' </summary>
    ''' <returns>DataTable avec toutes les colonnes nécessaires</returns>
    Public Shared Function ObtenirFacturesEnAttente() As DataTable
        Try
            Dim sql As String = "SELECT " &
                               "NumOR, NumFacture, NomFichier, DateFacture, " &
                               "Siret, CodeFournisseur, RaisonSociale, Immat, " &
                               "CodeParc, CodeModele, TotalHT, TotalTTC, " &
                               "RemiseHT, TotalTVA, Statut, Message, " &
                               "Utilisateur, DateImport, DateDebMouv, DateFinMouv, CompteurKm " &
                               "FROM HistoFacFournisseur " &
                               "WHERE Statut = 'EN_ATTENTE' " &
                               "ORDER BY DateImport DESC"

            Using acd As New AccesDonnees()
                Return acd.creation_datatable(sql, BaseDeDonnees)
            End Using

        Catch ex As Exception
            GestionnaireLog.Error("Erreur récupération factures EN_ATTENTE : " & ex.Message)
            Return New DataTable()
        End Try
    End Function


    ''' <summary>
    ''' Met à jour le statut d'une facture vers SUCCES
    ''' </summary>
    Public Shared Function MettreAJourStatutFactureSucces(numOR As String, numFacture As String) As Boolean
        If String.IsNullOrEmpty(numOR) OrElse String.IsNullOrEmpty(numFacture) Then
            Return False
        End If

        Try
            Dim sql As String = "UPDATE HistoFacFournisseur " &
                               "SET Statut = 'SUCCES', " &
                               "Message = 'Intégration réussie dans LocPro' " &
                               "WHERE NumOR = '" & numOR.Trim().Replace("'", "''") & "' " &
                               "AND NumFacture = '" & numFacture.Trim().Replace("'", "''") & "';"

            Dim nbLignes As Integer
            Using acd As New AccesDonnees()
                nbLignes = acd.ExecuterCommande(sql, BaseDeDonnees)
            End Using

            Return nbLignes > 0

        Catch ex As Exception
            GestionnaireLog.Error("Erreur mise à jour statut SUCCES : " & ex.Message)
            Return False
        End Try
    End Function


    ''' <summary>
    ''' Met à jour le statut d'une facture vers ERREUR_LOCPRO
    ''' </summary>
    Public Shared Function MettreAJourStatutFactureErreur(numOR As String, numFacture As String, messageErreur As String) As Boolean
        If String.IsNullOrEmpty(numOR) OrElse String.IsNullOrEmpty(numFacture) Then
            Return False
        End If

        Try
            Dim sql As String = "UPDATE HistoFacFournisseur " &
                               "SET Statut = 'ERREUR_LOCPRO', " &
                               "Message = '" & messageErreur.Trim().Replace("'", "''") & "' " &
                               "WHERE NumOR = '" & numOR.Trim().Replace("'", "''") & "' " &
                               "AND NumFacture = '" & numFacture.Trim().Replace("'", "''") & "';"

            Dim acd As New AccesDonnees()
            Dim nbLignes As Integer = acd.ExecuterCommande(sql, BaseDeDonnees)

            Return nbLignes > 0

        Catch ex As Exception
            GestionnaireLog.Error("Erreur mise à jour statut ERREUR_LOCPRO : " & ex.Message)
            Return False
        End Try
    End Function


    ''' <summary>
    ''' Met à jour les informations fournisseur d'une facture
    ''' </summary>
    ''' <param name="numOR">Numéro OR</param>
    ''' <param name="numFacture">Numéro facture</param>
    ''' <param name="codeFournisseur">Code fournisseur LocPro</param>
    Public Shared Sub MettreAJourInfosFournisseur(numOR As String, numFacture As String, codeFournisseur As String)
        If String.IsNullOrEmpty(numOR) OrElse String.IsNullOrEmpty(numFacture) OrElse String.IsNullOrEmpty(codeFournisseur) Then
            GestionnaireLog.Warn("Paramètres invalides pour MAJ infos fournisseur")
            Return
        End If

        Try
            Dim sql As String = "UPDATE HistoFacFournisseur " &
                               "SET CodeFournisseur = '" & codeFournisseur.Trim().Replace("'", "''") & "' " &
                               "WHERE NumOR = '" & numOR.Trim().Replace("'", "''") & "' " &
                               "AND NumFacture = '" & numFacture.Trim().Replace("'", "''") & "';"

            Dim acd As New AccesDonnees()
            Dim nbLignes As Integer = acd.ExecuterCommande(sql, BaseDeDonnees)

            If nbLignes > 0 Then
                GestionnaireLog.Info("Infos fournisseur mises à jour : " & codeFournisseur)
            Else
                GestionnaireLog.Warn("Aucune ligne mise à jour pour le fournisseur")
            End If

        Catch ex As Exception
            GestionnaireLog.Error("Erreur MAJ infos fournisseur : " & ex.Message)
        End Try
    End Sub

    ''' <summary>
    ''' Récupère une facture complète depuis la BDD
    ''' </summary>
    ''' <param name="numOR">Numéro OR</param>
    ''' <param name="numFacture">Numéro facture</param>
    ''' <returns>DataTable avec la facture</returns>
    Public Shared Function GetFacture(numOR As String, numFacture As String) As DataTable
        If String.IsNullOrEmpty(numOR) OrElse String.IsNullOrEmpty(numFacture) Then
            Return New DataTable()
        End If

        Try
            Dim sql As String = "SELECT * FROM HistoFacFournisseur " &
                               "WHERE NumOR = '" & numOR.Trim().Replace("'", "''") & "' " &
                               "AND NumFacture = '" & numFacture.Trim().Replace("'", "''") & "';"

            Dim acd As New AccesDonnees()
            Return acd.creation_datatable(sql, BaseDeDonnees)

        Catch ex As Exception
            GestionnaireLog.Error("Erreur récupération facture : " & ex.Message)
            Return New DataTable()
        End Try
    End Function


    ''' <summary>
    ''' Met à jour le SIRET d'une facture
    ''' </summary>
    ''' <param name="numOR">Numéro OR</param>
    ''' <param name="numFacture">Numéro facture</param>
    ''' <param name="nouveauSiret">Nouveau SIRET (14 chiffres)</param>
    ''' <returns>True si mise à jour réussie</returns>
    Public Shared Function MettreAJourSiret(numOR As String, numFacture As String, nouveauSiret As String) As Boolean
        If String.IsNullOrEmpty(numOR) OrElse String.IsNullOrEmpty(numFacture) OrElse String.IsNullOrEmpty(nouveauSiret) Then
            GestionnaireLog.Warn("Paramètres invalides pour MAJ SIRET")
            Return False
        End If

        Try
            ' Nettoyer le SIRET (supprimer les espaces éventuels)
            nouveauSiret = nouveauSiret.Trim().Replace(" ", "")

            Dim sql As String = "UPDATE HistoFacFournisseur " &
                               "SET Siret = '" & nouveauSiret.Replace("'", "''") & "' " &
                               "WHERE NumOR = '" & numOR.Trim().Replace("'", "''") & "' " &
                               "AND NumFacture = '" & numFacture.Trim().Replace("'", "''") & "';"

            Dim acd As New AccesDonnees()
            Dim nbLignes As Integer = acd.ExecuterCommande(sql, BaseDeDonnees)

            If nbLignes > 0 Then
                GestionnaireLog.Info("SIRET mis à jour : " & numFacture & " → " & nouveauSiret)
                Return True
            Else
                GestionnaireLog.Warn("Aucune ligne mise à jour pour le SIRET")
                Return False
            End If

        Catch ex As Exception
            GestionnaireLog.Error("Erreur MAJ SIRET : " & ex.Message)
            Return False
        End Try
    End Function


    '*********************Facture Dematérialisée****************
    ''' <summary>
    ''' Récupère les factures dématérialisées depuis D_invoice
    ''' </summary>
    Public Shared Function getFactureDemat() As DataTable
        Try
            Dim sql As String = "SELECT " &
                               "Statut AS Statut, " &
                               "NumeroFacture AS NumFacture, " &
                               "DateEmi AS DateFacture, " &
                               "SocieteEmet AS RaisonSociale, " &
                               "Siren_Vend AS Siren, " &
                               "Siret_Vend AS Siret_Vend, " &
                               "'' AS Immat, " &
                               "'' AS CodeParc, " &
                               "MontantHT AS TotalHT, " &
                               "MontantTotal AS TotalTTC, " &
                               "numOr AS NumOR, " &
                               "Message AS Message, " &
                               "IdFacture, " &
                               "StatutCycleDeVie " &
                               "FROM D_invoice " &
                               "ORDER BY DateCreation DESC"

            Using acd As New AccesDonnees()
                Return acd.creation_datatable(sql, BaseDeDonnees)
            End Using
        Catch ex As Exception
            GestionnaireLog.Error("Erreur récupération factures demat : " & ex.Message)
            Return New DataTable()
        End Try
    End Function

    ''' <summary>
    ''' Met à jour le statut du cycle de vie (Maileva) d'une facture dématérialisée
    ''' </summary>
    Public Shared Sub UpdateStatutCycleDeVie(idFacture As String, nouveauStatut As String)
        Try
            Dim sql As String = "UPDATE D_invoice SET StatutCycleDeVie = @NouveauStatut WHERE IdFacture = @IdFacture"
            Dim acd As New AccesDonnees()

            ' Utilisation d'ADO.NET classique pour éviter l'injection SQL
            Using conn As New SqlConnection(ConfigurationManager.ConnectionStrings(BaseDeDonnees).ConnectionString)
                Using cmd As New SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@NouveauStatut", If(String.IsNullOrEmpty(nouveauStatut), DBNull.Value, nouveauStatut))
                    cmd.Parameters.AddWithValue("@IdFacture", idFacture)

                    conn.Open()
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        Catch ex As Exception
            GestionnaireLog.Error("Erreur UpdateStatutCycleDeVie : " & ex.Message)
            Throw ' On remonte l'erreur pour la gérer côté UI
        End Try
    End Sub

    ''' <summary>
    ''' Récupère les lignes d'une facture dématérialisée depuis D_invoice_lignes
    ''' </summary>
    Public Shared Function getDematFacturesLignes(idFacture As String) As DataTable
        Try
            Dim sql As String = "SELECT " &
                               "L.IdFacture, " &
                               "L.NumLigne AS NumLig, " &
                               "L.refArticleFournisseur AS CodePrestaFournisseur, " &
                               "L.Designation AS Descr, " &
                               "'' AS CodePrestaLP, " &
                               "L.Quantite AS Qte, " &
                               "L.PrixUnitaireHT AS PrixUnitHT, " &
                               "0 AS TauxRemise, " &
                               "L.MontantHT AS MontantNetHT, " &
                               "L.TauxTVA AS TVA, " &
                               "I.numOr AS NumOR, " &
                               "I.Siren_Vend AS Siren, " &
                               "I.Siret_Vend AS Siret_Vend, " &
                               "I.NumeroFacture AS NumFacture " &
                               "FROM D_invoice_lignes L " &
                               "LEFT JOIN D_invoice I ON L.IdFacture = I.IdFacture " &
                               "WHERE L.IdFacture = '" & idFacture.Replace("'", "''") & "' " &
                               "ORDER BY L.NumLigne"

            Dim dt As DataTable
            Using acd As New AccesDonnees()
                dt = acd.creation_datatable(sql, BaseDeDonnees)
            End Using

            If dt IsNot Nothing AndAlso dt.Rows.Count > 0 Then
                ' Récupérer le code fournisseur LocPro à partir du SIREN
                Dim siren As String = dt.Rows(0)("Siren").ToString()
                Dim codeFournisseur As String = ""

                Try
                    Dim dtFourn As DataTable = GestionnaireBddFacture.retournerFournisseurSiren(siren)
                    If dtFourn IsNot Nothing AndAlso dtFourn.Rows.Count > 0 Then
                        codeFournisseur = dtFourn.Rows(0)("F050KY").ToString().Trim()
                    End If
                Catch ex As Exception
                    ' Ignorer si le fournisseur n'est pas trouvé, on laissera CodePrestaLP vide
                End Try

                If Not dt.Columns.Contains("CodeFournisseur") Then
                    dt.Columns.Add("CodeFournisseur", GetType(String))
                End If

                If Not String.IsNullOrEmpty(codeFournisseur) Then
                    For Each row As DataRow In dt.Rows
                        row("CodeFournisseur") = codeFournisseur
                        Dim codePresta As String = row("CodePrestaFournisseur").ToString()
                        Dim codesLP As List(Of String) = GetCodesLocPro(codeFournisseur, codePresta)
                        If codesLP IsNot Nothing AndAlso codesLP.Count > 0 Then
                            row("CodePrestaLP") = String.Join(", ", codesLP)
                        End If
                    Next
                End If
            End If

            Return dt
        Catch ex As Exception
            GestionnaireLog.Error("Erreur récupération lignes factures demat : " & ex.Message)
            Return New DataTable()
        End Try
    End Function
    Public Shared Sub UpdateSiret(numOR As String, numFacture As String, siret As String)
        Try
            Dim sql As String = "UPDATE HistoriqueFactures SET Siret = @Siret WHERE NumOR = @NumOR AND NumFacture = @NumFacture"
            Using conn As New SqlConnection(ConfigurationManager.ConnectionStrings(BaseDeDonnees).ConnectionString)
                Using cmd As New SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@Siret", siret.Trim().Replace(" ", ""))
                    cmd.Parameters.AddWithValue("@NumOR", numOR)
                    cmd.Parameters.AddWithValue("@NumFacture", numFacture)

                    conn.Open()
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        Catch ex As Exception
            GestionnaireLog.Error("Erreur UpdateSiret : " & ex.Message)
        End Try
    End Sub

    Public Shared Sub UpdateSirenDemat(idFacture As String, siren As String)
        Try
            Dim sql As String = "UPDATE D_invoice SET Siren_Vend = @Siren WHERE IdFacture = @IdFacture"
            Using conn As New SqlConnection(ConfigurationManager.ConnectionStrings(BaseDeDonnees).ConnectionString)
                Using cmd As New SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@Siren", siren.Trim().Replace(" ", ""))
                    cmd.Parameters.AddWithValue("@IdFacture", idFacture)

                    conn.Open()
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        Catch ex As Exception
            GestionnaireLog.Error("Erreur UpdateSirenDemat : " & ex.Message)
        End Try
    End Sub

    Public Shared Sub UpdateRefFournisseurLigne(idFacture As String, numLigne As String, refFournisseur As String)
        Try
            Dim sql As String = "UPDATE D_invoice_lignes SET refArticleFournisseur = @RefFournisseur WHERE IdFacture = @IdFacture AND NumLigne = @NumLigne"
            Using conn As New SqlConnection(ConfigurationManager.ConnectionStrings(BaseDeDonnees).ConnectionString)
                Using cmd As New SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@RefFournisseur", refFournisseur.Trim())
                    cmd.Parameters.AddWithValue("@IdFacture", idFacture)
                    cmd.Parameters.AddWithValue("@NumLigne", numLigne)

                    conn.Open()
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        Catch ex As Exception
            GestionnaireLog.Error("Erreur UpdateRefFournisseurLigne : " & ex.Message)
        End Try
    End Sub

    Public Shared Sub MettreAJourRaisonSocialeDemat(idFacture As String, raisonSociale As String)
        Try
            Dim sql As String = "UPDATE D_invoice SET SocieteEmet = @RaisonSociale WHERE IdFacture = @IdFacture"
            Using conn As New SqlConnection(ConfigurationManager.ConnectionStrings(BaseDeDonnees).ConnectionString)
                Using cmd As New SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@RaisonSociale", raisonSociale)
                    cmd.Parameters.AddWithValue("@IdFacture", idFacture)

                    conn.Open()
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        Catch ex As Exception
            GestionnaireLog.Error("Erreur MettreAJourRaisonSocialeDemat : " & ex.Message)
        End Try
    End Sub

    Public Shared Sub MettreAJourStatutFactureDemat(idFacture As String, statut As String, Optional message As String = "")
        Try
            Dim sql As String = "UPDATE D_invoice SET Statut = @Statut, Message = @Message WHERE IdFacture = @IdFacture"
            Using conn As New SqlConnection(ConfigurationManager.ConnectionStrings(BaseDeDonnees).ConnectionString)
                Using cmd As New SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@Statut", statut)
                    cmd.Parameters.AddWithValue("@Message", message)
                    cmd.Parameters.AddWithValue("@IdFacture", idFacture)

                    conn.Open()
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        Catch ex As Exception
            GestionnaireLog.Error("Erreur MettreAJourStatutFactureDemat : " & ex.Message)
        End Try
    End Sub

    Public Shared Function GetFacturePdfFromDb(numFacture As String) As Byte()
        Try
            Dim sql As String = "SELECT TOP (1) FichierPDF FROM FacturesPDF WHERE NumFac = @NumFacture"

            Using conn As New System.Data.SqlClient.SqlConnection(ConfigurationManager.ConnectionStrings(BaseDeDonnees).ConnectionString)
                Using cmd As New System.Data.SqlClient.SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@NumFacture", If(String.IsNullOrEmpty(numFacture), DBNull.Value, numFacture))
                    conn.Open()
                    Dim result As Object = cmd.ExecuteScalar()
                    If result IsNot Nothing AndAlso Not IsDBNull(result) Then
                        If TypeOf result Is Byte() Then
                            Dim bytesResult As Byte() = CType(result, Byte())
                            ' Si le fichier binaire contient en fait le texte de la chaîne hexadécimale "0x2550..." (48='0', 120='x')
                            If bytesResult.Length >= 2 AndAlso bytesResult(0) = 48 AndAlso bytesResult(1) = 120 Then
                                Dim hexStr As String = System.Text.Encoding.ASCII.GetString(bytesResult).Trim()
                                hexStr = hexStr.Substring(2)
                                Dim realBytes As Byte() = New Byte(hexStr.Length \ 2 - 1) {}
                                For i As Integer = 0 To hexStr.Length - 1 Step 2
                                    realBytes(i \ 2) = Convert.ToByte(hexStr.Substring(i, 2), 16)
                                Next
                                Return realBytes
                            End If
                            Return bytesResult
                        ElseIf TypeOf result Is String Then
                            Dim hexStr As String = CType(result, String).Trim()
                            If hexStr.StartsWith("0x", StringComparison.OrdinalIgnoreCase) Then
                                hexStr = hexStr.Substring(2)
                            End If
                            ' Convertir la chaîne hexadécimale en tableau d'octets
                            Dim bytes As Byte() = New Byte(hexStr.Length \ 2 - 1) {}
                            For i As Integer = 0 To hexStr.Length - 1 Step 2
                                bytes(i \ 2) = Convert.ToByte(hexStr.Substring(i, 2), 16)
                            Next
                            Return bytes
                        End If
                    End If
                End Using
            End Using
        Catch ex As Exception
            GestionnaireLog.Error("Erreur lors de la récupération du PDF en base (FacturesPDF) pour NumFac=" & numFacture & " : " & ex.Message)
        End Try
        Return Nothing
    End Function

    Public Shared Function GetFacturesDematNonMatcheesAvecPrestation(codePrestaFournisseur As String) As DataTable
        If String.IsNullOrEmpty(codePrestaFournisseur) Then
            Return New DataTable()
        End If

        Try
            Dim sql As String = "SELECT DISTINCT f.IdFacture, f.NumeroTVA_Vend " &
                               "FROM D_invoice f " &
                               "INNER JOIN D_invoice_lignes l ON f.IdFacture = l.IdFacture " &
                               "WHERE l.refArticleFournisseur = '" & codePrestaFournisseur.Trim().Replace("'", "''") & "' " &
                               "AND f.Statut = 'PRESTATION_INEXISTANTE';"

            Dim acd As New AccesDonnees()
            Return acd.creation_datatable(sql, BaseDeDonnees)

        Catch ex As Exception
            GestionnaireLog.Error("Erreur récupération factures demat non matchées : " & ex.Message)
            Return New DataTable()
        End Try
    End Function
    Public Shared Function ObtenirFacturesDematEnAttente() As DataTable
        Try
            Dim sql As String = "SELECT TOP 10 IdFacture, NumeroFacture, SocieteEmet, numOr, MontantTotal, MontantHT, MontantTVA, DateEmi, DateEcheance, NumeroTVA_Vend " &
                               "FROM D_invoice " &
                               "WHERE Statut IN ('EN_ATTENTE', 'ERROR', 'A_INTEGRER') " &
                               "ORDER BY DateCreation DESC"

            Dim acd As New AccesDonnees()
            Return acd.creation_datatable(sql, BaseDeDonnees)
        Catch ex As Exception
            GestionnaireLog.Error("Erreur récupération factures Demat EN_ATTENTE : " & ex.Message)
            Return New DataTable()
        End Try
    End Function
    ''' <summary>
    ''' Récupère les lignes d'une facture dématérialisée
    ''' </summary>
    Public Shared Function ObtenirLignesDemat(idFacture As String) As DataTable
        Try
            Dim sql As String = "SELECT refArticleFournisseur, designationArticle, quantiteArticle, prixNetHtArticle, prixTotalHtArticle " &
                               "FROM D_invoice_lignes " &
                               "WHERE idFacture = @IdFacture"

            Dim acd As New AccesDonnees()
            Dim dt As New DataTable()

            Using conn As New System.Data.SqlClient.SqlConnection(ConfigurationManager.ConnectionStrings(BaseDeDonnees).ConnectionString)
                Using cmd As New System.Data.SqlClient.SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@IdFacture", idFacture)
                    Using da As New System.Data.SqlClient.SqlDataAdapter(cmd)
                        da.Fill(dt)
                    End Using
                End Using
            End Using

            Return dt
        Catch ex As Exception
            GestionnaireLog.Error("Erreur récupération lignes Demat : " & ex.Message)
            Return New DataTable()
        End Try
    End Function


End Class

