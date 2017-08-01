var ExtendedGridView = {
    currentTimeout: null,
    prevValue: null,

    autoFilter: function (val, targ) {
        if (val != this.prevValue) {
            this.prevValue = val;

            if (this.currentTimeout) {
                window.clearTimeout(this.currentTimeout);
            }

            this.currentTimeout = window.setTimeout("__doPostBack('" + targ + "', '');", 200);
        }
    },

    toggleGroupedRows: function (gvr, hideonly) {
        var row = document.getElementById(gvr);
        var allgrouped = row.parentNode.querySelectorAll("[data-exgroup='" + gvr + "']")
        
        for (var i = 0; i < allgrouped.length; i++) {
            var next = allgrouped[i];
            if (next.style.display != "none") {
                if (next.getAttribute("data-exgrouphead") !== null) { this.toggleGroupedRows(next.id, true); }
                next.style.display = "none";
            } else if (!hideonly) {
                next.style.display = "";
            }
        }
    },

    createCopy: function (table, tpart, gridId, tablesuffix, tableid, cloneid) {
        var scrollingTable = table.cloneNode(false);
        window[gridId + tablesuffix] = scrollingTable;
        scrollingTable.id = gridId + tableid;
        scrollingTable.style.backgroundColor = "white";
        scrollingTable.style.position = "fixed";
        scrollingTable.style.top = "0px";
        scrollingTable.style.height = tpart.offsetHeight + "px";

        //Clone the thead from the original table
        var tpartclone = tpart.cloneNode(true);
        tpartclone.id = gridId + cloneid;

        //Add the created table to the subHeader div
        scrollingTable.appendChild(tpartclone);
        table.parentNode.appendChild(scrollingTable);

        return scrollingTable;
    },

    scrollHeaderAndFooter: function (gridId, forceres) {
        var firstRow = document.getElementById(gridId + "_firstHeaderRow");
        var firstFooterRow = document.getElementById(gridId + "_firstFooterRow");
        var computedTopOffset = window[gridId + "_compTopOffset"];
        var computedBottomOffset = window[gridId + "_compBottomOffset"];

        if (!firstRow) return;

        var thead = firstRow.parentNode;
        var tfoot = firstFooterRow ? firstFooterRow.parentNode : null;

        var table = thead.parentNode;

        var scrollingTable = window[gridId + "_scrollingTable"];
        var scrollingTableFooter = window[gridId + "_scrollingTableFooter"];

        //Create the clones for the scrolling header / footer
        if (!scrollingTable) {
            scrollingTable = this.createCopy(table, thead, gridId, "_scrollingTable", "_tableHeader", "_header_clone");
            scrollingTable.className = "ExtendedGridViewHeaderClone";
            scrollingTable.style.display = "none";
            scrollingTable.style.top = "0px";

            if (tfoot) {
                scrollingTableFooter = this.createCopy(table, tfoot, gridId, "_scrollingTableFooter", "_tableFooter", "_footer_clone");
                scrollingTableFooter.className = "ExtendedGridViewFooterClone";
                scrollingTableFooter.style.display = "none";
                scrollingTableFooter.style.top = "";
            }
        }

        // Handles the clones positioning (if needed)
        var tst = table.getBoundingClientRect();
        var scrollParentTop = 0;
        var scrollParentBottom = document.documentElement.clientHeight;

        if (window.top != window) {
            parentpos = (document.parentWindow ? document.parentWindow : document.defaultView).frameElement.getBoundingClientRect();
            scrollParentTop = -parentpos.top;
            scrollParentBottom = window.top.document.documentElement.clientHeight - parentpos.top;
        }

        if (computedTopOffset) {
            scrollParentTop += computedTopOffset;
        }

        if (computedBottomOffset) {
            scrollParentBottom -= computedBottomOffset;
        }

        if (scrollingTableFooter) {
            scrollingTableFooter.style.bottom = (computedBottomOffset || 0) + "px";
        }

        if (tst.top < scrollParentTop && tst.bottom - thead.offsetHeight > scrollParentTop) {
            this.resizeHeader(gridId, gridId + "_firstHeaderRow", gridId + "_header_clone", forceres);
            scrollingTable.style.display = "";
            if (scrollParentTop) {
                scrollingTable.style.top = scrollParentTop + "px";
            } else {
                scrollingTable.style.left = tst.left + "px";
            }
        } else {
            scrollingTable.style.display = "none";
        }

        if (tfoot && scrollingTableFooter) {
            this.resizeHeader(gridId, gridId + "_firstFooterRow", gridId + "_footer_clone", forceres);
            if (tst.bottom > scrollParentBottom && tst.top + tfoot.offsetHeight + thead.offsetHeight < scrollParentBottom) {
                scrollingTableFooter.style.display = "";
                if (window.top != window) {
                    scrollingTableFooter.style.top = scrollParentBottom + (computedBottomOffset || 0) - tfoot.offsetHeight + "px"; //  document.documentElement.clientHeight - parentBottom + "px";
                } else {
                    scrollingTableFooter.style.left = tst.left + "px";
                }
            } else {
                scrollingTableFooter.style.display = "none";
            }
        }
    },

    resizeHeader: function (gridId, firstRowId, cloneId, forceres) {
        var firstRow = document.getElementById(firstRowId);
        var thead = document.getElementById(cloneId);

        if (!thead) {// || (thead.sizeset && !forceres)) {
            return;
        }

        thead.sizeset = true;
        thead.style.display = "none";
        var theadHeader = firstRow.parentNode;
        try {
            thead.parentNode.style.minWidth = "unset";
        } catch (e) { };

        for (var j = 0; j < theadHeader.children.length; j++) {
            for (var i = 0; i < theadHeader.children[j].cells.length; i++) {
                var src = theadHeader.children[j].cells[i];
                var cl = thead.children[j].cells[i];

                cl.style.boxSizing = "border-box";
                cl.style.width = Math.max(src.offsetWidth, 0) + "px";
                cl.style.minWidth = cl.style.width;

                cl.style.height = Math.max(src.offsetHeight, 0) + "px";
                cl.style.minHeight = cl.style.height;
            }
        }
        thead.style.display = "";
    },

    ClearFields: function () {
        var allFilters = document.getElementsByClassName("ExtendedGridViewFilter");
        for (var i = 0; i < allFilters.length; i++) {
            allFilters[i].value = "";
        }

        //this.autoFilter("", allFilters[0]);
    }
};