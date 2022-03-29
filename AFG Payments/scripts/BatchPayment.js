﻿function validatePaymentTransactions(listOfSelectedPayments, commandProperties) {
    try {
        var totalPaymentCount = 0;
        var bankCode = 346380001;
        if (listOfSelectedPayments.length > 0) {
            if (commandProperties.SourceControlId.includes("CIBCBANK.Button")) {
                console.log("CIBC Clicked");
                bankCode = 346380001;
            }
            else if (commandProperties.SourceControlId.includes("FIFTHTHIRDBANK.Button")) {
                console.log("Fifth Third Clicked");
                bankCode = 346380000;
            }
            Xrm.Utility.showProgressIndicator("Creating Batch Please Wait..");
            for (let i = 0; i < listOfSelectedPayments.length; i++) {
                var payment = listOfSelectedPayments[i];
                var details = getPaymentDetails(payment);
                if (details != null) {
                    var amount = details[0];
                    if (amount != null) {
                        totalPaymentCount += amount;
                    }
                } else {
                    console.log("Unable to found payment details.");
                }                
            }
            if (totalPaymentCount < 1) {
                Xrm.Utility.closeProgressIndicator();
                Xrm.Utility.alertDialog("The total transactional amount should be greater than 0 before Batch creation.");
                return;
            }

            //Compare Total Payment Amount With Config Batch Limit
            var batchLimit = getBatchLimitAmount();
            if (totalPaymentCount > batchLimit) {
                Xrm.Utility.closeProgressIndicator();
                Xrm.Utility.alertDialog("Total Payment Transactions Amount cannot be greater than allowed Batch limit." + batchLimit);
                return;
            }

            //Create Batch for selected Payments
            createBatch(listOfSelectedPayments, totalPaymentCount, bankCode);
        } else {
            Xrm.Utility.alertDialog("Please select payment transactions before proceeding for batch creation");
        }

    } catch (ex) {
        Xrm.Utility.closeProgressIndicator();
        console.log("Error encountered while creating batch : " + ex.message);
    }
}

function getPaymentDetails(payment) {
    var req = new XMLHttpRequest();
    req.open("GET", Xrm.Page.context.getClientUrl() + "/api/data/v9.1/afg_payments(" + payment.Id + ")?$select=afg_amount,afg_paymentdistcode", false);
    req.setRequestHeader("OData-MaxVersion", "4.0");
    req.setRequestHeader("OData-Version", "4.0");
    req.setRequestHeader("Accept", "application/json");
    req.setRequestHeader("Content-Type", "application/json; charset=utf-8");
    req.setRequestHeader("Prefer", "odata.include-annotations=\"*\"");
    req.send();
    if (req.readyState === 4) {
        if (req.status === 200) {
            var result = JSON.parse(req.response);
            var paymentDistCode = result["afg_paymentdistcode"];
            var amount = result["afg_amount"];
            return [amount, paymentDistCode];
        }
    }
}

function createBatch(listOfSelectedPayments, totalPaymentCount, bankCode) {
    console.log("Trying to create Batch for payments.");
    var entity = {};
    entity.afg_name = "BATCH-" + (new Date().getTime()).toString(36).toUpperCase();
    entity.afg_batchdate = new Date();
    entity.afg_bank = bankCode;
    entity.afg_totalamount = Number(parseFloat(totalPaymentCount));
    Xrm.WebApi.online.createRecord("afg_batch", entity).then(
        function success(result) {
            console.log("Batch created successfully.");
            associatePaymentsWithBatch(result.id, listOfSelectedPayments);
        },
        function (error) {
            Xrm.Utility.closeProgressIndicator();
            Xrm.Utility.alertDialog(error.message);
        });
}

function associatePaymentsWithBatch(batchId, listOfSelectedPayment) {
    console.log("Trying to associate payments with Batch");
    for (let counter = 0; counter < listOfSelectedPayment.length; counter++) {
        var payment = listOfSelectedPayment[counter];
        var entity = {};
        entity.afg_paymentstatus = 346380003;
        entity["afg_batch@odata.bind"] = "/afg_batchs(" + batchId + ")";
        Xrm.WebApi.online.updateRecord("afg_payment", payment.Id, entity).then(
            function success(result) {
                console.log("Payment Updated:" + result.id);
            },
            function (error) {
                Xrm.Utility.alertDialog(error.message);
            });
    }
    Xrm.Utility.closeProgressIndicator();
    Xrm.Utility.openEntityForm("afg_batch", batchId);
}

function getBatchLimitAmount() {
    var req = new XMLHttpRequest();
    req.open("GET", Xrm.Page.context.getClientUrl() + "/api/data/v9.1/afg_batchconfigs?$select=afg_totalamountlimit&$orderby=createdon desc", false);
    req.setRequestHeader("OData-MaxVersion", "4.0");
    req.setRequestHeader("OData-Version", "4.0");
    req.setRequestHeader("Accept", "application/json");
    req.setRequestHeader("Content-Type", "application/json; charset=utf-8");
    req.setRequestHeader("Prefer", "odata.include-annotations=\"*\",odata.maxpagesize=1");
    req.send();
    if (req.readyState === 4) {
        if (req.status === 200) {
            var results = JSON.parse(req.response);
            if (results.value.length > 0) {
                return results.value[0]["afg_totalamountlimit"];
            }
            return -1;
        } else {
            console.log(req.statusText);
        }
    }
}
