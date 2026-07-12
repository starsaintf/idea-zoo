// ---------------------------------------------------------------------------
// Input and simulation.
// ---------------------------------------------------------------------------
function activeEntity(){return player.inCar?car:player;}
function nearestInteraction(){
  const e=activeEntity();let best=null,bd=Infinity;
  if(!player.inCar&&distance(player,car)<5)best={kind:'car',label:'Enter Keeper Unit Seven'},bd=distance(player,car);
  if(player.inCar&&Math.abs(car.speed)<3)best={kind:'exit',label:'Exit vehicle'},bd=0;
  interactables.forEach(o=>{if((o.special==='moth'&&state.mothFound)||(o.special==='numberer'&&state.numbererFound)||(o.fact&&state.clues.has(o.fact)))return;const d=distance(e,o);if(d<(player.inCar?7:4.5)&&d<bd){best={kind:'object',object:o,label:o.label};bd=d;}});
  if(state.captured&&!state.cageAttached&&player.inCar&&distance(car,creature)<8)best={kind:'load',label:'Load containment cage'},bd=0;
  if(state.cageAttached&&player.inCar&&distance(car,zooGate)<11)best={kind:'hearing',label:'Enter the Zoo hearing'},bd=0;
  return best;
}
function interact(){
  const n=nearestInteraction();if(!n)return;
  if(n.kind==='car'){player.inCar=true;car.occupied=true;player.x=car.x;player.z=car.z;setMission('FIELD UNIT','Drive to Open Market','Follow the eastern road. Possible Mirror–Teeth manifestation.');say('CONTROL','Keeper Unit Seven, you are cleared through the Whisper Gates. Bring it back alive if the useful core can be separated.');beep(380,.1,.03);return;}
  if(n.kind==='exit'){player.inCar=false;car.occupied=false;car.speed=0;const p=local(car,4,0,0);player.x=p[0];player.z=p[2];player.yaw=car.yaw;return;}
  if(n.kind==='load'){state.cageAttached=true;creature.active=false;setMission('RETURN','Transport the Crown to the Idea Zoo','Do not stop for crowds. The creature still hears every public claim.');say('RELEASE SHEPHERD','Cage locked. Return through the eastern Whisper Gate. Do not let the press approach the vehicle.');return;}
  if(n.kind==='hearing'){openVerdict();return;}
  const o=n.object;addLog(o.text);say(o.speaker||'FIELD NOTE',o.radio||o.text,5800);
  if(o.fact){state.clues.add(o.fact);const texts={first:'It studies keys and authority before speech.',appetite:'Certainty, forced confession and spectacle.',counter:'True belief can camouflage falsehood.',consent:'It exposes fear people did not consent to reveal.'};unlockFact(o.fact,texts[o.fact]);if(state.clues.size>=2&&state.mission<2){state.mission=2;setMission('INVESTIGATION','Complete the classification','Interview the remaining witnesses before attempting containment.');}if(state.clues.size===4){$('#classText').textContent='MIRROR–TEETH HYBRID';setMission('CONTAINMENT','Calm The Honest Crown','Use Silence near the creature. Praise, money or enemies will feed it.');say('COUNTERFACTUAL VET','Classification confirmed: Mirror–Teeth hybrid. Useful diagnostic behaviour. Predatory social pressure. Feed it silence.');}}
  if(o.special==='moth'){state.mothFound=true;setMeter('trust',state.trust+5);say('CHILDREN’S JURY','A thing can fail at its job and still succeed at being alive. The moth is cleared for the harbour clinic.');}
  if(o.special==='numberer'){state.numbererFound=true;setMeter('stability',state.stability+5);say('PUBLIC WORKS','Valve LH-17 tagged and isolated. Nobody will write a song about it. The harbour may survive because of it.');}
  if(o.special==='notice'){state.notices++;setMeter('leakage',state.leakage+2);}
}
function useTool(){
  if(state.toolCooldown>0||state.captured)return;const e=activeEntity();if(distance(e,creature)>18){say('FIELD UNIT','Tool has no target in range.',1800);return;}state.toolCooldown=1.1;const t=state.selectedTool;particles.push({x:creature.x,z:creature.z,life:1.2,color:{praise:'#e9c86f',money:'#71b77c',enemy:'#c85c50',silence:'#83b7c8'}[t]});
  if(t==='silence'){
    const knowledge=state.clues.size/4;state.calm+=22+knowledge*18;setMeter('agitation',state.agitation-(14+knowledge*12));say('APPETITE READER',knowledge>.7?'The appetite is dropping. Hold the quiet around it.':'It is slowing, but you are treating behaviour before understanding appetite.',2600);state.capturePulse=1;
    if(state.calm>=92&&state.agitation<=40){state.captured=true;state.capturePulse=6;creature.stunned=999;setMission('CAPTURE COMPLETE','Bring Keeper Unit Seven to the cage','Enter the vehicle, approach the field and load the specimen.');say('CONTROL','Containment seal confirmed. The creature is alive. Now comes the harder part: deciding whether it should remain so.');setMeter('stability',state.stability+6);}
  }else{
    state.wrongTools++;const effects={praise:[19,5,4,'Admiration feels like permission. Its mane expands.'],money:[14,2,8,'It learns the difference between prey and customer.'],enemy:[30,-7,10,'It finally looks happy. Every nearby face becomes evidence.']};const [ag,tr,le,msg]=effects[t];setMeter('agitation',state.agitation+ag);setMeter('trust',state.trust+tr);setMeter('leakage',state.leakage+le);state.calm=Math.max(0,state.calm-25);say('WEATHER WARDEN',msg,3300);creature.attackFlash=1.1;
  }
}
function updatePlayer(dt){
  const forward=(keys.KeyW||keys.ArrowUp?1:0)-(keys.KeyS||keys.ArrowDown?1:0)-touch.moveY;const strafe=(keys.KeyD||keys.ArrowRight?1:0)-(keys.KeyA||keys.ArrowLeft?1:0)+touch.moveX;
  if(player.inCar){
    const accel=forward*22;car.speed+=accel*dt;car.speed*=Math.pow(.37,dt);car.speed=clamp(car.speed,-12,28);const steer=strafe*(.75+Math.min(1,Math.abs(car.speed)/10));if(Math.abs(car.speed)>.4)car.yaw+=steer*dt*(car.speed>=0?1:-1);const nx=car.x+Math.sin(car.yaw)*car.speed*dt,nz=car.z+Math.cos(car.yaw)*car.speed*dt;if(collides(nx,nz,car.r)){car.speed*=-.2;setMeter('stability',state.stability-.6);}else{car.x=nx;car.z=nz;}player.x=car.x;player.z=car.z;player.yaw=car.yaw;if(state.siren&&soundOn&&Math.floor(performance.now()/500)%2===0)beep(780,.05,.012);
  }else{
    let mx=0,mz=0;if(forward||strafe){const fx=-Math.sin(camYaw),fz=-Math.cos(camYaw),rx=Math.cos(camYaw),rz=-Math.sin(camYaw);mx=fx*forward+rx*strafe;mz=fz*forward+rz*strafe;const l=Math.hypot(mx,mz)||1;mx/=l;mz/=l;player.yaw=Math.atan2(mx,mz);moveCircle(player,player.x+mx*8.8*dt,player.z+mz*8.8*dt,player.r);}
  }
  if(state.mission===1&&distance(activeEntity(),marketCenter)<50){state.mission=2;setMission('OPEN MARKET','Investigate The Honest Crown','Question the inventor, child, clerk and inspect the truth kiosk.');say('CONTROL','Open Market visual acquired. Crowd is self-silencing. Do not confuse quiet with safety.');}
}
function updateCreature(dt){
  if(!creature.active||state.captured)return;if(creature.attackFlash>0)creature.attackFlash-=dt;if(creature.stunned>0){creature.stunned-=dt;return;}
  const e=activeEntity(),d=distance(e,creature);let speed=1.8;
  if(state.agitation>58&&d<55){const a=Math.atan2(e.x-creature.x,e.z-creature.z);creature.yaw=a;speed=3.7+state.agitation*.025;creature.targetX=e.x;creature.targetZ=e.z;}else if(distance(creature,{x:creature.targetX,z:creature.targetZ})<3){creature.targetX=475+rnd()*65;creature.targetZ=135+rnd()*65;}
  const a=Math.atan2(creature.targetX-creature.x,creature.targetZ-creature.z);creature.yaw=a;const nx=creature.x+Math.sin(a)*speed*dt,nz=creature.z+Math.cos(a)*speed*dt;if(!collides(nx,nz,creature.r)){creature.x=nx;creature.z=nz;}else{creature.targetX=475+rnd()*65;creature.targetZ=135+rnd()*65;}
  if(d<(player.inCar?6:3.7)&&state.hitCooldown<=0&&state.agitation>55){state.hitCooldown=2.3;setMeter('stability',state.stability-6);setMeter('trust',state.trust-3);creature.attackFlash=1.2;say('CONTROL','Impact registered. The Crown is converting resistance into spectacle.',2500);if(player.inCar)car.speed*=-.55;else{const push=Math.atan2(player.x-creature.x,player.z-creature.z);moveCircle(player,player.x+Math.sin(push)*5,player.z+Math.cos(push)*5,player.r);}}
}
function updateParticles(dt){state.toolCooldown=Math.max(0,state.toolCooldown-dt);state.hitCooldown=Math.max(0,state.hitCooldown-dt);if(state.capturePulse>0)state.capturePulse+=dt;for(let i=particles.length-1;i>=0;i--){particles[i].life-=dt;if(particles[i].life<=0)particles.splice(i,1);}}
function updatePrompt(){const n=nearestInteraction();if(n){$('#prompt').textContent=`E · ${n.label}`;$('#prompt').classList.remove('hidden');promptAction=n;}else{$('#prompt').classList.add('hidden');promptAction=null;}}
function updateCamera(dt){
  camYaw+=touch.lookDX*.007;camPitch=clamp(camPitch+touch.lookDY*.004,.12,.72);touch.lookDX*=.6;touch.lookDY*=.6;
  const target=player.inCar?[car.x,2.1,car.z]:[player.x,2.2,player.z];camDistance=player.inCar?15:10;const horiz=camDistance*Math.cos(camPitch);cameraPos=[target[0]+Math.sin(camYaw)*horiz,target[1]+Math.sin(camPitch)*camDistance+2,target[2]+Math.cos(camYaw)*horiz];view=M4.lookAt(cameraPos,target);
  gl.uniformMatrix4fv(U.uView,false,view);gl.uniform3fv(U.uCamera,cameraPos);
}
function render(dt,time){
  gl.viewport(0,0,canvas.width,canvas.height);gl.clearColor(.035,.09,.115,1);gl.clear(gl.COLOR_BUFFER_BIT|gl.DEPTH_BUFFER_BIT);gl.useProgram(program);gl.uniformMatrix4fv(U.uProjection,false,projection);gl.uniformMatrix4fv(U.uView,false,view);gl.uniform3fv(U.uLightDir,new Float32Array([-.45,-1,.25]));gl.uniform3fv(U.uCamera,new Float32Array(cameraPos));gl.uniform3fv(U.uFogColor,new Float32Array([.04,.10,.125]));gl.uniform1f(U.uFogNear,45);gl.uniform1f(U.uFogFar,190);
  drawGround();drawSkyObjects(time);drawTraffic(dt);drawNPCs(dt);interactables.forEach(drawInteractable);drawPlayer();drawCarModel(car,false);drawCreature();
  particles.forEach((p,i)=>{const k=p.life;draw(GEO.torus,[p.x,1+(1-k)*3,p.z],[3+(1-k)*8,.28,3+(1-k)*8],p.color,[Math.PI/2,0,0],.8,k*.7);});
}
