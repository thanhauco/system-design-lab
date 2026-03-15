# Infrastructure

Deployment and platform-engineering assets: Docker, Kubernetes, Helm, Terraform, and service mesh.

| Asset | Status | Location |
|-------|--------|----------|
| Docker Compose (local platform) | ✅ | [../docker-compose.yml](../docker-compose.yml) |
| Monolith Dockerfile | ✅ | [../services/monolith/Dockerfile](../services/monolith/Dockerfile) |
| Kubernetes manifests | 🗺️ Phase 5 | `kubernetes/` |
| Helm charts | 🗺️ Phase 5 | `helm/` |
| Terraform modules | 🗺️ Phase 5 | `terraform/` |
| Service mesh (Istio) | 🗺️ Phase 5 | `mesh/` |

The local platform runs entirely from Docker Compose today. Kubernetes, Helm, Terraform, and the
service mesh arrive in Phase 5 (Cloud Native) — see [docs/roadmap.md](../docs/roadmap.md).
