Imports System
Imports System.Collections.Generic
Imports System.Data
Imports APILP
Imports Newtonsoft.Json.Linq
Imports Services

Public Class ServiceIntegrationDemat

    ''' <summary>
    ''' Ré-intègre toutes les factures dématérialisées (D_invoice) A_INTEGRER dans LocPro
    ''' </summary>
    Public Shared Function ReintegrerFacturesDematParLot() As ServiceReintegration.ResultatLot
        Dim resultat As New ServiceReintegration.ResultatLot()

        Try
            GestionnaireLog.Info("DÉBUT TRAITEMENT PAR LOT (DEMAT)")

            ' 1. Récupérer toutes les factures Demat à vérifier/intégrer
            Dim dtFactures As DataTable = GestionnaireBddFacture.ObtenirFacturesDematAVerifier()
            If dtFactures Is Nothing OrElse dtFactures.Rows.Count = 0 Then
                GestionnaireLog.Info("Aucune facture dématérialisée à traiter")
                Return resultat
            End If

            GestionnaireLog.Info(dtFactures.Rows.Count & " facture(s) dématérialisée(s) à traiter")

            ' 2. Traiter chaque facture
            For Each row As DataRow In dtFactures.Rows
                Dim idFacture As String = row("IdFacture").ToString()
                Dim numFacture As String = row("NumeroFacture").ToString()
                Dim numOR As String = row("numOr").ToString()
                Dim siret As String = ""
                If row.Table.Columns.Contains("Siret_Vend") AndAlso Not IsDBNull(row("Siret_Vend")) AndAlso Not String.IsNullOrWhiteSpace(row("Siret_Vend").ToString()) Then
                    siret = row("Siret_Vend").ToString().Trim()
                ElseIf row.Table.Columns.Contains("Siren_Vend") AndAlso Not IsDBNull(row("Siren_Vend")) AndAlso Not String.IsNullOrWhiteSpace(row("Siren_Vend").ToString()) Then
                    siret = row("Siren_Vend").ToString().Trim()
                End If

                Try
                    ' A. Trouver le code fournisseur LocPro
                    Dim codeFournisseur As String = GestionnaireBddFacture.GetCodeFournisseur(numOR, numFacture)
                    If String.IsNullOrEmpty(codeFournisseur) Then
                        Dim errMsg = "Aucun fournisseur trouvé pour le siret : " & siret
                        GestionnaireBddFacture.MettreAJourStatutFactureDemat(idFacture, "FOURNISSEUR_INTROUVABLE", errMsg)
                        resultat.NbEchecs += 1
                        resultat.DetailsEchecs.Add(numFacture & " : " & errMsg)
                        Continue For
                    End If

                    ' B. Préparer le payload de la facture
                    Dim docNum As String = idFacture
                    If docNum.Length > 15 Then docNum = docNum.Substring(0, 15) ' Tronqué à 15 caractères pour Locpro

                    Dim dateEmi As DateTime = If(IsDBNull(row("DateEmi")), DateTime.Now, Convert.ToDateTime(row("DateEmi")))
                    Dim dateEcheance As DateTime = If(IsDBNull(row("DateEcheance")), DateTime.Now, Convert.ToDateTime(row("DateEcheance")))

                    Dim ht As Double = If(IsDBNull(row("MontantHT")), 0.0, Convert.ToDouble(row("MontantHT")))
                    Dim tva As Double = If(IsDBNull(row("MontantTVA")), 0.0, Convert.ToDouble(row("MontantTVA")))
                    Dim ttc As Double = If(IsDBNull(row("MontantTotal")), 0.0, Convert.ToDouble(row("MontantTotal")))

                    '.termDate = dateEcheance.ToString("yyyy-MM-ddTHH:mm:ss"),
                    Dim payload = New With {
                        .wording = "Facture " & numFacture & " - " & row("SocieteEmet").ToString(),
                        .documentNumber = docNum,
                        .billingNumber = numOR,
                        .excludingTaxAmount = ht,
                        .excludingTaxAmountCurrency = ht,
                        .vatAmount = tva,
                        .vatAmountCurrency = tva,
                        .includingTaxAmount = ttc,
                        .includingTaxAmountCurrency = ttc,
                        .startDate = dateEmi.ToString("yyyy-MM-ddTHH:mm:ss"),
                        .termDate = "2026-12-31T00:00:00",
                        .status = New With {.id = "2"},
                        .customer = New With {.id = codeFournisseur},
                        .accountingType = New With {.id = "1"}
                    }

                    ' C. Créer la facture dans LocPro
                    Dim createdInvoiceId As String = APILocPro.createInvoice(payload)
                    GestionnaireLog.Info("Facture Demat intégrée dans Locpro avec l'ID: " & createdInvoiceId)

                    ' D. Récupérer les lignes de la facture
                    Dim dtLignes As DataTable = GestionnaireBddFacture.ObtenirLignesDemat(idFacture)
                    Dim aAuMoinsUneErreurPrestation As Boolean = False
                    Dim messageErreurPrestations As String = ""

                    For Each ligneRow As DataRow In dtLignes.Rows
                        Dim descLigne As String = ligneRow("designationArticle").ToString()
                        Dim codePrestaFournisseur As String = ligneRow("refArticleFournisseur").ToString()
                        Dim qte As Double = If(IsDBNull(ligneRow("quantiteArticle")), 1.0, Convert.ToDouble(ligneRow("quantiteArticle")))
                        Dim unitPrice As Double = If(IsDBNull(ligneRow("prixNetHtArticle")), 0.0, Convert.ToDouble(ligneRow("prixNetHtArticle")))

                        ' E. Trouver les correspondances LocPro (La règle)
                        Dim codesLocPro As List(Of String) = GestionnaireBddFacture.GetCodesLocPro(codeFournisseur, codePrestaFournisseur)

                        If codesLocPro Is Nothing OrElse codesLocPro.Count = 0 Then
                            aAuMoinsUneErreurPrestation = True
                            messageErreurPrestations &= codePrestaFournisseur & " - " & descLigne & ", "
                            Continue For
                        End If

                        ' Créer chaque prestation
                        For Each codeLigne As String In codesLocPro
                            APILocPro.createPrestationSimplifie(createdInvoiceId, descLigne, codeLigne, unitPrice, qte)
                        Next
                    Next

                    ' F. Si erreur de prestation, on annule (ou on change le statut)
                    If aAuMoinsUneErreurPrestation Then
                        Dim errMsg = "Aucune règle de correspondance pour : " & messageErreurPrestations.TrimEnd(","c, " "c)
                        GestionnaireBddFacture.MettreAJourStatutFactureDemat(idFacture, "PRESTATION_INEXISTANTE", errMsg)
                        resultat.NbEchecs += 1
                        resultat.DetailsEchecs.Add(numFacture & " : " & errMsg)
                    Else
                        ' Succès !
                        GestionnaireBddFacture.MettreAJourStatutFactureDemat(idFacture, "SUCCES", "Intégration réussie dans Locpro")
                        resultat.NbSucces += 1

                        ' Mise à jour du cycle de vie Maileva (PAID)
                        Try
                            Dim mailevaService As New MailevaApiService()
                            mailevaService.MettreAJourStatutCycleDeVieAsync(idFacture, "PAID").Wait()
                        Catch exMaileva As Exception
                            GestionnaireLog.Error("Impossible de mettre à jour le statut Maileva (PAID) pour la facture " & idFacture & " : " & exMaileva.Message)
                        End Try
                    End If

                Catch ex As Exception
                    GestionnaireLog.Error("Erreur réintégration Demat facture " & numFacture & " : " & ex.Message)
                    GestionnaireBddFacture.MettreAJourStatutFactureDemat(idFacture, "ERREUR", ex.Message)

                    resultat.NbEchecs += 1
                    resultat.DetailsEchecs.Add(numFacture & " : " & ex.Message)
                End Try
            Next

        Catch ex As Exception
            GestionnaireLog.Error("Erreur globale lors de la réintégration par lot Demat : " & ex.Message)
        End Try

        Return resultat
    End Function

End Class
