// Service worker mínimo de MRV Soft: solo sirve para que el navegador deje INSTALAR la app
// (acceso directo con ícono en el escritorio o en el celular).
// No guarda nada en caché: siempre se usa la versión publicada en Vercel.
self.addEventListener("install", () => self.skipWaiting());
self.addEventListener("activate", (e) => e.waitUntil(self.clients.claim()));
self.addEventListener("fetch", () => { /* lo resuelve el navegador como siempre */ });
