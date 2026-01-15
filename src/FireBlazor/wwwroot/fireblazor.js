// FireBlazor JS Interop Bridge
// Firebase SDK 12.7.0

let firebaseApp = null;
let firebaseAuth = null;
let firebaseFirestore = null;
let firebaseStorage = null;
let firebaseDatabase = null;

// Transform C# FieldValue sentinels to Firebase FieldValue calls
async function transformFieldValues(data) {
    if (data === null || data === undefined) return data;
    if (typeof data !== 'object') return data;
    if (Array.isArray(data)) {
        return Promise.all(data.map(item => transformFieldValues(item)));
    }

    // Check if this is a FieldValue sentinel
    if (data.__fieldValue__) {
        const { serverTimestamp, increment, arrayUnion, arrayRemove, deleteField } =
            await import('https://www.gstatic.com/firebasejs/12.7.0/firebase-firestore.js');

        switch (data.__fieldValue__) {
            case 'serverTimestamp':
                return serverTimestamp();
            case 'increment':
                if (typeof data.value !== 'number') {
                    throw new Error('increment requires a numeric value');
                }
                return increment(data.value);
            case 'arrayUnion':
                if (!Array.isArray(data.elements)) {
                    throw new Error('arrayUnion requires an elements array');
                }
                return arrayUnion(...data.elements);
            case 'arrayRemove':
                if (!Array.isArray(data.elements)) {
                    throw new Error('arrayRemove requires an elements array');
                }
                return arrayRemove(...data.elements);
            case 'delete':
                return deleteField();
            default:
                throw new Error(`Unknown FieldValue type: ${data.__fieldValue__}`);
        }
    }

    // Recursively transform nested objects
    const result = {};
    for (const [key, value] of Object.entries(data)) {
        result[key] = await transformFieldValues(value);
    }
    return result;
}

// Helper to parse and validate emulator host string
function parseEmulatorHost(hostPort) {
    if (!hostPort) return null;
    const parts = hostPort.split(':');
    if (parts.length !== 2) {
        console.error(`[FireBlazor] Invalid emulator host format: "${hostPort}". Expected "host:port".`);
        return null;
    }
    const host = parts[0];
    const port = parseInt(parts[1], 10);
    if (!host || isNaN(port)) {
        console.error(`[FireBlazor] Invalid emulator host format: "${hostPort}". Expected "host:port".`);
        return null;
    }
    return { host, port };
}

export function initialize(config) {
    return new Promise(async (resolve, reject) => {
        try {
            const { initializeApp } = await import('https://www.gstatic.com/firebasejs/12.7.0/firebase-app.js');
            firebaseApp = initializeApp(config);
            resolve(true);
        } catch (error) {
            reject(error.message);
        }
    });
}

// ============ AUTH ============

export async function initializeAuth(emulatorHost) {
    const { getAuth, connectAuthEmulator } = await import('https://www.gstatic.com/firebasejs/12.7.0/firebase-auth.js');
    firebaseAuth = getAuth(firebaseApp);

    if (emulatorHost) {
        const parsed = parseEmulatorHost(emulatorHost);
        if (parsed) {
            try {
                connectAuthEmulator(firebaseAuth, `http://${parsed.host}:${parsed.port}`, { disableWarnings: true });
                console.log(`[FireBlazor] Connected to Auth emulator at ${emulatorHost}`);
            } catch (error) {
                console.error(`[FireBlazor] Failed to connect to Auth emulator: ${error.message}`);
                throw error;
            }
        }
    }

    return true;
}

export async function subscribeToAuthState(dotnetHelper) {
    const { onAuthStateChanged } = await import('https://www.gstatic.com/firebasejs/12.7.0/firebase-auth.js');
    const unsubscribe = onAuthStateChanged(firebaseAuth, (user) => {
        const userData = user ? mapUser(user) : null;
        dotnetHelper.invokeMethodAsync('OnAuthStateChanged', userData);
    });
    return unsubscribe;
}

export async function signInWithEmail(email, password) {
    const { signInWithEmailAndPassword } = await import('https://www.gstatic.com/firebasejs/12.7.0/firebase-auth.js');
    try {
        const result = await signInWithEmailAndPassword(firebaseAuth, email, password);
        return { success: true, data: mapUser(result.user) };
    } catch (error) {
        return { success: false, error: { code: error.code, message: error.message } };
    }
}

export async function createUserWithEmail(email, password) {
    const { createUserWithEmailAndPassword } = await import('https://www.gstatic.com/firebasejs/12.7.0/firebase-auth.js');
    try {
        const result = await createUserWithEmailAndPassword(firebaseAuth, email, password);
        return { success: true, data: mapUser(result.user) };
    } catch (error) {
        return { success: false, error: { code: error.code, message: error.message } };
    }
}

export async function signInWithGoogle() {
    const { signInWithPopup, GoogleAuthProvider } = await import('https://www.gstatic.com/firebasejs/12.7.0/firebase-auth.js');
    try {
        const provider = new GoogleAuthProvider();
        const result = await signInWithPopup(firebaseAuth, provider);
        return { success: true, data: mapUser(result.user) };
    } catch (error) {
        return { success: false, error: { code: error.code, message: error.message } };
    }
}

export async function signInWithGitHub() {
    const { signInWithPopup, GithubAuthProvider } = await import('https://www.gstatic.com/firebasejs/12.7.0/firebase-auth.js');
    try {
        const provider = new GithubAuthProvider();
        const result = await signInWithPopup(firebaseAuth, provider);
        return { success: true, data: mapUser(result.user) };
    } catch (error) {
        return { success: false, error: { code: error.code, message: error.message } };
    }
}

export async function signInWithMicrosoft() {
    const { signInWithPopup, OAuthProvider } = await import('https://www.gstatic.com/firebasejs/12.7.0/firebase-auth.js');
    try {
        const provider = new OAuthProvider('microsoft.com');
        const result = await signInWithPopup(firebaseAuth, provider);
        return { success: true, data: mapUser(result.user) };
    } catch (error) {
        return { success: false, error: { code: error.code, message: error.message } };
    }
}

export async function signOut() {
    const { signOut: fbSignOut } = await import('https://www.gstatic.com/firebasejs/12.7.0/firebase-auth.js');
    try {
        await fbSignOut(firebaseAuth);
        return { success: true };
    } catch (error) {
        return { success: false, error: { code: error.code, message: error.message } };
    }
}

export async function getIdToken(forceRefresh = false) {
    if (!firebaseAuth?.currentUser) {
        return { success: false, error: { code: 'auth/no-user', message: 'No user is currently signed in' } };
    }
    try {
        const token = await firebaseAuth.currentUser.getIdToken(forceRefresh);
        return { success: true, data: token };
    } catch (error) {
        return { success: false, error: { code: error.code || 'auth/token-error', message: error.message } };
    }
}

export async function sendPasswordResetEmail(email) {
    const { sendPasswordResetEmail: fbSendReset } = await import('https://www.gstatic.com/firebasejs/12.7.0/firebase-auth.js');
    try {
        await fbSendReset(firebaseAuth, email);
        return { success: true };
    } catch (error) {
        return { success: false, error: { code: error.code, message: error.message } };
    }
}

export function getCurrentUser() {
    if (!firebaseAuth || !firebaseAuth.currentUser) return null;
    return mapUser(firebaseAuth.currentUser);
}

function mapUser(user) {
    return {
        uid: user.uid,
        email: user.email,
        displayName: user.displayName,
        photoUrl: user.photoURL,
        isEmailVerified: user.emailVerified,
        isAnonymous: user.isAnonymous,
        providers: user.providerData.map(p => p.providerId),
        createdAt: user.metadata.creationTime,
        lastSignInAt: user.metadata.lastSignInTime
    };
}

// ============ FIRESTORE ============

export async function initializeFirestore(options) {
    const { getFirestore, enableIndexedDbPersistence, connectFirestoreEmulator } = await import('https://www.gstatic.com/firebasejs/12.7.0/firebase-firestore.js');
    firebaseFirestore = getFirestore(firebaseApp);

    if (options?.emulatorHost) {
        const parsed = parseEmulatorHost(options.emulatorHost);
        if (parsed) {
            try {
                connectFirestoreEmulator(firebaseFirestore, parsed.host, parsed.port);
                console.log(`[FireBlazor] Connected to Firestore emulator at ${options.emulatorHost}`);
            } catch (error) {
                console.error(`[FireBlazor] Failed to connect to Firestore emulator: ${error.message}`);
                throw error;
            }
        }
    }

    if (options?.enableOfflinePersistence && !options?.emulatorHost) {
        try {
            await enableIndexedDbPersistence(firebaseFirestore);
        } catch (err) {
            console.warn('Firestore persistence failed:', err);
        }
    }
    return true;
}

export async function firestoreGet(path) {
    const { collection, getDocs, doc, getDoc } = await import('https://www.gstatic.com/firebasejs/12.7.0/firebase-firestore.js');
    try {
        // Check if path is a document or collection
        const segments = path.split('/');
        if (segments.length % 2 === 0) {
            // Document path
            const docRef = doc(firebaseFirestore, path);
            const snapshot = await getDoc(docRef);
            return {
                success: true,
                data: snapshot.exists() ? {
                    id: snapshot.id,
                    path: snapshot.ref.path,
                    exists: true,
                    data: snapshot.data(),
                    metadata: {
                        isFromCache: snapshot.metadata.fromCache,
                        hasPendingWrites: snapshot.metadata.hasPendingWrites
                    }
                } : { id: snapshot.id, path: snapshot.ref.path, exists: false }
            };
        } else {
            // Collection path
            const colRef = collection(firebaseFirestore, path);
            const snapshot = await getDocs(colRef);
            return {
                success: true,
                data: snapshot.docs.map(d => ({
                    id: d.id,
                    path: d.ref.path,
                    exists: true,
                    data: d.data(),
                    metadata: {
                        isFromCache: d.metadata.fromCache,
                        hasPendingWrites: d.metadata.hasPendingWrites
                    }
                }))
            };
        }
    } catch (error) {
        return { success: false, error: { code: error.code, message: error.message } };
    }
}

export async function firestoreAdd(path, data) {
    const { collection, addDoc } = await import('https://www.gstatic.com/firebasejs/12.7.0/firebase-firestore.js');
    try {
        const transformedData = await transformFieldValues(data);
        const colRef = collection(firebaseFirestore, path);
        const docRef = await addDoc(colRef, transformedData);
        return { success: true, data: { id: docRef.id } };
    } catch (error) {
        return { success: false, error: { code: error.code || 'firestore/unknown', message: error.message } };
    }
}

export async function firestoreSet(path, data, merge = false) {
    const { doc, setDoc } = await import('https://www.gstatic.com/firebasejs/12.7.0/firebase-firestore.js');
    try {
        const transformedData = await transformFieldValues(data);
        const docRef = doc(firebaseFirestore, path);
        await setDoc(docRef, transformedData, { merge });
        return { success: true, data: null };
    } catch (error) {
        return { success: false, error: { code: error.code || 'firestore/unknown', message: error.message } };
    }
}

export async function firestoreUpdate(path, data) {
    const { doc, updateDoc } = await import('https://www.gstatic.com/firebasejs/12.7.0/firebase-firestore.js');
    try {
        const transformedData = await transformFieldValues(data);
        const docRef = doc(firebaseFirestore, path);
        await updateDoc(docRef, transformedData);
        return { success: true, data: null };
    } catch (error) {
        return { success: false, error: { code: error.code || 'firestore/unknown', message: error.message } };
    }
}

export async function firestoreDelete(path) {
    const { doc, deleteDoc } = await import('https://www.gstatic.com/firebasejs/12.7.0/firebase-firestore.js');
    try {
        const docRef = doc(firebaseFirestore, path);
        await deleteDoc(docRef);
        return { success: true };
    } catch (error) {
        return { success: false, error: { code: error.code, message: error.message } };
    }
}

export async function firestoreBatchWrite(operations) {
    if (!operations || !Array.isArray(operations) || operations.length === 0) {
        return { success: false, error: { code: 'firestore/invalid-argument', message: 'Operations array is required and must not be empty' } };
    }

    const { writeBatch, doc } =
        await import('https://www.gstatic.com/firebasejs/12.7.0/firebase-firestore.js');

    try {
        const batch = writeBatch(firebaseFirestore);

        for (const op of operations) {
            if (!op.path || typeof op.path !== 'string') {
                throw new Error('Operation missing valid path');
            }
            if ((op.type === 'set' || op.type === 'update') && !op.data) {
                throw new Error(`${op.type} operation requires data`);
            }

            const docRef = doc(firebaseFirestore, op.path);

            switch (op.type) {
                case 'set': {
                    const transformedData = await transformFieldValues(op.data);
                    batch.set(docRef, transformedData, { merge: op.merge || false });
                    break;
                }
                case 'update': {
                    const transformedData = await transformFieldValues(op.data);
                    batch.update(docRef, transformedData);
                    break;
                }
                case 'delete':
                    batch.delete(docRef);
                    break;
                default:
                    throw new Error(`Unknown batch operation type: ${op.type}`);
            }
        }

        await batch.commit();
        return { success: true, data: null };
    } catch (error) {
        return { success: false, error: { code: error.code || 'firestore/unknown', message: error.message } };
    }
}

export async function firestoreRunTransaction(operations) {
    const { runTransaction, doc } =
        await import('https://www.gstatic.com/firebasejs/12.7.0/firebase-firestore.js');

    try {
        // Validate operations
        if (!operations || !Array.isArray(operations) || operations.length === 0) {
            return { success: false, error: { code: 'firestore/invalid-argument', message: 'Operations array is required and must not be empty' } };
        }

        const result = await runTransaction(firebaseFirestore, async (transaction) => {
            const results = [];

            for (const op of operations) {
                if (!op.path || typeof op.path !== 'string') {
                    throw new Error('Operation missing valid path');
                }

                const docRef = doc(firebaseFirestore, op.path);

                switch (op.type) {
                    case 'get': {
                        const snapshot = await transaction.get(docRef);
                        results.push({
                            type: 'get',
                            path: op.path,
                            exists: snapshot.exists(),
                            data: snapshot.exists() ? snapshot.data() : null,
                            id: snapshot.id
                        });
                        break;
                    }
                    case 'set': {
                        if (!op.data) {
                            throw new Error('set operation requires data');
                        }
                        const setData = await transformFieldValues(op.data);
                        transaction.set(docRef, setData, { merge: op.merge || false });
                        results.push({ type: 'set', path: op.path, success: true });
                        break;
                    }
                    case 'update': {
                        if (!op.data) {
                            throw new Error('update operation requires data');
                        }
                        const updateData = await transformFieldValues(op.data);
                        transaction.update(docRef, updateData);
                        results.push({ type: 'update', path: op.path, success: true });
                        break;
                    }
                    case 'delete': {
                        transaction.delete(docRef);
                        results.push({ type: 'delete', path: op.path, success: true });
                        break;
                    }
                    default:
                        throw new Error(`Unknown transaction operation type: ${op.type}`);
                }
            }

            return results;
        });

        return { success: true, data: result };
    } catch (error) {
        return { success: false, error: { code: error.code || 'firestore/unknown', message: error.message } };
    }
}

// Callback-based transaction that allows C# to process read data and return write operations
export async function firestoreRunTransactionWithCallback(readPaths, dotnetCallback) {
    const { runTransaction, doc } =
        await import('https://www.gstatic.com/firebasejs/12.7.0/firebase-firestore.js');

    try {
        const result = await runTransaction(firebaseFirestore, async (transaction) => {
            // Read all requested documents inside the transaction
            const readResults = {};
            for (const path of readPaths) {
                const docRef = doc(firebaseFirestore, path);
                const snapshot = await transaction.get(docRef);
                readResults[path] = {
                    path: path,
                    id: snapshot.id,
                    exists: snapshot.exists(),
                    data: snapshot.exists() ? snapshot.data() : null
                };
            }

            // Call back to C# with read data to get write operations
            const writeOpsResult = await dotnetCallback.invokeMethodAsync('ProcessTransaction', readResults);

            if (writeOpsResult.error) {
                throw new Error(writeOpsResult.error);
            }

            // Execute write operations
            for (const op of writeOpsResult.operations) {
                const docRef = doc(firebaseFirestore, op.path);
                switch (op.type) {
                    case 'set': {
                        const setData = await transformFieldValues(op.data);
                        transaction.set(docRef, setData, { merge: op.merge || false });
                        break;
                    }
                    case 'update': {
                        const updateData = await transformFieldValues(op.data);
                        transaction.update(docRef, updateData);
                        break;
                    }
                    case 'delete': {
                        transaction.delete(docRef);
                        break;
                    }
                }
            }

            return writeOpsResult.result;
        });

        return { success: true, data: result };
    } catch (error) {
        return { success: false, error: { code: error.code || 'firestore/unknown', message: error.message } };
    }
}

export async function firestoreQuery(path, queryParams) {
    const { collection, query, where, orderBy, limit, startAt, startAfter, endAt, endBefore, getDocs } = await import('https://www.gstatic.com/firebasejs/12.7.0/firebase-firestore.js');
    try {
        let q = collection(firebaseFirestore, path);
        const constraints = [];

        if (queryParams.where) {
            for (const w of queryParams.where) {
                constraints.push(where(w.field, w.op, w.value));
            }
        }
        if (queryParams.orderBy) {
            for (const o of queryParams.orderBy) {
                constraints.push(orderBy(o.field, o.direction || 'asc'));
            }
        }

        // Cursor constraints
        if (queryParams.startAt) {
            constraints.push(startAt(...queryParams.startAt));
        }
        if (queryParams.startAfter) {
            constraints.push(startAfter(...queryParams.startAfter));
        }
        if (queryParams.endAt) {
            constraints.push(endAt(...queryParams.endAt));
        }
        if (queryParams.endBefore) {
            constraints.push(endBefore(...queryParams.endBefore));
        }

        if (queryParams.limit) {
            constraints.push(limit(queryParams.limit));
        }

        q = query(q, ...constraints);
        const snapshot = await getDocs(q);

        return {
            success: true,
            data: snapshot.docs.map(d => ({
                id: d.id,
                path: d.ref.path,
                exists: true,
                data: d.data(),
                metadata: {
                    isFromCache: d.metadata.fromCache,
                    hasPendingWrites: d.metadata.hasPendingWrites
                }
            }))
        };
    } catch (error) {
        return { success: false, error: { code: error.code, message: error.message } };
    }
}

// ============ FIRESTORE AGGREGATE QUERIES ============

export async function firestoreCount(path, queryParams) {
    const { collection, query, where, getCountFromServer } =
        await import('https://www.gstatic.com/firebasejs/12.7.0/firebase-firestore.js');

    try {
        const collRef = collection(firebaseFirestore, ...path.split('/'));
        let q = collRef;

        if (queryParams?.where) {
            const constraints = queryParams.where.map(w => where(w.field, w.op, w.value));
            q = query(collRef, ...constraints);
        }

        const snapshot = await getCountFromServer(q);
        return { success: true, data: snapshot.data().count };
    } catch (error) {
        return { success: false, error: { code: error.code || 'firestore/unknown', message: error.message } };
    }
}

export async function firestoreSum(path, field, queryParams) {
    const { collection, query, where, getAggregateFromServer, sum } =
        await import('https://www.gstatic.com/firebasejs/12.7.0/firebase-firestore.js');

    try {
        const collRef = collection(firebaseFirestore, ...path.split('/'));
        let q = collRef;

        if (queryParams?.where) {
            const constraints = queryParams.where.map(w => where(w.field, w.op, w.value));
            q = query(collRef, ...constraints);
        }

        const snapshot = await getAggregateFromServer(q, { total: sum(field) });
        return { success: true, data: snapshot.data().total };
    } catch (error) {
        return { success: false, error: { code: error.code || 'firestore/unknown', message: error.message } };
    }
}

export async function firestoreAverage(path, field, queryParams) {
    const { collection, query, where, getAggregateFromServer, average } =
        await import('https://www.gstatic.com/firebasejs/12.7.0/firebase-firestore.js');

    try {
        const collRef = collection(firebaseFirestore, ...path.split('/'));
        let q = collRef;

        if (queryParams?.where) {
            const constraints = queryParams.where.map(w => where(w.field, w.op, w.value));
            q = query(collRef, ...constraints);
        }

        const snapshot = await getAggregateFromServer(q, { avg: average(field) });
        return { success: true, data: snapshot.data().avg };
    } catch (error) {
        return { success: false, error: { code: error.code || 'firestore/unknown', message: error.message } };
    }
}

// ============ FIRESTORE REAL-TIME LISTENERS ============

// Map to track active subscriptions for cleanup
const firestoreSubscriptions = new Map();
let subscriptionIdCounter = 0;

export async function firestoreSubscribeDocument(path, dotnetHelper) {
    const { doc, onSnapshot } = await import('https://www.gstatic.com/firebasejs/12.7.0/firebase-firestore.js');
    try {
        const docRef = doc(firebaseFirestore, path);
        const subscriptionId = ++subscriptionIdCounter;

        const unsubscribe = onSnapshot(docRef,
            (snapshot) => {
                const data = snapshot.exists() ? {
                    id: snapshot.id,
                    path: snapshot.ref.path,
                    exists: true,
                    data: snapshot.data(),
                    metadata: {
                        isFromCache: snapshot.metadata.fromCache,
                        hasPendingWrites: snapshot.metadata.hasPendingWrites
                    }
                } : { id: snapshot.id, path: snapshot.ref.path, exists: false };

                dotnetHelper.invokeMethodAsync('OnDocumentSnapshot', data);
            },
            (error) => {
                dotnetHelper.invokeMethodAsync('OnSnapshotError', {
                    code: error.code,
                    message: error.message
                });
            }
        );

        firestoreSubscriptions.set(subscriptionId, unsubscribe);
        return { success: true, data: { subscriptionId } };
    } catch (error) {
        return { success: false, error: { code: error.code, message: error.message } };
    }
}

export async function firestoreSubscribeCollection(path, queryParams, dotnetHelper) {
    const { collection, query, where, orderBy, limit, onSnapshot } = await import('https://www.gstatic.com/firebasejs/12.7.0/firebase-firestore.js');
    try {
        let q = collection(firebaseFirestore, path);

        if (queryParams) {
            const constraints = [];

            if (queryParams.where) {
                for (const w of queryParams.where) {
                    constraints.push(where(w.field, w.op, w.value));
                }
            }
            if (queryParams.orderBy) {
                for (const o of queryParams.orderBy) {
                    constraints.push(orderBy(o.field, o.direction || 'asc'));
                }
            }
            if (queryParams.limit) {
                constraints.push(limit(queryParams.limit));
            }

            if (constraints.length > 0) {
                q = query(q, ...constraints);
            }
        }

        const subscriptionId = ++subscriptionIdCounter;

        const unsubscribe = onSnapshot(q,
            (snapshot) => {
                const docs = snapshot.docs.map(d => ({
                    id: d.id,
                    path: d.ref.path,
                    exists: true,
                    data: d.data(),
                    metadata: {
                        isFromCache: d.metadata.fromCache,
                        hasPendingWrites: d.metadata.hasPendingWrites
                    }
                }));

                dotnetHelper.invokeMethodAsync('OnCollectionSnapshot', docs);
            },
            (error) => {
                dotnetHelper.invokeMethodAsync('OnSnapshotError', {
                    code: error.code,
                    message: error.message
                });
            }
        );

        firestoreSubscriptions.set(subscriptionId, unsubscribe);
        return { success: true, data: { subscriptionId } };
    } catch (error) {
        return { success: false, error: { code: error.code, message: error.message } };
    }
}

export function firestoreUnsubscribe(subscriptionId) {
    const unsubscribe = firestoreSubscriptions.get(subscriptionId);
    if (unsubscribe) {
        unsubscribe();
        firestoreSubscriptions.delete(subscriptionId);
        return { success: true };
    }
    return { success: false, error: { code: 'not-found', message: 'Subscription not found' } };
}

// ============ STORAGE ============

export async function initializeStorage(emulatorHost) {
    const { getStorage, connectStorageEmulator } = await import('https://www.gstatic.com/firebasejs/12.7.0/firebase-storage.js');
    firebaseStorage = getStorage(firebaseApp);

    if (emulatorHost) {
        const parsed = parseEmulatorHost(emulatorHost);
        if (parsed) {
            try {
                connectStorageEmulator(firebaseStorage, parsed.host, parsed.port);
                console.log(`[FireBlazor] Connected to Storage emulator at ${emulatorHost}`);
            } catch (error) {
                console.error(`[FireBlazor] Failed to connect to Storage emulator: ${error.message}`);
                throw error;
            }
        }
    }

    return true;
}

export async function storageGetDownloadUrl(path) {
    const { ref, getDownloadURL } = await import('https://www.gstatic.com/firebasejs/12.7.0/firebase-storage.js');
    try {
        const storageRef = ref(firebaseStorage, path);
        const url = await getDownloadURL(storageRef);
        return { success: true, data: url };
    } catch (error) {
        return { success: false, error: { code: error.code, message: error.message } };
    }
}

export async function storageDelete(path) {
    const { ref, deleteObject } = await import('https://www.gstatic.com/firebasejs/12.7.0/firebase-storage.js');
    try {
        const storageRef = ref(firebaseStorage, path);
        await deleteObject(storageRef);
        return { success: true };
    } catch (error) {
        return { success: false, error: { code: error.code, message: error.message } };
    }
}

export async function storageUpload(path, data, metadata, dotnetHelper) {
    const { ref, uploadBytesResumable, getDownloadURL } = await import('https://www.gstatic.com/firebasejs/12.7.0/firebase-storage.js');
    try {
        const storageRef = ref(firebaseStorage, path);

        // Build metadata object from full metadata if provided
        const uploadMetadata = metadata ? {
            contentType: metadata.contentType,
            cacheControl: metadata.cacheControl,
            contentDisposition: metadata.contentDisposition,
            contentEncoding: metadata.contentEncoding,
            contentLanguage: metadata.contentLanguage,
            customMetadata: metadata.customMetadata
        } : undefined;

        const uploadTask = uploadBytesResumable(storageRef, data, uploadMetadata);

        return new Promise((resolve) => {
            uploadTask.on('state_changed',
                (snapshot) => {
                    if (dotnetHelper) {
                        dotnetHelper.invokeMethodAsync('OnProgress', {
                            bytesTransferred: snapshot.bytesTransferred,
                            totalBytes: snapshot.totalBytes
                        });
                    }
                },
                (error) => {
                    resolve({ success: false, error: { code: error.code, message: error.message } });
                },
                async () => {
                    const url = await getDownloadURL(uploadTask.snapshot.ref);
                    resolve({
                        success: true,
                        data: {
                            downloadUrl: url,
                            fullPath: uploadTask.snapshot.ref.fullPath,
                            bytesTransferred: uploadTask.snapshot.bytesTransferred
                        }
                    });
                }
            );
        });
    } catch (error) {
        return { success: false, error: { code: error.code, message: error.message } };
    }
}

export async function storageGetBytes(path, maxSize) {
    const { ref, getBytes } = await import('https://www.gstatic.com/firebasejs/12.7.0/firebase-storage.js');
    try {
        const storageRef = ref(firebaseStorage, path);
        const bytes = await getBytes(storageRef, maxSize);
        return { success: true, data: Array.from(new Uint8Array(bytes)) };
    } catch (error) {
        return { success: false, error: { code: error.code, message: error.message } };
    }
}

export async function storageGetMetadata(path) {
    const { ref, getMetadata } = await import('https://www.gstatic.com/firebasejs/12.7.0/firebase-storage.js');
    try {
        const storageRef = ref(firebaseStorage, path);
        const metadata = await getMetadata(storageRef);
        return {
            success: true,
            data: {
                contentType: metadata.contentType,
                cacheControl: metadata.cacheControl,
                contentDisposition: metadata.contentDisposition,
                contentEncoding: metadata.contentEncoding,
                contentLanguage: metadata.contentLanguage,
                customMetadata: metadata.customMetadata
            }
        };
    } catch (error) {
        return { success: false, error: { code: error.code, message: error.message } };
    }
}

export async function storageListAll(path) {
    const { ref, listAll } = await import('https://www.gstatic.com/firebasejs/12.7.0/firebase-storage.js');
    try {
        const storageRef = ref(firebaseStorage, path);
        const result = await listAll(storageRef);
        return {
            success: true,
            data: {
                items: result.items.map(item => item.fullPath),
                prefixes: result.prefixes.map(prefix => prefix.fullPath)
            }
        };
    } catch (error) {
        return { success: false, error: { code: error.code, message: error.message } };
    }
}

export async function storageUploadString(path, data, format, metadata, dotnetHelper) {
    const { ref, uploadString, getDownloadURL } = await import('https://www.gstatic.com/firebasejs/12.7.0/firebase-storage.js');
    try {
        const storageRef = ref(firebaseStorage, path);

        // Map format enum to Firebase format string
        const formatMap = {
            0: 'raw',      // StringFormat.Raw
            1: 'base64',   // StringFormat.Base64
            2: 'base64url', // StringFormat.Base64Url
            3: 'data_url'  // StringFormat.DataUrl
        };
        const firebaseFormat = formatMap[format] || 'raw';

        const uploadMetadata = metadata ? {
            contentType: metadata.contentType,
            cacheControl: metadata.cacheControl,
            contentDisposition: metadata.contentDisposition,
            contentEncoding: metadata.contentEncoding,
            contentLanguage: metadata.contentLanguage,
            customMetadata: metadata.customMetadata
        } : undefined;

        const snapshot = await uploadString(storageRef, data, firebaseFormat, uploadMetadata);
        const downloadUrl = await getDownloadURL(snapshot.ref);

        return {
            success: true,
            data: {
                downloadUrl: downloadUrl,
                fullPath: snapshot.ref.fullPath,
                bytesTransferred: snapshot.bytesTransferred
            }
        };
    } catch (error) {
        return { success: false, error: { code: error.code || 'storage/unknown', message: error.message } };
    }
}

export async function storageUpdateMetadata(path, metadata) {
    const { ref, updateMetadata } = await import('https://www.gstatic.com/firebasejs/12.7.0/firebase-storage.js');
    try {
        const storageRef = ref(firebaseStorage, path);

        const newMetadata = {
            contentType: metadata.contentType,
            cacheControl: metadata.cacheControl,
            contentDisposition: metadata.contentDisposition,
            contentEncoding: metadata.contentEncoding,
            contentLanguage: metadata.contentLanguage,
            customMetadata: metadata.customMetadata
        };

        const updated = await updateMetadata(storageRef, newMetadata);

        return {
            success: true,
            data: {
                contentType: updated.contentType,
                cacheControl: updated.cacheControl,
                contentDisposition: updated.contentDisposition,
                contentEncoding: updated.contentEncoding,
                contentLanguage: updated.contentLanguage,
                customMetadata: updated.customMetadata
            }
        };
    } catch (error) {
        return { success: false, error: { code: error.code || 'storage/unknown', message: error.message } };
    }
}

export async function storageList(path, maxResults, pageToken) {
    const { ref, list } = await import('https://www.gstatic.com/firebasejs/12.7.0/firebase-storage.js');
    try {
        const storageRef = ref(firebaseStorage, path);

        const options = { maxResults: maxResults || 1000 };
        if (pageToken) {
            options.pageToken = pageToken;
        }

        const result = await list(storageRef, options);

        return {
            success: true,
            data: {
                items: result.items.map(item => item.fullPath),
                prefixes: result.prefixes.map(prefix => prefix.fullPath),
                nextPageToken: result.nextPageToken || null
            }
        };
    } catch (error) {
        return { success: false, error: { code: error.code || 'storage/unknown', message: error.message } };
    }
}

// ============ REALTIME DATABASE ============

export async function initializeDatabase(options) {
    const { getDatabase, connectDatabaseEmulator } = await import('https://www.gstatic.com/firebasejs/12.7.0/firebase-database.js');

    const url = options?.url;
    firebaseDatabase = url ? getDatabase(firebaseApp, url) : getDatabase(firebaseApp);

    if (options?.emulatorHost) {
        const parsed = parseEmulatorHost(options.emulatorHost);
        if (parsed) {
            try {
                connectDatabaseEmulator(firebaseDatabase, parsed.host, parsed.port);
                console.log(`[FireBlazor] Connected to Realtime Database emulator at ${options.emulatorHost}`);
            } catch (error) {
                console.error(`[FireBlazor] Failed to connect to Realtime Database emulator: ${error.message}`);
                throw error;
            }
        }
    }

    return true;
}

// Transform C# ServerValue sentinels to Firebase ServerValue calls
async function transformDatabaseServerValues(data) {
    if (data === null || data === undefined) return data;
    if (typeof data !== 'object') return data;
    if (Array.isArray(data)) {
        return Promise.all(data.map(item => transformDatabaseServerValues(item)));
    }

    // Check if this is a ServerValue sentinel
    if (data.__serverValue__) {
        const { serverTimestamp, increment } =
            await import('https://www.gstatic.com/firebasejs/12.7.0/firebase-database.js');

        switch (data.__serverValue__) {
            case 'timestamp':
                return serverTimestamp();
            case 'increment':
                if (typeof data.delta !== 'number') {
                    throw new Error('increment requires a numeric delta value');
                }
                return increment(data.delta);
            default:
                throw new Error(`Unknown ServerValue type: ${data.__serverValue__}`);
        }
    }

    // Recursively transform nested objects
    const result = {};
    for (const [key, value] of Object.entries(data)) {
        result[key] = await transformDatabaseServerValues(value);
    }
    return result;
}

export async function databaseGet(path) {
    const { ref, get } = await import('https://www.gstatic.com/firebasejs/12.7.0/firebase-database.js');
    try {
        const dbRef = ref(firebaseDatabase, path);
        const snapshot = await get(dbRef);
        return {
            success: true,
            data: {
                key: snapshot.key,
                exists: snapshot.exists(),
                value: snapshot.val()
            }
        };
    } catch (error) {
        return { success: false, error: { code: error.code || 'database/unknown', message: error.message } };
    }
}

export async function databaseSet(path, value) {
    const { ref, set } = await import('https://www.gstatic.com/firebasejs/12.7.0/firebase-database.js');
    try {
        const dbRef = ref(firebaseDatabase, path);
        const transformedValue = await transformDatabaseServerValues(value);
        await set(dbRef, transformedValue);
        return { success: true };
    } catch (error) {
        return { success: false, error: { code: error.code || 'database/unknown', message: error.message } };
    }
}

export async function databaseUpdate(path, value) {
    const { ref, update } = await import('https://www.gstatic.com/firebasejs/12.7.0/firebase-database.js');
    try {
        const dbRef = ref(firebaseDatabase, path);
        const transformedValue = await transformDatabaseServerValues(value);
        await update(dbRef, transformedValue);
        return { success: true };
    } catch (error) {
        return { success: false, error: { code: error.code || 'database/unknown', message: error.message } };
    }
}

export async function databasePush(path, value) {
    const { ref, push, set } = await import('https://www.gstatic.com/firebasejs/12.7.0/firebase-database.js');
    try {
        const dbRef = ref(firebaseDatabase, path);
        const newRef = push(dbRef);
        const transformedValue = await transformDatabaseServerValues(value);
        await set(newRef, transformedValue);
        return { success: true, data: { key: newRef.key } };
    } catch (error) {
        return { success: false, error: { code: error.code || 'database/unknown', message: error.message } };
    }
}

export async function databaseRemove(path) {
    const { ref, remove } = await import('https://www.gstatic.com/firebasejs/12.7.0/firebase-database.js');
    try {
        const dbRef = ref(firebaseDatabase, path);
        await remove(dbRef);
        return { success: true };
    } catch (error) {
        return { success: false, error: { code: error.code || 'database/unknown', message: error.message } };
    }
}

// Database subscription management
let databaseSubscriptions = new Map();
let databaseSubscriptionCounter = 0;

export async function databaseQuery(path, queryParams) {
    const { ref, get, query, orderByChild, orderByKey, orderByValue, limitToFirst, limitToLast, startAt, endAt, equalTo } = await import('https://www.gstatic.com/firebasejs/12.7.0/firebase-database.js');
    try {
        let dbRef = ref(firebaseDatabase, path);
        const constraints = [];

        if (queryParams) {
            if (queryParams.orderBy === 'child' && queryParams.orderByPath) {
                constraints.push(orderByChild(queryParams.orderByPath));
            } else if (queryParams.orderBy === 'key') {
                constraints.push(orderByKey());
            } else if (queryParams.orderBy === 'value') {
                constraints.push(orderByValue());
            }

            if (queryParams.limitToFirst) {
                constraints.push(limitToFirst(queryParams.limitToFirst));
            }
            if (queryParams.limitToLast) {
                constraints.push(limitToLast(queryParams.limitToLast));
            }
            if (queryParams.startAtValue !== null && queryParams.startAtValue !== undefined) {
                constraints.push(startAt(queryParams.startAtValue, queryParams.startAtKey || undefined));
            }
            if (queryParams.endAtValue !== null && queryParams.endAtValue !== undefined) {
                constraints.push(endAt(queryParams.endAtValue, queryParams.endAtKey || undefined));
            }
            if (queryParams.equalToValue !== null && queryParams.equalToValue !== undefined) {
                constraints.push(equalTo(queryParams.equalToValue, queryParams.equalToKey || undefined));
            }
        }

        const queryRef = constraints.length > 0 ? query(dbRef, ...constraints) : dbRef;
        const snapshot = await get(queryRef);

        return {
            success: true,
            data: {
                key: snapshot.key,
                exists: snapshot.exists(),
                value: snapshot.val()
            }
        };
    } catch (error) {
        return { success: false, error: { code: error.code || 'database/unknown', message: error.message } };
    }
}

export async function databaseSubscribeValue(path, queryParams, dotnetHelper) {
    const { ref, onValue, query, orderByChild, orderByKey, orderByValue, limitToFirst, limitToLast, startAt, endAt, equalTo, off } = await import('https://www.gstatic.com/firebasejs/12.7.0/firebase-database.js');
    try {
        let dbRef = ref(firebaseDatabase, path);
        const constraints = [];

        if (queryParams) {
            if (queryParams.orderBy === 'child' && queryParams.orderByPath) {
                constraints.push(orderByChild(queryParams.orderByPath));
            } else if (queryParams.orderBy === 'key') {
                constraints.push(orderByKey());
            } else if (queryParams.orderBy === 'value') {
                constraints.push(orderByValue());
            }

            if (queryParams.limitToFirst) {
                constraints.push(limitToFirst(queryParams.limitToFirst));
            }
            if (queryParams.limitToLast) {
                constraints.push(limitToLast(queryParams.limitToLast));
            }
            if (queryParams.startAtValue !== null && queryParams.startAtValue !== undefined) {
                constraints.push(startAt(queryParams.startAtValue, queryParams.startAtKey || undefined));
            }
            if (queryParams.endAtValue !== null && queryParams.endAtValue !== undefined) {
                constraints.push(endAt(queryParams.endAtValue, queryParams.endAtKey || undefined));
            }
            if (queryParams.equalToValue !== null && queryParams.equalToValue !== undefined) {
                constraints.push(equalTo(queryParams.equalToValue, queryParams.equalToKey || undefined));
            }
        }

        const queryRef = constraints.length > 0 ? query(dbRef, ...constraints) : dbRef;
        const subscriptionId = ++databaseSubscriptionCounter;

        const unsubscribe = onValue(queryRef,
            (snapshot) => {
                dotnetHelper.invokeMethodAsync('OnDataSnapshot', {
                    key: snapshot.key,
                    exists: snapshot.exists(),
                    value: snapshot.val()
                });
            },
            (error) => {
                dotnetHelper.invokeMethodAsync('OnSnapshotError', {
                    code: error.code || 'database/unknown',
                    message: error.message
                });
            }
        );

        databaseSubscriptions.set(subscriptionId, { unsubscribe, queryRef });
        return { success: true, data: { subscriptionId } };
    } catch (error) {
        return { success: false, error: { code: error.code || 'database/unknown', message: error.message } };
    }
}

export async function databaseSubscribeChild(path, eventType, queryParams, dotnetHelper) {
    const { ref, onChildAdded, onChildChanged, onChildRemoved, onChildMoved, query, orderByChild, orderByKey, orderByValue, limitToFirst, limitToLast, startAt, endAt, equalTo } = await import('https://www.gstatic.com/firebasejs/12.7.0/firebase-database.js');
    try {
        let dbRef = ref(firebaseDatabase, path);
        const constraints = [];

        if (queryParams) {
            if (queryParams.orderBy === 'child' && queryParams.orderByPath) {
                constraints.push(orderByChild(queryParams.orderByPath));
            } else if (queryParams.orderBy === 'key') {
                constraints.push(orderByKey());
            } else if (queryParams.orderBy === 'value') {
                constraints.push(orderByValue());
            }

            if (queryParams.limitToFirst) {
                constraints.push(limitToFirst(queryParams.limitToFirst));
            }
            if (queryParams.limitToLast) {
                constraints.push(limitToLast(queryParams.limitToLast));
            }
            if (queryParams.startAtValue !== null && queryParams.startAtValue !== undefined) {
                constraints.push(startAt(queryParams.startAtValue, queryParams.startAtKey || undefined));
            }
            if (queryParams.endAtValue !== null && queryParams.endAtValue !== undefined) {
                constraints.push(endAt(queryParams.endAtValue, queryParams.endAtKey || undefined));
            }
            if (queryParams.equalToValue !== null && queryParams.equalToValue !== undefined) {
                constraints.push(equalTo(queryParams.equalToValue, queryParams.equalToKey || undefined));
            }
        }

        const queryRef = constraints.length > 0 ? query(dbRef, ...constraints) : dbRef;
        const subscriptionId = ++databaseSubscriptionCounter;

        let listenerFn;
        switch (eventType) {
            case 'child_added':
                listenerFn = onChildAdded;
                break;
            case 'child_changed':
                listenerFn = onChildChanged;
                break;
            case 'child_removed':
                listenerFn = onChildRemoved;
                break;
            case 'child_moved':
                listenerFn = onChildMoved;
                break;
            default:
                throw new Error(`Unknown event type: ${eventType}`);
        }

        const unsubscribe = listenerFn(queryRef,
            (snapshot) => {
                dotnetHelper.invokeMethodAsync('OnDataSnapshot', {
                    key: snapshot.key,
                    exists: snapshot.exists(),
                    value: snapshot.val()
                });
            },
            (error) => {
                dotnetHelper.invokeMethodAsync('OnSnapshotError', {
                    code: error.code || 'database/unknown',
                    message: error.message
                });
            }
        );

        databaseSubscriptions.set(subscriptionId, { unsubscribe, queryRef });
        return { success: true, data: { subscriptionId } };
    } catch (error) {
        return { success: false, error: { code: error.code || 'database/unknown', message: error.message } };
    }
}

export function databaseUnsubscribe(subscriptionId) {
    const subscription = databaseSubscriptions.get(subscriptionId);
    if (subscription) {
        subscription.unsubscribe();
        databaseSubscriptions.delete(subscriptionId);
        return { success: true };
    }
    return { success: false, error: { code: 'database/not-found', message: 'Subscription not found' } };
}

export async function databaseRunTransaction(path, dotnetHelper) {
    const { ref, runTransaction } = await import('https://www.gstatic.com/firebasejs/12.7.0/firebase-database.js');
    try {
        const dbRef = ref(firebaseDatabase, path);

        const result = await runTransaction(dbRef, async (currentData) => {
            // Call back to .NET with current value, get new value
            const response = await dotnetHelper.invokeMethodAsync('OnTransactionUpdate', currentData);

            // null/undefined from .NET means abort
            if (response === null || response === undefined) {
                return undefined; // Abort transaction
            }

            return response;
        });

        return {
            success: true,
            data: {
                committed: result.committed,
                value: result.snapshot.val()
            }
        };
    } catch (error) {
        return { success: false, error: { code: error.code || 'database/unknown', message: error.message } };
    }
}

export async function databaseOnDisconnectSet(path, value) {
    const { ref, onDisconnect } = await import('https://www.gstatic.com/firebasejs/12.7.0/firebase-database.js');
    try {
        const dbRef = ref(firebaseDatabase, path);
        const transformedValue = await transformDatabaseServerValues(value);
        await onDisconnect(dbRef).set(transformedValue);
        return { success: true };
    } catch (error) {
        return { success: false, error: { code: error.code || 'database/unknown', message: error.message } };
    }
}

export async function databaseOnDisconnectRemove(path) {
    const { ref, onDisconnect } = await import('https://www.gstatic.com/firebasejs/12.7.0/firebase-database.js');
    try {
        const dbRef = ref(firebaseDatabase, path);
        await onDisconnect(dbRef).remove();
        return { success: true };
    } catch (error) {
        return { success: false, error: { code: error.code || 'database/unknown', message: error.message } };
    }
}

export async function databaseOnDisconnectUpdate(path, value) {
    const { ref, onDisconnect } = await import('https://www.gstatic.com/firebasejs/12.7.0/firebase-database.js');
    try {
        const dbRef = ref(firebaseDatabase, path);
        const transformedValue = await transformDatabaseServerValues(value);
        await onDisconnect(dbRef).update(transformedValue);
        return { success: true };
    } catch (error) {
        return { success: false, error: { code: error.code || 'database/unknown', message: error.message } };
    }
}

export async function databaseOnDisconnectCancel(path) {
    const { ref, onDisconnect } = await import('https://www.gstatic.com/firebasejs/12.7.0/firebase-database.js');
    try {
        const dbRef = ref(firebaseDatabase, path);
        await onDisconnect(dbRef).cancel();
        return { success: true };
    } catch (error) {
        return { success: false, error: { code: error.code || 'database/unknown', message: error.message } };
    }
}

export async function databaseSubscribeConnectionState(dotnetHelper) {
    const { ref, onValue } = await import('https://www.gstatic.com/firebasejs/12.7.0/firebase-database.js');
    try {
        const connectedRef = ref(firebaseDatabase, '.info/connected');

        const unsubscribe = onValue(connectedRef, (snapshot) => {
            const isConnected = snapshot.val() === true;
            dotnetHelper.invokeMethodAsync('OnConnectionStateChanged', isConnected);
        });

        const subscriptionId = ++databaseSubscriptionCounter;
        databaseSubscriptions.set(subscriptionId, { unsubscribe, queryRef: null });

        return { success: true, data: { subscriptionId } };
    } catch (error) {
        return { success: false, error: { code: error.code || 'database/unknown', message: error.message } };
    }
}

export async function databaseGoOffline() {
    const { goOffline } = await import('https://www.gstatic.com/firebasejs/12.7.0/firebase-database.js');
    try {
        goOffline(firebaseDatabase);
        return { success: true };
    } catch (error) {
        return { success: false, error: { code: error.code || 'database/unknown', message: error.message } };
    }
}

export async function databaseGoOnline() {
    const { goOnline } = await import('https://www.gstatic.com/firebasejs/12.7.0/firebase-database.js');
    try {
        goOnline(firebaseDatabase);
        return { success: true };
    } catch (error) {
        return { success: false, error: { code: error.code || 'database/unknown', message: error.message } };
    }
}

// ============ APP CHECK ============

let firebaseAppCheck = null;

function isLocalDevelopment() {
    const hostname = window.location.hostname;
    return hostname === 'localhost'
        || hostname === '127.0.0.1'
        || hostname === '[::1]'
        || hostname.endsWith('.localhost');
}

export async function initializeAppCheck(options) {
    const { initializeAppCheck, ReCaptchaV3Provider, ReCaptchaEnterpriseProvider, CustomProvider } = await import('https://www.gstatic.com/firebasejs/12.7.0/firebase-app-check.js');
    try {
        const appCheckOptions = {};

        // Handle auto-debug detection
        const shouldEnableDebug = options.debugMode ||
            (options.autoDetectDebugMode && isLocalDevelopment());

        if (shouldEnableDebug) {
            self.FIREBASE_APPCHECK_DEBUG_TOKEN = options.debugToken || true;
            if (isLocalDevelopment()) {
                console.log('[FireBlazor] App Check: Debug mode auto-enabled (localhost detected)');
            }
        }

        if (options.reCaptchaSiteKey) {
            appCheckOptions.provider = new ReCaptchaV3Provider(options.reCaptchaSiteKey);
        } else if (options.reCaptchaEnterpriseSiteKey) {
            appCheckOptions.provider = new ReCaptchaEnterpriseProvider(options.reCaptchaEnterpriseSiteKey);
        } else if (shouldEnableDebug) {
            // Use CustomProvider for debug-only mode (no reCAPTCHA key required)
            // The debug token set above will be used automatically by Firebase
            appCheckOptions.provider = new CustomProvider({
                getToken: () => {
                    return Promise.resolve({
                        token: 'debug-token-placeholder',
                        expireTimeMillis: Date.now() + 3600000
                    });
                }
            });
        }

        appCheckOptions.isTokenAutoRefreshEnabled = options?.isTokenAutoRefreshEnabled ?? true;

        firebaseAppCheck = initializeAppCheck(firebaseApp, appCheckOptions);
        return { success: true };
    } catch (error) {
        return { success: false, error: { code: error.code || 'appCheck/unknown', message: error.message } };
    }
}

export async function appCheckActivate() {
    // AppCheck is activated during initialization in Firebase v9+
    // This method exists for compatibility
    if (firebaseAppCheck) {
        return { success: true };
    }
    return { success: false, error: { code: 'appCheck/not-initialized', message: 'App Check not initialized' } };
}

export async function appCheckGetToken(forceRefresh) {
    const { getToken } = await import('https://www.gstatic.com/firebasejs/12.7.0/firebase-app-check.js');
    try {
        if (!firebaseAppCheck) {
            return { success: false, error: { code: 'appCheck/not-initialized', message: 'App Check not initialized' } };
        }

        const tokenResult = await getToken(firebaseAppCheck, forceRefresh);
        return {
            success: true,
            data: {
                token: tokenResult.token,
                // Use Firebase's expiration time, fallback to 30 min if not available
                expireTimeMillis: tokenResult.expireTimeMillis || (Date.now() + (30 * 60 * 1000))
            }
        };
    } catch (error) {
        return { success: false, error: { code: error.code || 'appCheck/unknown', message: error.message } };
    }
}

// App Check subscription management
let appCheckSubscriptions = new Map();
let appCheckSubscriptionCounter = 0;

export async function appCheckOnTokenChanged(dotnetHelper) {
    const { onTokenChanged } = await import('https://www.gstatic.com/firebasejs/12.7.0/firebase-app-check.js');
    try {
        if (!firebaseAppCheck) {
            return { success: false, error: { code: 'appCheck/not-initialized', message: 'App Check not initialized' } };
        }

        const subscriptionId = ++appCheckSubscriptionCounter;
        const unsubscribe = onTokenChanged(firebaseAppCheck, (tokenResult) => {
            dotnetHelper.invokeMethodAsync('OnTokenChanged', {
                token: tokenResult.token,
                expireTimeMillis: tokenResult.expireTimeMillis || (Date.now() + (30 * 60 * 1000))
            });
        });

        appCheckSubscriptions.set(subscriptionId, unsubscribe);
        return { success: true, data: { subscriptionId } };
    } catch (error) {
        return { success: false, error: { code: error.code || 'appCheck/unknown', message: error.message } };
    }
}

export function appCheckUnsubscribeTokenChanged(subscriptionId) {
    const unsubscribe = appCheckSubscriptions.get(subscriptionId);
    if (unsubscribe) {
        unsubscribe();
        appCheckSubscriptions.delete(subscriptionId);
        return { success: true };
    }
    return { success: false, error: { code: 'appCheck/not-found', message: 'Subscription not found' } };
}

export function appCheckSetTokenAutoRefreshEnabled(enabled) {
    if (firebaseAppCheck) {
        // Note: In Firebase v9+, this is set during initialization
        // and cannot be changed after. This is a no-op for compatibility.
        return { success: true };
    }
    return { success: false, error: { code: 'appCheck/not-initialized', message: 'App Check not initialized' } };
}

// ============ AI LOGIC ============

let firebaseAI = null;
const generativeModels = new Map();

export async function initializeAI(backend) {
    const { getAI, GoogleAIBackend, VertexAIBackend } =
        await import('https://www.gstatic.com/firebasejs/12.7.0/firebase-ai.js');

    const backendInstance = backend === 'vertex'
        ? new VertexAIBackend()
        : new GoogleAIBackend();

    firebaseAI = getAI(firebaseApp, { backend: backendInstance });
    return { success: true, data: null };
}

export async function aiGetGenerativeModel(modelName, config) {
    const { getGenerativeModel, getAI, GoogleAIBackend } =
        await import('https://www.gstatic.com/firebasejs/12.7.0/firebase-ai.js');

    try {
        // Auto-initialize AI if not already done
        if (!firebaseAI) {
            firebaseAI = getAI(firebaseApp, { backend: new GoogleAIBackend() });
        }

        const modelConfig = { model: modelName };
        if (config) {
            if (config.systemInstruction) {
                modelConfig.systemInstruction = config.systemInstruction;
            }
            const genConfig = {};
            if (config.temperature !== null && config.temperature !== undefined) {
                genConfig.temperature = config.temperature;
            }
            if (config.maxOutputTokens !== null && config.maxOutputTokens !== undefined) {
                genConfig.maxOutputTokens = config.maxOutputTokens;
            }
            if (config.topP !== null && config.topP !== undefined) {
                genConfig.topP = config.topP;
            }
            if (config.topK !== null && config.topK !== undefined) {
                genConfig.topK = config.topK;
            }
            if (config.stopSequences && config.stopSequences.length > 0) {
                genConfig.stopSequences = config.stopSequences;
            }
            if (Object.keys(genConfig).length > 0) {
                modelConfig.generationConfig = genConfig;
            }
            if (config.safetySettings && config.safetySettings.length > 0) {
                modelConfig.safetySettings = config.safetySettings.map(s => ({
                    category: mapHarmCategory(s.category),
                    threshold: mapHarmBlockThreshold(s.threshold)
                }));
            }
            if (config.tools) {
                modelConfig.tools = config.tools.map(t => ({
                    functionDeclarations: t.functionDeclarations?.map(fd => ({
                        name: fd.name,
                        description: fd.description,
                        parameters: fd.parameters
                    }))
                }));
            }
            if (config.toolConfig) {
                modelConfig.toolConfig = {
                    functionCallingConfig: {
                        mode: mapFunctionCallingMode(config.toolConfig.mode),
                        allowedFunctionNames: config.toolConfig.allowedFunctionNames
                    }
                };
            }
            if (config.grounding) {
                modelConfig.tools = modelConfig.tools || [];
                if (config.grounding.googleSearchGrounding) {
                    // googleSearch is a simple empty object - no config options
                    modelConfig.tools.push({
                        googleSearch: {}
                    });
                }
            }
        }

        const model = getGenerativeModel(firebaseAI, modelConfig);
        generativeModels.set(modelName, model);
        return { success: true, data: { modelName } };
    } catch (error) {
        return { success: false, error: { code: mapAIErrorCode(error), message: error.message } };
    }
}

export async function aiGenerateContent(modelName, prompt) {
    try {
        const model = generativeModels.get(modelName);
        if (!model) {
            return { success: false, error: { code: 'ai/model-not-found', message: `Model ${modelName} not initialized` } };
        }

        const result = await model.generateContent(prompt);
        const response = result.response;
        const candidate = response.candidates?.[0];

        // Extract function calls if present
        const functionCalls = candidate?.content?.parts
            ?.filter(part => part.functionCall)
            ?.map(part => ({
                name: part.functionCall.name,
                arguments: part.functionCall.args
            })) || null;

        return {
            success: true,
            data: {
                text: response.text(),
                usage: response.usageMetadata ? {
                    promptTokens: response.usageMetadata.promptTokenCount || 0,
                    candidateTokens: response.usageMetadata.candidatesTokenCount || 0,
                    totalTokens: response.usageMetadata.totalTokenCount || 0
                } : null,
                finishReason: mapFinishReason(candidate?.finishReason),
                safetyRatings: candidate?.safetyRatings?.map(r => ({
                    category: mapHarmCategoryFromResponse(r.category),
                    probability: mapHarmProbability(r.probability),
                    blocked: r.blocked || false
                })) || null,
                functionCalls: functionCalls,
                groundingMetadata: candidate?.groundingMetadata ? {
                    searchQueries: candidate.groundingMetadata.webSearchQueries || null,
                    groundingChunks: candidate.groundingMetadata.groundingChunks?.map(c => ({
                        web: c.web ? { uri: c.web.uri, title: c.web.title } : null
                    })) || null,
                    groundingSupports: candidate.groundingMetadata.groundingSupports?.map(s => ({
                        segment: s.segment ? {
                            startIndex: s.segment.startIndex,
                            endIndex: s.segment.endIndex,
                            text: s.segment.text
                        } : null,
                        groundingChunkIndices: s.groundingChunkIndices || null,
                        confidenceScores: s.confidenceScores || null
                    })) || null,
                    searchEntryPoint: candidate.groundingMetadata.searchEntryPoint ? {
                        renderedContent: candidate.groundingMetadata.searchEntryPoint.renderedContent,
                        sdkBlob: candidate.groundingMetadata.searchEntryPoint.sdkBlob
                    } : null
                } : null
            }
        };
    } catch (error) {
        return { success: false, error: { code: mapAIErrorCode(error), message: error.message } };
    }
}

export async function aiGenerateContentStream(modelName, prompt, dotNetRef, callbackMethod) {
    try {
        const model = generativeModels.get(modelName);
        if (!model) {
            await dotNetRef.invokeMethodAsync(callbackMethod, {
                success: false,
                error: { code: 'ai/model-not-found', message: `Model ${modelName} not initialized` }
            });
            return;
        }

        const result = await model.generateContentStream(prompt);

        for await (const chunk of result.stream) {
            const text = chunk.text();
            await dotNetRef.invokeMethodAsync(callbackMethod, {
                success: true,
                data: { text, isFinal: false }
            });
        }

        await dotNetRef.invokeMethodAsync(callbackMethod, {
            success: true,
            data: { text: '', isFinal: true }
        });
    } catch (error) {
        await dotNetRef.invokeMethodAsync(callbackMethod, {
            success: false,
            error: { code: mapAIErrorCode(error), message: error.message }
        });
    }
}

export async function generateContentWithParts(modelName, parts) {
    try {
        const model = generativeModels.get(modelName);
        if (!model) {
            return { success: false, error: { code: 'ai/model-not-found', message: `Model ${modelName} not initialized` } };
        }

        // Transform parts array to Firebase AI format
        const transformedParts = await Promise.all(parts.map(async (part) => {
            switch (part.type) {
                case 'text':
                    return { text: part.text };
                case 'image':
                    // byte[] comes as base64 from C#
                    return {
                        inlineData: {
                            data: part.base64Data,
                            mimeType: part.mimeType
                        }
                    };
                case 'base64Image':
                    return {
                        inlineData: {
                            data: part.base64Data,
                            mimeType: part.mimeType
                        }
                    };
                case 'fileUri':
                    return {
                        fileData: {
                            fileUri: part.uri,
                            mimeType: part.mimeType
                        }
                    };
                default:
                    throw new Error(`Unknown part type: ${part.type}`);
            }
        }));

        const result = await model.generateContent(transformedParts);
        const response = result.response;

        return {
            success: true,
            data: {
                text: response.text(),
                usage: response.usageMetadata ? {
                    promptTokens: response.usageMetadata.promptTokenCount || 0,
                    candidateTokens: response.usageMetadata.candidatesTokenCount || 0,
                    totalTokens: response.usageMetadata.totalTokenCount || 0
                } : null,
                finishReason: mapFinishReason(response.candidates?.[0]?.finishReason)
            }
        };
    } catch (error) {
        return { success: false, error: { code: mapAIErrorCode(error), message: error.message } };
    }
}

export async function generateContentStreamWithParts(modelName, parts, dotNetRef, callbackMethod) {
    try {
        const model = generativeModels.get(modelName);
        if (!model) {
            await dotNetRef.invokeMethodAsync(callbackMethod, {
                success: false,
                error: { code: 'ai/model-not-found', message: `Model ${modelName} not initialized` }
            });
            return;
        }

        // Transform parts array to Firebase AI format (same as generateContentWithParts)
        const transformedParts = await Promise.all(parts.map(async (part) => {
            switch (part.type) {
                case 'text':
                    return { text: part.text };
                case 'image':
                    return { inlineData: { data: part.base64Data, mimeType: part.mimeType } };
                case 'base64Image':
                    return { inlineData: { data: part.base64Data, mimeType: part.mimeType } };
                case 'fileUri':
                    return { fileData: { fileUri: part.uri, mimeType: part.mimeType } };
                default:
                    throw new Error(`Unknown part type: ${part.type}`);
            }
        }));

        const result = await model.generateContentStream(transformedParts);

        for await (const chunk of result.stream) {
            const text = chunk.text();
            await dotNetRef.invokeMethodAsync(callbackMethod, {
                success: true,
                data: { text, isFinal: false }
            });
        }

        // Send final chunk
        await dotNetRef.invokeMethodAsync(callbackMethod, {
            success: true,
            data: { text: '', isFinal: true }
        });
    } catch (error) {
        await dotNetRef.invokeMethodAsync(callbackMethod, {
            success: false,
            error: { code: mapAIErrorCode(error), message: error.message }
        });
    }
}

function mapAIErrorCode(error) {
    const message = error.message?.toLowerCase() || '';
    const code = error.code?.toLowerCase() || '';

    if (code.includes('api-key') || message.includes('api key')) return 'ai/invalid-api-key';
    if (code.includes('quota') || message.includes('quota')) return 'ai/quota-exceeded';
    if (code.includes('not-found') || message.includes('not found')) return 'ai/model-not-found';
    if (code.includes('blocked') || message.includes('blocked') || message.includes('safety')) return 'ai/content-blocked';
    if (code.includes('network') || message.includes('network')) return 'ai/network-error';
    if (code.includes('timeout') || message.includes('timeout')) return 'ai/timeout';
    if (code.includes('unavailable') || message.includes('unavailable')) return 'ai/service-unavailable';
    if (code.includes('invalid') || message.includes('invalid')) return 'ai/invalid-request';
    return 'ai/unknown';
}

function mapFinishReason(reason) {
    if (!reason) return 0;
    switch (reason.toUpperCase()) {
        case 'STOP': return 1;
        case 'MAX_TOKENS': return 2;
        case 'SAFETY': return 3;
        case 'RECITATION': return 4;
        default: return 5;
    }
}

function mapHarmCategory(category) {
    const categories = {
        1: 'HARM_CATEGORY_HATE_SPEECH',
        2: 'HARM_CATEGORY_DANGEROUS_CONTENT',
        3: 'HARM_CATEGORY_HARASSMENT',
        4: 'HARM_CATEGORY_SEXUALLY_EXPLICIT',
        5: 'HARM_CATEGORY_CIVIC_INTEGRITY'
    };
    return categories[category] || 'HARM_CATEGORY_UNSPECIFIED';
}

function mapHarmBlockThreshold(threshold) {
    const thresholds = {
        1: 'BLOCK_LOW_AND_ABOVE',
        2: 'BLOCK_MEDIUM_AND_ABOVE',
        3: 'BLOCK_ONLY_HIGH',
        4: 'BLOCK_NONE',
        5: 'OFF'
    };
    return thresholds[threshold] || 'HARM_BLOCK_THRESHOLD_UNSPECIFIED';
}

function mapHarmCategoryFromResponse(category) {
    const mapping = {
        'HARM_CATEGORY_HATE_SPEECH': 1,
        'HARM_CATEGORY_DANGEROUS_CONTENT': 2,
        'HARM_CATEGORY_HARASSMENT': 3,
        'HARM_CATEGORY_SEXUALLY_EXPLICIT': 4,
        'HARM_CATEGORY_CIVIC_INTEGRITY': 5
    };
    return mapping[category] || 0;
}

function mapHarmProbability(probability) {
    const mapping = {
        'NEGLIGIBLE': 1,
        'LOW': 2,
        'MEDIUM': 3,
        'HIGH': 4
    };
    return mapping[probability] || 0;
}

function mapFunctionCallingMode(mode) {
    const modes = {
        0: 'AUTO',
        1: 'ANY',
        2: 'NONE'
    };
    return modes[mode] || 'AUTO';
}

export async function countTokens(modelName, content) {
    try {
        const model = generativeModels.get(modelName);
        if (!model) {
            return { success: false, error: { code: 'ai/model-not-found', message: `Model ${modelName} not initialized` } };
        }

        // Build content request based on input type
        let countRequest;
        if (typeof content === 'string') {
            countRequest = content;
        } else if (Array.isArray(content)) {
            // Multimodal content parts
            countRequest = content.map(part => {
                if (part.text) return { text: part.text };
                if (part.inlineData) return { inlineData: part.inlineData };
                if (part.fileData) return { fileData: part.fileData };
                return part;
            });
        } else {
            countRequest = content;
        }

        const result = await model.countTokens(countRequest);

        return {
            success: true,
            data: {
                totalTokens: result.totalTokens,
                textTokens: result.textTokens ?? null,
                imageTokens: result.imageTokens ?? null,
                audioTokens: result.audioTokens ?? null,
                videoTokens: result.videoTokens ?? null
            }
        };
    } catch (error) {
        return { success: false, error: { code: mapAIErrorCode(error), message: error.message } };
    }
}

// Chat session management
const chatSessions = new Map();
let chatSessionCounter = 0;

export async function aiStartChat(modelName, historyJson) {
    try {
        let model = generativeModels.get(modelName);
        if (!model) {
            // Auto-initialize the model if not found
            const initResult = await aiGetGenerativeModel(modelName, null);
            if (!initResult.success) {
                return initResult; // Return the initialization error
            }
            model = generativeModels.get(modelName);
        }

        const chatOptions = {};
        if (historyJson) {
            const history = JSON.parse(historyJson);
            if (history && history.length > 0) {
                chatOptions.history = history.map(msg => ({
                    role: msg.Role || msg.role,
                    parts: [{ text: msg.Content || msg.content }]
                }));
            }
        }

        const chat = model.startChat(chatOptions);
        const sessionId = ++chatSessionCounter;
        chatSessions.set(sessionId, chat);

        return { success: true, data: { sessionId } };
    } catch (error) {
        return { success: false, error: { code: mapAIErrorCode(error), message: error.message } };
    }
}

export async function aiSendChatMessage(sessionId, message) {
    try {
        const chat = chatSessions.get(sessionId);
        if (!chat) {
            return { success: false, error: { code: 'ai/session-not-found', message: `Chat session ${sessionId} not found` } };
        }

        const result = await chat.sendMessage(message);
        const response = result.response;

        return {
            success: true,
            data: {
                text: response.text(),
                usage: response.usageMetadata ? {
                    promptTokens: response.usageMetadata.promptTokenCount || 0,
                    candidateTokens: response.usageMetadata.candidatesTokenCount || 0,
                    totalTokens: response.usageMetadata.totalTokenCount || 0
                } : null,
                finishReason: mapFinishReason(response.candidates?.[0]?.finishReason)
            }
        };
    } catch (error) {
        return { success: false, error: { code: mapAIErrorCode(error), message: error.message } };
    }
}

export async function aiSendChatMessageStream(sessionId, message, dotNetRef, callbackMethod) {
    try {
        const chat = chatSessions.get(sessionId);
        if (!chat) {
            await dotNetRef.invokeMethodAsync(callbackMethod, {
                success: false,
                error: { code: 'ai/session-not-found', message: `Chat session ${sessionId} not found` }
            });
            return;
        }

        const result = await chat.sendMessageStream(message);

        for await (const chunk of result.stream) {
            const text = chunk.text();
            await dotNetRef.invokeMethodAsync(callbackMethod, {
                success: true,
                data: { text, isFinal: false }
            });
        }

        await dotNetRef.invokeMethodAsync(callbackMethod, {
            success: true,
            data: { text: '', isFinal: true }
        });
    } catch (error) {
        await dotNetRef.invokeMethodAsync(callbackMethod, {
            success: false,
            error: { code: mapAIErrorCode(error), message: error.message }
        });
    }
}

export async function aiSendChatMessageWithParts(sessionId, parts) {
    try {
        const chat = chatSessions.get(sessionId);
        if (!chat) {
            return { success: false, error: { code: 'ai/session-not-found', message: `Chat session ${sessionId} not found` } };
        }

        // Transform parts array to Firebase AI format
        const transformedParts = parts.map((part) => {
            switch (part.type) {
                case 'text':
                    return { text: part.text };
                case 'image':
                case 'base64Image':
                    return { inlineData: { data: part.base64Data, mimeType: part.mimeType } };
                case 'fileUri':
                    return { fileData: { fileUri: part.uri, mimeType: part.mimeType } };
                default:
                    throw new Error(`Unknown part type: ${part.type}`);
            }
        });

        const result = await chat.sendMessage(transformedParts);
        const response = result.response;

        return {
            success: true,
            data: {
                text: response.text(),
                usage: response.usageMetadata ? {
                    promptTokens: response.usageMetadata.promptTokenCount || 0,
                    candidateTokens: response.usageMetadata.candidatesTokenCount || 0,
                    totalTokens: response.usageMetadata.totalTokenCount || 0
                } : null,
                finishReason: mapFinishReason(response.candidates?.[0]?.finishReason)
            }
        };
    } catch (error) {
        return { success: false, error: { code: mapAIErrorCode(error), message: error.message } };
    }
}

export async function aiSendChatMessageStreamWithParts(sessionId, parts, dotNetRef, callbackMethod) {
    try {
        const chat = chatSessions.get(sessionId);
        if (!chat) {
            await dotNetRef.invokeMethodAsync(callbackMethod, {
                success: false,
                error: { code: 'ai/session-not-found', message: `Chat session ${sessionId} not found` }
            });
            return;
        }

        // Transform parts array to Firebase AI format
        const transformedParts = parts.map((part) => {
            switch (part.type) {
                case 'text':
                    return { text: part.text };
                case 'image':
                case 'base64Image':
                    return { inlineData: { data: part.base64Data, mimeType: part.mimeType } };
                case 'fileUri':
                    return { fileData: { fileUri: part.uri, mimeType: part.mimeType } };
                default:
                    throw new Error(`Unknown part type: ${part.type}`);
            }
        });

        const result = await chat.sendMessageStream(transformedParts);

        for await (const chunk of result.stream) {
            const text = chunk.text();
            await dotNetRef.invokeMethodAsync(callbackMethod, {
                success: true,
                data: { text, isFinal: false }
            });
        }

        await dotNetRef.invokeMethodAsync(callbackMethod, {
            success: true,
            data: { text: '', isFinal: true }
        });
    } catch (error) {
        await dotNetRef.invokeMethodAsync(callbackMethod, {
            success: false,
            error: { code: mapAIErrorCode(error), message: error.message }
        });
    }
}

export function aiDisposeChatSession(sessionId) {
    if (chatSessions.has(sessionId)) {
        chatSessions.delete(sessionId);
        return { success: true };
    }
    return { success: false, error: { code: 'ai/session-not-found', message: `Chat session ${sessionId} not found` } };
}

// ============ AI LOGIC - IMAGE GENERATION ============

const imageModels = new Map();

export async function aiGetImageModel(modelName) {
    try {
        const { getImagenModel, getAI, GoogleAIBackend } =
            await import('https://www.gstatic.com/firebasejs/12.7.0/firebase-ai.js');

        // Auto-initialize AI if not already done
        if (!firebaseAI) {
            firebaseAI = getAI(firebaseApp, { backend: new GoogleAIBackend() });
        }

        const model = getImagenModel(firebaseAI, { model: modelName });
        imageModels.set(modelName, model);

        return { success: true, data: { modelName } };
    } catch (error) {
        return {
            success: false,
            error: { code: mapAIErrorCode(error), message: error.message }
        };
    }
}

export async function aiGenerateImages(modelName, prompt, config) {
    try {
        const model = imageModels.get(modelName);
        if (!model) {
            return {
                success: false,
                error: { code: 'ai/model-not-found', message: `Image model ${modelName} not initialized. Call getImageModel first.` }
            };
        }

        // Build generation config
        const generationConfig = {};
        if (config?.numberOfImages) {
            generationConfig.numberOfImages = config.numberOfImages;
        }
        if (config?.aspectRatio) {
            generationConfig.aspectRatio = config.aspectRatio;
        }
        if (config?.negativePrompt) {
            generationConfig.negativePrompt = config.negativePrompt;
        }

        // Generate images
        const result = await model.generateImages(prompt, generationConfig);

        // Map response to our format
        const images = result.images.map(img => ({
            base64Data: img.bytesBase64Encoded,
            mimeType: img.mimeType || 'image/png'
        }));

        return {
            success: true,
            data: { images }
        };
    } catch (error) {
        return {
            success: false,
            error: { code: mapAIErrorCode(error), message: error.message }
        };
    }
}
