#!/usr/bin/env node
// Chequea data/merliot.json contra los invariantes del CLAUDE.md.
// Uso: node tools/validar.js
const fs = require('fs');
const path = require('path');

const ruta = path.join(__dirname, '..', 'data', 'merliot.json');
const d = JSON.parse(fs.readFileSync(ruta, 'utf8'));
if (!d.terrenos || !d.terrenos.length) { console.error('Sin terrenos'); process.exit(1); }
const errores = [], avisos = [];
const razas = new Set(d.razas.map(r => r.id));
const RECURSOS = ['vigor', 'temple', 'destreza', 'saber'];

// ── invariantes ──
const ids = new Set();
for (const c of d.cartas) {
  if (ids.has(c.id)) errores.push(`id repetido: ${c.id}`);
  ids.add(c.id);
  if (!c.id || !/^[a-z0-9-]+$/.test(c.id)) errores.push(`${c.nombre}: id inválido "${c.id}"`);
  if (!(c.copias > 0)) errores.push(`${c.nombre}: sin copias`);
  if (c.costoCristales === undefined) errores.push(`${c.nombre}: sin costo`);
  if (c.raza && !razas.has(c.raza)) errores.push(`${c.nombre}: raza desconocida "${c.raza}"`);
  if (c.soloRaza && !razas.has(c.soloRaza)) errores.push(`${c.nombre}: soloRaza desconocida "${c.soloRaza}"`);
  if (c.tipo === 'perm') {
    if (!(c.vida > 0)) errores.push(`${c.nombre}: héroe sin vida`);
    if (!c.bandas || !c.bandas.length) errores.push(`${c.nombre}: héroe sin bandas`);
    if (!c.raza) avisos.push(`${c.nombre}: héroe sin raza`);
  }
  for (const b of c.bandas || []) {
    if (!(b.desde >= 2 && b.hasta <= 12 && b.desde <= b.hasta))
      errores.push(`${c.nombre}: banda fuera de rango ${b.desde}-${b.hasta}`);
    if (b.porCadaRaza && !razas.has(b.porCadaRaza))
      errores.push(`${c.nombre}: porCadaRaza desconocida "${b.porCadaRaza}"`);
    if (!b.produce && !b.cristales && !b.cura)
      errores.push(`${c.nombre}: banda ${b.desde}-${b.hasta} no produce nada`);
  }
}
for (const e of d.efimeros) {
  if (!e.vias || !e.vias.length) errores.push(`${e.nombre}: efímero sin vía de resolución`);
  if (!e.bueno && !e.efecto) errores.push(`${e.nombre}: efímero malo sin efecto`);
  for (const v of e.vias || [])
    if (!v.costo || !RECURSOS.some(r => v.costo[r])) errores.push(`${e.nombre}: vía sin costo`);
}
for (const c of d.criaturas) if (!(c.vida > 0)) errores.push(`${c.nombre}: criatura sin vida`);
for (const l of d.lugares)
  if (!l.costo || !RECURSOS.some(r => l.costo[r])) errores.push(`${l.nombre}: lugar sin costo`);

// ── cobertura del dado ──
const cobertura = {};
for (let v = 2; v <= 12; v++) {
  cobertura[v] = { cartas: 0 };
  for (const r of RECURSOS) cobertura[v][r] = 0;
  for (const c of d.cartas)
    for (const b of c.bandas || [])
      if (b.desde <= v && b.hasta >= v && b.produce) {
        cobertura[v].cartas++;
        for (const r of RECURSOS) cobertura[v][r] += (b.produce[r] || 0) * c.copias;
      }
  if (!cobertura[v].cartas) errores.push(`Ninguna carta produce con ${v}`);
}

// ── informe ──
const heroes = d.cartas.filter(c => c.tipo === 'perm');
console.log(`\n  ${d.cartas.length} cartas (${d.cartas.reduce((a, c) => a + c.copias, 0)} copias) · ` +
            `${d.criaturas.length} criaturas · ${d.lugares.length} lugares · ${d.efimeros.length} efímeros\n`);

console.log('  COBERTURA DEL DADO');
for (let v = 2; v <= 12; v++) {
  const c = cobertura[v];
  const barra = RECURSOS.map(r => r[0].toUpperCase().repeat(Math.min(9, Math.round(c[r] / 4)))).join('');
  console.log(`   ${String(v).padStart(2)}  ${String(c.cartas).padStart(2)} cartas  ${barra}`);
}

const curva = {};
heroes.forEach(c => curva[c.costoCristales] = (curva[c.costoCristales] || 0) + c.copias);
console.log('\n  CURVA DE HÉROES  ' + Object.keys(curva).sort().map(k => `${k}cr:${curva[k]}`).join('  '));

const porRaza = {};
d.razas.forEach(r => porRaza[r.id] = { heroes: 0, mejoras: 0 });
heroes.forEach(c => { if (c.raza) porRaza[c.raza].heroes++; });
d.cartas.filter(c => c.tipo === 'mejora' && c.soloRaza).forEach(c => porRaza[c.soloRaza].mejoras++);
console.log('\n  POR RAZA');
d.razas.forEach(r => {
  const p = porRaza[r.id];
  const alerta = p.mejoras === 0 ? '  ← sin mejora propia' : '';
  console.log(`   ${r.nombre.padEnd(9)} ${p.heroes} héroes · ${p.mejoras} mejoras${alerta}`);
});

console.log('');
if (avisos.length) { console.log('  AVISOS'); avisos.forEach(a => console.log('   · ' + a)); console.log(''); }
if (errores.length) {
  console.log('  ERRORES'); errores.forEach(e => console.log('   ✗ ' + e));
  console.log(`\n  ${errores.length} problemas.\n`);
  process.exit(1);
}
console.log('  Todo en orden.\n');
