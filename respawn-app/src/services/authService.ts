import Swal from 'sweetalert2';

// Helper function to apply futuristic theme to SweetAlert2
const getFuturisticSwalOptions = (title: string) => {
  return {
    titleText: title,
    background: '#1A2033', // surface color
    color: '#E0E0E0', // text-primary
    confirmButtonColor: '#00E0FF', // primary color
    cancelButtonColor: '#FF5252', // error color
    customClass: {
      popup: 'futuristic-swal-popup',
      title: 'futuristic-swal-title font-oxanium',
      htmlContainer: 'futuristic-swal-html-container font-inter',
      input: 'futuristic-swal-input',
      confirmButton: 'futuristic-swal-confirm-button futuristic-btn',
      cancelButton: 'futuristic-swal-cancel-button futuristic-btn',
      actions: 'futuristic-swal-actions',
    },
    buttonsStyling: false, // Important to use customClass for buttons
  };
};

export const displayLoginModal = async () => {
  const { value: formValues, isConfirmed } = await Swal.fire({
    ...getFuturisticSwalOptions('Přihlášení'),
    html: `
      <div class="swal-form-container">
        <label for="swal-nickname" class="swal-label futuristic-swal-label font-inter">Přezdívka</label>
        <input id="swal-nickname" class="swal2-input futuristic-swal-input" placeholder="TvojePřezdívka">

        <label for="swal-password" class="swal-label futuristic-swal-label font-oxanium">Heslo</label>
        <input id="swal-password" type="password" class="swal2-input futuristic-swal-input" placeholder="••••••••">
      </div>
    `,
    focusConfirm: false,
    showCancelButton: true,
    confirmButtonText: 'Přihlásit se',
    cancelButtonText: 'Zrušit',
    preConfirm: () => {
      const nicknameInput = document.getElementById('swal-nickname') as HTMLInputElement;
      const passwordInput = document.getElementById('swal-password') as HTMLInputElement;
      if (!nicknameInput || !passwordInput) {
        Swal.showValidationMessage('Chyba při načítání formuláře');
        return false;
      }
      const nickname = nicknameInput.value;
      const password = passwordInput.value;
      if (!nickname || !password) {
        Swal.showValidationMessage('Prosím, vyplňte přezdívku i heslo');
        return false;
      }
      return { nickname, password };
    }
  });

  if (isConfirmed && formValues) {
    // Zde bude volání API pro přihlášení
    console.log('Přihlašovací údaje:', formValues);
    Swal.fire({
        ...getFuturisticSwalOptions('Úspěch!'),
        icon: 'success',
        text: `Vítej zpět, ${formValues.nickname}! (Simulace)`,
        timer: 2000,
        showConfirmButton: false,
    });
    // TODO: Zavolat API, zpracovat token, aktualizovat stav uživatele
  }
};

export const displayRegisterModal = async () => {
  const { value: formValues, isConfirmed } = await Swal.fire({
    ...getFuturisticSwalOptions('Registrace'),
    html: `
      <div class="swal-form-container">
        <label for="swal-reg-nickname" class="swal-label futuristic-swal-label font-inter">Přezdívka</label>
        <input id="swal-reg-nickname" class="swal2-input futuristic-swal-input" placeholder="TvojePřezdívka">

        <label for="swal-reg-email" class="swal-label futuristic-swal-label font-inter">Email</label>
        <input id="swal-reg-email" type="email" class="swal2-input futuristic-swal-input" placeholder="email@example.com">

        <label for="swal-reg-password" class="swal-label futuristic-swal-label font-inter">Heslo</label>
        <input id="swal-reg-password" type="password" class="swal2-input futuristic-swal-input" placeholder="••••••••">

        <label for="swal-reg-confirm-password" class="swal-label futuristic-swal-label font-inter">Potvrzení hesla</label>
        <input id="swal-reg-confirm-password" type="password" class="swal2-input futuristic-swal-input" placeholder="••••••••">
      </div>
    `,
    focusConfirm: false,
    showCancelButton: true,
    confirmButtonText: 'Zaregistrovat se',
    cancelButtonText: 'Zrušit',
    preConfirm: () => {
      const nicknameInput = document.getElementById('swal-reg-nickname') as HTMLInputElement;
      const emailInput = document.getElementById('swal-reg-email') as HTMLInputElement;
      const passwordInput = document.getElementById('swal-reg-password') as HTMLInputElement;
      const confirmPasswordInput = document.getElementById('swal-reg-confirm-password') as HTMLInputElement;

      if (!nicknameInput || !emailInput || !passwordInput || !confirmPasswordInput) {
        Swal.showValidationMessage('Chyba při načítání formuláře');
        return false;
      }

      const nickname = nicknameInput.value;
      const email = emailInput.value;
      const password = passwordInput.value;
      const confirmPassword = confirmPasswordInput.value;

      if (!nickname || !email || !password || !confirmPassword) {
        Swal.showValidationMessage('Prosím, vyplňte všechna pole');
        return false;
      }
      if (password !== confirmPassword) {
        Swal.showValidationMessage('Hesla se neshodují');
        // Clear password fields for better UX
        passwordInput.value = '';
        confirmPasswordInput.value = '';
        passwordInput.focus();
        return false;
      }
      // Add more validation if needed (e.g., password strength, email format)
      return { nickname, email, password };
    }
  });

  if (isConfirmed && formValues) {
    // Zde bude volání API pro registraci
    console.log('Registrační údaje:', formValues);
     Swal.fire({
        ...getFuturisticSwalOptions('Registrace úspěšná!'),
        icon: 'success',
        text: `Vítej, ${formValues.nickname}! Byl jsi zaregistrován. (Simulace)`,
        timer: 2500,
        showConfirmButton: false,
    });
    // TODO: Zavolat API, zpracovat odpověď
  }
};
