# mycelium-bloom
mycelium bloom is the end user application that allows users to create and manipulate SysML v2 models

## Container hosting

Bloom consumes the official `Mycelium.Design.Bloom` NuGet package. See
[shared theme consumption](Mycelium.Bloom/Styles/README.md) for package upgrades,
automatic standalone Tailwind compilation and published-contract limitations.

Build and start the local container with `docker compose up --build -d`. Compose publishes Bloom on loopback for a same-host TLS-terminating reverse proxy. Its default Docker bridge uses `172.30.0.0/24`, and Bloom trusts forwarded headers from the `172.30.0.1` bridge gateway in addition to framework loopback defaults. Set `BLOOM_DOCKER_SUBNET` and `BLOOM_REVERSE_PROXY_IP` together when the host requires a different subnet; the proxy address must match the actual bridge gateway.

The proxy must preserve the original Host header, set `X-Forwarded-For` and `X-Forwarded-Proto`, and support WebSocket upgrades for `/_blazor`. Bloom processes one trusted forwarding hop before HTTPS redirection and HSTS. For a different hosting topology, set `ReverseProxy__KnownProxy` to the immediate proxy's address, not the public client address. Do not enable `ASPNETCORE_FORWARDEDHEADERS_ENABLED`, which bypasses the explicit proxy trust restriction. Configure `AllowedHosts` for the deployment's public host names and restrict direct container access to the proxy.

`/healthz` reports liveness and `/ready` reports readiness once the application can serve requests. Bloom currently has no external startup dependencies, so both use the framework's default application health check. The container serves HTTP on port 8080; TLS belongs at the proxy. If configuring `ASPNETCORE_HTTPS_PORT` for application-side redirects, send probes through HTTPS at the proxy as well.

The final image contains the ASP.NET runtime, the published application, static assets, and the local `Quantities.json` model payload. The .NET SDK remains in the build stage. Docker stages package CSS and compiles app.css with the pinned standalone Tailwind CLI in the SDK stage; neither stage needs Node or npm. The application runs as the image's non-root app user. Persistent data-protection key storage and certificates must be configured by the hosting platform when continuity across container replacement or multiple replicas is required; production provisioning is outside this repository's local Compose setup.

The Dockerfile uses the floating `10.0-noble` .NET image tags, so a build with `--pull` always takes the latest 10.0 patch; CSS compilation and verification pin the standalone Tailwind version and SHA-256 digests in `Mycelium.Bloom/Build/Tailwind.targets`. Update the platform digests from the official release when changing the CLI version. Validate security updates with a clean `docker build --pull --no-cache -t mycelium-bloom:hosting .`, runtime/proxy smoke tests, and `docker scout cves mycelium-bloom:hosting`. Check NuGet vulnerabilities with `dotnet list Mycelium.Bloom.sln package --vulnerable --include-transitive` and JavaScript test dependencies with `npm audit` from `Mycelium.Bloom`. Investigate any High/Critical findings before deployment.
