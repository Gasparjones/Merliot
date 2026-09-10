# MERLIOT — contexto para Claude Code

Juego de aventura por turnos con cartas y dados. **El destino final es una caja física**;
el build digital es para playtestear y ajustar números. No optimices para el motor:
optimizá para que sea fácil cambiar una carta y volver a probar.

Los autores son dos ilustradores. El arte no es decoración: es la mitad del proyecto.
Todo lo que se construya tiene que dejarles espacio para dibujar, no quitárselo.

## Regla de oro

**Los datos viven en `data/merliot.json`. Nunca hardcodees valores de balance en el código.**

Si vas a agregar una carta, un enemigo o un terreno: se toca el JSON.
Si el código necesita un número de balance, lo lee del JSON.
El `prototipo/index.html` todavía tiene su copia embebida — es deuda conocida.

## Estructura

- `data/merliot.json` — fuente de verdad: symlink a `Assets/Resources/merliot.json`
- `prototipo/index.html` — prototipo jugable, se abre sin servidor
- `docs/reglas.md` — reglas completas
- `docs/notas-de-diseño.md` — **leelo antes de proponer cambios de diseño**
- `Assets/Scripts/Merliot/` — el juego en Unity; `Assets/Editor/Merliot/` — el validador
- `tools/editor.py` — **el editor de cartas**: `python3 tools/editor.py`
- `tools/` — validadores en Node, para correr sin abrir el editor

## Modelo de datos

Una `carta` tiene `tipo`: `perm` (héroe), `mejora`, `amuleto` o `uso`.

- **perm**: tiene `vida`, `raza` y `bandas`. Va a la fila y produce recursos según el dado.
- **mejora**: se engancha a un héroe (2 slots por héroe). Puede tener `soloRaza`.
- **amuleto**: no produce, cambia una regla (`pasiva`: `dobles` | `barato` | `escudo` | `tintero`).
  Máximo 2. `tintero` anda en el prototipo y todavía no está en `Partida.cs`.
- **uso**: se juega una vez y se descarta.

Una `banda` es `{desde, hasta, produce}` sobre una tirada de 2d6, o sea **el rango vive entre 2 y 12**.
`porCadaRaza` multiplica la producción por cuántos miembros de esa raza haya en la fila.

Un `terreno` tiene `etapa` (1, 2 o 3), `intro` y `problema`. Un `efimero` tiene `nivel`:
el mazo de efímeros **rota** por etapa (entran los del nivel de la etapa y los del anterior).
`La semilla` y `Mutación` están en `cartas` con `copias: 0`: no se reparten, entran por la campaña.

`mazoInicial` es con lo que salís de la abadía; el resto del catálogo es recompensa.
Una banda puede dar `roba` además de `produce`, `cristales` o `cura`.

Un `sitio` no se destruye: se usa. Tiene un `trueque` (`costo` en recursos o en `cristales`,
y devuelve `roba` o `cura`), el pago se acumula entre turnos y al cobrarse vuelve a quedar
disponible. No cuenta para limpiar el terreno. En `apariciones` va con `q: "sitio"`.

Una `marca` es un hecho que el juego se acuerda para siempre. Hace dos cosas:
un efímero con `requiere` sólo entra al mazo si tenés la marca, y un lugar con `descuento`
cuesta menos si la tenés. `marcaSiFalla` da la marca por **no** resolver el efímero.

Recursos: `vigor` (mata criaturas), `temple` (se asigna como escudo), `destreza` (reordenar la fila),
`saber` (desarma lugares). Se pierden al terminar el turno.
Los `cristales` son aparte: sólo invocan, y son lo único que se acumula.

## Invariantes — si se rompen, el juego está roto

1. Toda banda cumple `2 <= desde <= hasta <= 12`.
2. Todo `perm` tiene `vida > 0` y al menos una banda.
3. Todo efímero tiene al menos una vía de resolución.
4. Toda `soloRaza` y toda `raza` existe en `razas`.
5. Los `id` son estables. Si renombrás una carta, cambiá `nombre`, **nunca `id`**:
   el arte se busca por id (`Arte/Cartas/{id}.png`).
6. Debería haber algo que produzca en cada resultado del 2 al 12. Si un número queda
   vacío, ese turno el jugador no hace nada y eso se siente pésimo.
7. Toda `aparicion` nombra una criatura, un lugar o un sitio que existe, según su `q`.
8. Hay al menos un terreno por etapa, y dos para elegir en la 2 y en la 3.
9. Todo `requiere`, `marcaSiFalla` y `descuento.marca` nombra una marca que existe.
10. Toda carta del `mazoInicial` existe en `cartas`.

## Decisiones ya tomadas — no las revivas sin motivo nuevo

- **No hay barra de vida global.** El daño va a la fila de héroes, de adelante hacia atrás.
- **No hay tope de tropas.** Ir ancho se castiga con barridos, no con una regla.
- **Los caídos van al cementerio, no al descarte.**
- **Cartas y cristales no se compran entre sí.** Se probó quemar cartas por cristales y se descartó.
- **Los efectos enemigos se anuncian al tirar y se aplican al terminar el turno**,
  para que el jugador pueda responder.
- **Dos de cualquier recurso valen uno del que falte.** Nunca te quedás trabado; pagás de más.

`docs/notas-de-diseño.md` tiene los errores que ya cometimos. Vale la pena leerlo:
varios son tentadores de repetir.

## Trampa conocida

El prototipo se editó mucho por reemplazo de texto, y **varias veces se agregó HTML
con clases que nunca tuvieron CSS**: el reemplazo fallaba en silencio y la interfaz
quedaba sin estilo sin que nadie se enterara. Si agregás marcado nuevo, corré:

```bash
node tools/chequeo-css.js
```

Lista las clases usadas que no tienen ninguna regla. Sale con código 1 si falta alguna.

## Cómo cambiar una carta

`python3 tools/editor.py` y se abre solo. Editás nombres, stats, costos, bandas y
efectos, y mientras editás ves la cobertura del dado, la curva de costos, el reparto
por recurso y por raza, y los invariantes de acá arriba corriendo en vivo.

Escribe `data/merliot.json` de verdad, preservando el orden de las claves: una carta
que no tocaste vuelve al archivo byte por byte igual, así que el diff de git muestra
sólo lo que cambiaste. Respaldos en `tools/.respaldos/`.

**El `id` no se edita** (invariante 5). Renombrar se hace en `nombre`, y el editor
arrastra el cambio a `mazoInicial`, que referencia las cartas por nombre.

## Cómo probar

- Prototipo: abrir `prototipo/index.html` en el navegador.
- Estilos: `node tools/chequeo-css.js`
- Validar el JSON sin Unity: `node tools/validar.js`
- Validar el JSON en Unity: menú **Merliot → Validar datos**
  (`Assets/Editor/Merliot/MerliotValidator.cs`, corre los invariantes de acá arriba).
- Tests: **Window → General → Test Runner → PlayMode**. El `README.md` tiene las
  líneas para correr validador y tests desde la terminal, en batchmode.

## Estilo

Todo en castellano rioplatense: nombres de cartas, textos, y también las claves del JSON.
Es un juego argentino y la consistencia importa más que la convención inglesa.
