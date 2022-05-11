# Notes
- get maps working
    1. include openlayers
        -  `<script src="https://cdn.jsdelivr.net/gh/openlayers/openlayers.github.io@master/en/v6.12.0/build/ol.js"></script>`
            - whole lib, in production: create own lib of used modules
    2. `<div id="map" class="map"></div>`
        - CSS sets width, height, ...
    3. create map with js
        ```js
        var map = new ol.Map({  // map without interaction
            target: 'map',      // attacing obj to div
            layers: [           // layers available to map
            new ol.layer.Tile({ // in EPSG:3857
                source: new ol.source.OSM()
            })
            ],
            view: new ol.View({ // specifies center, resolution, rotation
            center: ol.proj.fromLonLat([37.41, 8.82]), // EPSG:4326 == WGS84
            zoom: 4 // 0 == zoomed out
            })
        });
        ```
- building app
    - `npx create-ol-app`
    - `npm start`
    - `npm run build`
- map properties
    - ctor
    - setter methods
- view:
    - projection - EPSG:3857 (default)
    - center
    - zoom (default 0)
        - maxZoom (default: 28)
        - zoomFactor (default: 2)
        - maxResolution (default is calculated in such a way that the projection's validity extent fits in a 256x256 pixel tile)
- source: `ol/source/SOURCE`
    - tile service: OSM
    - OGC source: WMS, WMTS (protocol for serving map data)
    - vector data formats: GeoJSON
- layer: `ol/layer/LAYER`
    - tile
    - img
    - vector
    - vectorTile: 
- from workshop
    - rendering geojson(vector data) on map: https://openlayers.org/workshop/en/vector/geojson.html
        - could be used for colorizing maps: https://openlayers.org/workshop/en/vector/style.html + https://www.npmjs.com/package/colormap
            - napad: rozrezu mapu na kousky urciteho rozliseni, pro stred kazdeho z bodu urcim dostupnost, podle dostupnosti barvu
    - download button for created geojson: https://openlayers.org/workshop/en/vector/download.html
    - visualization chooser: https://openlayers.org/workshop/en/cog/visualizations.html
    - point rendering
        - Canvas 2D
            - ex. rucni parsovani csv
            - multiple layers = drawn on top of each other
                - maji Z-index (poradi prekryvu)
        - WebGL: lepsi pro vice bodu (omezeno jen na body??)
- from examples
    1. GOAL - marker na mape po kliknuti, ziskat jeho souradnice
        - DONE: vytvoreni + posunuti markeru: https://openlayers.org/en/latest/examples/draw-and-modify-features.html
        - draw/modify mod pro body: https://openlayers.org/en/latest/examples/snap.html
            - spise nezajimave
        - ikona misto tecky: https://openlayers.org/en/latest/examples/modify-icon.html

        - preosuvani markeru: https://openlayers.org/en/latest/examples/custom-interactions.html
            - subclass ol/interaction/Pointer (hodne customizovatelne)
            - ok, ale pracnejsi nez vyuzit hotove
        - reakce na klik na ikonu-prebarveni: https://openlayers.org/en/latest/examples/icon-negative.html
        - reakce na klik na ikonu-zobrazeni labelu: https://openlayers.org/en/latest/examples/icon.html
    2. omezeni velikosti mapy / ohraniceni
        https://openlayers.org/en/latest/examples/extent-constrained.html
    3. vyhodnoceni: prekryvny barevny filtr pres mapu - nebo barevne tecky (zastavky/rozliseni)
        - webgl ops: https://openlayers.org/en/latest/examples/webgl-points-layer.html
    4. real-time ziskavani souradnice mysi na mape => pro vyhodnoceni souradnic
        - https://openlayers.org/en/latest/examples/mouse-position.html
    4. zbytek
        - prebarveni pixelu mapy: https://openlayers.org/en/latest/examples/color-manipulation.html
        - GOAL 2: omezeni velikosti mapy: https://openlayers.org/en/latest/examples/extent-constrained.html
        - hit detection - snad dobry zaklad pro pozici kurzoru: https://openlayers.org/en/latest/examples/custom-hit-detection-renderer.html
        - point + blur => fluency: https://openlayers.org/en/latest/examples/heatmap-earthquakes.html
        - pocita souradnice stredu(~neni oficialni?): https://openlayers.org/en/latest/examples/geographic.html
        - GeoJson - pridavani barevnych objektu (barevne ctverce pro GOAL 3): https://openlayers.org/en/latest/examples/geojson.html
        - hit tolerance => pozice kurzoru: https://openlayers.org/en/latest/examples/hit-tolerance.html
        - switching layer visibility on/off: https://openlayers.org/en/latest/examples/layer-group.html
        - reprojekce OSM do WGS84(OSM jsou ve WGS84 - k cemu to je???): https://openlayers.org/en/latest/examples/reprojection-wgs84.html
        - transparent layer(MapBox:( - maybe useless) over OSM: https://openlayers.org/en/latest/examples/semi-transparent-layer.html
        - gradient: https://openlayers.org/en/latest/examples/canvas-gradient-pattern.html
        - labels?: https://openlayers.org/en/latest/examples/vector-labels.html
- vids
    - map on-click event: https://www.youtube.com/watch?v=_pbB4Po6Hyg&list=PLSWT7gmk_6LrvfkAFzfBpNckEsaF7S2GB&index=6&ab_channel=KhwarizmiMedia-WebDevelopment%26GISTutorials
    - vector-layer styling: https://www.youtube.com/watch?v=kgVEAKhuRDc&list=PLSWT7gmk_6LrvfkAFzfBpNckEsaF7S2GB&index=9&ab_channel=KhwarizmiMedia-WebDevelopment%26GISTutorials
    - vector feature interaction: https://www.youtube.com/watch?v=XUCDqzoUh6Y&list=PLSWT7gmk_6LrvfkAFzfBpNckEsaF7S2GB&index=10&ab_channel=KhwarizmiMedia-WebDevelopment%26GISTutorials