using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Collections.Concurrent;
using System.Diagnostics;

bool useLocalTestFile = false;
Image<TPixel> originalImg;

if (useLocalTestFile)
{
    originalImg = Image.Load<TPixel>("src.png");
    originalImg.Mutate(i => i.Resize(originalImg.Width / 3, originalImg.Height / 3, KnownResamplers.NearestNeighbor));
}
else
{
    originalImg = await amogus.DownloadPlace.Download();
}

var targetImg = originalImg.Clone();
targetImg.Mutate(i => i.Saturate(.08f).Contrast(.35f));

var matcher = new MatchRule[Enum.GetValues<PxType>().Select(x => (int)x).Max() + 1];
matcher[(int)PxType.Any] = /************/ new(0, (in MatchCtx c) => true);
matcher[(int)PxType.Body] = /***********/ new(0, (in MatchCtx c) => c.pixelColor == c.bodyColor);
matcher[(int)PxType.Face] = /***********/ new(0, (in MatchCtx c) => c.pixelColor == c.faceColor);
matcher[(int)PxType.NonBody] = /********/ new(2, (in MatchCtx c) => c.pixelColor != c.bodyColor);
matcher[(int)PxType.OptionalBody] = /***/ new(0, (in MatchCtx c) => true);
matcher[(int)PxType.VirtualBorder] = /**/ new(2, (in MatchCtx c) => true);
matcher[(int)0] = /*********************/ new(0, (in MatchCtx c) => c.pixelColor != c.bodyColor);

List<Pattern> patterns = Pattern.basePatterns.SelectMany(p => p.GenerateMirrorPatterns()).OrderByDescending(p => p.Width * p.Height).ToList();

var stopwatch = Stopwatch.StartNew();

var sussyBakas = new ConcurrentBag<(Vec2, Pattern)>();

Parallel.For(0, originalImg.Height, y =>
{
    originalImg.ProcessPixelRows(originalImgCtx =>
    {
        for (int x = 0; x < originalImgCtx.Width; ++x)
        {
            foreach (var pattern in patterns)
            {
                if (pattern.Sus(x, y, originalImgCtx, matcher))
                {
                    sussyBakas.Add((new(x, y), pattern));
                    break;
                }
            }
        }
    });
});

foreach (var (pos, pattern) in sussyBakas)
{
    var bodyColor = originalImg[pos.X + pattern.PosBody.X, pos.Y + pattern.PosBody.Y];
    for (int patternY = 0; patternY < pattern.Height; patternY++)
    {
        for (int patternX = 0; patternX < pattern.Width; patternX++)
        {
            var x = patternX + pos.X;
            var y = patternY + pos.Y;

            if (pattern[patternX, patternY] is PxType.Body or PxType.Face
                || (pattern[patternX, patternY] is PxType.OptionalBody && originalImg[x, y] == bodyColor))
            {
                targetImg[x, y] = originalImg[x, y];
            }
        }
    }
}

Console.WriteLine($"Time: {stopwatch.Elapsed}");
targetImg.SaveAsPng("out.png");

var target3 = targetImg.Clone();
target3.Mutate(i => i.Resize(target3.Width * 3, target3.Height * 3, KnownResamplers.NearestNeighbor));
target3.SaveAsPng("out3.png");

public record struct Vec2(int X, int Y);

public record struct MatchCtx(TPixel pixelColor, TPixel bodyColor, TPixel faceColor);
public record MatchRule(int MaxErr, MatcherFunc Matcher);
public delegate bool MatcherFunc(in MatchCtx ctx);
