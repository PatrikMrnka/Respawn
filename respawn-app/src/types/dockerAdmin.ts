// src/types/dockerAdmin.ts

export interface DockerVolumeDto {
  name: string;
  driver: string;
  createdAt: string; // ISO string
  sizeBytes: number;
  labels: Record<string, string>;
}

export interface DockerContainerPortDto {
  privatePort: number;
  publicPort?: number; // Může chybět, pokud není mapován na hosta
  type: string;
  ip?: string;
}

export interface DockerContainerDto {
  id: string;
  names: string[];
  image: string;
  imageId: string;
  command: string;
  created: string; // ISO string, ale Docker API vrací Unix timestamp, nutno konvertovat
  ports: DockerContainerPortDto[];
  state: string; // např. "running", "exited"
  status: string; // např. "Up 2 hours", "Exited (0) 5 minutes ago"
  labels: Record<string, string>;
}

export interface DockerImageDto {
  id: string; // Krátké ID
  fullId: string;
  repoTags: string[];
  repoDigests: string[];
  created: string; // ISO string, ale Docker API vrací Unix timestamp
  size: number;
  virtualSize: number;
  labels: Record<string, string>;
  containers: number;
}
