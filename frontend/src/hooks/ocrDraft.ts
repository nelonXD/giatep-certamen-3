function openDrafts(): Promise<IDBDatabase> {
  return new Promise((resolve, reject) => {
    const request = indexedDB.open('giatep-local-drafts', 1);
    request.onupgradeneeded = () => request.result.createObjectStore('ocr');
    request.onsuccess = () => resolve(request.result);
    request.onerror = () => reject(request.error);
  });
}
export async function readOcrDraft<T>(key: string): Promise<T | undefined> {
  const db = await openDrafts();
  try { return await new Promise<T | undefined>((resolve, reject) => {
    const request = db.transaction('ocr').objectStore('ocr').get(key);
    request.onsuccess = () => resolve(request.result); request.onerror = () => reject(request.error);
  }); } finally { db.close(); }
}
export async function writeOcrDraft(key: string, value: unknown | null) {
  const db = await openDrafts();
  try { await new Promise<void>((resolve, reject) => {
    const tx = db.transaction('ocr', 'readwrite');
    if (value === null) tx.objectStore('ocr').delete(key); else tx.objectStore('ocr').put(value, key);
    tx.oncomplete = () => resolve(); tx.onerror = () => reject(tx.error); tx.onabort = () => reject(tx.error);
  }); } finally { db.close(); }
}
