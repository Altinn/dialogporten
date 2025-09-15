# Deployment Lag Monitoring

This document describes the deployment lag monitoring system that automatically tracks the difference between staging and production deployments and sends notifications when production is lagging behind.

## Overview

The deployment lag monitoring system helps ensure that production deployments don't fall too far behind staging by:

1. **Scheduled Monitoring**: Automatically checks deployment status weekdays at 12pm Norway time
2. **Configurable Thresholds**: Alerts when production is behind by a configurable number of days or releases
3. **Rich Notifications**: Sends detailed Slack messages with commit information and contributor tagging
4. **Manual Triggers**: Allows on-demand checks with custom parameters

## Workflows

### Main Workflow: `ci-cd-deployment-lag-monitor.yml`

**Triggers:**
- **Schedule**: Weekdays at 12pm Norway time (`0 11 * * 1-5` UTC)
- **Manual Dispatch**: With configurable parameters

**Parameters (Manual Dispatch):**
- `days_threshold`: Days threshold for production lag warning (default: 3)
- `releases_threshold`: Release count threshold for staging/prod diff warning (default: 3)
- `force_notification`: Send notification regardless of thresholds (default: false)

**Jobs:**
1. **compare-deployments**: Fetches versions and calculates lag metrics
2. **get-commit-details**: Retrieves commit information between releases
3. **send-slack-notification**: Sends formatted Slack notification

### Supporting Workflow: `workflow-send-deployment-lag-slack-message.yml`

A reusable workflow that formats and sends Slack notifications with:
- Version information
- Deployment lag metrics
- Commit details with author information
- Action buttons for quick access

### Manual Trigger: `dispatch-deployment-lag-check.yml`

A simplified dispatch workflow for testing and on-demand checks.

## How It Works

### Version Tracking

The system uses GitHub environment variables to track deployed versions:
- `LATEST_DEPLOYED_APPS_VERSION` (per environment: staging, prod)
- These are set automatically during deployments via existing CI/CD workflows

### Threshold Logic

Notifications are sent when ANY of these conditions are met:
1. **Time-based**: Production deployment is older than `days_threshold` AND there are pending releases
2. **Release-based**: Staging is ahead by `releases_threshold` or more releases
3. **Force**: Manual trigger with `force_notification=true`

### Severity Levels

The system determines notification severity based on lag:
- **HIGH** (Red 🚨): ≥7 days OR ≥5 releases behind
- **MEDIUM** (Orange ⚠️): ≥5 days OR ≥4 releases behind  
- **LOW** (Yellow ℹ️): Below medium thresholds

## Slack Notifications

### Content

Notifications include:
- **Version Comparison**: Current staging vs production versions
- **Lag Metrics**: Days since last production deploy, release count difference
- **Recent Changes**: Up to 10 most recent commits with author information
- **Contributors**: Tagged contributors who have commits pending deployment
- **Action Buttons**: 
  - View Differences (GitHub compare)
  - Deploy to Production (workflow link)
  - View Workflow Run

### Channel

Notifications are sent to the channel configured in `SLACK_CHANNEL_ID_FOR_RELEASES`.

### Author Tagging

The system attempts to tag contributors by:
1. Extracting GitHub usernames from commits
2. Converting to `@username` format in Slack
3. For production use, consider maintaining a GitHub → Slack username mapping

## Configuration

### Required Secrets

- `SLACK_BOT_TOKEN`: Bot token for Slack integration
- `SLACK_CHANNEL_ID_FOR_RELEASES`: Channel ID for notifications
- `GITHUB_TOKEN`: Standard GitHub token (automatically provided)

### Environment Variables

The system reads deployment versions from GitHub environment variables:
- Environment: `staging` → Variable: `LATEST_DEPLOYED_APPS_VERSION`
- Environment: `prod` → Variable: `LATEST_DEPLOYED_APPS_VERSION`

### Customization

**Thresholds**: Default thresholds can be modified in the workflow file or overridden via manual dispatch.

**Schedule**: The cron schedule can be adjusted in the workflow file:
```yaml
schedule:
  - cron: '0 11 * * 1-5'  # 12pm Norway time, weekdays
```

**Notification Content**: The Slack template can be customized in `.github/slack-templates/deployment-lag-notification.json`.

## Usage

### Automatic Monitoring

The system runs automatically on weekdays at 12pm Norway time. No action required.

### Manual Checks

1. Go to **Actions** → **Dispatch Deployment Lag Check**
2. Click **Run workflow**
3. Optionally adjust thresholds or enable force notification
4. Click **Run workflow**

### Testing

For testing purposes, use the manual dispatch with:
- Lower thresholds (e.g., `days_threshold: 1`)
- `force_notification: true` to always send notifications

## Integration with Existing CI/CD

The deployment lag monitor integrates seamlessly with the existing CI/CD pipeline:

- **Version Tracking**: Uses the same GitHub environment variables set by deployment workflows
- **Slack Integration**: Uses the same Slack bot and channel configuration
- **Security**: Follows the same hardened runner and security patterns
- **Naming**: Follows established workflow naming conventions (`ci-cd-*`, `workflow-*`, `dispatch-*`)

## Troubleshooting

### No Notifications Received

1. Check that GitHub environment variables are set correctly
2. Verify Slack bot token and channel permissions
3. Review workflow run logs for errors
4. Test with `force_notification: true`

### Incorrect Version Information

1. Verify that deployment workflows are updating GitHub environment variables
2. Check that release tags follow the expected format (`v1.2.3`)
3. Ensure the repository has appropriate release history

### Author Tagging Issues

1. GitHub usernames may not match Slack usernames
2. Consider implementing a mapping file for production use
3. Check that commits have proper author information

## Future Enhancements

Potential improvements to consider:
1. **Slack User Mapping**: Maintain GitHub → Slack username mapping file
2. **Custom Channels**: Allow different channels for different severity levels
3. **Deployment Metrics**: Track deployment frequency and success rates
4. **Integration**: Add links to deployment dashboards or monitoring systems
5. **Filters**: Skip notifications for certain types of releases (e.g., hotfixes)
