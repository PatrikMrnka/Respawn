// File: haha/respawn-app/src/composables/useCreateServerModal.ts
import Swal, { type SweetAlertOptions } from 'sweetalert2';
import { GameType, ServerStatus } from '@/types/enums';
import { useGameDisplayHelpers } from '@/composables/useGameDisplayHelpers'; // Pro getGameTypeText
import { 
    mdiServerPlus, mdiServer, mdiGamepadVariant, mdiTuneVariant, 
    mdiRefresh, mdiAlertOctagon, mdiCheckCircle // Ikony pro notifikace
} from '@mdi/js';

// Definice typu pro data serveru z formuláře
export interface CreateServerFormData {
  name: string;
  gameType: GameType;
  additionalGsParams?: string;
}

// Definice typu pro návratovou hodnotu z API
interface CreatedServerResponse {
  gameServerId: string;
  name: string;
  gameType: GameType;
  status: ServerStatus;
  ipAddress?: string;
  port?: number;
  containerId?: string;
  createdAt: string;
  statusDetails?: string;
}


/**
 * Composable funkce pro zobrazení a zpracování modálního okna pro vytvoření nového herního serveru.
 * @param createServerApiCall Asynchronní funkce, která volá API pro vytvoření serveru.
 * Očekává objekt CreateServerFormData a vrací Promise<CreatedServerResponse | null>.
 */
export function useCreateServerModal(
    createServerApiCall: (data: CreateServerFormData) => Promise<CreatedServerResponse | null>
) {
    const { getGameTypeText } = useGameDisplayHelpers();

    const createIconHtml = (pathData: string, size: number = 20, color: string = '#FFFFFF', extraStyle: string = ''): string => {
        return `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" role="img" aria-hidden="true" width="${size}" height="${size}" fill="${color}" style="vertical-align: middle; margin-right: 10px; ${extraStyle}"><path d="${pathData}"></path></svg>`;
    };
    
    const getFuturisticSwalBaseOptions = (title: string, iconHtml?: string): SweetAlertOptions => ({
        titleText: title,
        iconHtml: iconHtml,
        background: 'rgba(10, 20, 40, 0.9)',
        color: '#E0E0E0',
        confirmButtonColor: '#00E0FF',
        cancelButtonColor: '#FF5252',
        customClass: {
            popup: 'futuristic-swal-popup swal2-backdrop-show large-swal animated-border',
            title: 'futuristic-swal-title font-oxanium',
            htmlContainer: 'futuristic-swal-html-container font-inter swal-form-container-custom-padding',
            input: 'futuristic-swal-input', // Bude použito pro inputy v HTML
            confirmButton: 'futuristic-swal-confirm-button futuristic-btn futuristic-glow-cyan',
            cancelButton: 'futuristic-swal-cancel-button futuristic-btn futuristic-glow-red',
            actions: 'futuristic-swal-actions',
            validationMessage: 'futuristic-swal-validation-message font-inter',
            icon: 'futuristic-swal-icon',
        },
        buttonsStyling: false,
        heightAuto: false,
        allowEnterKey: true,
        backdrop: `
            rgba(0,0,0,0.6)
            url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='100' height='100' viewBox='0 0 100 100'%3E%3Cg fill-rule='evenodd'%3E%3Cg fill='%2300e0ff' fill-opacity='0.07'%3E%3Cpath opacity='.5' d='M96 95h4v1h-4v4h-1v-4h-9v4h-1v-4h-9v4h-1v-4h-9v4h-1v-4h-9v4h-1v-4h-9v4h-1v-4h-9v4h-1v-4h-9v4h-1v-4h-9v4h-1v-4H0v-1h15v-9H0v-1h15v-9H0v-1h15v-9H0v-1h15v-9H0v-1h15v-9H0v-1h15v-9H0v-1h15v-9H0v-1h15v-9H0v-1h15V0h1v15h9V0h1v15h9V0h1v15h9V0h1v15h9V0h1v15h9V0h1v15h9V0h1v15h9V0h1v15h4v1h-4v9h4v1h-4v9h4v1h-4v9h4v1h-4v9h4v1h-4v9h4v1h-4v9h4v1h-4v9h4v1h-4v9zm-1 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-9-10h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm9-10v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-9-10h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm9-10v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-P9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-9-10h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9z'/%3E%3Cpath d='M6 5V0H5v5H0v1h5v94h1V6h94V5H6z'/%3E%3C/g%3E%3C/g%3E%3C/svg%3E")
            center/100px 100px repeat
            fixed
        `
    });


    const openModal = () => {
        const gameTypesOptions = Object.values(GameType)
            .filter(value => typeof value === 'number') // Jen číselné hodnoty enumu
            .map(value => ({ text: getGameTypeText(value as GameType), value: value as GameType }));
        
        let gameTypesHtml = gameTypesOptions
            .map(opt => `<option value="${opt.value}" class="futuristic-select-option">${opt.text}</option>`)
            .join('');

        const serverIconSvg = createIconHtml(mdiServerPlus, 24);
        const nameIconSvg = createIconHtml(mdiServer);
        const gameTypeIconSvg = createIconHtml(mdiGamepadVariant);
        const paramsIconSvg = createIconHtml(mdiTuneVariant);

        Swal.fire({
            ...getFuturisticSwalBaseOptions('Vytvořit Nový Herní Server', serverIconSvg),
            html: `
            <div class="swal-form-container futuristic-form">
                <div class="futuristic-input-group">
                    <label for="swal-server-name" class="futuristic-label">${nameIconSvg}Název serveru:</label>
                    <input id="swal-server-name" class="swal2-input futuristic-input" placeholder="Např. Můj CS 1.6 Server">
                </div>
                <div class="futuristic-input-group">
                    <label for="swal-game-type" class="futuristic-label mt-3">${gameTypeIconSvg}Typ hry:</label>
                    <select id="swal-game-type" class="swal2-input futuristic-input futuristic-select"> ${gameTypesHtml} </select>
                </div>
                <div class="futuristic-input-group">
                    <label for="swal-gs-params" class="futuristic-label mt-3">${paramsIconSvg}Extra GS_PARAMS (volitelné):</label>
                    <input id="swal-gs-params" class="swal2-input futuristic-input" placeholder="Např. -port 27016 +map de_dust2">
                </div>
            </div>`,
            confirmButtonText: 'Vytvořit Server',
            showCancelButton: true,
            cancelButtonText: 'Zrušit',
            focusConfirm: false,
            showLoaderOnConfirm: true,
            preConfirm: () => {
                const nameInput = document.getElementById('swal-server-name') as HTMLInputElement;
                const gameTypeSelect = document.getElementById('swal-game-type') as HTMLSelectElement;
                const paramsInput = document.getElementById('swal-gs-params') as HTMLInputElement;

                const name = nameInput.value;
                const gameType = parseInt(gameTypeSelect.value) as GameType;
                const additionalGsParams = paramsInput.value;

                if (!name || name.length < 3) {
                    Swal.showValidationMessage('Název serveru musí mít alespoň 3 znaky.');
                    return false;
                }
                if (isNaN(gameType)) {
                    Swal.showValidationMessage('Musíte vybrat typ hry.');
                    return false;
                }
                return { name, gameType, additionalGsParams };
            }
        }).then(async (result) => {
            if (result.isConfirmed && result.value) {
                const formData = result.value as CreateServerFormData;
                try {
                    const createdServer = await createServerApiCall(formData);
                    if (createdServer) {
                         Swal.fire({...getFuturisticSwalBaseOptions('Vytváření zahájeno', createIconHtml(mdiCheckCircle, 24, '#00E0FF')), html: `<div class="font-inter">Požadavek na vytvoření serveru <strong>${createdServer.name}</strong> byl odeslán.<br>Stav se brzy aktualizuje.</div>`, icon: 'success', timer: 3500, showConfirmButton: false});
                        // Komponenta, která toto volá, by měla aktualizovat seznam serverů (např. přes SignalR nebo re-fetch)
                    } else {
                        // createServerApiCall vrátilo null, což by mělo být ošetřeno již v něm (Swal error)
                        // Pokud ne, zobrazit obecnou chybu zde
                         Swal.fire({...getFuturisticSwalBaseOptions('Chyba!', createIconHtml(mdiAlertOctagon, 24, '#FF5252')), html: '<div class="font-inter">Nepodařilo se zpracovat požadavek na vytvoření serveru.</div>', icon: 'error'});
                    }
                } catch (err: any) {
                    // Chyba by měla být již zobrazena Swalem v createServerApiCall, pokud je tam tak implementováno
                    // Pokud ne, zobrazit zde:
                    // Swal.fire({...getFuturisticSwalBaseOptions('Chyba!', createIconHtml(mdiAlertOctagon, 24, '#FF5252')), html: `<div class="font-inter">${err.message || 'Došlo k neočekávané chybě.'}</div>`, icon: 'error'});
                    console.error("Chyba při volání createServerApiCall z modalu:", err);
                }
            }
        });
    };

    return {
        openCreateServerModal: openModal
    };
}
