import geopandas
import matplotlib.pyplot as plt
import contextily as ctx
import pandas as pd

data = pd.read_csv("PID_GTFS/stops.txt")
data = data.iloc[:,2:4]

gdf = geopandas.GeoDataFrame(data, geometry=geopandas.points_from_xy(data.stop_lon, data.stop_lat))

gdf = gdf.set_crs(epsg=4326)
#print(gdf.crs)
gdf = gdf.to_crs(epsg=3857)
#print(gdf.crs)

ax = gdf.plot(figsize=(15,15), marker=".", color="red", markersize=1)
ctx.add_basemap(ax) #source=ctx.providers.OpenStreetMap.Mapnik
plt.savefig("vizualizace_zastavek3.jpg")
