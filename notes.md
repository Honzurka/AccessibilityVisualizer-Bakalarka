[27.3.2021]
Rozsireni: Hledani idealniho mista k bydleni | Pocitat s beznym zpozdenim
Cil: na mape mit zobrazenou dojezdovou dobu mezi lib 2 misty
    aplikaci co nejobecnejsi
        aby slo aplikovat i na data jineho mesta
    rozumne rychle
        nacachovat si dobu mezi bloky cca 1km^2 a zbytky aproximovat
            mrizka zavisla na hustote stanic/frekvence odjezdu...
Postup:
    0) Co obsahuji data - staci mi to?
    1) V x hodin z 1 mista na 2. misto 
    2) Z 1 mista na 2. misto v x hodin
    -3) Vyhledavani tras mezi 2 misty - lib cas
    -4) Vyhledavani tras mezi 2 misty - lib cas
    -5) Z 1 mista na libovolne misto(v Praze)
    -6) Odkudkoliv kamkoliv - 

Data
    https://pid.cz/o-systemu/opendata/
    Praha - https://www.geoportalpraha.cz/cs/data/otevrena-data/seznam
    https://www.openstreetmap.org/
        hledani silnic, chodniku, budov,...
    GRFS svet: https://transitfeeds.com/
    google transit: https://developers.google.com/transit
        maji github - da se napojit na data!!!
    https://gtfs.org/

Routing
    napr. https://github.com/joshchea/gtfs-route-server

Podobne prace
    SIS-transport
    https://is.cuni.cz/webapps/zzp/detail/205705/
    https://is.cuni.cz/webapps/zzp/detail/190783/

-------------------------------------------------------------------------------
***GTFS
https://www.youtube.com/watch?v=WtD08ckCaIY&ab_channel=VirginiaTransit
    required files
        agency
            PID v mem pripade
            klic `agency_id`
        routes
            obs agency_id
            obs url linky -> pdf
        trips
            obs route_id, service_id, shape_id(path)
        stop_times == jizdni rad
            obs trip_id
            kde stavi, poradi, cas
        calendar
            yes/no - kdy jezdi
            klic service_id
            ?? v mych datech cisla >1 -> precist dokumentaci
        shapes
            body latitude, longtitude, poradi propojeni: propojim primou a dostanu trasu
        stops
            popis zastavky - bod
        calendar_dates
            service vyjimky - prazdniny,...

    extrakce dat
        routes,trips,stop times,calendar
            pres trips propojim zbyle dle id
        stop_times->arrival_time pro group
        2priklady v excelu: vyhledavani v datech--^

    Vizualizace
        stops,stop_times,routes,shapes
        GIS software-obecny nazev(konkretne vid:arcmap)
            x,y na bod
            body na line
            join
        ex. routes from shapes
        ex. bus stop locations

vizualizace: https://github.com/cmichi/gtfs-visualizations - pretekla heap..., nefunguje

[28.3.2021]
https://developers.google.com/transit/gtfs
    samples
        ucici datova sada
        moc nerozumim examplum
    reference
        detailni popis filu

https://github.com/google/transitfeed/wiki
    pomocne programky
        verifikace, tvorba, cisteni dat
        KMLWriter - prevod do KML(vizualizace napr. v Google Earth)
        ScheduleViewer - vizualizace
            python2, v prohlizeci pada(na win i lin)
            potrebuju mit google cloud API KEY...

[5.4.2021]
    ucim se s pandas
        getting started(proctene vsechny sekce)
            - na konci kazde sekce summary
        - kazdy plot je Matplotlib obj

[6.4.2021]
    geopandas
        getting started
            - existuje buffer-------------------------nezkouseno
        user guide
            making maps and plots
                Maps with layers
                    mapy musi mit stejne CRS
            managing projections
                info o CRS
                    WGS84 Latitude/Longitude: "EPSG:4326"
                    set proj: .set_crs()
                    reprojection: to_crs()
                    ...
            data structures
                objekty knihovny `shapely`
                    points,lines,polygons (+ multi-||-)
                set_geometry - ktery sloupec ma vykreslovat
                lze menit presnost
        examples gallery
            Creating a GeoDataFrame from a DataFrame with coordinates
                prevod longtitude,latitude na point + vyber kontinentu(mapy sveta geopandas nejsou detailni)
            Adding a background map to plots
                crs: epsg=3857
                vyuziva knihovnu `contextily`
                    lze menit source: OpenStreetMap,Stamen,...




----------------------------------------------------------------------------------------------------------------
            Plotting with folium
            An example of polygon plotting with folium
            https://geopandas.readthedocs.io/en/latest/gallery/spatial_joins.html ?

#region clipping--------------




geopandas:
    http://darribas.org/gds15/content/labs/lab_03.html
        od nejakeho ucitele
    https://gis.stackexchange.com/questions/362504/plotting-a-csv-with-coordinates-one-row-at-a-time-via-geopandas
        stack...
    
    openstreetmap - exportuji OSM => zakomponovat do geopandas???
        https://automating-gis-processes.github.io/CSC18/lessons/L3/retrieve-osm-data.html
    adding background to map:
        https://geopandas.readthedocs.io/en/latest/gallery/plotting_basemap_background.html
-------------------------------------------------------------------------------

map plot lib
    folium - asi pomale
    basemap
    https://stanmart.github.io/geodata/datashader/python/plotting/budapest/2018/10/17/bkk-datashader/
    http://simplistic.me/playing-with-gtfs-iii-geo-graphs.html


gtfs trip planning
    todo

-------------------------------------------------------------------------------
# [8.9.2021] Presun na C#
## Nalezeno:
    - CsvHelper (pomalejsi, ale user friendly)
    - .NET OpenSource route planning: http://www.itinero.tech/
        - chybi dokumentace k GTFS, navic neni nutne kompatibilni s CORE
        - pracuje s OSM/shapefile daty ==> obecne routovani, ne jen bus
        - soucast: `OsmSharp` - prace s OpenStreetMap daty
    - OpenTripPlanner
        - psany v Jave, zbytecne slozity kod

RAPTOR-Round-Based Public Transit Optimized Router algo: https://www.microsoft.com/en-us/research/wp-content/uploads/2012/01/raptor_alenex.pdf

## Reading scientific paper: https://www.youtube.com/watch?v=IeaD0ZaUJ3Y&t=17s
1)title and keywords
2)abstract
3)conclusion

4)tables and figues + captions
5)introduction
6)result+discussion
7)experimental

+)notes: avoid rereading

# [9.9.2021] pokracovani s RAPTORem
implementace datovych struktur
snaha o implementaci algoritmu

# [10.9.2021] pokracovani s RAPTORem
snaha o hlubsi pochopeni fungovani algoritmu
    - snazit se o implementaci bez detailni znalosti algoritmu nebylo moudre

# [13.9.2021]
inspirace implementaci v Jave psanou nekym z FITu - zdroj: https://gitlab.fel.cvut.cz/kasnezde/raptor
pojem `route` chapan jinak v GTFS a popisu RAPTORu
    - GTFS route - trasa, ale trip muze byt jen podmnozinou vsech zastavek
pojem `transfers` chapan jinak v GTFS a popisu RAPTORu
    - GTFS transfers: nejsou vsechny mozne prestupy, poze nejaka podmnozina
    - RAPTOR: lze vygenerovat dle vzdalenosti dane zemepisnymi souradnicemi (nejspise jen vzdusna vzdalenost, ne pesi)
        - inspirace z JAVA implementace
procteni optimalizaci pro algoritmus RAPTOR

# [14.9.2021]
Refactorovani kodu
GTFS calendar: https://transitdata.net/on-calendars-and-calendar_dates/
    - format start_date, end_date: YYYYMMDD
Generovani transfers

# [15.9.2021]
Pokracovani v refactorovani

# [16.9.2021]
Serializace do JSONu: ulozim zpracovana data, usetrim cas pri spousteni
    - c# jsonSerializer, nedokazal si poradit s cyklickymi referencemi, ani pri `ReferenceHandler.Preserve`
    - Newtonsoft.json - lepsi
        - problem se self-referenci `Stop`: vola default ctor
        - nutnost [JsonProperty] u private fieldu/proprety (u private getteru pro deserializaci!!!)

# [17.9.2021]
Unit testy
    idealne nezavisle na datech!! - nemuzu pouzit GTFS data (take by bylo pomale)
    fix bug by debugging test
    ???mocks,stubs

# [21.9.2021]
Dokonceni unit testu
Vypis journey

# [22.9.2021]
Debugovani

# [23.9.2021]
refactoring
optimalizace RAPTORu dle clanku
    cyklicke reference => stack overflow pri serializaci --- nejspise by to chtelo databazi

# [24.9.2021]
pythonni script pro vykresleni dosazitelnych zastavek
    dosazitelnost absolutni/dle poctu prestupu
    ???dosazitelnost dle casu???-----------------------
mereni rychlost hledani vsech dostupnych zastavek
    ~4.5sec

# [6.10.2021] Konzultace
- presun na gitlab
- zkusit release verzi
-------------------------------
- Chybi odliseni dnu a aktivnich/neaktivnich linek
- mozna impl db pro uchovani dat
- zlepsit hledani prestupu
-------------------------------
vyber casu: hodinove intervaly
vyber dne: kazdy den
- 1) jednodussi verze
    - vyber mist na mape => vyhledani nejdostupnejsiho mista
- 2) slozitejsi verze
    - rozrezani mapy na dily
    - beh algo z reprezentujcich zastavek (minimalni vrcholove pokryti?)
    - jak volit dily: clusterizace?
    - jak volit reprezentanta dilu: teorie grafu...
    - jak aproximovat dojezdovost v ramci dilu

# 11-12.10.2021
- mereni rychlosti (z 1 zastavky na vsechny dostupne)
    - DEBUG ~2.5s
    - RELEASE ~1.7s

- 1) jednodussi verze: vyber mist na mape => vyhledani nejdostupnejsiho mista
    - 0.1) vyber vice zastavek, z nich najdu nejdostupnejsi zastavku (soucet casu)
    - 0.2) cil: nejdostupnejsi zastavka (s obecnym mistem problem)
        - pozorovani: nejdostupnejsi misto != nejdostupnejsi zastavka
            - ex. chci na mista A,B - jsou dostupna z C,D: A <--> C  D <--> B
                - nejdostupnejsi misto je nekde mezi C a D
            - PROBLEM: scitanim casu dostupnosti na jednotlivych zastavkach zastru vyhody jednotlivych zastavek(jako C,D)
                - rozumne resitelne [SLOZITEJSI VERZI](#6102021-konzultace)
        - idealni by bylo pokryt mapu barevnou maskou(zelena dostupna, cervena nedostupna)
        - vizualizace
            - gpd colormap: https://matplotlib.org/2.0.2/users/colormaps.html
            - spatial algo
                - pouziti
                    - nejaka clusterizace pro [SLOZITEJSI VERZI](#6102021-konzultace)
                    - k nearest neighbors => **reseni 0.3**
                    - R-tree: clusterizace / hledani okoli zastavky
                    - convex/concave hull - obaleni vsech == vytyceni plochy, kde muze uzivatel neco hledat
                - implementation: https://pysal.org/viz/
                    - legendgram: https://github.com/pysal/legendgram
                        - pouziti: http://darribas.org/gds4ae/content/notebooks/03-Geovisualisation.html
    - 0.3) vyber mista mimo zastavku
        - k nejblizsich zastavek
        - 1 nejblizsi zastavka - hodne nepresne
        - zastavky do ~100m
        - potencialni problem(dlouhy vypocet): vice zastavek => vice behu algo(~2s run)

# 17.10.2021
- refactoring
- prace na (0.3) vyber mista mimo zastavku
    - jak to dela google? zkusit si precist nejake clanky / reverse engineerovat https://www.opentripplanner.org/

# 19.10.2021
- unrestricted walking?: https://hal.inria.fr/hal-02161283/document **zatim nepouzito**
    - section 2
        - public transit network: (S,T,R) - Stops, Trips, Routes
        - weighted footpath graph: G=(V,E,t-w)
            - V = S + something(ref section 5)
            - E = segment of street => could be traversed by walking
            - t-w(u,v): time needed to walk from u to v
            - d-G(u,v): length of shortest u-v path in G (min walking time)
        - computation problems
            - earliest arrival
            - #transfers +walking time => find pareto-optimal journeys
                - pareto-optimal == no journey in better in any criteria
        - two-hop labeling
            - for each node u: H-u(in-hub), H+u(out-hub) = subset of nodes
            - `two-hop property:` for any pair u,v: exists common hub `h` lying on shortest u-v path == `d-G(u,h) + d-G(h,u) = d-g(u,v)`
            - interestingly: hub set size is 100 in avg
    - section 3: HLRaptor
        - replace `ProcessingTransfers` phase with 2 subphases
            - 1. sub: scan `marked` stops & update arrival time at out-hubs to `...`
            - 2. sub: scan hubs improved in 1. & update nodes v containing scanned hubs and out-hubs to `...` 
        - target pruning optim
            - sort + precompute H-,H+ => ommit those with arrival time > best known arrival time to target (might not be useful in my case)
        - HLprRaptor: with profile queries
            - departure and arrival in **given interval**
    - section 5
        - (restricted) transfer == stop within 75meters of walk => transfer graph
        - unrestricted footpath graph extracted from http://download.geofabrik.de/ which is from https://www.openstreetmap.org/
            - TODO: `http://download.geofabrik.de/europe/czech-republic.html`
        - walking graph = unrestricted footpath graph + transfer graph
        - merge stop with walking graph
            - for each stop:
                - closest node within 5m => connect stop with node
                - otherwise: connect stop with 5 closest nodes within 100m
                - otherwise stop is isolated
        - computing hub labeling of walking graph ~ `1-2hours`
        - cca `6x slower` with unrestricted walk than with restricted
        - leads to `at least 12%` improved time compared to restricted walking
    - code(C++): https://github.com/lviennot/hl-csa-raptor/tree/master/src
        - psane v silenem C++
        - ma nejake optiony - prozkoumat
**- umozni mi to urcite moznost jet odkudkoliv kamkoliv?**
    - v tomto algoritmu pracuji s source stop a destination stop
    - => NEVIM

# 24-25.10.2021
- C#: target pruning, release, Mnisek->Malostranska, 5 total rounds(max 4 transfers)
    - 222ms
- paper
    - 8ms
    - v jine bakalarce(C++impl + C# call) ~ 70ms
- C++ (https://github.com/ducminh-phan/RAPTOR) pod WSL2
    - pracuje s jinym GTFS formateam dat (predpoklada integery, PID ma stringy)
        - bez dokumentace => nevhodne
    - umi unrestricted walking
        - ale pouzivaji se jen k zlepseni cesty mezi 2 zasavkami, ne k vyberu libovolne pocatecni pozice


## nalezen bug v `routes`: **chybi unit testy Timetable** 
- leads to 10x speedup (+mem savings)
- vznikla **nepresnost**
    - tripy po 1 route po stejnych zastavkach **se stale mohou lisit prijezdovymi/odjezdovymi casy**
    - priklad problemu:
        ```
        9_737_210131,16:01:00,16:01:00,U4Z1P,16,,0,0,8.33103 (stoptimes)
            from Hlusickova at 15:45:00:000
            to Arbesovo námestí at 16:01:00:000
        9_1003_210201,16:02:00,16:02:00,U4Z1P,16,,0,0,8.33103 (stoptimes)
            from Hlusickova at 15:44:00:000
            to Arbesovo námestí at 16:02:00:000
        ```
    - vyreseno pridanim doby trvani do rozlisujiciho klice 
        - zvyseny pocet routes: 5097 -> 7952
            - **efekt muze byt minimalni (+-jednotky minuty) ale zpomaluje program o ~56%**

## jak funguje google (snaha o inspiraci)
- omezena vzdalenost (~zastavka musi byt v dosahu 1h pesi chuze)
    - chuze ~4km/h
    - najdu zastavky v okoli n km od daneho bodu
        - a) ekvivalentni(jedou pres ne stejne spoje) zastavky sloucim, najdu 1 idealniho reprezentanta(nejblizsi)
            - **vraci prilis mnoho zastavek**
        - b) set cover: https://en.wikipedia.org/wiki/Set_cover_problem
            - aktualni reseni nerozlisuje smer jizdy
                - nekde muze byt problem, ale casto je protejsi zastavka blizko => mozny transfer
- mista s velkou hustotou zastavek => omezeni na k nejblizsich
- **OPTIMALIZACNI PROBLEM - zatim nevyresen**
    - mam dostatek zastavek, ze kterych pustit raptora, nektere jsou blizko sebe
        - ma cenu **poustet raptora ze vsech**, nejde najit **nejaky prekryv?**
            - moznost spustit raptora jen v 1 cyklu a dle vysledku se rozhodovat
            - **raptor s vice marked stops na vstupu**

## zbytek
- generalizace raptora (**raptor s vice marked stops na vstupu**)
    - vyresila problem s vyberem vstupnich zastavek z libovolneho mista

- prace s ruznymi casy
    - `walk time` - doba cesty na zastavku
    - `travel time` - doba cesty ze src k targetu (vcetne walk time???-------------------)
    - `departure time` - cas vyjezdu spoje
    - `start time` - cas, od ktereho Raptor.Solve() zacina vyhledavat
    - arrival time
    - ... TODO

# 26.10.2021
- restructuring
    - snaha o snizeni zavislosti
    - oddeleni dat GTFS od RAPTORa
- refactoring
- unit tests

# 27.10.2021
- zobecneni `MostAccessibleStopFinder`
    - umozni mi menit strategie vypoctu dostupnosti cilovych zastavek
- vypocet dostupnosti pro misto dane souradnicemi: `EvaluateSrcCoords()`
    - PROBLEM: pomale pro prakticke vyuziti
        - mozne reseni: sorteni
- vizualizace

# [3.11.2021] konzultace 
- zrychlene vyhodnoceni dostupnosti pro misto dane souradnicemi
    - predem urcit body + zjistit sousedni zastavky (body staci mit rozlozene po 100m)
- nedelat db
    - mit "matici sousednosti" == soubor spojujici routes a stops
- mit moznost vybrat konkretni dny (do budoucna)
    - prozatim staci dny v tydnu / vikend
- konkretni datumy zatim neresim (ani s tim spojene vyjimky ve svatky)
    - chci umoznit vyvoj do budoucna
- spoje pres pulnoc
    - umoznit hledani pres pulnoc
    - nejspise musim omezit cas, do kdy me spoje zajimaji (24hod?)
- doba, kdy hledam spoje
    1. statisticky vyhodnotit intervaly(umoznit libovolnou delku intervalu)
        - intervaly po ~1min
        - intervaly po 5min + nahodne vybrat minutu
    2. nechat uzivatele zadavat konkretni cas na konkretni zastavce? 

# 6.11.2021
- zrychleni dostupnosti pro misto dane souradnicemi
    - drive: 37000ms / 10000calls
    - nyni: 700ms / 10000calls (sousedy bodu si vypocitam predem)

# 7.11.2021
- refactor Timetables
    - uz nerozlisuji trips v routes dle celkoveho casu jizdy (reseno 24-25.10.2021)
- odebrani zastavek bez Route (ty, ktere nikde nestavi)
- reseni serializace
    - StopRoutes, RouteStops: nevyplati se serializovat
        - serializace trva dele nez prepocet

# 13.11.2021
- pridani calendar a calendar_dates
    - kazda Route ma Calendar
        - ktere dny jezdi
        - datum kdy nastavaji vyjimky

# 15.11.2021
- moznost vybrat konkretnich den dle datumu
    - pocitam s vyjimkami z calendar_dates
    - vytvoril jsem wrapper nad trip (pridavajici konkretni datum)
        - funkcnost beze zmeny raptora
        - maly pametovy overhead
    - umozni hledat spoje pres pulnoc

# 16+.11.2021
- vyuziti DateTime/TimeSpan pro reprezentace casu
    - beha pomaleji? (~50%) - zpusobeno pridanim tripu => ToDo: optimalizaci binarnim vyhledavanim
- unit testovani TimeTables
    - opravena chyba s nastavenim start/endDate v ctoru Calendar
        - main stale nefunguje, chova se stejne (s tou chybou ani nemel fungovat???)
    
# 12+.12.2021
- TripWithDates unit tests

# 15.12.2021
- optimalizace GetEarliestTripFromStop (praci s daty jsem zvysil pocet tripu)
    - binary search
- refactoring Timetables

# 30.1.2022
- **PROBLEM**?: pri vypoctu dostupnosti pocitam cestu z cilovych(zadanych) pozic na vsechny ostatni => nepocitam opacnou cestu (muze byt asymetricka?)
    - mozne reseni: pro top n zastavek(nebo pro takove, u kterych to ma jeste smysl) spocitat i opacnou cestu
    - ? **existuji vubec nesymetricke linky / ma cenu je resit?** ?
    - neslo by resit 'obracenym' raptorem? (hledal by linky proti smeru, do minulosti)
- unit testovani StopAccessibility

# 31.1.2022
- refactoring MostAccessibleStopFinderu

# 4.2.2022
- pridan ASP.NET web
- uprava MASF API
    - coords(lat, lon), list of dates, time, priority(importance)
        - list of dates oproti 1 date: umozni optimalizace
            - ex. PO-PA, spoje se budou hodne prekryvat
            - vysledky z jednotlivych coords budu prumerovat => usetrim pamet
    - staci cachovat 1 vysledek

# 5.2.2022
- MostAccessibleStopFinder nahrazen StopAccessibilityFinder s lepsim API
- unit testy StopAccessibilityFinder

# 6.2.2022
- raptor algo zpristupnen jako service pro web
- volani StopAccessibilityFinderu skrze formular

# 10.2.2022
- interaktivni mapy
    - alternativy: openlayers, mapquest (free 15K transakci/mesic), google api (free trial je omezeny na ~pul roku)
- openlayers
    1. stavba z modulu => vyuzivani `importu`
    2. pouzivani buildu cele knihovny => `full build syntax` (snazsi cesta - minimalne prozatim)

# 11.2.2022
- map marker
    - definovane interakce nevyhovujici (draw+modify -> obtizny delete) 
        - vytvorim si vlastni podle https://openlayers.org/en/latest/examples/custom-interactions.html
    - ikona: https://commons.wikimedia.org/wiki/File:678111-map-marker-512.png
- dynamicke vytvareni formulare

# 13.2.2022
- zpracovani formulare
- problem s timezone
    - z webu dostavam datum+cas bez timezone, C# parsuje v UTC+2, ale ja jsem v UTC+1
        - vyresil jsem umelym pridanim hodiny v C# (neni elegantni reseni, neni generalizovatelne)
- novy zdroj na uceni ASP.NET Razor pages: https://www.learnrazorpages.com/

# 14.2.2022
- zpracovani vysledku strankou
    - interpolace barev dle dostupnosti zastavek pomoci WebGL: https://openlayers.org/en/latest/examples/webgl-points-layer.html
        - TODO: zlepsit barvy, velikost kruhu, ...
- omezeni extentu (zobrazovane casti mapy) dle timetables

# 15.2.2022
- presunuti vrstvy s markery pred barevou vrstvu
- skalovani velikosti zastavek dle zoomu
- zobrazeni dostupnosti na pozici kurzoru mysi
    - **nelze pomoci session** (musi byt serializovatelne, nema cenu pro takhle caste requesty) => spise nelze na server-side
        - nutne presunout na klient side => javascript / blazor (jiny styl aplikace, vypada spise sloziteji)
        - snad by mohlo jit pomoci cookies, ale nevypada to prilis rozumne pro (real-time) ukazatel mysi
- slider dostupnosti filtrujici zobrazene zastavky

# 16.2.2022
- code cleaning
- server-side validace
    - atributy TargetData - **chybi validace dateTime**
- client-side validace
    - dateTime-local
    - weight - range na 2 mistech (server i klient ...)
- zkouseni jinych GTFS
    - Londyn
    - Auckland
    - Nemecko:
        - nefunguje, maji divny kalendar
        - po vyreseni kalendare: problem s mnozstvim dat
            - algoritmus hledani prestupu ma **kvadratickou slozitost** vzhledem k poctu zastavek
                - bez prechodu neni vysledek prilis zajimavy
- optimalizace serializace

# 17.2.2022 - konzultace
- done:
    - default: dnesni datum a cas
    - vice datumu na frontendu
    - pridavat misto skrze zastavku(nazev)
    - vypis nejdostupnejsich zastavek/mist
- todo:
    - statistika: vypocet v intervalech - jak casto spoje jezdi
        - asi bych mohl take nejak vizualizovat (prepinac pro zmenu vizualizaci?)
    - quad-tree optimalizace
        - pro generovani prechodu
    - mozna: vyhodnoceni vsech mist - neni nutne

    - bakalarka
        - po 1. kapitole poslat pro review
        - uzivatelska dokumentace - na webu + v priloze ( pokud nebude v praci)
        - programatorska: dokumentace public metod + parametru
            - + markdown s navrhem
            - hodi se generovani dokumentace z komentaru
        - word / tex / markdown+pandoc?

# 18.2.2022
- chybove hlaseni pri nedostupne zastavce
- bug fix: formRow koordinace se neupdatovaly pri pohybu markeru
- openlayers add modules / use library: https://gis.stackexchange.com/questions/310467/import-from-in-openlayers/325663
- js code cleaning

# 19.2.2022
- js code refactoring
- zvyrazneni nedosazitelnych cilovych mist
- barva zastavek zavisla na slideru
- uprava slideru
- server-side validace datetime a jsonu

# 20.2.2022
- moznost zadat vice datumu na webu
- vypnuti submit buttonu pri cekani na odpoved
- oprava kliknuti na marker: uz nereaguje na vizualizacni vrstvu
- pridani targetu skrze zastavku(nazev)
- vypis nejdostupnejsich zastavek

# 21.2.2022
- raptor lze zobecnit pro vice vypoctu v intervalech
    - spustit od posledniho intervalu k prvnimu + pamatovat si dojezdovy cas zastavek
    - vetsina zastavek bude mit nastaveny dojezdovy cas z budouciho casu => da se jen vylepsit
        - kazde zlepseni raptora se bude propagovat v jednotlivych kolech do vsech zlepsitelnych zastavek

# 22.2.2022
- **problem**: prestupy nejsou tranzitivni
    - => drivejsi stop mi muze odhalit drive nedostupne prestupy
        - pri vyuziti dat z raptora v budoucnosti nedojde k odhaleni techto prestupu
            => vysledky se lisi
    - **reseni?**: mit tranzitivni prestupy (lokalni reseni: staci zvysit vzdalenost pro prestup)

# 24.2.2022
- tranzitivni prestupy (grafovy problem: komponenty silne souvislosti)

# 25.2.2022
- optimalizace raptora pro vypocet dostupnosti v casech intervalu
    - **znemozni omezit vyhledavani dostupnosti podle poctu prestupu?**
        1. urcite znemozni, pokud vyresetuji arrivalTimes
        2. bez resetu arrivalTimes spise neznemozni 
    - proc raptor nemusi resetovat arrivalTimes?
        - myslenky
            1. v marked jsou jen zlepsene zastavky/tripy => GetArrivalTime se vola jen na zlepsenych=spravnych
            2. set se vola jen na zaklade EarliestArrival
    - proc to funguje?
        - ...

# 28.2.2022
- refactorovani js
- vypocet statistiky z intervalu

# 3.3.2022
- `range searching`
    - quad tree
        - nice, staci postavit jen 1x
    - spatial hashing
        - snazsi implementace nez quad-tree
    - binary space partitioning
        - spise nevhodne
    - R-tree: pouziva se primo na geograficka data
        - r-tree optimized: https://github.com/viceroypenguin/RBush
            - jednoduche reseni (chybi dokumentace nad ramec zakladniho prikladu)
        - bunch of stuff: https://nettopologysuite.github.io/NetTopologySuite/api/NetTopologySuite.Index.Strtree.STRtree-1.html
- zvolen RBush

# 4.3.2022
- optimalizovane generovani prestupu
    - ~10x zrychleni
- Nemecko(debug)
    - 3-5min nez zacne generovat prechody
    - generateTransfer: ~2.5K zastavek/min => 192minut --- oproti predchozim 250 => 10x rychlejsi
- release start: 21:34 - konec pred 24:00
- povedlo se pracovat s daty z Nemecka
    - nutne navysit stack size a deserialization depth

# 5.3.2022
- optimalizace hledani sousedu v NearestStops.GetStopsWithWalkTime() s vyuziti r-tree
    - time elapsed in ms: 34558 ~runs: 10000
    - time elapsed in ms: 5866 ~runs: 10000

# 6.3.2022
## video o psani bp
- https://www.mff.cuni.cz/cs/studenti/bc-a-mgr-studium/bakalarske-a-diplomove-prace
- https://www.mff.cuni.cz/cs/studenti/bc-a-mgr-studium/bakalarske-a-diplomove-prace/pruvodce-po-bakalarske-praci
- https://mj.ucw.cz/vyuka/bc/
- obsah
    - seznam obrazku/tabulek
        - jen v pripade vetsiho poctu obrazku/tabulek
- struktura
    - nazev
    - abstrakt
        - kratke shrnuti vsech casti (~1 veta o kazde kapitole)
            - Popis kontextu (GTFS, Hledani v jizdnich radech)
            - popis problemu
            - co jsem dokazal v praci
            - co bylo tezke (hlavni prinos prace)
            - abstrakce zaveru: komu by se to mohlo hodit, ...
        - ~100-200slov
    - uvod  ----------------------------------------------------------------------- **~hotovo**
        - popis kontextu
        - specifikace problemu
        - co je dulezite na praci
        - **pojmenovat cil prace**
            - musi byt jasne, cemu chci dospet:
                - napr. napsat "Cilem teto prace je 1. ..., 2. ..."
                    - detailni popis cilu
        - struktura prace
            - popis kapitol
        - related works - kapitola za uvodem (nekdy soucasti)
            - existujici metody + porovnani s nimi
            - muze byt i na konci, pokud neexistuji (?)
    - maso
        - analyza
            - informace z denicku
            - projit cile
                - ke kazdemu cili dopsat problemy, na ktere jsem narazil
                    - volba pristupu, algoritmu, datovych struktur, knihoven
            - popis cesty, ne jen vysledky => psat vysvetlit proc jsem co zvolil (mohou byt i rovnocenne)
            - specialne pro nekoho, kdo by chtel delat neco podobneho => zajimava je pro nej cesta, aby nedelal stejne chyby
            - nemusim popisovat cely algo (staci odkaz + popsat klicove myslenky pro rozhodnuti)
            - pokud upravuji algo, hodi se popsat ho vice detailneji
        - vyvojova dokumentace
            1. pro nekoho, kdo chce upravovat program
                - jak to cele funguje jako celek
                    - popsat zavilosti modulu, knihovny, vyvojove prostredi
                    - top-down pristup
                    - hodi se obrazky
                    - aby bylo jasne, kde se da co rozsirit
            2. popis trid a metod => **generovana dokumentace** mimo bp (co neni primo videt z deklarace)
                - pozadavky
                - vyhazovane vyjimky
                - popis algo
            3. uzivatelska dokumentace
                - instalace, pouziti (forma tutorialu pri typickem pouzivani)
                    - ~1+ stranka
                        - je-li delsi => mimo bp
                - v pripade knihoven
                    - cileny uzivatel je programator
    - zaver
    - literatura
    - prilohy
- u nekterch kateder tisknu poster (k obhajobe)
    - pro prezentaci prace: hodne obrazku
- dostanu posudek prace
    - u obhajoby se o tom diskutuje: 10min
    - nevyhovuje => nutne obhajit, jinak problem
- text
    - psan v 1. osobe mn. cisla (provadim ctenare)
    - poradi: nechceme mit dopredne odkazy
    - pro koho pisu: idea - pro spoluzaka
    - strucnost
    - zkusit cist nahlast, testovat na lidech
    - vnimat text ostatnich
    - Zvolte si typ písma pro definice, **identifikátory**, zvýraznění
- zakouti
    - ruzne druhy pomlcek
    - desetinna tecka vs carka (lepsi tecka)
    - nedelitelne mezery
    - obrazky ve vysokem rozliseni - al. 300DPI png nebo vektorove
- PDF/A
    - PDF pro dlouhodobou archivaci
    - varianty: A-1a nebo A-2u
    - latex: pdfx
    - VeraPDF: sis kontroluje timeto korektni PDF/A
- povolene formaty priloh prilohy
    - UTF-8
    - mozna bude problem s .cs (???)
- citace
    - odkaz v zavorce ma jit vynechat (a veta stale bude davat smysl)
        - `Vysledek najdeme na [3]` - SPATNE
    - nehodi se citovat encyklopedie, nahodne stranky (pokud jsou jedinym zdrojem => citovat alespon to)
    - ISO 690: https://www.citace.com/

## [info o uprave bp](https://www.mff.cuni.cz/data/web/obsah-puvodni/studium/bcmgr/prace/bp_uprava_18.pdf)
- Rozlišujeme různé druhy pomlček
    - červeno-černý (krátká pomlčka), strana 16–22 (střední), 45 − 44 (matematické minus), a toto je — jak se asi dalo čekat — vložená věta ohraničená dlouhými pomlčkami.
- pouzivat „české“ uvozovky
- prevzaty delsi text: vyznacit uvozovkami nebo jinak

# 9.3.2022
- vytvareni UML
    - https://app.diagrams.net/#G191V32XyKswivxmA37GKb7kRGNev8vcvb
    - db modelovani: https://sparxsystems.com/resources/tutorials/uml/datamodel.html
        - PK, FK
    - UML notation: https://www.tutorialspoint.com/uml/uml_basic_notations.htm

# 8+.3.2022 - psani textu bp
- todo
    - projit commity a pripadne prepsat zpravy do bp
- psat v 1. os mn.
- mam spoustu obrazku => vyuzit
- v casti `implementace` bude pouze moje implementace a pripadne mozne body rozsireni, vse co se da napsat jinam bude jinde (v analyze)
- co vse se ma davat do bibliografie a na oc staci poznamka pod carou??
    - take je potreba upravit bibliografii, aby text po vytisteni na papir daval smysl


# 29.3.2022
- odebrani local pruning(earliestArrival) kvuli korekci vypoctu v intervalu
    - zpomaleni: ~20ms == 5%
    - korekce intervalu
        - bylo potreba brat earliest arrival (ze vsech round indexu!)
        - propagace hodnot multilabelu jen pri zlepseni

- jine prace pro inspiraci
    - bc: https://dspace.cuni.cz/handle/20.500.11956/108300
    - mgr: https://dspace.cuni.cz/handle/20.500.11956/90481
    - ----------------------- jine prace, neprimo se tykajici meho tematu
    - osadnici z katanu: https://dspace.cuni.cz/handle/20.500.11956/119379
    - speedcubing - hodne stranek: https://dspace.cuni.cz/handle/20.500.11956/119422
    - vyhledavani znamych scen: https://dspace.cuni.cz/handle/20.500.11956/120985
    - prohlizec fotek: https://dspace.cuni.cz/handle/20.500.11956/116920
    - realtime strategie: https://dspace.cuni.cz/handle/20.500.11956/120961
    - GPS ve sportech: https://dspace.cuni.cz/handle/20.500.11956/119440

# popis raptora z clanku
\section{beta-raptor}
## sekce 2 - definice
Π ⊂ N0 is the period of operation (think of it as the seconds of a day)
S is a set of stops
T a set of trips
    trip = posloupnost stopu
    rozdelene do routes
R a set of routes
    trip jedouci pres stejne zastavky
F a set of transfers (or foot-paths).
    l(p1, p2) - walking time
    transitive
J journeys
    output: sekvence tripu[with pickup+dropoff stop] a transfer
    k tripu => k-1 transfers

each stop p in a trip t has associated arrival and departure times
τarr(t, p), τdep(t, p) ∈ Π, with τarr(t, p) ≤ τdep(t, p)

puzit jen pro Earliest Arrival Problem
    input
        source stop p_s
        target stop p_t
        departure time τ
    output
        journey
            departs p_s no earlier than τ
            arrives at pt as early as possible

## sekce 3
zakladni verze: minimalizuje arrival time a pocet prestupu
    - input
        p_s ∈ S -  source
        τ ∈ Π - departure time
    - goal
        - pro kazde k spocitat JOURNEY do p_t s minimalnim arrival time a maximalne o k tripech
    - Pro kazdou zastavku si pamatuje multilabel (earliest arrival pro kazde kolo), init na INF
    - invariant: na zacatku k-teho kola jsou multilabely do intexu k-1 spravne
    - vypocet k-teho kola probiha ve 3 fazich:
        1. τ_k(p) = τ_k−1(p) pro vsechny zastavky == upper bound
        2. projde vsechny route r:
            - T (r) = (t0, t1, . . . , t|T (r)|−1) - posloupnost tripu jedouci po route r od nejpozdejsiho k nejdrivejsimu
            - zvazuji journey takove, ze v k-1 kole jsem dojel na prave prochazenou route r
            - a) najdu nejdrivejsi trip
                -  et(r, pi) = nejdrivejsi trip na r, na ktery nastoupim na zastavce p_i be the earliest
                    - nejdrivejsi t tz. τdep(t, pi) ≥ τk−1(pi).
                    - nemusi existovat
                - r zpracuji tak, ze projdu vsechny zastavky pi, dokud nenajdu definovane et(r, pi) == muzu nastoupit na 'route'
            - b) updatuji cas prijedu na vsech zastavkach ktere jsou na nalezenem nejdrivejsim tripu po zastavce p_i
                - na nektere zastavky jsem vsak mohl prijet drive v k-1 kole => muzu mit moznost chytit drivejsi trip
                    - musim kontrolovat: nastane-li τk−1(pi) < τarr(t, pi) pak musim updatovat nejdrivejsi trip
        3. vyuziti prestupu
            - pro kazdy prestup ∈ F nastavi τk(pj ) = min{τk(pj ), τk(pi) + `(pi, pj )}
    - algo muze byt v k-tem kole zastaven (pokud nebyl updatovan zadny cas prijezdu na zastavku
- improved verze
    - 1. staci mi navstevovat route, jejizch al. 1 zastavka byla updatnuta v predchozim kole
        - impl: marking update stops
            v 1. fazi projdu marked stops a naleznu jim odpovidajici routes
        - route muze byt updatnuta az od 1. updatnute zastavky => pro kazdou marked route si pamatuji jeste jeji nejdrivejsi marked stop
    - 2. local pruning
        - pro kazdou zastavku si zapamatuji celkovy nejdrivejsi cas prijezdu
            - markuji stop <=> vylepsim tento cas
        - zbavim se 1. faze == kopirovani labelu z predchoziho kola
    - 3. target pruning
        - nemarkuje zastavky s dostupnosti vyssi nez je aktualne na cilove zastavce


# zajimavosti - z poznamek o psani BP
    1. Zkratky použité v textu musí být vysvětleny vždy u prvního výskytu zkratky (v~závorce nebo v poznámce pod čarou, jde-li o složitější vysvětlení pojmu či zkratky). Pokud je zkratek více, připojuje se seznam použitých zkratek, včetně jejich vysvětlení a/nebo odkazů na definici.
    2. Mezi číslo a jednotku patří úzká mezera
    3. různé druhy pomlček:
        - červeno-černý (krátká pomlčka),
        - strana 16--22 (střední),
        - $45-44$ (matematické minus),
        - a~toto je --- jak se asi dalo čekat --- vložená věta ohraničená dlouhými pomlčkami
    4. \uv{ceske uvozovky}
    5. nezalomitelne mezery
        - `u~předložek (neslabičnych, nebo obecně jednopísmenných), vrchol~$v$, před $k$~kroky, a~proto, \dots{} obecně kdekoliv, kde by při rozlomení čtenář \uv{ško\-brt\-nul}.`
- odkazy na literaturu - TODO: ---- kontrola s ukazkou!!!
        - Odkazy na literaturu vytváříme nejlépe pomocí příkazů \verb|\citet|, \verb|\citep| atp. (viz {\LaTeX}ový balíček \textsf{natbib}) a~následného použití Bib{\TeX}u.
        - V~matematickém textu obvykle odkazujeme stylem \uv{Jméno autora/autorů (rok vydání)}, resp. \uv{Jméno autora/autorů [číslo odkazu]}.
- {Tabulky, obrázky, programy}: TODO
- 

# update (verze 7) - poslano
- sepsan en abstrakt
- UTC offset je soucasti konfigu
- pridani `~`
- zbaveni se hbox overfull warningu
    - pridani `microtype`
- obrazky nepretekaji sekce
    - pridani `\usepackage[section]{placeins}`
- uprava metadat a kontrola PDF/A

# TODO
- OTAZKY???
    - jine prace maji abstrakt i jako prilohu prace
    - mam spravne citace?
    - mam spravne klicova slova?
- otazky k tisku
    - jednostranne
    - nejaka stranka se nemela tisknout?

# mozne zlepseni
- vyzkouset realne webovou aplikaci + pristup vice uzivatelu
- improved RAPTOR: file:///C:/Users/jfuer/Downloads/IMPROVED_PUBLIC_TRANSIT_ROUTING_ALGORITHM_FOR_FIND.pdf
- https://github.com/google/transitfeed/wiki/Merge
    - da se pouzit k mergovani datasetu


# -----------------------------------------------
https://www.diplomka24.cz/zizkov
## odevzdani bp
- kdy dostanu zapocet

musi splnovat https://www.mff.cuni.cz/cs/vnitrni-zalezitosti/predpisy/opatreni-dekana/smernice-dekana-c-1-2015
	- pevna vazba, ktera neumoznuje vymenu listu
	- a) Desky - Na deskách se povinně uvádí plný název univerzity a fakulty, označení druhu závěrečné práce (bakalářská ap.), jméno a příjmení autora a rok odevzdání, volitelně též název závěrečné práce, vše v jazyce práce.
		- Univerzita Karlova, Matematicko-fyzikální fakulta, Bakalářská práce, jméno a příjmení autora, rok odevzdání, vše v jazyce práce	
	- b) DONE: titulka - je v pdf
	- c) Na samostatném listu - kopie podepsaného zadání závěrečné práce (pouze vázaná verze, nikoli elektronická).
		- dostal jsem postou (staci kopie)
	- d) SEMI-DONE: Prohlášení autora - musim podepsat, ale jak kdyz se listy nedaji vyndat ???

! 2 vytisky
Místo odevzdání závisí na Vašem studijním programu. Studenti informatiky odevzdávají dva výtisky práce na Katedru softwaru a výuky informatiky k paní Kateřině Hegrové.


---------------

- Na deskach:  Univerzita Karlova, Matematicko-fyzikální fakulta, Bakalářská práce, jméno a příjmení autora: Jan Fürst, rok odevzdání: 2022
- podepsat list s prohlasenim
- prilozit zadani: za mezi `titulkou` a `prohlasenim`

