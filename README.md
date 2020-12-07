# corewms

A WMS implementation in .NET Core 3.1 using SkiaSharp as the rendering engine.

Supports PostgreSQL and FlatGeobuf as data sources and SLD 1.1 as styling language.

## How to run

Currently depends on having a recent clone of https://github.com/flatgeobuf/flatgeobuf at `../flatgeobuf`.

Get some test data with `wget http://flatgeobuf.org/test/data/countries.fgb`.

CoreWms should now be runnable out of the box with `dotnet run`.

## Example requests

http://localhost:5000/wms?service=WMS&request=GetCapabilities

http://localhost:5000/wms?service=WMS&request=GetMap&layers=tiger_roads&styles=&crs=&bbox=-74.1,40.7,-73.9,40.9&width=500&height=500&format=image/png