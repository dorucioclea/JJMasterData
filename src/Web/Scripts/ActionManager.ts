class ActionManager {

    static executeSqlCommand(componentName, rowId, confirmMessage) {
        if (confirmMessage) {
            var result = confirm(confirmMessage);
            if (!result) {
                return false;
            }
        }

        document.querySelector<HTMLInputElement>("#grid-view-action-" + componentName).value = "";
        document.querySelector<HTMLInputElement>("#grid-view-row-" + componentName).value = rowId;

        const formViewActionMapElement = document.querySelector<HTMLInputElement>("#form-view-action-map-" + componentName);

        if (formViewActionMapElement) {
            formViewActionMapElement.value = "";
        }

        document.querySelector("form").dispatchEvent(new Event("submit"));
    }

    static executeRedirectAction(componentName: string, routeContext: string, encryptedActionMap: string, confirmationMessage?: string) {
        if (confirmationMessage) {
            const result = confirm(confirmationMessage);
            if (!result) {
                return false;
            }
        }

        const currentFormActionInput = document.querySelector<HTMLInputElement>("#form-view-action-map-" + componentName);
        currentFormActionInput.value = encryptedActionMap;

        const urlBuilder = new UrlBuilder();
        urlBuilder.addQueryParameter("routeContext", routeContext);
        urlBuilder.addQueryParameter("componentName", componentName);

        const url = urlBuilder.build();

        this.executeUrlRedirect(url);

        return true;
    }

    private static executeUrlRedirect(url: string) {
        postFormValues({
            url: url,
            success: (data) => {
                if (data.urlAsPopUp) {
                    if(data.isIframe){
                        defaultModal.showIframe(data.urlRedirect, data.popUpTitle);
                    } 
                    else{
                        defaultModal.showUrl(data.urlRedirect, data.popUpTitle);
                    }
                } else {
                    window.location.href = data.urlRedirect;
                }
            }
        })
    }

    static executeActionData(actionData: ActionData){
        const {
            componentName,
            actionMap,
            modalTitle,
            modalRouteContext,
            gridViewRouteContext,
            formViewRouteContext,
            isSubmit,
            confirmationMessage
        } = actionData;
        
        if (confirmationMessage) {
            if (!confirm(confirmationMessage)) {
                return false;
            }
        }

        const gridViewActionInput = document.querySelector<HTMLInputElement>("#grid-view-action-" + componentName);
        const formViewActionInput = document.querySelector<HTMLInputElement>("#form-view-action-map-" + componentName);

        if (gridViewActionInput) {
            gridViewActionInput.value = "";
        }
        if (formViewActionInput) {
            formViewActionInput.value = actionMap;
        }

        let form = document.querySelector<HTMLFormElement>("form");

        if (!form) {
            return;
        }

        if (modalRouteContext) {
            const urlBuilder = new UrlBuilder();
            urlBuilder.addQueryParameter("routeContext", modalRouteContext);

            const modal = new Modal();
            modal.modalId = componentName + "-modal";

            modal.showUrl({
                url: urlBuilder.build(), requestOptions: {
                    method: "POST",
                    body: new FormData(document.querySelector("form"))
                }
            }, modalTitle).then(function (data) {
                
                listenAllEvents("#" + modal.modalId + " ")    
                
                if (typeof data === "object") {
                    if (data.closeModal) {
                        GridViewHelper.refresh(componentName,gridViewRouteContext)
                        modal.remove();
                    }
                }
            })
        } else {
            if(!isSubmit){
                const urlBuilder = new UrlBuilder();
                urlBuilder.addQueryParameter("routeContext", formViewRouteContext);

                postFormValues({url:urlBuilder.build(), success:(data)=>{
                        HTMLHelper.setInnerHTML(componentName,data);
                        listenAllEvents("#" + componentName)
                    }});
            } 
            else{
                document.forms[0].requestSubmit();
            }
        }
    }
    
    static executeAction(actionDataJson: string){
        const actionData = JSON.parse(actionDataJson);
        
        return this.executeActionData(actionData);
    }
    
    static hideActionModal(componentName:string){
        const modal = new Modal();
        modal.modalId = componentName + "-modal";
        modal.hide();
    }
}