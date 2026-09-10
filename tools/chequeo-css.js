const fs=require('fs');
const s=fs.readFileSync(require('path').join(__dirname,'..','prototipo','index.html'),'utf8');
const css=s.split('<style>')[1].split('</style>')[0];
// clases usadas en el HTML/JS
const usadas=new Set();
for(const m of s.matchAll(/class="([^"$]*)"/g)) m[1].split(/\s+/).forEach(c=>c&&usadas.add(c));
for(const m of s.matchAll(/class="([^"]*?)\$\{[^}]*\}([^"]*)"/g)){
  (m[1]+' '+m[2]).split(/\s+/).forEach(c=>c&&usadas.add(c));
}
const definidas=new Set();
for(const m of css.matchAll(/\.([a-zA-Z][\w-]*)/g)) definidas.add(m[1]);
const faltan=[...usadas].filter(c=>!definidas.has(c)&&!c.includes('$')&&!c.includes('?')).sort();
console.log('\n  Clases usadas en el prototipo que no tienen CSS:');
console.log(faltan.length? '   x '+faltan.join('\n   x ')+'\n' : '   ninguna. Todo estilado.\n');
if(faltan.length)process.exit(1);
