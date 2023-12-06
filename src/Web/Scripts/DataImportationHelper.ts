﻿class DataImportationHelper {
    private static insertCount = 0;
    private static updateCount = 0;
    private static deleteCount = 0;
    private static ignoreCount = 0;
    private static errorCount = 0;

    private static pasteEventListener;
    private static intervalId;

    private static setLoadMessage() {
        const options = {
            lines: 13, // The number of lines to draw
            length: 38, // The length of each line
            width: 17, // The line thickness
            radius: 45, // The radius of the inner circle
            scale: 0.2, // Scales overall size of the spinner
            corners: 1, // Corner roundness (0..1)
            color: "#000", // #rgb or #rrggbb or array of colors
            opacity: 0.3, // Opacity of the lines
            rotate: 0, // The rotation offset
            direction: 1, // 1: clockwise, -1: counterclockwise
            speed: 1.2, // Rounds per second
            trail: 62, // Afterglow percentage
            fps: 20, // Frames per second when using setTimeout() as a fallback for CSS
            zIndex: 2e9, // The z-index (defaults to 2000000000)
            className: "spinner", // The CSS class to assign to the spinner
            top: "50%", // Top position relative to parent
            left: "50%", // Left position relative to parent
            shadow: false, // Whether to render a shadow
            hwaccel: false, // Whether to use hardware acceleration
            position: "absolute" // Element positioning

        };
        const target = document.getElementById('impSpin');
        // @ts-ignore
        new Spinner(options).spin(target);
    }

    private static checkProgress(componentName, importationRouteContext, gridRouteContext) {
        showSpinnerOnPost = false;


        let urlBuilder = new UrlBuilder()
        urlBuilder.addQueryParameter("routeContext", importationRouteContext)
        urlBuilder.addQueryParameter("dataImportationOperation", "checkProgress")
        urlBuilder.addQueryParameter("componentName", componentName)
        const url = urlBuilder.build()


        fetch(url, {
            method: 'GET',
            cache: 'no-cache',
            headers: {
                'Content-Type': 'application/json',
            },
        })
            .then(response => response.json())
            .then(result => {
                const processMessageDiv = document.querySelector<HTMLElement>("#process-status");
                if (processMessageDiv) {
                    processMessageDiv.style.display = "";
                }

                const progressBar = document.querySelector<HTMLElement>(".progress-bar");
                if (progressBar) {
                    progressBar.style.width = result.PercentProcess + "%";
                    progressBar.textContent = result.PercentProcess + "%";
                }

                const processMessage = document.querySelector<HTMLElement>("#process-message");
                if (processMessage) {
                    processMessage.textContent = result.Message;
                }

                const startDateLabel = document.querySelector<HTMLElement>("#start-date-label");
                if (startDateLabel) {
                    startDateLabel.textContent = result.StartDate;
                }
                if (result.Insert > 0) {
                    document.querySelector<HTMLElement>("#lblInsert").style.display = "";
                    if (result.PercentProcess === 100) {
                        document.querySelector("#lblInsertCount").textContent = result.Insert;
                    } else {
                        jjutil.animateValue("lblInsertCount", DataImportationHelper.insertCount, result.Insert, 1000);
                    }
                    DataImportationHelper.insertCount = result.Insert;
                }

                if (result.Update > 0) {
                    document.querySelector<HTMLElement>("#lblUpdate").style.display = "";
                    if (result.PercentProcess === 100) {
                        document.querySelector("#lblUpdateCount").textContent = result.Update;
                    } else {
                        jjutil.animateValue("lblUpdateCount", DataImportationHelper.updateCount, result.Update, 1000);
                    }
                    DataImportationHelper.updateCount = result.Update;
                }

                if (result.Delete > 0) {
                    document.querySelector<HTMLElement>("#lblDelete").style.display = "";
                    if (result.PercentProcess === 100) {
                        document.querySelector("#lblDeleteCount").textContent = result.Delete;
                    } else {
                        jjutil.animateValue("lblDeleteCount", DataImportationHelper.deleteCount, result.Delete, 1000);
                    }
                    DataImportationHelper.deleteCount = result.Delete;
                }

                if (result.Ignore > 0) {
                    document.querySelector<HTMLElement>("#lblIgnore").style.display = "";
                    if (result.PercentProcess === 100) {
                        document.querySelector("#lblIgnoreCount").textContent = result.Ignore;
                    } else {
                        jjutil.animateValue("lblIgnoreCount", DataImportationHelper.ignoreCount, result.Ignore, 1000);
                    }
                    DataImportationHelper.ignoreCount = result.Ignore;
                }

                if (result.Error > 0) {
                    document.querySelector<HTMLElement>("#lblError").style.display = "";
                    if (result.PercentProcess === 100) {
                        document.querySelector("#lblErrorCount").textContent = result.Error;
                    } else {
                        jjutil.animateValue("lblErrorCount", DataImportationHelper.errorCount, result.Error, 1000);
                    }
                    DataImportationHelper.errorCount = result.Error;
                }

                if (!result.IsProcessing) {

                    clearInterval(DataImportationHelper.intervalId)
                    let urlBuilder = new UrlBuilder();
                    urlBuilder.addQueryParameter("routeContext", importationRouteContext)
                    urlBuilder.addQueryParameter("dataImportationOperation", "log")
                    DataImportationModal.getInstance().showUrl({url: urlBuilder.build()}, "Import", ModalSize.ExtraLarge).then(_ => {
                        GridViewHelper.refreshGrid(componentName, gridRouteContext)
                    })
                }
            })
            .catch(error => {
                console.error('Error fetching data:', error);
            });
    }

    static show(componentName: string, routeContext: string, gridRouteContext: string) {
        const urlBuilder = new UrlBuilder();
        urlBuilder.addQueryParameter("routeContext", routeContext);

        DataImportationHelper.addPasteListener(componentName, routeContext, gridRouteContext);

        DataImportationModal.getInstance().showUrl({
            url: urlBuilder.build(),
            requestOptions: {method: "POST", body: new FormData(document.querySelector("form"))}
        }, "Import", ModalSize.ExtraLarge).then(_ => {
            UploadAreaListener.listenFileUpload()
        })
    }

    static showLog(componentName, routeContext) {
        const urlBuilder = new UrlBuilder();
        urlBuilder.addQueryParameter("routeContext", routeContext);
        urlBuilder.addQueryParameter("dataImportationOperation", "log");
        DataImportationModal.getInstance().showUrl({url: urlBuilder.build()}, "Import", ModalSize.ExtraLarge);
    }

    static start(componentName, routeContext, gridRouteContext) {

        DataImportationHelper.setLoadMessage();

        DataImportationHelper.intervalId = setInterval(function () {
            DataImportationHelper.checkProgress(componentName, routeContext, gridRouteContext);
        }, 3000);

    }

    static help(componentName, routeContext) {
        const urlBuilder = new UrlBuilder();
        urlBuilder.addQueryParameter("routeContext", routeContext);
        urlBuilder.addQueryParameter("dataImportationOperation", "help");
        postFormValues({
            url: urlBuilder.build(), success: html => {
                document.querySelector<HTMLInputElement>("#" + componentName).innerHTML = html;
            }
        })
    }

    static stop(componentName, routeContext, stopLabel) {
        showSpinnerOnPost = false;

        let urlBuilder = new UrlBuilder()
        urlBuilder.addQueryParameter("routeContext", routeContext)
        urlBuilder.addQueryParameter("dataImportationOperation", "checkProgress")
        urlBuilder.addQueryParameter("componentName", componentName)
        const url = urlBuilder.build()

        fetch(url).then(response => response.json()).then(data => {
            if (data.isProcessing === false) {
                document.getElementById("divMsgProcess").innerHTML = stopLabel;
            }
        });
    }

    static addPasteListener(componentName: string, routeContext: string, gridRouteContext: string) {
        DataImportationHelper.pasteEventListener = function onPaste(e) {
            DataImportationHelper.removePasteListener();
            let pastedText = undefined;
            if (window.clipboardData && window.clipboardData.getData) { // IE 
                pastedText = window.clipboardData.getData("Text");
            } else if (e.clipboardData && e.clipboardData.getData) {
                pastedText = e.clipboardData.getData("text/plain");
            }
            e.preventDefault();
            if (pastedText != undefined) {
                document.querySelector<HTMLInputElement>("#pasteValue").value = pastedText;

                let urlBuilder = new UrlBuilder();

                urlBuilder.addQueryParameter("routeContext", routeContext)
                urlBuilder.addQueryParameter("dataImportationOperation", "processPastedText")
                DataImportationModal.getInstance().showUrl({
                    url: urlBuilder.build(),
                    requestOptions: {method: "POST", body: new FormData(document.querySelector("form"))}
                }, "Import", ModalSize.Small).then(_ => {
                    DataImportationHelper.start(componentName, routeContext, gridRouteContext)
                })

            }
            return false;
        }

        document.addEventListener("paste", DataImportationHelper.pasteEventListener, {once: true});
    }

    static removePasteListener() {
        if (DataImportationHelper.pasteEventListener) {
            document.removeEventListener("paste", DataImportationHelper.pasteEventListener);
        }
    }
}
