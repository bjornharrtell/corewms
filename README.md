# CoreWms

[![CircleCI](https://circleci.com/gh/bjornharrtell/corewms/tree/main.svg?style=svg)](https://circleci.com/gh/bjornharrtell/corewms/tree/main)
[![Nuget](https://img.shields.io/nuget/v/CoreWms)](https://www.nuget.org/packages/CoreWms/)

A [WMS](https://www.ogc.org/standards/wms) implementation in [.NET 6](https://dotnet.microsoft.com/en-us/) using [SkiaSharp](https://github.com/mono/SkiaSharp) as the rendering engine.

Supports [PostgreSQL/PostGIS](https://postgis.net) and [FlatGeobuf](https://flatgeobuf.org) as data sources and [SLD 1.0](https://www.ogc.org/standards/sld) as styling language.

Subproject folders [WebApp](WebApp) and [Function](Function) are intended to be starting points for hosting an instance with ASP.NET Core and Azure Function respectively, but can essentially be used as is.

Open source under the [BSD 2-Clause License](https://tldrlegal.com/license/bsd-2-clause-license-(freebsd)).

## Performance

Early results indicate that CoreWms can produce a layered style 500x500 pixel PNG from 100 000 road segments from OpenStreetMap in approximately half the amount of time compared to GeoServer.

## TODO

* [ ] Schema validated capabilities document output
* [ ] Complete non spatial filter support
* [ ] Complete symbolizer support
* [ ] More usage documentation

### Stretch goals

* [ ] Dynamic styling support
* [ ] Spatial filtering support (other than bbox)
* [ ] SLD 1.1 support

### Non goals

* Remote sources
* Reprojection
* Raster sources

## How to run / usage

Should be runnable out of the box with `dotnet run --project WebApp`.

See [WebApp/appsettings.json](WebApp/appsettings.json) for a configuration example. SLD files corresponding to layer name is expected to be found at `DataPath` (default is current path) in a subfolder named `sld`.

## Example requests

* http://localhost:5000/wms?service=WMS&request=GetCapabilities

* http://localhost:5000/wms?service=WMS&request=GetMap&layers=countries&styles=&crs=&bbox=-180,-90,180,90&width=500&height=500&format=image/png
