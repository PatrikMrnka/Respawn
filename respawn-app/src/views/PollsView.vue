<template>
  <v-container class="futuristic-page polls-view-container">
    <v-row justify="space-between" align="center" class="mb-6">
      <v-col>
        <h1 class="text-h3 font-oxanium page-title">Ankety</h1>
      </v-col>
      <v-col cols="auto">
        <v-btn color="primary" @click="openCreatePollModal" class="futuristic-btn" :prepend-icon="mdiPlusBox">
          Vytvořit anketu
        </v-btn>
      </v-col>
    </v-row>

     <v-alert v-if="signalRError" type="warning" density="compact" class="mb-4 font-inter" closable @click:close="signalRError = null">
      Chyba real-time spojení: {{ signalRError }}. Data se nemusí aktualizovat automaticky.
      <v-btn variant="text" size="small" @click="attemptReconnect" :loading="reconnectingSignalR">Zkusit znovu</v-btn>
    </v-alert>

    <v-progress-linear v-if="loading" indeterminate color="primary" class="mb-4"></v-progress-linear>
    <v-alert v-if="apiError" type="error" prominent class="mb-4 font-inter">{{ apiError }}</v-alert>

    <div v-if="!loading && polls.length === 0 && !apiError" class="text-center pa-8">
        <v-icon :icon="mdiPoll" size="64" color="grey-darken-1"></v-icon>
        <p class="text-h6 font-inter mt-4 text-grey-darken-1">Zatím nebyly vytvořeny žádné ankety.</p>
        <p class="font-inter text-grey-darken-1">Buďte první a vytvořte novou anketu!</p>
    </div>

    <v-row justify="center" v-if="!loading && polls.length > 0">
      <v-col v-for="poll in sortedPolls" :key="poll.pollId" cols="12" md="10" lg="8">
        <v-card class="futuristic-card poll-card mb-6" :class="{ 'closed-poll': isPollEffectivelyClosed(poll) }">
          <v-img v-if="poll.imageUrl" :src="poll.imageUrl" height="180px" cover class="poll-image">
            <template v-slot:placeholder>
              <v-row class="fill-height ma-0" align="center" justify="center">
                <v-progress-circular indeterminate color="grey-lighten-5"></v-progress-circular>
              </v-row>
            </template>
            <template v-slot:error>
                <v-row class="fill-height ma-0" align="center" justify="center" style="background-color: rgba(0,0,0,0.3);">
                    <v-icon :icon="mdiImageBrokenVariant" color="white" size="48"></v-icon>
                    <span class="ml-2 white--text">Obrázek nelze načíst</span>
                </v-row>
            </template>
          </v-img>
          <v-chip v-if="isPollEffectivelyClosed(poll)" color="grey" label small class="status-chip font-exo2">Uzavřeno</v-chip>
          <v-chip v-else-if="userHasVoted(poll.pollId)" color="info" label small class="status-chip font-exo2">Hlasováno</v-chip>
          <v-chip v-else color="success" label small class="status-chip font-exo2">Aktivní</v-chip>

          <v-card-title class="font-exo2 poll-question pt-4">{{ poll.question }}</v-card-title>
          <v-card-subtitle class="pb-0 font-inter">
            Vytvořil: {{ poll.creatorNickname || poll.creatorUserId }} |
            Konec: <span :title="formatFullDateTime(poll.endTime)">{{ formatRelativeTime(poll.endTime, currentTime) }}</span>
            <span v-if="poll.isMultipleChoice" class="ml-2">(Možnost více odpovědí)</span>
          </v-card-subtitle>

          <v-card-text class="pt-3">
            <div v-if="!isPollEffectivelyClosed(poll) && !userHasVoted(poll.pollId) && poll.options && poll.options.length > 0">
              <v-radio-group
                v-if="!poll.isMultipleChoice"
                v-model="selectedOptions[poll.pollId]"
                class="options-group"
              >
                <v-radio
                  v-for="option in poll.options"
                  :key="option.optionId"
                  :label="option.text"
                  :value="option.optionId"
                  color="primary"
                  class="option-item font-inter"
                >
                  <template v-slot:label>
                    <div>
                      <v-icon v-if="option.imageUrl" start :icon="mdiImageOutline" size="small" class="mr-1"></v-icon>
                      {{ option.text }}
                    </div>
                  </template>
                </v-radio>
              </v-radio-group>

              <div v-if="poll.isMultipleChoice" class="options-group">
                <v-checkbox
                  v-for="option in poll.options"
                  :key="option.optionId"
                  v-model="selectedOptions[poll.pollId]"
                  :label="option.text"
                  :value="option.optionId"
                  color="primary"
                  hide-details
                  class="option-item font-inter"
                >
                 <template v-slot:label>
                    <div>
                      <v-icon v-if="option.imageUrl" start :icon="mdiImageOutline" size="small" class="mr-1"></v-icon>
                      {{ option.text }}
                    </div>
                  </template>
                </v-checkbox>
              </div>

              <v-btn
                block
                color="secondary"
                @click="handleVote(poll.pollId)"
                class="mt-4 futuristic-btn"
                :disabled="!canSubmitVote(poll.pollId) || votingStates[poll.pollId]"
                :loading="votingStates[poll.pollId]"
              >
                Hlasovat
              </v-btn>
            </div>
            <div v-else-if="!isPollEffectivelyClosed(poll) && !userHasVoted(poll.pollId) && (!poll.options || poll.options.length === 0)" class="font-inter text-grey-darken-1">
                Tato anketa zatím nemá žádné možnosti hlasování.
            </div>

            <div v-if="isPollEffectivelyClosed(poll) || userHasVoted(poll.pollId)" class="results-section">
              <p class="font-inter mb-2 text-h6">Výsledky (Celkem hlasů: {{ poll.totalVotes || 0 }}):</p>
              <div v-if="!poll.options || poll.options.length === 0" class="font-inter text-grey-darken-1">
                Pro tuto anketu nebyly k dispozici žádné možnosti hlasování.
              </div>
              <v-list v-else lines="one" dense class="results-list">
                <v-list-item
                  v-for="option in getPollOptionsWithResults(poll)"
                  :key="option.optionId"
                  class="result-item"
                  :class="{ 'winning-option': isWinningOption(poll, option.optionId) }"
                >
                  <v-list-item-title class="font-inter d-flex align-center">
                     <v-icon v-if="option.imageUrl" start :icon="mdiImageOutline" size="small" class="mr-1"></v-icon>
                    {{ option.text }}
                    <v-icon v-if="didUserVoteForOption(poll, option.optionId)" color="primary" size="small" class="ml-2">{{ mdiCheckCircle }}</v-icon>
                  </v-list-item-title>
                  <template v-slot:append>
                    <span class="font-roboto-mono">{{ option.voteCount }} ({{ calculatePercentage(poll.totalVotes || 0, option.voteCount || 0) }}%)</span>
                  </template>
                   <v-progress-linear
                      :model-value="calculatePercentage(poll.totalVotes || 0, option.voteCount || 0)"
                      :color="isWinningOption(poll, option.optionId) ? 'primary' : 'grey-lighten-1'"
                      height="10"
                      rounded
                      class="mt-1 result-progress"
                    ></v-progress-linear>
                </v-list-item>
              </v-list>
            </div>
          </v-card-text>

          <v-card-actions v-if="canManagePoll(poll)" class="poll-actions">
            <v-spacer></v-spacer>
            <v-btn :icon="mdiPencil" color="warning" variant="text" @click="openEditPollModal(poll)" title="Upravit anketu"></v-btn>
            <v-btn :icon="mdiDelete" color="error" variant="text" @click="confirmDeletePoll(poll.pollId)" title="Smazat anketu"></v-btn>
          </v-card-actions>
        </v-card>
      </v-col>
    </v-row>
  </v-container>
</template>

<script setup lang="ts">
import { ref, onMounted, onBeforeUnmount, computed, reactive } from 'vue';
import Swal, {type SweetAlertOptions} from 'sweetalert2';
import { useAuthStore } from '@/stores/authStore';
import { UserRoles } from '@/types/enums';
import {
  getAllPolls, createPoll, updatePoll, deletePoll, submitVote,
  type Poll, type PollOption, type CreatePollDto, type UpdatePollDto, type UpdatePollOptionPayload
} from '@/services/pollService';
import { signalRService } from '@/services/signalrService';
import {
    mdiPlusBox, mdiPoll, mdiPencil, mdiDelete, mdiImageOutline, mdiImageBrokenVariant,
    mdiCalendarClock, mdiImage, mdiCheckboxMultipleBlankOutline, mdiFormatListBulletedSquare, mdiHelpCircleOutline,
    mdiCheckCircle, mdiTrashCanOutline
} from '@mdi/js';

const authStore = useAuthStore();
const polls = ref<Poll[]>([]);
const loading = ref(true);
const apiError = ref<string | null>(null);
const signalRError = ref<string | null>(null);
const reconnectingSignalR = ref(false);

const selectedOptions = reactive<Record<string, string | string[]>>({});
const votingStates = reactive<Record<string, boolean>>({});

const currentTime = ref(new Date());
let timeInterval: number | undefined;

const updateCurrentTimeAndPollsStatus = () => {
  currentTime.value = new Date();
  polls.value.forEach((poll) => { // Není třeba index, protože Vue reaktivita sleduje změny v objektech pole
    if (!poll.isClosed && new Date(poll.endTime) <= currentTime.value) {
      poll.isClosed = true; // Optimistická aktualizace na frontendu
    }
  });
};

// SignalR Handlery
const handleReceivePollUpdate = (updatedPollFromSignalR: Poll) => {
  console.log('SignalR: ReceivePollUpdate', updatedPollFromSignalR);
  const index = polls.value.findIndex(p => p.pollId === updatedPollFromSignalR.pollId);

  if (index !== -1) {
    const existingPoll = polls.value[index];
    const mergedUserVotedOptionIds =
      (updatedPollFromSignalR.userVotedOptionIds && updatedPollFromSignalR.userVotedOptionIds.length > 0)
        ? updatedPollFromSignalR.userVotedOptionIds
        : existingPoll.userVotedOptionIds;

    // isClosed z SignalR má přednost, nebo se odvodí z endTime, pokud není explicitně v datech
    const isNowClosedBySignalR = updatedPollFromSignalR.isClosed || (updatedPollFromSignalR.endTime && new Date(updatedPollFromSignalR.endTime) <= new Date());

    polls.value[index] = {
      ...existingPoll,
      ...updatedPollFromSignalR,
      isClosed: isNowClosedBySignalR,
      userVotedOptionIds: mergedUserVotedOptionIds,
    };

    const finalPollState = polls.value[index];
    if (finalPollState.userVotedOptionIds && finalPollState.userVotedOptionIds.length > 0) {
      selectedOptions[finalPollState.pollId] = finalPollState.isMultipleChoice
        ? [...finalPollState.userVotedOptionIds]
        : (finalPollState.userVotedOptionIds[0] || '');
    } else {
      selectedOptions[finalPollState.pollId] = finalPollState.isMultipleChoice ? [] : '';
    }
  } else {
    polls.value.unshift(updatedPollFromSignalR);
    selectedOptions[updatedPollFromSignalR.pollId] = updatedPollFromSignalR.isMultipleChoice
      ? []
      : (updatedPollFromSignalR.userVotedOptionIds && updatedPollFromSignalR.userVotedOptionIds.length > 0 ? updatedPollFromSignalR.userVotedOptionIds[0] : '');
  }
};

const handleReceivePollDelete = (pollId: string) => {
  console.log('SignalR: ReceivePollDelete', pollId);
  polls.value = polls.value.filter(p => p.pollId !== pollId);
  delete selectedOptions[pollId];
  delete votingStates[pollId];
};

const handleReceiveVoteUpdate = (updatedPoll: Poll) => {
  console.log('SignalR: ReceiveVoteUpdate', updatedPoll);
  handleReceivePollUpdate(updatedPoll);
};

const setupSignalRListeners = () => {
    signalRService.on("ReceivePollUpdate", handleReceivePollUpdate);
    signalRService.on("ReceivePollDelete", handleReceivePollDelete);
    signalRService.on("ReceiveVoteUpdate", handleReceiveVoteUpdate);
};

const removeSignalRListeners = () => {
    signalRService.off("ReceivePollUpdate", handleReceivePollUpdate);
    signalRService.off("ReceivePollDelete", handleReceivePollDelete);
    signalRService.off("ReceiveVoteUpdate", handleReceiveVoteUpdate);
};

const attemptReconnect = async () => {
    if (signalRService.getConnectionState() === 'Disconnected' || signalRService.getConnectionState() === null) {
        reconnectingSignalR.value = true;
        signalRError.value = null;
        try {
            await signalRService.startConnection();
        } catch (err: any) {
            signalRError.value = err.message || "Nepodařilo se znovu připojit k real-time službě.";
        } finally {
            reconnectingSignalR.value = false;
        }
    }
};

const fetchPolls = async () => {
  loading.value = true;
  apiError.value = null;
  try {
    const data = await getAllPolls();
    polls.value = data.map(poll => ({
        ...poll,
        isClosed: poll.isClosed || new Date(poll.endTime) <= new Date(),
        userVotedOptionIds: poll.userVotedOptionIds || []
    }));
    data.forEach(poll => {
        if (poll.userVotedOptionIds && poll.userVotedOptionIds.length > 0) {
            selectedOptions[poll.pollId] = poll.isMultipleChoice ? [...poll.userVotedOptionIds] : poll.userVotedOptionIds[0];
        } else {
            selectedOptions[poll.pollId] = poll.isMultipleChoice ? [] : '';
        }
    });
    updateCurrentTimeAndPollsStatus();
  } catch (err: any) {
    apiError.value = err.message || 'Nepodařilo se načíst ankety.';
  } finally {
    loading.value = false;
  }
};

onMounted(async () => {
  await fetchPolls();
  timeInterval = window.setInterval(updateCurrentTimeAndPollsStatus, 1000); // Změněno na 1 sekundu
  try {
    await signalRService.startConnection();
    setupSignalRListeners();
  } catch (err: any) {
    signalRError.value = err.message || "Nepodařilo se připojit k real-time službě.";
    console.error("SignalR connection error on mount:", err);
  }
});

onBeforeUnmount(() => {
  if (timeInterval) clearInterval(timeInterval);
  removeSignalRListeners();
  signalRService.stopConnection();
});

const isPollEffectivelyClosed = (poll: Poll): boolean => {
    // Používáme přímo poll.isClosed, které je aktualizováno v updateCurrentTimeAndPollsStatus
    // nebo přes SignalR. currentTime.value je pro formatRelativeTime.
    return poll.isClosed || new Date(poll.endTime) <= currentTime.value;
};

const sortedPolls = computed(() => {
  return [...polls.value].sort((a, b) => {
    const aIsEffectivelyClosed = isPollEffectivelyClosed(a);
    const bIsEffectivelyClosed = isPollEffectivelyClosed(b);
    if (aIsEffectivelyClosed && !bIsEffectivelyClosed) return 1;
    if (!aIsEffectivelyClosed && bIsEffectivelyClosed) return -1;
    return new Date(b.endTime).getTime() - new Date(a.endTime).getTime();
  });
});

const formatRelativeTime = (isoDateTime: string, now: Date): string => {
    const date = new Date(isoDateTime);
    const diffSeconds = Math.round((date.getTime() - now.getTime()) / 1000);

    if (diffSeconds < -60) {
        const diffMinutesAbs = Math.abs(Math.round(diffSeconds / 60));
        const diffHoursAbs = Math.abs(Math.round(diffMinutesAbs / 60));
        const diffDaysAbs = Math.abs(Math.round(diffHoursAbs / 24));
        if (diffDaysAbs > 1) return `skončila před ${diffDaysAbs} dny`;
        if (diffDaysAbs === 1) return `skončila včera`;
        if (diffHoursAbs > 1) return `skončila před ${diffHoursAbs} hodinami`;
        if (diffHoursAbs === 1) return `skončila před hodinou`;
        if (diffMinutesAbs > 1) return `skončila před ${diffMinutesAbs} minutami`;
        return "skončila před chvílí";
    }
    if (diffSeconds <= 0) return "právě skončila";

    const rtf = new Intl.RelativeTimeFormat('cs', { numeric: 'auto' });
    const days = Math.floor(diffSeconds / (3600 * 24));
    if (days > 1) return rtf.format(days, 'day');
    if (days === 1) return 'zítra';
    const hours = Math.floor(diffSeconds / 3600);
    if (hours > 0) return rtf.format(hours, 'hour');
    const minutes = Math.floor(diffSeconds / 60);
    if (minutes > 0) return rtf.format(minutes, 'minute');
    return `za ${diffSeconds} s`;
};
const formatFullDateTime = (isoDateTime: string): string => {
    return new Date(isoDateTime).toLocaleString('cs-CZ', { dateStyle: 'medium', timeStyle: 'short' });
};

const canManagePoll = (poll: Poll): boolean => {
  if (!authStore.user) return false;
  return poll.creatorUserId === authStore.user.id ||
         authStore.user.roles.includes(UserRoles.Administrator) ||
         authStore.user.roles.includes(UserRoles.Spravce);
};

const userHasVoted = (pollId: string): boolean => {
    const poll = polls.value.find(p => p.pollId === pollId);
    return !!(poll && poll.userVotedOptionIds && poll.userVotedOptionIds.length > 0);
};

const didUserVoteForOption = (poll: Poll, optionId: string): boolean => {
    return !!(poll.userVotedOptionIds && poll.userVotedOptionIds.includes(optionId));
};

const canSubmitVote = (pollId: string): boolean => {
    const poll = polls.value.find(p => p.pollId === pollId);
    if (!poll || isPollEffectivelyClosed(poll) || userHasVoted(pollId)) return false;
    const selection = selectedOptions[pollId];
    if (Array.isArray(selection)) return selection.length > 0;
    return !!selection;
};

const handleVote = async (pollId: string) => {
  const poll = polls.value.find(p => p.pollId === pollId);
  if (!poll) return;
  const selection = selectedOptions[pollId];
  let optionIdsToSubmit: string[] = [];
  if (poll.isMultipleChoice) {
    if (Array.isArray(selection) && selection.length > 0) optionIdsToSubmit = selection;
  } else {
    if (typeof selection === 'string' && selection) optionIdsToSubmit = [selection];
  }
  if (optionIdsToSubmit.length === 0) {
    Swal.fire({ ...getFuturisticSwalBaseOptions('Chyba'), text: 'Prosím, vyberte alespoň jednu možnost.', icon: 'error' });
    return;
  }
  votingStates[pollId] = true;
  const updatedPollData = await submitVote(pollId, { optionIds: optionIdsToSubmit });
  votingStates[pollId] = false;
  if (updatedPollData) {
     handleReceiveVoteUpdate(updatedPollData);
  }
};

const getPollOptionsWithResults = (poll: Poll): PollOption[] => {
    if (!poll.options) return [];
    return poll.options.map(opt => ({ ...opt, voteCount: opt.voteCount || 0 }))
                           .sort((a, b) => (b.voteCount || 0) - (a.voteCount || 0));
};

const calculatePercentage = (totalVotes: number, optionVotes: number): string => {
    if (totalVotes === 0) return '0.0';
    return ((optionVotes / totalVotes) * 100).toFixed(1);
};

const isWinningOption = (poll: Poll, optionId: string): boolean => {
    if (!isPollEffectivelyClosed(poll) && !userHasVoted(poll.pollId)) return false;
    if (!poll.options || (poll.totalVotes || 0) === 0) return false;
    const optionsWithResults = getPollOptionsWithResults(poll);
    if (!optionsWithResults.length) return false;
    const maxVotes = optionsWithResults[0].voteCount;
    const currentOption = optionsWithResults.find(opt => opt.optionId === optionId);
    return currentOption?.voteCount === maxVotes && (maxVotes || 0) > 0;
};

const getFuturisticSwalBaseOptions = (title: string): SweetAlertOptions => ({
  titleText: title,
  background: '#1A2033', color: '#E0E0E0',
  confirmButtonColor: '#00E0FF', cancelButtonColor: '#FF5252',
  customClass: {
    popup: 'futuristic-swal-popup', title: 'futuristic-swal-title font-oxanium',
    htmlContainer: 'futuristic-swal-html-container font-inter', input: 'futuristic-swal-input',
    confirmButton: 'futuristic-swal-confirm-button futuristic-btn',
    cancelButton: 'futuristic-swal-cancel-button futuristic-btn',
    actions: 'futuristic-swal-actions', validationMessage: 'futuristic-swal-validation-message font-inter',
  },
  buttonsStyling: false, heightAuto: false, allowEnterKey: true,
});

const createIconHtml = (pathData: string, size: number = 18, color: string = 'currentColor', extraStyle: string = ''): string => {
  return `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" role="img" aria-hidden="true" width="${size}" height="${size}" fill="${color}" style="${extraStyle}"><path d="${pathData}"></path></svg>`;
};

const getLocalDateTimeForInput = (date: Date): string => {
    const year = date.getFullYear();
    const month = (date.getMonth() + 1).toString().padStart(2, '0');
    const day = date.getDate().toString().padStart(2, '0');
    const hours = date.getHours().toString().padStart(2, '0');
    const minutes = date.getMinutes().toString().padStart(2, '0');
    return `${year}-${month}-${day}T${hours}:${minutes}`;
};

const openCreatePollModal = () => {
  const iconColor = 'var(--v-theme-primary)';
  const iconStyle = 'vertical-align: middle; margin-right: 8px;';
  let nextOptionId = 0;

  Swal.fire({
    ...getFuturisticSwalBaseOptions('Vytvořit novou anketu'),
    html: `
      <div id="createPollForm" class="swal-form-container">
        <label for="swal-question" class="swal-label">
            ${createIconHtml(mdiHelpCircleOutline, 18, iconColor, iconStyle)}Otázka:
        </label>
        <input id="swal-question" class="swal2-input futuristic-swal-input" placeholder="Např. Jaká je vaše oblíbená hra?">
        <label for="swal-endTime" class="swal-label">
            ${createIconHtml(mdiCalendarClock, 18, iconColor, iconStyle)}Datum a čas ukončení (lokální čas):
        </label>
        <input id="swal-endTime" type="datetime-local" class="swal2-input futuristic-swal-input">
        <label for="swal-imageUrl" class="swal-label">
            ${createIconHtml(mdiImage, 18, iconColor, iconStyle)}URL obrázku (volitelné):
        </label>
        <input id="swal-imageUrl" class="swal2-input futuristic-swal-input" placeholder="https://example.com/image.png">
        <label class="swal-label">
            ${createIconHtml(mdiFormatListBulletedSquare, 18, iconColor, iconStyle)}Možnosti odpovědí (min. 2):
        </label>
        <div id="swal-options-container" class="mb-2">
          <div class="swal-option-item mb-2">
            <input class="swal2-input futuristic-swal-input" placeholder="Možnost 1" data-option-input-id="${nextOptionId++}">
          </div>
          <div class="swal-option-item mb-2">
            <input class="swal2-input futuristic-swal-input" placeholder="Možnost 2" data-option-input-id="${nextOptionId++}">
          </div>
        </div>
        <button id="swal-add-option" type="button" class="futuristic-btn-secondary">Přidat další možnost</button>
        <label class="swal-checkbox-label">
          <input id="swal-isMultipleChoice" type="checkbox" class="swal2-checkbox futuristic-swal-checkbox">
          ${createIconHtml(mdiCheckboxMultipleBlankOutline, 18, iconColor, iconStyle)}Povolit výběr více možností
        </label>
      </div>
    `,
    customClass: {
        popup: 'futuristic-swal-popup large-swal',
        htmlContainer: 'futuristic-swal-html-container font-inter swal-form-container-custom-padding',
        input: 'futuristic-swal-input', actions: 'futuristic-swal-actions',
        confirmButton: 'futuristic-swal-confirm-button futuristic-btn',
        cancelButton: 'futuristic-swal-cancel-button futuristic-btn',
        title: 'futuristic-swal-title font-oxanium',
        validationMessage: 'futuristic-swal-validation-message font-inter',
    },
    confirmButtonText: 'Vytvořit anketu', cancelButtonText: 'Zrušit',
    showCancelButton: true, focusConfirm: false, showLoaderOnConfirm: true,
    didOpen: () => {
      const addOptionButton = document.getElementById('swal-add-option');
      const optionsContainer = document.getElementById('swal-options-container');
      addOptionButton?.addEventListener('click', () => {
        const optionItemDiv = document.createElement('div');
        optionItemDiv.className = 'swal-option-item mb-2';
        const newInput = document.createElement('input');
        newInput.className = 'swal2-input futuristic-swal-input';
        newInput.placeholder = `Možnost ${optionsContainer!.children.length + 1}`;
        newInput.dataset.optionInputId = `${nextOptionId++}`;
        optionItemDiv.appendChild(newInput);
        optionsContainer?.appendChild(optionItemDiv);
        newInput.focus();
      });
      const endTimeInput = document.getElementById('swal-endTime') as HTMLInputElement;
      const now = new Date();
      endTimeInput.min = getLocalDateTimeForInput(now);
      const defaultEndTime = new Date(now.getTime() + 30 * 60000);
      endTimeInput.value = getLocalDateTimeForInput(defaultEndTime);
      (document.getElementById('swal-question') as HTMLInputElement)?.focus();
    },
    preConfirm: () => {
      const question = (document.getElementById('swal-question') as HTMLInputElement).value.trim();
      const endTimeValue = (document.getElementById('swal-endTime') as HTMLInputElement).value;
      const imageUrl = (document.getElementById('swal-imageUrl') as HTMLInputElement).value.trim();
      const isMultipleChoice = (document.getElementById('swal-isMultipleChoice') as HTMLInputElement).checked;
      const optionsInputs = document.querySelectorAll('#swal-options-container input[data-option-input-id]');
      const options = Array.from(optionsInputs).map(input => ({ text: (input as HTMLInputElement).value.trim() })).filter(opt => opt.text !== '');
      let validationMessage = '';
      if (!question) validationMessage += 'Otázka je povinná.<br>';
      else if (question.length < 5) validationMessage += 'Otázka musí mít alespoň 5 znaků.<br>';
      if (!endTimeValue) validationMessage += 'Datum a čas ukončení je povinný.<br>';
      else { const localEndDate = new Date(endTimeValue); if (localEndDate <= new Date()) validationMessage += 'Čas ukončení musí být v budoucnosti.<br>';}
      if (options.length < 2) validationMessage += 'Musíte zadat alespoň dvě možnosti odpovědí.<br>';
      if (imageUrl && !/^https?:\/\/[^\s/$.?#].[^\s]*$/i.test(imageUrl)) validationMessage += 'URL obrázku se zdá být neplatné.<br>';
      if (validationMessage) { Swal.showValidationMessage(validationMessage); return false; }
      const localSelectedDate = new Date(endTimeValue);
      localSelectedDate.setHours(localSelectedDate.getHours() + 2);
      const finalUtcEndTime = localSelectedDate.toISOString();
      return { question, endTime: finalUtcEndTime, imageUrl: imageUrl || undefined, options, isMultipleChoice } as CreatePollDto;
    }
  }).then(async (result) => {
    if (result.isConfirmed && result.value) {
        await createPoll(result.value);
    }
  });
};

const openEditPollModal = (poll: Poll) => {
  const iconColor = 'var(--v-theme-primary)';
  const iconStyle = 'vertical-align: middle; margin-right: 8px;';
  const pollEndTimeLocal = new Date(poll.endTime);
  const formattedPollEndTimeForInput = getLocalDateTimeForInput(pollEndTimeLocal);
  let tempOptions: UpdatePollOptionPayload[] = JSON.parse(JSON.stringify(poll.options.map(o => ({optionId: o.optionId, text: o.text, imageUrl: o.imageUrl || ''}))));
  let nextTempOptionIdCounter = 0;

  const generateOptionsHtml = (currentOptions: UpdatePollOptionPayload[]) => {
    let optionsHtml = '';
    currentOptions.forEach((opt, index) => {
        const tempDomId = opt.optionId || `new-edit-option-${index}-${Date.now()}`;
        optionsHtml += `
        <div class="swal-option-item mb-2" data-option-id="${opt.optionId || ''}" data-temp-id="${tempDomId}">
          <input class="swal2-input futuristic-swal-input swal-option-text-input" value="${opt.text}" placeholder="Text možnosti ${index + 1}" ${poll.totalVotes && poll.totalVotes > 0 ? 'disabled' : ''}>
          <input class="swal2-input futuristic-swal-input mt-1 swal-option-image-input" value="${opt.imageUrl || ''}" placeholder="URL obrázku (volitelné)" ${poll.totalVotes && poll.totalVotes > 0 ? 'disabled' : ''}>
          ${ !(poll.totalVotes && poll.totalVotes > 0) ?
            `<button type="button" class="swal-remove-option-btn futuristic-btn-icon error" data-remove-temp-id="${tempDomId}" title="Smazat možnost">
              ${createIconHtml(mdiTrashCanOutline, 16, 'var(--v-theme-error)')}
            </button>` : ''
          }
        </div>`;
    });
    return optionsHtml;
  };

  Swal.fire({
    ...getFuturisticSwalBaseOptions(`Upravit anketu`),
    html: `
      <div id="editPollForm" class="swal-form-container">
        <label for="swal-edit-question" class="swal-label">
            ${createIconHtml(mdiHelpCircleOutline, 18, iconColor, iconStyle)}Otázka:
        </label>
        <input id="swal-edit-question" class="swal2-input futuristic-swal-input" value="${poll.question}">
        <label for="swal-edit-endTime" class="swal-label">
            ${createIconHtml(mdiCalendarClock, 18, iconColor, iconStyle)}Datum a čas ukončení (lokální čas):
        </label>
        <input id="swal-edit-endTime" type="datetime-local" class="swal2-input futuristic-swal-input" value="${formattedPollEndTimeForInput}">
        <label for="swal-edit-imageUrl" class="swal-label">
            ${createIconHtml(mdiImage, 18, iconColor, iconStyle)}URL obrázku ankety (volitelné):
        </label>
        <input id="swal-edit-imageUrl" class="swal2-input futuristic-swal-input" value="${poll.imageUrl || ''}">
        <label class="swal-label">
            ${createIconHtml(mdiFormatListBulletedSquare, 18, iconColor, iconStyle)}Možnosti odpovědí:
        </label>
        <div id="swal-edit-options-container" class="mb-2">
            ${generateOptionsHtml(tempOptions)}
        </div>
        ${ !(poll.totalVotes && poll.totalVotes > 0) ?
            `<button id="swal-edit-add-option" type="button" class="futuristic-btn-secondary">Přidat další možnost</button>` : ''
        }
        ${poll.totalVotes && poll.totalVotes > 0 ? '<p class="font-inter text-warning text-caption mt-2">Upozornění: Anketa již má hlasy. Změna možností není povolena.</p>' : ''}
      </div>
    `,
    customClass: {
        popup: 'futuristic-swal-popup large-swal edit-poll-swal',
        htmlContainer: 'futuristic-swal-html-container font-inter swal-form-container-custom-padding',
        input: 'futuristic-swal-input', actions: 'futuristic-swal-actions',
        confirmButton: 'futuristic-swal-confirm-button futuristic-btn',
        cancelButton: 'futuristic-swal-cancel-button futuristic-btn',
        title: 'futuristic-swal-title font-oxanium',
        validationMessage: 'futuristic-swal-validation-message font-inter',
    },
    confirmButtonText: 'Uložit změny', cancelButtonText: 'Zrušit',
    showCancelButton: true, focusConfirm: false, showLoaderOnConfirm: true,
    didOpen: (modalElement) => {
        const optionsContainer = modalElement.querySelector('#swal-edit-options-container');
        const addOptionButton = modalElement.querySelector('#swal-edit-add-option');

        const rebindRemoveListeners = () => {
            modalElement.querySelectorAll('.swal-remove-option-btn').forEach(btn => {
                const oldBtn = btn;
                const newBtn = oldBtn.cloneNode(true);
                oldBtn.parentNode?.replaceChild(newBtn, oldBtn);

                newBtn.addEventListener('click', (e) => {
                    const targetButton = e.currentTarget as HTMLButtonElement;
                    const tempIdToRemove = targetButton.dataset.removeTempId;
                    const indexToRemove = tempOptions.findIndex(opt => (opt.optionId || `new-edit-option-${tempOptions.indexOf(opt)}-${Date.now()}`) === tempIdToRemove);
                    if (indexToRemove !== -1) {
                        tempOptions.splice(indexToRemove, 1);
                    }
                    if (optionsContainer) optionsContainer.innerHTML = generateOptionsHtml(tempOptions);
                    rebindRemoveListeners();
                });
            });
        };

        if (addOptionButton) {
            addOptionButton.addEventListener('click', () => {
                tempOptions.push({ optionId: undefined, text: '', imageUrl: '' });
                if (optionsContainer) optionsContainer.innerHTML = generateOptionsHtml(tempOptions);
                rebindRemoveListeners();
                const newInputs = optionsContainer?.querySelectorAll('.swal-option-text-input');
                if (newInputs && newInputs.length > 0) (newInputs[newInputs.length -1] as HTMLElement).focus();
            });
        }
        rebindRemoveListeners();

        const endTimeInput = modalElement.querySelector('#swal-edit-endTime') as HTMLInputElement;
        const now = new Date();
        endTimeInput.min = getLocalDateTimeForInput(now);
        (modalElement.querySelector('#swal-edit-question') as HTMLInputElement)?.focus();
    },
    preConfirm: () => {
      const question = (document.getElementById('swal-edit-question') as HTMLInputElement).value.trim();
      const endTimeValue = (document.getElementById('swal-edit-endTime') as HTMLInputElement).value;
      const imageUrl = (document.getElementById('swal-edit-imageUrl') as HTMLInputElement).value.trim();
      const finalOptions: UpdatePollOptionPayload[] = [];

      if (!(poll.totalVotes && poll.totalVotes > 0)) {
        document.querySelectorAll('#swal-edit-options-container .swal-option-item').forEach(itemDiv => {
            const textInput = itemDiv.querySelector('.swal-option-text-input') as HTMLInputElement;
            const imageInput = itemDiv.querySelector('.swal-option-image-input') as HTMLInputElement;
            const optionIdAttr = (itemDiv as HTMLElement).dataset.optionId;
            const optionId = optionIdAttr === '' ? undefined : optionIdAttr;

            if (textInput && textInput.value.trim() !== '') {
                finalOptions.push({
                    optionId: optionId,
                    text: textInput.value.trim(),
                    imageUrl: imageInput ? imageInput.value.trim() || undefined : undefined
                });
            }
        });
      } else {
          poll.options.forEach(opt => finalOptions.push({optionId: opt.optionId, text: opt.text, imageUrl: opt.imageUrl}));
      }

      let validationMessage = '';
      if (!question) validationMessage += 'Otázka je povinná.<br>';
      else if (question.length < 5) validationMessage += 'Otázka musí mít alespoň 5 znaků.<br>';
      if (!endTimeValue) validationMessage += 'Datum a čas ukončení je povinný.<br>';
      else {
          const localEndDate = new Date(endTimeValue);
          const originalPollEndTime = new Date(poll.endTime);
          if (originalPollEndTime > new Date() && localEndDate <= new Date()) {
            validationMessage += 'Nový čas ukončení musí být v budoucnosti, pokud anketa ještě neskončila.<br>';
          }
      }
      if (finalOptions.length < 2) validationMessage += 'Musíte zadat alespoň dvě možnosti odpovědí.<br>';
      if (imageUrl && !/^https?:\/\/[^\s/$.?#].[^\s]*$/i.test(imageUrl)) validationMessage += 'URL obrázku se zdá být neplatné.<br>';

      if (validationMessage) { Swal.showValidationMessage(validationMessage); return false; }

      const localSelectedDate = new Date(endTimeValue);
      localSelectedDate.setHours(localSelectedDate.getHours() + 2);
      const finalUtcEndTime = localSelectedDate.toISOString();

      return {
          question,
          endTime: finalUtcEndTime,
          imageUrl: imageUrl || undefined,
          options: finalOptions
      } as UpdatePollDto;
    }
  }).then(async (result) => {
    if (result.isConfirmed && result.value) {
      await updatePoll(poll.pollId, result.value);
    }
  });
};

const confirmDeletePoll = (pollId: string) => {
  Swal.fire({
    ...getFuturisticSwalBaseOptions('Opravdu smazat anketu?'),
    text: "Tato akce je nevratná!", icon: 'warning',
    showCancelButton: true, confirmButtonText: 'Ano, smazat', cancelButtonText: 'Zrušit',
  }).then(async (result) => {
    if (result.isConfirmed) {
        await deletePoll(pollId);
    }
  });
};

</script>

<style scoped>
.polls-view-container {
  max-width: 960px;
}
.page-title {
  color: var(--v-theme-primary);
}
.poll-card {
  display: flex;
  flex-direction: column;
  height: 100%;
  position: relative;
  transition: transform 0.2s ease-in-out, box-shadow 0.2s ease-in-out;
  background-color: var(--v-theme-surface);
  border: 1px solid rgba(var(--v-theme-primary-rgb), 0.2);
}
.poll-card:hover {
    transform: translateY(-5px);
    box-shadow: 0 8px 25px rgba(var(--v-theme-primary-rgb), 0.2);
}
.poll-image {
  border-bottom: 1px solid rgba(var(--v-theme-primary-rgb), 0.2);
}
.status-chip {
    position: absolute;
    top: 12px;
    right: 12px;
    font-size: 0.75rem !important;
    font-weight: 500;
}
.poll-question {
  font-size: 1.3rem !important;
  line-height: 1.4 !important;
  margin-bottom: 4px;
  word-break: break-word;
}
.options-group {
  margin-top: 12px;
  display: flex;
  flex-direction: column;
  gap: 0px;
}
.option-item {
  width: 100%;
  margin-bottom: 0px;
}
.option-item :deep(.v-label) {
    opacity: 1 !important;
    color: var(--v-theme-text-primary) !important;
    padding-top: 8px;
    padding-bottom: 8px;
}
.option-item :deep(.v-selection-control__input > .v-icon) {
    color: var(--v-theme-primary);
}

.results-section {
  margin-top: 16px;
}
.results-list {
    background-color: transparent !important;
}
.result-item {
    padding: 10px 4px !important;
    border-bottom: 1px solid rgba(var(--v-theme-text-primary-rgb), 0.08);
}
.result-item:last-child {
    border-bottom: none;
}
.winning-option .v-list-item-title, .winning-option span {
    color: var(--v-theme-primary) !important;
    font-weight: bold;
}
.result-progress {
    transition: width 0.5s ease-in-out;
}
.poll-actions {
    border-top: 1px solid rgba(var(--v-theme-text-primary-rgb), 0.1);
    padding-top: 8px !important;
    margin-top: auto;
}

:deep(.swal-form-container) {
  text-align: left;
  max-height: 70vh;
  overflow-y: auto;
  padding-right: 15px;
  padding-left: 5px;
}
:deep(.swal-form-container .swal-label) {
    display: flex;
    align-items: center;
    color: var(--v-theme-text-secondary);
    margin-bottom: 0.35rem;
    margin-top: 0.8rem;
    font-size: 0.9rem;
}
:deep(.swal-form-container .swal-label > svg) {
    margin-right: 8px;
}

:deep(.swal-form-container .futuristic-swal-input) {
    width: 100%;
    box-sizing: border-box;
}
:deep(.swal-checkbox-label) {
  display: flex;
  align-items: center;
  cursor: pointer;
  padding: 0.5rem 0.2rem;
  border-radius: 4px;
  transition: background-color 0.2s ease-in-out;
}
:deep(.swal-checkbox-label:hover) {
  background-color: rgba(var(--v-theme-primary-rgb), 0.05);
}
:deep(.swal-checkbox-label input[type="checkbox"]) {
  margin-right: 0.75rem;
  transform: scale(1.1);
  accent-color: var(--v-theme-primary);
}
:deep(.swal-checkbox-label > svg) {
    margin-right: 8px;
}

:deep(.futuristic-btn-secondary) {
    background-color: var(--v-theme-secondary) !important;
    color: white !important;
    padding: 0.6em 1.2em;
    border-radius: 6px;
    border: none;
    cursor: pointer;
    font-family: 'Exo 2', sans-serif;
    font-size: 0.9rem;
    text-transform: none;
    letter-spacing: 0.3px;
    transition: background-color 0.2s ease-in-out;
    margin-top: 0.5rem;
}
:deep(.futuristic-btn-secondary:hover) {
    background-color: var(--v-theme-secondary-darken-1) !important;
}
:deep(.large-swal) {
    width: 650px !important;
}
:deep(.swal-form-container-custom-padding) {
    padding: 0 1.5em 1.25em 1.5em !important;
}
:deep(.swal-option-item) {
    display: flex;
    align-items: center;
    gap: 8px;
}
:deep(.swal-option-item .swal-option-text-input) {
    flex-grow: 1;
}
:deep(.swal-option-item .swal-option-image-input) {
    flex-grow: 1;
}
:deep(.swal-remove-option-btn) {
    background-color: transparent !important;
    border: none;
    padding: 4px;
    cursor: pointer;
    line-height: 1;
    color: var(--v-theme-error); /* Barva ikony koše */
}
:deep(.swal-remove-option-btn svg) {
    display: block;
}
:deep(.edit-poll-swal .swal-label) {
    margin-top: 1rem;
}
:deep(.edit-poll-swal #swal-edit-options-container + .futuristic-btn-secondary) {
    margin-bottom: 1rem;
}

</style>