﻿const listenAllEvents = (selectorPrefix: string = String()) => {
    selectorPrefix += " "
    
    $(selectorPrefix + ".selectpicker").selectpicker({
        iconBase: bootstrapVersion === 5 ? 'fa' : 'glyphicon'
    });
    
    if(bootstrapVersion === 3){
        $(selectorPrefix + "input[type=checkbox][data-toggle^=toggle]").bootstrapToggle();
    }
    
    CalendarListener.listen(selectorPrefix);
    TextAreaListener.listenKeydown(selectorPrefix);
    SearchBoxListener.listenTypeahead(selectorPrefix);
    LookupListener.listenChanges(selectorPrefix);
    SortableListener.listenSorting(selectorPrefix);
    UploadAreaListener.listenFileUpload(selectorPrefix);
    TabNavListener.listenTabNavs(selectorPrefix);
    SliderListener.listenSliders(selectorPrefix);
    SliderListener.listenInputs(selectorPrefix);
    
    //@ts-ignore
    Inputmask().mask(document.querySelectorAll("input"));
    
    if(bootstrapVersion === 5){
        TooltipListener.listen(selectorPrefix)
    }else{
        $(selectorPrefix + '[data-toggle="tooltip"]').tooltip();
    }
    
    document.querySelectorAll(selectorPrefix + ".jj-numeric").forEach(applyDecimalPlaces)
    
    document.querySelector("form").addEventListener("submit", function (event) {
        let isValid: boolean;

        if (typeof this.reportValidity === "function") {
            isValid = this.reportValidity();
        } else {
            isValid = true;
        }

        if (isValid && showSpinnerOnPost) {
            setTimeout(function () {
                SpinnerOverlay.show();
            }, 1);
        }
    });
};   
