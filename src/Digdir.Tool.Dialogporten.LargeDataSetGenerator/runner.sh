#!/bin/bash

# Start timer
START_TIME=$(date +%s)

# Initial dates
START_DATE="1970-01-02"
END_DATE="1970-01-28"

export DIALOG_AMOUNT="2000000"
export CONN_STRING="Server=localhost;Database=dialogporten;Port=5432;User Id=postgres;Password=supersecret;Ssl Mode=disable;Trust Server Certificate=true;" # DO NOT COMMIT

# Detect platform (macOS or Linux)
IS_MACOS=false
if date --version >/dev/null 2>&1; then
  IS_MACOS=false
else
  IS_MACOS=true
fi

for (( i=1; i<=4; i++ ))
do
  # Set environment variables
  export FROM_DATE="$START_DATE"
  export TO_DATE="$END_DATE"

  echo "Loop $i: FROM_DATE=$FROM_DATE, TO_DATE=$TO_DATE"

  # Run the dotnet command (commented out for testing the date ranges)
  dotnet run -c Release

  # Update dates for the next loop
  if $IS_MACOS; then
    # macOS (BSD `date`)
    START_DATE=$(date -j -f "%Y-%m-%d" "$END_DATE" "+%Y-%m-02" | xargs -I{} date -j -v+1m -f "%Y-%m-%d" {} "+%Y-%m-%d")
    END_DATE=$(date -j -f "%Y-%m-%d" "$START_DATE" "+%Y-%m-%d" | xargs -I{} date -j -v+26d -f "%Y-%m-%d" {} "+%Y-%m-%d")
  else
    # Linux (GNU `date`)
    START_DATE=$(date -d "$END_DATE +1 month" +"%Y-%m-02")
    END_DATE=$(date -d "$START_DATE +26 days" +"%Y-%m-%d")
  fi
done

# End timer
END_TIME=$(date +%s)

# Calculate and print total execution time
TOTAL_TIME=$((END_TIME - START_TIME))
echo "Script completed in $TOTAL_TIME seconds 🚀"
