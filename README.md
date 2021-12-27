# CoreWms

A WMS implementation in .NET Core 6 using SkiaSharp as the rendering engine.

Supports PostgreSQL and FlatGeobuf as data sources and SLD 1.0 as styling language.

Open source under the [BSD 2-Clause License](https://tldrlegal.com/license/bsd-2-clause-license-(freebsd)).

## TODO

* [ ] Schema validated capabilities document output
* [ ] Complete non spatial filter support
* [ ] Complete symbolizer support

### Stretch goals

* [ ] Dynamic styling support
* [ ] Spatial filtering support
* [ ] SLD 1.1 support

### Non goals

* Remote sources
* Reprojection
* Raster sources

## How to run

Should be runnable out of the box with `dotnet run --project WebApp`.

## Example requests

* http://localhost:5000/wms?service=WMS&request=GetCapabilities

* http://localhost:5000/wms?service=WMS&request=GetMap&layers=countries&styles=&crs=&bbox=-180,-90,180,90&width=500&height=500&format=image/png