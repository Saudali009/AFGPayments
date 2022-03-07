function downloadNACHAFile() {
    console.log("Trying to download NACHA file for current Batch.");
    try {
        Xrm.Utility.showProgressIndicator("Downloading NACHA file. Please Wait..");
        var annotationId = Xrm.Page.data.entity.getId().replace('{', '').replace('}', '');
        getDocumentDetails(annotationId);
    }

    catch (error) {
        Xrm.Utility.closeProgressIndicator();
        console.log("Error occured while calling an action: " + error.message);
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
    console.log("Encoded Fetch XML is" + encodedFetchXML);
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
                    downloadFileInBrowser(filename, documentbody);
                } else {
                    Xrm.Utility.closeProgressIndicator();
                    Xrm.Utility.alertDialog("File corrupted, please try again.");
                }
            } else {
                Xrm.Utility.closeProgressIndicator();
                Xrm.Utility.alertDialog("Unable to find NACHA file, please create again.");
            }
            
        } else {
            Xrm.Utility.closeProgressIndicator();
            Xrm.Utility.alertDialog(req.statusText);
        }
    }
}

function downloadFileInBrowser(filename, documentbody) {
    console.log("Trying to download file in browser from details.");
    var ID = "DownloadLink";
    var a = document.createElement("a");
    a.setAttribute("download", filename)
    a.setAttribute("href", "data:text/plain;base64," + documentbody);
    a.setAttribute("style", "display:none");
    a.setAttribute("id", ID);
    document.getElementsByTagName("body")[0].append(a);
    a.click();
    a.remove();
    Xrm.Utility.closeProgressIndicator();
}