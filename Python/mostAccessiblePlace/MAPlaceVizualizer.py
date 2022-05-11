# problem with matplotlib loading after using XLAUNCH

import geopandas as gpd
import matplotlib.pyplot as plt
import contextily as ctx
import pandas as pd
from shapely.geometry import Point
import mapclassify #plotting with scheme

PATH = 'Python/mostAccessiblePlace/'

places = pd.read_csv(PATH + "MostAccessiblePlaces.csv")
places['point'] = places.apply(lambda row: Point(row['lon'], row['lat']), axis = 1)

gdf = gpd.GeoDataFrame(places).set_geometry('point').set_crs(epsg=4326).to_crs(epsg=3857)

#ax = gdf.plot(figsize=(15,15), column='time', cmap='nipy_spectral', markersize=1.0, legend=True) #cmap='gist_stern'
ax = gdf.plot(figsize=(15,15), column='arrival_time', cmap='nipy_spectral', markersize=5.0, legend=True, scheme='quantiles', k=10, alpha=0.7)
#ax = gdf.plot(figsize=(15,15), column='time', cmap='nipy_spectral', markersize=1.0, legend=True, scheme='natural_breaks', k=10)

ctx.add_basemap(ax)
plt.savefig(PATH + "result.jpg")

