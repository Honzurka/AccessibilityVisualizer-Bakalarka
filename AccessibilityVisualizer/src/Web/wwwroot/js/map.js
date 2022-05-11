// os layers modules
const VectorSource = ol.source.Vector;
const ToLonLat = ol.proj.toLonLat;
const FromLonLat = ol.proj.fromLonLat;
const Feature = ol.Feature;
const Point = ol.geom.Point;
const WebGLPointsLayer = ol.layer.WebGLPoints;
const TileLayer = ol.layer.Tile;
const VectorLayer = ol.layer.Vector;
const Style = ol.style.Style;
const Icon = ol.style.Icon;
const Text = ol.style.Text;
const Fill = ol.style.Fill;
const Stroke = ol.style.Stroke;
const OSM = ol.source.OSM;
const Map = ol.Map;
const View = ol.View;
const Modify = ol.interaction.Modify;

// constants
const FormId = 'FinderForm';
const FormRowIdBase = 'FinderFormRow';
const MarkerNumKey = 'label';
const MarkerIconPath = 'data/icon.png'
const MapDivId = "map";
const LatitudeField = "Latitude";
const LongitudeField = "Longitude";
const WeightField = "weight";
const DateField = "date";
const StopSearchFormId = "StopSearchForm";
const MostAccessibleStopsId = "MostAccessibleStops";
const CalculateStatistics = "CalculateStatistics";
const VisualizationType = "visualizationType";

const SecInDay = 86400;
const SecInMin = 60;

const HighlightStyle = 'bg-danger';

/**
 * Represents form with target selected by user.
 */
class Form {
    constructor(formId) {
        this.form = document.getElementById(formId);
        this.weightMin = 0.1; //separate checks exist on server side
        this.weightMax = 10;
        this.weightStep = 0.1;
    }

    /**
     * Adds field representing target data to dynamic form.
     * 
     * @param {number[]} coords
     * @param {number} markerNum
     */
    addField([lon, lat], markerNum) {
        function addDate(parent, allowedDates) {
            let date = htmlToElement(`<div class="row"><input name="${DateField}" id="${DateField}${markerNum}-${dateIdx}" class="offset-md-2"
                    type="datetime-local" value="${currentDatetime}" min="${allowedDates.dataset.startDate}"
                    max="${allowedDates.dataset.endDate}" required></div>`);
            dateIdx++;
            parent.append(date);
        }

        function removeDate(parent) {
            const dates = parent.querySelectorAll(`[name=${DateField}]`);
            if (dates.length !== 1) {
                dates[dates.length - 1].remove();
            }
        }

        const allowedDates = document.getElementById('allowedDates');
        let div = htmlToElement(`<div id="${FormRowIdBase}${markerNum}" name="${FormRowIdBase}"></div>`);

        let latitude = `<input name="${LatitudeField}" value="${lat}" type="hidden">`;
        let longitude = `<input name="${LongitudeField}" value="${lon}" type="hidden">`;
        let weightLabel = `<label for="${WeightField}${markerNum}">vaha</label>`;
        let weight = `<input name="${WeightField}" id="${WeightField}${markerNum}" type="number"
                     value="1" min="${this.weightMin}" max="${this.weightMax}" step="${this.weightStep}" required="">`;

        let dateIdx = 0; //used for unique id of date
        const currentDatetime = function () {
            const d = new Date();
            return new Date(d.getTime() - d.getTimezoneOffset() * 60000).toISOString().substr(0, 16);
        }();

        let bPlus = htmlToElement('<button type="button" class="btn btn-success">+</button>');
        let bMinus = htmlToElement('<button type="button" class="btn btn-danger">-</button>');

        bPlus.onclick = () => { addDate(div, allowedDates); };
        bMinus.onclick = () => { removeDate(div) };

        div.innerHTML = `#${markerNum}: ` + latitude + longitude + weightLabel + weight;

        let dateLabel = htmlToElement(`<label for="${DateField}${markerNum}-${dateIdx}">datum</label>`);
        let date = htmlToElement(`<input name="${DateField}" id="${DateField}${markerNum}-${dateIdx}"
                    type="datetime-local" value="${currentDatetime}" min="${allowedDates.dataset.startDate}"
                    max="${allowedDates.dataset.endDate}" required>`);
        div.append(dateLabel);
        div.append(date);
        div.append(bPlus);
        div.append(bMinus);

        this.form.prepend(div);
    }

    removeField(markerNum) {
        document.getElementById(`${FormRowIdBase}${markerNum}`).remove();
    }

    /**
     * Submits form with selected targets.
     * 
     * @param {Event} submitEvt
     * @param {OLMap} olmap
     */
    submit(submitEvt, olmap) {
        /**
         * Highlights target that was unreachable by any trip.
         * 
         * @param {Promise<string>} targetPromise
         * @param {HTMLElement} form
         */
        async function unreachableTargetHandler(targetPromise, form) {
            removeRowHighlight(form);

            const target = JSON.parse(await targetPromise);
            const targetRow = form.children[target.TargetIdx];
            targetRow.classList.add(HighlightStyle);
            
            alert("Nedostupné vstupní místo.");
        }
        function removeRowHighlight(form) {
            form.querySelectorAll(`[name=${FormRowIdBase}]`).forEach(c => c.classList.remove(HighlightStyle));
        }

        /**
         * Parses data from form rows.
         * 
         * @returns {[Object]} Array of targets with targetData.
         */
        function getTargets() {
            let result = [];
            const formRows = submitEvt.target.querySelectorAll(`[name=${FormRowIdBase}]`);
            for (let row of formRows) {
                let newTarget = {};

                newTarget.Latitude = row.querySelector(`[name=${LatitudeField}]`).value;
                newTarget.Longitude = row.querySelector(`[name=${LongitudeField}]`).value;
                newTarget.Weight = row.querySelector(`[name=${WeightField}]`).value;
                newTarget.Dates = [];
                row.querySelectorAll(`[name=${DateField}]`).forEach(d => newTarget.Dates.push(new Date(d.value)));

                result.push(newTarget);
            }
            return result;
        }

        /**
         * Visualizes data from response on map.
         * Displays most accessible stops.
         * Creates accessibility filter.
         * 
         * @param {string} response
         * @param {OLMap} olmap
         * @param {HTMLElement} form
         */
        function handleResponse(response, olmap, form) {
            /**
             * Configures HTML element accessibilityFilter to filter accessibility.
             * 
             * @param {number} minAccess
             * @param {number} maxAccess
             * @param {Object} vars
             * @param {number} vars.accessibilityLimit Currently set accessibility limit.
             * @param {Map} map
             */
            function createAccessibilityFilter(minAccess, maxAccess, vars, map) {
                const div = document.getElementById("accessibilityFilter");
                const range = div.firstElementChild;
                const input = range.nextElementSibling;

                div.hidden = false;
                input.value = maxAccess / SecInMin;
                input.max = maxAccess / SecInMin;
                range.value = 100;

                const coef = (maxAccess - minAccess) / 100; //1 percent

                range.oninput = function () {
                    const val = Math.round(minAccess + coef * this.value);
                    input.value = Math.round(val / SecInMin);
                    vars.accessibilityLimit = val;
                    map.render();
                };

                input.oninput = function () {
                    range.value = (SecInMin * this.value - minAccess) / coef;
                    vars.accessibilityLimit = SecInMin * Number(this.value);
                    map.render();
                }
            }

            /**
             * Creates table with most accessible stops.
             * 
             * @param {[Object]} stops
             * @param {number} count Limits number of visible stops.
             */
            function showMostAccessibleStops(stops, count = 10) {
                let table = document.getElementById(MostAccessibleStopsId);
                table.innerHTML = 
                    `<tr>
                        <th scope="col">Název zastávky</th>
                        <th scope="col">Zeměpisná šířka</th>
                        <th scope="col">Zeměpisná délka</th>
                        <th scope="col">Dostupnost(sec)</th>
                    </tr>`;
                
                let bestStops = stops.sort((fst, snd) => { return fst.totalSec - snd.totalSec; }).slice(0, count);
                for (stop of bestStops) {
                    const tr = table.insertRow();
                    let td;

                    td = tr.insertCell();
                    td.appendChild(document.createTextNode(stop.name));

                    td = tr.insertCell();
                    td.appendChild(document.createTextNode(stop.latitude));

                    td = tr.insertCell();
                    td.appendChild(document.createTextNode(stop.longitude));

                    td = tr.insertCell();
                    td.appendChild(document.createTextNode(stop.totalSec));
                }

            }

            removeRowHighlight(form);
            const responseArr = JSON.parse(response);
            showMostAccessibleStops(responseArr);

            //const maxAccess = 86400 * 3; const minAccess = 0; //used for Germany
            const maxAccess = Math.min(SecInDay, Math.max(...responseArr.map(o => o.totalSec)));
            const minAccess = Math.min(...responseArr.map(o => o.totalSec));
            const vars = { //shared between map and slider => slider changes are visible in map
                accessibilityLimit: Math.round(maxAccess)
            };

            createAccessibilityFilter(minAccess, maxAccess, vars, olmap.map);
            olmap.addVisualisationLayer(responseArr, vars);
        }

        submitEvt.preventDefault();

        // disables submit button during calculation
        submitEvt.submitter.disabled = true;

        const targets = getTargets();
        if (targets.length === 0) return;

        let fd = new FormData();
        fd.append("jsonData", JSON.stringify(targets));
        fd.append("visualizationType", document.getElementById(VisualizationType).value);
        fd.append("calculateStatistics", document.getElementById(CalculateStatistics).checked);

        fetch("", {
            headers: {
                RequestVerificationToken: document.getElementsByName("__RequestVerificationToken")[0].value
            },
            method: 'post',
            body: fd
        })
        .then(response => {
            if (response.status === 400) {
                submitEvt.submitter.disabled = false;
                unreachableTargetHandler(response.text(), this.form);
                throw "Unreachable target detected";
            }
            else {
                return response.text();
            }
        })
        .then(responseText => {
            handleResponse(responseText, olmap, this.form);
            submitEvt.submitter.disabled = false;
        })
        .catch(e => console.log(e));
    }
}

/**
 * Represents interactive map provided by OpenLayers.
 */
class OLMap {
    /**
     * Creates map and prepares visualisation layers.
     */
    constructor() {
        /**
         * Created map with limited extent.
         * Adds marker layer and visualisation layer to map.
         * 
         * @param {VectorLayer} markerLayer
         * @param {WebGLPointsLayer} visualizationLayer
         */
        function createMap(markerLayer, visualizationLayer) {
            const OSMLayer = new TileLayer({
                source: new OSM()
            });

            const stopPositionLimits = document.getElementById('stopPositionLimits');

            // Limits the visible extent of a map
            const [minX, minY] = FromLonLat([stopPositionLimits.dataset.minLon, stopPositionLimits.dataset.minLat]);
            const [maxX, maxY] = FromLonLat([stopPositionLimits.dataset.maxLon, stopPositionLimits.dataset.maxLat]);
            const broaderExtent = 100000;

            return new Map({
                view: new View({
                    center: [(minX + maxX) / 2, (minY + maxY) / 2],
                    zoom: 0,
                    extent: [minX - broaderExtent, minY - broaderExtent, maxX + broaderExtent, maxY + broaderExtent],
                }),
                layers: [
                    OSMLayer,
                    markerLayer,
                    visualizationLayer
                ],
                target: MapDivId
            });
        }

        /**
         * Updates target coords in form after marker is moved.
         * 
         * @param {Modify} modify
         */
        function updateFormOnMarkerMovement(modify) {
            modify.on('modifyend', function (e) {
                let target;
                e.features.forEach(f => target = f);

                const [lon, lat] = ToLonLat(target.getGeometry().flatCoordinates);

                document.getElementsByName(LatitudeField)[0].value = lat;
                document.getElementsByName(LongitudeField)[0].value = lon;

            });

        }

        /**
         * Changes cursor skin.
         * 
         * @param {Modify} modify
         */
        function changeCursorOnMarkerMovement(modify) {
            const mapDiv = document.getElementById(MapDivId);
            modify.on(['modifystart', 'modifyend'], function (e) {
                mapDiv.style.cursor = e.type === 'modifystart' ? 'grabbing' : 'pointer';
            });
            modify.getOverlay().getSource().on(['addfeature', 'removefeature'], function (e) {
                mapDiv.style.cursor = e.type === 'addfeature' ? 'pointer' : '';
            });
        }

        /**
         * Configures how are points with accessibility visualized.
         * 
         * @param {VectorSource} visualizationSrc
         * @returns {WebGLPointsLayer}
         */
        function createVisualisationLayer(visualizationSrc) {
            const sizeExpBase = 2.5;
            const sizeInterpolP0 = 0;
            const sizeInterpolV0 = 2;
            const sizeInterpolP1 = 16;
            const sizeInterpolV2 = 20;

            const colorInterpolV0 = "#FFFF00"; //yellow
            const colorInterpolV1 = '#FF8000'; //orange
            const colorInterpolV2 = '#FF0000'; //red
            const colorInterpolV3 = '#000000'; //black

            const result = new WebGLPointsLayer({
                source: visualizationSrc,
                style: {
                    variables: {
                        accessibilityLimit: 0
                    },
                    filter: ['<=', ['get', 'accessibility'], ['var', 'accessibilityLimit']],
                    symbol: {
                        symbolType: "circle",
                        size: [
                            "interpolate",
                            [
                                "exponential",
                                sizeExpBase
                            ],
                            [
                                "zoom",
                            ],
                            sizeInterpolP0,
                            sizeInterpolV0,
                            sizeInterpolP1,
                            sizeInterpolV2,
                        ],
                        color: [
                            "interpolate",
                            [
                                "linear"
                            ],
                            [
                                "get",
                                "accessibility"
                            ],
                            0,
                            colorInterpolV0,
                            ['*', ['var', 'accessibilityLimit'], 0.1],
                            colorInterpolV1,
                            ['*', ['var', 'accessibilityLimit'], 0.5],
                            colorInterpolV2,
                            ['var', 'accessibilityLimit'],
                            colorInterpolV3,
                        ],
                        opacity: 0.8,
                    }
                }
            });
            return result;
        }

        this.vecSrc = new VectorSource();
        this.markerLayer = new VectorLayer({
            source: this.vecSrc,
            zIndex: 1,
        });

        this.visualizationSrc = new VectorSource();
        this.visualizationLayer = createVisualisationLayer(this.visualizationSrc);

        this.map = createMap(this.markerLayer, this.visualizationLayer);

        // marker interaction
        const modify = new Modify({
            source: this.vecSrc,
            hitDetection: this.markerLayer
        });

        updateFormOnMarkerMovement(modify);
        changeCursorOnMarkerMovement(modify);
        this.map.addInteraction(modify);

        // used for form target identification
        this.markerCounter = 1;
    }

    /**
     * Adds marker to specific location.
     * Adds row with new target to form.
     * 
     * @param {Form} form
     * @param {[number} coords In EPSG:3857 (WGS84) system.
     */
    addMarker(form, coords) {
        /**
         * Configures marker image and description.
         * 
         * @param {Feature} feature
         */
        function styleFunc(feature) {
            return new Style({
                image: new Icon({
                    anchor: [0.5, 40], // bottom-middle of marker icon (based on icon img size)
                    anchorXUnits: 'fraction',
                    anchorYUnits: 'pixels',
                    src: MarkerIconPath,
                }),

                text: new Text({
                    font: '20px Arial',
                    fill: new Fill({ color: '#000' }),
                    stroke: new Stroke({
                        color: '#fff', width: 2
                    }),
                    text: feature.get(MarkerNumKey),
                    offsetY: 10,
                })
            })
        }

        form.addField(ToLonLat(coords), this.markerCounter);
        let p = new Feature({
            geometry: new Point(coords),
            label: `${this.markerCounter++}`,
        });

        p.setStyle(styleFunc(p));
        this.vecSrc.addFeature(p);
    }

    /**
     * Reaction to click on map.
     * If there is marker under cursor => removes marker.
     * Otherwise adds new marker.
     * 
     * @param {Event} clickEvt
     * @param {Form} form
     */
    addOrRemoveMarker(clickEvt, form) {
        function removeMarker(form, clickedFeature, olmap) {
            form.removeField(clickedFeature.get(MarkerNumKey));
            olmap.vecSrc.removeFeature(clickedFeature);
        }

        const clickedFeature = this.map.forEachFeatureAtPixel(clickEvt.pixel,
            feature => { return feature; },
            {
                layerFilter: layer => { return layer === this.markerLayer; }
            }
        );
        if (clickedFeature) {
            removeMarker(form, clickedFeature, this);
        }
        else {
            this.addMarker(form, clickEvt.coordinate);
        }
    }

    /**
     * Adds layer visualizing points with accessibility.
     * Removes/adds points depending on accessibililty limit set through vars.accessibilityLimit.
     * 
     * @param {Array} responseArr Contains points with accessibility.
     * @param {Object} vars
     * @param {number} vars.accessibilityLimit Currently set accessibility limit.
     */
    addVisualisationLayer(responseArr, vars) {
        function getFeatures(responseArr) {
            const result = [];
            for (let coordsAndAccessibility of responseArr) {
                const coords = FromLonLat([coordsAndAccessibility.longitude, coordsAndAccessibility.latitude]);
                result.push(
                    new Feature({
                        geometry: new Point(coords),
                        accessibility: coordsAndAccessibility.totalSec
                    })
                );
            }
            return result;
        }

        this.visualizationSrc.clear();
        this.visualizationSrc.addFeatures(getFeatures(responseArr));
        this.visualizationLayer.getProperties().style.variables = vars;
    }
}

window.onload = init;

function init() {
    const form = new Form(FormId);
    const olmap = new OLMap();
    
    form.form.addEventListener("submit", e => form.submit(e, olmap));
    olmap.map.on('singleclick', e => olmap.addOrRemoveMarker(e, form));
    document.getElementById(StopSearchFormId).addEventListener("submit", e => addStop(e, form, olmap));
}

/**
 * Helper function for converting HTML text to HTML element.
 * 
 * @param {any} html Input text
 * @returns {ChildNode}
 */
function htmlToElement(html) {
    let template = document.createElement('template');
    template.innerHTML = html.trim();
    return template.content.firstChild;
}

/**
 * Handles adding stop by selecting its' name from list.
 * 
 * @param {Event} e
 * @param {Form} form
 * @param {OLMap} olmap
 */
function addStop(e, form, olmap) {
    e.preventDefault();
    const stopId = e.target.getElementsByTagName('select')[0].value;

    fetch(`/Finder?handler=stopid&stopId=${stopId}`, {

        method: 'get'
    })
        .then(response => response.text())
        .then(jsonCoords => {
            let coords = JSON.parse(jsonCoords);
            olmap.addMarker(form, FromLonLat([coords.longitude, coords.latitude]));
        })
        .catch(e => console.log(e));
}