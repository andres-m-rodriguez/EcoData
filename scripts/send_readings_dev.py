#!/usr/bin/env python3
"""
Sensor reading simulator for EcoData (Development).
Sends random readings to the local API using a sensor JWT token.
"""

import requests
import random
import time
from datetime import datetime, timezone

# Configuration - Development defaults
BASE_URL = "https://localhost:5000"
INTERVAL = 30
JWT_TOKEN = ""
SENSOR_ID = ""

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


def send_readings():
    """Send a batch of readings to the API."""
    url = f"{BASE_URL}/api/sensors/{SENSOR_ID}/readings"
    headers = {
        "Authorization": f"Bearer {JWT_TOKEN}",
        "Content-Type": "application/json",
    }

    readings = [generate_reading(param) for param in PARAMETERS]
    payload = {
        "sensorId": SENSOR_ID,
        "readings": readings,
    }

    try:
        # Disable SSL verification for local development
        response = requests.post(url, json=payload, headers=headers, verify=False)
        response.raise_for_status()
        result = response.json()
        print(f"[{datetime.now().strftime('%H:%M:%S')}] Sent {result['accepted']}/{result['totalSubmitted']} readings")
        if result.get("errors"):
            for error in result["errors"]:
                print(f"  Error: {error}")
    except requests.exceptions.RequestException as e:
        print(f"[{datetime.now().strftime('%H:%M:%S')}] Error: {e}")


def parse_jwt_sensor_id(token: str) -> str | None:
    """Extract sensor_id from JWT token payload."""
    try:
        import base64
        import json
        # JWT is header.payload.signature - we want the payload
        payload = token.split(".")[1]
        # Add padding if needed
        padding = 4 - len(payload) % 4
        if padding != 4:
            payload += "=" * padding
        decoded = base64.urlsafe_b64decode(payload)
        data = json.loads(decoded)
        return data.get("sensor_id") or data.get("sub")
    except Exception:
        return None


def main():
    global JWT_TOKEN, SENSOR_ID

    print("Development Sensor Simulator")
    print("============================\n")

    # Get JWT token from input
    JWT_TOKEN = input("Paste JWT token: ").strip()

    if not JWT_TOKEN:
        print("Error: JWT token required")
        return

    # Extract sensor ID from JWT
    SENSOR_ID = parse_jwt_sensor_id(JWT_TOKEN)

    if not SENSOR_ID:
        print("Error: Could not extract sensor ID from token")
        return

    # Suppress SSL warnings for local dev
    import urllib3
    urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

    print(f"\nAPI URL:   {BASE_URL}")
    print(f"Sensor ID: {SENSOR_ID}")
    print(f"Interval:  {INTERVAL} seconds")
    print("\nPress Ctrl+C to stop\n")

    while True:
        send_readings()
        time.sleep(INTERVAL)


if __name__ == "__main__":
    main()
