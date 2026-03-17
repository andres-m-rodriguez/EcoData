# EcoData User Workflows

This document describes the complete user journey through EcoData, from initial registration to managing sensors and viewing environmental data.

---

## Workflow Overview

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                              USER JOURNEY OVERVIEW                                      │
└─────────────────────────────────────────────────────────────────────────────────────────┘

    ┌──────────────┐      ┌──────────────┐      ┌──────────────┐      ┌──────────────┐
    │   REGISTER   │ ───► │    LOGIN     │ ───► │  JOIN/CREATE │ ───► │   MANAGE     │
    │   Account    │      │   Session    │      │     ORG      │      │   SENSORS    │
    └──────────────┘      └──────────────┘      └──────────────┘      └──────────────┘
           │                     │                     │                     │
           ▼                     ▼                     ▼                     ▼
    ┌──────────────┐      ┌──────────────┐      ┌──────────────┐      ┌──────────────┐
    │ Email        │      │ Credentials  │      │ Request      │      │ Register     │
    │ Display Name │      │ Validated    │      │ Access or    │      │ View Data    │
    │ Password     │      │ Session Set  │      │ Get Invited  │      │ Monitor      │
    └──────────────┘      └──────────────┘      └──────────────┘      └──────────────┘
```

---

## 1. Authentication Flow

### 1.1 User Registration

New users create an account to access the platform.

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                              REGISTRATION FLOW                                          │
└─────────────────────────────────────────────────────────────────────────────────────────┘

    User                        Frontend                       Backend
     │                             │                              │
     │  Fill registration form     │                              │
     │  ─────────────────────────► │                              │
     │                             │                              │
     │                             │  POST /api/auth/register     │
     │                             │  ───────────────────────────►│
     │                             │                              │
     │                             │                              │  Validate:
     │                             │                              │  - Email unique
     │                             │                              │  - Email format
     │                             │                              │  - Password 8-100 chars
     │                             │                              │  - Passwords match
     │                             │                              │
     │                             │                              │  Create User:
     │                             │                              │  - Generate UUID v7
     │                             │                              │  - Hash password
     │                             │                              │  - Set EmailConfirmed
     │                             │                              │
     │                             │        201 Created           │
     │                             │  ◄───────────────────────────│
     │                             │                              │
     │   Redirect to login         │                              │
     │  ◄───────────────────────── │                              │
     │                             │                              │
```

**Endpoint:** `POST /api/auth/register`

**Request:**
```json
{
  "email": "user@example.com",
  "displayName": "John Doe",
  "password": "SecurePass123",
  "confirmPassword": "SecurePass123"
}
```

**Validation Rules:**
| Field | Rules |
|-------|-------|
| Email | Required, valid format, max 256 chars, must be unique |
| Display Name | Required, 2-200 characters |
| Password | Required, 8-100 characters |
| Confirm Password | Must match password |

---

### 1.2 User Login

Authenticated users gain access to protected features.

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                                 LOGIN FLOW                                              │
└─────────────────────────────────────────────────────────────────────────────────────────┘

    User                        Frontend                       Backend
     │                             │                              │
     │  Enter credentials          │                              │
     │  ─────────────────────────► │                              │
     │                             │                              │
     │                             │  POST /api/auth/login        │
     │                             │  ───────────────────────────►│
     │                             │                              │
     │                             │                              │  Validate credentials
     │                             │                              │  Check rate limit
     │                             │                              │  Check lockout status
     │                             │                              │
     │                             │      200 OK + UserInfo       │
     │                             │      Set-Cookie: session     │
     │                             │  ◄───────────────────────────│
     │                             │                              │
     │   Redirect to dashboard     │                              │
     │  ◄───────────────────────── │                              │
     │                             │                              │
```

**Endpoint:** `POST /api/auth/login`

**Request:**
```json
{
  "email": "user@example.com",
  "password": "SecurePass123",
  "rememberMe": true
}
```

**Response:**
```json
{
  "id": "01234567-89ab-cdef-0123-456789abcdef",
  "email": "user@example.com",
  "displayName": "John Doe",
  "globalRole": null,
  "createdAt": "2024-01-15T10:30:00Z"
}
```

**Error Responses:**
| Status | Meaning |
|--------|---------|
| 401 | Invalid credentials |
| 423 | Account locked (too many failed attempts) |
| 429 | Rate limited (too many requests) |

---

### 1.3 Session Management

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                            SESSION ENDPOINTS                                            │
└─────────────────────────────────────────────────────────────────────────────────────────┘

    GET /api/auth/me              Get current user profile
    POST /api/auth/logout         End session and clear cookies
```

---

## 2. Organization Workflow

Organizations are the core unit for grouping users and sensors. Users must belong to an organization to manage sensors.

### 2.1 Discovering Organizations

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                          ORGANIZATION DISCOVERY                                         │
└─────────────────────────────────────────────────────────────────────────────────────────┘

    ┌─────────────────┐                              ┌─────────────────┐
    │   Public List   │                              │  My Orgs List   │
    │                 │                              │                 │
    │  GET /api/      │                              │  GET /api/      │
    │  organizations/ │                              │  organizations/ │
    │                 │                              │  my             │
    └────────┬────────┘                              └────────┬────────┘
             │                                                │
             ▼                                                ▼
    ┌─────────────────┐                              ┌─────────────────┐
    │ Browse all orgs │                              │ View memberships│
    │ View details    │                              │ See your role   │
    │ Request access  │                              │ Quick access    │
    └─────────────────┘                              └─────────────────┘
```

**Endpoints:**
| Endpoint | Auth | Description |
|----------|------|-------------|
| `GET /api/organizations/` | Public | List all organizations |
| `GET /api/organizations/{id}` | Public | Get organization details |
| `GET /api/organizations/my` | Required | Get user's organizations with roles |

---

### 2.2 Requesting Access to an Organization

Users can request to join existing organizations.

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                          ACCESS REQUEST FLOW                                            │
└─────────────────────────────────────────────────────────────────────────────────────────┘

    User                        System                     Org Admin
     │                             │                           │
     │  Request access             │                           │
     │  POST /api/organizations/   │                           │
     │       {id}/access-requests/ │                           │
     │  ─────────────────────────► │                           │
     │                             │                           │
     │                             │  Validate:                │
     │                             │  - Not already member     │
     │                             │  - Not blocked            │
     │                             │  - No pending request     │
     │                             │                           │
     │      Request Created        │                           │
     │  ◄───────────────────────── │                           │
     │                             │                           │
     │                             │   Request appears in      │
     │                             │   admin dashboard         │
     │                             │  ────────────────────────►│
     │                             │                           │
     │                             │                           │  Review request
     │                             │                           │  Approve/Reject
     │                             │     PUT .../status        │
     │                             │  ◄────────────────────────│
     │                             │                           │
     │                             │  If approved:             │
     │                             │  Create member with       │
     │                             │  "Viewer" role            │
     │                             │                           │
     │   Notification of decision  │                           │
     │  ◄───────────────────────── │                           │
     │                             │                           │
```

**User Endpoints:**
| Endpoint | Description |
|----------|-------------|
| `POST /api/organizations/{id}/access-requests/` | Submit access request |
| `GET /api/me/access-requests/` | View own requests |
| `POST /api/me/access-requests/{id}/cancel` | Cancel pending request |

**Admin Endpoints (requires `org:members:manage`):**
| Endpoint | Description |
|----------|-------------|
| `GET /api/organizations/{id}/access-requests/` | List pending requests |
| `PUT /api/organizations/{id}/access-requests/{id}/status` | Approve/Reject |

**Request Status Flow:**
```
    ┌─────────┐
    │ Pending │
    └────┬────┘
         │
    ┌────┴────┬──────────┐
    ▼         ▼          ▼
┌────────┐ ┌────────┐ ┌──────────┐
│Approved│ │Rejected│ │Cancelled │
└────────┘ └────────┘ └──────────┘
     │
     ▼
┌─────────────────┐
│ Member Created  │
│ (Viewer role)   │
└─────────────────┘
```

---

### 2.3 Organization Roles & Permissions

Members have roles that grant specific permissions.

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                          ROLE HIERARCHY                                                 │
└─────────────────────────────────────────────────────────────────────────────────────────┘

    ┌─────────┐
    │  Owner  │  ─── Full control, cannot be removed
    └────┬────┘
         │
    ┌────▼────┐
    │  Admin  │  ─── Manage members, sensors, settings
    └────┬────┘
         │
    ┌────▼────┐
    │ Editor  │  ─── Create/edit sensors
    └────┬────┘
         │
    ┌────▼────┐
    │ Viewer  │  ─── Read-only access (default for new members)
    └─────────┘
```

**Permissions by Role:**
| Permission | Viewer | Editor | Admin | Owner |
|------------|--------|--------|-------|-------|
| `sensor:read` | Yes | Yes | Yes | Yes |
| `sensor:create` | - | Yes | Yes | Yes |
| `sensor:update` | - | Yes | Yes | Yes |
| `sensor:delete` | - | - | Yes | Yes |
| `org:update` | - | - | Yes | Yes |
| `org:members:manage` | - | - | Yes | Yes |
| `org:delete` | - | - | - | Yes |

---

### 2.4 Managing Members

Organization admins can manage the member roster.

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                          MEMBER MANAGEMENT                                              │
└─────────────────────────────────────────────────────────────────────────────────────────┘

    GET  /api/organizations/{id}/members/           List all members
    GET  /api/organizations/{id}/members/{userId}   Get member details
    PUT  /api/organizations/{id}/members/{userId}   Change member role
    DELETE /api/organizations/{id}/members/{userId} Remove member

    ┌─────────────────────────────────────────────────────────────────┐
    │                    Blocked Users                                │
    ├─────────────────────────────────────────────────────────────────┤
    │  GET  /api/organizations/{id}/blocked-users/     List blocked  │
    │  POST /api/organizations/{id}/blocked-users/     Block user    │
    │  DELETE /api/organizations/{id}/blocked-users/{userId} Unblock │
    └─────────────────────────────────────────────────────────────────┘
```

---

## 3. Sensor Workflow

Sensors are IoT devices that submit environmental readings to the platform.

### 3.1 Registering a Sensor

Users with `sensor:create` permission can register new sensors.

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                          SENSOR REGISTRATION FLOW                                       │
└─────────────────────────────────────────────────────────────────────────────────────────┘

    User                        Frontend                       Backend
     │                             │                              │
     │  Navigate to org sensors    │                              │
     │  ─────────────────────────► │                              │
     │                             │                              │
     │                             │  Check sensor:create perm    │
     │                             │  ───────────────────────────►│
     │                             │                              │
     │   Show "Add Sensor" button  │                              │
     │  ◄───────────────────────── │                              │
     │                             │                              │
     │  Fill sensor form:          │                              │
     │  - External ID              │                              │
     │  - Name                     │                              │
     │  - Coordinates              │                              │
     │  - Municipality             │                              │
     │  ─────────────────────────► │                              │
     │                             │                              │
     │                             │  POST /api/sensors/register  │
     │                             │  ───────────────────────────►│
     │                             │                              │
     │                             │                              │  Validate inputs
     │                             │                              │  Create sensor
     │                             │                              │  Generate JWT token
     │                             │                              │
     │                             │   SensorRegistrationResult   │
     │                             │   - sensorId                 │
     │                             │   - accessToken              │
     │                             │   - expiresAt                │
     │                             │  ◄───────────────────────────│
     │                             │                              │
     │  Show credentials dialog    │                              │
     │  (SAVE THE TOKEN!)          │                              │
     │  ◄───────────────────────── │                              │
     │                             │                              │
```

**Endpoint:** `POST /api/sensors/register`

**Request:**
```json
{
  "organizationId": "org-uuid",
  "name": "River Monitor Station 1",
  "externalId": "RMS-001",
  "latitude": 40.7128,
  "longitude": -74.0060,
  "municipalityId": "municipality-uuid"
}
```

**Response:**
```json
{
  "sensorId": "sensor-uuid",
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "expiresAt": "2025-01-15T10:30:00Z"
}
```

**Validation Rules:**
| Field | Rules |
|-------|-------|
| External ID | Required, unique within organization |
| Name | Required |
| Latitude | -90 to +90 |
| Longitude | -180 to +180 |
| Municipality | Must exist in system |

---

### 3.2 Sensor Authentication

Sensors use JWT tokens to authenticate when submitting data.

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                          SENSOR AUTHENTICATION                                          │
└─────────────────────────────────────────────────────────────────────────────────────────┘

    ┌─────────────────────────────────────────────────────────────────┐
    │                    Two Auth Schemes                             │
    ├─────────────────────────────────────────────────────────────────┤
    │                                                                 │
    │   User Auth (Cookie/Session)      Sensor Auth (JWT)            │
    │   ─────────────────────────       ─────────────────            │
    │   - Web browser sessions          - IoT device auth            │
    │   - Interactive users             - Machine-to-machine         │
    │   - Cookie-based                  - Bearer token               │
    │                                                                 │
    │   Used for:                       Used for:                    │
    │   - Login/Register                - Submit readings            │
    │   - Manage orgs/sensors           - Send heartbeats            │
    │   - View dashboards               - Device health reports      │
    │                                                                 │
    └─────────────────────────────────────────────────────────────────┘

    Sensor Request Example:
    ┌─────────────────────────────────────────────────────────────────┐
    │  POST /api/sensors/{id}/readings/                              │
    │  Authorization: Bearer eyJhbGciOiJIUzI1NiIs...                 │
    │  Content-Type: application/json                                │
    │                                                                 │
    │  { "readings": [...] }                                         │
    └─────────────────────────────────────────────────────────────────┘
```

---

### 3.3 Submitting Sensor Readings

Sensors submit environmental data in batches.

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                          READING SUBMISSION FLOW                                        │
└─────────────────────────────────────────────────────────────────────────────────────────┘

    Sensor Device                                  Backend
         │                                            │
         │  Collect environmental data                │
         │  (pH, Temperature, etc.)                   │
         │                                            │
         │  POST /api/sensors/{id}/readings/          │
         │  Authorization: Bearer {token}             │
         │  ──────────────────────────────────────────►
         │                                            │
         │                                            │  Validate JWT
         │                                            │  Validate each reading
         │                                            │  Store valid readings
         │                                            │  Update health status
         │                                            │
         │         Submission Result                  │
         │         - totalCount: 10                   │
         │         - validCount: 9                    │
         │         - errorCount: 1                    │
         │         - errors: [...]                    │
         │  ◄──────────────────────────────────────────
         │                                            │
```

**Endpoint:** `POST /api/sensors/{sensorId}/readings/`

**Request:**
```json
{
  "readings": [
    {
      "parameter": "pH",
      "value": 7.2,
      "unit": "pH",
      "recordedAt": "2024-01-15T10:30:00Z"
    },
    {
      "parameter": "Temperature",
      "value": 22.5,
      "unit": "°C",
      "recordedAt": "2024-01-15T10:30:00Z"
    },
    {
      "parameter": "Dissolved Oxygen",
      "description": "DO level",
      "value": 8.1,
      "unit": "mg/L",
      "recordedAt": "2024-01-15T10:30:00Z"
    }
  ]
}
```

**Response:**
```json
{
  "totalCount": 3,
  "validCount": 3,
  "errorCount": 0,
  "errors": []
}
```

---

### 3.4 Viewing Sensor Readings

Readings are publicly accessible for transparency.

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                          READING RETRIEVAL                                              │
└─────────────────────────────────────────────────────────────────────────────────────────┘

    GET /api/sensors/{id}/readings/              Paginated readings list
    GET /api/sensors/{id}/readings/parameters    Distinct parameters for sensor

    Query Parameters:
    ┌─────────────────────────────────────────────────────────────────┐
    │  ?page=1                    Page number                        │
    │  ?pageSize=25               Items per page                     │
    │  ?parameter=pH              Filter by parameter                │
    │  ?from=2024-01-01           Start date filter                  │
    │  ?to=2024-01-31             End date filter                    │
    └─────────────────────────────────────────────────────────────────┘
```

---

### 3.5 Sensor Health Monitoring

The system monitors sensor health based on activity.

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                          HEALTH MONITORING FLOW                                         │
└─────────────────────────────────────────────────────────────────────────────────────────┘

    Sensor Activity                    Health Status
    ───────────────                    ─────────────

    Reading submitted ──────────────►  LastReadingAt updated
                                              │
    Heartbeat sent ─────────────────►  LastHeartbeatAt updated
                                              │
                                              ▼
                                    ┌─────────────────┐
                                    │ Health Monitor  │
                                    │ checks interval │
                                    └────────┬────────┘
                                             │
              ┌──────────────┬───────────────┼───────────────┬──────────────┐
              ▼              ▼               ▼               ▼              ▼
         ┌─────────┐   ┌─────────┐     ┌─────────┐    ┌───────────┐  ┌─────────┐
         │ Healthy │   │  Stale  │     │Unhealthy│    │  Unknown  │  │ Alerts  │
         │         │   │         │     │         │    │           │  │ Created │
         │ Recent  │   │ Delayed │     │ Critical│    │  No data  │  │         │
         │ activity│   │ activity│     │  delay  │    │   yet     │  │         │
         └─────────┘   └─────────┘     └─────────┘    └───────────┘  └─────────┘
```

**Health Endpoints:**
| Endpoint | Description |
|----------|-------------|
| `GET /api/health/sensors/` | List all sensor health statuses |
| `GET /api/health/sensors/summary` | Overall health summary |
| `GET /api/sensors/{id}/health/` | Specific sensor health |
| `POST /api/sensors/{id}/health/heartbeat` | Record heartbeat (Sensor JWT) |
| `GET /api/health/sensors/alerts` | List health alerts |

**Health Summary Response:**
```json
{
  "totalMonitored": 50,
  "healthy": 42,
  "stale": 5,
  "unhealthy": 2,
  "unknown": 1
}
```

**Health Status Definitions:**
| Status | Meaning |
|--------|---------|
| Healthy | Recent activity within expected interval |
| Stale | Activity delayed beyond normal threshold |
| Unhealthy | Critical delay, possible device failure |
| Unknown | No data received yet |

---

## 4. Complete User Journey

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                          COMPLETE USER JOURNEY                                          │
└─────────────────────────────────────────────────────────────────────────────────────────┘

Step 1: REGISTER
────────────────
    User visits /register
    Fills: email, display name, password
    Account created with no org memberships
         │
         ▼
Step 2: LOGIN
─────────────
    User visits /login
    Enters credentials
    Session established
         │
         ▼
Step 3: DISCOVER ORGANIZATIONS
──────────────────────────────
    User browses /organizations
    Views organization profiles
    Finds relevant organization
         │
         ▼
Step 4: REQUEST ACCESS
──────────────────────
    User clicks "Request Access"
    Optionally adds message
    Request submitted (Pending)
         │
         ▼
Step 5: WAIT FOR APPROVAL
─────────────────────────
    User can view request status at /me/access-requests
    Org admin reviews and approves
    User becomes member with "Viewer" role
         │
         ▼
Step 6: VIEW ORGANIZATION
─────────────────────────
    User accesses /organizations/{id}
    Sees org details, members, sensors
    Can view sensor readings (public)
         │
         ▼
Step 7: GET ELEVATED ROLE (Optional)
────────────────────────────────────
    Admin promotes user to Editor/Admin
    User gains sensor:create permission
         │
         ▼
Step 8: REGISTER SENSOR
───────────────────────
    User navigates to /organizations/{id}/sensors/create
    Fills: external ID, name, coordinates, municipality
    Receives JWT token for device
    MUST SAVE TOKEN - shown only once!
         │
         ▼
Step 9: CONFIGURE DEVICE
────────────────────────
    User configures IoT device with:
    - Sensor ID
    - JWT access token
    - API endpoint URL
         │
         ▼
Step 10: DEVICE SUBMITS DATA
────────────────────────────
    Device sends readings to POST /api/sensors/{id}/readings/
    System validates and stores readings
    Health status updated automatically
         │
         ▼
Step 11: MONITOR & ANALYZE
──────────────────────────
    User views readings on dashboard
    Checks health status
    Receives alerts if sensor goes unhealthy
```

---

## API Quick Reference

### Authentication
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/auth/register` | Create account |
| POST | `/api/auth/login` | Login |
| GET | `/api/auth/me` | Get current user |
| POST | `/api/auth/logout` | Logout |

### Organizations
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/organizations/` | List all |
| GET | `/api/organizations/my` | User's orgs |
| GET | `/api/organizations/{id}` | Get details |
| POST | `/api/organizations/` | Create (Admin) |
| PUT | `/api/organizations/{id}` | Update |
| DELETE | `/api/organizations/{id}` | Delete |

### Members
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/organizations/{id}/members/` | List members |
| PUT | `/api/organizations/{id}/members/{userId}` | Change role |
| DELETE | `/api/organizations/{id}/members/{userId}` | Remove |

### Access Requests
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/organizations/{id}/access-requests/` | Request access |
| GET | `/api/organizations/{id}/access-requests/` | List (Admin) |
| PUT | `/api/organizations/{id}/access-requests/{id}/status` | Approve/Reject |
| GET | `/api/me/access-requests/` | My requests |
| POST | `/api/me/access-requests/{id}/cancel` | Cancel request |

### Sensors
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/sensors/` | List all |
| GET | `/api/sensors/{id}` | Get details |
| POST | `/api/sensors/register` | Register new |
| PUT | `/api/sensors/{id}` | Update |
| DELETE | `/api/sensors/{id}` | Delete |

### Readings
| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/api/sensors/{id}/readings/` | Sensor JWT | Submit readings |
| GET | `/api/sensors/{id}/readings/` | Public | Get readings |
| GET | `/api/sensors/{id}/readings/parameters` | Public | Get parameters |

### Health
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/health/sensors/` | All health statuses |
| GET | `/api/health/sensors/summary` | Health summary |
| GET | `/api/sensors/{id}/health/` | Sensor health |
| POST | `/api/sensors/{id}/health/heartbeat` | Send heartbeat |
| GET | `/api/health/sensors/alerts` | Health alerts |
