import geopandas
import matplotlib.pyplot as plt
import contextily as ctx
import pandas as pd
from shapely.geometry import Point, LineString

#learning how to draw a multiline :)
line = LineString([Point(14.40969, 50.05917), Point(14.34990, 49.93147), Point(14.54990, 50.031479)])

test = pd.DataFrame({'geometry' : [line]})
gdf = geopandas.GeoDataFrame(test)
gdf = gdf.set_crs(epsg=4326)
print(gdf)
gdf = gdf.to_crs(epsg=3857)
print(gdf)

ax = gdf.plot(figsize=(5,5), marker=".", color="red", markersize=1)
ctx.add_basemap(ax) #source=ctx.providers.OpenStreetMap.Mapnik
plt.savefig("result.jpg")
