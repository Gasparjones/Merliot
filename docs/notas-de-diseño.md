# Notas de diseño

Cosas que ya se probaron y por qué quedaron así. Sirve para no volver a discutirlas.

## Decisiones tomadas

**El daño va a los héroes, no a una barra de vida.** No hay aguante global.
La fila numerada recibe el golpe de adelante hacia atrás.

**No hay tope de tropas.** El límite lo ponen los cristales y el mazo.
Lo que castiga ir ancho son los **barridos** (daño a cada héroe), no una regla.

**Los caídos van al cementerio, no al descarte.** Si volvieran al mazo,
morir no costaría nada. Se sale del cementerio sólo con cartas específicas.

**Dos economías separadas.** Cartas y cristales no se compran entre sí.
Se probó "quemar cartas por cristales" y se descartó: mezclaba los dos recursos.

**Los efectos enemigos se anuncian y se aplican al final del turno.**
Si pegaran al tirar, escudar llegaría siempre tarde y no habría decisión.

## Errores que ya cometimos

- **Refugios reutilizables.** Se podía rebotar sobre la abadía y curarse infinito.
- **Fallar sin recompensa.** Perder una zona no daba carta, así que perder te hacía
  perder más. Ahora cruzar terreno nuevo siempre deja algo.
- **Tope de party puesto para tapar un problema de balance.** Si el motor le gana
  al juego, el arreglo es más presión, no una prohibición.
- **Texto y mecánica en lugares distintos.** Las cartas decían "recuperás 4 de aguante"
  cuando el aguante ya no existía. Los textos de efecto se generan desde los datos.
- **Empezar con muchos recursos.** Tapaba que el ingreso por turno era demasiado bajo.

## Cosas sin resolver

- La curva del mazo está cargada arriba: hay que emparejarla.
- 111 cartas es mucho para una caja. El mazo real debería andar en 40.
- Falta el mapa de exploración: hoy hay un solo terreno.
- Falta la campaña de la semilla, que es el motivo por el que existe el juego.
