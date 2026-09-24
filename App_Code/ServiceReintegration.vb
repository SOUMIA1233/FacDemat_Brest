Imports System
Imports System.Collections.Generic
Imports System.Data
Imports System.Threading.Tasks
Imports APILP
Imports Exceptions
Imports Newtonsoft.Json.Linq

''' <summary>
''' Service de ré-intégration et re-matchage des factures fournisseur
''' </summary>
Public Class ServiceReintegration

#Region "RÉ-INTÉGRATION PAR LOT"
    ''' <summary>
    ''' Ré-intègre toutes les factures EN_ATTENTE dans LocPro
    ''' </summary>
    Public Shared Function ReintegrerFacturesParLot() As ResultatLot
        Dim resultat As New ResultatLot()

        Try
            GestionnaireLog.Info("DÉBUT TRAITEMENT PAR LOT")

            ' Récupérer toutes les factures EN_ATTENTE
            Dim dtFactures As DataTable = GestionnaireBddFacture.ObtenirFacturesEnAttente()
            If dtFactures Is Nothing OrElse dtFactures.Rows.Count = 0 Then
                GestionnaireLog.Info("Aucune facture EN_ATTENTE à traiter")
                Return resultat
            End If

            GestionnaireLog.Info(dtFactures.Rows.Count & " facture(s) EN_ATTENTE à traiter")

            ' Traiter chaque facture
            For Each row As DataRow In dtFactures.Rows
                Dim numOR As String = row("NumOR").ToString().Trim()
                Dim numFacture As String = row("NumFacture").ToString().Trim()
                Try
                    ' Ré-intégrer la facture
                    Dim resultatFacture As ResultatReintegration = ReintegrerFacture(row)

                    If resultatFacture.Succes Then
                        resultat.NbSucces += 1
                    Else
                        resultat.NbEchecs += 1
                        resultat.DetailsEchecs.Add(numFacture & " : " & resultatFacture.Message)
                    End If
                Catch ex As Exception
                    resultat.NbEchecs += 1
                    Dim erreur As String = numFacture & " : " & ex.Message
                    resultat.DetailsEchecs.Add(erreur)
                    GestionnaireLog.Error("Erreur traitement facture " & numFacture & " : " & ex.ToString())
                End Try
            Next
            GestionnaireLog.Info("FIN TRAITEMENT PAR LOT")
            GestionnaireLog.Info("Succès : " & resultat.NbSucces & " | Échecs : " & resultat.NbEchecs)

        Catch ex As Exception
            GestionnaireLog.Error("Erreur critique traitement par lot : " & ex.ToString())
        End Try
        Return resultat
    End Function

    ''' <summary>
    ''' Ré-intègre une facture dans LocPro
    ''' </summary>
    Private Shared Function ReintegrerFacture(factureRow As DataRow) As ResultatReintegration
        Dim resultat As New ResultatReintegration()
        Dim numOR As String = factureRow("NumOR").ToString().Trim()
        Dim numFacture As String = factureRow("NumFacture").ToString().Trim()

        resultat.NumFacture = numFacture

        Try
            GestionnaireLog.Info("Facture " & numFacture & " - Début intégration")

            ' ÉTAPE 1 : Créer la facture dans LocPro
            Dim pkFacLocPro As String = CreerFactureLocPro(factureRow)
            If String.IsNullOrEmpty(pkFacLocPro) Then
                Throw New Exception("Échec création facture LocPro")
            End If
            GestionnaireLog.Info("Facture " & numFacture & " - Facture créée (ID: " & pkFacLocPro & ")")

            ' ÉTAPE 2 : Créer les prestations
            Dim nbPrestations As Integer = CreerPrestationsLocPro(pkFacLocPro, factureRow)
            GestionnaireLog.Info("Facture " & numFacture & " - " & nbPrestations & " prestation(s) créée(s)")

            ' ÉTAPE 3 : Mettre à jour le statut en SUCCES
            GestionnaireBddFacture.MettreAJourStatutFactureSucces(numOR, numFacture)

            GestionnaireLog.Info("Facture " & numFacture & " - Statut: SUCCES")

            resultat.Succes = True
            resultat.Message = "Intégration réussie"

        Catch ex As ApiLocProException
            ' Erreur API LocPro
            Dim msgErreur As String = "Erreur API LocPro : " & ex.Message
            GestionnaireLog.Error("Facture " & numFacture & " - " & msgErreur)
            GestionnaireBddFacture.MettreAJourStatutFactureErreur(numOR, numFacture, msgErreur)

            resultat.Succes = False
            resultat.Message = msgErreur
        Catch ex As Exception
            ' Erreur générale
            Dim msgErreur As String = "Erreur : " & ex.Message
            GestionnaireLog.Error("Facture " & numFacture & " - " & msgErreur)
            GestionnaireBddFacture.MettreAJourStatutFactureErreur(numOR, numFacture, msgErreur)

            resultat.Succes = False
            resultat.Message = msgErreur
        End Try
        Return resultat
    End Function

    ''' <summary>
    ''' Crée la facture dans LocPro via l'API
    ''' </summary>
    Private Shared Function CreerFactureLocPro(factureRow As DataRow) As String
        Try
            ' Récupérer les valeurs
            Dim libelle As String = factureRow("RaisonSociale").ToString()
            Dim montantHT As Double = Convert.ToDouble(factureRow("TotalHT"))
            Dim montantTVA As Double = Convert.ToDouble(factureRow("TotalTVA"))
            Dim montantTTC As Double = Convert.ToDouble(factureRow("TotalTTC"))
            Dim codeFournisseur As String = factureRow("CodeFournisseur").ToString().Trim()
            Dim codeModele As String = If(IsDBNull(factureRow("CodeModele")), Nothing, factureRow("CodeModele").ToString().Trim())
            Dim codeParc As String = If(IsDBNull(factureRow("CodeParc")), Nothing, factureRow("CodeParc").ToString().Trim())
            Dim numFacture As String = factureRow("NumFacture").ToString().Trim()
            Dim numOR As String = factureRow("NumOR").ToString().Trim()
            Dim dateDebut As DateTime = If(IsDBNull(factureRow("DateDebMouv")), Nothing, Convert.ToDateTime(factureRow("DateDebMouv")))
            Dim dateFin As DateTime = If(IsDBNull(factureRow("DateFinMouv")), Nothing, Convert.ToDateTime(factureRow("DateFinMouv")))
            Dim compteurKm As Integer = If(IsDBNull(factureRow("CompteurKm")), 0, Convert.ToInt32(factureRow("CompteurKm")))

            ' Appel API
            Dim pkFacture As String = APILocPro.createFacture(
                libelle:=libelle,
                status:="2",
                montantRes:=0,
                montantHTDevise:=montantHT,
                montantHT:=montantHT,
                montantTVA:=montantTVA,
                montantTVADevise:=montantTVA,
                montantTTC:=montantTTC,
                montantTTCDevise:=montantTTC,
                typeFac:="OR",
                accountingType:="2",
                moyenPaiem:="VIR_BS",
                agence:="CCB",
                codeClient:=codeFournisseur,
                codeModele:=codeModele,
                codeParc:=codeParc,
                facNm:=numFacture,
                nmDoc:=numOR,
                startDate:=dateDebut,
                termDate:=dateFin,
                mileage:=compteurKm
            )

            Return pkFacture

        Catch ex As Exception
            GestionnaireLog.Error("Erreur création facture LocPro : " & ex.Message)
            Throw
        End Try
    End Function

    ''' <summary>
    ''' Crée les prestations dans LocPro
    ''' </summary>
    Private Shared Function CreerPrestationsLocPro(pkFacLocPro As String, factureRow As DataRow) As Integer
        Dim nbPrestationsCreees As Integer = 0

        Try
            Dim numOR As String = factureRow("NumOR").ToString().Trim()
            Dim numFacture As String = factureRow("NumFacture").ToString().Trim()
            Dim codeFournisseur As String = factureRow("CodeFournisseur").ToString().Trim()
            Dim codeParc As String = If(IsDBNull(factureRow("CodeParc")), Nothing, factureRow("CodeParc").ToString().Trim())
            Dim compteurKm As Integer = If(IsDBNull(factureRow("CompteurKm")), 0, Convert.ToInt32(factureRow("CompteurKm")))

            ' Récupérer les lignes de la facture
            Dim lignes As DataTable = GestionnaireBddFacture.ObtenirLignesFacture(numOR, numFacture)

            If lignes Is Nothing OrElse lignes.Rows.Count = 0 Then
                Return 0
            End If

            Dim numLigne As Integer = 1

            ' Pour chaque ligne
            For Each ligne As DataRow In lignes.Rows
                Dim codePrestaFournisseur As String = ligne("CodePrestaFournisseur").ToString().Trim()

                ' Récupérer la règle JSON
                Dim regleJObject As JObject = GestionnaireBddFacture.GetRegleCorrespondance(codeFournisseur, codePrestaFournisseur)

                ' Parser le JSON
                Dim jPrestations As JArray = CType(regleJObject("prestations"), JArray)

                ' Pour chaque prestation dans la règle
                For Each jPresta As JObject In jPrestations
                    Try
                        Dim codePrestaLP As String = jPresta("code").ToString()
                        Dim jMontantHT As JObject = CType(jPresta("montant_ht"), JObject)

                        ' Calculer le montant HT
                        Dim montantHTParPresta As Double = CalculateurMontants.CalculerMontantDepuisJSON(jMontantHT, ligne)

                        ' Si montant = 0, ne pas créer la prestation (incluse)
                        If montantHTParPresta = 0 Then
                            Continue For
                        End If

                        ' Calculer les autres montants
                        Dim tauxTVA As Double = Convert.ToDouble(ligne("TVA"))
                        Dim montantTVAParPresta As Double = montantHTParPresta * (tauxTVA / 100)
                        Dim montantTTCParPresta As Double = montantHTParPresta * (1 + (tauxTVA / 100))

                        ' Récupérer les autres infos
                        Dim descr As String = ligne("Descr").ToString()
                        Dim qte As Double = Convert.ToDouble(ligne("Qte"))
                        Dim tauxRemise As Double = Convert.ToDouble(ligne("TauxRemise"))

                        Dim prixUnitaireHTParPresta As Double = If(qte > 0, montantHTParPresta / qte, montantHTParPresta)

                        ' Appel API création prestation
                        Dim pkPrestation As String = APILocPro.createPrestation(
                            idFact:=pkFacLocPro,
                            descLigne:=descr,
                            codeLigne:=codePrestaLP,
                            codeTva:="1",
                            isWithoutTax:=False,
                            modeCalc:="2",
                            qte:=qte,
                            agence:="CCB",
                            lineNumber:=numLigne,
                            km:=compteurKm,
                            discountAmount:=0,
                            includingTaxAmount:=montantTTCParPresta,
                            unitPrice:=prixUnitaireHTParPresta,
                            vatAmount:=montantTVAParPresta,
                            excludingTaxAmount:=montantHTParPresta,
                            vatRate:=tauxTVA.ToString(),
                            codeParc:=codeParc
                        )

                        GestionnaireLog.Info("  → Prestation " & codePrestaLP & " créée (ID: " & pkPrestation & ", Montant: " & montantHTParPresta.ToString("N2") & "€)")
                        nbPrestationsCreees += 1
                        numLigne += 1

                    Catch ex As ApiLocProException
                        ' Erreur sur une prestation : logger mais continuer
                        GestionnaireLog.Error("  → Erreur création prestation : " & ex.Message)
                        ' On continue avec les autres prestations
                    End Try
                Next
            Next

            ' Ajouter la remise globale si elle existe
            Dim remiseHT As Double = If(IsDBNull(factureRow("RemiseHT")), 0, Convert.ToDouble(factureRow("RemiseHT")))
            If Math.Abs(remiseHT) > 0 Then
                Dim montantRemiseFixeHT As Double = -Math.Abs(remiseHT) ' Assure un montant négatif
                Dim lastCodePrestaLP As String
                Dim lastTauxTVA As Double = 0
                If lignes.Rows.Count > 0 Then
                    Dim lastLigne As DataRow = lignes.Rows(lignes.Rows.Count - 1)
                    lastTauxTVA = Convert.ToDouble(lastLigne("TVA"))
                    Dim codePrestaFourn As String = lastLigne("CodePrestaFournisseur").ToString().Trim()
                    Dim regleLast As JObject = GestionnaireBddFacture.GetRegleCorrespondance(codeFournisseur, codePrestaFourn)
                    If regleLast IsNot Nothing AndAlso regleLast("prestations") IsNot Nothing Then
                        Dim prestaArray As JArray = CType(regleLast("prestations"), JArray)
                        If prestaArray.Count > 0 Then
                            lastCodePrestaLP = CType(prestaArray.Last(), JObject)("code").ToString()
                        End If
                    End If
                End If

                Dim montantTVARemise As Double = montantRemiseFixeHT * (lastTauxTVA / 100)
                Dim montantTTCRemise As Double = montantRemiseFixeHT * (1 + (lastTauxTVA / 100))

                Try
                    Dim pkPrestationRemise As String = APILocPro.createPrestation(
                        idFact:=pkFacLocPro,
                        descLigne:="Remise globale",
                        codeLigne:=lastCodePrestaLP,
                        codeTva:="1",
                        isWithoutTax:=False,
                        modeCalc:="2",
                        qte:=1,
                        agence:="CCB",
                        lineNumber:=numLigne,
                        km:=compteurKm,
                        discountAmount:=0,
                        includingTaxAmount:=montantTTCRemise,
                        unitPrice:=montantRemiseFixeHT,
                        vatAmount:=montantTVARemise,
                        excludingTaxAmount:=montantRemiseFixeHT,
                        vatRate:=lastTauxTVA.ToString(),
                        codeParc:=codeParc
                    )
                    GestionnaireLog.Info("  → Prestation globale Remise créée (ID: " & pkPrestationRemise & ", Montant: " & montantRemiseFixeHT.ToString("N2") & "€)")
                    nbPrestationsCreees += 1
                Catch ex As ApiLocProException
                    GestionnaireLog.Error("  → Erreur création prestation Remise globale : " & ex.Message)
                End Try
            End If

        Catch ex As Exception
            GestionnaireLog.Error("Erreur création prestations : " & ex.Message)
            Throw
        End Try

        Return nbPrestationsCreees
    End Function

    ''' <summary>
    ''' Vérifie l'état de toutes les factures dématérialisées (siret/prestations) sans les intégrer
    ''' </summary>
    Public Shared Function VerifierFacturesDematParLot() As ResultatLot
        Dim resultat As New ResultatLot()

        Try
            GestionnaireLog.Info("DÉBUT TRAITEMENT PAR LOT DEMAT")

            ' Récupérer toutes les factures à vérifier
            Dim dtFactures As DataTable = GestionnaireBddFacture.ObtenirFacturesDematAVerifier()
            If dtFactures Is Nothing OrElse dtFactures.Rows.Count = 0 Then
                GestionnaireLog.Info("Aucune facture Demat à traiter")
                Return resultat
            End If

            GestionnaireLog.Info(dtFactures.Rows.Count & " facture(s) Demat à vérifier/valider")

            ' Traiter chaque facture
            For Each row As DataRow In dtFactures.Rows
                Dim idFacture As String = row("IdFacture").ToString().Trim()
                Dim numFacture As String = row("NumeroFacture").ToString().Trim()
                Try
                    ' Au lieu de réintégrer directement, on valide la facture
                    Dim siret As String = ""
                    If row.Table.Columns.Contains("Siret_Vend") AndAlso Not IsDBNull(row("Siret_Vend")) AndAlso Not String.IsNullOrWhiteSpace(row("Siret_Vend").ToString()) Then
                        siret = row("Siret_Vend").ToString().Trim()
                    ElseIf row.Table.Columns.Contains("Siren_Vend") AndAlso Not IsDBNull(row("Siren_Vend")) AndAlso Not String.IsNullOrWhiteSpace(row("Siren_Vend").ToString()) Then
                        siret = row("Siren_Vend").ToString().Trim()
                    End If
                    Dim errMsg As String = ""
                    Dim estValide As Boolean = RetraiterFactureSiretDemat(idFacture, siret, errMsg)

                    If estValide Then
                        resultat.NbSucces += 1
                    Else
                        resultat.NbEchecs += 1
                        resultat.DetailsEchecs.Add(numFacture & " : " & errMsg)
                    End If
                Catch ex As Exception
                    resultat.NbEchecs += 1
                    Dim erreur As String = numFacture & " : " & ex.Message
                    resultat.DetailsEchecs.Add(erreur)
                    GestionnaireLog.Error("Erreur traitement facture Demat " & numFacture & " : " & ex.ToString())
                End Try
            Next
            GestionnaireLog.Info("FIN TRAITEMENT PAR LOT DEMAT")
            GestionnaireLog.Info("Succès : " & resultat.NbSucces & " | Échecs : " & resultat.NbEchecs)

        Catch ex As Exception
            GestionnaireLog.Error("Erreur critique traitement par lot Demat : " & ex.ToString())
        End Try
        Return resultat
    End Function

    ''' <summary>
    ''' Ré-intègre une facture dématérialisée dans LocPro
    ''' </summary>
    Public Shared Function ReintegrerFactureDemat(factureRow As DataRow) As ResultatReintegration
        Dim resultat As New ResultatReintegration()
        Dim idFacture As String = factureRow("IdFacture").ToString().Trim()
        Dim numFacture As String = factureRow("NumeroFacture").ToString().Trim()
        Dim siret As String = ""
        If factureRow.Table.Columns.Contains("Siret_Vend") AndAlso Not IsDBNull(factureRow("Siret_Vend")) AndAlso Not String.IsNullOrWhiteSpace(factureRow("Siret_Vend").ToString()) Then
            siret = factureRow("Siret_Vend").ToString().Trim()
        ElseIf factureRow.Table.Columns.Contains("Siren_Vend") AndAlso Not IsDBNull(factureRow("Siren_Vend")) AndAlso Not String.IsNullOrWhiteSpace(factureRow("Siren_Vend").ToString()) Then
            siret = factureRow("Siren_Vend").ToString().Trim()
        End If

        resultat.NumFacture = numFacture

        Try
            GestionnaireLog.Info("Facture Demat " & numFacture & " - Début intégration")

            ' Récupérer le code fournisseur
            Dim infosFour As InfosFournisseur = ServiceOR.retournerInfosFournisseur(siret)
            If infosFour Is Nothing Then
                Throw New Exception("Fournisseur introuvable avec SIRET : " & siret)
            End If

            ' ÉTAPE 1 : Créer la facture dans LocPro
            Dim pkFacLocPro As String = CreerFactureLocProDemat(factureRow, infosFour.CodeFournisseur)
            If String.IsNullOrEmpty(pkFacLocPro) Then
                Throw New Exception("Échec création facture LocPro")
            End If
            GestionnaireLog.Info("Facture Demat " & numFacture & " - Facture créée (ID: " & pkFacLocPro & ")")

            ' ÉTAPE 2 : Créer les prestations
            Dim nbPrestations As Integer = CreerPrestationsLocProDemat(pkFacLocPro, idFacture, infosFour.CodeFournisseur)
            GestionnaireLog.Info("Facture Demat " & numFacture & " - " & nbPrestations & " prestation(s) créée(s)")

            ' ÉTAPE 3 : Mettre à jour le statut en SUCCES
            GestionnaireBddFacture.MettreAJourStatutFactureDemat(idFacture, "SUCCES", "Intégration réussie dans Locpro")

            GestionnaireLog.Info("Facture Demat " & numFacture & " - Statut: SUCCES")

            resultat.Succes = True
            resultat.Message = "Intégration réussie"

        Catch ex As ApiLocProException
            Dim msgErreur As String = "Erreur API LocPro : " & ex.Message
            GestionnaireLog.Error("Facture Demat " & numFacture & " - " & msgErreur)
            GestionnaireBddFacture.MettreAJourStatutFactureDemat(idFacture, "ERROR", msgErreur)

            resultat.Succes = False
            resultat.Message = msgErreur
        Catch ex As Exception
            Dim msgErreur As String = "Erreur : " & ex.Message
            GestionnaireLog.Error("Facture Demat " & numFacture & " - " & msgErreur)
            GestionnaireBddFacture.MettreAJourStatutFactureDemat(idFacture, "ERROR", msgErreur)

            resultat.Succes = False
            resultat.Message = msgErreur
        End Try
        Return resultat
    End Function

    ''' <summary>
    ''' Crée la facture Demat dans LocPro via l'API
    ''' </summary>
    Private Shared Function CreerFactureLocProDemat(factureRow As DataRow, codeFournisseur As String) As String
        Try
            Dim numFacture As String = factureRow("NumeroFacture").ToString().Trim()
            Dim societe As String = factureRow("SocieteEmet").ToString().Trim()
            Dim libelle As String = "Facture " & numFacture & " - " & societe

            Dim montantHT As Double = If(IsDBNull(factureRow("MontantHT")), 0, Convert.ToDouble(factureRow("MontantHT")))
            Dim montantTVA As Double = If(IsDBNull(factureRow("MontantTVA")), 0, Convert.ToDouble(factureRow("MontantTVA")))
            Dim montantTTC As Double = If(IsDBNull(factureRow("MontantTotal")), 0, Convert.ToDouble(factureRow("MontantTotal")))

            Dim numOR As String = If(IsDBNull(factureRow("numOr")), "VD7802500014", factureRow("numOr").ToString().Trim())
            If String.IsNullOrEmpty(numOR) Then numOR = "VD7802500014"

            Dim dateFacture As Date? = If(IsDBNull(factureRow("DateEmi")), Nothing, Convert.ToDateTime(factureRow("DateEmi")))
            Dim dateEcheance As Date? = If(IsDBNull(factureRow("DateEcheance")), Nothing, Convert.ToDateTime(factureRow("DateEcheance")))

            Dim docNum As String = numFacture
            If docNum.Length > 15 Then docNum = docNum.Substring(0, 15)

            ' Note pour les tests : dans le cas où on a pas de dates, mettre la date d'aujourd'hui 
            ' en prod il faut remettre strictement les dates : 
            ' startDate = If(dateFacture.HasValue AndAlso dateFacture.Value > DateTime.MinValue, dateFacture.Value.ToString("yyyy-MM-ddTHH:mm:ss"), Nothing)
            ' termDate = If(dateEcheance.HasValue AndAlso dateEcheance.Value > DateTime.MinValue, dateEcheance.Value.ToString("yyyy-MM-ddTHH:mm:ss"), Nothing)
            Dim payload = New With {
                .wording = libelle,
                .documentNumber = docNum,
                .billingNumber = numOR,
                .excludingTaxAmount = montantHT,
                .excludingTaxAmountCurrency = montantHT,
                .vatAmount = montantTVA,
                .vatAmountCurrency = montantTVA,
                .includingTaxAmount = montantTTC,
                .includingTaxAmountCurrency = montantTTC,
                .startDate = If(dateFacture.HasValue, dateFacture.Value, DateTime.Now).ToString("yyyy-MM-ddTHH:mm:ss"),
                .termDate = "2026-12-31T00:00:00",
                .status = New With {.id = "2"},
                .customer = New With {.id = codeFournisseur},
                .accountingType = New With {.id = "1"}
            }

            Dim pkFacture As String = APILP.APILocPro.createInvoice(payload)
            Return pkFacture

        Catch ex As Exception
            GestionnaireLog.Error("Erreur création facture Demat LocPro : " & ex.Message)
            Throw
        End Try
    End Function

    ''' <summary>
    ''' Crée les prestations Demat dans LocPro
    ''' </summary>
    Private Shared Function CreerPrestationsLocProDemat(pkFacLocPro As String, idFacture As String, codeFournisseur As String) As Integer
        Dim nbPrestationsCreees As Integer = 0

        Try
            Dim lignes As DataTable = GestionnaireBddFacture.getDematFacturesLignes(idFacture)

            If lignes Is Nothing OrElse lignes.Rows.Count = 0 Then
                Return 0
            End If

            Dim numLigne As Integer = 1

            For Each ligne As DataRow In lignes.Rows
                Dim codePrestaFournisseur As String = ligne("CodePrestaFournisseur").ToString().Trim()

                Dim regleJObject As JObject = GestionnaireBddFacture.GetRegleCorrespondance(codeFournisseur, codePrestaFournisseur)
                If regleJObject Is Nothing Then Continue For

                Dim jPrestations As JArray = CType(regleJObject("prestations"), JArray)

                For Each jPresta As JObject In jPrestations
                    Try
                        Dim codePrestaLP As String = jPresta("code").ToString()
                        Dim jMontantHT As JObject = CType(jPresta("montant_ht"), JObject)

                        Dim montantHTParPresta As Double = CalculateurMontants.CalculerMontantDepuisJSON(jMontantHT, ligne)

                        If montantHTParPresta = 0 Then Continue For

                        Dim tauxTVA As Double = Convert.ToDouble(ligne("TVA"))
                        Dim montantTVAParPresta As Double = montantHTParPresta * (tauxTVA / 100)
                        Dim montantTTCParPresta As Double = montantHTParPresta * (1 + (tauxTVA / 100))

                        Dim descr As String = ligne("Descr").ToString()
                        Dim qte As Double = Convert.ToDouble(ligne("Qte"))

                        Dim prixUnitaireHTParPresta As Double = If(qte > 0, montantHTParPresta / qte, montantHTParPresta)

                        Dim pkPrestation As String = APILP.APILocPro.createPrestationSimplifie(
                            idFact:=pkFacLocPro,
                            descLigne:=descr,
                            codeLigne:=codePrestaLP,
                            unitPrice:=prixUnitaireHTParPresta,
                            qte:=qte,
                            vatRate:=tauxTVA.ToString(),
                            vatAmount:=montantTVAParPresta,
                            excludingTaxAmount:=montantHTParPresta,
                            includingTaxAmount:=montantTTCParPresta
                        )

                        GestionnaireLog.Info("  → Prestation Demat " & codePrestaLP & " créée (ID: " & pkPrestation & ", Montant: " & montantHTParPresta.ToString("N2") & "€)")
                        nbPrestationsCreees += 1
                        numLigne += 1

                    Catch ex As ApiLocProException
                        GestionnaireLog.Error("  → Erreur création prestation Demat : " & ex.Message)
                    End Try
                Next
            Next
        Catch ex As Exception
            GestionnaireLog.Error("Erreur création prestations Demat : " & ex.Message)
            Throw
        End Try

        Return nbPrestationsCreees
    End Function

#End Region

#Region "RE-MATCHAGE AUTOMATIQUE"
    ''' <summary>
    ''' Re-matche les factures après création d'une règle de correspondance
    ''' Met à jour le statut des factures dont TOUTES les lignes ont maintenant une correspondance
    ''' </summary>
    ''' <param name="codeFournisseur">Code du fournisseur</param>
    ''' <param name="codePrestaFournisseur">Code de la prestation fournisseur</param>
    ''' <returns>Nombre de factures mises à jour vers EN_ATTENTE</returns>
    Public Shared Function RematcherFactures(codeFournisseur As String, codePrestaFournisseur As String) As Integer
        If String.IsNullOrEmpty(codeFournisseur) OrElse String.IsNullOrEmpty(codePrestaFournisseur) Then
            Return 0
        End If

        Try
            GestionnaireLog.Info("Début re-matchage pour " & codeFournisseur & " - " & codePrestaFournisseur)

            ' 2. Trouver les factures concernées par les règles de correspondances rajoutées
            Dim facturesConcernees As List(Of FactureInfo) = TrouverFacture(codeFournisseur, codePrestaFournisseur)

            ' 3. Vérifier chaque facture et mettre à jour si 100% matchée
            Dim nbFacturesMisesAJour As Integer = 0

            For Each facture As FactureInfo In facturesConcernees
                If VerifierEtMettreAJourFacture(facture) Then
                    nbFacturesMisesAJour += 1
                End If
            Next

            ' 4. Re-matchage des factures dématérialisées (D_invoice)
            Try
                Dim dtDemat As DataTable = GestionnaireBddFacture.GetFacturesDematNonMatcheesAvecPrestation(codePrestaFournisseur)
                For Each row As DataRow In dtDemat.Rows
                    Dim idFacture As String = row("IdFacture").ToString()
                    Dim siret As String = ""
                    If row.Table.Columns.Contains("Siret_Vend") AndAlso Not IsDBNull(row("Siret_Vend")) AndAlso Not String.IsNullOrWhiteSpace(row("Siret_Vend").ToString()) Then
                        siret = row("Siret_Vend").ToString().Trim()
                    ElseIf row.Table.Columns.Contains("Siren_Vend") AndAlso Not IsDBNull(row("Siren_Vend")) AndAlso Not String.IsNullOrWhiteSpace(row("Siren_Vend").ToString()) Then
                        siret = row("Siren_Vend").ToString().Trim()
                    End If

                    ' On vérifie si ce SIRET correspond bien au codeFournisseur de la règle qu'on vient de créer
                    Dim dtFour As DataTable = GestionnaireBddFacture.retournerFournisseur(siret)
                    If dtFour IsNot Nothing AndAlso dtFour.Rows.Count > 0 Then
                        Dim locproCodeFour As String = dtFour.Rows(0)("F050KY").ToString().Trim()
                        If locproCodeFour = codeFournisseur Then
                            ' La facture démat appartient bien au fournisseur, on la re-traite !
                            Dim errMsg As String = ""
                            If RetraiterFactureSiretDemat(idFacture, siret, errMsg) Then
                                nbFacturesMisesAJour += 1
                            End If
                        End If
                    End If
                Next
            Catch ex As Exception
                GestionnaireLog.Error("Erreur lors du re-matchage Demat : " & ex.Message)
            End Try

            GestionnaireLog.Info("Re-matchage terminé : " & nbFacturesMisesAJour & " facture(s) mise(s) à jour")
            Return nbFacturesMisesAJour

        Catch ex As Exception
            GestionnaireLog.Error("Erreur lors du re-matchage : " & ex.Message)
            Return 0
        End Try
    End Function


    ''' <summary>
    ''' Convertit une DataTable en liste d'objets FactureInfo
    ''' </summary>
    Private Shared Function TrouverFacture(codeFournisseur As String, codePrestaFournisseur As String) As List(Of FactureInfo)

        ' 1. Récupérer la DataTable des factures concernées
        Dim dtFactures As DataTable = GestionnaireBddFacture.GetFacturesNonMatcheesAvecPrestation(codeFournisseur, codePrestaFournisseur)

        If dtFactures Is Nothing OrElse dtFactures.Rows.Count = 0 Then
            GestionnaireLog.Info("Aucune facture à re-matcher")
        End If

        GestionnaireLog.Info(dtFactures.Rows.Count & " facture(s) trouvée(s) avec cette prestation")

        Dim factures As New List(Of FactureInfo)()

        Try
            For Each row As DataRow In dtFactures.Rows
                factures.Add(New FactureInfo With {
                    .NumOR = row("NumOR").ToString().Trim(),
                    .NumFacture = row("NumFacture").ToString().Trim(),
                    .CodeFournisseur = row("CodeFournisseur").ToString().Trim()
                })
            Next
        Catch ex As Exception
            GestionnaireLog.Error("Erreur conversion DataTable en FactureInfo : " & ex.Message)
        End Try

        Return factures
    End Function


    ''' <summary>
    ''' Vérifie si toutes les lignes d'une facture ont une correspondance, et met à jour le statut si oui
    ''' </summary>
    Private Shared Function VerifierEtMettreAJourFacture(facture As FactureInfo) As Boolean
        Try
            ' Récupérer toutes les lignes de la facture via GestionnaireBddFacture
            Dim lignes As DataTable = GestionnaireBddFacture.ObtenirLignesFacture(facture.NumOR, facture.NumFacture)

            If lignes Is Nothing OrElse lignes.Rows.Count = 0 Then
                Return False
            End If

            ' Vérifier chaque ligne
            Dim nbLignesTotal As Integer = lignes.Rows.Count
            Dim nbLignesMatchees As Integer = 0

            For Each ligne As DataRow In lignes.Rows
                Dim codePrestaFournisseur As String = ligne("CodePrestaFournisseur").ToString().Trim()

                ' Vérifier si une règle existe pour cette ligne via GestionnaireBddFacture
                If GestionnaireBddFacture.RegleCorrespondanceExiste(facture.CodeFournisseur, codePrestaFournisseur) Then
                    nbLignesMatchees += 1

                    ' Récupérer les codes LocPro de la règle
                    Dim codesLocPro As List(Of String) = GestionnaireBddFacture.GetCodesLocPro(facture.CodeFournisseur, codePrestaFournisseur)
                    Dim codesStr As String = String.Join(", ", codesLocPro)

                    ' Mettre à jour la ligne avec les codes LocPro
                    GestionnaireBddFacture.MettreAJourCodesLocProLigne(facture.NumOR, facture.NumFacture, codePrestaFournisseur, codesStr)

                End If
            Next

            GestionnaireLog.Info("Facture " & facture.NumFacture & " : " & nbLignesMatchees & "/" & nbLignesTotal & " lignes matchées")

            ' Si 100% des lignes sont matchées, mettre à jour le statut via GestionnaireBddFacture
            If nbLignesMatchees = nbLignesTotal Then
                Return GestionnaireBddFacture.MettreAJourStatutFactureEnAttente(facture.NumOR, facture.NumFacture)
            End If

            Return False

        Catch ex As Exception
            GestionnaireLog.Error("Erreur vérification facture " & facture.NumFacture & " : " & ex.Message)
            Return False
        End Try
    End Function
#End Region

#Region "RE-TRAITEMENT FOURNISSEUR / IMMAT"

    ''' <summary>
    ''' Re-traite une facture après correction du SIRET
    ''' </summary>
    ''' <param name="numOR">Numéro OR</param>
    ''' <param name="numFacture">Numéro facture</param>
    ''' <returns>True si succès</returns>
    Public Shared Function RetraiterFactureSiret(numOR As String, numFacture As String, ByRef errorMessage As String) As Boolean
        Try
            GestionnaireLog.Info("RE-TRAITEMENT SIRET : " & numFacture)

            ' Récupérer la facture depuis la BDD
            Dim dtFacture As DataTable = GestionnaireBddFacture.GetFacture(numOR, numFacture)


            Dim rowFacture As DataRow = dtFacture.Rows(0)
            Dim siret As String = rowFacture("Siret").ToString().Trim()

            GestionnaireLog.Info("SIRET à vérifier : " & siret)

            Dim infosFour As InfosFournisseur = ServiceOR.retournerInfosFournisseur(siret)

            If infosFour Is Nothing Then
                ' Fournisseur toujours introuvable
                GestionnaireLog.Warn("Fournisseur introuvable avec SIRET : " & siret)
                GestionnaireBddFacture.MettreAJourStatutFacture(numOR, numFacture, "SUPPLIER_NOT_FOUND",
                    "Fournisseur avec SIRET " & siret & " introuvable dans LocPro")
                Return False
            End If

            GestionnaireLog.Info("Fournisseur trouve : " & infosFour.CodeFournisseur)

            ' ÉTAPE 2 : Mettre à jour les infos fournisseur en BDD
            GestionnaireBddFacture.MettreAJourInfosFournisseur(numOR, numFacture, infosFour.CodeFournisseur)

            ' ÉTAPE 3 : Vérifier et mettre à jour les correspondances de prestations
            Dim lignes As DataTable = GestionnaireBddFacture.ObtenirLignesFacture(numOR, numFacture)

            If lignes Is Nothing OrElse lignes.Rows.Count = 0 Then
                GestionnaireLog.Warn("Aucune ligne de facture")
                GestionnaireBddFacture.MettreAJourStatutFacture(numOR, numFacture, "ERROR", "Aucune ligne de prestation")
                Return False
            End If

            GestionnaireLog.Info("Vérification de " & lignes.Rows.Count & " ligne(s) de prestation")

            Dim toutesLignesMatchees As Boolean = True

            For Each ligne As DataRow In lignes.Rows
                Dim codePrestaFournisseur As String = ligne("CodePrestaFournisseur").ToString().Trim()

                ' Vérifier si une règle existe pour ce fournisseur et cette prestation
                If GestionnaireBddFacture.RegleCorrespondanceExiste(infosFour.CodeFournisseur, codePrestaFournisseur) Then
                    Dim regleJObject As JObject = GestionnaireBddFacture.GetRegleCorrespondance(infosFour.CodeFournisseur, codePrestaFournisseur)

                    ' Extraire les codes LocPro depuis le JSON
                    Dim jPrestations As JArray = CType(regleJObject("prestations"), JArray)
                    Dim codesLocPro As New List(Of String)()

                    For Each jPresta As JObject In jPrestations
                        Dim code As String = jPresta("code").ToString()
                        If Not String.IsNullOrEmpty(code) Then
                            codesLocPro.Add(code)
                        End If
                    Next

                    ' Convertir la liste en string séparée par des virgules
                    Dim codesLocProString As String = String.Join(",", codesLocPro)

                    ' Mettre à jour la ligne avec les codes LocPro
                    GestionnaireBddFacture.MettreAJourCodesLocProLigne(numOR, numFacture, codePrestaFournisseur, codesLocProString)

                Else
                    ' Aucune règle trouvée
                    toutesLignesMatchees = False
                    GestionnaireLog.Warn("  ✗ " & codePrestaFournisseur & " → Aucune règle de correspondance")
                End If
            Next

            ' ÉTAPE 4 : Mettre à jour le statut final
            If toutesLignesMatchees Then
                ' Toutes les prestations sont matchées → EN_ATTENTE
                GestionnaireBddFacture.MettreAJourStatutFacture(numOR, numFacture, "EN_ATTENTE",
                    "Facture prête pour l'intégration dans LocPro")
                GestionnaireLog.Info("Statut final : EN_ATTENTE")
            Else
                ' Au moins une prestation sans correspondance → PRESTATION_INEXISTANTE
                GestionnaireBddFacture.MettreAJourStatutFacture(numOR, numFacture, "PRESTATION_INEXISTANTE",
                    "Certaines prestations n'ont pas de correspondance LocPro")
                GestionnaireLog.Info("Statut final : PRESTATION_INEXISTANTE")
            End If

            GestionnaireLog.Info("Re-traitement SIRET termine avec succes")
            Return True

        Catch ex As Exception
            GestionnaireLog.Error("Erreur re-traitement SIRET : " & ex.ToString())
            GestionnaireBddFacture.MettreAJourStatutFacture(numOR, numFacture, "ERROR",
                "Erreur lors du re-traitement : " & ex.Message)
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Re-traite une facture dématérialisée après correction du SIRET
    ''' </summary>
    Public Shared Function RetraiterFactureSiretDemat(idFacture As String, siret As String, ByRef errorMessage As String) As Boolean
        Try
            GestionnaireLog.Info("RE-TRAITEMENT SIRET DEMAT : " & idFacture & " - " & siret)

            Dim codeFournisseur As String = ""
            Dim raisonSociale As String = ""
            
            Dim dtFourn As DataTable = GestionnaireBddFacture.RechercherFournisseurParSiretOuSiren(siret, "")
            If dtFourn IsNot Nothing AndAlso dtFourn.Rows.Count > 0 Then
                codeFournisseur = dtFourn.Rows(0)("F050KY").ToString().Trim()
                If dtFourn.Columns.Contains("F050NOM") Then
                    raisonSociale = dtFourn.Rows(0)("F050NOM").ToString().Trim()
                End If
            End If

            If String.IsNullOrEmpty(codeFournisseur) Then
                GestionnaireLog.Warn("Fournisseur introuvable avec SIREN/SIRET : " & siret)
                GestionnaireBddFacture.MettreAJourStatutFactureDemat(idFacture, "FOURNISSEUR_INTROUVABLE", "Le SIREN/SIRET saisi est introuvable dans la base LocPro.")
                errorMessage = "Le SIREN/SIRET saisi est introuvable dans la base LocPro."
                Return False
            End If

            GestionnaireLog.Info("Fournisseur trouve : " & codeFournisseur)

            ' Mise à jour de la raison sociale pour l'affichage IHM
            If Not String.IsNullOrEmpty(raisonSociale) Then
                GestionnaireBddFacture.MettreAJourRaisonSocialeDemat(idFacture, raisonSociale)
            End If

            Dim lignes As DataTable = GestionnaireBddFacture.getDematFacturesLignes(idFacture)

            If lignes Is Nothing OrElse lignes.Rows.Count = 0 Then
                GestionnaireLog.Warn("Aucune ligne de facture pour : " & idFacture)
                GestionnaireBddFacture.MettreAJourStatutFactureDemat(idFacture, "ERROR", "La facture ne contient aucune ligne de prestation.")
                errorMessage = "La facture ne contient aucune ligne de prestation."
                Return False
            End If

            Dim toutesLignesMatchees As Boolean = True

            For Each ligne As DataRow In lignes.Rows
                Dim codePrestaFournisseur As String = ligne("CodePrestaFournisseur").ToString().Trim()

                If GestionnaireBddFacture.RegleCorrespondanceExiste(codeFournisseur, codePrestaFournisseur) Then
                    Dim regleJObject As JObject = GestionnaireBddFacture.GetRegleCorrespondance(codeFournisseur, codePrestaFournisseur)
                    Dim jPrestations As JArray = CType(regleJObject("prestations"), JArray)
                    Dim codesLocPro As New List(Of String)()

                    For Each jPresta As JObject In jPrestations
                        Dim code As String = jPresta("code").ToString()
                        If Not String.IsNullOrEmpty(code) Then codesLocPro.Add(code)
                    Next

                    Dim codesLocProString As String = String.Join(",", codesLocPro)

                    Dim numOR As String = ligne("NumOR").ToString().Trim()
                    Dim numFacture As String = ligne("NumFacture").ToString().Trim()
                    GestionnaireBddFacture.MettreAJourCodesLocProLigne(numOR, numFacture, codePrestaFournisseur, codesLocProString)
                Else
                    toutesLignesMatchees = False
                End If
            Next

            If toutesLignesMatchees Then
                GestionnaireBddFacture.MettreAJourStatutFactureDemat(idFacture, "A_INTEGRER", "La facture est prête à être intégrée dans Locpro, veuillez cliquer sur comptabiliser pour le faire.")
                Return True
            Else
                GestionnaireLog.Warn("SIRET validé, mais certaines prestations n'ont pas de correspondance LocPro.")
                GestionnaireBddFacture.MettreAJourStatutFactureDemat(idFacture, "PRESTATION_INEXISTANTE", "Certaines prestations n'ont pas de correspondance LocPro")
                Return True
            End If

        Catch exFournisseur As Exceptions.FournisseurIntrouvableException
            GestionnaireLog.Warn("Fournisseur introuvable avec SIRET : " & siret)
            GestionnaireBddFacture.MettreAJourStatutFactureDemat(idFacture, "FOURNISSEUR_INTROUVABLE", "Le SIREN/SIRET saisi est introuvable dans la base LocPro.")
            errorMessage = "Le SIREN/SIRET saisi est introuvable dans la base LocPro."
            Return False
        Catch ex As Exception
            GestionnaireLog.Error("Erreur re-traitement SIRET Demat : " & ex.ToString())
            GestionnaireBddFacture.MettreAJourStatutFactureDemat(idFacture, "ERROR", "Erreur système : " & ex.Message)
            errorMessage = "Erreur système : " & ex.Message
            Return False
        End Try
    End Function

#End Region


#Region "CLASSES INTERNES"
    ''' <summary>Résultat d'une ré-intégration unitaire</summary>
    Public Class ResultatReintegration
        Public Property Succes As Boolean
        Public Property Message As String
        Public Property NumFacture As String
    End Class

    ''' <summary>Résultat d'une ré-intégration par lot</summary>
    Public Class ResultatLot
        Public Property NbSucces As Integer
        Public Property NbEchecs As Integer
        Public Property DetailsEchecs As New List(Of String)()
    End Class

    ''' <summary>
    ''' Classe interne pour stocker les informations d'une facture
    ''' </summary>
    Private Class FactureInfo
        Public Property NumOR As String
        Public Property NumFacture As String
        Public Property CodeFournisseur As String
    End Class

#End Region

End Class
