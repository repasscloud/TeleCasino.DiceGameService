#!/usr/bin/env bash
set -euo pipefail

API_URL="http://localhost:8080"
BET_ARG="Odd"

echo "🎲 Testing Dice API endpoint..."
curl -v -X POST \
  "$API_URL/api/Dice/play?wager=5&betArg=$BET_ARG&gameSessionId=21" \
  -H 'accept: text/plain' \
  -H 'Content-Type: application/json' \
  -d ''

echo "🎲 Testing Dice video availability..."
ID=$(curl -s -X POST \
  "$API_URL/api/Dice/play?wager=5&betArg=$BET_ARG&gameSessionId=21" \
  -H 'accept: application/json' \
  -H 'Content-Type: application/json' \
  -d '' | jq -r '.id')

if [ -z "$ID" ] || [ "$ID" == "null" ]; then
  echo "❌ No game ID returned from API"
  exit 1
fi

STATUS=$(curl -s -o /dev/null -w "%{http_code}" "$API_URL/${ID}.mp4")

if [ "$STATUS" -ne 200 ]; then
  echo "❌ Video file not available (HTTP $STATUS)"
  exit 1
fi

echo "✅ Dice API tests passed successfully!"
