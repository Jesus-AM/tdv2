
const questions = [
 {m:"Microsoft 365",q:"Necesitas trabajar desde otra computadora y encontrar el mismo archivo que guardaste ayer. ¿Qué opción está pensada para eso?",a:["OneDrive","Calculadora","Paint","Bloc de notas"],c:0,e:"OneDrive permite almacenar archivos en la nube y acceder a ellos desde distintos dispositivos."},
 {m:"Word",q:"Quieres que todos los títulos de un documento tengan el mismo formato y puedas cambiarlo de manera global. ¿Qué usarías?",a:["Copiar y pegar formato manualmente","Estilos","Captura de pantalla","Zoom"],c:1,e:"Los estilos permiten aplicar y mantener formatos consistentes en documentos largos."},
 {m:"Excel",q:"Tienes una tabla con miles de registros y quieres mostrar solamente los de una determinada unidad. ¿Qué usarías primero?",a:["Filtro","Combinar celdas","WordArt","Transición"],c:0,e:"El filtro permite mostrar temporalmente los registros que cumplen determinados criterios."},
 {m:"Excel",q:"En una celda quieres sumar automáticamente los valores de B2 a B20. ¿Cuál fórmula es adecuada?",a:["=SUMA(B2:B20)","=CONTAR(B2-B20)","=TOTAL(B2;B20)","=ADD(B2:B20)"],c:0,e:"SUMA con un rango B2:B20 agrega todos los valores del intervalo."},
 {m:"PowerPoint",q:"Una presentación tiene texto, datos y gráficos, pero se ve saturada. ¿Qué acción ayuda más?",a:["Agregar más animaciones","Reducir contenido y jerarquizar visualmente","Poner todo en negritas","Usar una fuente diferente en cada diapositiva"],c:1,e:"Una presentación efectiva prioriza la información y utiliza una jerarquía visual clara."},
 {m:"Outlook",q:"Quieres programar una reunión y permitir que las personas reciban la invitación en su calendario. ¿Qué usarías?",a:["Nuevo evento/reunión del calendario","Bloc de notas","WordArt","Papelera"],c:0,e:"Los eventos y reuniones del calendario permiten convocar participantes y gestionar la cita."},
 {m:"Teams",q:"Un equipo de trabajo necesita conversar, compartir archivos y mantener las conversaciones organizadas por proyecto. ¿Qué estructura es adecuada?",a:["Un chat individual para todo","Equipo con canales","Solo correos personales","Una carpeta local"],c:1,e:"Teams permite organizar la colaboración mediante equipos y canales según temas o proyectos."},
 {m:"Microsoft 365",q:"Una compañera necesita editar contigo un documento al mismo tiempo. ¿Qué característica facilita esta tarea?",a:["Coautoría","Imprimir el documento","Cambiar el fondo","Cerrar sesión"],c:0,e:"La coautoría permite que varias personas trabajen simultáneamente en archivos compatibles almacenados en la nube."},
 {m:"Forms",q:"Quieres aplicar un diagnóstico a 100 personas y obtener las respuestas organizadas automáticamente. ¿Qué herramienta usarías?",a:["Microsoft Forms","Paint","Calculadora","PowerPoint"],c:0,e:"Microsoft Forms permite crear formularios y cuestionarios y recopilar sus respuestas."},
 {m:"Seguridad",q:"Recibes un correo inesperado que solicita tu contraseña mediante un enlace. ¿Qué debes hacer?",a:["Dar clic inmediatamente","Compartir la contraseña","Verificar el remitente y no proporcionar credenciales mediante enlaces sospechosos","Reenviarlo a todos"],c:2,e:"Las solicitudes inesperadas de credenciales son una señal de alerta. Verifica el origen antes de actuar."}
];

let index=0,score=0,xp=0,streak=0,maxStreak=0,answered=false;

function startGame(){
 document.getElementById("welcome").classList.add("hidden");
 document.getElementById("result").classList.add("hidden");
 document.getElementById("quiz").classList.remove("hidden");
 document.getElementById("liveStats").classList.remove("hidden");
 index=score=xp=streak=maxStreak=0; render();
}
function render(){
 const q=questions[index];
 document.getElementById("counter").textContent=`Reto ${index+1} de ${questions.length}`;
 document.getElementById("percent").textContent=Math.round(index/questions.length*100)+"%";
 document.getElementById("bar").style.width=(index/questions.length*100)+"%";
 document.getElementById("module").textContent=q.m;
 document.getElementById("question").textContent=q.q;
 document.getElementById("streak2").textContent=streak;
 document.getElementById("streak").textContent=streak;
 document.getElementById("xp").textContent=xp;
 const box=document.getElementById("answers"); box.innerHTML="";
 q.a.forEach((ans,i)=>{
   const b=document.createElement("button"); b.className="answer";
   b.innerHTML=`<span class="letter">${String.fromCharCode(65+i)}</span><span>${ans}</span>`;
   b.onclick=()=>answer(i,b); box.appendChild(b);
 });
 const f=document.getElementById("feedback"); f.className="feedback"; f.innerHTML="";
 document.getElementById("nextBtn").style.display="none"; answered=false;
}
function answer(choice,btn){
 if(answered)return; answered=true;
 const q=questions[index];
 document.querySelectorAll(".answer").forEach((b,i)=>{
   b.disabled=true;
   if(i===q.c)b.classList.add("correct");
 });
 const f=document.getElementById("feedback");
 if(choice===q.c){
   score++; streak++; maxStreak=Math.max(maxStreak,streak); xp+=100+(streak-1)*20;
   f.className="feedback show good"; f.innerHTML=`<b>🎉 ¡Correcto!</b> +${100+(streak-1)*20} XP<br><span>${q.e}</span>`;
 }else{
   streak=0;
   btn.classList.add("wrong");
   f.className="feedback show bad"; f.innerHTML=`<b>💡 Casi.</b> La respuesta correcta es <b>${q.a[q.c]}</b>.<br><span>${q.e}</span>`;
 }
 document.getElementById("streak2").textContent=streak;
 document.getElementById("streak").textContent=streak;
 document.getElementById("xp").textContent=xp;
 document.getElementById("nextBtn").style.display="block";
}
function nextQuestion(){
 index++;
 if(index>=questions.length) finish(); else render();
}
function finish(){
 document.getElementById("quiz").classList.add("hidden");
 document.getElementById("liveStats").classList.add("hidden");
 document.getElementById("result").classList.remove("hidden");
 const pct=Math.round(score/questions.length*100);
 let title,text,level;
 if(pct>=90){title="🚀 ¡Nivel experto!";level="Avanzado";text="Tienes una base muy sólida para aprovechar Microsoft 365 en situaciones reales."}
 else if(pct>=70){title="🌟 ¡Muy buen nivel!";level="Intermedio";text="Dominas varias herramientas y tienes una buena base para seguir profundizando."}
 else if(pct>=50){title="💪 ¡Buen comienzo!";level="Operativo";text="Ya cuentas con conocimientos útiles; algunas áreas pueden fortalecerse."}
 else{title="🌱 ¡Vamos paso a paso!";level="Básico";text="El diagnóstico te muestra oportunidades claras para desarrollar tus competencias digitales."}
 document.getElementById("resultTitle").textContent=title;
 document.getElementById("resultText").textContent=text;
 document.getElementById("score").textContent=pct+"%";
 document.getElementById("correctCount").textContent=`${score}/${questions.length}`;
 document.getElementById("finalXP").textContent=xp+" XP";
 document.getElementById("level").textContent=level;
 document.getElementById("maxStreak").textContent=maxStreak;
 document.getElementById("ring").style.background=`conic-gradient(#2563eb ${pct*3.6}deg,#e9eef6 0deg)`;
 const badges=[];
 if(score>=7)badges.push("🏅 Mente Microsoft");
 if(maxStreak>=3)badges.push("🔥 Racha x3");
 if(score===10)badges.push("💎 Perfección");
 if(xp>=1000)badges.push("⚡ Coleccionista XP");
 if(!badges.length)badges.push("🎯 Primer reto completado");
 document.getElementById("badges").innerHTML=badges.map(x=>`<span class="badge">${x}</span>`).join("");
}
function restart(){startGame()}
