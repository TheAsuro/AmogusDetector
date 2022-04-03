using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Runtime.InteropServices;
using TPixel = SixLabors.ImageSharp.PixelFormats.Rgb24;

public enum PxType : byte
{
    Any = 10,
    Body = 11,
    Face = 12,
    NonBody = 13,
    OptionalBody = 14,
    VirtualBorder = 15,
}

public class Pattern
{
    public static Pattern[] basePatterns;

    static Pattern()
    {
        var _ = PxType.Any;
        var B = PxType.Body;
        var F = PxType.Face;
        var X = PxType.NonBody;
        var O = PxType.OptionalBody;

        basePatterns = new Pattern[] {
            new(new PxType[,]{
                { _, X, X, X, _ },
                { X, B, B, B, X },
                { O, B, F, F, _ },
                { O, B, B, B, X },
                { O, B, O, B, X },
                { X, B, 0, B, X },
                { _, X, _, X, _ },
            }),
            new(new PxType[,]{
                { _, X, X, X, _ },
                { X, B, B, B, X },
                { O, B, F, F, _ },
                { O, B, B, B, X },
                { O, B, B, B, X },
                { X, B, O, B, X },
                { X, B, 0, B, X },
                { _, X, _, X, _ },
            }),
            new(new PxType[,]{
                { _, X, X, X, _ },
                { X, B, B, B, X },
                { O, B, F, F, _ },
                { O, B, B, B, X },
                { O, B, 0, B, X },
                { _, X, _, X, _ },
            }),
        };
    }

    private Image<L8> data;
    public int width => data.Width;
    public int height => data.Height;
    public PxType this[int x, int y] => (PxType)data[x, y].PackedValue;
    public Vec2 PosFace { get; }
    public Vec2 PosBody { get; }
    public Dictionary<PxType, int> Error { get; } = new();

    public Pattern(PxType[,] data) : this(ArrayToImage(data)) { }

    public Pattern(Image<L8> data)
    {
        this.data = data;
        PosBody = FindFirstPixelOffset(PxType.Body);
        PosFace = FindFirstPixelOffset(PxType.Face);
    }

    private static Image<L8> ArrayToImage(PxType[,] data)
    {
        var tmpImg = new Image<L8>(data.GetLength(1), data.GetLength(0));
        for (int y = 0; y < data.GetLength(0); ++y)
        {
            for (int x = 0; x < data.GetLength(1); ++x)
            {
                tmpImg[x, y] = new L8((byte)data[y, x]);
            }
        }
        return tmpImg;
    }

    public List<Pattern> GenerateMirrorPatterns()
    {
        var patterns = new List<Pattern>();

        foreach (var (rotation, flip) in
            from rm in Enum.GetValues<RotateMode>()
            from fm in Enum.GetValues<FlipMode>()
            select (rm, fm))
        {
            var newData = data.Clone();
            newData.Mutate(i => i.RotateFlip(rotation, flip));
            patterns.Add(new Pattern(newData));
        }

        return patterns;
    }

    private Vec2 FindFirstPixelOffset(PxType type)
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (this[x, y] == type)
                {
                    return new(x, y);
                }
            }
        }
        throw new InvalidOperationException("No face found!");
    }

    public bool Sus(int x, int y, Image<TPixel> image, MatcherDict matcher)
    {
        if (x + this.width >= image.Width || y + this.height >= image.Height)
        {
            return false;
        }

        var bodyColor = image[x + PosBody.X, y + PosBody.Y];
        var faceColor = image[x + PosFace.X, y + PosFace.Y];
        if (bodyColor == faceColor)
        {
            return false;
        }

        var matcherErrors = new Dictionary<PxType, int>();

        bool FinalMatch(PxType pxType, in MatchCtx ctx)
        {
            if (matcher.TryGetValue(pxType, out var rule))
            {
                if (!rule.Matcher(in ctx))
                {
                    ref var error = ref CollectionsMarshal.GetValueRefOrAddDefault(matcherErrors, pxType, out _);
                    error++;
                    if (error > rule.MaxErr)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        const int backgroundSize = 0;
        for (int patternY = -backgroundSize; patternY < this.height + backgroundSize; patternY++)
        {
            for (int patternX = -backgroundSize; patternX < this.width + backgroundSize; patternX++)
            {
                if (x + patternX < 0 || y + patternY < 0 || x + patternX >= image.Width || y + patternY >= image.Height)
                {
                    continue;
                }
                var color = image[x + patternX, y + patternY];
                if (patternX < 0 || patternY < 0 || patternX >= this.width || patternY >= this.height)
                {
                    if(!FinalMatch(PxType.VirtualBorder, new MatchCtx(color, bodyColor, faceColor)))
                        return false;

                    continue;
                }

                if(!FinalMatch(this[patternX, patternY], new MatchCtx(color, bodyColor, faceColor)))
                    return false;
            }
        }

        return true;
    }
}
