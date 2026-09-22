Imports System.Data
Imports System.IO
Imports System.Threading.Tasks
Imports APILP
Imports Exceptions
Imports Models
Imports Newtonsoft.Json.Linq
Imports ServiceOR

Public Class ServiceImportFacture

    ''' <summary>
    ''' Intègre une facture complète : extraction, enrichissement, enregistrement BDD, intégration LocPro
    ''' </summary>
    Public Shared Async Function integrerFacture(filePath As String, utilisateur As String) As Task(Of ResultatTraitementImportFacture)
        Dim resultat As New ResultatTraitementImportFacture() With {
            .NomFichier = Path.GetFileName(filePath)
        }
        Dim facture As ObjetMindeeFacture = Nothing
        Dim infosVeh As InfosVeh = Nothing
        Dim infosMouv As InfosMouvement = Nothing
        Dim infosFour As InfosFournisseur = Nothing

        Try
            ' ==================== ÉTAPE 1 : EXTRACTION MINDEE ====================
            GestionnaireLog.Info("[1/6] Extraction Mindee : " & resultat.NomFichier)
            facture = Await GestionnaireMindee.extraireInfosFacture(filePath)
            resultat.NumeroFacture = facture.GetNumFacture
            GestionnaireLog.Info("Facture extraite : " & resultat.NumeroFacture)

            ' ==================== ÉTAPE 2 : ENRICHISSEMENT ====================
            GestionnaireLog.Info("[2/6] Enrichissement des données")

            infosVeh = retournerInfosVeh(facture.GetImmatriculation)
            GestionnaireLog.Info("Véhicule trouvé : " & infosVeh.CodeParc & " (" & facture.GetImmatriculation & ")")

            infosMouv = retournerInfosMouvement(infosVeh.CodeParc, facture.GetDateFacture)
            GestionnaireLog.Info("OR trouvé : " & infosMouv.NumeroOR)

            infosFour = retournerInfosFournisseur(facture.GetSiret)
            GestionnaireLog.Info("Fournisseur : " & infosFour.CodeFournisseur)

            ' ==================== ÉTAPE 3 : VÉRIFICATION PRESTATIONS ====================
            GestionnaireLog.Info("[3/6] Vérification des correspondances prestations")

            Dim lignesNonMatchees As New List(Of String)()
            For Each ligne In facture.GetLignesFacture()
                ' Recherche par CodePrestaFournisseur (Reference) ET pas par Designation
                Dim regle As JObject = GestionnaireBddFacture.GetRegleCorrespondance(
                    infosFour.CodeFournisseur, ligne.Reference)

                If regle Is Nothing Then
                    ' Ajouter la référence + désignation pour plus de clarté
                    lignesNonMatchees.Add(ligne.Reference & " - " & ligne.Designation)
                End If
            Next

            ' ==================== ÉTAPE 4 : ENREGISTREMENT EN BDD ====================
            GestionnaireLog.Info("[4/6] Enregistrement en base de données")

            Dim statutInitial As String
            Dim messageInitial As String

            If lignesNonMatchees.Count > 0 Then
                ' Prestations non matchées → Enregistrer avec statut NON_MATCHED_SERVICES
                statutInitial = "PRESTATION_INEXISTANTE"
                messageInitial = lignesNonMatchees.Count.ToString() & " prestation(s) non identifiée(s) : " & String.Join(", ", lignesNonMatchees)
                GestionnaireLog.Warn(messageInitial)
            Else
                ' Toutes les prestations sont matchées → Enregistrer avec statut EN_ATTENTE
                statutInitial = "EN_ATTENTE"
                messageInitial = "Facture validée, en attente d'intégration LocPro"
                GestionnaireLog.Info("Toutes les prestations sont identifiées (" & facture.GetLignesFacture().Count.ToString() & " lignes)")
            End If

            ' Enregistrement en BDD avec le statut approprié
            GestionnaireFactureFournisseur.enregistrerFacture(
                facture, infosVeh, infosMouv, infosFour, utilisateur, resultat.NomFichier, statutInitial, messageInitial
            )

            resultat.NumeroOR = infosMouv.NumeroOR

            ' Si des prestations ne sont pas matchées, on s'arrête ici
            If lignesNonMatchees.Count > 0 Then
                resultat.Statut = "PRESTATION_INEXISTANTE"
                resultat.Message = messageInitial
                GestionnaireLog.Warn("Traitement arrêté : " & resultat.Message)
                Return resultat
            End If

            ' ==================== ÉTAPE 5 : INTÉGRATION LOCPRO ====================
            ' Cette étape n'est atteinte QUE si toutes les prestations sont matchées
            GestionnaireLog.Info("[5/6] Intégration dans LocPro")

            ' 5.1 Création de la facture
            Dim pkFacLocPro As String = APILocPro.createFactureTest(facture.GetNomFrounisseur, 0, facture.GetTotalHT, facture.GetTotalHT,
                                                                facture.GetTVA, facture.GetTVA, facture.GetTotalTTC, facture.GetTotalTTC,
                                                                "OR", 2, "VIR_BS", "CCB", infosFour.CodeFournisseur, codeModele:=infosVeh.CodeModele,
                                                                codeParc:=infosVeh.CodeParc, facNm:=facture.GetNumFacture, nmDoc:=infosMouv.NumeroOR,
                                                                startDate:=infosMouv.DateDepart, termDate:=infosMouv.DateArrivee, mileage:=facture.GetCompteurKm, billingDate:=facture.GetDateFacture)
            GestionnaireLog.Info("Facture LocPro créée : PK=" & pkFacLocPro)

            ' 5.2 Création des prestations dans LocPro
            Dim numLigne As Integer = 1
            For Each ligne In facture.GetLignesFacture()
                Dim regle As JObject = GestionnaireBddFacture.GetRegleCorrespondance(infosFour.CodeFournisseur, ligne.Reference)

                If regle IsNot Nothing AndAlso regle("prestations") IsNot Nothing Then
                    Dim prestationsArray As JArray = CType(regle("prestations"), JArray)

                    Dim prixTTC As Double = ligne.MontantNetHT * (1 + (ligne.TVA / 100))
                    Dim montantTVA As Double = ligne.MontantNetHT * (ligne.TVA / 100)

                    For Each prestaObj In prestationsArray
                        Dim prestaDetail As JObject = CType(prestaObj, JObject)
                        Dim codePrestaLP As String = prestaDetail("code").ToString()
                        Dim montantConfig As JObject = CType(prestaDetail("montant_ht"), JObject)

                        ' Calculer le montant HT de manière dynamique
                        Dim montantHTParPresta As Double = CalculerMontant(montantConfig, ligne, codePrestaLP)

                        ' Calculer les autres montants proportionnellement
                        Dim montantTVAParPresta As Double = montantHTParPresta * (ligne.TVA / 100)
                        Dim prixTTCParPresta As Double = montantHTParPresta * (1 + (ligne.TVA / 100))
                        Dim prixUnitaireHTParPresta As Double = If(ligne.Quantite > 0, montantHTParPresta / ligne.Quantite, montantHTParPresta)

                        APILocPro.createPrestationTest(pkFacLocPro, ligne.Designation, codePrestaLP, 1, False, 2, ligne.Quantite,
                                                  "CCB", numLigne, facture.GetCompteurKm, 0, prixTTCParPresta, prixUnitaireHTParPresta,
                                                  montantTVAParPresta, montantHTParPresta, ligne.TVA, infosVeh.CodeParc)
                        numLigne += 1
                    Next
                End If
            Next
            ' 5.3 Création d'une ligne négative pour la remise globale si existante
            Dim remiseGlobalHT As Double = facture.GetRemiseHT
            GestionnaireLog.Info("Valeur de remiseGlobalHT lue par Mindee: " & remiseGlobalHT.ToString())
            If Math.Abs(remiseGlobalHT) > 0 Then
                Dim montantRemiseFixeHT As Double = -Math.Abs(remiseGlobalHT) ' Assure un montant négatif
                ' On recupère les infos de la dernière ligne traitée pour s'aligner
                Dim lastCodePrestaLP As String ' Valeur par défaut
                Dim lastTVA As Double = 0
                If facture.GetLignesFacture().Count > 0 Then
                    Dim lastLigne = facture.GetLignesFacture().Last()
                    lastTVA = lastLigne.TVA
                    Dim regleLast As JObject = GestionnaireBddFacture.GetRegleCorrespondance(infosFour.CodeFournisseur, lastLigne.Reference)
                    If regleLast IsNot Nothing AndAlso regleLast("prestations") IsNot Nothing Then
                        Dim prestaArray As JArray = CType(regleLast("prestations"), JArray)
                        If prestaArray.Count > 0 Then
                            lastCodePrestaLP = CType(prestaArray.Last(), JObject)("code").ToString()
                        End If
                    End If
                End If

                Dim montantTVARemise As Double = montantRemiseFixeHT * (lastTVA / 100)
                Dim montantTTCRemise As Double = montantRemiseFixeHT * (1 + (lastTVA / 100))

                APILocPro.createPrestationTest(pkFacLocPro, "Remise globale", lastCodePrestaLP, 1, False, 2, 1,
                                          "CCB", numLigne, facture.GetCompteurKm, 0, montantTTCRemise, montantRemiseFixeHT,
                                          montantTVARemise, montantRemiseFixeHT, lastTVA, infosVeh.CodeParc)
                numLigne += 1
            End If
            GestionnaireLog.Info((numLigne - 1).ToString() & " prestation(s) LocPro créée(s)")

            ' ==================== ÉTAPE 6 : FINALISATION ====================
            GestionnaireLog.Info("[6/6] Finalisation")

            ' Mise à jour du statut en BDD → SUCCES
            GestionnaireFactureFournisseur.mettreAJourStatutApresIntegration(
                infosMouv.NumeroOR,
                facture.GetNumFacture,
                "SUCCES",
                "Intégration réussie dans LocPro"
            )

            resultat.Statut = "SUCCES"
            resultat.Message = "Facture intégrée avec succès"

            GestionnaireLog.Info("Traitement terminé avec succès")

            ' ==================== GESTION DES ERREURS ====================
        Catch ex As FactureDejaExistanteException
            resultat.Statut = "FACTURE_EXISTANTE"
            resultat.Message = ex.Message
            GestionnaireLog.Warn(ex.Message)

        Catch ex As MindeeException
            resultat.Statut = "ERREUR_EXTRACTION_MINDEE"
            resultat.Message = "Erreur d'extraction : " & ex.Message
            GestionnaireLog.Error("Mindee : " & ex.Message)

        Catch ex As CodeParcIntrouvable
            resultat.Statut = "PARC_INTROUVABLE"
            Dim immat As String = If(facture IsNot Nothing, facture.GetImmatriculation, "inconnue")
            resultat.Message = "Véhicule introuvable : " & immat
            GestionnaireLog.Error(resultat.Message)

        Catch ex As ORIntrouvableException
            resultat.Statut = "OR_INTROUVABLE"
            Dim msgOR As String = "OR introuvable"
            If infosVeh IsNot Nothing AndAlso facture IsNot Nothing Then
                msgOR = "OR introuvable pour le parc " & infosVeh.CodeParc & " à la date " & Format(facture.GetDateFacture, "dd/MM/yyyy")
            End If
            resultat.Message = msgOR
            GestionnaireLog.Error(resultat.Message)

        Catch ex As FournisseurIntrouvableException
            resultat.Statut = "FOURNISSEUR_INTROUVABLE"
            Dim siret As String = If(facture IsNot Nothing, facture.GetSiret, "inconnu")
            resultat.Message = "Fournisseur introuvable (SIRET : " & siret & ")"
            GestionnaireLog.Error(resultat.Message)

        Catch ex As SiretNonLuException
            resultat.Statut = "SIRET_NON_LU"
            resultat.Message = "SIRET non lu sur la facture"
            GestionnaireLog.Error(resultat.Message)

        Catch ex As EnregistrementFactureException
            resultat.Statut = "ERREUR_BDD"
            resultat.Message = "Échec de l'enregistrement en base de données"
            Dim innerMsg As String = If(ex.InnerException IsNot Nothing, ex.InnerException.Message, "")
            GestionnaireLog.Error("BDD : " & innerMsg)

        Catch ex As ApiLocProException
            resultat.Statut = "ERREUR_APILP"
            resultat.Message = "Erreur LocPro : " & ex.Message
            GestionnaireLog.Error("API LocPro : " & ex.Message)

            If infosMouv IsNot Nothing AndAlso facture IsNot Nothing Then
                Try
                    GestionnaireFactureFournisseur.mettreAJourStatutApresIntegration(
                        infosMouv.NumeroOR,
                        facture.GetNumFacture,
                        "ERREUR_APILP",
                        "Échec intégration LocPro : " & ex.Message
                    )
                    resultat.NumeroOR = infosMouv.NumeroOR
                Catch
                    GestionnaireLog.Error("Impossible de mettre à jour le statut en BDD")
                End Try
            End If

        Catch ex As Exception
            resultat.Statut = "ERREUR_GENERIQUE"
            resultat.Message = "Erreur inattendue : " & ex.Message
            GestionnaireLog.Error("Erreur inattendue : " & ex.ToString())
        End Try

        Return resultat
    End Function



    Private Shared Function CalculerMontant(montantConfig As JObject, ligne As ObjetMindeeFacture.LigneFacture, codePrestaLP As String) As Double
        Dim operation As String = montantConfig("operation").ToString().ToLower()

        ' Exécuter l'opération de manière dynamique
        Select Case operation
            Case "fixed"
                Return CDbl(montantConfig("valeur").ToString())

            Case "multiply"
                Dim baseValue As Double = ObtenirValeurBase(montantConfig("base").ToString(), ligne, codePrestaLP)
                Dim facteur As Double = CDbl(montantConfig("facteur").ToString())
                Return baseValue * facteur

            Case "add"
                Dim baseValue As Double = ObtenirValeurBase(montantConfig("base").ToString(), ligne, codePrestaLP)
                Dim valeur As Double = CDbl(montantConfig("valeur").ToString())
                Return baseValue + valeur

            Case "subtract"
                Dim baseValue As Double = ObtenirValeurBase(montantConfig("base").ToString(), ligne, codePrestaLP)
                Dim valeur As Double = CDbl(montantConfig("valeur").ToString())
                Return Math.Max(0, baseValue - valeur)

            Case "divide"
                Dim baseValue As Double = ObtenirValeurBase(montantConfig("base").ToString(), ligne, codePrestaLP)
                Dim diviseur As Double = CDbl(montantConfig("diviseur").ToString())
                Return baseValue / diviseur

            Case Else
                ' Opération inconnue
                Throw New FormatRegleException("Opération inconnue '" & operation & "' pour la prestation " & codePrestaLP & ". Opérations disponibles : fixed, multiply, add, subtract, divide")
        End Select
    End Function

    ''' <summary>
    ''' Obtient la valeur de base depuis les données de la ligne de facture
    ''' </summary>
    Private Shared Function ObtenirValeurBase(nomBase As String, ligne As ObjetMindeeFacture.LigneFacture, codePrestaLP As String) As Double
        Select Case nomBase.ToLower()
            Case "ligne_montant_ht"
                Return ligne.MontantNetHT

            Case "ligne_prix_unitaire_ht"
                Return ligne.PrixUnitaireHT

            Case "ligne_quantite"
                Return ligne.Quantite

            Case "ligne_remise"
                Return ligne.TauxRemise

            Case Else
                Throw New FormatRegleException("Base inconnue '" & nomBase & "' pour la prestation " & codePrestaLP & ". Bases disponibles : ligne_montant_ht, ligne_prix_unitaire_ht, ligne_quantite, ligne_remise")
        End Select
    End Function
End Class

Public Class ServiceOR
    Public Shared Function retournerInfosFournisseur(Siret As String) As InfosFournisseur
        If String.IsNullOrEmpty(Siret) Then Throw New SiretNonLuException()

        ' 1. Requête SQL via la couche BDD
        Dim dt As DataTable = GestionnaireBddFacture.retournerFournisseur(Siret)

        ' 2. Vérification du résultat
        If dt Is Nothing OrElse dt.Rows.Count = 0 Then
            Throw New FournisseurIntrouvableException("Aucun fournisseur trouvé pour le siret : " & Siret)
        End If

        ' 3. Lecture de la première ligne
        Dim row As DataRow = dt.Rows(0)

        Dim infos As New InfosFournisseur With {
            .CodeFournisseur = row("F050KY").ToString.Trim(),
            .RaisonSociale = If(row.Table.Columns.Contains("F020RAISON") AndAlso row("F020RAISON") IsNot DBNull.Value, row("F020RAISON").ToString().Trim(), "")
        }

        Return infos
    End Function

    Public Shared Function retournerInfosVeh(immat As String) As InfosVeh
        ' 1. Formater l'immatriculation
        Dim immatFormatee As String = FormaterImmat(immat)

        ' 2. Requête en base
        Dim dt As DataTable = GestionnaireBddFacture.retournerInfosVeh(immatFormatee)

        ' 3. Vérification du résultat
        If dt Is Nothing OrElse dt.Rows.Count = 0 Then
            Throw New CodeParcIntrouvable("Aucun véhicule trouvé avec l'immatriculation " & immatFormatee & "")
        End If

        Dim row As DataRow = dt.Rows(0)
        Dim infos As New InfosVeh With {
            .CodeParc = row("code_parc").ToString().Trim(),
            .CodeModele = row("code_modele").ToString().Trim()
        }

        Return infos
    End Function

    Public Shared Function retournerInfosMouvement(codeParc As String, dateFacture As Date) As InfosMouvement
        If String.IsNullOrEmpty(codeParc) Then Return Nothing

        ' 1. Requête SQL via la couche BDD
        Dim dt As DataTable = GestionnaireBddFacture.retournerInfosMouvement(codeParc, dateFacture)

        ' 2. Vérification du résultat
        If dt Is Nothing OrElse dt.Rows.Count = 0 Then
            Throw New ORIntrouvableException("Aucun mouvement trouvé pour le véhicule " & codeParc & " en date du " & dateFacture)
        End If

        ' 3. Lecture de la première ligne
        Dim row As DataRow = dt.Rows(0)

        Dim infos As New InfosMouvement With {
            .NumeroOR = If(row("K570400EVT") IsNot DBNull.Value, row("K570400EVT").ToString().Trim(), String.Empty),
            .DateDepart = If(row("F570DTDEP") IsNot DBNull.Value, Convert.ToDateTime(row("F570DTDEP")), Date.MinValue),
            .DateArrivee = If(row("F570DTARR") IsNot DBNull.Value, Convert.ToDateTime(row("F570DTARR")), Date.MinValue)
        }

        Return infos
    End Function


    Public Shared Function FormaterImmat(immat As String) As String
        If String.IsNullOrEmpty(immat) Then Return immat

        immat = immat.Trim().ToUpper()

        If immat.Length = 7 Then
            Return immat.Substring(0, 2) & "-" & immat.Substring(2, 3) & "-" & immat.Substring(5, 2)
        Else
            Return immat
        End If
    End Function

End Class

Public Class ResultatTraitementImportFacture

    ' ===== INFORMATIONS DE BASE =====
    Public Property NomFichier As String
    Public Property NumeroFacture As String
    Public Property NumeroOR As String

    ' ===== STATUT DU TRAITEMENT =====
    Public Property Statut As String
    Public Property Message As String

    ' ===== CONSTRUCTEUR =====
    Public Sub New()
    End Sub

End Class

Public Class InfosFournisseur
    Public Property CodeFournisseur As String
    Public Property RaisonSociale As String
End Class

Public Class InfosVeh
    Public Property CodeParc As String
    Public Property CodeModele As String
End Class

Public Class InfosMouvement
    Public Property NumeroOR As String
    Public Property DateDepart As Date
    Public Property DateArrivee As Date
End Class