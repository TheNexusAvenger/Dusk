# Dusk
Dusk is a bridge between systems for synchronizing the clipboard and opening
applications *(currently planned but not implemented)*.

It is intended to be used with Sunshine/Moonlight remote streaming setups that
don't have clipboard syncing.

## Server Setup
The server is intended to be hosted on another system running Docker. Port 23594
must be open for TCP traffic.

```
docker compose up --build -d
```

Once created, the configuration can be found in `configuration/settings-server.json`.
The main configuration of note is `Domains`. In most cases, there will be only one,
but multiple are supported. **You should change the default `Secret`**, but the name
is arbitrary and not stored on the client configurations.

## Client Setup
### Windows
`scripts/setup-windows.py` is set up to create a scheduled task through Task Scheduler
on login. It requires Python 3 and .NET to be installed. **It must be run as administrator
to create and run scheduled tasks.**

The configuration is stored in `%APPDATA%/Dusk/settings-client.json`.

### Linux (Wayland)
Dusk only supports Wayland desktop environments. No included setup is currently
provided, but `wl-clipboard` must be in the `PATH` when running. Running as a
systemd user process is recommended.

The configuration is stored in `~/.config/Dusk/settings-client.json`.

### Configuration
In `Connection`, the `Host` must be changed to the host of the server, and the
`Secret` must match the one on the server.

## License
Dusk is available under the terms of the MIT  License. See [LICENSE](LICENSE)
for details.