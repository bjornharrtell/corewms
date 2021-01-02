# corewms

A WMS implementation in .NET Core 3.1 using SkiaSharp as the rendering engine.

Supports PostgreSQL and FlatGeobuf as data sources and SLD 1.1 as styling language.

## TODO

* [ ] Multi layer rendering
* [ ] [Multi pass](https://www.youtube.com/watch?v=RdqiaNsKR2E) rendering for layered styles
* [ ] Schema validated capabiltities document output
* [ ] Complete filter support
* [ ] Complete symbolizer support
* [ ] SLD 1.0 support

## How to run

Get some test data with `wget http://flatgeobuf.org/test/data/countries.fgb`.

Should now be runnable out of the box with `dotnet run`.

## Example requests

http://localhost:5000/wms?service=WMS&request=GetCapabilities

http://localhost:5000/wms?service=WMS&request=GetMap&layers=countries&styles=&crs=&bbox=-180,-90,180,90&width=500&height=500&format=image/png