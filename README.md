# TeleCasino Dice Game

[![Build and Test DiceGameService](https://github.com/repasscloud/TeleCasino.DiceGameService/actions/workflows/test-dice-api.yml/badge.svg)](https://github.com/repasscloud/TeleCasino.DiceGameService/actions/workflows/test-dice-api.yml)
[![🚀 Publish TeleCasino.KenoGameService (linux-x64)](https://github.com/repasscloud/TeleCasino.DiceGameService/actions/workflows/docker-image.yml/badge.svg)](https://github.com/repasscloud/TeleCasino.DiceGameService/actions/workflows/docker-image.yml)
![GitHub tag (latest SemVer)](https://img.shields.io/github/v/tag/repasscloud/TeleCasino.DiceGameService?label=version)

A command-line Dice game animation and result generator built with .NET and ASP.NET API.  
Users place bets (Odd, Even, Over7, Under7, Pair, or exact sum) with a wager ($1, $2, or $5) and receive an animated video of the dice roll, plus a JSON summary.

## Features

- **Animated roll**:  
  1. **Frame-by-frame** dice roll animation using SVG assets  
  2. **Cryptographically secure randomness** for fair rolls  
  3. **Video generation** via ffmpeg  
  4. **Result file** (MP4) served from API `wwwroot`  
- **JSON output**: Detailed result including wager, bet type, sum, win/loss, payout, net gain, video file.  
- **House edge**: Pays 95% of fair odds.

## Installation

1. Ensure [.NET 9.0 SDK](https://dotnet.microsoft.com/download) is installed.  
2. Clone or download this repository.  
3. Add dependencies:

   ```bash
   dotnet add package SkiaSharp
   dotnet add package Svg.Skia
   dotnet add package NanoidDotNet
   ```

4. Place your `die1.svg` … `die6.svg` files in `/shared/Dice/images/`.

## Build & Publish

```bash
# Clean and build
rm -rf bin obj
dotnet clean
dotnet restore
dotnet publish -c Release

# The build output will be in:
#   bin/Release/net9.0/<RID>/publish/
```

## API Usage

Endpoint: `POST /api/Dice/play`

### Query Parameters

- `wager` (int): Amount wagered ($1, $2, or $5).  
- `betArg` (string): One of "Odd", "Even", "Over7", "Under7", "Pair3", "Pair4", ..., or exact sum (e.g., "7", "11").  
- `gameSessionId` (int): Session identifier.

### Example

```bash
curl -X 'POST' 'http://localhost:8080/api/Dice/play?wager=1&betArg=Odd&gameSessionId=221' -H 'accept: application/json' -d ''
```

Example response:

```json
{
  "id": "abc123xyz",
  "wager": 1,
  "payout": 1.9,
  "netGain": 0.9,
  "videoFile": "abc123xyz.mp4",
  "win": true,
  "betType": "Odd",
  "dieSum": 9,
  "gameSessionId": 221
}
```

The generated video is accessible at:

```url
http://localhost:8080/abc123xyz.mp4
```

## Rules & Parameters

- **Dice roll**: Two fair 6-sided dice (cryptographic RNG).  
- **Bet types**:
  - **Odd/Even**: Win if sum is odd/even (pays 1.9×).  
  - **Over7/Under7**: Win if sum is >7 or <7 (pays 1.9×).  
  - **PairX**: Win if both dice match value X (pays higher).  
  - **Exact sum (2–12)**: Win if sum matches (pays variable odds).  
- **House edge**: 5% reduction on fair odds.

## License

This project is released under the MIT License.
