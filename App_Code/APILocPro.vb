Imports System.Net.Http
Imports Exceptions
Imports Newtonsoft.Json

Namespace APILP

    Public Class APILocPro

        Private Shared ReadOnly xVegaSerial As String = ConfigurationManager.AppSettings("xVegaSerial")
        Private Shared ReadOnly acceptLanguage As String = ConfigurationManager.AppSettings("acceptLanguage")
        Private Shared ReadOnly xVegaVersion As String = ConfigurationManager.AppSettings("xVegaVersion")
        Private Shared ReadOnly xVegaApp As String = ConfigurationManager.AppSettings("xVegaApp")
        Private Shared ReadOnly xVegaContext As String = ConfigurationManager.AppSettings("xVegaContext")
        Private Shared ReadOnly xVegaContextTest As String = ConfigurationManager.AppSettings("xVegaContextTest")

        Public Shared Function getToken() As String
            Dim username As String = ConfigurationManager.AppSettings("username")
            Dim password As String = ConfigurationManager.AppSettings("password")

            Dim apiUrl As String = "http://192.168.1.19:8001/users/login"
            Dim httpClient As New HttpClient()

            ' headers 
            httpClient.DefaultRequestHeaders.Add("x-vega-token", xVegaToken)
            httpClient.DefaultRequestHeaders.Add("x-vega-version", xVegaVersion)
            httpClient.DefaultRequestHeaders.Add("x-vega-app", xVegaApp)
            httpClient.DefaultRequestHeaders.Add("x-vega-serial", xVegaSerial)
            httpClient.DefaultRequestHeaders.Add("accept-language", acceptLanguage)
            httpClient.DefaultRequestHeaders.Add("x-vega-context", xVegaContext)

            ' Request body
            Dim login = New With {
                .username = username,
                .password = password
            }

            Dim json As String = JsonConvert.SerializeObject(login)
            Dim content As New StringContent(json, Encoding.UTF8, "application/json")

            Dim httpResponse As HttpResponseMessage = httpClient.PostAsync(apiUrl, content).Result

            If httpResponse.IsSuccessStatusCode Then
                Dim result As String = httpResponse.Content.ReadAsStringAsync().Result
                Dim loginResult As LoginResponse = JsonConvert.DeserializeObject(Of LoginResponse)(result)

                Return loginResult.token
            Else
                GestionnaireLog.Error("Erreur sur le service users login" & httpResponse.Content.ReadAsStringAsync().Result)
                Return Nothing
            End If
        End Function

        Public Shared Function getTokenTest() As String
            Dim username As String = ConfigurationManager.AppSettings("username")
            Dim password As String = ConfigurationManager.AppSettings("password")

            Dim apiUrl As String = "http://192.168.1.19:8001/users/login"
            Dim httpClient As New HttpClient()

            ' headers 
            httpClient.DefaultRequestHeaders.Add("x-vega-token", xVegaToken)
            httpClient.DefaultRequestHeaders.Add("x-vega-version", xVegaVersion)
            httpClient.DefaultRequestHeaders.Add("x-vega-app", xVegaApp)
            httpClient.DefaultRequestHeaders.Add("x-vega-serial", xVegaSerial)
            httpClient.DefaultRequestHeaders.Add("accept-language", acceptLanguage)
            httpClient.DefaultRequestHeaders.Add("x-vega-context", xVegaContextTest)

            ' Request body
            Dim login = New With {
                .username = username,
                .password = password
            }

            Dim json As String = JsonConvert.SerializeObject(login)
            Dim content As New StringContent(json, Encoding.UTF8, "application/json")

            Dim httpResponse As HttpResponseMessage = httpClient.PostAsync(apiUrl, content).Result

            If httpResponse.IsSuccessStatusCode Then
                Dim result As String = httpResponse.Content.ReadAsStringAsync().Result
                Dim loginResult As LoginResponse = JsonConvert.DeserializeObject(Of LoginResponse)(result)

                Return loginResult.token
            Else
                GestionnaireLog.Error("Erreur sur le service users login" & httpResponse.Content.ReadAsStringAsync().Result)
                Return Nothing
            End If
        End Function

        Public Shared Function createFacture(libelle As String, status As String, montantRes As Double, montantHTDevise As Double, montantHT As Double, montantTVA As Double,
                                     montantTVADevise As Double, montantTTC As Double, montantTTCDevise As Double, typeFac As String,
                                     accountingType As String, moyenPaiem As String, agence As String, codeClient As String,
                                     Optional codeModele As String = Nothing, Optional codeParc As String = Nothing,
                                     Optional facNm As String = Nothing, Optional nmDoc As String = Nothing, Optional startDate As DateTime = Nothing, Optional termDate As DateTime = Nothing, Optional mileage As Integer = Nothing) As String

            Dim apiUrl As String = "http://192.168.1.19:8001/invoices"
            Dim httpClient As New HttpClient()
            Dim token = getToken()

            ' headers 
            httpClient.DefaultRequestHeaders.Add("x-vega-serial", xVegaSerial)
            httpClient.DefaultRequestHeaders.Add("accept-language", acceptLanguage)
            httpClient.DefaultRequestHeaders.Add("x-vega-version", xVegaVersion)
            httpClient.DefaultRequestHeaders.Add("x-vega-app", xVegaApp)
            httpClient.DefaultRequestHeaders.Add("x-vega-context", xVegaContext)
            httpClient.DefaultRequestHeaders.Add("x-vega-token", token)

            Dim facture = New With {
                .wording = libelle,
                .documentNumber = nmDoc,
                .billingNumber = facNm,
                .remainingAmount = montantRes,
                .excludingTaxAmount = montantHT,
                .excludingTaxAmountCurrency = montantHTDevise,
                .vatAmount = montantTVA,
                .vatAmountCurrency = montantTVADevise,
                .includingTaxAmount = montantTTC,
                .includingTaxAmountCurrency = montantTTCDevise,
                .startDate = startDate,
                .termDate = termDate,
                .mileage = mileage,
                .quoteNumber = "",
                .status = New With {.id = status},
                .termPaymentType = New With {.id = "0000"},
                .auxiliaryAccountType = New With {.id = "1"},
                .documentType = New With {.id = typeFac},
                .location = New With {.id = agence},
                .equipment = If(String.IsNullOrEmpty(codeParc), Nothing, New With {.id = codeParc}),
                .customer = New With {.id = codeClient},
                .paymentMethod = New With {.id = moyenPaiem},
                .accountingType = New With {.id = accountingType},
                .model = If(String.IsNullOrEmpty(codeModele), Nothing, New With {.id = codeModele})
            }

            ' Sérialisation JSON
            Dim settings As New JsonSerializerSettings With {
                .NullValueHandling = NullValueHandling.Ignore
            }

            Dim json As String = JsonConvert.SerializeObject(facture, settings)
            Dim content As New StringContent(json, Encoding.UTF8, "application/json")

            ' Envoi de la requête POST
            Dim response As HttpResponseMessage = httpClient.PostAsync(apiUrl, content).Result
            If response.IsSuccessStatusCode Then
                Dim result As String = response.Content.ReadAsStringAsync().Result
                Dim factureId As String = response.Headers.GetValues("x-vega-id").FirstOrDefault()
                GestionnaireLog.Info("Facture créé : " & factureId)
                Return factureId
            Else
                Dim errorMsg As String = "Erreur " & response.StatusCode.ToString() & " : " & response.Content.ReadAsStringAsync().Result
                GestionnaireLog.Error("Erreur sur le service création facture" & errorMsg)
                Throw New ApiLocProException(errorMsg)
                Return errorMsg
            End If
        End Function

        Public Shared Function createFactureTest(libelle As String, montantRes As Double, montantHTDevise As Double, montantHT As Double, montantTVA As Double,
                                     montantTVADevise As Double, montantTTC As Double, montantTTCDevise As Double, typeFac As String,
                                     accountingType As String, moyenPaiem As String, agence As String, codeClient As String,
                                     Optional codeModele As String = Nothing, Optional codeParc As String = Nothing,
                                     Optional facNm As String = Nothing, Optional nmDoc As String = Nothing, Optional startDate As DateTime = Nothing, Optional termDate As DateTime = Nothing, Optional mileage As Integer = Nothing, Optional billingDate As Date? = Nothing) As String

            Dim apiUrl As String = "http://192.168.1.19:8001/invoices"
            Dim httpClient As New HttpClient()
            Dim token = getTokenTest()

            ' headers 
            httpClient.DefaultRequestHeaders.Add("x-vega-serial", xVegaSerial)
            httpClient.DefaultRequestHeaders.Add("accept-language", acceptLanguage)
            httpClient.DefaultRequestHeaders.Add("x-vega-version", xVegaVersion)
            httpClient.DefaultRequestHeaders.Add("x-vega-app", xVegaApp)
            httpClient.DefaultRequestHeaders.Add("x-vega-context", xVegaContextTest)
            httpClient.DefaultRequestHeaders.Add("x-vega-token", token)

            Dim facture = New With {
        .wording = libelle,
        .documentNumber = nmDoc,
        .billingNumber = facNm,
        .billingDate = If(billingDate.HasValue, billingDate.Value.ToString("yyyy-MM-dd"), Nothing),
        .remainingAmount = montantRes,
        .excludingTaxAmount = montantHT,
        .excludingTaxAmountCurrency = montantHTDevise,
        .vatAmount = montantTVA,
        .vatAmountCurrency = montantTVADevise,
        .includingTaxAmount = montantTTC,
        .includingTaxAmountCurrency = montantTTCDevise,
        .startDate = startDate,
        .termDate = termDate,
        .mileage = mileage,
        .quoteNumber = "",
        .status = New With {.id = "2"},
        .termPaymentType = New With {.id = "0000"},
        .auxiliaryAccountType = New With {.id = "1"},
        .documentType = New With {.id = typeFac},
        .location = New With {.id = agence},
        .equipment = If(String.IsNullOrEmpty(codeParc), Nothing, New With {.id = codeParc}),
        .customer = New With {.id = codeClient},
        .paymentMethod = New With {.id = moyenPaiem},
        .accountingType = New With {.id = accountingType},
        .model = If(String.IsNullOrEmpty(codeModele), Nothing, New With {.id = codeModele})
    }

            ' Sérialisation JSON
            Dim settings As New JsonSerializerSettings With {
        .NullValueHandling = NullValueHandling.Ignore
    }

            Dim json As String = JsonConvert.SerializeObject(facture, settings)
            Dim content As New StringContent(json, Encoding.UTF8, "application/json")

            ' Envoi de la requête POST
            Dim response As HttpResponseMessage = httpClient.PostAsync(apiUrl, content).Result
            If response.IsSuccessStatusCode Then
                Dim result As String = response.Content.ReadAsStringAsync().Result
                Dim factureId As String = response.Headers.GetValues("x-vega-id").FirstOrDefault()
                GestionnaireLog.Info("Facture créé : " & factureId)
                Return factureId
            Else
                Dim errorMsg As String = "Erreur " & response.StatusCode.ToString() & " : " & response.Content.ReadAsStringAsync().Result
                GestionnaireLog.Error("Erreur sur le service création facture" & errorMsg)
                Throw New ApiLocProException(errorMsg)
                Return errorMsg
            End If
        End Function

        Public Shared Function getFactById(idFact As String) As Object
            Dim apiUrl As String = "http://192.168.1.19:8001/invoices/" & idFact
            Dim httpClient As New HttpClient()
            Dim token = getToken()

            ' headers 
             httpClient.DefaultRequestHeaders.Add("x-vega-token", xVegaToken)
            httpClient.DefaultRequestHeaders.Add("x-vega-version", xVegaVersion)
            httpClient.DefaultRequestHeaders.Add("x-vega-app", xVegaApp)
            httpClient.DefaultRequestHeaders.Add("x-vega-serial", xVegaSerial)
            httpClient.DefaultRequestHeaders.Add("accept-language", acceptLanguage)
            httpClient.DefaultRequestHeaders.Add("x-vega-context", xVegaContext)

            Dim response As HttpResponseMessage = httpClient.GetAsync(apiUrl).Result

            If response.IsSuccessStatusCode Then
                Dim json As String = response.Content.ReadAsStringAsync().Result
                Dim facture As Facture = JsonConvert.DeserializeObject(Of Facture)(json)
                Return facture
            Else
                Return "Erreur " & response.StatusCode.ToString() & " : " & response.Content.ReadAsStringAsync().Result
            End If
        End Function


        Public Shared Function getFactures() As Object
            Dim apiUrl As String = "http://192.168.1.19:8001/invoices"
            Dim httpClient As New HttpClient()
            Dim token = getToken()

            ' headers 
            httpClient.DefaultRequestHeaders.Add("x-vega-token", xVegaToken)
            httpClient.DefaultRequestHeaders.Add("x-vega-version", xVegaVersion)
            httpClient.DefaultRequestHeaders.Add("x-vega-app", xVegaApp)
            httpClient.DefaultRequestHeaders.Add("x-vega-serial", xVegaSerial)
            httpClient.DefaultRequestHeaders.Add("accept-language", acceptLanguage)
            httpClient.DefaultRequestHeaders.Add("x-vega-context", xVegaContext)


            Dim response As HttpResponseMessage = httpClient.GetAsync(apiUrl).Result

            If response.IsSuccessStatusCode Then
                Dim json As String = response.Content.ReadAsStringAsync().Result
                Dim factures As List(Of Facture) = JsonConvert.DeserializeObject(Of List(Of Facture))(json)
                Return factures
            Else
                Return "Erreur " & response.StatusCode.ToString() & " : " & response.Content.ReadAsStringAsync().Result
            End If

        End Function


        Public Shared Function createPrestation(idFact As String, descLigne As String, codeLigne As String, codeTva As String, isWithoutTax As Boolean, modeCalc As String,
                                        qte As Double, agence As String, lineNumber As Integer, km As Double, discountAmount As Double, includingTaxAmount As Double,
                                        unitPrice As Double, vatAmount As Double, excludingTaxAmount As Double, vatRate As String, Optional codeParc As String = Nothing) As String
            Dim apiUrl As String = "http://192.168.1.19:8001/invoices/" & idFact & "/lines"
            Dim httpClient As New HttpClient()
            Dim token = getToken()

            ' headers 
            httpClient.DefaultRequestHeaders.Add("x-vega-serial", xVegaSerial)
            httpClient.DefaultRequestHeaders.Add("accept-language", acceptLanguage)
            httpClient.DefaultRequestHeaders.Add("x-vega-version", xVegaVersion)
            httpClient.DefaultRequestHeaders.Add("x-vega-app", xVegaApp)
            httpClient.DefaultRequestHeaders.Add("x-vega-context", xVegaContext)
            httpClient.DefaultRequestHeaders.Add("x-vega-token", token)

            Dim prestation = New With {
                .wording = descLigne,
                .service = New With {
                    .id = codeLigne
                },
                .equipment = New With {
                    .id = codeParc
                },
                .vatCode = New With {
                    .id = codeTva
                },
                .calculationMode = New With {
                    .id = modeCalc
                },
                .quantity = qte,
                .isWithoutTax = isWithoutTax,
                .unitPrice = unitPrice,
                .unitPriceCurrency = unitPrice,
                .percentageAmount = 0,
                .discountAmount = discountAmount,
                .discountAmountCurrency = discountAmount,
                .excludingTaxAmount = excludingTaxAmount,
                .excludingTaxAmountCurrency = excludingTaxAmount,
                .vatAmount = vatAmount,
                .vatAmountCurrency = vatAmount,
                .includingTaxAmount = includingTaxAmount,
                .includingTaxAmountCurrency = includingTaxAmount,
                .vatRate = vatRate,
                .mileage = km,
                .lineNumber = lineNumber,
                .location = New With {
                    .id = agence
                },
                .event = New With {
                    .id = idFact
                },
                .quantityAssignment = New With {
                    .id = "0"
                },
                .amountAssignment = New With {
                    .id = "0"
                },
                .amountCanBeUpdated = True,
                .quantityCanBeUpdated = True
            }

            ' Sérialisation JSON
            Dim settings As New JsonSerializerSettings With {
                .NullValueHandling = NullValueHandling.Ignore
            }

            Dim json As String = JsonConvert.SerializeObject(prestation, settings)
            Dim content As New StringContent(json, Encoding.UTF8, "application/json")

            ' Envoi de la requête POST
            Dim response As HttpResponseMessage = httpClient.PostAsync(apiUrl, content).Result
            If response.IsSuccessStatusCode Then
                Dim result As String = response.Content.ReadAsStringAsync().Result
                Dim PrestaId As String = response.Headers.GetValues("x-vega-id").FirstOrDefault()
                GestionnaireLog.Info("Prestaion (lines) pour la facture " & idFact & " créé : " & PrestaId)
                Return PrestaId
            Else
                Dim errorMsg As String = "Erreur " & response.StatusCode.ToString() & " : " & response.Content.ReadAsStringAsync().Result
                GestionnaireLog.Error("Erreur sur le service création prestation" & errorMsg)
                Throw New ApiLocProException(errorMsg)
                Return errorMsg
            End If
        End Function

        Public Shared Function createPrestationTest(idFact As String, descLigne As String, codeLigne As String, codeTva As String, isWithoutTax As Boolean, modeCalc As String,
                                                qte As Double, agence As String, lineNumber As Integer, km As Double, discountAmount As Double, includingTaxAmount As Double,
                                                unitPrice As Double, vatAmount As Double, excludingTaxAmount As Double, vatRate As String, Optional codeParc As String = Nothing) As String
            Dim apiUrl As String = "http://192.168.1.19:8001/invoices/" & idFact & "/lines"
            Dim httpClient As New HttpClient()
            Dim token = getToken()

            ' headers 
            httpClient.DefaultRequestHeaders.Add("x-vega-serial", "serial")
            httpClient.DefaultRequestHeaders.Add("accept-language", "001")
            httpClient.DefaultRequestHeaders.Add("x-vega-version", "1")
            httpClient.DefaultRequestHeaders.Add("x-vega-app", "10999")
            httpClient.DefaultRequestHeaders.Add("x-vega-context", xVegaContextTest)
            httpClient.DefaultRequestHeaders.Add("x-vega-token", token)

            Dim prestation = New With {
                .wording = descLigne,
                .service = New With {
                    .id = codeLigne
                },
                .equipment = New With {
                    .id = codeParc
                },
                .vatCode = New With {
                    .id = codeTva
                },
                .calculationMode = New With {
                    .id = modeCalc
                },
                .quantity = qte,
                .isWithoutTax = isWithoutTax,
                .unitPrice = unitPrice,
                .unitPriceCurrency = unitPrice,
                .percentageAmount = 0,
                .discountAmount = discountAmount,
                .discountAmountCurrency = discountAmount,
                .excludingTaxAmount = excludingTaxAmount,
                .excludingTaxAmountCurrency = excludingTaxAmount,
                .vatAmount = vatAmount,
                .vatAmountCurrency = vatAmount,
                .includingTaxAmount = includingTaxAmount,
                .includingTaxAmountCurrency = includingTaxAmount,
                .vatRate = vatRate,
                .mileage = km,
                .lineNumber = lineNumber,
                .location = New With {
                    .id = agence
                },
                .event = New With {
                    .id = idFact
                },
                .quantityAssignment = New With {
                    .id = "0"
                },
                .amountAssignment = New With {
                    .id = "0"
                },
                .amountCanBeUpdated = True,
                .quantityCanBeUpdated = True
            }

            ' Sérialisation JSON
            Dim settings As New JsonSerializerSettings With {
                .NullValueHandling = NullValueHandling.Ignore
            }

            Dim json As String = JsonConvert.SerializeObject(prestation, settings)
            Dim content As New StringContent(json, Encoding.UTF8, "application/json")

            ' Envoi de la requête POST
            Dim response As HttpResponseMessage = httpClient.PostAsync(apiUrl, content).Result
            If response.IsSuccessStatusCode Then
                Dim result As String = response.Content.ReadAsStringAsync().Result
                Dim PrestaId As String = response.Headers.GetValues("x-vega-id").FirstOrDefault()
                GestionnaireLog.Info("Prestaion (lines) pour la facture " & idFact & " créé : " & PrestaId)
                Return PrestaId
            Else
                Dim errorMsg As String = "Erreur " & response.StatusCode.ToString() & " : " & response.Content.ReadAsStringAsync().Result
                GestionnaireLog.Error("Erreur sur le service création prestation" & errorMsg)
                Throw New ApiLocProException(errorMsg)
                Return errorMsg
            End If
        End Function

        Public Shared Function CreationImmat(codeParc As String, registration As String, dateRegistration As DateTime) As String
            Dim apiUrl As String = "http://192.168.1.19:8001/equipments/" & codeParc & "/registrations"
            Dim httpClient As New HttpClient

            Dim xVegaToken As String = GetToken

            ' Headers
            httpClient.DefaultRequestHeaders.Add("x-vega-token", xVegaToken)
            httpClient.DefaultRequestHeaders.Add("x-vega-version", xVegaVersion)
            httpClient.DefaultRequestHeaders.Add("x-vega-app", xVegaApp)
            httpClient.DefaultRequestHeaders.Add("x-vega-serial", xVegaSerial)
            httpClient.DefaultRequestHeaders.Add("accept-language", acceptLanguage)
            httpClient.DefaultRequestHeaders.Add("x-vega-context", xVegaContext)

            ' Request body
            Dim registrationBody = New With {
                registration,
                .dateRegistration = dateRegistration.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            }

            Dim json As String = JsonConvert.SerializeObject(registrationBody)
            Dim content As New StringContent(json, Encoding.UTF8, "application/json")

            Dim httpResponse As HttpResponseMessage = httpClient.PostAsync(apiUrl, content).Result

            If httpResponse.IsSuccessStatusCode Then
                Dim result As String = httpResponse.Content.ReadAsStringAsync().Result
                Dim registrationId As String = httpResponse.Headers.GetValues("x-vega-id").FirstOrDefault()

                Return registrationId
            Else
                Dim errorMsg As String = "Erreur " & httpResponse.StatusCode.ToString() & " : " & httpResponse.Content.ReadAsStringAsync().Result
                Return errorMsg
            End If
        End Function

        ''' <summary>
        ''' Met à jour un parc dont le code parc est égal à equipmentId avec les informations optionnelles ci-dessous.
        ''' </summary>
        ''' <param name="equipmentId">Code parc</param>
        ''' <param name="positionId">Position administrative</param>
        ''' <param name="registrationId">Clé primaire de l'immatriculation dans LocPro</param>
        ''' <param name="registration">Immatriculation (A)</param>
        ''' <param name="dateRegistration">Date de 1ère immatriculation (B)</param>
        ''' <param name="serialNumber">Numéro d'identification (E)</param>
        ''' <param name="wording">Libellé (D.1 + D.3 + J.3)</param>
        ''' <param name="registrationD2">Type variante version (D.2)</param>
        ''' <param name="mineType">Code national d'identification du type (D.2.1)</param>
        ''' <param name="registrationD3">Dénomination commerciale (D.3)</param>
        ''' <param name="registrationF1">Masse en charge maximale techniquement admissible (F.1)</param>
        ''' <param name="registrationF2">Masse en charge maximale admissible du véhicule en service dans l'état membre d'immatriculation (F.2)</param>
        ''' <param name="registrationF3">Masse en charge maximale admissible de l'ensemble en service dans l'état membre d'immatriculation (F.3)</param>
        ''' <param name="registrationG">Masse du véhicule en service (G)</param>
        ''' <param name="registrationG1">Poids à vide national (G.1)</param>
        ''' <param name="registrationJ">Catégorie du véhicule (J)</param>
        ''' <param name="registrationJ2">Carrosserie CE (J.2)</param>
        ''' <param name="bodyId">Carrosserie (J.3)</param>
        ''' <param name="displacement">Cylindrée (P.1)</param>
        ''' <param name="registrationP2">Puissance nette maximale (P.2)</param>
        ''' <param name="registrationU1">Niveau sonore à l'arrêt (U.1)</param>
        ''' <param name="registrationU2">Vitesse moteur (U.2)</param>
        ''' <param name="registrationV7">C02 (V.7)</param>
        ''' <param name="registrationV9">Classe environnementale (V.9)</param>
        ''' <returns></returns>
        Public Shared Function UpdateParc(equipmentId As String, Optional positionId As String = "", Optional registrationId As String = "", Optional registration As String = "",
                                          Optional dateRegistration As DateTime = Nothing, Optional serialNumber As String = "", Optional wording As String = "",
                                          Optional registrationD2 As String = "", Optional mineType As String = "", Optional registrationD3 As String = "",
                                          Optional registrationF1 As String = "", Optional registrationF2 As String = "",
                                          Optional registrationF3 As String = "", Optional registrationG As String = "", Optional registrationG1 As String = "",
                                          Optional registrationJ As String = "", Optional registrationJ2 As String = "", Optional bodyId As String = "", Optional displacement As String = "",
                                          Optional registrationP2 As String = "", Optional registrationU1 As String = "", Optional registrationU2 As String = "", Optional registrationV7 As String = "",
                                          Optional registrationV9 As String = "") As String
            Dim apiUrl As String = "http://192.168.1.19:8001/equipments/" & equipmentId
            Dim httpClient As New HttpClient
            Dim xVegaToken As String = GetToken

            ' Headers
            httpClient.DefaultRequestHeaders.Add("x-vega-token", xVegaToken)
            httpClient.DefaultRequestHeaders.Add("x-vega-version", xVegaVersion)
            httpClient.DefaultRequestHeaders.Add("x-vega-app", xVegaApp)
            httpClient.DefaultRequestHeaders.Add("x-vega-serial", xVegaSerial)
            httpClient.DefaultRequestHeaders.Add("accept-language", acceptLanguage)
            httpClient.DefaultRequestHeaders.Add("x-vega-context", xVegaContext)

            ' Request body
            Dim parcBody As New Dictionary(Of String, Object)
            Dim positionBody As New Dictionary(Of String, Object)
            Dim registrationBody As New Dictionary(Of String, Object)
            Dim personalModelBody As New Dictionary(Of String, Object)
            Dim bodyBody As New Dictionary(Of String, Object)

            Dim dateRegistrationFormat As String = dateRegistration.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")

            Dim formatDate = Function(v As Date?) If(v.HasValue, v.Value.ToUniversalTime().ToString("o"), Nothing)

            AddIfNotEmpty(positionBody, "id", positionId)
            AddIfNotEmpty(parcBody, "position", positionBody)
            AddIfNotEmpty(registrationBody, "id", registrationId)
            AddIfNotEmpty(registrationBody, "registration", registration)
            AddIfNotEmpty(registrationBody, "dateRegistration", formatDate(dateRegistrationFormat))
            AddIfNotEmpty(parcBody, "registration", registrationBody)
            AddIfNotEmpty(parcBody, "serialNumber", serialNumber)
            AddIfNotEmpty(parcBody, "wording", wording)
            AddIfNotEmpty(personalModelBody, "registrationD2", If(registrationD2 IsNot Nothing AndAlso registrationD2.Length > 30, registrationD2.Substring(0, 30), registrationD2))
            AddIfNotEmpty(personalModelBody, "mineType", mineType)
            AddIfNotEmpty(personalModelBody, "registrationD3", If(registrationD3 IsNot Nothing AndAlso registrationD3.Length > 20, registrationD3.Substring(0, 20), registrationD3))
            AddIfNotEmpty(personalModelBody, "registrationF1", Replace(registrationF1, ",", "."))
            AddIfNotEmpty(personalModelBody, "registrationF2", Replace(registrationF2, ",", "."))
            AddIfNotEmpty(personalModelBody, "registrationF3", Replace(registrationF3, ",", "."))
            AddIfNotEmpty(personalModelBody, "registrationG", Replace(registrationG, ",", "."))
            AddIfNotEmpty(personalModelBody, "registrationG1", Replace(registrationG1, ",", "."))
            AddIfNotEmpty(personalModelBody, "registrationJ", registrationJ)
            AddIfNotEmpty(personalModelBody, "registrationJ2", registrationJ2)
            AddIfNotEmpty(bodyBody, "id", bodyId)
            AddIfNotEmpty(personalModelBody, "body", bodyBody)
            AddIfNotEmpty(personalModelBody, "displacement", Replace(displacement, ",", "."))
            AddIfNotEmpty(personalModelBody, "registrationP2", Replace(registrationP2, ",", "."))
            AddIfNotEmpty(personalModelBody, "registrationU1", registrationU1)
            AddIfNotEmpty(personalModelBody, "registrationU2", registrationU2)
            AddIfNotEmpty(personalModelBody, "registrationV7", Replace(registrationV7, ",", "."))
            AddIfNotEmpty(personalModelBody, "registrationV9", registrationV9)
            AddIfNotEmpty(parcBody, "personalModel", personalModelBody)
            AddIfNotEmpty(parcBody, "firstRegistrationDate", formatDate(dateRegistrationFormat))

            If parcBody.Count = 0 Then
                Return False
            End If

            Dim json As String = JsonConvert.SerializeObject(parcBody)
            Dim content As New StringContent(json, Encoding.UTF8, "application/json")

            Dim response As HttpResponseMessage = httpClient.PutAsync(apiUrl, content).Result

            If response.IsSuccessStatusCode Then
                Return response.StatusCode.ToString()
            Else
                Return "Erreur " & response.StatusCode.ToString() & " : " & response.Content.ReadAsStringAsync().Result & " Content : " & json
            End If
        End Function

        Public Shared Sub AddIfNotEmpty(dic As Dictionary(Of String, Object), key As String, value As String)
            If Not String.IsNullOrEmpty(value) Then
                dic(key) = value
            End If
        End Sub

        Public Shared Sub AddIfNotEmpty(dic As Dictionary(Of String, Object), key As String, value As Dictionary(Of String, Object))
            If value.Count > 0 Then
                dic(key) = value
            End If
        End Sub

        Public Shared Function VerificationExistenceImmat(immat As String) As Boolean
            Dim apiUrl As String = "http://192.168.1.19:8001/equipments?search=" & immat & "&active=true&_limit=NOLIMIT"
            Dim httpClient As New HttpClient
            Dim xVegaToken As String = getToken()

            ' Headers
            httpClient.DefaultRequestHeaders.Add("x-vega-token", xVegaToken)
            httpClient.DefaultRequestHeaders.Add("x-vega-version", xVegaVersion)
            httpClient.DefaultRequestHeaders.Add("x-vega-app", xVegaApp)
            httpClient.DefaultRequestHeaders.Add("x-vega-serial", xVegaSerial)
            httpClient.DefaultRequestHeaders.Add("accept-language", acceptLanguage)
            httpClient.DefaultRequestHeaders.Add("x-vega-context", xVegaContext)

            Dim response As HttpResponseMessage = httpClient.GetAsync(apiUrl).Result
            Dim statusCode As String = response.ToString.Split(",")(0)

            Return statusCode.Contains("200")
        End Function

        '************** Facture Dematerialisée****************

        Public Shared Function createPrestationSimplifie(idFact As String, descLigne As String, codeLigne As String, unitPrice As Double, qte As Double, Optional vatRate As String = Nothing, Optional vatAmount As Double? = Nothing, Optional excludingTaxAmount As Double? = Nothing, Optional includingTaxAmount As Double? = Nothing) As String

            ' DÉSACTIVATION LOCPRO (Mode Simulation)
            GestionnaireLog.Info("Simulation : createPrestationSimplifie ignorée pour éviter l'envoi à LocPro.")
            Return "SIMUL-" & Guid.NewGuid().ToString().Substring(0, 8)

            Dim apiUrl As String = "http://192.168.1.19:8001/invoices/" & idFact & "/lines"
            Dim httpClient As New HttpClient()
            Dim token = getToken()

            ' headers 
            httpClient.DefaultRequestHeaders.Add("x-vega-token", xVegaToken)
            httpClient.DefaultRequestHeaders.Add("x-vega-version", xVegaVersion)
            httpClient.DefaultRequestHeaders.Add("x-vega-app", xVegaApp)
            httpClient.DefaultRequestHeaders.Add("x-vega-serial", xVegaSerial)
            httpClient.DefaultRequestHeaders.Add("accept-language", acceptLanguage)
            httpClient.DefaultRequestHeaders.Add("x-vega-context", xVegaContextTest)
            If Not String.IsNullOrEmpty(token) Then
                httpClient.DefaultRequestHeaders.Add("x-vega-token", token)
            End If

            Dim prestation = New With {
                .wording = descLigne,
                .service = New With {
                    .id = codeLigne
                },
                .unitPrice = unitPrice,
                .unitPriceCurrency = unitPrice,
                .event = New With {
                    .id = idFact
                },
                .quantity = qte,
                .vatRate = vatRate,
                .vatCode = If(vatRate IsNot Nothing, New With {.id = "1"}, Nothing),
                .vatAmount = vatAmount,
                .vatAmountCurrency = vatAmount,
                .excludingTaxAmount = excludingTaxAmount,
                .excludingTaxAmountCurrency = excludingTaxAmount,
                .includingTaxAmount = includingTaxAmount,
                .includingTaxAmountCurrency = includingTaxAmount
            }

            ' Sérialisation JSON
            Dim settings As New JsonSerializerSettings With {
                .NullValueHandling = NullValueHandling.Ignore
            }

            Dim json As String = JsonConvert.SerializeObject(prestation, settings)
            Dim content As New StringContent(json, Encoding.UTF8, "application/json")

            ' Envoi de la requête POST
            Dim response As HttpResponseMessage = httpClient.PostAsync(apiUrl, content).Result
            If response.IsSuccessStatusCode Then
                Dim PrestaId As String = response.Headers.GetValues("x-vega-id").FirstOrDefault()
                GestionnaireLog.Info("Prestation simplifiée pour la facture " & idFact & " créé : " & PrestaId)
                Return PrestaId
            Else
                Dim errorMsg As String = "Erreur " & response.StatusCode.ToString() & " : " & response.Content.ReadAsStringAsync().Result
                GestionnaireLog.Error("Erreur sur le service création prestation simplifiée : " & errorMsg)
                Throw New ApiLocProException(errorMsg)
            End If
        End Function

        Public Shared Function createInvoice(payload As Object) As String

            ' DÉSACTIVATION LOCPRO (Mode Simulation)
            GestionnaireLog.Info("Simulation : createInvoice ignorée pour éviter l'envoi à LocPro.")
            Return "SIMUL-" & Guid.NewGuid().ToString().Substring(0, 8)

            Dim apiUrl As String = "http://192.168.1.19:8001/invoices"
            Dim httpClient As New HttpClient()
            Dim token = getToken()

            ' headers
            httpClient.DefaultRequestHeaders.Add("x-vega-token", xVegaToken)
            httpClient.DefaultRequestHeaders.Add("x-vega-version", xVegaVersion)
            httpClient.DefaultRequestHeaders.Add("x-vega-app", xVegaApp)
            httpClient.DefaultRequestHeaders.Add("x-vega-serial", xVegaSerial)
            httpClient.DefaultRequestHeaders.Add("accept-language", acceptLanguage)
            httpClient.DefaultRequestHeaders.Add("x-vega-context", xVegaContextTest)
            If Not String.IsNullOrEmpty(token) Then
                httpClient.DefaultRequestHeaders.Add("x-vega-token", token)
            End If

            Dim json As String = JsonConvert.SerializeObject(payload, New JsonSerializerSettings With {.NullValueHandling = NullValueHandling.Ignore})

            Dim response = httpClient.PostAsync(apiUrl, New StringContent(json, Encoding.UTF8, "application/json")).Result
            If response.IsSuccessStatusCode Then
                Dim invoiceId As String = response.Headers.GetValues("x-vega-id").FirstOrDefault()
                GestionnaireLog.Info("[createInvoice] Facture créée avec succès : " & invoiceId)
                Return invoiceId
            End If

            Dim errorBody As String = response.Content.ReadAsStringAsync().Result
            GestionnaireLog.Error("[createInvoice] HTTP " & CInt(response.StatusCode).ToString() & " - Payload: " & json & " | Réponse: " & errorBody)
            Throw New ApiLocProException("Erreur création facture: " & errorBody)
        End Function

    End Class

End Namespace