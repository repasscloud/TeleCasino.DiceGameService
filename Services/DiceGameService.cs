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
    private static readonly string _soundsSubDir = "sounds";

    public DiceGameService(IConfiguration config)
    {
        _sharedDir = config["SharedDirectory"] ?? "/shared";
        _htmlDir = config["HtmlDir"] ?? "/app/wwwroot";
    }

    public async Task<DiceResult> PlayGameAsync(decimal wager, string betArg, int gameSessionId)
    {
        if (!Enum.TryParse<DiceBetType>(betArg, ignoreCase: true, out var betType))
            throw new Exception($"Unknown bet type '{betArg}'");

        var diceResultId = await Nanoid.GenerateAsync();
        var diceSharedRootPath = Path.Combine(_sharedDir, "Dice", diceResultId);
        var videoDir = Path.Combine(diceSharedRootPath, _videosSubDir);
        var videoFile = Path.Combine(videoDir, $"{diceResultId}.mp4");
        var framesDir = Path.Combine(diceSharedRootPath, diceResultId, _framesSubDir);
        var imagesDir = Path.Combine(diceSharedRootPath, _imagesSubDir);
        var soundsDir = Path.Combine(diceSharedRootPath, _soundsSubDir);
        
        PrepareDirectory(framesDir);
        DeleteThisFile(videoFile);
        PrepareDirectory(videoDir);

        var svgs = LoadDiceSvgs(imagesDir);
        var soundFile = Path.Combine(soundsDir, "Ij76x_px8jo.mp3");

        int d1 = RollDice();
        int d2 = RollDice();
        int sum = d1 + d2;

        for (int i = 0; i < FrameCount; i++)
        {
            bool last = i == FrameCount - 1;
            int f1 = last ? d1 : SecureNext(1, 7);
            int f2 = last ? d2 : SecureNext(1, 7);
            DrawFrame(svgs, f1, f2, i, framesDir);
        }

        AssembleVideo(framesDir, soundFile, videoFile);
        Directory.Delete(framesDir, true);

        bool isWin = IsWinningRoll(betType, d1, d2, sum);

        if (!DiceProperties.HouseOdds.TryGetValue(betType, out var odds))
            throw new Exception($"Missing odds for bet type: {betType}");

        decimal payout = isWin ? Math.Round(wager * odds, 2) : 0m;
        decimal netGain = Math.Round(payout - wager, 2);

        File.Move(videoFile, Path.Combine(_htmlDir, Path.GetFileName(videoFile)), true);
        DeleteThisDirectory(diceSharedRootPath);

        return new DiceResult
        {
            Id = diceResultId,
            Wager = wager,
            Payout = payout,
            NetGain = netGain,
            VideoFile = Path.GetFileName(videoFile),
            Win = isWin,
            BetType = betType,
            DieSum = sum,
            GameSessionId = gameSessionId
        };
    }

    private static bool IsWinningRoll(DiceBetType type, int d1, int d2, int sum) => type switch
    {
        DiceBetType.Odd    => sum % 2 == 1,
        DiceBetType.Even   => sum % 2 == 0,
        DiceBetType.Under7 => sum < 7,
        DiceBetType.Over7  => sum > 7,
        DiceBetType.Pair1  => d1 == 1 && d2 == 1,
        DiceBetType.Pair2  => d1 == 2 && d2 == 2,
        DiceBetType.Pair3  => d1 == 3 && d2 == 3,
        DiceBetType.Pair4  => d1 == 4 && d2 == 4,
        DiceBetType.Pair5  => d1 == 5 && d2 == 5,
        DiceBetType.Pair6  => d1 == 6 && d2 == 6,
        DiceBetType.Two    => sum == 2,
        DiceBetType.Three  => sum == 3,
        DiceBetType.Four   => sum == 4,
        DiceBetType.Five   => sum == 5,
        DiceBetType.Six    => sum == 6,
        DiceBetType.Seven  => sum == 7,
        DiceBetType.Eight  => sum == 8,
        DiceBetType.Nine   => sum == 9,
        DiceBetType.Ten    => sum == 10,
        DiceBetType.Eleven => sum == 11,
        DiceBetType.Twelve => sum == 12,
        _ => throw new Exception($"Unhandled bet type: {type}")
    };

    private static Dictionary<int, SKSvg> LoadDiceSvgs(string dir)
    {
        var dict = new Dictionary<int, SKSvg>();
        for (int i = 1; i <= 6; i++)
        {
            var path = Path.Combine(dir, $"die{i}.svg");
            if (!File.Exists(path))
                throw new FileNotFoundException($"Missing SVG asset: {path}");
            var svg = new SKSvg();
            svg.Load(path);
            dict[i] = svg;
        }
        return dict;
    }

    private static void AssembleVideo(string framesDir, string soundFile, string outputFile)
    {
        const int fps = 10;
        const int minDuration = 11;
        double padSeconds = Math.Max(0, minDuration - FrameCount / (double)fps);

        var ffArgs = $"-y -framerate {fps} -i {framesDir}/frame_%03d.png " +
                     $"-i {soundFile} " +
                     $"-filter_complex \"[0:v]tpad=stop_mode=clone:stop_duration={padSeconds}[v]\" " +
                     "-map \"[v]\" -map 1:a " +
                     $"-t {minDuration} -r 30 -c:v libx264 -preset fast -pix_fmt yuv420p " +
                     "-c:a aac -b:a 128k -movflags +faststart -f mp4 " +
                     outputFile;

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
    }

    private static int RollDice()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[4];
        rng.GetBytes(bytes);
        return (BitConverter.ToInt32(bytes, 0) & int.MaxValue) % 6 + 1;
    }

    private static int SecureNext(int min, int max)
    {
        if (min >= max) throw new ArgumentOutOfRangeException();
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[4];
        rng.GetBytes(bytes);
        return (BitConverter.ToInt32(bytes, 0) & int.MaxValue) % (max - min) + min;
    }

    private static void PrepareDirectory(string path)
    {
        if (Directory.Exists(path)) Directory.Delete(path, true);
        Directory.CreateDirectory(path);
    }

    private static void DeleteThisFile(string filePath)
    {
        if (File.Exists(filePath)) File.Delete(filePath);
    }

    private static void DeleteThisDirectory(string dir)
    {
        if (Directory.Exists(dir)) Directory.Delete(dir, true);
    }

    private static void DrawFrame(Dictionary<int, SKSvg> svgs, int face1, int face2, int frameIndex, string outDir)
    {
        using var bmp = new SKBitmap(Width, Height);
        using var canvas = new SKCanvas(bmp);
        canvas.Clear(SKColors.White);

        var p1 = svgs[face1].Picture!;
        var p2 = svgs[face2].Picture!;
        var r1 = p1.CullRect;
        var r2 = p2.CullRect;

        float halfW = Width / 2f;
        float s1 = Math.Min(halfW / r1.Width, Height / r1.Height);
        float s2 = Math.Min(halfW / r2.Width, Height / r2.Height);

        var m1 = SKMatrix.CreateScale(s1, s1);
        m1.TransX = (halfW - r1.Width * s1) / 2f;
        m1.TransY = (Height - r1.Height * s1) / 2f;

        var m2 = SKMatrix.CreateScale(s2, s2);
        m2.TransX = halfW + (halfW - r2.Width * s2) / 2f;
        m2.TransY = (Height - r2.Height * s2) / 2f;

        canvas.DrawPicture(p1, in m1);
        canvas.DrawPicture(p2, in m2);

        var framePath = Path.Combine(outDir, $"frame_{frameIndex:D3}.png");
        using var imgData = SKImage.FromBitmap(bmp).Encode(SKEncodedImageFormat.Png, 100);
        using var fs = File.OpenWrite(framePath);
        imgData.SaveTo(fs);
    }
}
