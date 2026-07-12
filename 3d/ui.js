// ---------------------------------------------------------------------------
// Minimap and UI.
// ---------------------------------------------------------------------------
const mini=$('#minimap'),mctx=mini.getContext('2d');
function drawMinimap(){const w=mini.width,h=mini.height,sx=w/WORLD.w,sz=h/WORLD.d;mctx.fillStyle='#071015';mctx.fillRect(0,0,w,h);mctx.fillStyle='#15242a';mctx.fillRect(0,0,w,h);mctx.fillStyle='#243238';[[0,277,620,46],[229,0,42,620],[402,0,36,620],[0,144,620,32],[0,460,620,30]].forEach(([x,z,rw,rd])=>mctx.fillRect(x*sx,z*sz,rw*sx,rd*sz));mctx.fillStyle='#5e4b2e';mctx.fillRect(50*sx,440*sz,95*sx,100*sz);mctx.fillStyle='#80683f';mctx.fillRect(450*sx,110*sz,120*sx,120*sz);const e=activeEntity();mctx.fillStyle='#d2a45f';mctx.beginPath();mctx.arc(e.x*sx,e.z*sz,4,0,Math.PI*2);mctx.fill();if(creature.active){mctx.fillStyle=state.captured?'#7ea27b':'#c85c50';mctx.beginPath();mctx.arc(creature.x*sx,creature.z*sz,5,0,Math.PI*2);mctx.fill();}if(!state.mothFound){mctx.fillStyle='#80b7ca';mctx.fillRect(73*sx-2,122*sz-2,4,4);}if(!state.numbererFound){mctx.fillStyle='#aaa89d';mctx.fillRect(106*sx-2,174*sz-2,4,4);}mctx.strokeStyle='#d2a45f';mctx.strokeRect(0,0,w,h);}
function resize(){const dpr=Math.min(2,window.devicePixelRatio||1);canvas.width=Math.floor(innerWidth*dpr);canvas.height=Math.floor(innerHeight*dpr);canvas.style.width=innerWidth+'px';canvas.style.height=innerHeight+'px';projection=M4.perspective(Math.PI/3,canvas.width/canvas.height,.1,500);gl.uniformMatrix4fv(U.uProjection,false,projection);}
window.addEventListener('resize',resize);resize();updateMeters();

// ---------------------------------------------------------------------------
// Verdict and endings.
// ---------------------------------------------------------------------------
function openVerdict(){running=false;document.exitPointerLock?.();$('#verdictModal').classList.remove('hidden');beep(210,.25,.04);}
function verdict(v){state.verdict=v;$('#verdictModal').classList.add('hidden');let title='',text='';
  if(v==='release'){setMeter('stability',state.stability-18);setMeter('trust',state.trust+4);setMeter('leakage',state.leakage+20);title='The city receives a lion and calls it honesty.';text='Trials shorten. Diplomacy freezes. Children learn to say nothing until they understand what they feel. The Crown is useful every day and humane almost never.';}
  if(v==='restrict'){setMeter('stability',state.stability+9);setMeter('trust',state.trust+11);setMeter('leakage',state.leakage+4);title='Glassmarket keeps the teeth behind consent.';text='The Crown is used only where every participant can leave. It still frightens people, but fear is no longer mistaken for proof.';}
  if(v==='molt'){setMeter('stability',state.stability+5);setMeter('trust',state.trust+7);setMeter('leakage',state.leakage+2);title='The Crown returns with shorter teeth.';text='The Molt Surgeons add appeal, visible appetite and an exit. It becomes less impressive, less profitable and far more useful.';}
  if(v==='sanctuary'){setMeter('stability',state.stability+8);setMeter('trust',state.trust-1);setMeter('leakage',state.leakage+7);title='The city visits certainty behind glass.';text='The Crown remains alive in the Predator Vaults. Ministers complain. Schoolchildren understand the warning immediately.';}
  if(v==='destroy'){setMeter('stability',state.stability+2);setMeter('trust',state.trust-5);setMeter('leakage',state.leakage+31);title='The original dies. The description escapes.';text='The White Room dissolves the creature into meaningless syllables. By sunrise, a song about the execution is teaching the city how to hatch copies.';}
  let twist='';if(state.numbererFound){twist='At midnight, THE FUTURE IS ARRIVING strikes the grid. The Grey Numberer closes valve LH-17 and isolates three unstable systems. Low Harbour survives because someone respected a boring idea.';setMeter('stability',state.stability+12);}else{twist='At midnight, THE FUTURE IS ARRIVING strikes the grid. No one recorded valve LH-17. Low Harbour floods while the newspapers debate the Crown.';setMeter('stability',state.stability-17);}
  if(state.mothFound)twist+=' The Compliment Moth spends dawn in the hospital, whispering to people whose names will never enter the official report.';
  if(v==='destroy')twist+=' By noon, children are drawing black lions with transparent skulls.';
  $('#endingTitle').textContent=title;$('#endingText').textContent=text;$('#endingTwist').textContent=twist;$('#endStability').textContent=Math.round(state.stability);$('#endTrust').textContent=Math.round(state.trust);$('#endLeak').textContent=Math.round(state.leakage);let score=state.stability+state.trust-state.leakage-state.wrongTools*5+(state.mothFound?8:0)+(state.numbererFound?14:0);$('#endRank').textContent=score>130?'Civic Naturalist':score>95?'Senior Keeper':score>65?'Field Keeper':'Public Hazard';$('#ending').classList.remove('hidden');beep(330,.4,.04);
}
$$('[data-verdict]').forEach(b=>b.addEventListener('click',()=>verdict(b.dataset.verdict)));

// ---------------------------------------------------------------------------
// Events and main loop.
// ---------------------------------------------------------------------------
$('#startBtn').addEventListener('click',()=>{$('#intro').classList.add('hidden');$('#gameShell').classList.remove('hidden');running=true;last=performance.now();say('CONTROL','Keeper Unit Seven: possible predator manifestation at Open Market. Civilian inventor remains on scene.');requestAnimationFrame(loop);});
$('#soundBtn').addEventListener('click',()=>{soundOn=!soundOn;$('#soundBtn').textContent=soundOn?'♫':'♪';if(soundOn){beep(440,.08,.03);say('GLASSMARKET CIVIC RADIO','The Ministry confirms the panic is under control. Please continue panicking in designated areas.',3500);}});
$('#caseToggle').addEventListener('click',()=>$('#casebook').classList.toggle('hidden'));
$$('.tool-belt button').forEach(b=>b.addEventListener('click',()=>chooseTool(b.dataset.tool)));
window.addEventListener('keydown',e=>{keys[e.code]=true;if(e.code==='KeyE')interact();if(e.code==='Space'){e.preventDefault();useTool();}if(/^Digit[1-4]$/.test(e.code))chooseTool(['praise','money','enemy','silence'][Number(e.code.slice(-1))-1]);if(e.code==='KeyQ'){state.siren=!state.siren;say('KEEPER UNIT SEVEN',state.siren?'Civic siren engaged. Citizens are clearing the road.':'Civic siren disengaged.',1300);}});
window.addEventListener('keyup',e=>keys[e.code]=false);
canvas.addEventListener('click',()=>{if(!matchMedia('(pointer:coarse)').matches)canvas.requestPointerLock?.();});
document.addEventListener('pointerlockchange',()=>mouseCaptured=document.pointerLockElement===canvas);
document.addEventListener('mousemove',e=>{if(mouseCaptured){camYaw-=e.movementX*.0025;camPitch=clamp(camPitch-e.movementY*.002,.12,.72);}});

function bindStick(el,onMove){let id=null,cx=0,cy=0;el.addEventListener('pointerdown',e=>{id=e.pointerId;el.setPointerCapture(id);const r=el.getBoundingClientRect();cx=r.left+r.width/2;cy=r.top+r.height/2;});el.addEventListener('pointermove',e=>{if(e.pointerId!==id)return;const dx=e.clientX-cx,dy=e.clientY-cy,l=Math.max(1,Math.hypot(dx,dy)),m=Math.min(1,l/45);onMove(dx/l*m,dy/l*m);const knob=el.querySelector('i');if(knob)knob.style.transform=`translate(${dx/l*m*32}px,${dy/l*m*32}px)`;});const end=e=>{if(e.pointerId!==id)return;id=null;onMove(0,0);const knob=el.querySelector('i');if(knob)knob.style.transform='';};el.addEventListener('pointerup',end);el.addEventListener('pointercancel',end);}
bindStick($('#moveStick'),(x,y)=>{touch.moveX=x;touch.moveY=y;});
let lookId=null,lx=0,ly=0;$('#lookPad').addEventListener('pointerdown',e=>{lookId=e.pointerId;lx=e.clientX;ly=e.clientY;$('#lookPad').setPointerCapture(lookId);});$('#lookPad').addEventListener('pointermove',e=>{if(e.pointerId!==lookId)return;touch.lookDX+=e.clientX-lx;touch.lookDY+=e.clientY-ly;lx=e.clientX;ly=e.clientY;});$('#lookPad').addEventListener('pointerup',()=>lookId=null);$('#touchInteract').addEventListener('click',interact);$('#touchTool').addEventListener('click',useTool);

window.__zoo3d={state,player,car,creature,interact,useTool,chooseTool,verdict,teleport(x,z,vehicle=false){if(vehicle||player.inCar){car.x=x;car.z=z;player.x=x;player.z=z;}else{player.x=x;player.z=z;}},setCooldown(){state.toolCooldown=0;}};

function loop(now){if(!running)return;const dt=Math.min(.033,(now-last)/1000||.016);last=now;updatePlayer(dt);updateCreature(dt);updateParticles(dt);updatePrompt();updateCamera(dt);render(dt,now/1000);drawMinimap();requestAnimationFrame(loop);}
