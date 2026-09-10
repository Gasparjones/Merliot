# Brief: editor de cartas

## Qué problema resuelve

Hoy para cambiar una carta hay que editar JSON a mano y abrir el prototipo para ver
si rompiste algo. Eso frena el diseño.

Pero ojo: **un editor que sea sólo un formulario es peor que editar el JSON a mano.**
El valor no está en los campos, está en *ver las consecuencias mientras editás*.

## Lo que tiene que mostrar mientras editás

Esto es lo que justifica la herramienta:

1. **Cobertura del dado.** Una barra del 2 al 12 mostrando cuántas cartas producen en cada
   resultado, y de qué recurso. Si un número queda pelado, se tiene que ver enseguida.
2. **Curva de costos.** Cuántas cartas hay de 1, 2, 3, 4+ cristales. Hoy está cargada arriba.
3. **Reparto por recurso.** Cuántas bandas producen Vigor, Temple, Destreza, Saber.
   La Destreza viene quedando escasa.
4. **Reparto por raza.** Cuántos héroes y cuántas mejoras exclusivas tiene cada una.
   Una raza con héroes pero sin mejora propia no tiene arquetipo.
5. **Validación en vivo** contra los invariantes del `CLAUDE.md`.

## Forma sugerida

Una app local que lea y escriba `data/merliot.json` directamente.
Node + un HTML, sin build step, sin framework pesado. Se levanta con un comando
y se abre en el navegador. Que escriba el archivo de verdad, no que descargue una copia:
si hay que mover archivos a mano, se deja de usar.

Importante: **preservar el formato y el orden del JSON** al guardar, para que los diffs
de git sean legibles y se pueda ver qué cambió en cada commit.

## Cosas que van a hacer falta pronto

- Duplicar una carta como punto de partida para otra.
- Filtrar por tipo, raza y costo.
- Marcar cartas como borrador para que no entren al mazo del prototipo.
- Ver qué cartas todavía no tienen arte.

## Lo que NO debería hacer

- No manejar el arte adentro. Los archivos van en carpeta, se buscan por `id`.
- No inventar campos nuevos sin actualizar `CLAUDE.md` y los scripts de C#.
- No convertirse en el juego. Es una herramienta de diseño.

---

## Estado: construido

`tools/editor.py` + `tools/editor.html`. Se levanta con `python3 tools/editor.py`.

En Python y no en Node como decía "forma sugerida", nada más porque en esta máquina
no hay `node` y el brief pedía "se levanta con un comando, sin build step". Sólo
biblioteca estándar. Si algún día se estandariza en Node, portarlo es directo: la
única lógica no trivial del servidor es `canonizar_carta`.

Lo que pedía el brief y está:

- Los cinco paneles en vivo: cobertura del dado, curva de costos, reparto por
  recurso, reparto por raza y los invariantes del `CLAUDE.md`.
- Escribe el archivo de verdad, atómicamente, con respaldo previo.
- **Preserva formato y orden de claves.** Guardar sin tocar nada deja el archivo
  byte por byte igual; cambiar tres valores da un diff de tres líneas. Esto lo
  garantiza `canonizar_carta`, que reconstruye cada carta en el orden canónico y
  omite lo vacío, en vez de confiar en que el navegador no reordene nada.
- Duplicar, filtrar por tipo/raza/costo, y ver qué cartas no tienen arte.

Lo que falta del brief:

- **Marcar cartas como borrador.** Es un campo nuevo (`borrador`), así que antes hay
  que tocar `CLAUDE.md`, `MerliotData.cs` y quien arma el mazo. No se hizo por eso.

Decisiones que vale la pena no revivir:

- **El `id` es de sólo lectura.** Invariante 5: el arte se busca por id y en C# hay
  búsquedas por id literal (`la-semilla`, `mutaci-n`). Se renombra por `nombre`.
- **Renombrar arrastra `mazoInicial`**, que referencia por nombre. Sin eso, cambiar
  el nombre de una carta vacía el mazo de salida en silencio. El servidor además se
  niega a guardar si quedó alguna referencia colgada.
- **En los paneles la identidad la carga la posición, no el color.** `temple`
  (`#5f8fa8`) y `saber` (`#9a86c4`) están a ΔE 9.7 a vista normal y 4.4 con
  deuteranopía: son casi el mismo color. La paleta es la del juego y no se cambia
  desde acá, así que cada recurso tiene su fila rotulada y su número, y el color
  sólo acompaña. Si alguna vez se retocan esos dos colores en el juego, el editor
  los hereda solo.
- **Sólo edita `cartas`.** Criaturas, lugares, efímeros, terrenos, sitios y marcas
  se siguen tocando a mano; meterlos acá sin un brief propio es convertirlo en el
  juego, que es justo lo que el brief prohíbe.
