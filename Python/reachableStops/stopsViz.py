import geopandas
import matplotlib.pyplot as plt
import contextily as ctx
import pandas as pd
from shapely.geometry import Point

stops = pd.read_csv("PID_GTFS/stops.txt")

stops['point'] = stops.apply(lambda row: Point(row[3], row[2]), axis = 1)
stops['color'] = 'red'
stops = stops[['stop_id', 'point', 'color']]

rowByStopId = {}
for rowNum in range(len(stops)):
    id = stops.iat[rowNum,0]
    rowByStopId[id] = rowNum

reachableStops = pd.read_csv("Python/ReachableStops.csv")
for id in reachableStops['stop_id']:
    stops.iat[rowByStopId[id], 2] = 'blue'

gdf = geopandas.GeoDataFrame(stops).set_geometry('point').set_crs(epsg=4326).to_crs(epsg=3857)
ax = gdf.plot(figsize=(15,15), color=stops['color'], markersize=1)

ctx.add_basemap(ax)
plt.savefig("result.jpg")
