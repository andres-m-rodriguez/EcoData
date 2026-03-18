# Getting Started: For Researchers & Educators

Welcome to EcoData! This guide helps professors, researchers, and lab coordinators set up and manage environmental monitoring projects with student teams.

## Overview

As a researcher or educator, you can:
1. Create and manage organizations for your lab/class
2. Approve student access requests
3. Manage team roles and permissions
4. Oversee sensor deployments
5. Monitor data collection health

---

## Step 1: Create Your Organization

1. Log in to your EcoData account
2. Go to **Organizations** in the navigation
3. Click **Create Organization**
4. Fill in your organization details:

| Field | Example |
|-------|---------|
| **Name** | UANL Environmental Engineering Lab |
| **Description** | Water quality monitoring research group |

5. Click **Create**

You are automatically the **Owner** with full permissions.

---

## Step 2: Invite Your Team

### Option A: Students Request Access

Share your organization name with students and have them:
1. Create an EcoData account
2. Find your organization in the directory
3. Click **Request Access**

You'll see pending requests in your organization's **Access Requests** tab.

### Option B: Direct Invitation (Coming Soon)

Direct email invitations will be available in a future update.

---

## Step 3: Manage Access Requests

1. Go to your organization page
2. Click the **Access Requests** tab
3. Review pending requests
4. Click **Approve** or **Reject** for each request

Approved members start with the **Viewer** role.

---

## Step 4: Assign Roles

Different roles for different responsibilities:

| Role | Can Do | Best For |
|------|--------|----------|
| **Viewer** | View sensors and readings | General students, observers |
| **Editor** | Create and edit sensors | Students deploying sensors |
| **Admin** | Manage members, all sensor ops | Teaching assistants, lab managers |
| **Owner** | Full control, delete org | You (principal investigator) |

To change a member's role:
1. Go to your organization's **Members** tab
2. Find the member
3. Click the role dropdown
4. Select the new role

> **Tip:** Give "Editor" access to students who need to register their own sensors. Keep most students as "Viewers" if you're managing the sensors centrally.

---

## Step 5: Plan Your Sensor Network

Before deploying sensors, consider:

### Naming Convention
Use consistent External IDs for easy identification:
- `SITE-01`, `SITE-02`, `SITE-03`
- `RIO-UPSTREAM`, `RIO-MIDSTREAM`, `RIO-DOWNSTREAM`
- `LAB-STATION-A`, `LAB-STATION-B`

### Reporting Intervals
Choose based on your research needs:

| Interval | Best For | Data Volume |
|----------|----------|-------------|
| 1 minute | High-resolution studies, events | ~1,440 readings/day |
| 5 minutes | Standard monitoring | ~288 readings/day |
| 15 minutes | Long-term trends | ~96 readings/day |
| 1 hour | Low-power deployments | ~24 readings/day |

### Health Monitoring
The system automatically monitors sensor health:
- **Stale threshold**: 3x your reporting interval
- **Unhealthy threshold**: 12x your reporting interval

Example: A 5-minute interval sensor becomes "Stale" after 15 minutes of silence, "Unhealthy" after 1 hour.

---

## Step 6: Monitor Your Network

### Sensor Health Dashboard

Check sensor status regularly:
1. Go to your organization's **Sensors** tab
2. Look for health status badges:
   - **Healthy** - Operating normally
   - **Stale** - May need attention
   - **Unhealthy** - Likely offline, investigate

### Common Issues

| Symptom | Likely Cause | Solution |
|---------|--------------|----------|
| Sensor shows "Unknown" | Never received data | Check device configuration |
| Sensor went "Stale" | Connectivity issue | Check WiFi/power at site |
| Sensor is "Unhealthy" | Device failure | Visit site, check hardware |
| Wrong readings | Sensor calibration | Recalibrate physical sensor |

---

## Step 7: Credential Management

### If a Student Loses Their Token

1. Go to the sensor's detail page
2. Click **Regenerate Credentials** (requires Admin/Owner role)
3. The old token is invalidated immediately
4. Share the new credentials with the student

> **Security Note:** Only regenerate credentials if the student confirms they've lost them. The old token stops working immediately.

---

## Best Practices for Classes

### For Lab Courses

1. **Pre-register sensors** yourself before class
2. Give students the credentials on lab day
3. Keep students as "Viewers" for data analysis
4. Use consistent naming: `LAB-BENCH-01`, `LAB-BENCH-02`, etc.

### For Field Research Projects

1. Add students as "Editors" so they can register their own sensors
2. Establish a naming convention beforehand
3. Create a shared document for credential backup
4. Schedule regular health checks

### For Long-term Monitoring

1. Use 15-minute or 1-hour intervals to conserve power
2. Set up regular site visits for maintenance
3. Monitor the health dashboard weekly
4. Document sensor locations with photos

---

## Data Management

### Accessing Data

All sensor readings are accessible via:
- **Web interface** - Browse and filter on sensor detail pages
- **API** - Programmatic access for analysis scripts
- **Export** - Download CSV files for offline analysis

### API Access for Analysis

```python
import requests

# Get readings for a sensor
response = requests.get(
    "https://your-server.com/api/sensors/{sensor_id}/readings/",
    params={
        "from": "2024-01-01",
        "to": "2024-01-31",
        "parameter": "pH"
    }
)
readings = response.json()
```

---

## Organization Settings

### Blocking Users

If you need to remove a problematic member:
1. Go to **Members** tab
2. Find the member
3. Click **Remove**

To prevent them from requesting access again:
1. Go to **Blocked Users** tab
2. Click **Block User**
3. Enter their email

---

## Need Help?

- Review the full [User Workflows Documentation](../user-workflows.md)
- Check the [API Documentation](../creating-endpoints.md) for integration
- Contact system administrators for technical issues
