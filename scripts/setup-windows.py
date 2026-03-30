# Sets up Dusk as a Windows Task Scheduler task.
# This script can run standalone and download Dusk automatically.
# The schtasks /create command requires admin.

import os
import shutil
import subprocess
import tempfile
import urllib.request
import uuid
import zipfile

# Get the Dusk path, and download Dusk if it is running standalone.
print("Getting Dusk build path.")
duskPath = os.path.realpath(os.path.join(os.path.dirname(__file__), ".."))
if not os.path.exists(os.path.join(duskPath, "Dusk.sln")):
    print("Running in standalone mode. Downloading Dusk archive from GitHub.")

    # Download the Dusk ZIP file.
    duskExtractPath = os.path.join(tempfile.gettempdir(), "Dusk-" + str(uuid.uuid4()))
    duskDownloadPath = duskExtractPath + ".zip"
    print("Downloading Dusk archive to " + duskDownloadPath)
    with urllib.request.urlopen("https://github.com/TheNexusAvenger/Dusk/archive/refs/heads/master.zip") as response, open(duskDownloadPath, "wb") as file:
        # Efficiently copy the response stream to the local file
        shutil.copyfileobj(response, file)

    # Extract the ZIP file.
    print("Extracting Dusk archive to " + duskExtractPath)
    with zipfile.ZipFile(duskDownloadPath, "r") as zipFile:
        zipFile.extractall(duskExtractPath)
    duskPath = os.path.join(duskExtractPath, os.listdir(duskExtractPath)[0])

# Build Dusk.
print("Building Dusk in " + duskPath)
buildProcess = subprocess.Popen(["dotnet", "publish", "Dusk/Dusk.csproj", "-c", "Release", "-r", "win-x64", "-o", "bin"], cwd=duskPath)
buildProcess.wait()
if buildProcess.returncode != 0:
    raise Exception("Build failed with exit code " + str(buildProcess.returncode))

# Copy Dusk to the AppData path.
duskAppDataFolder = os.path.join(os.getenv("APPDATA"), "Dusk")
if not os.path.exists(duskAppDataFolder):
    os.mkdir(duskAppDataFolder)
duskAppPath = os.path.join(duskAppDataFolder, "Dusk.exe")
print("Copying Dusk to " + duskAppPath)
shutil.copy2(os.path.join(duskPath, "bin", "Dusk.exe"), duskAppPath)

# Create the scheduled task.
print("Creating scheduled task.")
scheduleCreateTask = subprocess.Popen(["schtasks", "/create", "/tn", "DuskClient", "/tr", duskAppPath + " run", "/sc", "onlogon", "/it", "/f"])
scheduleCreateTask.wait()
if scheduleCreateTask.returncode != 0:
    raise Exception("Creating scheduled task failed with exit code " + str(scheduleCreateTask.returncode) + ". It might need to be run as administrator.")

# Start the scheduled task.
print("Starting scheduled task.")
scheduleRunTask = subprocess.Popen(["schtasks", "/run", "/tn", "DuskClient"])
scheduleRunTask.wait()
if scheduleRunTask.returncode != 0:
    raise Exception("Running scheduled task failed with exit code " + str(scheduleCreateTask.returncode))