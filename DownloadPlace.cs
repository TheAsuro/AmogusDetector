using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace amogus;

public static class DownloadPlace
{
    public static async Task<Image<TPixel>> Download()
    {
        var web = new HttpClient();
        var webResponse = await web.GetStringAsync("https://new.reddit.com/");
        var tokenMatch = Regex.Match(webResponse, "\"accessToken\"\\s*:\\s*\"(?<Token>[\\w-]+)\"");
        if (!tokenMatch.Success)
            throw new Exception("Could not find access token");
        var tokenFull = tokenMatch.Groups["Token"].Value;

        using var ws = new ClientWebSocket();
        ws.Options.SetRequestHeader("Origin", "https://hot-potato.reddit.com");
        await ws.ConnectAsync(new Uri("wss://gql-realtime-2.reddit.com/query"), CancellationToken.None);

        var resultBuffer = new byte[1024 * 1024];
        async Task<JsonElement> Receive()
        {
            var result = await ws.ReceiveAsync(resultBuffer, CancellationToken.None);
            if (!result.EndOfMessage)
                throw new Exception("Fugg");
            return JsonSerializer.Deserialize<JsonElement>(resultBuffer.AsSpan(0, result.Count));
        }
        async Task Send(string text)
        {
            await ws.SendAsync(Encoding.UTF8.GetBytes(text), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        await Send($"{{\"type\": \"connection_init\", \"payload\":{{\"Authorization\": \"Bearer {tokenFull}\"}}}}");
        Console.WriteLine(await Receive()); // {"type":"connection_ack"}
        Console.WriteLine(await Receive()); // {"type":"ka"}
        //await Send("{\"id\":\"1\",\"type\":\"start\",\"payload\":{\"variables\":{\"input\":{\"channel\":{\"teamOwner\":\"AFD2022\",\"category\":\"CONFIG\"}}},\"extensions\":{},\"operationName\":\"configuration\",\"query\":\"subscription configuration($input: SubscribeInput!) {\n  subscribe(input: $input) {\n    id\n    ... on BasicMessage {\n      data {\n        __typename\n        ... on ConfigurationMessageData {\n          colorPalette {\n            colors {\n              hex\n              index\n              __typename\n            }\n            __typename\n          }\n          canvasConfigurations {\n            index\n            dx\n            dy\n            __typename\n          }\n          canvasWidth\n          canvasHeight\n          __typename\n        }\n      }\n      __typename\n    }\n    __typename\n  }\n}\n\"}}");
        //await Receive(); // subans
        for (int i = 0; i < 4; i++)
        {
            await Send($"{{\"id\":\"4\",\"type\":\"start\",\"payload\":{{\"variables\":{{\"input\":{{\"channel\":{{\"teamOwner\":\"AFD2022\",\"category\":\"CANVAS\",\"tag\":\"{i}\"}}}}}},\"extensions\":{{}},\"operationName\":\"replace\",\"query\":\"subscription replace($input: SubscribeInput!) {{\\n  subscribe(input: $input) {{\\n    id\\n    ... on BasicMessage {{\\n      data {{\\n        __typename\\n        ... on FullFrameMessageData {{\\n          __typename\\n          name\\n          timestamp\\n        }}\\n        ... on DiffFrameMessageData {{\\n          __typename\\n          name\\n          currentTimestamp\\n          previousTimestamp\\n        }}\\n      }}\\n      __typename\\n    }}\\n    __typename\\n  }}\\n}}\\n\"}}}}");
        }

        var frames = new List<string>();
        while (true)
        {
            var elem = await Receive();
            Console.WriteLine(elem);
            var frame = elem.GetProperty("payload").GetProperty("data").GetProperty("subscribe").GetProperty("data").Deserialize<FrameData>();
            if (frame.__typename != "FullFrameMessageData")
                continue;
            frames.Add(frame.name);
            if (frames.Count == 4)
                break;
        }

        var finalImage = new Image<TPixel>(2000, 2000);
        foreach (var frameUrl in frames)
        {
            var index = int.Parse(Regex.Match(frameUrl, "-(?<index>\\d)-f-").Groups["index"].Value);
            var stream = await web.GetStreamAsync(frameUrl);
            using var partImg = Image.Load<TPixel>(stream);
            finalImage.Mutate(m =>
                m.DrawImage(partImg, index switch
                {
                    0 => new Point(0__0, 0__0),
                    1 => new Point(1000, 0__0),
                    2 => new Point(0__0, 1000),
                    3 => new Point(1000, 1000),
                }, 1)
            );
        }

        await finalImage.SaveAsPngAsync("test.png");

        return finalImage;
    }
}

class FrameData
{
    public string __typename { get; init; }
    public string name { get; init; }
}