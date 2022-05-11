# problem with matplotlib loading after using XLAUNCH

import geopandas as gpd
import matplotlib.pyplot as plt
import contextily as ctx
import pandas as pd
from shapely.geometry import Point
import mapclassify #plotting with scheme

PATH = 'Python/mostAccessibleStop/'

stops = pd.read_csv("PID_GTFS/stops.txt")
stops['point'] = stops.apply(lambda row: Point(row['stop_lon'], row['stop_lat']), axis = 1)
stops = stops[['stop_id', 'point']]


rowByStopId = {}
for rowNum in range(len(stops)):
    id = stops.at[rowNum, 'stop_id']
    rowByStopId[id] = rowNum

reachedStops = pd.read_csv(PATH + "MostAccessibleStops.csv")
for (_, reachedStop) in reachedStops.iterrows():
    id = reachedStop['stop_id']
    stops.at[rowByStopId[id], 'time'] = reachedStop['arrival_time_sum'] #-----------------should be time_avg

#filtering out stops reachable after n sec
#stops = stops[stops['time'] < 5000]

gdf = gpd.GeoDataFrame(stops).set_geometry('point').set_crs(epsg=4326).to_crs(epsg=3857)

#ax = gdf.plot(figsize=(15,15), column='time', cmap='nipy_spectral', markersize=1.0, legend=True) #cmap='gist_stern'
ax = gdf.plot(figsize=(15,15), column='time', cmap='nipy_spectral', markersize=1.0, legend=True, scheme='quantiles', k=10)
#ax = gdf.plot(figsize=(15,15), column='time', cmap='nipy_spectral', markersize=1.0, legend=True, scheme='natural_breaks', k=10)

ctx.add_basemap(ax)
plt.savefig(PATH + "result.png")
