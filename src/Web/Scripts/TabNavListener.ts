class TabNavListener{
    static listenTabNavs(selectorPrefix = String()){
        $(selectorPrefix + "a.jj-tab-link").on("shown.bs.tab", function (e) {
            const link = $(e.target);
            $("#" + link.attr("jj-objectid")).val(link.attr("jj-tabindex"));
        });
    }
}