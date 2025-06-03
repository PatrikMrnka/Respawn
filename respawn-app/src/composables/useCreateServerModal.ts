import Swal, { type SweetAlertOptions } from 'sweetalert2'
import { GameType, ServerStatus } from '@/types/enums'
import { useGameDisplayHelpers } from '@/composables/useGameDisplayHelpers'
import {
  mdiServerPlus,
  mdiServer,
  mdiGamepadVariant,
  mdiTuneVariant,
  mdiAlertOctagon,
  mdiCheckCircle,
} from '@mdi/js'

// Definition of server data type from the form
export interface CreateServerFormData {
  name: string
  gameType: GameType
  additionalGsParams?: string
}

// Definition of API response type
interface CreatedServerResponse {
  gameServerId: string
  name: string
  gameType: GameType
  status: ServerStatus
  ipAddress?: string
  port?: number
  containerId?: string
  createdAt: string
  statusDetails?: string
}

/**
 * Composable function for displaying and processing a modal window for creating a new game server.
 * @param createServerApiCall Asynchronous function that calls the API to create a server.
 * Expects a CreateServerFormData object and returns Promise<CreatedServerResponse | null>.
 */
export function useCreateServerModal(
  createServerApiCall: (data: CreateServerFormData) => Promise<CreatedServerResponse | null>,
) {
  const { getGameTypeText } = useGameDisplayHelpers()

  const createIconHtml = (
    pathData: string,
    size: number = 20,
    color: string = '#FFFFFF',
    extraStyle: string = '',
  ): string => {
    return `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" role="img" aria-hidden="true" width="${size}" height="${size}" fill="${color}" style="vertical-align: middle; margin-right: 10px; ${extraStyle}"><path d="${pathData}"></path></svg>`
  }

  const getFuturisticSwalBaseOptions = (title: string): SweetAlertOptions => ({
    titleText: title,
    background: 'rgba(10, 20, 40, 0.9)',
    color: '#E0E0E0',
    confirmButtonColor: '#00E0FF',
    cancelButtonColor: '#FF5252',
    buttonsStyling: false,
    heightAuto: false,
    allowEnterKey: true,
    customClass: {
      popup: 'futuristic-swal-popup',
      title: 'futuristic-swal-title font-oxanium',
      htmlContainer: 'futuristic-swal-html-container font-inter',
      input: 'futuristic-swal-input',
      confirmButton: 'futuristic-swal-confirm-button futuristic-btn',
      cancelButton: 'futuristic-swal-cancel-button futuristic-btn',
      actions: 'futuristic-swal-actions',
      validationMessage: 'futuristic-swal-validation-message font-inter',
    },
    backdrop: `
            rgba(0,0,0,0.6)
            url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='100' height='100' viewBox='0 0 100 100'%3E%3Cg fill-rule='evenodd'%3E%3Cg fill='%2300e0ff' fill-opacity='0.07'%3E%3Cpath opacity='.5' d='M96 95h4v1h-4v4h-1v-4h-9v4h-1v-4h-9v4h-1v-4h-9v4h-1v-4h-9v4h-1v-4h-9v4h-1v-4h-9v4h-1v-4h-9v4h-1v-4h-9v4h-1v-4H0v-1h15v-9H0v-1h15v-9H0v-1h15v-9H0v-1h15v-9H0v-1h15v-9H0v-1h15v-9H0v-1h15v-9H0v-1h15v-9H0v-1h15V0h1v15h9V0h1v15h9V0h1v15h9V0h1v15h9V0h1v15h9V0h1v15h9V0h1v15h9V0h1v15h4v1h-4v9h4v1h-4v9h4v1h-4v9h4v1h-4v9h4v1h-4v9h4v1h-4v9h4v1h-4v9h4v1h-4v9zm-1 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-9-10h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm9-10v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-9-10h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm9-10v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-P9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-9-10h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9z'/%3E%3Cpath d='M6 5V0H5v5H0v1h5v94h1V6h94V5H6z'/%3E%3C/g%3E%3C/g%3E%3C/svg%3E")
            center/100px 100px repeat
            fixed
        `,
  })

  const openModal = () => {
    const gameTypesOptions = Object.values(GameType)
      .filter((value) => typeof value === 'number') // Only numeric enum values
      .map((value) => ({ text: getGameTypeText(value as GameType), value: value as GameType }))

    let gameTypesHtml = gameTypesOptions
      .map(
        (opt) =>
          `<option value="${opt.value}" class="futuristic-select-option">${opt.text}</option>`,
      )
      .join('')

    const nameIconSvg = createIconHtml(mdiServer)
    const gameTypeIconSvg = createIconHtml(mdiGamepadVariant)
    const paramsIconSvg = createIconHtml(mdiTuneVariant)

    Swal.fire({
      ...getFuturisticSwalBaseOptions('Nový server'),
      html: `
            <div class="swal-form-container">
                  <label for="swal-server-name" class="swal-label">
                    ${nameIconSvg}Název:
                    </label>
                  <input id="swal-server-name" class="swal2-input futuristic-input" placeholder="MujSkvelyServer">
                    <label for="swal-game-type" class="swal-label mt-3">${gameTypeIconSvg}Hra:</label>
                    <select id="swal-game-type" class="swal2-input futuristic-input futuristic-select"> ${gameTypesHtml} </select>
                    <label for="swal-gs-params" class="swal-label mt-3">${paramsIconSvg}LinuxGSM parametry:</label>
                    <input id="swal-gs-params" style="text-align:center;" class="swal2-input futuristic-input" placeholder="Momentalne se nepouziva!">

            </div>`,
      confirmButtonText: 'Vytvořit server',
      showCancelButton: true,
      cancelButtonText: 'Zrušit',
      focusConfirm: false,
      showLoaderOnConfirm: true,
      preConfirm: () => {
        const nameInput = document.getElementById('swal-server-name') as HTMLInputElement
        const gameTypeSelect = document.getElementById('swal-game-type') as HTMLSelectElement
        const paramsInput = document.getElementById('swal-gs-params') as HTMLInputElement

        const name = nameInput.value
        const gameType = parseInt(gameTypeSelect.value) as GameType
        const additionalGsParams = paramsInput.value

        if (!name || name.length < 3) {
          Swal.showValidationMessage('Název serveru musí mít alespoň 3 znaky.')
          return false
        }
        if (isNaN(gameType)) {
          Swal.showValidationMessage('Vyberte platný typ hry.')
          return false
        }
        return { name, gameType, additionalGsParams }
      },
    }).then(async (result) => {
      if (result.isConfirmed && result.value) {
        const formData = result.value as CreateServerFormData
        try {
          const createdServer = await createServerApiCall(formData)
          if (createdServer) {
            Swal.fire({
              ...getFuturisticSwalBaseOptions('Server vytvořen!'),
              html: `<div class="font-inter">Požadavek na vytvoření serveru <strong>${createdServer.name}</strong> byl odeslán.<br>Status se brzy aktualizuje!</div>`,
              icon: 'success',
              timer: 3500,
              showConfirmButton: false,
            })
          } else {
            Swal.fire({
              ...getFuturisticSwalBaseOptions('Error!'),
              html: '<div class="font-inter">Chybné zpracování požadavku.</div>',
              icon: 'error',
            })
          }
        } catch (err: any) {
          console.error('Error calling createServerApiCall from modal:', err)
        }
      }
    })
  }
  return {
    openCreateServerModal: openModal,
  }
}
