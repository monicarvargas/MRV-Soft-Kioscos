// Atajos de teclado del programa de escritorio (F2 Vender, F3 Productos, F4 Cobrar...).
// Lo registra MenuPrincipal cuando ConAtajos = true (solo el escritorio).
//  - Si en la pantalla hay un botón con data-atajo="F4" (ej: Cobrar), lo aprieta.
//  - Si no, le avisa al menú, que va a la opción que tiene esa tecla (si el usuario la puede usar).
//  - F5 no hace nada: así no se recarga el programa sin querer.
const TECLAS = ["F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9"];
let menu = null;
let registrado = false;

export function registrar(referencia) {
    menu = referencia;
    if (registrado) return;
    registrado = true;
    document.addEventListener("keydown", (e) => {
        if (!TECLAS.includes(e.key) || e.ctrlKey || e.altKey || e.metaKey) return;
        e.preventDefault();
        e.stopPropagation();
        if (e.key === "F5" || e.repeat) return;

        const boton = document.querySelector(`[data-atajo="${e.key}"]`);
        if (boton) {
            if (!boton.disabled && boton.offsetParent !== null) boton.click();
            return;
        }
        if (menu) menu.invokeMethodAsync("Atajo", e.key).catch(() => { });
    }, true);
}

export function quitar() {
    menu = null;
}
