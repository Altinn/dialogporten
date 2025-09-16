# Deployment Lag Monitoring

Automatically monitors deployment lag between staging and production, sending Slack notifications when production falls behind configurable thresholds.

## Overview

- **Schedule**: Runs weekdays at 12pm Norway time
- **Thresholds**: Configurable days (default: 3) and release count (default: 3) limits
- **Notifications**: Rich Slack messages with commit details and contributor tagging

## Configuration

Thresholds are configured via environment variables in the workflow:
```yaml
env:
  DEFAULT_DAYS_THRESHOLD: 3      # Days behind threshold
  DEFAULT_RELEASES_THRESHOLD: 3  # Release count threshold
```

Schedule: `0 11 * * 1-5` (12pm Norway time, weekdays)

## How It Works

Uses GitHub environment variables (`LATEST_DEPLOYED_APPS_VERSION`) set by deployment workflows to compare staging vs production versions.

**Notification triggers:**
1. Production is ≥ `days_threshold` days behind AND has pending releases
2. Staging is ≥ `releases_threshold` releases ahead

**Severity levels:**
- 🚨 **HIGH**: ≥7 days OR ≥5 releases behind
- ⚠️ **MEDIUM**: ≥5 days OR ≥4 releases behind  
- ℹ️ **LOW**: Below medium thresholds

## Slack Notifications

Sent to `SLACK_CHANNEL_ID_FOR_RELEASES` with:
- Version comparison and lag metrics
- Up to 10 recent commits with author tagging
- Action buttons (view differences, deploy, workflow run)

Note: GitHub usernames are converted to `@username` format.

## Usage

Runs automatically on weekdays at 12pm Norway time. For testing, temporarily lower the threshold values in the workflow environment variables.

