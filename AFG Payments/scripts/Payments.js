function onSave(executionContext) {
    try {
        var formContext = executionContext.getFormContext();
        var eventArgs = executionContext.getEventArgs();
        var formType = formContext.ui.getFormType();

        if ((eventArgs.getSaveMode() == 70 || eventArgs.getSaveMode() == 2 || eventArgs.getSaveMode() == 1 || eventArgs.getSaveMode() == 59)
            && (formType == 2)) {
            eventArgs.preventDefault();

            var statusReason = formContext.getAttribute("statuscode").getValue();
            if (statusReason !== 1) {
                var isPaymentProcessor = checkIfPaymentProcessor();
                if (isPaymentProcessor) {
                    Xrm.Page.data.save();
                } else {
                    eventArgs.preventDefault();
                    Xrm.Utility.alertDialog("Only Payment Processor can update status of Payment. Please contact Admin for more details.");
                }
            } else {
                console.log("You are good to go");
                Xrm.Page.data.save();
            }
        }
    }
    catch (ex) {
        console.log("Error has occured while saving" + ex.message);
    }
}

function onload(executionContext) {
    try {
        var formContext = executionContext.getFormContext();
        if (!checkIfPaymentProcessor()) {
            formContext.getControl("statuscode").setDisabled(true);
        } else {
            formContext.getControl("statuscode").setDisabled(false);
        }
    } catch (ex) {
        console.log("Error has occured while saving" + ex.message);
    }
}

function validateRoutingNumber(executionContext) {
    try {
        var formContext = executionContext.getFormContext();
        var bankRoutingCode = formContext.getAttribute("afg_bankroutingcode").getValue();

        if (bankRoutingCode != null && (bankRoutingCode.length !== 9 || !isNumber(bankRoutingCode))) {
            formContext.getControl("afg_bankroutingcode").setNotification("Code must be 9 digits without any special characters.", "ROUTING_NUM_NOTIF");
            return false;
        }
        else {
            formContext.getControl("afg_bankroutingcode").clearNotification("ROUTING_NUM_NOTIF");
        }
    }
    catch (ex) {
        console.log("There is an exception: " + ex.message);
    }
}

function validateBankAccountNumber(executionContext) {
    var formContext = executionContext.getFormContext();
    var accountNumber = formContext.getAttribute("afg_bankaccountnumber").getValue();
    if (accountNumber.match(/\W/)) {
        formContext.getControl("afg_bankaccountnumber").setNotification("Special characters are not allowed! Please enter valid Account.", "ACCOUNT_NUM_NOTIF");
    }
    else {
        formContext.getControl("afg_bankaccountnumber").clearNotification("ACCOUNT_NUM_NOTIF");
    }
}

function isNumber(n) {
    return /^-?[\d.]+(?:e-?\d+)?$/.test(n);
}

function checkIfPaymentProcessor() {
    let roles = Xrm.Utility.getGlobalContext().userSettings.roles;
    roles.forEach(role => {
        if (role.name === "AFG Payment Processor" || role.name === "System Administrator"
            || role.name === "System Customizer") {
            return true;
        }
    });
    return false;
}

function onchangeOfOpportunity(executionContext) {
    try {
        var formContext = executionContext.getFormContext();
        var opportunity = formContext.getAttribute("afg_opportunity");
        if (opportunity != null) {
            var opportunityId = opportunity.getValue()[0].id;
            opportunityId = opportunityId.replace("{","").replace("}","");
            var account = getAccountFromOpportunity(opportunityId);
            if (account != null) {
                setLookupValue(formContext, "account", "afg_account", account);
            }
        }
    }
    catch (ex) {
        console.log("There is an exception: " + ex.message);
    }
}

function getAccountFromOpportunity(opportunityId) {
    var req = new XMLHttpRequest();
    req.open("GET", Xrm.Page.context.getClientUrl() + "/api/data/v9.1/opportunities(" + opportunityId + ")?$select=_accountid_value,name", false);
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
            var accountId = result["_accountid_value"];
            var accountName = result["_accountid_value@OData.Community.Display.V1.FormattedValue"];
            const accountDetails = {};
            accountDetails.id = accountId;
            accountDetails.name = accountName;
            return accountDetails;
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