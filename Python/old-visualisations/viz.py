import geopandas
import matplotlib.pyplot as plt
import contextily as ctx
import pandas as pd
from shapely.geometry import Point, LineString

#ToDo: vykresleni vsech tras
shapes = pd.read_csv("PID_GTFS/shapes.txt")
shapes = shapes.head(10) #zrychleni...

def makePoint(row):
    lat = row[1]
    lon = row[2]
    return Point(lon, lat)

def makeLineString(x):
    lst = []
    for i in x:
        lst.append(i) #nejde pridavat primo, x se pozdeji indexuje od posledniho indexu...
    return LineString(lst)

shapes['points'] = shapes.apply(lambda row: makePoint(row), axis=1)
shapes = shapes[['shape_id','points']]

shapes = shapes.groupby('shape_id')['points'].agg(lambda points: makeLineString(points))
#print(shapes)

gdf = geopandas.GeoDataFrame(shapes)
gdf = gdf.set_geometry('points')
gdf = gdf.set_crs(epsg=4326)
gdf = gdf.to_crs(epsg=3857)

ax = gdf.plot(figsize=(15,15), color="red", markersize=1)
ctx.add_basemap(ax) #source=ctx.providers.OpenStreetMap.Mapnik
plt.savefig("result.jpg")


