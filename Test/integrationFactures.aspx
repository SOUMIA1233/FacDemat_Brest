<%@ Page Language="VB" Async="true" AutoEventWireup="false" MasterPageFile="~/MPIntranet.master" MaintainScrollPositionOnPostback="true"
    Title="Intégration de Factures fournisseur" CodeFile="integrationFactures.aspx.vb" Inherits="integrationFactures"
    Culture="fr-FR" UICulture="fr-FR" CodePage="65001" %>
    <%@ Register TagPrefix="telerik" Namespace="Telerik.Web.UI" Assembly="Telerik.Web.UI" %>
        <asp:Content ID="Content1" ContentPlaceHolderID="head" runat="Server">
            <style>
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
                                <telerik:RadButton ID="btnReintegrerToutDemat" runat="server"
                                    Text="Ré-intégrer tout (LocPro)" Skin="Bootstrap" ButtonType="StandardButton"
                                    CssClass="btn-reintegrer-tout" OnClick="btnReintegrerToutDemat_Click">
                                    <Icon PrimaryIconCssClass="rbRefresh" />
                                </telerik:RadButton>
                            </div>

                            <telerik:RadTabStrip ID="rtsFacturesDemat" runat="server" MultiPageID="rmpFacturesDemat" SelectedIndex="0" Skin="MetroTouch" Style="margin-bottom: 10px;">
                                <Tabs>
                                    <telerik:RadTab Text="Factures en cours de traitement" Value="InProgress" PageViewID="rpvInProgress"></telerik:RadTab>
                                    <telerik:RadTab Text="Historique (Terminées)" Value="History" PageViewID="rpvHistory"></telerik:RadTab>
                                </Tabs>
                            </telerik:RadTabStrip>

                            <telerik:RadMultiPage ID="rmpFacturesDemat" runat="server" SelectedIndex="0">
                                <telerik:RadPageView ID="rpvInProgress" runat="server">
                                    <telerik:RadGrid ID="rgFacturesDemat" runat="server" AutoGenerateColumns="False"
                                        Width="100%" AllowPaging="True" PageSize="20" Skin="MetroTouch" CssClass="factures-grid"
                                        OnNeedDataSource="rgFacturesDemat_NeedDataSource"
                                OnItemDataBound="rgFacturesDemat_ItemDataBound"
                                OnItemCommand="rgFacturesDemat_ItemCommand">
                                <MasterTableView DataKeyNames="IdFacture" CommandItemDisplay="None"
                                    HierarchyLoadMode="Client" RetainExpandStateOnRebind="true">
                                    <Columns>
                                        <telerik:GridTemplateColumn HeaderText="Statut" UniqueName="Statut"
                                            DataField="Statut" SortExpression="Statut">
                                            <ItemTemplate>
                                                <asp:Label ID="lblStatutDemat" runat="server"
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
                                            UniqueName="FournisseurDemat" DataField="RaisonSociale"
                                            SortExpression="RaisonSociale" HeaderStyle-Width="250px">
                                            <ItemTemplate>
                                                <asp:Panel ID="pnlFournisseurDemat" runat="server">
                                                    <div style="white-space: nowrap;">
                                                        <asp:TextBox ID="txtSiretDemat" runat="server" Visible="false"
                                                            MaxLength="14" CssClass="textbox-siret-custom"
                                                            Style="vertical-align: middle;" placeholder="14 chiffres">
                                                        </asp:TextBox>
                                                        <telerik:RadButton ID="btnValiderSiretDemat" runat="server"
                                                            Visible="false" ButtonType="StandardButton"
                                                            ToolTip="Valider le SIRET" CommandName="ValidateSiret"
                                                            CommandArgument='<%# Eval("IdFacture") %>'
                                                            Style="vertical-align: middle; margin-left: 4px; "
                                                            CssClass="btn-valider">
                                                            <Icon PrimaryIconCssClass="rbOk" />
                                                        </telerik:RadButton>
                                                    </div>

                                                    <asp:Label ID="lblFournisseurDemat" runat="server"
                                                        Text='<%# Eval("RaisonSociale") %>'
                                                        CssClass="label-fournisseur">
                                                    </asp:Label>
                                                    <div style="margin-top: 5px;">
                                                        <asp:Label ID="lblSiretMsg" runat="server" Visible="false"
                                                            Font-Size="11px" Font-Bold="true"></asp:Label>
                                                    </div>
                                                </asp:Panel>
                                            </ItemTemplate>
                                        </telerik:GridTemplateColumn>

                                        <telerik:GridBoundColumn DataField="TotalHT" HeaderText="Total HT"
                                            UniqueName="TotalHT" DataFormatString="{0:N2} €"
                                            ItemStyle-HorizontalAlign="Left" HeaderStyle-HorizontalAlign="Left"
                                            ItemStyle-CssClass="nowrap"></telerik:GridBoundColumn>
                                        <telerik:GridBoundColumn DataField="TotalTTC" HeaderText="Total TTC"
                                            UniqueName="TotalTTC" DataFormatString="{0:N2} €"
                                            ItemStyle-HorizontalAlign="Left" HeaderStyle-HorizontalAlign="Left"
                                            ItemStyle-CssClass="nowrap"></telerik:GridBoundColumn>
                                        <telerik:GridBoundColumn DataField="NumOR" HeaderText="N° OR"
                                            UniqueName="NumOR"></telerik:GridBoundColumn>
                                        <telerik:GridTemplateColumn UniqueName="StatutCycleDeVie"
                                            SortExpression="StatutCycleDeVie" HeaderStyle-Width="180px">
                                            <HeaderTemplate>
                                                Cycle de Vie (Maileva)
                                                <span class="tooltip-cycle">
                                                    <i class="info-icon">i</i>
                                                    <span class="tooltiptext">
                                                        <b>Prise en charge :</b> En cours de traitement<br />
                                                        <b>Suspendue :</b> Erreur côté fournisseur. En attente de sa correction<br />
                                                        <b>Refusée :</b> Facture définitivement rejetée (ex: doublon)<br />
                                                        <b>Approuvée partiel. :</b> Intégrée avec des réserves<br />
                                                        <b>Paiement Transmis :</b> Intégrée avec succès, paiement acté<br />
                                                        <b>En litige :</b> Désaccord, en attente d'une action ou d'un avoir
                                                    </span>
                                                </span>
                                            </HeaderTemplate>
                                            <ItemTemplate>
                                                <telerik:RadDropDownList ID="ddlStatutCycleDeVie" runat="server"
                                                    AutoPostBack="true"
                                                    OnSelectedIndexChanged="ddlStatutCycleDeVie_SelectedIndexChanged"
                                                    SelectedValue='<%# If(IsDBNull(Eval("StatutCycleDeVie")) OrElse String.IsNullOrEmpty(Eval("StatutCycleDeVie").ToString()), "IN_PROCESS", Eval("StatutCycleDeVie").ToString().Trim().ToUpper()) %>'>
                                                    <Items>
                                                        <telerik:DropDownListItem Text="Prise en charge"
                                                            Value="IN_PROCESS" />
                                                        <telerik:DropDownListItem Text="Suspendue" Value="ON_HOLD" />
                                                        <telerik:DropDownListItem Text="Refusée" Value="REFUSED" />
                                                        <telerik:DropDownListItem Text="Approuvée Partiellement"
                                                            Value="CONDITIONNALY_ACCEPTED" />
                                                        <telerik:DropDownListItem Text="Paiement Transmis"
                                                            Value="PAID" />
                                                        <telerik:DropDownListItem Text="En litige"
                                                            Value="UNDER_QUERY" />
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
                                            ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center">
                                            <ItemTemplate>
                                                <a href='DownloadPdf.ashx?id=<%# Eval("IdFacture") %>' target="_blank"
                                                    style="text-decoration:none; font-size:20px;"
                                                    title="Visualiser la facture (PDF)">
                                                    &#128196;
                                                </a>
                                            </ItemTemplate>
                                        </telerik:GridTemplateColumn>
                                        <telerik:GridTemplateColumn HeaderText="Message" UniqueName="Message">
                                            <ItemTemplate>
                                                <asp:Label ID="lblMessageDemat" runat="server"
                                                    Text='<%# Eval("Message") %>' ToolTip='<%# Eval("Message") %>'>
                                                </asp:Label>
                                            </ItemTemplate>
                                        </telerik:GridTemplateColumn>
                                    </Columns>
                                    <NestedViewTemplate>
                                        <div
                                            style="display:flex; width:100%; height:600px; padding: 10px; background-color:#fafafa; border-bottom:1px solid #ddd;">
                                            <div style="flex:1; overflow-y:auto; padding-right:10px;">
                                                <h3 style="margin-top:0;">Lignes de prestation</h3>
                                                <telerik:RadGrid ID="rgLignesInternes" runat="server" Width="100%"
                                                    AutoGenerateColumns="False" Skin="MetroTouch"
                                                    OnNeedDataSource="rgLignesInternes_NeedDataSource"
                                                    OnItemDataBound="rgLignesInternes_ItemDataBound"
                                                    OnItemCommand="rgLignesInternes_ItemCommand">
                                                    <MasterTableView DataKeyNames="IdFacture">
                                                        <Columns>
                                                            <telerik:GridBoundColumn DataField="NumLig"
                                                                HeaderText="N° Ligne" UniqueName="NumLig">
                                                            </telerik:GridBoundColumn>
                                                            <telerik:GridBoundColumn DataField="CodePrestaFournisseur"
                                                                HeaderText="Réf. Fournisseur"
                                                                UniqueName="CodePrestaFournisseur">
                                                            </telerik:GridBoundColumn>
                                                            <telerik:GridTemplateColumn HeaderText="Code LocPro"
                                                                UniqueName="CodePrestaLP">
                                                                <ItemTemplate>
                                                                    <telerik:RadLabel ID="lblCodePrestaLPDemat"
                                                                        runat="server"
                                                                        Text='<%# Eval("CodePrestaLP") %>'>
                                                                    </telerik:RadLabel>
                                                                    <telerik:RadButton ID="btnAjouterRegleDemat"
                                                                        runat="server" Visible="false"
                                                                        CommandName="AjouterRegleLigne"
                                                                        CommandArgument='<%# Eval("NumOR") & "|" & Eval("NumFacture") & "|" & Eval("CodePrestaFournisseur") & "|" & Eval("Descr") & "|" & Eval("CodeFournisseur") %>'
                                                                        ToolTip="Ajouter une correspondance LocPro"
                                                                        ButtonType="LinkButton" Text="&#10133;"
                                                                        CssClass="btn-ajouter-regle-mini">
                                                                    </telerik:RadButton>
                                                                </ItemTemplate>
                                                            </telerik:GridTemplateColumn>
                                                            <telerik:GridBoundColumn DataField="Descr"
                                                                HeaderText="Désignation" UniqueName="Descr">
                                                            </telerik:GridBoundColumn>
                                                            <telerik:GridBoundColumn DataField="Qte" HeaderText="Qté"
                                                                UniqueName="Qte" DataFormatString="{0:N0}"
                                                                ItemStyle-HorizontalAlign="Center">
                                                            </telerik:GridBoundColumn>
                                                            <telerik:GridBoundColumn DataField="PrixUnitHT"
                                                                HeaderText="Prix Unit. HT" UniqueName="PrixUnitHT"
                                                                DataFormatString="{0:N2} €"
                                                                ItemStyle-HorizontalAlign="Left">
                                                            </telerik:GridBoundColumn>
                                                            <telerik:GridBoundColumn DataField="TauxRemise"
                                                                HeaderText="Remise %" UniqueName="TauxRemise"
                                                                DataFormatString="{0:N2} %"
                                                                ItemStyle-HorizontalAlign="Left">
                                                            </telerik:GridBoundColumn>
                                                            <telerik:GridBoundColumn DataField="MontantNetHT"
                                                                HeaderText="Montant HT" UniqueName="MontantNetHT"
                                                                DataFormatString="{0:N2} €"
                                                                ItemStyle-HorizontalAlign="Left">
                                                            </telerik:GridBoundColumn>
                                                            <telerik:GridBoundColumn DataField="TVA" HeaderText="TVA %"
                                                                UniqueName="TVA" DataFormatString="{0:N2} %"
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
                                    <telerik:RadGrid ID="rgFacturesDematHistorique" runat="server" AutoGenerateColumns="False" AllowPaging="True"
                                        PageSize="20" Skin="MetroTouch" CssClass="factures-grid"
                                        OnNeedDataSource="rgFacturesDematHistorique_NeedDataSource"
                                        OnItemDataBound="rgFacturesDemat_ItemDataBound"
                                        OnItemCommand="rgFacturesDemat_ItemCommand">
                                <MasterTableView DataKeyNames="IdFacture" CommandItemDisplay="None"
                                    HierarchyLoadMode="Client" RetainExpandStateOnRebind="true">
                                    <Columns>
                                        <telerik:GridTemplateColumn HeaderText="Statut" UniqueName="Statut"
                                            DataField="Statut" SortExpression="Statut">
                                            <ItemTemplate>
                                                <asp:Label ID="lblStatutDemat" runat="server"
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
                                            UniqueName="FournisseurDemat" DataField="RaisonSociale"
                                            SortExpression="RaisonSociale" HeaderStyle-Width="250px">
                                            <ItemTemplate>
                                                <asp:Panel ID="pnlFournisseurDemat" runat="server">
                                                    <div style="white-space: nowrap;">
                                                        <asp:TextBox ID="txtSiretDemat" runat="server" Visible="false"
                                                            MaxLength="14" CssClass="textbox-siret-custom"
                                                            Style="vertical-align: middle;" placeholder="14 chiffres">
                                                        </asp:TextBox>
                                                        <telerik:RadButton ID="btnValiderSiretDemat" runat="server"
                                                            Visible="false" ButtonType="StandardButton"
                                                            ToolTip="Valider le SIRET" CommandName="ValidateSiret"
                                                            CommandArgument='<%# Eval("IdFacture") %>'
                                                            Style="vertical-align: middle; margin-left: 4px; "
                                                            CssClass="btn-valider">
                                                            <Icon PrimaryIconCssClass="rbOk" />
                                                        </telerik:RadButton>
                                                    </div>

                                                    <asp:Label ID="lblFournisseurDemat" runat="server"
                                                        Text='<%# Eval("RaisonSociale") %>'
                                                        CssClass="label-fournisseur">
                                                    </asp:Label>
                                                    <div style="margin-top: 5px;">
                                                        <asp:Label ID="lblSiretMsg" runat="server" Visible="false"
                                                            Font-Size="11px" Font-Bold="true"></asp:Label>
                                                    </div>
                                                </asp:Panel>
                                            </ItemTemplate>
                                        </telerik:GridTemplateColumn>

                                        <telerik:GridBoundColumn DataField="TotalHT" HeaderText="Total HT"
                                            UniqueName="TotalHT" DataFormatString="{0:N2} €"
                                            ItemStyle-HorizontalAlign="Left" HeaderStyle-HorizontalAlign="Left"
                                            ItemStyle-CssClass="nowrap"></telerik:GridBoundColumn>
                                        <telerik:GridBoundColumn DataField="TotalTTC" HeaderText="Total TTC"
                                            UniqueName="TotalTTC" DataFormatString="{0:N2} €"
                                            ItemStyle-HorizontalAlign="Left" HeaderStyle-HorizontalAlign="Left"
                                            ItemStyle-CssClass="nowrap"></telerik:GridBoundColumn>
                                        <telerik:GridBoundColumn DataField="NumOR" HeaderText="N° OR"
                                            UniqueName="NumOR"></telerik:GridBoundColumn>
                                        <telerik:GridTemplateColumn UniqueName="StatutCycleDeVie"
                                            SortExpression="StatutCycleDeVie" HeaderStyle-Width="180px">
                                            <HeaderTemplate>
                                                Cycle de Vie (Maileva)
                                                <span class="tooltip-cycle">
                                                    <i class="info-icon">i</i>
                                                    <span class="tooltiptext">
                                                        <b>Prise en charge :</b> En cours de traitement<br />
                                                        <b>Suspendue :</b> Erreur côté fournisseur. En attente de sa correction<br />
                                                        <b>Refusée :</b> Facture définitivement rejetée (ex: doublon)<br />
                                                        <b>Approuvée partiel. :</b> Intégrée avec des réserves<br />
                                                        <b>Paiement Transmis :</b> Intégrée avec succès, paiement acté<br />
                                                        <b>En litige :</b> Désaccord, en attente d'une action ou d'un avoir
                                                    </span>
                                                </span>
                                            </HeaderTemplate>
                                            <ItemTemplate>
                                                <telerik:RadDropDownList ID="ddlStatutCycleDeVie" runat="server"
                                                    AutoPostBack="true"
                                                    OnSelectedIndexChanged="ddlStatutCycleDeVie_SelectedIndexChanged"
                                                    SelectedValue='<%# If(IsDBNull(Eval("StatutCycleDeVie")) OrElse String.IsNullOrEmpty(Eval("StatutCycleDeVie").ToString()), "IN_PROCESS", Eval("StatutCycleDeVie").ToString().Trim().ToUpper().Replace(" ", "_").Replace("SUSPENDED", "ON_HOLD")) %>'>
                                                    <Items>
                                                        <telerik:DropDownListItem Text="Prise en charge"
                                                            Value="IN_PROCESS" />
                                                        <telerik:DropDownListItem Text="Suspendue" Value="ON_HOLD" />
                                                        <telerik:DropDownListItem Text="Refusée" Value="REFUSED" />
                                                        <telerik:DropDownListItem Text="Approuvée Partiellement"
                                                            Value="CONDITIONNALY_ACCEPTED" />
                                                        <telerik:DropDownListItem Text="Paiement Transmis"
                                                            Value="PAID" />
                                                        <telerik:DropDownListItem Text="En litige"
                                                            Value="UNDER_QUERY" />
                                                    </Items>
                                                </telerik:RadDropDownList>
                                                <asp:HiddenField ID="hdnIdFactureCycle" runat="server"
                                                    Value='<%# Eval("IdFacture") %>' />
                                            </ItemTemplate>
                                        </telerik:GridTemplateColumn>
                                        <telerik:GridTemplateColumn HeaderText="PDF" UniqueName="VisualiserPDF"
                                            ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center">
                                            <ItemTemplate>
                                                <a href='DownloadPdf.ashx?id=<%# Eval("IdFacture") %>' target="_blank"
                                                    style="text-decoration:none; font-size:20px;"
                                                    title="Visualiser la facture (PDF)">
                                                    &#128196;
                                                </a>
                                            </ItemTemplate>
                                        </telerik:GridTemplateColumn>
                                        <telerik:GridTemplateColumn HeaderText="Message" UniqueName="Message">
                                            <ItemTemplate>
                                                <asp:Label ID="lblMessageDemat" runat="server"
                                                    Text='<%# Eval("Message") %>' ToolTip='<%# Eval("Message") %>'>
                                                </asp:Label>
                                            </ItemTemplate>
                                        </telerik:GridTemplateColumn>
                                    </Columns>
                                    <NestedViewTemplate>
                                        <div
                                            style="display:flex; width:100%; height:600px; padding: 10px; background-color:#fafafa; border-bottom:1px solid #ddd;">
                                            <div style="flex:1; overflow-y:auto; padding-right:10px;">
                                                <h3 style="margin-top:0;">Lignes de prestation</h3>
                                                <telerik:RadGrid ID="rgLignesInternes" runat="server" Width="100%"
                                                    AutoGenerateColumns="False" Skin="MetroTouch"
                                                    OnNeedDataSource="rgLignesInternes_NeedDataSource"
                                                    OnItemDataBound="rgLignesInternes_ItemDataBound"
                                                    OnItemCommand="rgLignesInternes_ItemCommand">
                                                    <MasterTableView DataKeyNames="IdFacture">
                                                        <Columns>
                                                            <telerik:GridBoundColumn DataField="NumLig"
                                                                HeaderText="N° Ligne" UniqueName="NumLig">
                                                            </telerik:GridBoundColumn>
                                                            <telerik:GridBoundColumn DataField="CodePrestaFournisseur"
                                                                HeaderText="Réf. Fournisseur"
                                                                UniqueName="CodePrestaFournisseur">
                                                            </telerik:GridBoundColumn>
                                                            <telerik:GridTemplateColumn HeaderText="Code LocPro"
                                                                UniqueName="CodePrestaLP">
                                                                <ItemTemplate>
                                                                    <telerik:RadLabel ID="lblCodePrestaLPDemat"
                                                                        runat="server"
                                                                        Text='<%# Eval("CodePrestaLP") %>'>
                                                                    </telerik:RadLabel>
                                                                    <telerik:RadButton ID="btnAjouterRegleDemat"
                                                                        runat="server" Visible="false"
                                                                        CommandName="AjouterRegleLigne"
                                                                        CommandArgument='<%# Eval("NumOR") & "|" & Eval("NumFacture") & "|" & Eval("CodePrestaFournisseur") & "|" & Eval("Descr") & "|" & Eval("CodeFournisseur") %>'
                                                                        ToolTip="Ajouter une correspondance LocPro"
                                                                        ButtonType="LinkButton" Text="&#10133;"
                                                                        CssClass="btn-ajouter-regle-mini">
                                                                    </telerik:RadButton>
                                                                </ItemTemplate>
                                                            </telerik:GridTemplateColumn>
                                                            <telerik:GridBoundColumn DataField="Descr"
                                                                HeaderText="Désignation" UniqueName="Descr">
                                                            </telerik:GridBoundColumn>
                                                            <telerik:GridBoundColumn DataField="Qte" HeaderText="Qté"
                                                                UniqueName="Qte" DataFormatString="{0:N0}"
                                                                ItemStyle-HorizontalAlign="Center">
                                                            </telerik:GridBoundColumn>
                                                            <telerik:GridBoundColumn DataField="PrixUnitHT"
                                                                HeaderText="Prix Unit. HT" UniqueName="PrixUnitHT"
                                                                DataFormatString="{0:N2} €"
                                                                ItemStyle-HorizontalAlign="Left">
                                                            </telerik:GridBoundColumn>
                                                            <telerik:GridBoundColumn DataField="TauxRemise"
                                                                HeaderText="Remise %" UniqueName="TauxRemise"
                                                                DataFormatString="{0:N2} %"
                                                                ItemStyle-HorizontalAlign="Left">
                                                            </telerik:GridBoundColumn>
                                                            <telerik:GridBoundColumn DataField="MontantNetHT"
                                                                HeaderText="Montant HT" UniqueName="MontantNetHT"
                                                                DataFormatString="{0:N2} €"
                                                                ItemStyle-HorizontalAlign="Left">
                                                            </telerik:GridBoundColumn>
                                                            <telerik:GridBoundColumn DataField="TVA" HeaderText="TVA %"
                                                                UniqueName="TVA" DataFormatString="{0:N2} %"
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
                                            <telerik:GridBoundColumn DataField="NumOR" HeaderText="N° OR"
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
                                                        HeaderText="Réf. Fournisseur"
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
                                                                ButtonType="LinkButton" CommandName="AjouterRegleLigne"
                                                                CommandArgument='<%# Eval("NumOR") & "|" & Eval("NumFacture") & "|" & Eval("CodePrestaFournisseur") & "|" & Eval("Descr") %>'
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


                                <telerik:RadWindow ID="rwInfoStatus" runat="server"
                                    Title="Processus et cycle de vie des factures" Width="600px" Height="450px"
                                    Modal="true" Behaviors="Close,Move" VisibleStatusbar="false" Skin="MetroTouch">
                                    <ContentTemplate>
                                        <div
                                            style="padding: 20px; font-family: 'Segoe UI', Tahoma, sans-serif; line-height: 1.6; color: #333;">
                                            <h3 style="margin-top: 0; color: #444;">Détails des statuts (Maileva)</h3>
                                            <p style="font-size: 14px; margin-bottom: 15px;">Ce diagramme explique ce
                                                que chaque statut signifie et d'où proviennent les blocages éventuels :
                                            </p>
                                            <ul style="list-style-type: none; padding: 0; font-size: 14px;">
                                                <li
                                                    style="margin-bottom: 12px; padding-left: 10px; border-left: 4px solid #007bff;">
                                                    <b style="color: #007bff;">Prise en charge :</b> La facture est
                                                    arrivée dans LocPro et est en cours de traitement. C'est le statut
                                                    d'attente normal.
                                                </li>
                                                <li
                                                    style="margin-bottom: 12px; padding-left: 10px; border-left: 4px solid #dc3545;">
                                                    <b style="color: #dc3545;">Suspendue :</b> <u>Uniquement si la faute
                                                        vient du fournisseur</u>. Ce statut le notifie qu'il doit
                                                    corriger la facture de son côté.<br /><i>Note : Ne sélectionnez pas
                                                        ce statut pour un problème purement interne à LocPro (ex:
                                                        FOURNISEUR INEXISTANT). Laissez-la en "Prise en charge".</i>
                                                </li>
                                                <li
                                                    style="margin-bottom: 12px; padding-left: 10px; border-left: 4px solid #343a40;">
                                                    <b style="color: #343a40;">Refusée :</b> La facture est
                                                    définitivement rejetée . Le fournisseur sait qu'elle ne sera pas
                                                    payée.
                                                </li>
                                                <li
                                                    style="margin-bottom: 12px; padding-left: 10px; border-left: 4px solid #17a2b8;">
                                                    <b style="color: #17a2b8;">Approuvée partiel. :</b> Intégrée, mais
                                                    avec des réserves de la part du système.
                                                </li>
                                                <li
                                                    style="margin-bottom: 12px; padding-left: 10px; border-left: 4px solid #28a745;">
                                                    <b style="color: #28a745;">Paiement Transmis :</b> C'est le succès.
                                                    Une fois qu'on clique sur "Ré-intégrer tout", et que la facture
                                                    s'est intégrée
                                                    dans LocPro sans erreur. Elle sera considérée comme payée et le
                                                    fournisseur est notifié que le paiement est
                                                    acté.
                                                </li>
                                                <li
                                                    style="margin-bottom: 12px; padding-left: 10px; border-left: 4px solid #fd7e14;">
                                                    <b style="color: #fd7e14;">En litige :</b> Désaccord commercial avec
                                                    le fournisseur (ex: quantité ou montant incorrect). La facture n'est
                                                    pas intégrée.
                                                </li>
                                            </ul>
                                        </div>
                                    </ContentTemplate>
                                </telerik:RadWindow>

                                <!-- RadWindow pour le formulaire de correspondance -->
                                <telerik:RadWindow ID="rwFormulaireCorrespondance" runat="server"
                                    Title="Ajouter une correspondance prestation" Width="900px" Height="800px"
                                    Modal="true" Behaviors="Close,Move" VisibleStatusbar="false" Skin="MetroTouch"
                                    OnClientClose="refreshRadGrid">
                                </telerik:RadWindow>
                                <telerik:RadCodeBlock runat="server">
                                    <script>
                                        function showInfoStatus() {
                                            var win = $find("<%= rwInfoStatus.ClientID %>");
                                            if (win) {
                                                win.show();
                                            }
                                        }

                                        function refreshRadGrid() {
                                            var grid = $find("<%= rgHistoriqueFactures.ClientID %>");
                                            if (grid) {
                                                console.log("Rafraîchissement du RadGrid (Historique)...");
                                                grid.get_masterTableView().rebind();
                                            }

                                            var gridDemat = $find("<%= rgFacturesDemat.ClientID %>");
                                            if (gridDemat) {
                                                console.log("Rafraîchissement du RadGrid (Demat)...");
                                                gridDemat.get_masterTableView().rebind();
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