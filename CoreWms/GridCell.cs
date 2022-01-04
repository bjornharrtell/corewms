using NetTopologySuite.Geometries;

namespace CoreWms;

public struct GridCell
{
    public Envelope Bbox { get; init; }

    public int X { get; init; }
    public int Y { get; init; }

    public int Width { get; init; }
    public int Height { get; init; }

    public GridCell[] Split(int depth)
    {
        return Split(depth, Array.Empty<GridCell>());
    }

    public GridCell[] Split(int depth, GridCell[] acc)
    {
        if (depth == 1)
            return Split();
        depth--;
        var parts = Split();
        var moreParts = parts[0].Split(depth, acc).Concat(parts[1].Split(depth, acc)).ToArray();
        if (depth == 1) {
            return acc.Concat(moreParts).ToArray();
        } else {
            return moreParts;
        }
    }

    public GridCell[] Split()
    {
        if (Width > Height)
        {
            var left = new GridCell() {
                X = X,
                Y = Y,
                Width = Width / 2,
                Height = Height,
                Bbox = new Envelope(Bbox.MinX, Bbox.MinX + Bbox.Width / 2, Bbox.MinY, Bbox.MaxY)
            };
            var right = new GridCell() {
                X = X + Width / 2,
                Y = Y,
                Width = Width / 2,
                Height = Height,
                Bbox = new Envelope(Bbox.MinX + Bbox.Width / 2, Bbox.MaxX, Bbox.MinY, Bbox.MaxY)
            };
            return new GridCell[] { left, right };
        }
        else
        {
            var up = new GridCell() {
                X = X,
                Y = Y,
                Width = Width,
                Height = Height / 2,
                Bbox = new Envelope(Bbox.MinX, Bbox.MaxX, Bbox.MinY, Bbox.MinY + Bbox.Height/2)
            };
            var down = new GridCell() {
                X = X,
                Y = Y + Height / 2,
                Width = Width,
                Height = Height / 2,
                Bbox = new Envelope(Bbox.MinX, Bbox.MaxX, Bbox.MinY + Bbox.Height/2, Bbox.MaxY)
            };
            return new GridCell[] { up, down };
        }
    }
}