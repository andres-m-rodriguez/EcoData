#!/usr/bin/env python3
"""
Sensor simulator script that logs in, creates a sensor, and sends readings every 10 seconds.
"""

import requests
import time
import random
import uuid
import sys

BASE_URL = "https://localhost:5000"

# Disable SSL warnings for local dev
import urllib3
urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)


def main():
    session = requests.Session()
    session.verify = False  # Allow self-signed certs for local dev

    # 1. Login as admin
    print("Logging in as admin...")
    login_response = session.post(
        f"{BASE_URL}/api/auth/login",
        json={"email": "admin@gmail.com", "password": "Admin@123"}
    )

    if login_response.status_code != 200:
        print(f"Login failed: {login_response.status_code} - {login_response.text}")
        sys.exit(1)

    print("Login successful!")

    # 2. Get organizations to find one to use
    print("Fetching organizations...")
    orgs_response = session.get(f"{BASE_URL}/api/organizations")

    if orgs_response.status_code != 200:
        print(f"Failed to get organizations: {orgs_response.status_code}")
        sys.exit(1)

    orgs = orgs_response.json()
    if not orgs:
        print("No organizations found!")
        sys.exit(1)

    org = orgs[0]
    org_id = org["id"]
    org_name = org["name"]
    print(f"Using organization: {org_name} ({org_id})")

    # 3. Get municipalities for location
    print("Fetching municipalities...")
    municipalities_response = session.get(f"{BASE_URL}/api/municipalities?pageSize=1")

    if municipalities_response.status_code != 200:
        print(f"Failed to get municipalities: {municipalities_response.status_code}")
        sys.exit(1)

    municipalities = municipalities_response.json()
    if not municipalities:
        print("No municipalities found!")
        sys.exit(1)

    municipality = municipalities[0]
    municipality_id = municipality["id"]
    latitude = municipality.get("centroidLatitude", 18.4655)
    longitude = municipality.get("centroidLongitude", -66.1057)
    print(f"Using municipality: {municipality['name']}")

    # 4. Register a new sensor
    print("Registering new sensor...")
    sensor_name = f"Simulator-{uuid.uuid4().hex[:8]}"
    reading_interval = 10  # seconds between readings
    register_response = session.post(
        f"{BASE_URL}/api/sensors/register",
        json={
            "organizationId": org_id,
            "organizationName": org_name,
            "name": sensor_name,
            "externalId": str(uuid.uuid4()),
            "latitude": latitude,
            "longitude": longitude,
            "municipalityId": municipality_id,
            "expectedIntervalSeconds": reading_interval
        }
    )

    if register_response.status_code != 200:
        print(f"Failed to register sensor: {register_response.status_code} - {register_response.text}")
        sys.exit(1)

    credentials = register_response.json()
    sensor_id = credentials["sensorId"]
    access_token = credentials["accessToken"]
    print(f"Sensor registered: {sensor_name} ({sensor_id})")
    print(f"Access token: {access_token[:20]}...")

    # 5. Send readings every 10 seconds
    print(f"\nStarting to send readings every 10 seconds...")
    print(f"View live at: {BASE_URL}/sensors/{sensor_id}")
    print("Press Ctrl+C to stop\n")

    reading_count = 0
    while True:
        try:
            reading_count += 1
            temperature = round(20 + random.uniform(-5, 10), 2)
            ph = round(7 + random.uniform(-1, 1), 2)
            dissolved_oxygen = round(8 + random.uniform(-2, 2), 2)

            # Use sensor JWT auth for posting readings
            headers = {"Authorization": f"Bearer {access_token}"}

            reading_response = session.post(
                f"{BASE_URL}/api/sensors/{sensor_id}/readings",
                json={
                    "sensorId": sensor_id,
                    "readings": [
                        {"parameter": "Temperature", "value": temperature, "unit": "°C", "recordedAt": time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime())},
                        {"parameter": "pH", "value": ph, "unit": "pH", "recordedAt": time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime())},
                        {"parameter": "Dissolved Oxygen", "value": dissolved_oxygen, "unit": "mg/L", "recordedAt": time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime())}
                    ]
                },
                headers=headers
            )

            if reading_response.status_code == 200:
                result = reading_response.json()
                print(f"[{reading_count}] Sent: Temp={temperature}°C, pH={ph}, DO={dissolved_oxygen}mg/L (accepted: {result.get('accepted', 'N/A')})")
            else:
                print(f"[{reading_count}] Failed to send reading: {reading_response.status_code} - {reading_response.text}")

            time.sleep(10)

        except KeyboardInterrupt:
            print("\n\nStopping simulator...")
            break
        except Exception as e:
            print(f"Error: {e}")
            time.sleep(10)


if __name__ == "__main__":
    main()
