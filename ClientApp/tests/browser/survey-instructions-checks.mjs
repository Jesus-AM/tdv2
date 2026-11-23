import {expect} from '@playwright/test';

// Texto solicitado, independiente del recurso productivo. Se comprueba el DOM visible.
const expected={
 perfil:{title:'Perfil',subtitle:'Comencemos por conocerte',paragraphs:[
  'Completa tu perfil antes de iniciar el diagnóstico. Esta información nos permite asociar tus resultados con tu dependencia e identificar qué capacitación puede ayudarte según las funciones de tu puesto.',
  'Los campos marcados con un asterisco (*) son obligatorios.',
 ]},
 m365:{title:'Microsoft 365',subtitle:'Herramientas de Microsoft 365',paragraphs:[
  'En esta sección encontrarás las herramientas de Microsoft 365. Para cada una, selecciona la opción que mejor describa lo que puedes hacer actualmente en tu trabajo.',
 ],options:[
  'Elige el nivel más alto cuyas actividades puedas realizar, incluidas las de los niveles anteriores.',
  'Selecciónalo si la herramienta corresponde a tus funciones y todavía no la utilizas.',
  'Selecciónalo si la herramienta no corresponde a tus funciones. Esta opción queda fuera del puntaje.',
  'Elige esta opción si sabes utilizar la herramienta, aunque tu puesto no la requiera. Selecciona tu nivel: Básico, Intermedio o Avanzado. Este nivel cuenta para tus conocimientos digitales generales y queda fuera del puntaje de tu puesto.',
  'Si dejas una herramienta sin seleccionar, quedará pendiente.',
 ]},
 ciber:{title:'Ciberseguridad',paragraphs:[
  'En esta sección encontrarás prácticas para proteger tus cuentas, equipos e información en el trabajo. En cada tema, marca todas las actividades que ya realizas, aunque sean de distintos niveles.',
 ],options:[
  'Alcanzas un nivel cuando realizas al menos 3 de sus 4 actividades.',
  'Selecciónalo si las prácticas del tema corresponden a tus funciones y todavía no realizas ninguna.',
  'Selecciónalo si las prácticas del tema no corresponden a tus funciones. Esta opción queda fuera del puntaje.',
  'Si sabes realizar prácticas de este tema que tu puesto no requiere, marca las actividades que sabes hacer para determinar tu nivel. Este nivel cuenta para tus conocimientos digitales generales y queda fuera del puntaje de tu puesto.',
  'Si dejas un tema sin responder, quedará pendiente.',
 ],note:true},
 datos:{title:'Gobernanza de datos',paragraphs:[
  'En esta sección encontrarás prácticas relacionadas con la calidad, la protección, la transparencia y el uso de los datos institucionales. En cada tema, marca todas las actividades que ya realizas, aunque sean de distintos niveles.',
 ],options:[
  'Alcanzas un nivel cuando realizas al menos 3 de sus 4 actividades.',
  'Selecciónalo si las actividades del tema corresponden a tus funciones y todavía no realizas ninguna.',
  'Selecciónalo si las actividades del tema no corresponden a tus funciones. Esta opción queda fuera del puntaje.',
  'Si sabes realizar actividades de este tema que tu puesto no requiere, marca las que sabes hacer para determinar tu nivel. Este nivel cuenta para tus conocimientos digitales generales y queda fuera del puntaje de tu puesto.',
  'Si dejas un tema sin responder, quedará pendiente.',
 ],note:true},
 sii:{title:'Sistema Integral de Información',paragraphs:[
  'En esta sección encontrarás actividades de los módulos institucionales generales como POA adquisiciones, egresos, gestión documental, boletos de avión y transporte.',
  'En cada módulo, marca todas las actividades que ya realizas, aunque sean de distintos niveles.',
 ],options:[
  'Alcanzas un nivel cuando realizas al menos 3 de sus 4 actividades.',
  'Selecciónalo si el módulo corresponde a tus funciones y todavía no realizas ninguna de las actividades indicadas.',
  'Selecciónalo si el módulo no corresponde a tus funciones. Esta opción queda fuera del puntaje.',
  'Si sabes realizar actividades de este módulo que tu puesto no requiere, marca las que sabes hacer para determinar tu nivel. Este nivel cuenta para tus conocimientos digitales generales y queda fuera del puntaje de tu puesto.',
  'Si dejas un módulo sin responder, quedará pendiente.',
 ],note:true},
 ia:{title:'Inteligencia artificial',paragraphs:[
  'En esta sección encontrarás principios para usar la inteligencia artificial (IA) de manera responsable en tu trabajo. Las actividades se centran en lo que puedes hacer con las versiones básicas y gratuitas de estas herramientas, como redactar una solicitud clara, revisar la respuesta antes de utilizarla y proteger la información institucional que manejas.',
  'Para cada tema, selecciona la opción que mejor describa lo que puedes hacer actualmente.',
 ],options:[
  'Elige el nivel más alto cuyas actividades puedas realizar, incluidas las de los niveles anteriores. Los niveles describen tus habilidades, independientemente del plan de la herramienta que utilices.',
  'Selecciónalo si las actividades del tema corresponden a tus funciones y todavía no realizas ninguna.',
  'Selecciónalo si las actividades del tema no corresponden a tus funciones. Esta opción queda fuera del puntaje.',
  'Elige esta opción si sabes realizar las actividades del tema, aunque tu puesto no las requiera. Selecciona tu nivel: Básico, Intermedio o Avanzado. Este nivel cuenta para tus conocimientos digitales generales y queda fuera del puntaje de tu puesto.',
  'Si dejas un tema sin responder, quedará pendiente.',
 ]},
};
const leads=['Básico, Intermedio o Avanzado.','Inicial.','No aplica.','No aplica a mis funciones, pero sé hacerlo.','Sin respuesta.'];
const note='Nota: Para alcanzar Intermedio, debes cumplir también Básico. Para alcanzar Avanzado, debes cumplir Básico e Intermedio.';
export async function verifySurveyInstructions(page,id){
 const value=expected[id],intro=page.locator('.cd-survey-intro');
 await expect(intro).toHaveCount(1);await expect(intro.locator('h2')).toHaveText(value.title);
 await expect(intro.locator('h3')).toHaveText(value.subtitle?[value.subtitle]:[]);
 await expect(intro.locator(':scope > p')).toHaveText([...value.paragraphs,...value.note?[note]:[]]);
 await expect(intro.getByRole('list')).toHaveCount(value.options?1:0);
 await expect(intro.getByRole('listitem')).toHaveText(value.options?.map((text,i)=>leads[i]+' '+text)||[]);
 await expect(intro.locator('strong')).toHaveText(id==='perfil'?[value.paragraphs[1]]:[...leads,...value.note?['Intermedio','Básico','Avanzado','Básico','Intermedio']:[]]);
 await expect(intro.locator('ul .cd-instruction-note')).toHaveCount(0);
 await expect(intro.locator(':scope > .cd-instruction-note')).toHaveCount(value.note?1:0);
 for(const paragraph of [...value.paragraphs,...value.note?[note]:[]]){
  const escape=paragraph.replace(/[.*+?^${}()|[\]\\]/g,'\\$&');
  await expect(page.locator('#cd-main p').filter({hasText:new RegExp('^'+escape+'$')})).toHaveCount(1);
 }
 await expect(page.getByRole('button',{name:'Sin respuesta',exact:true})).toHaveCount(0);
 await expect(page.getByRole('radio',{name:'Sin respuesta',exact:true})).toHaveCount(0);
 await expect(page.locator('#cd-main')).not.toContainText(/Con estos datos ubicamos tus resultados|Elige tu nivel:|es un nivel cero válido|Principios de uso y las 22 herramientas/);
}
