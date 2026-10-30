import assert from 'node:assert/strict';
import test from 'node:test';
import {readFileSync} from 'node:fs';
import ts from 'typescript';
const source=readFileSync(new URL('../../resources/js/lib/session-deadline.ts',import.meta.url),'utf8');
const js=ts.transpileModule(source,{compilerOptions:{target:ts.ScriptTarget.ES2022,module:ts.ModuleKind.ES2022}}).outputText;
const {SessionDeadline}=await import(`data:text/javascript;base64,${Buffer.from(js).toString('base64')}`);
const start=1000000000000,info={lifetime_ms:7200000,id:'sesion-sintetica',started_at:start,expires_at:start+7200000,server_now:start};
test('reloj servidor y monotónico: válido antes y vencido exactamente a dos horas',()=>{
 const clock=new SessionDeadline();clock.configure(info,0);assert.equal(clock.remaining(7199999),1);assert.equal(clock.remaining(7200000),0);
});
test('actualizar página, actividad o polling no reinicia ni amplía el límite',()=>{
 const clock=new SessionDeadline();clock.configure(info,0);clock.configure({...info,server_now:start+3600000},3600000);assert.equal(clock.remaining(3600000),3600000);
 clock.configure({...info,expires_at:start+14400000,server_now:start+3600000},3600000);assert.equal(clock.remaining(7200000),0);
});
test('ambos sitios/pestañas tienen el mismo vencimiento y un login distinto se distingue',()=>{
 const a=new SessionDeadline(),b=new SessionDeadline();a.configure(info,0);b.configure({...info,server_now:start+10000},10000);assert.equal(a.remaining(7200000),b.remaining(7200000));assert.equal(a.configure({...info,id:'otra-sesion'},20000),'changed');
});
