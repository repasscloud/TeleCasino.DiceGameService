using System.Diagnostics;
using System.Security.Cryptography;
using NanoidDotNet;
using SkiaSharp;
using Svg.Skia;
using TeleCasino.DiceGameService.Models;
using TeleCasino.DiceGameService.Services.Interface;

namespace TeleCasino.DiceGameService.Services;

public class DiceGameService : IDiceGameService
{
    private readonly string _sharedDir;
    private readonly string _htmlDir;
    private const int FrameCount = 30;
    private const int Width = 400;
    private const int Height = 200;
    private static readonly string _framesSubDir = "frames";
    private static readonly string _videosSubDir = "videos";
    private static readonly string _imagesSubDir = "images";


    public DiceGameService(IConfiguration config)
    {
        _sharedDir = config["SharedDirectory"] ?? "/shared";
        _htmlDir = config["HtmlDir"] ?? "/app/wwwroot";
    }

    public async Task<DiceResult> PlayGameAsync(int wager, string betArg, int gameSessionId)
    {
        if (!Enum.TryParse<DiceBetType>(betArg, out var betArgEnum))
            throw new Exception($"Unknown bet type '{betArg}'");

        var diceResultId = await Nanoid.GenerateAsync();
        var diceSharedRootPath = Path.Combine(_sharedDir, "Dice");
        var videoDir = Path.Combine(diceSharedRootPath, diceResultId, _videosSubDir);
        var videoFile = Path.Combine(videoDir, $"{diceResultId}.mp4");
        var framesDir = Path.Combine(diceSharedRootPath, diceResultId, _framesSubDir);
        var imagesDir = Path.Combine(diceSharedRootPath, _imagesSubDir);

        PrepareDirectory(framesDir);
        DeleteThisFile(videoFile);
        PrepareDirectory(videoDir);

        // load SVGs
        var svgAssets = new Dictionary<int, SKSvg>();
        for (int face = 1; face <= 6; face++)
        {
            var path = Path.Combine(imagesDir, $"die{face}.svg");
            if (!File.Exists(path))
                throw new FileNotFoundException($"Missing SVG asset: {path}");
            var svg = new SKSvg();
            svg.Load(path);
            svgAssets[face] = svg;
        }

        // do the roll
        int d1 = RollDice();
        int d2 = RollDice();
        int sum = d1 + d2;

        // animate
        for (int i = 0; i < FrameCount; i++)
        {
            bool last = i == FrameCount - 1;
            int f1 = last ? d1 : SecureNext(1, 7);
            int f2 = last ? d2 : SecureNext(1, 7);
            DrawFrame(svgAssets, f1, f2, i, framesDir);
        }

        // Assemble video
        var ffArgs = $"-y -framerate 10 -i {framesDir}/frame_%03d.png " +
                        "-c:v libx264 -preset fast -pix_fmt yuv420p " +
                        "-movflags +faststart " +
                        videoFile;
        var psi = new ProcessStartInfo("ffmpeg", ffArgs)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        using var proc = Process.Start(psi)!;
        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();
        proc.WaitForExit();

        // Cleanup frames
        Directory.Delete(framesDir, true);

        // Determine win/loss
        bool isWin = betArg.Equals("Odd", StringComparison.OrdinalIgnoreCase)
                    ? (sum % 2 == 1)
                    : betArg.Equals("Even", StringComparison.OrdinalIgnoreCase)
                    ? (sum % 2 == 0)
                    : betArg.Equals("Under7", StringComparison.OrdinalIgnoreCase)
                    ? (sum < 7)
                    : betArg.Equals("Over7", StringComparison.OrdinalIgnoreCase)
                    ? (sum > 7)
                    : betArg.StartsWith("Pair", StringComparison.OrdinalIgnoreCase)
                    ? (sum % 2 == 0 && d1 == d2 && $"Pair{d1}".Equals(betArg, StringComparison.OrdinalIgnoreCase))
                    : betArg == sum.ToString();

        decimal odds = DiceProperties.HouseOdds[betArg];
        decimal payout = isWin ? Math.Round(wager * odds, 2) : 0m;
        decimal netGain = Math.Round(payout - wager, 2);

        File.Move(videoFile, Path.Combine("/app/wwwroot", Path.GetFileName(videoFile)), true);
        DeleteThisDirectory(Path.Combine(diceSharedRootPath, diceResultId));

        return new DiceResult
        {
            Id = diceResultId,
            Wager = wager,
            Payout = payout,
            NetGain = netGain,
            VideoFile = Path.GetFileName(videoFile),
            Win = isWin,
            BetType = betArgEnum,
            DieSum = sum,
            GameSessionId = gameSessionId
        };
    }

    // cryptographic randomness
    private int RollDice()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[4];
        rng.GetBytes(bytes);
        int value = BitConverter.ToInt32(bytes, 0) & int.MaxValue; // Ensure non-negative
        return (value % 6) + 1; // 1–6 inclusive
    }

    // cryptographic randomness again
    private int SecureNext(int minValue, int maxValue)
    {
        if (minValue >= maxValue)
            throw new ArgumentOutOfRangeException(nameof(maxValue), "maxValue must be greater than minValue");

        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[4];
        rng.GetBytes(bytes);
        int value = BitConverter.ToInt32(bytes, 0) & int.MaxValue;
        return (value % (maxValue - minValue)) + minValue;
    }

    private static void PrepareDirectory(string dir)
    {
        if (Directory.Exists(dir))
            Directory.Delete(dir, true);
        Directory.CreateDirectory(dir);
    }

    private static void DeleteThisFile(string filePath)
    {
        if (File.Exists(filePath)) File.Delete(filePath);
    }

    private static void DeleteThisDirectory(string dir)
    {
        if (Directory.Exists(dir))
            Directory.Delete(dir, true);
    }

    private static void DrawFrame(
        Dictionary<int, SKSvg> svgs,
        int face1,
        int face2,
        int frameIndex,
        string outDir)
    {
        using var bmp = new SKBitmap(Width, Height);
        using var canvas = new SKCanvas(bmp);
        canvas.Clear(SKColors.White);

        var pic1 = svgs[face1].Picture!;
        var pic2 = svgs[face2].Picture!;
        var r1 = pic1.CullRect;
        var r2 = pic2.CullRect;

        float halfW = Width / 2f;
        float s1 = Math.Min(halfW / r1.Width, Height / r1.Height);
        float s2 = Math.Min(halfW / r2.Width, Height / r2.Height);

        var m1 = SKMatrix.CreateScale(s1, s1);
        m1.TransX = (halfW - r1.Width * s1) / 2f;
        m1.TransY = (Height - r1.Height * s1) / 2f;

        var m2 = SKMatrix.CreateScale(s2, s2);
        m2.TransX = halfW + (halfW - r2.Width * s2) / 2f;
        m2.TransY = (Height - r2.Height * s2) / 2f;

        canvas.DrawPicture(pic1, in m1);
        canvas.DrawPicture(pic2, in m2);

        var framePath = Path.Combine(outDir, $"frame_{frameIndex:D3}.png");
        using var imgData = SKImage.FromBitmap(bmp)
                                    .Encode(SKEncodedImageFormat.Png, 100);
        using var fs = File.OpenWrite(framePath);
        imgData.SaveTo(fs);
    }
}
