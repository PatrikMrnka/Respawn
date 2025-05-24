// src/services/dockerAdminService.ts
import { useAuthStore } from '@/stores/authStore';
import type { DockerVolumeDto, DockerContainerDto, DockerImageDto } from '@/types/dockerAdmin'; // Předpokládáme, že typy budou zde
import Swal from 'sweetalert2';

const API_BASE_URL = 'http://localhost:5207/api/dockeradmin';

const getAuthHeaders = () => {
  const authStore = useAuthStore();
  return {
    'Authorization': `Bearer ${authStore.token}`,
    'Content-Type': 'application/json',
  };
};

const handleApiError = async (response: Response, defaultMessage: string) => {
    if (!response.ok) {
        try {
            const errorData = await response.json();
            throw new Error(errorData.message || errorData.title || defaultMessage);
        } catch (e) { // Pokud odpověď není JSON nebo json() selže
            throw new Error(`${defaultMessage} (Stav: ${response.status} ${response.statusText})`);
        }
    }
    // Pokud je odpověď OK, ale může být prázdná (např. 204 No Content)
    if (response.status === 204) return null;
    return response.json();
};


// Volumes
export const getVolumes = async (): Promise<DockerVolumeDto[]> => {
  const response = await fetch(`${API_BASE_URL}/volumes`, { headers: getAuthHeaders() });
  return handleApiError(response, 'Nepodařilo se načíst Docker volumes.');
};

export const deleteVolume = async (volumeName: string, force: boolean = false): Promise<boolean> => {
  const response = await fetch(`${API_BASE_URL}/volumes/${encodeURIComponent(volumeName)}?force=${force}`, {
    method: 'DELETE',
    headers: getAuthHeaders(),
  });
  if (!response.ok) {
    const errorData = await response.json().catch(() => ({ message: `Chyba při mazání volume ${volumeName}.` }));
    Swal.fire('Chyba', errorData.message, 'error');
    return false;
  }
  return true;
};

// Containers
export const getContainers = async (all: boolean = true): Promise<DockerContainerDto[]> => {
  const response = await fetch(`${API_BASE_URL}/containers?all=${all}`, { headers: getAuthHeaders() });
  return handleApiError(response, 'Nepodařilo se načíst Docker kontejnery.');
};

export const startContainer = async (containerId: string): Promise<boolean> => {
  const response = await fetch(`${API_BASE_URL}/containers/${containerId}/start`, {
    method: 'POST',
    headers: getAuthHeaders(),
  });
   if (!response.ok) {
    const errorData = await response.json().catch(() => ({ message: `Chyba při spouštění kontejneru ${containerId}.` }));
    Swal.fire('Chyba', errorData.message, 'error');
    return false;
  }
  return true;
};

export const stopContainer = async (containerId: string): Promise<boolean> => {
  const response = await fetch(`${API_BASE_URL}/containers/${containerId}/stop`, {
    method: 'POST',
    headers: getAuthHeaders(),
  });
   if (!response.ok) {
    const errorData = await response.json().catch(() => ({ message: `Chyba při zastavování kontejneru ${containerId}.` }));
    Swal.fire('Chyba', errorData.message, 'error');
    return false;
  }
  return true;
};

export const deleteContainer = async (containerId: string, removeVolume: boolean = false): Promise<boolean> => {
  const response = await fetch(`${API_BASE_URL}/containers/${containerId}?removeVolume=${removeVolume}`, {
    method: 'DELETE',
    headers: getAuthHeaders(),
  });
  if (!response.ok) {
    const errorData = await response.json().catch(() => ({ message: `Chyba při mazání kontejneru ${containerId}.` }));
    Swal.fire('Chyba', errorData.message, 'error');
    return false;
  }
  return true;
};

export const getContainerLogs = async (containerId: string, tail: number = 200): Promise<string> => {
  const response = await fetch(`${API_BASE_URL}/containers/${containerId}/logs?tail=${tail}`, {
    headers: getAuthHeaders(),
  });
  if (!response.ok) {
      const errorText = await response.text(); // Logy mohou být text, ne JSON
      throw new Error(errorText || `Nepodařilo se načíst logy kontejneru ${containerId}.`);
  }
  return response.text();
};

// Images
export const getImages = async (all: boolean = false): Promise<DockerImageDto[]> => {
  const response = await fetch(`${API_BASE_URL}/images?all=${all}`, { headers: getAuthHeaders() });
  return handleApiError(response, 'Nepodařilo se načíst Docker images.');
};

export const deleteImage = async (imageId: string, force: boolean = false, pruneChildren: boolean = false): Promise<boolean> => {
  const response = await fetch(`${API_BASE_URL}/images/${encodeURIComponent(imageId)}?force=${force}&pruneChildren=${pruneChildren}`, {
    method: 'DELETE',
    headers: getAuthHeaders(),
  });
  if (!response.ok) {
    const errorData = await response.json().catch(() => ({ message: `Chyba při mazání image ${imageId}.` }));
    Swal.fire('Chyba', errorData.message, 'error');
    return false;
  }
  return true;
};
