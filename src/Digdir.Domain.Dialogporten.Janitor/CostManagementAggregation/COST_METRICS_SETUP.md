# Cost Metrics Aggregation - Setup Guide

This document provides step-by-step instructions for setting up the cost metrics aggregation job in production.

## Overview

The cost metrics aggregation job runs in the production environment and:
- Queries Application Insights from all environments (prod, staging, test, yt01)
- Aggregates cost metrics data
- Uploads results to a storage account in the prod resource group
- Runs daily at 2:00 AM

## Prerequisites

- Access to Azure Portal
- Access to the production Key Vault
- Permissions to grant RBAC roles
- The job must be deployed first (via GitHub Actions)

## Step 1: Deploy the Job

The job is automatically deployed via GitHub Actions when you deploy to the production environment. The deployment will:

1. Create a user-assigned Managed Identity
2. Create a storage account in the prod resource group
3. Create a blob container named "costmetrics"
4. Deploy the Container App Job

## Step 2: Add Secrets to Key Vault

Add the following secrets to the **production Key Vault**:

| Secret Name | Description | Example Value |
|-------------|-------------|---------------|
| `aggregateCostMetricsStagingSubscriptionId` | Staging subscription ID | `12345678-1234-1234-1234-123456789abc` |
| `aggregateCostMetricsProdSubscriptionId` | Production subscription ID | `87654321-4321-4321-4321-cba987654321` |
| `aggregateCostMetricsTestSubscriptionId` | Test subscription ID | `11111111-2222-3333-4444-555555555555` |
| `aggregateCostMetricsYt01SubscriptionId` | YT01 subscription ID | `99999999-8888-7777-6666-555555555555` |

**Note:** These are the subscription IDs where the Application Insights resources are located, not the subscription where the job runs.

## Step 3: Grant RBAC Permissions

After deployment, you need to grant the Managed Identity read access to Application Insights in all environments.

### Find the Managed Identity

1. Go to Azure Portal
2. Navigate to the production resource group
3. Find the user-assigned managed identity: `dp-be-prod-aggregate-cost-metrics-identity`
4. Copy the **Object (principal) ID**

### Grant Application Insights Access

For each environment (staging, test, yt01), grant the Managed Identity access:

1. Go to the Application Insights resource in each environment
2. Navigate to **Access control (IAM)**
3. Click **Add** → **Add role assignment**
4. Select role: **Monitoring Reader**
5. Assign access to: **User, group, or service principal**
6. Search for and select: `dp-be-prod-aggregate-cost-metrics-identity`
7. Click **Save**

Repeat for all Application Insights resources:
- Staging: `dp-be-staging-applicationInsights`
- Test: `dp-be-test-applicationInsights` 
- YT01: `dp-be-yt01-applicationInsights`

**Note:** The job already has access to the production Application Insights since it runs in the same resource group.

## Step 4: Verify Setup

### Check Job Status

1. Go to Azure Portal
2. Navigate to the production resource group
3. Find the Container App Job: `dp-be-prod-aggregate-cost-metrics`
4. Check the **Jobs** tab to see execution history

### Test the Job

You can manually trigger the job to test:

1. In the Container App Job, go to **Jobs** tab
2. Click **Create job execution**
3. Leave all fields as default
4. Click **Create**

### Check Storage Account

1. Navigate to the storage account created by the job
2. Go to **Containers** → **costmetrics**
3. Look for files named: `Dialogporten_metrics_prod-staging_YYYY-MM-DD.parquet`

## Step 5: Monitor and Troubleshoot

### View Logs

1. Go to the Container App Job
2. Navigate to **Log stream** or **Logs**
3. Look for log entries from the cost metrics aggregation

### Common Issues

**Job fails with "Access denied" to Application Insights:**
- Verify RBAC permissions are granted correctly
- Check that the Managed Identity has "Monitoring Reader" role on all App Insights resources

**Job fails with "Storage access denied":**
- This should not happen since storage is in the same resource group
- Verify the storage account was created successfully

**No data in output files:**
- Check that Application Insights has data for the target date
- Verify the KQL query is working (check Application Insights logs)
- Ensure the job is running in the correct timezone (Norwegian time)

**Missing subscription ID secrets:**
- Verify all required secrets are added to the production Key Vault
- Check secret names match exactly (case-sensitive)

## Configuration

### Schedule

The job runs daily at 2:00 AM Norwegian time. To change the schedule:

1. Edit `.azure/applications/aggregate-cost-metrics-job/prod.bicepparam`
2. Modify the `jobSchedule` parameter
3. Use cron format: `'0 2 * * *'` (minute hour day month day-of-week)

### Timeout

Default timeout is 30 minutes. To change:

1. Edit `.azure/applications/aggregate-cost-metrics-job/prod.bicepparam`
2. Modify the `replicaTimeOutInSeconds` parameter

### Storage Container

Default container name is "costmetrics". To change:

1. Edit `.azure/applications/aggregate-cost-metrics-job/prod.bicepparam`
2. Modify the `storageContainerName` parameter

## Security Notes

- The job only has read access to Application Insights (Monitoring Reader role)
- Storage account is in the same resource group as the job (automatic access)
- No cross-environment secrets are needed
- All aggregated data stays in the production environment

## Support

If you encounter issues:

1. Check the job logs in Azure Portal
2. Verify all RBAC permissions are correctly assigned
3. Ensure all required secrets are present in Key Vault

