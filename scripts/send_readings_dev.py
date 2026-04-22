#!/usr/bin/env python3
"""
Sensor reading simulator for EcoData (Development).
Logs in with user credentials, registers a sensor, and sends readings using sensor JWT.
"""

import requests
import random
import time
import uuid
import sys
from datetime import datetime, timezone

# Configuration
BASE_URL = "https://localhost:5000"
INTERVAL = 30

# Disable SSL warnings for local dev
import urllib3
urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

# Simulated parameters
PARAMETERS = [
    {"parameter": "temperature", "unit": "°C", "min": 20.0, "max": 35.0, "description": "Air temperature"},
    {"parameter": "humidity", "unit": "%", "min": 40.0, "max": 90.0, "description": "Relative humidity"},
    {"parameter": "pm25", "unit": "µg/m³", "min": 5.0, "max": 50.0, "description": "PM2.5 particulate matter"},
    {"parameter": "co2", "unit": "ppm", "min": 400.0, "max": 800.0, "description": "Carbon dioxide"},
]


def generate_reading(param_config: dict) -> dict:
    """Generate a random reading for a parameter."""
    return {
        "parameter": param_config["parameter"],
        "description": param_config["description"],
        "value": round(random.uniform(param_config["min"], param_config["max"]), 2),
        "unit": param_config["unit"],
        "recordedAt": datetime.now(timezone.utc).isoformat(),
    }


def login(session: requests.Session, email: str, password: str) -> dict | None:
    """Login and return user info with token."""
    response = session.post(
        f"{BASE_URL}/identity/auth/login",
        json={"email": email, "password": password}
    )

    if response.status_code != 200:
        print(f"Login failed: {response.status_code} - {response.text}")
        return None

    return response.json()


def get_organization(session: requests.Session) -> dict | None:
    """Get first available organization."""
    response = session.get(f"{BASE_URL}/organization/organizations")

    if response.status_code != 200:
        print(f"Failed to get organizations: {response.status_code}")
        return None

    orgs = response.json()
    return orgs[0] if orgs else None


def get_municipality(session: requests.Session) -> dict | None:
    """Get first available municipality."""
    response = session.get(f"{BASE_URL}/locations/municipalities?pageSize=1")

    if response.status_code != 200:
        print(f"Failed to get municipalities: {response.status_code}")
        return None

    municipalities = response.json()
    return municipalities[0] if municipalities else None


def register_sensor(session: requests.Session, org: dict, municipality: dict) -> dict | None:
    """Register a new sensor and return credentials."""
    sensor_name = f"DevSimulator-{uuid.uuid4().hex[:8]}"

    response = session.post(
        f"{BASE_URL}/sensors/register",
        json={
            "organizationId": org["id"],
            "organizationName": org["name"],
            "name": sensor_name,
            "externalId": str(uuid.uuid4()),
            "latitude": municipality.get("centroidLatitude", 18.4655),
            "longitude": municipality.get("centroidLongitude", -66.1057),
            "municipalityId": municipality["id"],
            "expectedIntervalSeconds": INTERVAL
        }
    )

    if response.status_code != 200:
        print(f"Failed to register sensor: {response.status_code} - {response.text}")
        return None

    return response.json()


def send_readings(session: requests.Session, sensor_id: str, sensor_token: str):
    """Send a batch of readings to the API using sensor JWT."""
    url = f"{BASE_URL}/sensors/sensors/{sensor_id}/readings"
    headers = {"Authorization": f"Bearer {sensor_token}"}

    readings = [generate_reading(param) for param in PARAMETERS]
    payload = {
        "sensorId": sensor_id,
        "readings": readings,
    }

    try:
        response = session.post(url, json=payload, headers=headers)
        response.raise_for_status()
        result = response.json()
        print(f"[{datetime.now().strftime('%H:%M:%S')}] Sent {result['accepted']}/{result['totalSubmitted']} readings")
        if result.get("errors"):
            for error in result["errors"]:
                print(f"  Error: {error}")
    except requests.exceptions.RequestException as e:
        print(f"[{datetime.now().strftime('%H:%M:%S')}] Error: {e}")


def main():
    print("Development Sensor Simulator")
    print("============================\n")

    session = requests.Session()
    session.verify = False

    # Get credentials
    email = input("Email [admin@gmail.com]: ").strip() or "admin@gmail.com"
    password = input("Password [Admin@123]: ").strip() or "Admin@123"

    # Login
    print("\nLogging in...")
    login_data = login(session, email, password)
    if not login_data:
        sys.exit(1)

    user_token = login_data["token"]
    user_info = login_data["user"]
    print(f"Welcome {user_info['displayName']}!")

    session.headers.update({"Authorization": f"Bearer {user_token}"})

    # Get organization
    print("Fetching organization...")
    org = get_organization(session)
    if not org:
        print("No organizations found!")
        sys.exit(1)
    print(f"Using: {org['name']}")

    # Get municipality
    print("Fetching municipality...")
    municipality = get_municipality(session)
    if not municipality:
        print("No municipalities found!")
        sys.exit(1)
    print(f"Using: {municipality['name']}")

    # Register sensor
    print("Registering sensor...")
    credentials = register_sensor(session, org, municipality)
    if not credentials:
        sys.exit(1)

    sensor_id = credentials["sensorId"]
    sensor_token = credentials["accessToken"]
    print(f"Sensor ID: {sensor_id}")

    print(f"\nSending readings every {INTERVAL} seconds...")
    print("Press Ctrl+C to stop\n")

    try:
        while True:
            send_readings(session, sensor_id, sensor_token)
            time.sleep(INTERVAL)
    except KeyboardInterrupt:
        print("\n\nStopping simulator...")


if __name__ == "__main__":
    main()
