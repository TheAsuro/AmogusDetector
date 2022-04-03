global using TPixel = SixLabors.ImageSharp.PixelFormats.Rgba32;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

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

    private readonly Image<L8> data;
    public int Width => data.Width;
    public int Height => data.Height;
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
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                if (this[x, y] == type)
                {
                    return new(x, y);
                }
            }
        }
        throw new InvalidOperationException("No face found!");
    }

    public bool Sus(int x, int y, PixelAccessor<TPixel> image, MatchRule[] matcher)
    {
        if (x + this.Width >= image.Width || y + this.Height >= image.Height)
        {
            return false;
        }

        var bodyColor = image.GetRowSpan(y + PosBody.Y)[x + PosBody.X];
        var faceColor = image.GetRowSpan(y + PosFace.Y)[x + PosFace.X];
        if (bodyColor == faceColor)
        {
            return false;
        }

        int[]? matcherErrors = null;

        for (int patternY = 0; patternY < this.Height; patternY++)
        {
            var imageRow = image.GetRowSpan(y + patternY);

            for (int patternX = 0; patternX < this.Width; patternX++)
            {
                var color = imageRow[x + patternX];
                var pxType = (int)this[patternX, patternY];
                var rule = matcher[pxType];
                if (rule is not null && !rule.Matcher(new MatchCtx(color, bodyColor, faceColor)))
                {
                    matcherErrors ??= new int[matcher.Length];
                    ref var error = ref matcherErrors[pxType];
                    error++;
                    if (error > rule.MaxErr)
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }
}
