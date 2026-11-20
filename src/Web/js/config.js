// Dirección de la API.
// Con Live Server (puerto 5500) la API corre en otro puerto: cambia 7016 por el de tu API (el que ves en launchSettings.json).
// Detrás de NGINX la web y la API comparten dirección, por eso queda vacío.
export const API_URL = window.location.port === "5500" ? "https://localhost:7016" : "";
