/* =========================================================
   ASRS MONITOR
   ========================================================= */


/* =========================================================
   GLOBAL VARIABLES
   ========================================================= */

let selectedLevel = 8;

let asrsData = [];

let zoneData = [];

const TOTAL_COLUMNS = 44;

const DEFAULT_ROWS = 18;


/* =========================================================
   BOOKMYSHOW STYLE MAGNIFYING GLASS
   ========================================================= */

const ZOOM_COLUMNS = 5;

const ZOOM_ROWS = 3;


/* =========================================================
   MOVEMENT ZONE EXCLUDED STATUSES
   ========================================================= */

/*
 * Movement-zone colors must NOT be applied
 * to these location statuses.
 */

const EXCLUDED_ZONE_STATUSES = [
    "PARENTROADWAY",
    "EMPTY",
    "ROADWAY",
    "CACHEROADWAY",
    "BLOCKED",
    "ISOLATION",
    "LIFT"
];


/* =========================================================
   LOADING SIMULATION
   ========================================================= */

let loadingSimulationRunning = false;

let loadingSimulationTimer = null;

let simulationPalletElement = null;

let loadingSimulationData = [];

let loadingSimulationIndex = 0;


/* =========================================================
   FG CODE COLORS
   ========================================================= */

const fgColorMap = new Map();

let fgColorIndex = 0;


/*
 * Generate visually distinct colors.
 *
 * Each new FGCode gets the next color.
 * Same FGCode always keeps the same color
 * during the current page session.
 */
function getFGColor(fgCode) {

    if (!fgCode) {
        return null;
    }

    fgCode = String(fgCode)
        .trim()
        .toUpperCase();

    if (fgCode === "") {
        return null;
    }


    /*
     * Already assigned?
     */
    if (fgColorMap.has(fgCode)) {

        return fgColorMap.get(fgCode);

    }


    /*
     * Golden-angle distribution.
     *
     * This spreads colors around the entire
     * color wheel instead of creating similar
     * colors next to each other.
     */
    const goldenAngle = 137.508;

    const hue =
        Math.round(
            (fgColorIndex * goldenAngle) % 360
        );


    /*
     * Keep saturation/lightness controlled
     * so pallet text/icon remains visible.
     */
    const color =
        "hsl(" +
        hue +
        ", 70%, 50%)";


    fgColorMap.set(
        fgCode,
        color
    );

    fgColorIndex++;

    return color;
}


/* =========================================================
   DOCUMENT READY
   ========================================================= */

document.addEventListener(
    "DOMContentLoaded",
    function () {

        console.log(
            "ASRS Monitor initialized"
        );


        /* =================================================
           LEVEL SELECTOR
           ================================================= */

        const selector =
            document.getElementById(
                "levelSelector"
            );


        if (!selector) {

            console.error(
                "levelSelector not found"
            );

            return;

        }


        selectedLevel =
            parseInt(
                selector.value,
                10
            ) || 8;


        selector.addEventListener(
            "change",
            function () {

                selectedLevel =
                    parseInt(
                        this.value,
                        10
                    ) || 8;


                const mapTitle =
                    document.getElementById(
                        "mapTitle"
                    );


                if (mapTitle) {

                    mapTitle.innerText =
                        "Level " +
                        selectedLevel +
                        " — Top View";

                }


                hideZoomPreview();

                loadASRS();

            }
        );


        /* =================================================
           SEARCH
           ================================================= */

        const searchBox =
            document.getElementById(
                "locationSearch"
            );


        if (searchBox) {

            searchBox.addEventListener(
                "input",
                function () {

                    filterLocations(
                        this.value
                    );

                }
            );

        }


        /* =================================================
           INITIAL LOAD
           ================================================= */

        loadASRS();


        /* =================================================
           AUTO REFRESH
           ================================================= */

        setInterval(
            function () {

                /*
                 * Do not refresh while loading
                 * simulation is running.
                 */

                if (
                    !loadingSimulationRunning
                ) {

                    loadASRS();

                }

            },
            5000
        );


        /* =================================================
           WINDOW RESIZE
           ================================================= */

        window.addEventListener(
            "resize",
            function () {

                hideZoomPreview();

            }
        );

    }
);


/* =========================================================
   LOAD ASRS DATA
   ========================================================= */

function loadASRS() {

    /*
     * Check URLs
     */

    if (
        !window.ASRS_URLS
    ) {

        console.error(
            "ASRS_URLS not found."
        );

        return;

    }


    if (
        !window.ASRS_URLS.getLevel ||
        !window.ASRS_URLS.getSummary ||
        !window.ASRS_URLS.getZones
    ) {

        console.error(
            "ASRS API URLs are missing.",
            window.ASRS_URLS
        );

        return;

    }


    /*
     * =====================================================
     * API REQUESTS
     * =====================================================
     */

    Promise.all([

        fetch(
            window.ASRS_URLS.getLevel +
            "?level=" +
            encodeURIComponent(
                selectedLevel
            ),
            {
                method: "GET",
                cache: "no-store"
            }
        )
            .then(
                handleFetchResponse
            ),


        fetch(
            window.ASRS_URLS.getSummary +
            "?level=" +
            encodeURIComponent(
                selectedLevel
            ),
            {
                method: "GET",
                cache: "no-store"
            }
        )
            .then(
                handleFetchResponse
            ),


        fetch(
            window.ASRS_URLS.getZones,
            {
                method: "GET",
                cache: "no-store"
            }
        )
            .then(
                handleFetchResponse
            )

    ])
        .then(
            function (results) {

                const levelResult =
                    results[0];

                const summaryResult =
                    results[1];

                const zoneResult =
                    results[2];


                console.log(
                    "Level:",
                    levelResult
                );

                console.log(
                    "Summary:",
                    summaryResult
                );

                console.log(
                    "Zones:",
                    zoneResult
                );


                /* =============================================
                   LEVEL DATA
                   ============================================= */

                if (
                    levelResult &&
                    levelResult.success
                ) {

                    asrsData =
                        Array.isArray(
                            levelResult.data
                        )
                            ? levelResult.data
                            : [];

                }
                else {

                    asrsData = [];

                    console.warn(
                        "Level API returned no data."
                    );

                }


                /* =============================================
                   SUMMARY
                   ============================================= */

                if (
                    summaryResult &&
                    summaryResult.success
                ) {

                    updateSummary(
                        summaryResult.data || {}
                    );

                }
                else {

                    updateSummary({});

                }


                /* =============================================
                   ZONES
                   ============================================= */

                if (
                    zoneResult &&
                    zoneResult.success
                ) {

                    zoneData =
                        Array.isArray(
                            zoneResult.data
                        )
                            ? zoneResult.data
                            : [];

                }
                else {

                    zoneData = [];

                }


                /* =============================================
                   CREATE GRID
                   ============================================= */

                createGrid(
                    asrsData
                );


                /* =============================================
                   ZONE INFORMATION
                   ============================================= */

                renderZones(
                    zoneData
                );


                /* =============================================
                   UPDATE TIME
                   ============================================= */

                updateTime();

            }
        )
        .catch(
            function (error) {

                console.error(
                    "ASRS loading error:",
                    error
                );

            }
        );

}


/* =========================================================
   FETCH RESPONSE HANDLER
   ========================================================= */

function handleFetchResponse(
    response
) {

    if (!response.ok) {

        throw new Error(
            "HTTP " +
            response.status +
            " - " +
            response.statusText
        );

    }


    return response.json();

}


/* =========================================================
   CREATE GRID
   ========================================================= */


function createGrid(data) {
    const container = document.getElementById("gridRows");

    if (!container) {
        console.error("gridRows not found.");
        return;
    }

    // Clear existing grid
    container.innerHTML = "";

    // Determine the maximum number of rows
    let maxRow = DEFAULT_ROWS;

    if (Array.isArray(data)) {
        data.forEach(function (item) {
            const rowValue = parseInt(
                item.Position || item.Row,
                10
            );

            if (!isNaN(rowValue) && rowValue > maxRow) {
                maxRow = rowValue;
            }
        });
    }

    // Create rows
    for (let row = 1; row <= maxRow; row++) {
        const rowDiv = document.createElement("div");
        rowDiv.className = "map-row";

        // Row label
        const rowLabel = document.createElement("div");
        rowLabel.className = "row-label";

        rowLabel.innerText =
            "R" + row.toString().padStart(2, "0");

        rowDiv.appendChild(rowLabel);

        // Create columns from C44 to C01
        for (
            let column = TOTAL_COLUMNS;
            column >= 1;
            column--
        ) {
            const cell = document.createElement("div");

            // Store row and column coordinates
            cell.dataset.row = row;
            cell.dataset.column = column;

            // Find matching storage location
            const location = findLocation(
                data,
                row,
                column
            );

            // Parent roadway / shuttle pathway
            if (location && isParentRoadway(location)) {
                cell.className = "parent-roadway";

                cell.innerHTML =
                    '<i class="fas fa-arrows-alt-h shuttle-icon"></i>';

                cell.title = "Shuttle Pathway";

                cell.dataset.location =
                    location.LocationNo || "";

                attachZoomEvents(cell);
            } else {
                // Normal storage location
                cell.className = "location not-defined";

                if (location) {
                    setLocation(
                        cell,
                        location,
                        row,
                        column
                    );
                } else {
                    // Undefined storage location
                    cell.dataset.location = "";

                    cell.title =
                        "R" + row + " / C" + column;

                    attachZoomEvents(cell);
                }
            }

            // Add cell to the current row
            rowDiv.appendChild(cell);
        }

        // Add completed row to the grid
        container.appendChild(rowDiv);
    }

    // Position loading side
    positionLoadingSide();

    console.log(
        `ASRS grid created: ${maxRow} rows x ${TOTAL_COLUMNS} columns`
    );
}


/* =========================================================
   FIND LOCATION
   ========================================================= */

function findLocation(
    data,
    row,
    column
) {

    if (
        !Array.isArray(data)
    ) {

        return null;

    }


    return data.find(
        function (item) {

            const itemRow =
                parseInt(
                    item.Position ||
                    item.Row,
                    10
                );


            const itemColumn =
                parseInt(
                    item.Column ||
                    item.Col,
                    10
                );


            return (
                itemRow === row &&
                itemColumn === column
            );

        }
    ) || null;

}


/* =========================================================
   PARENT ROADWAY
   ========================================================= */

function isParentRoadway(
    location
) {

    if (!location) {

        return false;

    }


    const value =
        location.ParentRoadway;


    if (value === true) {

        return true;

    }


    if (value === 1) {

        return true;

    }


    if (value === "1") {

        return true;

    }


    if (value === "true") {

        return true;

    }


    if (
        typeof value === "string" &&
        value
            .trim()
            .toUpperCase() ===
        "PARENTROADWAY"
    ) {

        return true;

    }


    /*
     * Also check LocaStatus.
     */

    const locaStatus =
        (
            location.LocaStatus ||
            ""
        )
            .toString()
            .trim()
            .toUpperCase();


    if (
        locaStatus ===
        "PARENTROADWAY"
    ) {

        return true;

    }


    return false;

}


/* =========================================================
   NORMALIZE STATUS
   ========================================================= */

function getLocationStatus(
    location
) {

    if (!location) {

        return "UNKNOWN";

    }


    /*
     * Prefer LocaStatus because movement-zone
     * exclusion is based on physical location type.
     */

    const value =
        location.LocaStatus ||
        location.Status ||
        location.LocationStatus ||
        "UNKNOWN";


    return value
        .toString()
        .trim()
        .toUpperCase();

}


/* =========================================================
   CHECK MOVEMENT ZONE ELIGIBILITY
   ========================================================= */

function canApplyMovementZone(
    location
) {

    if (!location) {

        return false;

    }


    const status =
        getLocationStatus(
            location
        );


    return !EXCLUDED_ZONE_STATUSES.includes(
        status
    );

}


/* =========================================================
   APPLY MOVEMENT ZONE
   ========================================================= */

function applyMovementZone(
    element,
    zoneClass,
    location
) {

    if (!element) {

        return;

    }


    /*
     * Always remove previous zone classes.
     */

    element.classList.remove(
        "movement-fast",
        "movement-medium",
        "movement-slow"
    );


    /*
     * Do NOT apply zone color to excluded
     * location statuses.
     */

    if (
        !canApplyMovementZone(
            location
        )
    ) {

        return;

    }


    /*
     * Apply zone class.
     */

    if (zoneClass) {

        element.classList.add(
            zoneClass
        );

    }

}


/* =========================================================
   GET MOVEMENT ZONE
   ========================================================= */

function getMovementZone(
    row,
    column,
    level
) {

    if (
        !Array.isArray(zoneData) ||
        zoneData.length === 0
    ) {

        return null;

    }


    const matchingZones =
        zoneData.filter(
            function (zone) {

                const columnFrom =
                    parseInt(
                        zone.ColumnFrom,
                        10
                    );

                const columnTo =
                    parseInt(
                        zone.ColumnTo,
                        10
                    );

                const rowFrom =
                    parseInt(
                        zone.RowFrom,
                        10
                    );

                const rowTo =
                    parseInt(
                        zone.RowTo,
                        10
                    );

                const levelFrom =
                    parseInt(
                        zone.LevelFrom,
                        10
                    );

                const levelTo =
                    parseInt(
                        zone.LevelTo,
                        10
                    );


                if (
                    isNaN(columnFrom) ||
                    isNaN(columnTo) ||
                    isNaN(rowFrom) ||
                    isNaN(rowTo) ||
                    isNaN(levelFrom) ||
                    isNaN(levelTo)
                ) {

                    return false;

                }


                const minColumn =
                    Math.min(
                        columnFrom,
                        columnTo
                    );

                const maxColumn =
                    Math.max(
                        columnFrom,
                        columnTo
                    );


                const minRow =
                    Math.min(
                        rowFrom,
                        rowTo
                    );

                const maxRow =
                    Math.max(
                        rowFrom,
                        rowTo
                    );


                const minLevel =
                    Math.min(
                        levelFrom,
                        levelTo
                    );

                const maxLevel =
                    Math.max(
                        levelFrom,
                        levelTo
                    );


                return (

                    column >= minColumn &&
                    column <= maxColumn &&

                    row >= minRow &&
                    row <= maxRow &&

                    level >= minLevel &&
                    level <= maxLevel

                );

            }
        );


    if (
        matchingZones.length === 0
    ) {

        return null;

    }


    /*
     * Lower Priority number wins.
     *
     * 1 = highest
     * 2 = next
     * 3 = next
     */

    matchingZones.sort(
        function (a, b) {

            return (
                parseInt(
                    a.Priority || 999,
                    10
                ) -
                parseInt(
                    b.Priority || 999,
                    10
                )
            );

        }
    );


    return matchingZones[0];

}


/* =========================================================
   GET MOVEMENT ZONE CLASS
   ========================================================= */

function getMovementZoneClass(
    movementZone
) {

    if (!movementZone) {

        return "";

    }


    const movementType =
        (
            movementZone.MovementType ||
            ""
        )
            .toString()
            .trim()
            .toUpperCase();


    if (
        movementType === "FAST"
    ) {

        return "movement-fast";

    }


    if (
        movementType === "MEDIUM"
    ) {

        return "movement-medium";

    }


    if (
        movementType === "SLOW"
    ) {

        return "movement-slow";

    }


    return "";

}


/* =========================================================
   SET LOCATION
   ========================================================= */

function setLocation(
    cell,
    location,
    row,
    column
) {

    if (
        !cell ||
        !location
    ) {

        return;

    }


    /*
     * =====================================================
     * STATUS
     * =====================================================
     */

    const status =
        getLocationStatus(
            location
        );


    let className =
        "unknown";


    if (
        status === "EMPTY"
    ) {

        className =
            "empty";

    }

    else if (
        status === "LOADED"
    ) {

        className =
            "loaded";

    }

    else if (
        status === "AVAILABLE"
    ) {

        className =
            "available";

    }

    else if (
        status === "BLOCKED"
    ) {

        className =
            "blocked";

    }


    /*
     * =====================================================
     * BASE CLASS
     * =====================================================
     */

    cell.className =
        "location " +
        className;




    //    /*
    //* =====================================================
    //* FG CODE COLOR for Testing
    //* =====================================================
    //*/

    const fgCode =
        getFGCode(location);


    if (
        status === "LOADED" &&
        fgCode !== ""
    ) {

        const fgColor =
            getFGColor(fgCode);


        cell.style.setProperty(
            "--fg-color",
            fgColor
        );


        cell.dataset.fgcode =
            fgCode;

    }
    else {

        cell.style.removeProperty(
            "--fg-color"
        );

        delete cell.dataset.fgcode;

    }
    /*
     * =====================================================
     * DATA ATTRIBUTES
     * =====================================================
     */

    cell.dataset.location =
        location.LocationNo ||
        "";


    cell.dataset.row =
        row;


    cell.dataset.column =
        column;


    cell.dataset.status =
        status;


    /*
     * =====================================================
     * MOVEMENT ZONE
     * =====================================================
     */

    const movementZone =
        getMovementZone(
            row,
            column,
            selectedLevel
        );


    const zoneClass =
        getMovementZoneClass(
            movementZone
        );


    /*
     * IMPORTANT:
     *
     * Zone color is applied ONLY when
     * the location is eligible.
     */

    applyMovementZone(
        cell,
        zoneClass,
        location
    );


    /*
     * =====================================================
     * TITLE
     * =====================================================
     */

    if (
        movementZone &&
        canApplyMovementZone(
            location
        )
    ) {

        const movementType =
            (
                movementZone.MovementType ||
                ""
            )
                .toString()
                .trim()
                .toUpperCase();


        cell.dataset.movement =
            movementType;


        cell.dataset.zone =
            movementZone.ZoneName ||
            "";


        cell.title =
            (
                location.LocationNo ||
                "R" +
                row +
                " / C" +
                column
            ) +
            " | " +
            (
                movementZone.ZoneName ||
                movementType
            );

    }
    else {

        delete cell.dataset.movement;

        delete cell.dataset.zone;


        cell.title =
            location.LocationNo ||
            "R" +
            row +
            " / C" +
            column;

    }


    /*
     * =====================================================
     * ICON
     * =====================================================
     */

    if (
        status === "LOADED"
    ) {

        cell.innerHTML =
            '<i class="fas fa-box pallet-icon"></i>';

    }

    else if (
        status === "EMPTY"
    ) {

        cell.innerHTML =
            "";

    }

    else if (
        status === "AVAILABLE"
    ) {

        cell.innerHTML =
            '<i class="fas fa-check-circle pallet-icon"></i>';

    }

    else if (
        status === "BLOCKED"
    ) {

        cell.innerHTML =
            '<i class="fas fa-ban pallet-icon"></i>';

    }

    else if (
        status === "PARENTROADWAY"
    ) {

        cell.innerHTML =
            '<div class="shuttle-path">' +
            '<i class="fas fa-arrows-alt-h shuttle-path-icon"></i>' +
            '</div>';

    }

    else if (
        status === "ROADWAY"
    ) {

        cell.innerHTML =
            '<div class="shuttle-path">' +
            '<i class="fas fa-arrows-alt-v shuttle-path-icon"></i>' +
            '</div>';

    }

    else if (
        status === "CACHEROADWAY"
    ) {

        cell.innerHTML =
            '<div class="shuttle-path">' +
            '<i class="fas fa-arrows-alt-v shuttle-path-icon"></i>' +
            '</div>';

    }

    else if (
        status === "LIFT"
    ) {

        cell.innerHTML =
            '<i class="fas fa-arrows-alt pallet-icon"></i>';

    }

    else if (
        status === "CHARGE"
    ) {

        cell.innerHTML =
            '<i class="fas fa-bolt pallet-icon"></i>';

    }

    else if (
        status === "ISOLATION"
    ) {

        cell.innerHTML =
            '<i class="fas fa-shield-alt pallet-icon"></i>';

    }

    else {

        cell.innerHTML =
            '<i class="fas fa-question-circle pallet-icon"></i>';

    }


    /*
     * =====================================================
     * CLICK
     * =====================================================
     */

    cell.addEventListener(
        "click",
        function () {

            hideZoomPreview();

            showLocationModal(
                location
            );

        }
    );


    /*
     * =====================================================
     * MAGNIFIER
     * =====================================================
     */

    attachZoomEvents(
        cell
    );

}


/* =========================================================
   ZOOM EVENTS
   ========================================================= */

function attachZoomEvents(
    cell
) {

    if (!cell) {

        return;

    }


    /*
     * Prevent duplicate listeners.
     */

    if (
        cell.dataset.zoomAttached === "true"
    ) {

        return;

    }


    cell.dataset.zoomAttached =
        "true";


    /*
     * =====================================================
     * MOUSE ENTER
     * =====================================================
     */

    cell.addEventListener(
        "mouseenter",
        function (event) {

            const row =
                parseInt(
                    this.dataset.row,
                    10
                );


            const column =
                parseInt(
                    this.dataset.column,
                    10
                );


            if (
                isNaN(row) ||
                isNaN(column)
            ) {

                return;

            }


            showZoomPreview(
                row,
                column,
                event
            );

        }
    );


    /*
     * =====================================================
     * MOUSE MOVE
     * =====================================================
     */

    cell.addEventListener(
        "mousemove",
        function (event) {

            const row =
                parseInt(
                    this.dataset.row,
                    10
                );


            const column =
                parseInt(
                    this.dataset.column,
                    10
                );


            if (
                isNaN(row) ||
                isNaN(column)
            ) {

                return;

            }


            const preview =
                document.getElementById(
                    "zoomPreview"
                );


            if (!preview) {

                return;

            }


            const previousRow =
                preview.dataset.row;


            const previousColumn =
                preview.dataset.column;


            /*
             * If cursor entered another cell,
             * rebuild magnifier contents.
             */

            if (
                previousRow !==
                String(row) ||

                previousColumn !==
                String(column)
            ) {

                showZoomPreview(
                    row,
                    column,
                    event
                );

            }
            else {

                moveZoomPreview(
                    event
                );

            }

        }
    );


    /*
     * =====================================================
     * MOUSE LEAVE
     * =====================================================
     */

    cell.addEventListener(
        "mouseleave",
        function () {

            hideZoomPreview();

        }
    );

}


/* =========================================================
   SHOW MAGNIFYING GLASS
   ========================================================= */

function showZoomPreview(
    row,
    column,
    event
) {

    const preview =
        document.getElementById(
            "zoomPreview"
        );


    if (!preview) {

        return;

    }


    /*
     * Store current center cell.
     */

    preview.dataset.row =
        row;


    preview.dataset.column =
        column;


    /*
     * =====================================================
     * FOOTER
     * =====================================================
     */

    const zoomRow =
        document.getElementById(
            "zoomRow"
        );


    const zoomColumn =
        document.getElementById(
            "zoomColumn"
        );


    if (zoomRow) {

        zoomRow.innerText =
            "R" +
            String(row)
                .padStart(
                    2,
                    "0"
                );

    }


    if (zoomColumn) {

        zoomColumn.innerText =
            "C" +
            String(column)
                .padStart(
                    2,
                    "0"
                );

    }


    /*
     * =====================================================
     * CREATE MAGNIFIED GRID
     * =====================================================
     */

    createZoomGrid(
        row,
        column
    );


    /*
     * =====================================================
     * SHOW
     * =====================================================
     */

    preview.hidden =
        false;


    preview.classList.add(
        "show"
    );


    /*
     * =====================================================
     * POSITION
     * =====================================================
     */

    moveZoomPreview(
        event
    );

}


/* =========================================================
   CREATE ZOOM GRID
   ========================================================= */

function createZoomGrid(
    centerRow,
    centerColumn
) {

    const zoomGrid =
        document.getElementById(
            "zoomGrid"
        );


    if (!zoomGrid) {

        return;

    }


    zoomGrid.innerHTML =
        "";


    /*
     * =====================================================
     * GRID SIZE
     * =====================================================
     */

    zoomGrid.style.gridTemplateColumns =
        "repeat(" +
        ZOOM_COLUMNS +
        ", 1fr)";


    /*
     * =====================================================
     * CALCULATE START POSITION
     * =====================================================
     */

    const halfRows =
        Math.floor(
            ZOOM_ROWS / 2
        );


    const halfColumns =
        Math.floor(
            ZOOM_COLUMNS / 2
        );


    let startRow =
        centerRow -
        halfRows;


    let startColumn =
        centerColumn -
        halfColumns;


    /*
     * TOP
     */

    if (
        startRow < 1
    ) {

        startRow =
            1;

    }


    /*
     * LEFT
     */

    if (
        startColumn < 1
    ) {

        startColumn =
            1;

    }


    /*
     * BOTTOM
     */

    const maxRow =
        DEFAULT_ROWS;


    if (
        startRow +
        ZOOM_ROWS -
        1 >
        maxRow
    ) {

        startRow =
            Math.max(
                1,
                maxRow -
                ZOOM_ROWS +
                1
            );

    }


    /*
     * RIGHT / C44
     */

    if (
        startColumn +
        ZOOM_COLUMNS -
        1 >
        TOTAL_COLUMNS
    ) {

        startColumn =
            Math.max(
                1,
                TOTAL_COLUMNS -
                ZOOM_COLUMNS +
                1
            );

    }


    /*
     * =====================================================
     * CREATE MAGNIFIED CELLS
     * =====================================================
     */

    for (
        let r = 0;
        r < ZOOM_ROWS;
        r++
    ) {

        const actualRow =
            startRow +
            r;


        const zoomRow =
            document.createElement(
                "div"
            );


        zoomRow.className =
            "zoom-row";


        for (
            let c = 0;
            c < ZOOM_COLUMNS;
            c++
        ) {

            const actualColumn =
                startColumn +
                c;


            const location =
                getLocation(
                    actualRow,
                    actualColumn
                );


            const zoomCell =
                document.createElement(
                    "div"
                );


            zoomCell.className =
                "zoom-cell";


            /*
             * =================================================
             * CENTER CELL
             * =================================================
             */

            if (
                actualRow === centerRow &&
                actualColumn === centerColumn
            ) {

                zoomCell.classList.add(
                    "zoom-center"
                );

            }


            zoomCell.dataset.row =
                actualRow;


            zoomCell.dataset.column =
                actualColumn;


            /*
             * =================================================
             * NO LOCATION
             * =================================================
             */

            if (!location) {

                zoomCell.classList.add(
                    "unknown"
                );


                zoomCell.innerHTML =
                    '<span class="zoom-location-name">' +
                    "R" +
                    String(actualRow)
                        .padStart(
                            2,
                            "0"
                        ) +
                    " / C" +
                    String(actualColumn)
                        .padStart(
                            2,
                            "0"
                        ) +
                    "</span>";


                zoomCell.title =
                    "R" +
                    actualRow +
                    " / C" +
                    actualColumn;

            }


            /*
             * =================================================
             * LOCATION EXISTS
             * =================================================
             */

            else {

                const status =
                    getLocationStatus(
                        location
                    );


                let statusClass =
                    "unknown";


                if (
                    status === "EMPTY"
                ) {

                    statusClass =
                        "empty";

                }

                else if (
                    status === "LOADED"
                ) {

                    statusClass =
                        "loaded";

                }

                else if (
                    status === "AVAILABLE"
                ) {

                    statusClass =
                        "available";

                }

                else if (
                    status === "BLOCKED"
                ) {

                    statusClass =
                        "blocked";

                }


                /*
                 * Apply normal status color.
                 */

                zoomCell.classList.add(
                    statusClass
                );


                /*
                 * =================================================
                 * MOVEMENT ZONE
                 * =================================================
                 */

                const movementZone =
                    getMovementZone(
                        actualRow,
                        actualColumn,
                        selectedLevel
                    );


                const zoneClass =
                    getMovementZoneClass(
                        movementZone
                    );


                /*
                 * IMPORTANT:
                 *
                 * Same exclusion rule as main map.
                 */

                applyMovementZone(
                    zoomCell,
                    zoneClass,
                    location
                );


                /*
                 * =================================================
                 * ICON
                 * =================================================
                 */

                let icon =
                    "fa-question-circle";


                if (
                    status === "EMPTY"
                ) {

                    icon =
                        "fa-inbox";

                }

                else if (
                    status === "LOADED"
                ) {

                    icon =
                        "fa-box";

                }

                else if (
                    status === "AVAILABLE"
                ) {

                    icon =
                        "fa-check-circle";

                }

                else if (
                    status === "BLOCKED"
                ) {

                    icon =
                        "fa-ban";

                }

                else if (
                    status === "PARENTROADWAY"
                ) {

                    icon =
                        "fa-arrows-alt-h";

                }

                else if (
                    status === "ROADWAY"
                ) {

                    icon =
                        "fa-arrows-alt-v";

                }

                else if (
                    status === "CACHEROADWAY"
                ) {

                    icon =
                        "fa-arrows-alt-v";

                }

                else if (
                    status === "CHARGE"
                ) {

                    icon =
                        "fa-bolt";

                }

                else if (
                    status === "ISOLATION"
                ) {

                    icon =
                        "fa-shield-alt";

                }

                else if (
                    status === "LIFT"
                ) {

                    icon =
                        "fa-arrows-alt";

                }


                /*
                 * =================================================
                 * CELL CONTENT
                 * =================================================
                 */

                const locationName =
                    location.LocationNo ||
                    (
                        "R" +
                        String(actualRow)
                            .padStart(
                                2,
                                "0"
                            ) +
                        " / C" +
                        String(actualColumn)
                            .padStart(
                                2,
                                "0"
                            )
                    );


                zoomCell.innerHTML =
                    '<i class="fas ' +
                    icon +
                    ' zoom-icon"></i>' +

                    '<span class="zoom-location-name">' +
                    locationName +
                    '</span>';


                zoomCell.title =
                    locationName;


                /*
                 * =================================================
                 * CLICK MAGNIFIED CELL
                 * =================================================
                 */

                zoomCell.addEventListener(
                    "click",
                    function () {

                        hideZoomPreview();

                        showLocationModal(
                            location
                        );

                    }
                );

            }


            zoomRow.appendChild(
                zoomCell
            );

        }


        zoomGrid.appendChild(
            zoomRow
        );

    }

}


/* =========================================================
   MOVE MAGNIFYING GLASS
   ========================================================= */

function moveZoomPreview(
    event
) {

    const preview =
        document.getElementById(
            "zoomPreview"
        );


    if (
        !preview ||
        preview.hidden
    ) {

        return;

    }


    /*
     * Get current dimensions.
     */

    const rect =
        preview.getBoundingClientRect();


    /*
     * =====================================================
     * CURSOR IS CENTER OF MAGNIFIER
     * =====================================================
     */

    let left =
        event.clientX -
        (
            rect.width /
            2
        );


    let top =
        event.clientY -
        (
            rect.height /
            2
        );


    /*
     * =====================================================
     * KEEP MAGNIFIER INSIDE SCREEN
     * =====================================================
     */

    const margin =
        8;


    /*
     * RIGHT
     */

    if (
        left +
        rect.width >
        window.innerWidth -
        margin
    ) {

        left =
            window.innerWidth -
            rect.width -
            margin;

    }


    /*
     * BOTTOM
     */

    if (
        top +
        rect.height >
        window.innerHeight -
        margin
    ) {

        top =
            window.innerHeight -
            rect.height -
            margin;

    }


    /*
     * LEFT
     */

    if (
        left <
        margin
    ) {

        left =
            margin;

    }


    /*
     * TOP
     */

    if (
        top <
        margin
    ) {

        top =
            margin;

    }


    /*
     * APPLY
     */

    preview.style.left =
        left +
        "px";


    preview.style.top =
        top +
        "px";

}


/* =========================================================
   HIDE MAGNIFYING GLASS
   ========================================================= */

function hideZoomPreview() {

    const preview =
        document.getElementById(
            "zoomPreview"
        );


    if (!preview) {

        return;

    }


    preview.classList.remove(
        "show"
    );


    preview.hidden =
        true;


    delete preview.dataset.row;

    delete preview.dataset.column;

}


/* =========================================================
   LOCATION MODAL
   ========================================================= */

function showLocationModal(
    location
) {

    if (!location) {

        return;

    }


    const status =
        getLocationStatus(
            location
        );


    /*
     * =====================================================
     * LOCATION
     * =====================================================
     */

    const title =
        document.getElementById(
            "modalLocationTitle"
        );


    if (title) {

        title.innerText =
            location.LocationNo ||
            "Location Details";

    }


    const modalLocation =
        document.getElementById(
            "modalLocation"
        );


    if (modalLocation) {

        modalLocation.innerText =
            location.LocationNo ||
            "-";

    }


    const modalLevel =
        document.getElementById(
            "modalLevel"
        );


    if (modalLevel) {

        modalLevel.innerText =
            location.Level ||
            selectedLevel ||
            "-";

    }


    const modalColumn =
        document.getElementById(
            "modalColumn"
        );


    if (modalColumn) {

        modalColumn.innerText =
            location.Column ||
            location.Col ||
            "-";

    }


    const modalRow =
        document.getElementById(
            "modalRow"
        );


    if (modalRow) {

        modalRow.innerText =
            location.Position ||
            location.Row ||
            "-";

    }


    const modalPallet =
        document.getElementById(
            "modalPallet"
        );


    if (modalPallet) {

        modalPallet.innerText =
            location.PalletNo ||
            location.Pallet ||
            location.Barcode ||
            "-";

    }

    const modalPart =
        document.getElementById("modalPart");

    if (modalPart) {

        console.log("LOCATION OBJECT:", location);
        console.log("PalletQr:", location.PalletQr);

        let palletQr =
            location.PalletQr;

        if (palletQr !== null &&
            palletQr !== undefined) {

            palletQr =
                String(palletQr).trim();

            const qrParts =
                palletQr.split(";");

            console.log("QR PARTS:", qrParts);

            // MODEL-X is the 8th field
            modalPart.innerText =
                qrParts[7] ?
                    qrParts[9].trim() :
                    "-";

        }
        else {

            modalPart.innerText = "-";

        }

    }

    const modalQty =
        document.getElementById(
            "modalQty"
        );


    if (modalQty) {

        modalQty.innerText =
            location.Qty ??
            location.Quantity ??
            0;

    }


    /*
     * =====================================================
     * STATUS
     * =====================================================
     */

    const modalStatusText =
        document.getElementById(
            "modalStatusText"
        );


    if (modalStatusText) {

        modalStatusText.innerText =
            status;

    }


    const statusElement =
        document.getElementById(
            "modalStatus"
        );


    if (statusElement) {

        statusElement.className =
            "modal-status";


        if (
            status === "EMPTY"
        ) {

            statusElement.style.background =
                "#dcfce7";

            statusElement.style.color =
                "#166534";

        }

        else if (
            status === "LOADED"
        ) {

            statusElement.style.background =
                "#dbeafe";

            statusElement.style.color =
                "#1d4ed8";

        }

        else if (
            status === "AVAILABLE"
        ) {

            statusElement.style.background =
                "#fef3c7";

            statusElement.style.color =
                "#92400e";

        }

        else if (
            status === "BLOCKED"
        ) {

            statusElement.style.background =
                "#fee2e2";

            statusElement.style.color =
                "#b91c1c";

        }

        else {

            statusElement.style.background =
                "#e5e7eb";

            statusElement.style.color =
                "#475569";

        }

    }


    /*
     * =====================================================
     * SHOW MODAL
     * =====================================================
     */

    const modal =
        document.getElementById(
            "locationModal"
        );


    if (modal) {

        modal.classList.add(
            "show"
        );

    }


    document.body.style.overflow =
        "hidden";

}


/* =========================================================
   CLOSE LOCATION MODAL
   ========================================================= */

function closeLocationModal(
    event
) {

    /*
     * If clicked inside modal,
     * do nothing.
     */

    if (
        event &&
        event.target !==
        event.currentTarget
    ) {

        return;

    }


    const modal =
        document.getElementById(
            "locationModal"
        );


    if (modal) {

        modal.classList.remove(
            "show"
        );

    }


    document.body.style.overflow =
        "";

}


/* =========================================================
   SUMMARY
   ========================================================= */

function updateSummary(
    data
) {

    data =
        data || {};


    const total =
        document.getElementById(
            "totalCount"
        );


    const empty =
        document.getElementById(
            "emptyCount"
        );


    const loaded =
        document.getElementById(
            "loadedCount"
        );


    const available =
        document.getElementById(
            "availableCount"
        );


    const blocked =
        document.getElementById(
            "blockedCount"
        );


    if (total) {

        total.innerText =
            data.total ??
            0;

    }


    if (empty) {

        empty.innerText =
            data.empty ??
            0;

    }


    if (loaded) {

        loaded.innerText =
            data.loaded ??
            0;

    }


    if (available) {

        available.innerText =
            data.available ??
            0;

    }


    if (blocked) {

        blocked.innerText =
            data.blocked ??
            0;

    }

}


/* =========================================================
   RENDER ZONES
   ========================================================= */

function renderZones(
    zones
) {

    if (
        !Array.isArray(zones)
    ) {

        return;

    }


    console.log(
        "Loaded movement zones:",
        zones.length
    );

}


/* =========================================================
   POSITION LOADING SIDE
   ========================================================= */

function positionLoadingSide() {

    const loadingSide =
        document.getElementById(
            "loadingSide"
        );


    if (!loadingSide) {

        return;

    }


    loadingSide.style.display =
        "flex";

}


/* =========================================================
   SEARCH
   ========================================================= */

function filterLocations(
    search
) {

    search =
        (
            search ||
            ""
        )
            .trim()
            .toUpperCase();


    document
        .querySelectorAll(
            ".location"
        )
        .forEach(
            function (cell) {

                const location =
                    (
                        cell.dataset.location ||
                        ""
                    )
                        .toUpperCase();


                if (
                    search === "" ||
                    location.includes(
                        search
                    )
                ) {

                    cell.style.opacity =
                        "1";

                }
                else {

                    cell.style.opacity =
                        "0.15";

                }

            }
        );

}


/* =========================================================
   UPDATE TIME
   ========================================================= */

function updateTime() {

    const element =
        document.getElementById(
            "lastUpdate"
        );


    if (!element) {

        return;

    }


    const now =
        new Date();


    element.innerText =
        now.toLocaleTimeString();

}


/* =========================================================
   ESCAPE KEY
   ========================================================= */

document.addEventListener(
    "keydown",
    function (event) {

        if (
            event.key ===
            "Escape"
        ) {

            hideZoomPreview();

            closeLocationModal();

        }

    }
);


/* =========================================================
   LOADING SIMULATION
   ========================================================= */

function startLoadingSimulation() {

    if (
        loadingSimulationRunning
    ) {

        return;

    }


    /*
     * =====================================================
     * CHECK API
     * =====================================================
     */

    if (
        !window.ASRS_URLS ||
        !window.ASRS_URLS.getLoadingSimulation
    ) {

        console.error(
            "GetLoadingSimulation URL not found."
        );


        alert(
            "Loading simulation API URL is not configured."
        );


        return;

    }


    /*
     * =====================================================
     * PANEL
     * =====================================================
     */

    const panel =
        document.getElementById(
            "loadingSimulationPanel"
        );


    if (panel) {

        panel.classList.add(
            "active"
        );

    }


    /*
     * =====================================================
     * CLEAR PREVIOUS
     * =====================================================
     */

    clearSimulationVisuals();


    /*
     * =====================================================
     * INITIAL STATUS
     * =====================================================
     */

    updateSimulationInfo(
        0,
        0,
        "-",
        "-",
        "-",
        "-",
        "-",
        "Loading sequence..."
    );


    /*
     * =====================================================
     * BUTTONS
     * =====================================================
     */

    const simulateButton =
        document.getElementById(
            "btnSimulateLoading"
        );


    const stopButton =
        document.getElementById(
            "btnStopSimulation"
        );


    if (simulateButton) {

        simulateButton.disabled =
            true;

    }


    if (stopButton) {

        stopButton.style.display =
            "inline-block";

    }


    /*
     * =====================================================
     * API
     * =====================================================
     */

    fetch(
        window.ASRS_URLS.getLoadingSimulation +
        "?level=" +
        encodeURIComponent(
            selectedLevel
        ),
        {
            method: "GET",
            cache: "no-store"
        }
    )
        .then(
            handleFetchResponse
        )
        .then(
            function (data) {

                let simulationRows =
                    [];


                if (
                    Array.isArray(data)
                ) {

                    simulationRows =
                        data;

                }
                else if (
                    data &&
                    Array.isArray(
                        data.data
                    )
                ) {

                    simulationRows =
                        data.data;

                }


                if (
                    simulationRows.length === 0
                ) {

                    updateSimulationInfo(
                        0,
                        0,
                        "-",
                        "-",
                        "-",
                        "-",
                        "-",
                        "No available locations"
                    );


                    finishLoadingSimulation();

                    return;

                }


                /*
                 * =================================================
                 * SORT
                 * =================================================
                 */

                loadingSimulationData =
                    simulationRows.sort(
                        function (a, b) {

                            return (
                                parseInt(
                                    a.sequence ||
                                    a.Sequence ||
                                    0,
                                    10
                                ) -
                                parseInt(
                                    b.sequence ||
                                    b.Sequence ||
                                    0,
                                    10
                                )
                            );

                        }
                    );


                loadingSimulationIndex =
                    0;


                loadingSimulationRunning =
                    true;


                runNextLoadingStep();

            }
        )
        .catch(
            function (error) {

                console.error(
                    "Loading simulation error:",
                    error
                );


                updateSimulationInfo(
                    0,
                    0,
                    "-",
                    "-",
                    "-",
                    "-",
                    "-",
                    "Error loading simulation"
                );


                finishLoadingSimulation();

            }
        );

}


/* =========================================================
   RUN NEXT LOADING STEP
   ========================================================= */

function runNextLoadingStep() {

    if (
        !loadingSimulationRunning
    ) {

        return;

    }


    /*
     * Finished
     */

    if (
        loadingSimulationIndex >=
        loadingSimulationData.length
    ) {

        finishLoadingSimulation();

        return;

    }


    const item =
        loadingSimulationData[
        loadingSimulationIndex
        ];


    const currentStep =
        loadingSimulationIndex +
        1;


    const row =
        parseInt(
            item.Row ||
            item.Position ||
            0,
            10
        );


    const column =
        parseInt(
            item.Col ||
            item.Column ||
            0,
            10
        );


    const layer =
        item.Layer ||
        selectedLevel;


    const pallet =
        item.PalletNo ||
        item.PalletQr ||
        item.Pallet ||
        "SIM-PALLET";


    const locationName =
        item.LocationName ||
        item.LocationNo ||
        (
            row > 0 &&
                column > 0
                ? "R" +
                row +
                " / C" +
                column
                : "-"
        );


    /*
     * =====================================================
     * UPDATE INFO
     * =====================================================
     */

    updateSimulationInfo(

        currentStep,

        loadingSimulationData.length,

        pallet,

        locationName,

        layer,

        column,

        row,

        "Moving"

    );


    /*
     * =====================================================
     * CLEAR PREVIOUS
     * =====================================================
     */

    clearCurrentSimulationCell();


    /*
     * =====================================================
     * TARGET
     * =====================================================
     */

    highlightSimulationTarget(
        row,
        column
    );


    /*
     * =====================================================
     * MOVE PALLET
     * =====================================================
     */

    moveSimulationPalletToLocation(
        item,
        function () {

            if (
                !loadingSimulationRunning
            ) {

                return;

            }


            /*
             * Mark loaded.
             */

            markSimulationLoaded(
                row,
                column
            );


            /*
             * Update.
             */

            updateSimulationInfo(

                currentStep,

                loadingSimulationData.length,

                pallet,

                locationName,

                layer,

                column,

                row,

                "Loaded"

            );


            /*
             * Next.
             */

            loadingSimulationIndex++;


            loadingSimulationTimer =
                setTimeout(
                    function () {

                        runNextLoadingStep();

                    },
                    700
                );

        }
    );

}


/* =========================================================
   MOVE SIMULATION PALLET
   ========================================================= */

function moveSimulationPalletToLocation(
    item,
    callback
) {

    const row =
        parseInt(
            item.Row ||
            item.Position ||
            0,
            10
        );


    const column =
        parseInt(
            item.Col ||
            item.Column ||
            0,
            10
        );


    const cell =
        document.querySelector(
            '.location[data-row="' +
            row +
            '"][data-column="' +
            column +
            '"]'
        );


    if (!cell) {

        console.warn(
            "Simulation target not found:",
            row,
            column
        );


        callback();

        return;

    }


    /*
     * Create pallet.
     */

    createSimulationPallet();


    if (
        !simulationPalletElement
    ) {

        callback();

        return;

    }


    const rect =
        cell.getBoundingClientRect();


    const targetLeft =
        rect.left +
        (
            rect.width /
            2
        ) -
        16;


    const targetTop =
        rect.top +
        (
            rect.height /
            2
        ) -
        16;


    requestAnimationFrame(
        function () {

            simulationPalletElement
                .classList.add(
                    "moving"
                );


            simulationPalletElement.style.left =
                targetLeft +
                "px";


            simulationPalletElement.style.top =
                targetTop +
                "px";

        }
    );


    setTimeout(
        function () {

            if (
                simulationPalletElement
            ) {

                simulationPalletElement
                    .classList.remove(
                        "moving"
                    );

            }


            callback();

        },
        650
    );

}


/* =========================================================
   CREATE SIMULATION PALLET
   ========================================================= */

function createSimulationPallet() {

    /*
     * Remove previous.
     */

    if (
        simulationPalletElement
    ) {

        simulationPalletElement.remove();

        simulationPalletElement =
            null;

    }


    /*
     * Create.
     */

    simulationPalletElement =
        document.createElement(
            "div"
        );


    simulationPalletElement.className =
        "simulation-pallet";


    simulationPalletElement.innerHTML =
        '<i class="fas fa-box"></i>';


    document.body.appendChild(
        simulationPalletElement
    );


    /*
     * Start from loading side.
     */

    const loadingSide =
        document.getElementById(
            "loadingSide"
        );


    if (
        loadingSide
    ) {

        const rect =
            loadingSide.getBoundingClientRect();


        simulationPalletElement.style.left =
            (
                rect.left +
                (
                    rect.width /
                    2
                ) -
                16
            ) +
            "px";


        simulationPalletElement.style.top =
            (
                rect.top +
                (
                    rect.height /
                    2
                ) -
                16
            ) +
            "px";

    }

}


/* =========================================================
   HIGHLIGHT SIMULATION TARGET
   ========================================================= */

function highlightSimulationTarget(
    row,
    column
) {

    clearSimulationTarget();


    const cell =
        document.querySelector(
            '.location[data-row="' +
            row +
            '"][data-column="' +
            column +
            '"]'
        );


    if (!cell) {

        return;

    }


    cell.classList.add(
        "simulation-target"
    );

}


/* =========================================================
   CLEAR SIMULATION TARGET
   ========================================================= */

function clearSimulationTarget() {

    document
        .querySelectorAll(
            ".simulation-target"
        )
        .forEach(
            function (element) {

                element.classList.remove(
                    "simulation-target"
                );

            }
        );

}

/* =========================================================
   GET FG CODE FROM LOCATION
   ========================================================= */

function getFGCode(location) {

    if (!location) {
        return "";
    }


    /*
     * Direct FGCode if API already provides it.
     */
    if (location.FGCode) {

        return String(location.FGCode)
            .trim()
            .toUpperCase();

    }


    /*
     * Otherwise extract from PalletQr.
     *
     * Current QR structure contains FG information.
     */
    let palletQr =
        location.PalletQr;


    if (
        palletQr === null ||
        palletQr === undefined
    ) {

        return "";

    }


    palletQr =
        String(palletQr).trim();


    if (palletQr === "") {

        return "";

    }


    const qrParts =
        palletQr.split(";");


    /*setLocation
     * FGCode position.
     *
     * Your current code is already reading
     * the FG information from the QR fields.
     */
    if (qrParts.length > 9) {

        return String(qrParts[9] || "")
            .trim()
            .toUpperCase();

    }


    return "";
}


/* =========================================================
   CLEAR CURRENT SIMULATION CELL
   ========================================================= */

function clearCurrentSimulationCell() {

    document
        .querySelectorAll(
            ".simulation-current"
        )
        .forEach(
            function (element) {

                element.classList.remove(
                    "simulation-current"
                );

            }
        );

}


/* =========================================================
   MARK SIMULATION LOADED
   ========================================================= */

function markSimulationLoaded(
    row,
    column
) {

    const cell =
        document.querySelector(
            '.location[data-row="' +
            row +
            '"][data-column="' +
            column +
            '"]'
        );


    if (!cell) {

        return;

    }


    cell.classList.remove(
        "simulation-target"
    );


    cell.classList.add(
        "simulation-current"
    );


    /*
     * Keep loaded visual.
     */

    cell.classList.remove(
        "empty",
        "available",
        "blocked",
        "unknown"
    );


    cell.classList.add(
        "loaded"
    );


    cell.innerHTML =
        '<i class="fas fa-box pallet-icon"></i>';

}


/* =========================================================
   HIGHLIGHT CURRENT CELL
   ========================================================= */

function highlightSimulationCurrent(
    row,
    column
) {

    document
        .querySelectorAll(
            ".location"
        )
        .forEach(
            function (cell) {

                cell.classList.remove(
                    "simulation-current"
                );

            }
        );


    const cell =
        document.querySelector(
            '.location[data-row="' +
            row +
            '"][data-column="' +
            column +
            '"]'
        );


    if (cell) {

        cell.classList.add(
            "simulation-current"
        );

    }

}


/* =========================================================
   CLEAR SIMULATION VISUALS
   ========================================================= */

function clearSimulationVisuals() {

    document
        .querySelectorAll(
            ".location"
        )
        .forEach(
            function (element) {

                element.classList.remove(
                    "simulation-current",
                    "simulation-path",
                    "simulation-target"
                );

            }
        );


    const loadingSide =
        document.getElementById(
            "loadingSide"
        );


    if (
        loadingSide
    ) {

        loadingSide.classList.remove(
            "simulation-loading"
        );

    }


    if (
        simulationPalletElement
    ) {

        simulationPalletElement.remove();

        simulationPalletElement =
            null;

    }

}


/* =========================================================
   STOP LOADING SIMULATION
   ========================================================= */

function stopLoadingSimulation() {

    loadingSimulationRunning =
        false;


    /*
     * Clear timer.
     */

    if (
        loadingSimulationTimer
    ) {

        clearTimeout(
            loadingSimulationTimer
        );

        loadingSimulationTimer =
            null;

    }


    /*
     * Clear visuals.
     */

    clearSimulationVisuals();


    /*
     * Buttons.
     */

    const simulateButton =
        document.getElementById(
            "btnSimulateLoading"
        );


    const stopButton =
        document.getElementById(
            "btnStopSimulation"
        );


    if (
        simulateButton
    ) {

        simulateButton.disabled =
            false;

    }


    if (
        stopButton
    ) {

        stopButton.style.display =
            "none";

    }


    /*
     * Status.
     */

    updateSimulationInfo(
        0,
        0,
        "-",
        "-",
        "-",
        "-",
        "-",
        "Stopped"
    );

}


/* =========================================================
   FINISH LOADING SIMULATION
   ========================================================= */

function finishLoadingSimulation() {

    loadingSimulationRunning =
        false;


    if (
        loadingSimulationTimer
    ) {

        clearTimeout(
            loadingSimulationTimer
        );

        loadingSimulationTimer =
            null;

    }


    /*
     * Remove pallet.
     */

    if (
        simulationPalletElement
    ) {

        simulationPalletElement.remove();

        simulationPalletElement =
            null;

    }


    clearSimulationTarget();


    /*
     * Buttons.
     */

    const simulateButton =
        document.getElementById(
            "btnSimulateLoading"
        );


    const stopButton =
        document.getElementById(
            "btnStopSimulation"
        );


    if (
        simulateButton
    ) {

        simulateButton.disabled =
            false;

    }


    if (
        stopButton
    ) {

        stopButton.style.display =
            "none";

    }


    /*
     * Final status.
     */

    updateSimulationInfo(

        loadingSimulationData.length,

        loadingSimulationData.length,

        "-",

        "-",

        "-",

        "-",

        "-",

        "Loading Complete"

    );

}


/* =========================================================
   UPDATE SIMULATION INFORMATION
   ========================================================= */

function updateSimulationInfo(
    step,
    total,
    pallet,
    location,
    layer,
    column,
    row,
    status
) {

    const simulationStep =
        document.getElementById(
            "simulationStep"
        );


    const simulationPallet =
        document.getElementById(
            "simulationPallet"
        );


    const simulationCurrent =
        document.getElementById(
            "simulationCurrent"
        );


    const simulationTarget =
        document.getElementById(
            "simulationTarget"
        );


    const simulationLayer =
        document.getElementById(
            "simulationLayer"
        );


    const simulationColumn =
        document.getElementById(
            "simulationColumn"
        );


    const simulationRow =
        document.getElementById(
            "simulationRow"
        );


    const simulationStatus =
        document.getElementById(
            "simulationStatus"
        );


    if (
        simulationStep
    ) {

        simulationStep.innerText =
            step +
            " / " +
            total;

    }


    if (
        simulationPallet
    ) {

        simulationPallet.innerText =
            pallet ||
            "-";

    }


    if (
        simulationCurrent
    ) {

        simulationCurrent.innerText =
            location ||
            "-";

    }


    if (
        simulationTarget
    ) {

        simulationTarget.innerText =
            location ||
            "-";

    }


    if (
        simulationLayer
    ) {

        simulationLayer.innerText =
            layer ??
            "-";

    }


    if (
        simulationColumn
    ) {

        simulationColumn.innerText =
            column ??
            "-";

    }


    if (
        simulationRow
    ) {

        simulationRow.innerText =
            row ??
            "-";

    }


    if (
        simulationStatus
    ) {

        simulationStatus.innerText =
            status ||
            "-";

    }

}


/* =========================================================
   CREATE LOADING PATH
   ========================================================= */

function createLoadingPath(
    targetRow,
    targetColumn
) {

    const path = [];


    /*
     * Start loading side.
     */

    path.push({

        row: 0,

        column: 0,

        loadingSide: true

    });


    /*
     * Horizontal movement.
     */

    let currentColumn =
        0;


    const columnStep =
        targetColumn >= currentColumn
            ? 1
            : -1;


    while (
        currentColumn !==
        targetColumn
    ) {

        currentColumn +=
            columnStep;


        path.push({

            row: 0,

            column:
                currentColumn

        });

    }


    /*
     * Vertical movement.
     */

    let currentRow =
        0;


    const rowStep =
        targetRow >= currentRow
            ? 1
            : -1;


    while (
        currentRow !==
        targetRow
    ) {

        currentRow +=
            rowStep;


        path.push({

            row:
                currentRow,

            column:
                targetColumn

        });

    }


    return path;

}


/* =========================================================
   MOVE PALLET TO CELL
   ========================================================= */

function movePalletToCell(
    row,
    column
) {

    return new Promise(
        function (resolve) {

            if (
                !simulationPalletElement
            ) {

                resolve();

                return;

            }


            /*
             * Loading side.
             */

            if (
                row === 0 &&
                column === 0
            ) {

                const loadingSide =
                    document.getElementById(
                        "loadingSide"
                    );


                if (
                    loadingSide
                ) {

                    movePalletToElement(
                        loadingSide,
                        resolve
                    );

                    return;

                }

            }


            /*
             * Normal cell.
             */

            const cell =
                document.querySelector(
                    '.location[data-row="' +
                    row +
                    '"][data-column="' +
                    column +
                    '"]'
                );


            if (!cell) {

                resolve();

                return;

            }


            movePalletToElement(
                cell,
                resolve
            );

        }
    );

}


/* =========================================================
   MOVE PALLET TO ELEMENT
   ========================================================= */

function movePalletToElement(
    element,
    callback
) {

    if (
        !simulationPalletElement ||
        !element
    ) {

        callback();

        return;

    }


    const rect =
        element.getBoundingClientRect();


    const left =
        rect.left +
        (
            rect.width /
            2
        ) -
        21;


    const top =
        rect.top +
        (
            rect.height /
            2
        ) -
        21;


    simulationPalletElement
        .classList.add(
            "moving"
        );


    simulationPalletElement.style.left =
        left +
        "px";


    simulationPalletElement.style.top =
        top +
        "px";


    setTimeout(
        function () {

            if (
                simulationPalletElement
            ) {

                simulationPalletElement
                    .classList.remove(
                        "moving"
                    );

            }


            callback();

        },
        500
    );

}


/* =========================================================
   MARK SIMULATION PATH
   ========================================================= */

function markSimulationPath(
    row,
    column
) {

    const cell =
        document.querySelector(
            '.location[data-row="' +
            row +
            '"][data-column="' +
            column +
            '"]'
        );


    if (cell) {

        cell.classList.add(
            "simulation-path"
        );

    }

}


/* =========================================================
   MARK SIMULATION TARGET
   ========================================================= */

function markSimulationTarget(
    row,
    column
) {

    const cell =
        document.querySelector(
            '.location[data-row="' +
            row +
            '"][data-column="' +
            column +
            '"]'
        );


    if (!cell) {

        return;

    }


    cell.classList.remove(
        "simulation-current"
    );


    cell.classList.remove(
        "simulation-path"
    );


    cell.classList.add(
        "simulation-target"
    );


    /*
     * Scroll target into view.
     */

    cell.scrollIntoView({
        behavior: "smooth",
        block: "center",
        inline: "center"
    });

}


/* =========================================================
   HIGHLIGHT LOADING SIDE
   ========================================================= */

function highlightLoadingSide() {

    const loadingSide =
        document.getElementById(
            "loadingSide"
        );


    if (
        loadingSide
    ) {

        loadingSide.classList.add(
            "simulation-loading"
        );

    }

}


/* =========================================================
   SLEEP
   ========================================================= */

function sleep(
    milliseconds
) {

    return new Promise(
        function (resolve) {

            setTimeout(
                resolve,
                milliseconds
            );

        }
    );

}


/* =========================================================
   DEBUG
   ========================================================= */

console.log(
    "ASRS_Monitor.js loaded successfully"
);