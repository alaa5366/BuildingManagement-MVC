
// ============================================================
// Google Sign-In — Super Admin
// ============================================================

let firebaseConfig = null;

export function initGoogleAuth(config) {
    firebaseConfig = config;
}

// ============================================================
// Load Firebase SDK
// ============================================================
async function loadFirebaseSDK() {
    if (window.firebase) return window.firebase;

    return new Promise((resolve, reject) => {
        const script1 = document.createElement('script');
        script1.src = 'https://www.gstatic.com/firebasejs/10.7.0/firebase-app-compat.js';
        script1.onload = () => {
            const script2 = document.createElement('script');
            script2.src = 'https://www.gstatic.com/firebasejs/10.7.0/firebase-auth-compat.js';
            script2.onload = () => resolve(window.firebase);
            script2.onerror = reject;
            document.head.appendChild(script2);
        };
        script1.onerror = reject;
        document.head.appendChild(script1);
    });
}

// ============================================================
// Sign In with Google
// ============================================================
export async function signInWithGoogle() {
    try {
        const firebase = await loadFirebaseSDK();

        if (!firebase.apps.length) {
            firebase.initializeApp(firebaseConfig);
        }

        const provider = new firebase.auth.GoogleAuthProvider();
        provider.setCustomParameters({ prompt: 'select_account' });

        const result = await firebase.auth().signInWithPopup(provider);
        const user = result.user;

        // 2. احصل على ID Token
        const idToken = await user.getIdToken();

        return {
            success: true,
            email: user.email,
            displayName: user.displayName,
            photoURL: user.photoURL,
            uid: user.uid,
            idToken: idToken
        };
    } catch (error) {
        console.error('Google Sign-In failed:', error);
        return {
            success: false,
            error: error.code || error.message
        };
    }
}