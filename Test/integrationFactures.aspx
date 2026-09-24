<%@ Page Language="VB" Async="true" AutoEventWireup="false" MasterPageFile="~/MPIntranet.master"
    MaintainScrollPositionOnPostback="true" Title="Intégration de Factures fournisseur"
    CodeFile="integrationFactures.aspx.vb" Inherits="integrationFactures" Culture="fr-FR" UICulture="fr-FR"
    CodePage="65001" %>
    <%@ Register TagPrefix="telerik" Namespace="Telerik.Web.UI" Assembly="Telerik.Web.UI" %>
        <asp:Content ID="Content1" ContentPlaceHolderID="head" runat="Server">
            <style>
                .RadGrid .rgRow>td,
                .RadGrid .rgAltRow>td,
                .RadGrid .rgEditRow>td,
                .RadGrid .rgFooter>td,
                .RadGrid .rgFilterRow>td,
                .RadGrid .rgHeader,
                .RadGrid .rgResizeCol,
                .RadGrid .rgGroupHeader td {
                    padding-left: 5px !important;
                    padding-right: 8px !important;
                }

                .tooltip-cycle {
                    position: relative;
                    display: inline-block;
                    cursor: help;
                }

                .tooltip-cycle .tooltiptext {
                    visibility: hidden;
                    width: 280px;
                    background-color: #333;
                    color: #fff;
                    text-align: left;
                    border-radius: 6px;
                    padding: 10px;
                    position: absolute;
                    z-index: 9999;
                    bottom: 125%;
                    left: 50%;
                    margin-left: -140px;
                    opacity: 0;
                    transition: opacity 0.3s;
                    font-weight: normal;
                    font-size: 12px;
                    line-height: 1.5;
                    box-shadow: 0px 4px 6px rgba(0, 0, 0, 0.3);
                }

                .tooltip-cycle:hover .tooltiptext {
                    visibility: visible;
                    opacity: 1;
                }

                .info-icon {
                    display: inline-block;
                    width: 16px;
                    height: 16px;
                    background: #17a2b8;
                    color: white;
                    border-radius: 50%;
                    text-align: center;
                    line-height: 16px;
                    font-size: 12px;
                    font-style: italic;
                    font-family: serif;
                    margin-left: 5px;
                }

                .textbox-siret-custom {
                    padding: 6px 10px;
                    border: 1px solid #ced4da;
                    border-radius: 4px;
                    font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
                    font-size: 13px;
                    color: #495057;
                    background-color: #fff;
                    transition: border-color 0.15s ease-in-out, box-shadow 0.15s ease-in-out;
                    width: 180px;
                    min-width: 180px;
                    letter-spacing: 0.5px;
                }

                .textbox-siret-custom:focus {
                    outline: none;
                    border-color: #25a0da;
                    box-shadow: 0 0 0 2px rgba(37, 160, 218, 0.2);
                }

                .btn-valider {
                    height: 33px !important;
                    width: 38px !important;
                    border-radius: 4px !important;
                    vertical-align: middle;
                    box-sizing: content-box !important;
                }
            </style>
        </asp:Content>

        <asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="Server">
            <telerik:RadPageLayout runat="server">
                <Rows>
                    <telerik:LayoutRow CssClass="center-content"
                        Style="max-width: 98% !important; width: 98% !important; margin: 0 auto;">
                        <Content>
                            <br />
                            <br />
                            <br />
                            <br />
                            <div
                                style="display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 10px;">
                                <div style="display: flex; flex-direction: column; gap: 5px;">
                                    <div style="display: flex; align-items: center; gap: 15px;">
                                        <h2 style="margin: 0;">Factures Dématérialisées</h2>
                                    </div>
                                    <a href="#" onclick="showInfoStatus(); return false;"
                                        style="font-size: 13px; color: #007bff; text-decoration: none; display: inline-flex; align-items: center; gap: 5px;">
                                        <i class="info-icon"
                                            style="background:#007bff; width: 14px; height: 14px; line-height: 14px; font-size: 11px;">i</i>
                                        <span>Comprendre le processus de cycle de vie</span>
                                    </a>
                                </div>
                            </div>

                            <telerik:RadWindowManager ID="RadWindowManager1" runat="server" Skin="MetroTouch">
                            </telerik:RadWindowManager>
                            <telerik:RadTabStrip ID="rtsFacturesDemat" runat="server" MultiPageID="rmpFacturesDemat"
                                SelectedIndex="0" Skin="MetroTouch" Style="margin-bottom: 10px;">
                                <Tabs>
                                    <telerik:RadTab Text="Factures en cours de traitement" Value="InProgress"
                                        PageViewID="rpvInProgress"></telerik:RadTab>
                                    <telerik:RadTab Text="Historique (Terminées)" Value="History"
                                        PageViewID="rpvHistory"></telerik:RadTab>
                                </Tabs>
                            </telerik:RadTabStrip>

                            <telerik:RadMultiPage ID="rmpFacturesDemat" runat="server" SelectedIndex="0">
                                <telerik:RadPageView ID="rpvInProgress" runat="server">
                                    <asp:HiddenField ID="hfActionToken" runat="server" />
                                    <script type="text/javascript">
                                        function onComptabiliserClicking(sender, args) {
                                            var hf = document.getElementById('<%= hfActionToken.ClientID %>');
                                            if (hf) {
                                                hf.value = new Date().getTime().toString();
                                            }
                                        }
                                    </script>
                                    <telerik:RadGrid ID="rgFacturesDemat" runat="server" AutoGenerateColumns="False"
                                        Width="100%" AllowPaging="True" PageSize="20" Skin="MetroTouch"
                                        CssClass="factures-grid" Style="font-size: 13px;">
                                        <MasterTableView DataKeyNames="IdFacture" CommandItemDisplay="None"
                                            HierarchyLoadMode="Client" RetainExpandStateOnRebind="true">
                                            <Columns>
                                                <telerik:GridTemplateColumn HeaderText="Statut" UniqueName="Statut"
                                                    DataField="Statut" SortExpression="Statut"
                                                    HeaderStyle-Width="110px">
                                                    <ItemTemplate>
                                                        <asp:Label ID="lblStatutDemat" runat="server"
                                                            Text='<%# Eval("Statut") %>'
                                                            CssClass='<%# "statut-badge statut-" & Eval("Statut").ToString().ToLower() %>'>
                                                        </asp:Label>
                                                    </ItemTemplate>
                                                </telerik:GridTemplateColumn>
                                                <telerik:GridBoundColumn DataField="NumFacture" HeaderText="N° Facture"
                                                    UniqueName="NumFacture" HeaderStyle-Width="70px">
                                                </telerik:GridBoundColumn>
                                                <telerik:GridBoundColumn DataField="DateFacture"
                                                    HeaderText="Date Facture" UniqueName="DateFacture"
                                                    DataFormatString="{0:dd/MM/yyyy}" FilterControlWidth="80px"
                                                    HeaderStyle-Width="70px">
                                                </telerik:GridBoundColumn>
                                                <telerik:GridTemplateColumn HeaderText="Code Fournisseur"
                                                    UniqueName="ColCodeFournisseur" HeaderStyle-Width="80px">
                                                    <ItemTemplate>
                                                        <asp:Label ID="lblCodeFournisseur" runat="server"
                                                            CssClass="label-fournisseur"
                                                            Style="white-space: normal; line-height: 1.1; font-weight: bold;">
                                                        </asp:Label>
                                                    </ItemTemplate>
                                                </telerik:GridTemplateColumn>

                                                <telerik:GridTemplateColumn HeaderText="Nom Fournisseur"
                                                    UniqueName="ColRaisonSociale" DataField="RaisonSociale"
                                                    SortExpression="RaisonSociale" HeaderStyle-Width="120px">
                                                    <ItemTemplate>
                                                        <asp:Label ID="lblFournisseur" runat="server"
                                                            Text='<%# Eval("RaisonSociale") %>'
                                                            CssClass="label-fournisseur"
                                                            Style="white-space: normal; line-height: 1.1;">
                                                        </asp:Label>
                                                    </ItemTemplate>
                                                </telerik:GridTemplateColumn>

                                                <telerik:GridTemplateColumn HeaderText="SIREN / SIRET"
                                                    UniqueName="Fournisseur" HeaderStyle-Width="100px">
                                                    <ItemTemplate>
                                                        <asp:Panel ID="pnlFournisseur" runat="server">
                                                            <div style="white-space: nowrap;">
                                                                <asp:Label ID="lblSirenText" runat="server"
                                                                    Visible="false"
                                                                    Style="margin-right: 5px; font-weight: bold;">
                                                                </asp:Label>
                                                                <asp:TextBox ID="txtSirenDemat" runat="server"
                                                                    Visible="true" MaxLength="14"
                                                                    CssClass="textbox-siret-custom"
                                                                    Style="vertical-align: middle; width: 110px;"
                                                                    placeholder="SIREN/SIRET">
                                                                </asp:TextBox>
                                                                <telerik:RadButton ID="btnValiderSirenDemat"
                                                                    runat="server" Visible="true"
                                                                    ButtonType="StandardButton"
                                                                    ToolTip="Valider le SIREN / SIRET"
                                                                    CommandName="ValidateSiren"
                                                                    CommandArgument='<%# Eval("IdFacture") %>'
                                                                    Style="vertical-align: middle; margin-left: 4px; "
                                                                    CssClass="btn-valider">
                                                                    <Icon PrimaryIconCssClass="rbOk" />
                                                                </telerik:RadButton>
                                                            </div>
                                                            <div style="margin-top: 5px;">
                                                                <asp:Label ID="lblSiretMsg" runat="server"
                                                                    Visible="false" Font-Size="11px" Font-Bold="true">
                                                                </asp:Label>
                                                            </div>
                                                        </asp:Panel>
                                                    </ItemTemplate>
                                                </telerik:GridTemplateColumn>

                                                <telerik:GridBoundColumn DataField="TotalHT" HeaderText="Total HT (€)"
                                                    UniqueName="TotalHT" DataFormatString="{0:N2}"
                                                    ItemStyle-HorizontalAlign="Left" HeaderStyle-HorizontalAlign="Left"
                                                    ItemStyle-CssClass="nowrap" HeaderStyle-Width="70px">
                                                </telerik:GridBoundColumn>
                                                <telerik:GridBoundColumn DataField="TotalTTC" HeaderText="Total TTC (€)"
                                                    UniqueName="TotalTTC" DataFormatString="{0:N2}"
                                                    ItemStyle-HorizontalAlign="Left" HeaderStyle-HorizontalAlign="Left"
                                                    ItemStyle-CssClass="nowrap" HeaderStyle-Width="70px">
                                                </telerik:GridBoundColumn>
                                                <telerik:GridBoundColumn DataField="NumOR" HeaderText="N° Commande"
                                                    UniqueName="NumOR" HeaderStyle-Width="70px">
                                                </telerik:GridBoundColumn>
                                                <telerik:GridTemplateColumn UniqueName="StatutCycleDeVie"
                                                    SortExpression="StatutCycleDeVie" HeaderStyle-Width="130px">
                                                    <HeaderTemplate>
                                                        Cycle de Vie (Maileva)
                                                        <span class="tooltip-cycle">
                                                            <i class="info-icon">i</i>
                                                            <span class="tooltiptext">
                                                                <b>Mise à disposition :</b> La facture a été déposée sur
                                                                la plateforme de Dématérialisation Partenaire. C'est le
                                                                statut initial d'attente.<br />
                                                                <b>Prise en charge :</b> L'acheteur prend connaissance
                                                                de la facture et l'accepte pour traitement.<br />
                                                                <b>Suspendue :</b> Le traitement de la facture peut être
                                                                suspendu lorsqu'une ou plusieurs pièces justificatives
                                                                sont manquantes (en attente d'un avoir ou d'une
                                                                correction).<br />
                                                                <b>Refusée :</b> La facture est refusée manuellement
                                                                pour un motif commercial ou de gestion. Elle reste
                                                                visible car on attend la réception d'un avoir (et/ou
                                                                d'une nouvelle facture) pour enfin la
                                                                "Comptabiliser".<br />
                                                                <b>Approuvée :</b> La facture est traitée totalement par
                                                                l'acheteur. Le paiement est validé
                                                            </span>
                                                        </span>
                                                    </HeaderTemplate>
                                                    <ItemTemplate>
                                                        <telerik:RadDropDownList ID="ddlStatutCycleDeVie" runat="server"
                                                            AutoPostBack="true"
                                                            OnSelectedIndexChanged="ddlStatutCycleDeVie_SelectedIndexChanged"
                                                            SelectedValue='<%# If(IsDBNull(Eval("StatutCycleDeVie")) OrElse String.IsNullOrEmpty(Eval("StatutCycleDeVie").ToString()), "IN_PROCESS", Eval("StatutCycleDeVie").ToString().Trim().ToUpper()) %>'>
                                                            <Items>
                                                                <telerik:DropDownListItem Text="Mise à disposition"
                                                                    Value="ACKNOWLEDGE" />
                                                                <telerik:DropDownListItem Text="Prise en charge"
                                                                    Value="IN_PROCESS" />
                                                                <telerik:DropDownListItem Text="Suspendue"
                                                                    Value="ON_HOLD" />
                                                                <telerik:DropDownListItem Text="Refusée"
                                                                    Value="REFUSED" />
                                                                <telerik:DropDownListItem Text="Approuvée"
                                                                    Value="ACCEPTED" />
                                                            </Items>
                                                        </telerik:RadDropDownList>
                                                        <asp:HiddenField ID="hdnIdFactureCycle" runat="server"
                                                            Value='<%# Eval("IdFacture") %>' />
                                                        <div style="margin-top:5px;">
                                                            <asp:Label ID="lblCycleMsg" runat="server" Visible="false"
                                                                Font-Size="11px" Font-Bold="true"></asp:Label>
                                                        </div>
                                                    </ItemTemplate>
                                                </telerik:GridTemplateColumn>
                                                <telerik:GridTemplateColumn HeaderText="PDF" UniqueName="VisualiserPDF"
                                                    ItemStyle-HorizontalAlign="Center"
                                                    HeaderStyle-HorizontalAlign="Center" HeaderStyle-Width="50px">
                                                    <ItemTemplate>
                                                        <a href='DownloadPdf.ashx?id=<%# Eval("IdFacture") %>'
                                                            target="_blank"
                                                            style="text-decoration:none; font-size:20px;"
                                                            title="Visualiser la facture (PDF)">
                                                            &#128196;
                                                        </a>
                                                    </ItemTemplate>
                                                </telerik:GridTemplateColumn>
                                                <telerik:GridTemplateColumn HeaderText="Action"
                                                    UniqueName="Comptabiliser" ItemStyle-HorizontalAlign="Center"
                                                    HeaderStyle-HorizontalAlign="Center" HeaderStyle-Width="110px">
                                                    <ItemTemplate>
                                                        <telerik:RadButton ID="btnComptabiliser" runat="server"
                                                            Text="Comptabiliser" CommandName="Comptabiliser"
                                                            CommandArgument='<%# Eval("IdFacture") %>' Skin="MetroTouch"
                                                            ButtonType="StandardButton" CssClass="btn-success"
                                                            OnClientClicking="onComptabiliserClicking">
                                                        </telerik:RadButton>
                                                    </ItemTemplate>
                                                </telerik:GridTemplateColumn>
                                                <telerik:GridTemplateColumn HeaderText="Message" UniqueName="Message"
                                                    HeaderStyle-Width="200px">
                                                    <ItemTemplate>
                                                        <asp:Label ID="lblMessageDemat" runat="server"
                                                            Text='<%# Eval("Message") %>'
                                                            ToolTip='<%# Eval("Message") %>' Font-Size="12px">
                                                        </asp:Label>
                                                    </ItemTemplate>
                                                </telerik:GridTemplateColumn>
                                            </Columns>
                                            <NestedViewTemplate>
                                                <div
                                                    style="display:flex; width:100%; height:600px; padding: 10px; background-color:#fafafa; border-bottom:1px solid #ddd;">
                                                    <div style="flex:1; overflow-y:auto; padding-right:10px;">
                                                        <h3 style="margin-top:0;">Lignes de prestation</h3>
                                                        <telerik:RadGrid ID="rgLignesInternes" runat="server"
                                                            Width="100%" AutoGenerateColumns="False" Skin="MetroTouch"
                                                            OnNeedDataSource="rgLignesInternes_NeedDataSource"
                                                            OnItemDataBound="rgLignesInternes_ItemDataBound"
                                                            OnItemCommand="rgLignesInternes_ItemCommand">
                                                            <MasterTableView DataKeyNames="IdFacture">
                                                                <Columns>
                                                                    <telerik:GridBoundColumn DataField="NumLig"
                                                                        HeaderText="N° Ligne" UniqueName="NumLig">
                                                                    </telerik:GridBoundColumn>
                                                                    <telerik:GridTemplateColumn
                                                                        HeaderText="Code prestation fournisseur"
                                                                        UniqueName="CodePrestaFournisseur"
                                                                        DataField="CodePrestaFournisseur">
                                                                        <ItemTemplate>
                                                                            <div style="white-space: nowrap;">
                                                                                <asp:TextBox ID="txtRefFournisseur"
                                                                                    runat="server" Visible="false"
                                                                                    MaxLength="50"
                                                                                    CssClass="textbox-siret-custom"
                                                                                    Style="vertical-align: middle; width: 120px; min-width: 120px;"
                                                                                    placeholder="Référence">
                                                                                </asp:TextBox>
                                                                                <telerik:RadButton
                                                                                    ID="btnValiderRefFournisseur"
                                                                                    runat="server" Visible="false"
                                                                                    ButtonType="StandardButton"
                                                                                    ToolTip="Valider la référence"
                                                                                    CommandName="ValidateRefFournisseur"
                                                                                    CommandArgument='<%# Eval("IdFacture").ToString() & "|" & Eval("NumLig").ToString() %>'
                                                                                    Style="vertical-align: middle; margin-left: 4px; "
                                                                                    CssClass="btn-valider">
                                                                                    <Icon PrimaryIconCssClass="rbOk" />
                                                                                </telerik:RadButton>
                                                                            </div>
                                                                            <asp:Label ID="lblRefFournisseur"
                                                                                runat="server"
                                                                                Text='<%# Eval("CodePrestaFournisseur") %>'>
                                                                            </asp:Label>
                                                                        </ItemTemplate>
                                                                    </telerik:GridTemplateColumn>
                                                                    <telerik:GridTemplateColumn HeaderText="Code LocPro"
                                                                        UniqueName="CodePrestaLP">
                                                                        <ItemTemplate>
                                                                            <telerik:RadLabel ID="lblCodePrestaLPDemat"
                                                                                runat="server"
                                                                                Text='<%# Eval("CodePrestaLP") %>'>
                                                                            </telerik:RadLabel>
                                                                            <telerik:RadButton ID="btnAjouterRegleDemat"
                                                                                runat="server" Visible="false"
                                                                                AutoPostBack="false"
                                                                                OnClientClicking="openPopupFromBtn"
                                                                                CommandArgument='<%# Eval("NumOR") & "~" & Eval("NumFacture") & "~" & Eval("CodePrestaFournisseur") & "~" & Eval("Descr") %>'
                                                                                ToolTip="Ajouter une correspondance LocPro"
                                                                                ButtonType="LinkButton" Text="&#10133;"
                                                                                CssClass="btn-ajouter-regle-mini">
                                                                            </telerik:RadButton>
                                                                        </ItemTemplate>
                                                                    </telerik:GridTemplateColumn>
                                                                    <telerik:GridBoundColumn DataField="Descr"
                                                                        HeaderText="Désignation" UniqueName="Descr">
                                                                    </telerik:GridBoundColumn>
                                                                    <telerik:GridBoundColumn DataField="Qte"
                                                                        HeaderText="Qté" UniqueName="Qte"
                                                                        DataFormatString="{0:N0}"
                                                                        ItemStyle-HorizontalAlign="Center">
                                                                    </telerik:GridBoundColumn>
                                                                    <telerik:GridBoundColumn DataField="PrixUnitHT"
                                                                        HeaderText="Prix Unit. HT (€)"
                                                                        UniqueName="PrixUnitHT"
                                                                        DataFormatString="{0:N2}"
                                                                        ItemStyle-HorizontalAlign="Left">
                                                                    </telerik:GridBoundColumn>
                                                                    <telerik:GridBoundColumn DataField="TauxRemise"
                                                                        HeaderText="Remise (%)" UniqueName="TauxRemise"
                                                                        DataFormatString="{0:N2}"
                                                                        ItemStyle-HorizontalAlign="Left">
                                                                    </telerik:GridBoundColumn>
                                                                    <telerik:GridBoundColumn DataField="MontantNetHT"
                                                                        HeaderText="Montant HT (€)"
                                                                        UniqueName="MontantNetHT"
                                                                        DataFormatString="{0:N2}"
                                                                        ItemStyle-HorizontalAlign="Left">
                                                                    </telerik:GridBoundColumn>
                                                                    <telerik:GridBoundColumn DataField="TVA"
                                                                        HeaderText="TVA (%)" UniqueName="TVA"
                                                                        DataFormatString="{0:N2}"
                                                                        ItemStyle-HorizontalAlign="Right">
                                                                    </telerik:GridBoundColumn>
                                                                </Columns>
                                                            </MasterTableView>
                                                        </telerik:RadGrid>
                                                    </div>
                                                    <div id="divPdfViewer" runat="server"
                                                        style="flex:1; padding-left:10px; border-left:1px solid #ccc;">
                                                        <iframe id="iframePdf" runat="server"
                                                            src='<%# "DownloadPdf.ashx?id=" & Eval("IdFacture").ToString() %>'
                                                            width="100%" height="100%" style="border:none;"></iframe>
                                                    </div>
                                                </div>
                                            </NestedViewTemplate>
                                            <PagerStyle Mode="NextPrevAndNumeric" />
                                        </MasterTableView>
                                    </telerik:RadGrid>
                                </telerik:RadPageView>
                                <telerik:RadPageView ID="rpvHistory" runat="server">
                                    <div class="filtres-historique"
                                        style="margin-bottom: 15px; padding: 15px; background-color: #f9f9f9; border: 1px solid #ddd; border-radius: 4px; display: flex; gap: 20px; align-items: flex-end; flex-wrap: wrap;">
                                        <div>
                                            <asp:Label ID="lblFiltreFournisseur" runat="server" Text="Fournisseur:"
                                                Font-Bold="true" Style="display: block; margin-bottom: 5px;">
                                            </asp:Label>
                                            <telerik:RadTextBox ID="txtFiltreFournisseur" runat="server" Width="200px"
                                                EmptyMessage="Nom du Fournisseur..."></telerik:RadTextBox>
                                        </div>
                                        <div>
                                            <asp:Label ID="lblFiltreNumFacture" runat="server" Text="N° Facture:"
                                                Font-Bold="true" Style="display: block; margin-bottom: 5px;">
                                            </asp:Label>
                                            <telerik:RadTextBox ID="txtFiltreNumFacture" runat="server" Width="150px"
                                                EmptyMessage="Numéro..."></telerik:RadTextBox>
                                        </div>
                                        <div>
                                            <asp:Label ID="lblFiltreDate" runat="server" Text="Date de facture:"
                                                Font-Bold="true" Style="display: block; margin-bottom: 5px;">
                                            </asp:Label>
                                            <div style="display: flex; gap: 5px; align-items: center;">
                                                <telerik:RadDatePicker ID="dpFiltreDateDebut" runat="server"
                                                    Width="120px" EmptyMessage="Début">
                                                    <DateInput DateFormat="dd/MM/yyyy" DisplayDateFormat="dd/MM/yyyy"
                                                        runat="server"></DateInput>
                                                </telerik:RadDatePicker>
                                                <span>au</span>
                                                <telerik:RadDatePicker ID="dpFiltreDateFin" runat="server" Width="120px"
                                                    EmptyMessage="Fin">
                                                    <DateInput DateFormat="dd/MM/yyyy" DisplayDateFormat="dd/MM/yyyy"
                                                        runat="server"></DateInput>
                                                </telerik:RadDatePicker>
                                            </div>
                                        </div>
                                        <div style="display: flex; gap: 10px;">
                                            <telerik:RadButton ID="btnFiltrerHistorique" runat="server" Text="Filtrer"
                                                OnClick="btnFiltrerHistorique_Click" Skin="MetroTouch"
                                                Icon-PrimaryIconCssClass="rbSearch"></telerik:RadButton>
                                            <telerik:RadButton ID="btnReinitialiserFiltres" runat="server"
                                                Text="Réinitialiser" OnClick="btnReinitialiserFiltres_Click"
                                                Skin="MetroTouch" ButtonType="LinkButton"></telerik:RadButton>
                                        </div>
                                    </div>
                                    <telerik:RadGrid ID="rgFacturesDematHistorique" runat="server"
                                        AutoGenerateColumns="False" AllowPaging="True" PageSize="20" Skin="MetroTouch"
                                        CssClass="factures-grid" Style="font-size: 13px;">
                                        <MasterTableView DataKeyNames="IdFacture" CommandItemDisplay="None"
                                            HierarchyLoadMode="Client" RetainExpandStateOnRebind="true">
                                            <Columns>
                                                <telerik:GridTemplateColumn HeaderText="Statut" UniqueName="Statut"
                                                    DataField="Statut" SortExpression="Statut"
                                                    HeaderStyle-Width="110px">
                                                    <ItemTemplate>
                                                        <asp:Label ID="lblStatutDemat" runat="server"
                                                            Text='<%# Eval("Statut") %>'
                                                            CssClass='<%# "statut-badge statut-" & Eval("Statut").ToString().ToLower() %>'>
                                                        </asp:Label>
                                                    </ItemTemplate>
                                                </telerik:GridTemplateColumn>
                                                <telerik:GridBoundColumn DataField="NumFacture" HeaderText="N° Facture"
                                                    UniqueName="NumFacture" HeaderStyle-Width="70px">
                                                </telerik:GridBoundColumn>
                                                <telerik:GridBoundColumn DataField="DateFacture"
                                                    HeaderText="Date Facture" UniqueName="DateFacture"
                                                    DataFormatString="{0:dd/MM/yyyy}" FilterControlWidth="80px"
                                                    HeaderStyle-Width="70px">
                                                </telerik:GridBoundColumn>
                                                <telerik:GridTemplateColumn HeaderText="Code Fournisseur"
                                                    UniqueName="ColCodeFournisseur" HeaderStyle-Width="80px">
                                                    <ItemTemplate>
                                                        <asp:Label ID="lblCodeFournisseur" runat="server"
                                                            CssClass="label-fournisseur"
                                                            Style="white-space: normal; line-height: 1.1; font-weight: bold;">
                                                        </asp:Label>
                                                    </ItemTemplate>
                                                </telerik:GridTemplateColumn>
                                                <telerik:GridTemplateColumn HeaderText="FOURNISSEUR"
                                                    UniqueName="ColRaisonSocialeDemat" DataField="RaisonSociale"
                                                    SortExpression="RaisonSociale" HeaderStyle-Width="120px">
                                                    <ItemTemplate>
                                                        <asp:Label ID="lblFournisseurDemat" runat="server"
                                                            Text='<%# Eval("RaisonSociale") %>'
                                                            CssClass="label-fournisseur"
                                                            Style="white-space: normal; line-height: 1.1;">
                                                        </asp:Label>
                                                    </ItemTemplate>
                                                </telerik:GridTemplateColumn>

                                                <telerik:GridTemplateColumn HeaderText="SIREN"
                                                    UniqueName="FournisseurDemat" HeaderStyle-Width="100px">
                                                    <ItemTemplate>
                                                        <asp:Panel ID="pnlFournisseurDemat" runat="server">
                                                            <div style="white-space: nowrap;">
                                                                <asp:Label ID="lblSirenTextDemat" runat="server"
                                                                    Visible="false"
                                                                    Style="margin-right: 5px; font-weight: bold;">
                                                                </asp:Label>
                                                                <asp:TextBox ID="txtSirenDemat" runat="server"
                                                                    Visible="true" MaxLength="14"
                                                                    CssClass="textbox-siret-custom"
                                                                    Style="vertical-align: middle; width: 100px;"
                                                                    placeholder="SIREN/SIRET">
                                                                </asp:TextBox>
                                                                <telerik:RadButton ID="btnValiderSirenDemat"
                                                                    runat="server" Visible="true"
                                                                    ButtonType="StandardButton"
                                                                    ToolTip="Valider le SIREN"
                                                                    CommandName="ValidateSiren"
                                                                    CommandArgument='<%# Eval("IdFacture") %>'
                                                                    Style="vertical-align: middle; margin-left: 4px; "
                                                                    CssClass="btn-valider">
                                                                    <Icon PrimaryIconCssClass="rbOk" />
                                                                </telerik:RadButton>
                                                            </div>
                                                            <div style="margin-top: 5px;">
                                                                <asp:Label ID="lblSiretMsg" runat="server"
                                                                    Visible="false" Font-Size="11px" Font-Bold="true">
                                                                </asp:Label>
                                                            </div>
                                                        </asp:Panel>
                                                    </ItemTemplate>
                                                </telerik:GridTemplateColumn>

                                                <telerik:GridBoundColumn DataField="TotalHT" HeaderText="Total HT (€)"
                                                    UniqueName="TotalHT" DataFormatString="{0:N2}"
                                                    ItemStyle-HorizontalAlign="Left" HeaderStyle-HorizontalAlign="Left"
                                                    ItemStyle-CssClass="nowrap" HeaderStyle-Width="70px">
                                                </telerik:GridBoundColumn>
                                                <telerik:GridBoundColumn DataField="TotalTTC" HeaderText="Total TTC (€)"
                                                    UniqueName="TotalTTC" DataFormatString="{0:N2}"
                                                    ItemStyle-HorizontalAlign="Left" HeaderStyle-HorizontalAlign="Left"
                                                    ItemStyle-CssClass="nowrap" HeaderStyle-Width="70px">
                                                </telerik:GridBoundColumn>
                                                <telerik:GridBoundColumn DataField="NumOR" HeaderText="N° Commande"
                                                    UniqueName="NumOR" HeaderStyle-Width="70px">
                                                </telerik:GridBoundColumn>

                                                <telerik:GridTemplateColumn HeaderText="PDF" UniqueName="VisualiserPDF"
                                                    ItemStyle-HorizontalAlign="Center"
                                                    HeaderStyle-HorizontalAlign="Center" HeaderStyle-Width="50px">
                                                    <ItemTemplate>
                                                        <a href='DownloadPdf.ashx?id=<%# Eval("IdFacture") %>'
                                                            target="_blank"
                                                            style="text-decoration:none; font-size:20px;"
                                                            title="Visualiser la facture (PDF)">
                                                            &#128196;
                                                        </a>
                                                    </ItemTemplate>
                                                </telerik:GridTemplateColumn>

                                                <telerik:GridTemplateColumn HeaderText="Message" UniqueName="Message"
                                                    HeaderStyle-Width="200px">
                                                    <ItemTemplate>
                                                        <asp:Label ID="lblMessageDemat" runat="server"
                                                            Text='<%# Eval("Message") %>'
                                                            ToolTip='<%# Eval("Message") %>' Font-Size="12px">
                                                        </asp:Label>
                                                    </ItemTemplate>
                                                </telerik:GridTemplateColumn>
                                            </Columns>
                                            <NestedViewTemplate>
                                                <div
                                                    style="display:flex; width:100%; height:600px; padding: 10px; background-color:#fafafa; border-bottom:1px solid #ddd;">
                                                    <div style="flex:1; overflow-y:auto; padding-right:10px;">
                                                        <h3 style="margin-top:0;">Lignes de prestation</h3>
                                                        <telerik:RadGrid ID="rgLignesInternes" runat="server"
                                                            Width="100%" AutoGenerateColumns="False" Skin="MetroTouch"
                                                            OnNeedDataSource="rgLignesInternes_NeedDataSource"
                                                            OnItemDataBound="rgLignesInternes_ItemDataBound"
                                                            OnItemCommand="rgLignesInternes_ItemCommand">
                                                            <MasterTableView DataKeyNames="IdFacture">
                                                                <Columns>
                                                                    <telerik:GridBoundColumn DataField="NumLig"
                                                                        HeaderText="N° Ligne" UniqueName="NumLig">
                                                                    </telerik:GridBoundColumn>
                                                                    <telerik:GridTemplateColumn
                                                                        HeaderText="Réf. Fournisseur"
                                                                        UniqueName="CodePrestaFournisseur"
                                                                        DataField="CodePrestaFournisseur">
                                                                        <ItemTemplate>
                                                                            <div style="white-space: nowrap;">
                                                                                <asp:TextBox ID="txtRefFournisseur"
                                                                                    runat="server" Visible="false"
                                                                                    MaxLength="50"
                                                                                    CssClass="textbox-siret-custom"
                                                                                    Style="vertical-align: middle; width: 120px; min-width: 120px;"
                                                                                    placeholder="Référence">
                                                                                </asp:TextBox>
                                                                                <telerik:RadButton
                                                                                    ID="btnValiderRefFournisseur"
                                                                                    runat="server" Visible="false"
                                                                                    ButtonType="StandardButton"
                                                                                    ToolTip="Valider la référence"
                                                                                    CommandName="ValidateRefFournisseur"
                                                                                    CommandArgument='<%# Eval("IdFacture").ToString() & "|" & Eval("NumLig").ToString() %>'
                                                                                    Style="vertical-align: middle; margin-left: 4px; "
                                                                                    CssClass="btn-valider">
                                                                                    <Icon PrimaryIconCssClass="rbOk" />
                                                                                </telerik:RadButton>
                                                                            </div>
                                                                            <asp:Label ID="lblRefFournisseur"
                                                                                runat="server"
                                                                                Text='<%# Eval("CodePrestaFournisseur") %>'>
                                                                            </asp:Label>
                                                                        </ItemTemplate>
                                                                    </telerik:GridTemplateColumn>
                                                                    <telerik:GridTemplateColumn HeaderText="Code LocPro"
                                                                        UniqueName="CodePrestaLP">
                                                                        <ItemTemplate>
                                                                            <telerik:RadLabel ID="lblCodePrestaLPDemat"
                                                                                runat="server"
                                                                                Text='<%# Eval("CodePrestaLP") %>'>
                                                                            </telerik:RadLabel>
                                                                            <telerik:RadButton ID="btnAjouterRegleDemat"
                                                                                runat="server" Visible="false"
                                                                                AutoPostBack="false"
                                                                                OnClientClicking="openPopupFromBtn"
                                                                                CommandArgument='<%# Eval("NumOR") & "~" & Eval("NumFacture") & "~" & Eval("CodePrestaFournisseur") & "~" & Eval("Descr") %>'
                                                                                ToolTip="Ajouter une correspondance LocPro"
                                                                                ButtonType="LinkButton" Text="&#10133;"
                                                                                CssClass="btn-ajouter-regle-mini">
                                                                            </telerik:RadButton>
                                                                        </ItemTemplate>
                                                                    </telerik:GridTemplateColumn>
                                                                    <telerik:GridBoundColumn DataField="Descr"
                                                                        HeaderText="Désignation" UniqueName="Descr">
                                                                    </telerik:GridBoundColumn>
                                                                    <telerik:GridBoundColumn DataField="Qte"
                                                                        HeaderText="Qté" UniqueName="Qte"
                                                                        DataFormatString="{0:N0}"
                                                                        ItemStyle-HorizontalAlign="Center">
                                                                    </telerik:GridBoundColumn>
                                                                    <telerik:GridBoundColumn DataField="PrixUnitHT"
                                                                        HeaderText="Prix Unit. HT (€)"
                                                                        UniqueName="PrixUnitHT"
                                                                        DataFormatString="{0:N2}"
                                                                        ItemStyle-HorizontalAlign="Left">
                                                                    </telerik:GridBoundColumn>
                                                                    <telerik:GridBoundColumn DataField="TauxRemise"
                                                                        HeaderText="Remise (%)" UniqueName="TauxRemise"
                                                                        DataFormatString="{0:N2}"
                                                                        ItemStyle-HorizontalAlign="Left">
                                                                    </telerik:GridBoundColumn>
                                                                    <telerik:GridBoundColumn DataField="MontantNetHT"
                                                                        HeaderText="Montant HT (€)"
                                                                        UniqueName="MontantNetHT"
                                                                        DataFormatString="{0:N2}"
                                                                        ItemStyle-HorizontalAlign="Left">
                                                                    </telerik:GridBoundColumn>
                                                                    <telerik:GridBoundColumn DataField="TVA"
                                                                        HeaderText="TVA (%)" UniqueName="TVA"
                                                                        DataFormatString="{0:N2}"
                                                                        ItemStyle-HorizontalAlign="Right">
                                                                    </telerik:GridBoundColumn>
                                                                </Columns>
                                                            </MasterTableView>
                                                        </telerik:RadGrid>
                                                    </div>
                                                    <div id="divPdfViewer" runat="server"
                                                        style="flex:1; padding-left:10px; border-left:1px solid #ccc;">
                                                        <iframe id="iframePdf" runat="server"
                                                            src='<%# "DownloadPdf.ashx?id=" & Eval("IdFacture").ToString() %>'
                                                            width="100%" height="100%" style="border:none;"></iframe>
                                                    </div>
                                                </div>
                                            </NestedViewTemplate>
                                            <PagerStyle Mode="NextPrevAndNumeric" />
                                        </MasterTableView>
                                    </telerik:RadGrid>
                                </telerik:RadPageView>
                            </telerik:RadMultiPage>
                            <div style="margin-top: 15px; margin-bottom: 20px;">
                                <asp:Label ID="lblResultatReintegrationDemat" runat="server" Visible="false"
                                    CssClass="label-resultat-reintegration"
                                    Style="padding: 10px; border-radius: 4px; font-size: 14px; display: inline-block;">
                                </asp:Label>
                            </div>
                            <br />

                            <h1>Intégration de Factures fournisseur</h1>
                            <telerik:RadAjaxLoadingPanel runat="server" ID="LoadingPanel"></telerik:RadAjaxLoadingPanel>
                            <telerik:RadAjaxPanel ID="RadAjaxPanel1" LoadingPanelID="LoadingPanel" runat="server">
                                <!-- RadWindow déplacé en haut pour éviter le saut de page vers le bas lors du focus natif du navigateur -->
                                <telerik:RadWindow ID="rwFormulaireCorrespondance" runat="server"
                                    Title="Ajouter une correspondance prestation" Width="1050px" Height="800px"
                                    Modal="true" Behaviors="Close,Move" VisibleStatusbar="false" Skin="MetroTouch"
                                    KeepInScreenBounds="true" CenterIfModal="true" ShowContentDuringLoad="false">
                                </telerik:RadWindow>

                                <p>Veuillez téléverser les pdfs des factures à intégrer.
                                </p>
                                <telerik:RadAsyncUpload runat="server" ID="rtb_repertoire"
                                    MultipleFileSelection="Automatic" MaxFileSize="10485760" CssClass="input">
                                </telerik:RadAsyncUpload>
                                <br />
                                <telerik:RadButton runat="server" ID="rb_validerRep" Text="Intégrer"
                                    CssClass="bouton-custom" HoveredCssClass="bouton-hover" Width="100%">
                                </telerik:RadButton>
                                <br />
                                <asp:Label ID="lbl_result" runat="server" CssClass="result-label"></asp:Label>
                                <br />
                                <div
                                    style="display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 10px;">
                                    <div style="display: flex; flex-direction: column; gap: 5px;">
                                        <div style="display: flex; align-items: center; gap: 15px;">
                                            <h2 style="margin: 0;">Historique d'import des factures fournisseur</h2>
                                            <asp:Label ID="lblResultatReintegration" runat="server" Visible="false"
                                                CssClass="label-resultat-reintegration"
                                                Style="padding: 5px 10px; border-radius: 4px; font-size: 13px;">
                                            </asp:Label>
                                        </div>
                                    </div>
                                    <telerik:RadButton ID="btnReintegrerTout" runat="server"
                                        Text="Ré-intégrer tout (LocPro)" Skin="Bootstrap" ButtonType="StandardButton"
                                        CssClass="btn-reintegrer-tout" OnClick="btnReintegrerTout_Click">
                                        <Icon PrimaryIconCssClass="rbRefresh" />
                                    </telerik:RadButton>
                                </div>

                                <telerik:RadGrid ID="rgHistoriqueFactures" runat="server" AutoGenerateColumns="False"
                                    AllowPaging="True" PageSize="20" Skin="MetroTouch" CssClass="factures-grid">
                                    <MasterTableView DataKeyNames="NumOR,NumFacture" CommandItemDisplay="Top"
                                        HierarchyLoadMode="Client" RetainExpandStateOnRebind="true">
                                        <CommandItemSettings ShowRefreshButton="true" ShowAddNewRecordButton="false" />
                                        <Columns>
                                            <telerik:GridTemplateColumn HeaderText="Statut" UniqueName="Statut"
                                                DataField="Statut" SortExpression="Statut">
                                                <ItemTemplate>
                                                    <asp:Label ID="lblStatut" runat="server"
                                                        Text='<%# Eval("Statut") %>'
                                                        CssClass='<%# "statut-badge statut-" & Eval("Statut").ToString().ToLower() %>'>
                                                    </asp:Label>
                                                </ItemTemplate>
                                            </telerik:GridTemplateColumn>
                                            <telerik:GridBoundColumn DataField="NumFacture" HeaderText="N° Facture"
                                                UniqueName="NumFacture"></telerik:GridBoundColumn>
                                            <telerik:GridBoundColumn DataField="DateFacture" HeaderText="Date Facture"
                                                UniqueName="DateFacture" DataFormatString="{0:dd/MM/yyyy}"
                                                FilterControlWidth="100px"></telerik:GridBoundColumn>
                                            <telerik:GridTemplateColumn HeaderText="FOURNISSEUR"
                                                UniqueName="Fournisseur" DataField="RaisonSociale"
                                                SortExpression="RaisonSociale">
                                                <ItemTemplate>
                                                    <asp:Panel ID="pnlFournisseur" runat="server">
                                                        <asp:TextBox ID="txtSiret" runat="server" Visible="false"
                                                            MaxLength="14" Width="55%" CssClass="textbox-siret-custom"
                                                            placeholder="14 chiffres"></asp:TextBox>
                                                        <telerik:RadButton ID="btnValiderSiret" runat="server"
                                                            Width="20%" Visible="false" ButtonType="StandardButton"
                                                            ToolTip="Valider le SIRET" CommandName="ValidateSiret"
                                                            CommandArgument='<%# Eval("NumOR") & "|" & Eval("NumFacture") %>'
                                                            CssClass="btn-valider">
                                                            <Icon PrimaryIconCssClass="rbOk" />
                                                        </telerik:RadButton>

                                                        <asp:Label ID="lblFournisseur" runat="server"
                                                            Text='<%# Eval("RaisonSociale") %>'
                                                            CssClass="label-fournisseur">
                                                        </asp:Label>
                                                    </asp:Panel>
                                                </ItemTemplate>
                                            </telerik:GridTemplateColumn>
                                            <telerik:GridTemplateColumn HeaderText="Immat" UniqueName="Immat"
                                                DataField="Immat" SortExpression="Immat">
                                                <ItemTemplate>
                                                    <asp:Panel ID="pnlImmat" runat="server">
                                                        <asp:TextBox ID="txtImmat" runat="server" Visible="false"
                                                            MaxLength="9" Width="45%"
                                                            CssClass="textbox-editable textbox-immat"
                                                            placeholder="AA123BB"></asp:TextBox>

                                                        <telerik:RadButton ID="btnValiderImmat" runat="server"
                                                            Visible="false" Width="20%" ButtonType="StandardButton"
                                                            ToolTip="Valider l'immatriculation"
                                                            CommandName="ValidateImmat"
                                                            CommandArgument='<%# Eval("NumOR") & "|" & Eval("NumFacture") %>'
                                                            CssClass="btn-valider">
                                                            <Icon PrimaryIconCssClass="rbOk" />
                                                        </telerik:RadButton>

                                                        <asp:Label ID="lblImmat" runat="server" Visible="false"
                                                            Text='<%# Eval("Immat") %>'></asp:Label>
                                                    </asp:Panel>
                                                </ItemTemplate>
                                            </telerik:GridTemplateColumn>
                                            <telerik:GridBoundColumn DataField="CodeParc" HeaderText="Code Parc"
                                                UniqueName="CodeParc"></telerik:GridBoundColumn>
                                            <telerik:GridBoundColumn DataField="TotalHT" HeaderText="Total HT"
                                                UniqueName="TotalHT" DataFormatString="{0:N2} €"
                                                ItemStyle-HorizontalAlign="Left" HeaderStyle-HorizontalAlign="Left"
                                                ItemStyle-CssClass="nowrap"></telerik:GridBoundColumn>
                                            <telerik:GridBoundColumn DataField="TotalTTC" HeaderText="Total TTC"
                                                UniqueName="TotalTTC" DataFormatString="{0:N2} €"
                                                ItemStyle-HorizontalAlign="Left" HeaderStyle-HorizontalAlign="Left"
                                                ItemStyle-CssClass="nowrap"></telerik:GridBoundColumn>
                                            <telerik:GridBoundColumn DataField="NumOR" HeaderText="N° Commande"
                                                UniqueName="NumOR"></telerik:GridBoundColumn>
                                            <telerik:GridTemplateColumn HeaderText="Message" UniqueName="Message">
                                                <ItemTemplate>
                                                    <asp:Label ID="lblMessage" runat="server"
                                                        Text='<%# Eval("Message") %>' ToolTip='<%# Eval("Message") %>'>
                                                    </asp:Label>
                                                </ItemTemplate>
                                            </telerik:GridTemplateColumn>
                                        </Columns>
                                        <DetailTables>
                                            <telerik:GridTableView Name="LignesFacture"
                                                DataKeyNames="NumOR,NumFacture,NumLig" Width="100%"
                                                AutoGenerateColumns="False">
                                                <ParentTableRelation>
                                                    <telerik:GridRelationFields DetailKeyField="NumOR"
                                                        MasterKeyField="NumOR" />
                                                    <telerik:GridRelationFields DetailKeyField="NumFacture"
                                                        MasterKeyField="NumFacture" />
                                                </ParentTableRelation>
                                                <Columns>
                                                    <telerik:GridBoundColumn DataField="NumLig" HeaderText="N° Ligne"
                                                        UniqueName="NumLig">
                                                    </telerik:GridBoundColumn>
                                                    <telerik:GridBoundColumn DataField="CodePrestaFournisseur"
                                                        HeaderText="Code prestation fournisseur"
                                                        UniqueName="CodePrestaFournisseur"></telerik:GridBoundColumn>
                                                    <telerik:GridBoundColumn DataField="Descr" HeaderText="Désignation"
                                                        UniqueName="Descr">
                                                    </telerik:GridBoundColumn>
                                                    <telerik:GridTemplateColumn HeaderText="Code(s) LocPro"
                                                        UniqueName="CodePrestaLP" DataField="CodePrestaLP">
                                                        <ItemTemplate>
                                                            <telerik:RadLabel ID="lblCodePrestaLP" runat="server"
                                                                Text='<%# Eval("CodePrestaLP") %>'
                                                                CssClass="code-presta-label">
                                                            </telerik:RadLabel>

                                                            <telerik:RadButton ID="btnAjouterRegle" runat="server"
                                                                ButtonType="LinkButton" AutoPostBack="false"
                                                                OnClientClicking="openPopupFromBtn"
                                                                CommandArgument='<%# Eval("NumOR") & "~" & Eval("NumFacture") & "~" & Eval("CodePrestaFournisseur") & "~" & Eval("Descr") %>'
                                                                Visible="false"
                                                                ToolTip="Créer une règle de correspondance"
                                                                Text="&#10133;" CssClass="btn-ajouter-regle-mini">
                                                            </telerik:RadButton>
                                                        </ItemTemplate>
                                                    </telerik:GridTemplateColumn>
                                                    <telerik:GridBoundColumn DataField="Qte" HeaderText="Qté"
                                                        UniqueName="Qte" DataFormatString="{0:N0}"
                                                        ItemStyle-HorizontalAlign="Center"
                                                        HeaderStyle-HorizontalAlign="Center"></telerik:GridBoundColumn>
                                                    <telerik:GridBoundColumn DataField="PrixUnitHT"
                                                        HeaderText="Prix Unit. HT" UniqueName="PrixUnitHT"
                                                        DataFormatString="{0:N2} €" ItemStyle-HorizontalAlign="Left"
                                                        HeaderStyle-HorizontalAlign="Left"></telerik:GridBoundColumn>
                                                    <telerik:GridBoundColumn DataField="TauxRemise"
                                                        HeaderText="Remise %" UniqueName="TauxRemise"
                                                        DataFormatString="{0:N2} %" ItemStyle-HorizontalAlign="Left"
                                                        HeaderStyle-HorizontalAlign="Left"></telerik:GridBoundColumn>
                                                    <telerik:GridBoundColumn DataField="MontantNetHT"
                                                        HeaderText="Montant HT" UniqueName="MontantNetHT"
                                                        DataFormatString="{0:N2} €" ItemStyle-HorizontalAlign="Left"
                                                        HeaderStyle-HorizontalAlign="Left"></telerik:GridBoundColumn>
                                                    <telerik:GridBoundColumn DataField="TVA" HeaderText="TVA %"
                                                        UniqueName="TVA" DataFormatString="{0:N2} %"
                                                        ItemStyle-HorizontalAlign="Right"
                                                        HeaderStyle-HorizontalAlign="Right"></telerik:GridBoundColumn>
                                                </Columns>
                                            </telerik:GridTableView>
                                        </DetailTables>
                                        <PagerStyle Mode="NextPrevAndNumeric" />
                                    </MasterTableView>
                                </telerik:RadGrid>



                                <br />


                                <telerik:RadWindow ID="rwIntegrationResult" runat="server"
                                    Title="Résultat de l'intégration" Width="500px" Height="250px" Modal="true"
                                    Behaviors="Close,Move" VisibleStatusbar="false" Skin="MetroTouch"
                                    KeepInScreenBounds="true" CenterIfModal="true">
                                    <ContentTemplate>
                                        <div
                                            style="padding: 30px; text-align: center; font-family: 'Segoe UI', Tahoma, sans-serif;">
                                            <asp:Label ID="lblIntegrationResult" runat="server" Font-Size="16px"
                                                Font-Bold="true"></asp:Label>
                                        </div>
                                    </ContentTemplate>
                                </telerik:RadWindow>

                                <telerik:RadWindow ID="rwInfoStatus" runat="server"
                                    Title="Processus et cycle de vie des factures" Width="600px" Height="450px"
                                    Modal="true" Behaviors="Close,Move" VisibleStatusbar="false" Skin="MetroTouch">
                                    <ContentTemplate>
                                        <div
                                            style="padding: 20px; font-family: 'Segoe UI', Tahoma, sans-serif; line-height: 1.6; color: #333;">
                                            <h3 style="margin-top: 0; color: #444;">Détails des statuts cycle de vie des
                                                factures</h3>
                                            <p style="font-size: 14px; margin-bottom: 15px;">Ce diagramme explique ce
                                                que chaque statut cycle de vie signifie :
                                            </p>
                                            <ul style="list-style-type: none; padding: 0; font-size: 14px;">
                                                <li
                                                    style="margin-bottom: 12px; padding-left: 10px; border-left: 4px solid #6c757d;">
                                                    <b style="color: #6c757d;">Mise à disposition :</b> La facture a été
                                                    déposée sur la plateforme de Dématérialisation Partenaire. C'est le
                                                    statut initial d'attente.
                                                </li>
                                                <li
                                                    style="margin-bottom: 12px; padding-left: 10px; border-left: 4px solid #007bff;">
                                                    <b style="color: #007bff;">Prise en charge :</b> L'acheteur prend
                                                    connaissance de la facture et l'accepte pour traitement.
                                                </li>
                                                <li
                                                    style="margin-bottom: 12px; padding-left: 10px; border-left: 4px solid #dc3545;">
                                                    <b style="color: #dc3545;">Suspendue :</b> Le traitement de la
                                                    facture peut être suspendu lorsqu'une ou plusieurs pièces
                                                    justificatives sont manquantes(en attente d'un avoir ou d'une
                                                    correction).
                                                </li>
                                                <li
                                                    style="margin-bottom: 12px; padding-left: 10px; border-left: 4px solid #343a40;">
                                                    <b style="color: #343a40;">Refusée :</b> La facture est refusée
                                                    manuellement pour un motif commercial ou de gestion. Elle reste
                                                    visible car on attend la réception d'un
                                                    avoir (et/ou d'une nouvelle facture) pour enfin la "Comptabiliser".
                                                </li>
                                                <li
                                                    style="margin-bottom: 12px; padding-left: 10px; border-left: 4px solid #28a745;">
                                                    <b style="color: #28a745;">Approuvée :</b> La facture est traitée
                                                    totalement par l'acheteur. Le paiement est validé
                                                </li>
                                            </ul>
                                        </div>
                                    </ContentTemplate>
                                </telerik:RadWindow>

                                <telerik:RadCodeBlock runat="server">
                                    <script>
                                        function openPopupFromBtn(sender, args) {
                                            var argsStr = sender.get_commandArgument();
                                            var argArr = argsStr.split("~");
                                            var numOR = argArr[0] || "";
                                            var numFacture = argArr[1] || "";
                                            var refFour = argArr[2] || "";
                                            var libelle = argArr[3] || "";
                                            var codeFour = argArr[4] || "";

                                            // Détecter la grille active pour que la popup sache quoi rafraîchir
                                            var gridID = "";
                                            var gridDemat = $find("<%= rgFacturesDemat.ClientID %>");
                                            var gridHisto = $find("<%= rgFacturesDematHistorique.ClientID %>");
                                            if (gridDemat && gridDemat.get_element().offsetHeight > 0) {
                                                gridID = "<%= rgFacturesDemat.ClientID %>";
                                            } else if (gridHisto && gridHisto.get_element().offsetHeight > 0) {
                                                gridID = "<%= rgFacturesDematHistorique.ClientID %>";
                                            }

                                            var url = "FormulaireCorrespondancePopup.aspx?" +
                                                "codeFour=" + encodeURIComponent(codeFour) +
                                                "&refFour=" + encodeURIComponent(refFour) +
                                                "&libelle=" + encodeURIComponent(libelle) +
                                                "&numOR=" + encodeURIComponent(numOR) +
                                                "&numFac=" + encodeURIComponent(numFacture) +
                                                "&gridID=" + encodeURIComponent(gridID);

                                            var oWnd = $find("<%= rwFormulaireCorrespondance.ClientID %>");
                                            if (oWnd) {
                                                // Sauvegarde de la position pour bloquer le saut vers le bas
                                                var y = window.scrollY || document.documentElement.scrollTop;
                                                var x = window.scrollX || document.documentElement.scrollLeft;

                                                oWnd.setUrl(url);
                                                oWnd.show();

                                                // Restauration immédiate après le focus auto de Telerik
                                                setTimeout(function () { window.scrollTo(x, y); }, 10);
                                            }
                                        }


                                        function closeIntegrationResult(sender, args) {
                                            var win = $find("<%= rwIntegrationResult.ClientID %>");
                                            if (win) win.close();
                                        }

                                        function showIntegrationResult(isSuccess, message) {
                                            var lbl = document.getElementById("<%= lblIntegrationResult.ClientID %>");
                                            if (lbl) {
                                                if (isSuccess) {
                                                    lbl.innerHTML = "<span style='color: #4CAF50;'>" + message + "</span>";
                                                } else {
                                                    lbl.innerHTML = "<span style='color: #F44336;'>" + message + "</span>";
                                                }
                                            }
                                            var win = $find("<%= rwIntegrationResult.ClientID %>");
                                            if (win) {
                                                win.show();
                                            }
                                        }

                                        function showInfoStatus() {
                                            var win = $find("<%= rwInfoStatus.ClientID %>");
                                            if (win) {
                                                win.show();
                                            }
                                        }
                                    </script>
                                </telerik:RadCodeBlock>
                            </telerik:RadAjaxPanel>
                            <br />
                        </Content>
                    </telerik:LayoutRow>
                </Rows>
            </telerik:RadPageLayout>
        </asp:Content>