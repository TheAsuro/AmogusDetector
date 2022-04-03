global using MatcherDict = System.Collections.Generic.Dictionary<PxType, MatchRule>;
using System;
using System.Diagnostics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using TPixel = SixLabors.ImageSharp.PixelFormats.Rgb24;

var originalImg = Image.Load<TPixel>("src.png");
originalImg.Mutate(i => i.Resize(originalImg.Width / 3, originalImg.Height / 3, KnownResamplers.NearestNeighbor));

var targetImg = originalImg.Clone();
targetImg.Mutate(i => i.Saturate(.08f).Contrast(.35f));

var matcher = new MatcherDict();
matcher.Add(PxType.Any, /************/ new(0, (in MatchCtx c) => true));
matcher.Add(PxType.Body, /***********/ new(0, (in MatchCtx c) => c.pixelColor == c.bodyColor));
matcher.Add(PxType.Face, /***********/ new(0, (in MatchCtx c) => c.pixelColor == c.faceColor));
matcher.Add(PxType.NonBody, /********/ new(2, (in MatchCtx c) => c.pixelColor != c.bodyColor));
matcher.Add(PxType.OptionalBody, /***/ new(0, (in MatchCtx c) => true));
matcher.Add(PxType.VirtualBorder, /**/ new(2, (in MatchCtx c) => true));
matcher.Add(0, /*********************/ new(0, (in MatchCtx c) => c.pixelColor != c.bodyColor));

List<Pattern> patterns = Pattern.basePatterns.SelectMany(p => p.GenerateMirrorPatterns()).OrderByDescending(p => p.width * p.height).ToList();

var stopwatch = Stopwatch.StartNew();

var sussyBakas = new List<(Vec2, Pattern)>();
for (int y = 0; y < originalImg.Height; ++y)
{
    for (int x = 0; x < originalImg.Width; ++x)
    {
        foreach (var pattern in patterns)
        {
            if (pattern.Sus(x, y, originalImg, matcher))
            {
                sussyBakas.Add((new(x, y), pattern));
                break;
            }
        }
    }
}

foreach (var (pos, pattern) in sussyBakas)
{
    var bodyColor = originalImg[pos.X + pattern.PosBody.X, pos.Y + pattern.PosBody.Y];
    for (int patternY = 0; patternY < pattern.height; patternY++)
    {
        for (int patternX = 0; patternX < pattern.width; patternX++)
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