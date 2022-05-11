import geopandas
import matplotlib.pyplot as plt
import contextily as ctx
import pandas as pd
from shapely.geometry import Point, LineString

#vizualizace 1 z 6 tras linkey 317

routes = pd.read_csv("PID_GTFS/routes.txt")
myRoute = routes[routes.route_id == "L317"]
myRoute = myRoute["route_id"]

trips = pd.read_csv("PID_GTFS/trips.txt")
trips = trips[trips.route_id=="L317"]
trips = trips[["route_id", "shape_id"]]
trips = trips.drop_duplicates()

shapes = pd.read_csv("PID_GTFS/shapes.txt")
shapes = shapes.drop("shape_dist_traveled", axis=1)

data = pd.merge(myRoute, trips, on="route_id")
data = pd.merge(data, shapes, how="left", on="shape_id")

data = data[data.shape_id=="L317V1"] #1 type of path only for simlicity


dataPoints = geopandas.GeoDataFrame(data, geometry=geopandas.points_from_xy(data.shape_pt_lon, data.shape_pt_lat))
points = dataPoints["geometry"]
gdf = geopandas.GeoDataFrame(pd.DataFrame({'geometry' : [LineString(points)]}))
gdf = gdf.set_crs(epsg=4326)
gdf = gdf.to_crs(epsg=3857)

ax = gdf.plot(figsize=(10,10), marker=".", color="red", markersize=1)
ctx.add_basemap(ax) #source=ctx.providers.OpenStreetMap.Mapnik
plt.savefig("result.jpg")
