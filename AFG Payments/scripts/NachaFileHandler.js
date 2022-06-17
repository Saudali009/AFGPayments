function downloadNACHAFile() {    
    try {
        Xrm.Utility.showProgressIndicator("Downloading NACHA File Please Wait..");
        var batchId = Xrm.Page.data.entity.getId().replace('{', '').replace('}', '');
        getDocumentDetails(batchId);
        createNACHADownloadLog(batchId, 1);
    }
    catch (error) {
        createNACHADownloadLog(batchId, 2);
        console.log("Error occured while calling an action: " + error.message);
        Xrm.Utility.closeProgressIndicator();
    }
}

function getDocumentDetails(annotationId) {
    var fetchXML = "<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>" +
        "<entity name='annotation'>" +
        "<attribute name='documentbody' />" +
        "<attribute name='filename' />" +
        "<order attribute='createdon' descending='true' />" +
        "<link-entity name='afg_batch' from='afg_batchid' to='objectid' link-type='inner' alias='ac'>" +
        "<filter type='and'>" +
        "<condition attribute='afg_batchid' operator='eq' value='" + annotationId + "' />" +
        "</filter>" +
        "</link-entity>" +
        "</entity>" +
        "</fetch>";

    var encodedFetchXML = encodeURIComponent(fetchXML);
    var req = new XMLHttpRequest();
    req.open("GET", Xrm.Page.context.getClientUrl() + "/api/data/v9.1/annotations?fetchXml=" + encodedFetchXML, false);
    req.setRequestHeader("OData-MaxVersion", "4.0");
    req.setRequestHeader("OData-Version", "4.0");
    req.setRequestHeader("Accept", "application/json");
    req.setRequestHeader("Content-Type", "application/json; charset=utf-8");
    req.setRequestHeader("Prefer", "odata.include-annotations=\"*\"");
    req.send();
    if (req.readyState === 4) {
        if (req.status === 200) {
            var result = JSON.parse(req.response);
            if (result.value.length > 0) {
                var documentbody = result.value[0]["documentbody"];
                var filename = result.value[0]["filename"];
                if (documentbody != null && filename != null) {
                    Xrm.Utility.closeProgressIndicator();
                    downloadFileInBrowser(filename, documentbody);
                } else {
                    createNACHADownloadLog(batchId, 2);
                    Xrm.Utility.closeProgressIndicator();
                    Xrm.Utility.alertDialog("File corrupted, please try again.");
                }
            } else {
                createNACHADownloadLog(batchId, 2);
                Xrm.Utility.closeProgressIndicator();
                Xrm.Utility.alertDialog("Unable to find NACHA file, please create again.");
            }

        } else {
            createNACHADownloadLog(batchId, 2);
            Xrm.Utility.closeProgressIndicator();
            Xrm.Utility.alertDialog(req.statusText);
        }
    }
}

function downloadFileInBrowser(filename, documentbody) {
    var ID = "DownloadLink";
    var a = document.createElement("a");
    a.setAttribute("download", filename)
    a.setAttribute("href", "data:text/plain;base64," + documentbody);
    a.setAttribute("style", "display:none");
    a.setAttribute("id", ID);
    document.getElementsByTagName("body")[0].append(a);
    a.click();
    a.remove();
}

function createNACHADownloadLog(batchId, status) {
    console.log("Trying to create download file log");
    var entity = {};
    entity.afg_accesseddate = new Date().toISOString();
    entity["afg_Batch@odata.bind"] = "/afg_batchs(" + batchId + ")";
    entity.afg_downloadstatus = status;

    Xrm.WebApi.online.createRecord("afg_nachadownloadtracking", entity).then(
        function success(result) {
            console.log("Log created successfully!");
        },
        function (error) {
            console.log("Exception occured while creating a log" + error.message);
            Xrm.Utility.closeProgressIndicator();
        }
    );
}