<%@ Page Language="VB" AutoEventWireup="false" CodeFile="formulaireCorrespondancePopup.aspx.vb" Inherits="formulaireCorrespondancePopup" %>
<%@ Register Src="~/ControlesUtilisateur/FormulaireCorrespondance.ascx" TagName="FormulaireCorrespondance" TagPrefix="uc" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Formulaire Correspondance</title>
    <link href="styles/form_prestation.css" rel="stylesheet" />
    <script type="text/javascript" src="scripts/script_validation_form.js"></script>
    <style>
        body {
            margin: 0;
            padding: 0;
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
        }
    </style>
    <script type="text/javascript">
        // Fonction pour fermer la popup
        function GetRadWindow() {
            var oWindow = null;
            if (window.radWindow) oWindow = window.radWindow;
            else if (window.frameElement.radWindow) oWindow = window.frameElement.radWindow;
            return oWindow;
        }
        
        function closeWindow() {
            GetRadWindow().close();
        }
    </script>
</head>
<body>
    <form id="form1" runat="server">
        <telerik:RadScriptManager ID="RadScriptManager1" runat="server"></telerik:RadScriptManager>
        <telerik:RadWindowManager ID="RadWindowManager1" runat="server" Skin="MetroTouch">
            <Localization OK="Oui" Cancel="Non" />
        </telerik:RadWindowManager>
        <!-- Contrôle utilisateur -->
        <uc:FormulaireCorrespondance ID="formulaire1" runat="server" />
    </form>
</body>
</html>