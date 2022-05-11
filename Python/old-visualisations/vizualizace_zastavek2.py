import geopandas
import matplotlib.pyplot as plt
import pandas as pd

world = geopandas.read_file(geopandas.datasets.get_path('naturalearth_lowres'))
data = pd.read_csv("PID_GTFS/stops.txt")
data = data.iloc[:,2:4]


gdf = geopandas.GeoDataFrame(data, geometry=geopandas.points_from_xy(data.stop_lon, data.stop_lat))


fig, ax = plt.subplots()#???
ax.set_aspect("equal") #???

world[world.name == "Czechia"].plot(ax=ax, color="white", edgecolor="black")
gdf.plot(ax=ax, marker=".")


plt.savefig("vizualizace_zastavek2.jpg")
