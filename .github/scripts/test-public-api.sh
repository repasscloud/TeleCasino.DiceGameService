#!/bin/bash

# Runs the full bet sweep 20 times, pretty-prints each JSON,
# and prints running totals of .wager and .netGain at the end,
# plus house profit, customer loss %, and payout ratio.

betArgs=(
  Two Three Four Five Six Seven Eight Nine Ten Eleven Twelve
  Odd Even Under7 Over7 Pair1 Pair2 Pair3 Pair4 Pair5 Pair6
)

TOTAL_WAGER=0
TOTAL_NET=0

for round in $(seq 1 100); do
  echo -e "\n====================== Round ${round} ======================\n"
  for betArg in "${betArgs[@]}"; do
    echo "🎯 Bet: $betArg"
    resp=$(curl -s -X POST \
      "http://170.64.191.107/dice/api/Dice/play?wager=5&betArg=${betArg}&gameSessionId=21")

    # Pretty print response
    echo "$resp" | jq .

    # Extract numbers (fallback to 0 if missing/null)
    wager=$(echo "$resp" | jq -r '.wager // 0')
    netGain=$(echo "$resp" | jq -r '.netGain // 0')

    # Accumulate
    TOTAL_WAGER=$(echo "$TOTAL_WAGER + $wager" | bc -l)
    TOTAL_NET=$(echo "$TOTAL_NET + $netGain" | bc -l)

    echo -e "\n----------------------------------------\n"
  done
done

# Final totals
HOUSE_PROFIT=$(echo "scale=2; $TOTAL_WAGER - ($TOTAL_WAGER + $TOTAL_NET)" | bc)
CUSTOMER_LOSS_PCT=$(echo "scale=2; (($HOUSE_PROFIT / $TOTAL_WAGER) * 100)" | bc)
PAYOUT_RATIO=$(echo "scale=2; (100 - $CUSTOMER_LOSS_PCT)" | bc)

echo -e "\n====================== Totals ======================\n"
printf "💵 Total Wager: %0.2f\n" "$TOTAL_WAGER"
printf "📈 Total NetGain: %0.2f\n" "$TOTAL_NET"
printf "🏦 House Profit: %0.2f\n" "$HOUSE_PROFIT"
printf "📉 Customer Loss %%: %0.2f%%\n" "$CUSTOMER_LOSS_PCT"
printf "💰 Payout Ratio: %0.2f%%\n" "$PAYOUT_RATIO"
