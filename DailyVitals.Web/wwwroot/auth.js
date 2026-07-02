window.dailyVitalsAuth = {
    async signIn(ticket) {
        const response = await fetch('/auth/session', {
            method: 'POST',
            credentials: 'same-origin',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ ticket })
        });
        return response.ok;
    },

    async signOut() {
        const response = await fetch('/auth/signout', {
            method: 'POST',
            credentials: 'same-origin'
        });
        return response.ok;
    }
};
