# mycelium-bloom
mycelium bloom is the end user application that allows users to create and manipulate SysML v2 models

## Mycelium SDK integration

Bloom uses Mycelium.SDK 0.1.0 for the project lifecycle contract and registers
Mycelium.SDK.Serializer.Json 0.1.0 serialization services through
`AddApplicationServices()`. The stateless platform JSON services are shared;
SysML model assembly, loading, context, and selection retain their circuit scope.
Production uses the SDK lifecycle enumeration in application context and
navigation contracts. The platform JSON services are registered as a DI boundary
and exercised by integration tests; no production feature currently resolves
them or exchanges platform DTO payloads.

Mycelium.SDK 0.1.0 contains Fabric platform DTOs, POCOs, enumerations, JSON and
MessagePack serializers, and DTO validators. It does not provide SysML model
types, a model assembler, an HTTP client, or context and selection services.
Bloom therefore retains SysML2.NET.Dal and SysML2.NET.Serializer.Json 0.23.0
for the local Quantities model and canonical SysML element identity. Platform
and SysML JSON services use their respective SDK interfaces and payloads.
The platform JSON deserializer requires non-null values for required string
fields, including a project's name and description; DTO property defaults alone
do not guarantee a payload that can be deserialized.

The project switcher item remains a UI projection, including presentation and
interaction fields. No backend project loading is introduced. MessagePack and
validation packages are omitted because Bloom has no corresponding consumers.

## Container hosting

Bloom includes generated runtime tokens and Tailwind theme mappings. A normal
checkout needs no external token workspace or token package. See
[shared theme consumption](Mycelium.Bloom/Styles/README.md) for build prerequisites
and the external DTCG update workflow; do not edit generated token values manually.

Build and start the local container with `docker compose up --build -d`. Compose publishes Bloom on loopback for a same-host TLS-terminating reverse proxy. Its default Docker bridge uses `172.30.0.0/24`, and Bloom trusts forwarded headers from the `172.30.0.1` bridge gateway in addition to framework loopback defaults. Set `BLOOM_DOCKER_SUBNET` and `BLOOM_REVERSE_PROXY_IP` together when the host requires a different subnet; the proxy address must match the actual bridge gateway.

The proxy must preserve the original Host header, set `X-Forwarded-For` and `X-Forwarded-Proto`, and support WebSocket upgrades for `/_blazor`. Bloom processes one trusted forwarding hop before HTTPS redirection and HSTS. For a different hosting topology, set `ReverseProxy__KnownProxy` to the immediate proxy's address, not the public client address. Do not enable `ASPNETCORE_FORWARDEDHEADERS_ENABLED`, which bypasses the explicit proxy trust restriction. Configure `AllowedHosts` for the deployment's public host names and restrict direct container access to the proxy.

`/healthz` reports liveness and `/ready` reports readiness once the application can serve requests. Bloom currently has no external startup dependencies, so both use the framework's default application health check. The container serves HTTP on port 8080; TLS belongs at the proxy. If configuring `ASPNETCORE_HTTPS_PORT` for application-side redirects, send probes through HTTPS at the proxy as well.

The final image contains the ASP.NET runtime, the published application, static assets, and the local `Quantities.json` model payload. The .NET SDK remains in the build stage. Docker consumes committed CSS; it never installs or runs Node, npm or Tailwind. The application runs as the image's non-root app user. Persistent data-protection key storage and certificates must be configured by the hosting platform when continuity across container replacement or multiple replicas is required; production provisioning is outside this repository's local Compose setup.

The Dockerfile pins the .NET images by release tag; explicit CSS regeneration and verification pin the standalone Tailwind version and SHA-256 digests in `Mycelium.Bloom/Build/Tailwind.targets`. Update the platform digests from the official release when changing the CLI version. Validate security updates with a clean `docker build --pull --no-cache -t mycelium-bloom:hosting .`, runtime/proxy smoke tests, and `docker scout cves mycelium-bloom:hosting`. Check NuGet vulnerabilities with `dotnet list Mycelium.Bloom.sln package --vulnerable --include-transitive` and JavaScript test dependencies with `npm audit` from `Mycelium.Bloom`. Investigate any High/Critical findings before deployment.
