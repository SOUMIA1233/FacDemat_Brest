<%@ Control Language="VB" AutoEventWireup="false" CodeFile="FormulaireCorrespondance.ascx.vb" Inherits="ControlesUtilisateur_FormulaireCorrespondance" %>
	<link href="../styles/form_prestation.css" rel="stylesheet" />
    <telerik:RadWindowManager ID="RadWindowManager1" runat="server"></telerik:RadWindowManager>
<div class="form-correspondance">
    <div class="form-title">
        Ajouter une correspondance prestation Fournisseur/LocPro
    </div>

    <div class="form-section">
        <div class="form-group">
            <label class="form-label">Code Fournisseur</label>
            <asp:TextBox ID="txtCodeFournisseur" runat="server" CssClass="form-control-readonly" ReadOnly="true"></asp:TextBox>
        </div>

        <div class="form-group">
            <label class="form-label">Référence de prestation Fournisseur</label>
            <asp:TextBox ID="txtRefFournisseur" runat="server" CssClass="form-control-readonly" ReadOnly="true"></asp:TextBox>
        </div>

        <div class="form-group">
            <label class="form-label">Libellé de la prestation fournisseur</label>
            <asp:TextBox ID="txtLibelle" runat="server" CssClass="form-control-editable" MaxLength="200"></asp:TextBox>
        </div>
    </div>

    <div class="message-info">
        💡 Vous pouvez associer une ou plusieurs prestations LocPro à cette prestation fournisseur. Configurez la répartition du montant pour chaque prestation. 
        <br />
        <i>Cliquez sur <b>"+ Ajouter une prestation"</b> pour en ajouter d'autres. Une fois que vous avez ajouté toutes les prestations nécessaires, cliquez sur le bouton <b>"Valider et créer"</b> en bas pour les enregistrer.</i>
    </div>

    <!-- NOUVEAU : Conteneur d'erreurs -->
    <div id="panelErreurs" class="panel-erreurs" style="display: none;">
        <div class="erreurs-header">
            <span class="erreurs-icon">❌</span>
            <span class="erreurs-titre">Erreurs de validation</span>
            <button type="button" class="btn-fermer-erreurs" onclick="fermerPanelErreurs()">×</button>
        </div>
        <ul id="listeErreurs" class="erreurs-liste">
            <!-- Les erreurs seront ajoutées ici dynamiquement -->
        </ul>
    </div>

    <telerik:RadCodeBlock ID="RadCodeBlock1" runat="server">
        <script type="text/javascript">
            function confirmDelete(button, event) {
                if (event) {
                    event.preventDefault();
                }
                
                radconfirm("Voulez-vous vraiment supprimer cette prestation ?", function(arg) {
                    if (arg) {
                        // Exécute le __doPostBack contenu dans le href du LinkButton
                        eval(button.href.replace('javascript:', ''));
                    }
                }, 500, 160,null, "Confirmation de suppression");
                
                return false;
            }
        </script>
    </telerik:RadCodeBlock>

    <div class="prestations-section">
        <div class="prestations-title">📋 Prestations LocPro</div>

        <asp:Repeater ID="rptPrestations" runat="server"
            OnItemDataBound="rptPrestations_ItemDataBound"
            OnItemCommand="rptPrestations_ItemCommand">

            <ItemTemplate>
                <div class="prestation-item">
                    <div class="prestation-header">
                        <div class="prestation-number">Prestation <%# Container.ItemIndex + 1 %></div>

                        <asp:LinkButton ID="btnSupprimer" runat="server"
                            CssClass="btn-remove-prestation"
                            CommandName="SUPPRIMER"
                            CommandArgument='<%# Eval("RowId") %>'
                            OnClientClick="return confirmDelete(this, event);"
                            Text="×" />
                    </div>

                    <asp:HiddenField ID="hidRowId" runat="server" Value='<%# Eval("RowId") %>' />

                    <div class="form-group">
                        <label class="form-label">Code LocPro</label>
                        <telerik:RadComboBox ID="cboCodeLocPro" runat="server" Width="600px" 
                            EnableLoadOnDemand="true" ShowMoreResultsBox="true" EnableVirtualScrolling="true"
                            EmptyMessage="Selectionnez un code prestation" AllowCustomText="false" MarkFirstMatch="true" Filter="Contains">
                        </telerik:RadComboBox>
                    </div>

                    <div class="form-group">
                        <label class="form-label">Répartition</label>

                        <div class="repartition-options">
                            <div class="radio-option">
                                <asp:RadioButton ID="rbTotal" runat="server" GroupName='<%# "rep_" & Eval("RowId") %>' Text="Montant total" />
                            </div>

                            <div class="radio-option">
                                <asp:RadioButton ID="rbPourcent" runat="server" GroupName='<%# "rep_" & Eval("RowId") %>' Text="Pourcentage" />
                                <asp:TextBox ID="txtPourcent" runat="server" CssClass="input-inline" />
                            </div>

                            <div class="radio-option">
                                <asp:RadioButton ID="rbMontant" runat="server" GroupName='<%# "rep_" & Eval("RowId") %>' Text="Montant fixe" />
                                <asp:TextBox ID="txtMontant" runat="server" CssClass="input-inline" />
                            </div>

                            <div class="radio-option">
                                <asp:RadioButton ID="rbInclus" runat="server" GroupName='<%# "rep_" & Eval("RowId") %>' Text="Prestaion incluse (0€)" />
                            </div>
                        </div>
                    </div>
                </div>
            </ItemTemplate>
        </asp:Repeater>

        <asp:Button ID="btnAjouterPrestation" runat="server" Text="+ Ajouter une prestation" CssClass="btn-add-prestation" />
    </div>

    <asp:Label ID="lblMessage" runat="server" Visible="false"></asp:Label>

    <asp:Panel ID="pnlRecapitulatif" runat="server" Visible="false" CssClass="panel-recapitulatif">
        <div class="recap-header">
            <span class="recap-icon">📋</span>
            <span class="recap-titre">Récapitulatif de la règle</span>
        </div>
    
        <div class="recap-content">
            <asp:Literal ID="litRecapitulatif" runat="server"></asp:Literal>
        </div>
    
        <div class="recap-footer">
            <asp:Button ID="btnRetourRecap" runat="server" 
                Text="← Retour" 
                CssClass="btn-retour-recap"
                OnClick="btnRetourRecap_Click" />
        
            <asp:Button ID="btnConfirmerCreation" runat="server" 
                Text="✓ Confirmer la création" 
                CssClass="btn-confirmer-creation"
                OnClick="btnConfirmerCreation_Click" />
        </div>
    </asp:Panel>

    <div class="form-footer">
        <telerik:RadButton ID="btnValider" runat="server" Text="Valider et créer" CssClass="btn-submit" OnClientClicking="onClientClicking" />
    </div>

   
</div>
