namespace BlazorKioscoCompartido.Services
{
    /// <summary>
    /// Textos de ayuda de cada parte del sistema (Configuración → Ayuda y plan, y el manual de usuario).
    /// Clave = la de <see cref="FuncionesPlan"/>; null = está siempre (no depende del plan).
    /// Para agregar una función nueva: sumar una entrada acá con la misma clave.
    /// Los nombres de las listas van como {L1} (precio_venta), {L2} (precio_lista2) y {L3} (precio_lista3):
    /// al mostrarse se cambian por el nombre que les puso cada negocio (ver <see cref="ConNombres"/>).
    /// </summary>
    public static class AyudaSistema
    {
        public sealed record Tema(string? Clave, string Titulo, string Icono, string DondeEsta, string ParaQue, string[] Pasos, string[]? Consejos = null);

        public static readonly IReadOnlyList<Tema> Temas = new List<Tema>
        {
            // ======================= SIEMPRE =======================
            new(null, "Vender y cobrar", "bi-cart-fill", "Menú → Vender",
                "Cargar los productos que lleva el cliente y cobrar con una o varias formas de pago.",
                new[]
                {
                    "Al entrar, el cursor ya está en el buscador: escribí el nombre o pasá el lector de códigos y el producto se suma solo.",
                    "Para cambiar la cantidad usá + / − o escribila. En productos por peso tocá el lápiz y cargá los gramos o el importe.",
                    "Si el producto no está cargado, se ofrece venderlo como \"producto sin código\" (escribís descripción y precio).",
                    "Tocá \"Cobrar\", elegí la forma de pago y el monto (los botones con montos ayudan a calcular el vuelto). Se puede pagar con más de una.",
                    "Si una parte queda fiada, elegí el cliente: el sistema pregunta a qué precio se fía (el de ese momento o el de la lista «{L1}»).",
                    "\"Confirmar venta\": se guarda, baja el stock y te muestra el vuelto. Si está prendido \"Imprimir ticket\", sale el ticket.",
                },
                new[]
                {
                    "El precio que se cobra depende del momento: en el horario configurado puede ir la lista «{L2}» y los domingos o feriados la lista «{L3}» (según Configuración → Listas de precios). En el buscador se ve en color.",
                    "La lista de precios queda fija al empezar la venta: si se pasa de hora mientras cobrás, no cambia.",
                    "Si el producto tiene precio por mayor o precios por cantidad (ej: 10 u x $250), el precio cambia solo.",
                    "Cuando llevando 1 más le sale más barato al cliente, aparece un aviso amarillo para ofrecérselo.",
                }),
            new(null, "Ticket para la impresora de tickets", "bi-receipt", "Vender → Cobrar → \"Imprimir ticket\"",
                "Imprimir el ticket de la venta en la impresora de rollo angosto (58 u 80 mm).",
                new[]
                {
                    "En Cobrar, prendé \"Imprimir ticket\" y elegí el ancho del rollo: 58 mm (el más común) u 80 mm.",
                    "Al confirmar la venta se abre la impresión con el ticket: elegí la impresora de tickets y aceptá.",
                    "La elección queda guardada en ese equipo: no hay que prenderla en cada venta.",
                },
                new[]
                {
                    "La primera vez, en la ventana de impresión sacá \"Encabezados y pies de página\" y poné márgenes \"Ninguno\" para que no salga la dirección de la página.",
                    "Si el ticket sale muy chico o cortado, revisá que el ancho elegido coincida con el rollo.",
                }),
            new(null, "Atajos de teclado (PC)", "bi-keyboard-fill", "En el programa de escritorio",
                "Ir a cada parte con una tecla, sin usar el mouse. La tecla se ve al lado de cada opción del menú.",
                new[]
                {
                    "F2: Vender.",
                    "F3: Productos.",
                    "F4: Cobrar (en Vender, con productos cargados). En Cobrar, F4 confirma la venta cuando ya está todo pagado.",
                    "F6: Nuevo producto.",
                    "F7: Carteles y códigos de barras.",
                    "F8: Recaudación del día · F9: Deudas (fiado) (si el usuario tiene permiso).",
                },
                new[] { "F5 no hace nada: así no se recarga el programa sin querer y no se pierde la venta que estás cargando." }),
            new(null, "Productos", "bi-tag-fill", "Menú → Productos",
                "Ver, buscar y editar todos los productos del negocio.",
                new[]
                {
                    "Buscá por nombre o código. Los botones de arriba filtran: Stock bajo, Sin stock, Combos, Inactivos (y limpian la familia elegida).",
                    "Filtrá por familia y subfamilia, y ordená por nombre, por menos stock o por modificados recientemente.",
                    "El recuadro verde \"SE COBRA\" muestra el precio que se cobra ahora (en rojo los domingos y feriados). Los productos por gramos muestran el precio x 100 g.",
                    "Con el botón + sumás stock sin entrar a editar. Los combos muestran cuántos se pueden armar con el stock de lo que los forma.",
                    "Tocá un producto para editarlo: precios, stock, familia, proveedores, código de barras. Para dejar de venderlo, apagá \"Activo\".",
                }),
            new(null, "Nuevo producto", "bi-plus-square-fill", "Menú → Nuevo producto",
                "Dar de alta un producto con su código, precios y stock.",
                new[]
                {
                    "Escaneá el código de barras (si ya existe, te ofrece abrirlo). Si no tiene, tocá \"No tiene: crear un código\" (empieza con 29).",
                    "Escribí el nombre, elegí la familia y, si querés, la subfamilia (con + se crea una nueva). Si no elegís, queda en ARTICULOS y el proveedor PROVEEDOR.",
                    "Al elegir la familia o subfamilia, la utilidad se pone sola con la del grupo (Configuración → Familias).",
                    "Elegí cómo se vende: por unidad, por kilo (precio por kilo), por gramos (se muestra x 100 g), etc.",
                    "Cargá el costo y la utilidad (el precio se calcula solo) o directamente el precio de venta.",
                    "Si lo comprás por bulto, usá \"Lo compro por bulto\": cargás el precio del bulto y cuántas unidades trae y calcula el costo por unidad. Queda guardado para ver si aumentó la próxima vez.",
                    "Si se vende por peso (fiambre, alimento balanceado), el bulto va en KILOS: ej. bolsa de 15 kg o horma de 4,35 kg (con coma). Calcula el costo por kilo y de ahí salen los precios por kilo o cada 100 g.",
                    "Opcional: precio «{L2}», precio por mayor, precios por cantidad, stock inicial, mínimo e ideal, y proveedores.",
                    "\"Guardar\" o \"Guardar y cargar otra variante\" (para el mismo producto en otro sabor o color).",
                },
                new[]
                {
                    "Redondeo: si el negocio redondea precios, cada producto tiene un interruptor. Apagalo en cosas chicas (gomitas a $30).",
                    "Precio «{L2}» igual al de «{L1}» (gomitas): se respeta y no se le suma el recargo.",
                    "Precios por cantidad: ej. c/u $30 · 10 u x $250 · 23 u x $500. Al vender se arma solo el precio más barato.",
                }),
            new(null, "Configuración", "bi-gear-fill", "Menú → Configuración",
                "Los datos del negocio: clientes, proveedores, familias, listas de precios, formas de pago, usuarios, turnos, unidades, color y redondeo.",
                new[]
                {
                    "Elegí la pestaña de lo que querés cambiar. Para agregar o editar se abre una ventana con el formulario.",
                    "Familias: cada familia puede tener subfamilias (botón \"Subfamilias\") y su propio % de utilidad.",
                    "Usuarios (solo el dueño): crear empleados con su perfil (encargado/a o vendedor/a) y contraseña.",
                    "Ayuda y plan → Perfiles: qué puede hacer cada perfil. El dueño le puede dar o sacar cosas al encargado y al vendedor.",
                    "Copia de seguridad (solo el dueño): baja un Excel con todos los datos del negocio. En la PC se puede hacer sola, todos los días o una vez por semana, en la carpeta de Google Drive.",
                }),
            new(null, "Listas de precios: {L2}, {L3} y por mayor", "bi-currency-dollar", "Configuración → Listas de precios",
                "Cobrar otro precio en cierto horario («{L2}»), los domingos y feriados («{L3}»), o por mayor.",
                new[]
                {
                    "Tildá las listas que usa el negocio: «{L1}» (siempre), «{L2}», «{L3}» y Precio x Mayor. Con el lápiz les cambiás el nombre.",
                    "«{L2}»: elegí el horario (ej: de 00:00 a 08:00) y el recargo por tramos de costo (con \"Agregar tramo\").",
                    "«{L3}»: elegí los días de la semana (ej: domingo) y marcá los feriados en el almanaque. El recargo puede ser un monto fijo o un %. Ese día vale todo el día y gana sobre «{L2}».",
                    "Si a un producto le cargás a mano el precio «{L2}», se respeta. Si no lo tiene (o es menor que el de «{L1}»), se calcula con el recargo.",
                },
                new[] { "Lo fiado: al cobrar se pregunta a qué precio se fía; al cobrar la deuda se puede elegir el precio según el horario en que lo sacó o todo con la lista «{L1}»." }),
            new(null, "Utilidad por familia", "bi-percent", "Configuración → Familias",
                "Que cada rubro tenga su ganancia: ferretería 60 %, fiambres 50 %, golosinas 40 %...",
                new[]
                {
                    "Editá la familia y cargá el \"% de utilidad\". En cada subfamilia también podés poner uno propio (escribís el número y tocás ✓ o Enter).",
                    "Al cambiarla, el sistema pregunta si querés actualizar los productos que ya existen: les cambia la utilidad y recalcula el precio de venta con su costo.",
                    "Los productos nuevos (a mano o importados de Excel) arrancan con la utilidad de su subfamilia; si no tiene, la de la familia; si tampoco, 40 %.",
                    "En Editar producto, si el grupo tiene otra utilidad aparece un botón para aplicarla.",
                }),

            // ======================= SEGÚN EL PLAN =======================
            new(FuncionesPlan.Carteles, "Carteles y códigos de barras", "bi-tags-fill", "Menú → Carteles y códigos de barras",
                "Imprimir carteles de precios para la góndola y etiquetas con código de barras para pegar (potes, frascos, bolsitas).",
                new[]
                {
                    "Solapa \"Carteles de precios\": buscá y tildá productos, o traé de una todos los que cambiaron de precio desde una fecha.",
                    "Elegí el tamaño (grandes o chicos) y si muestra el precio «{L2}».",
                    "Tocá \"Imprimir\", o descargalos en Word (para modificarlos antes) o en PDF.",
                    "Solapa \"Códigos de barras\": elegí los productos y cuántas etiquetas de cada uno. Si no tienen código, se lo crea. También se pueden bajar en Word o PDF.",
                    "Imprimí en hoja A4 (común o autoadhesiva) y probá leer una con el lector antes de pegar todas.",
                },
                new[] { "Con familia y subfamilia + \"Agregar todos\" armás los carteles de toda una góndola en un paso." }),
            new(FuncionesPlan.ImportarExcel, "Importar desde Excel", "bi-file-earmark-excel-fill", "Menú → Importar desde Excel",
                "Cargar o actualizar muchos productos de una vez desde el Excel del mayorista o de otro sistema.",
                new[]
                {
                    "Elegí el tipo: factura de compra, carga de productos o solo precios. Si corresponde, elegí el proveedor y la familia de los productos nuevos (ahí ves qué utilidad se les aplica).",
                    "Subí el archivo. El sistema reconoce solo las columnas (código, descripción, costo, precio, familia, subfamilia...).",
                    "Revisá la vista previa: nuevos, que ya existen, parecidos, errores. Podés cambiar la utilidad o el precio de cada fila (\"volver a la regla\" deshace lo que cambiaste).",
                    "Si un producto viene por bulto, indicá cuántas unidades trae (o cuántos kilos, si se vende por peso): el costo se divide y el producto guarda el precio del bulto.",
                    "Al confirmar, si hay productos nuevos se pregunta qué es el código del Excel: tu código de barras, el del proveedor, los dos, o generar uno propio (29...).",
                },
                new[] { "La utilidad sale del Excel si la trae; si no, la que ya tiene el producto; en los nuevos, la de su familia o subfamilia." }),
            new(FuncionesPlan.ReporteDia, "Recaudación del día", "bi-calendar-check-fill", "Menú → Recaudación del día",
                "Cuánto entró en el día: total, por forma de pago, por turno, por empleado y lo fiado.",
                new[]
                {
                    "Elegí el día. Arriba ves el total recaudado y el detalle por forma de pago.",
                    "Más abajo: por turno, por empleado (con lo que tiene que tener en caja) y lo que se fió.",
                    "En cada empleado, \"Ver qué vendió\" muestra el detalle de sus ventas.",
                },
                new[] { "De fábrica la ve solo el dueño. Se le puede habilitar al encargado en Ayuda y plan → Perfiles." }),
            new(FuncionesPlan.PreciosGrupo, "Cambiar precios en grupo", "bi-collection-fill", "Menú → Cambiar precios en grupo",
                "Aumentar o cambiar el precio de muchos productos juntos (ej: toda una familia un 10 %).",
                new[]
                {
                    "Buscá y tildá productos, o filtrá por familia, subfamilia o proveedor y tocá \"Agregar todos\".",
                    "Elegí qué cambiar (costo o venta, por porcentaje o monto) y revisá cómo quedan.",
                    "Si el producto se compra por bulto y cambiás el costo, el precio del bulto también se actualiza.",
                    "Guardá. Después podés mandar los productos a Carteles para imprimir los precios nuevos.",
                }),
            new(FuncionesPlan.VentaRapida, "Venta rápida y combos", "bi-lightning-charge-fill", "Menú → Venta rápida y combos",
                "Botones por categoría para vender rápido lo que no tiene código (pan, fiambre, cigarrillos) y armar combos o promos.",
                new[]
                {
                    "Venta rápida: creá categorías y sumá los productos con el buscador (se abre una ventana para tildar varios).",
                    "En Vender, el botón de venta rápida abre esas categorías.",
                    "Combos: armá un producto combo con los productos que lo forman, su precio y, si querés, vigencia y límite de unidades. Con la varita le creás un código numérico.",
                    "Al vender un combo se descuenta el stock de cada producto que lo forma.",
                    "El combo se arma solo: si en la venta escanean todos sus productos (ej: 1 pan + 2 hamburguesas), se cambian por el combo y se cobra su precio, con un cartel que avisa. Con \"Desarmar\" vuelven a quedar sueltos. Para que funcione, el combo tiene que tener los mismos productos (y cantidades) que pasan por el lector. Los combos con productos por peso (fiambres) se venden buscando el combo por su nombre.",
                }),
            new(FuncionesPlan.Fiado, "Clientes y fiado", "bi-journal-bookmark-fill", "Menú → Deudas (fiado) · Configuración → Clientes",
                "Vender fiado a clientes, ver cuánto debe cada uno y cobrar las entregas.",
                new[]
                {
                    "Cargá los clientes en Configuración → Clientes.",
                    "Al cobrar, elegí el cliente y la forma de pago \"Fiado\": se pregunta a qué precio se fía.",
                    "En Deudas ves quién debe y cuánto, con el detalle de lo que llevó.",
                    "Para cobrar: abrí el cliente y registrá la entrega (parcial o total). Se descuenta de lo más viejo primero. Se puede cobrar según el horario en que lo sacó o todo con la lista «{L1}».",
                    "Solapa \"Historial y comprobante\": todo lo que se fió y se pagó, y el comprobante en PDF para darle al cliente.",
                },
                new[] { "De fábrica las deudas las ve solo el dueño. Se pueden habilitar en Ayuda y plan → Perfiles." }),
            new(FuncionesPlan.ReportesProductos, "Reportes de productos", "bi-clipboard-data-fill", "Menú → Reportes de productos",
                "Encontrar productos con precio viejo, con poca venta o que no se venden hace mucho.",
                new[]
                {
                    "Elegí el reporte y los días (ej: precios sin actualizar hace 30 días).",
                    "Filtrá por familia, subfamilia o solo los que tienen stock.",
                    "Desde la lista podés marcar \"precio revisado\", ir a editar o descargar el Excel.",
                }),
            new(FuncionesPlan.ComprasMayorista, "Compras al mayorista", "bi-basket2-fill", "Menú → Compras al mayorista",
                "Registrar lo que se compra al mayorista y actualizar costos y stock.",
                new[]
                {
                    "Cargá la compra (a mano, con la calculadora o importando la factura en Excel).",
                    "Revisá costos y cantidades y confirmá: sube el stock y se actualizan los costos.",
                    "En \"Recibir mercadería\" podés cargar por bulto: cuántos bultos llegaron y cuánto trae cada uno (unidades, o kilos si se vende por peso). Escribís el costo del bulto y calcula el de la unidad o el del kilo.",
                    "En el historial quedan todas las compras.",
                }),
            new(FuncionesPlan.PedidosProveedores, "Pedidos a proveedores", "bi-truck", "Menú → Pedidos a proveedores",
                "Armar el pedido de reposición según el stock y después recibir la mercadería.",
                new[]
                {
                    "Elegí el proveedor. Se abre la ventana \"Agregar productos al pedido\".",
                    "En la ventana filtrá por familia, subfamilia y \"Stock actual hasta\", o prendé \"Solo los que tienen stock mínimo\" (los que tienen menos stock que su mínimo). Tildá los que querés y \"Agregar al pedido\". También podés buscar uno en particular.",
                    "En la grilla está lo que vas a pedir. La cantidad sugerida es stock ideal − stock actual; si viene por bulto o caja, se pide en bultos (el stock se cuenta en unidades, o en kilos si se vende por peso: ej. 2 bolsas de 15 kg = 30 kg).",
                    "Guardá el pedido, mandalo por WhatsApp o en PDF (al proveedor no se le dicen las unidades de cada bulto).",
                    "Cuando llega, \"Recibir mercadería\": controlás lo que vino y sube el stock.",
                }),
            new(FuncionesPlan.DeudasProveedores, "Cuentas con proveedores", "bi-cash-coin", "Menú → Cuentas con proveedores",
                "Llevar la cuenta corriente con cada proveedor: lo que se le debe y lo que se le pagó.",
                new[]
                {
                    "Elegí el proveedor. Solapa roja: cargar deuda (factura, remito). Solapa verde: registrar una entrega o pago.",
                    "Arriba ves el saldo: cuánto se le debe hoy.",
                    "En el historial queda quién cargó cada movimiento y cuándo. Si algo se cargó mal, se anula (no se borra) y queda registrado.",
                    "Podés bajar el comprobante en PDF.",
                }),
            new(FuncionesPlan.Asistencia, "Asistencia del personal", "bi-person-check-fill", "Menú → Asistencia del personal",
                "Saber a qué hora entra y sale cada empleado y cuántas horas trabajó.",
                new[]
                {
                    "El empleado ficha la entrada solo al entrar al sistema, y la salida al tocar \"Cambiar usuario\".",
                    "En Asistencia ves los horarios por día y por empleado, con las horas trabajadas (ej: \"5 horas. 45 min\"), y podés corregir un fichaje.",
                    "Si alguien se olvida de salir, el sistema cierra el turno solo.",
                }),
            new(FuncionesPlan.Encargado, "Perfiles: encargado/a y vendedor/a", "bi-person-badge", "Configuración → Ayuda y plan → Perfiles",
                "Ver qué puede hacer cada perfil y darle o sacarle cosas (ej: que el encargado vea la recaudación del día).",
                new[]
                {
                    "En Configuración → Usuarios elegí el perfil de cada empleado: encargado/a o vendedor/a.",
                    "En Ayuda y plan → Perfiles ves qué puede hacer cada uno. El dueño prende o apaga cada cosa y se guarda en el momento.",
                    "De fábrica: el encargado puede todo menos la recaudación, el fiado y los usuarios; el vendedor, vender, productos y carteles.",
                    "Usuarios, redondeo, copia de seguridad y los permisos son siempre solo del dueño.",
                },
                new[] { "Los cambios valen desde que el empleado vuelve a entrar al sistema." }),
            new(FuncionesPlan.Tareas, "Tareas del personal", "bi-list-check", "Menú → Tareas del día · Reporte de tareas",
                "Que cada empleado anote lo que hizo en el día (repuso góndola, limpió, recibió mercadería...).",
                new[]
                {
                    "El empleado entra a \"Tareas del día\" y carga cada cosa que hizo, renglón por renglón (con los minutos, si quiere).",
                    "El dueño o encargado ve en \"Reporte de tareas\" lo que hizo cada uno, por día.",
                }),
        };

        /// <summary>Cambia {L1} {L2} {L3} por el nombre de cada lista en este negocio.</summary>
        public static string ConNombres(string? texto, string l1, string l2, string l3) =>
            (texto ?? "").Replace("{L1}", l1).Replace("{L2}", l2).Replace("{L3}", l3);

        /// <summary>Tema de una función del plan (null si no hay ayuda cargada).</summary>
        public static Tema? DeFuncion(string clave) => Temas.FirstOrDefault(t => t.Clave == clave);
    }
}
