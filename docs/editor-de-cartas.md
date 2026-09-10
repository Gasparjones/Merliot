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
