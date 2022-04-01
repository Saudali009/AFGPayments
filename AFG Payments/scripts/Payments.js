function onSave(executionContext) {
    try {
        var formContext = executionContext.getFormContext();
        var formType = formContext.ui.getFormType();
        onchangeOfOpportunity(executionContext);
    }
    catch (ex) {
        console.log("Error has occured while saving" + ex.message);
    }
}

function onchangeOfOpportunity(executionContext) {
    try {
        var formContext = executionContext.getFormContext();
        var opportunity = formContext.getAttribute("afg_opportunity");
        if (opportunity != null) {
            var opportunityId = opportunity.getValue()[0].id;
            opportunityId = opportunityId.replace("{", "").replace("}", "");
            var distcode = getAccountFromOpportunity(opportunityId);
            if (distcode != null) {
                //setLookupValue(formContext, "account", "afg_account", account);
                formContext.getAttribute("afg_paymentdistcode").setValue(distcode);
            }
        }
    }
    catch (ex) {
        console.log("There is an exception: " + ex.message);
    }
}

function getAccountFromOpportunity(opportunityId) {
    var req = new XMLHttpRequest();
    req.open("GET", Xrm.Page.context.getClientUrl() + "/api/data/v9.1/opportunities(" + opportunityId + ")?$select=_accountid_value,name,afg_lplusdistcode", false);
    req.setRequestHeader("OData-MaxVersion", "4.0");
    req.setRequestHeader("OData-Version", "4.0");
    req.setRequestHeader("Accept", "application/json");
    req.setRequestHeader("Content-Type", "application/json; charset=utf-8");
    req.setRequestHeader("Prefer", "odata.include-annotations=\"*\"");
    req.send();
    if (req.readyState === 4) {
        req.onreadystatechange = null;
        if (req.status === 200) {
            var result = JSON.parse(req.response);
            var distcode = result["afg_lplusdistcode"];
            //var accountId = result["_accountid_value"];
            //var accountName = result["_accountid_value@OData.Community.Display.V1.FormattedValue"];
            //const accountDetails = {};
            //accountDetails.id = accountId;
            //accountDetails.name = accountName;
            //accountDetails.distcode = distcode;
            return distcode;
        } else {
            console.log("Error occured while get account from opportunity" + req.statusText);
        }
    }
}

function setLookupValue(formContext, entityname, fieldName, value) {
    var lookupValue = new Array();
    lookupValue[0] = new Object();
    lookupValue[0].id = value.id;
    lookupValue[0].name = value.name;
    lookupValue[0].entityType = entityname;
    formContext.getAttribute(fieldName).setValue(lookupValue);
}