// ---------------------------------------------------------------------------
// Game state and world.
// ---------------------------------------------------------------------------
const WORLD={w:620,d:620};
const keys={};let running=false,last=0,soundOn=false,audio=null,radioTimer=null,promptAction=null,mouseCaptured=false;
let camYaw=-.25,camPitch=.31,camDistance=11;
const touch={moveX:0,moveY:0,lookDX:0,lookDY:0};
const state={mission:0,selectedTool:'silence',clues:new Set(),captured:false,cageAttached:false,returned:false,mothFound:false,numbererFound:false,stability:74,trust:61,leakage:9,agitation:38,calm:0,wrongTools:0,toolCooldown:0,hitCooldown:0,capturePulse:0,siren:false,notices:0};
const player={x:125,z:590,yaw:Math.PI,r:1.2,speed:0,inCar:false};
const car={x:145,z:566,yaw:Math.PI,speed:0,r:3.5,occupied:false};
const creature={x:505,z:168,yaw:0,r:3.2,targetX:510,targetZ:170,active:true,stunned:0,attackFlash:0};
const zooGate={x:125,z:475};
const marketCenter={x:505,z:170};
const collisionRects=[];const buildings=[];const npcs=[];const traffic=[];const particles=[];
const interactables=[
{id:'inventor',x:482,z:184,type:'npc',fact:'first',label:'Minister Vale, inventor',text:'“It only growls at lies,” the minister insists. The lion studies the keys on his belt before it studies his face.',speaker:'APPETITE READER',radio:'First action noted: it studies access and authority before speech. This is not merely a truth tool. It is a power animal.'},
{id:'child',x:535,z:145,type:'npc',fact:'consent',label:'Frightened child',text:'“It growled when I said I was fine. I did not want everyone to know I was scared.”',speaker:'CHILDREN’S JURY',radio:'Who gets hurt? Anyone who needs privacy before they are ready to speak. Can they say no? Not while the lion is loose.'},
{id:'clerk',x:530,z:205,type:'npc',fact:'counter',label:'Ministry clerk',text:'A fanatic praised the minister beside the creature. It stayed silent. The man believed every word.',speaker:'COUNTERFACTUAL VET',radio:'Confirmed. It detects conflict between speech and belief—not falsehood. Certainty is camouflage.'},
{id:'kiosk',x:463,z:142,type:'kiosk',fact:'appetite',label:'Broken public-truth kiosk',text:'Every public speaker nearby has stopped talking. The lion feeds on forced confession, certainty and spectacle.',speaker:'WEATHER WARDEN',radio:'Primary appetite: certainty. Secondary appetite: public attention. Do not give it an enemy. Do not let the crowd applaud.'},
{id:'moth',x:75,z:124,type:'moth',special:'moth',label:'Blue compliment moth',text:'It lands beside a patient outside the harbour clinic. “You came even though you were afraid,” it whispers.'},
{id:'numberer',x:108,z:176,type:'numberer',special:'numberer',label:'The Grey Numberer',text:'A squat filing-cabinet animal tags a rusting flood valve: LH-17 / last serviced eleven years ago.'},
{id:'notice1',x:320,z:292,type:'sign',special:'notice',label:'Public notice',text:'MINISTRY NOTICE: The panic is under control. Please continue panicking in designated areas.'},
{id:'notice2',x:412,z:320,type:'sign',special:'notice',label:'Public notice',text:'UNLICENSED METAPHORS MUST BE SURRENDERED BY THURSDAY.'}
];

function seedRandom(seed){let s=seed>>>0;return()=>{s=(s+0x6D2B79F5)|0;let t=Math.imul(s^s>>>15,1|s);t=t+Math.imul(t^t>>>7,61|t)^t;return((t^t>>>14)>>>0)/4294967296;};}
const rnd=seedRandom(24071);
function addBox(x,z,w,d,h,color='#27343b',name='building'){
  buildings.push({x,z,w,d,h,color,name});collisionRects.push({x:x-w/2,z:z-d/2,w,d});
}
function road(x,z,w,d){buildings.push({x,z,w,d,h:.08,color:'#111a1f',road:true});}
function setupWorld(){
  road(310,300,620,46);road(250,310,42,620);road(420,310,36,620);road(310,160,620,32);road(310,475,620,30);
  for(let x=8;x<620;x+=18){buildings.push({x,z:300,w:9,d:.35,h:.12,color:'#8c774c',line:true});buildings.push({x,z:160,w:8,d:.28,h:.12,color:'#8c774c',line:true});}
  for(let z=8;z<620;z+=18){buildings.push({x:250,z,w:.35,d:9,h:.12,color:'#8c774c',line:true});buildings.push({x:420,z,w:.28,d:8,h:.12,color:'#8c774c',line:true});}
  addBox(78,500,65,45,20,'#203638','THE IDEA ZOO');addBox(78,456,38,22,10,'#263b3b','Hatchery');addBox(30,480,25,65,12,'#192b2e','Predator Vault');
  buildings.push({x:105,z:535,w:32,d:32,h:1.2,color:'#755d38',domeBase:true});
  addBox(470,105,65,34,16,'#3a3024','Market Arcade');addBox(548,105,55,35,22,'#30291f','Market Tower');addBox(570,220,48,55,18,'#352a20','Ministry Annex');addBox(450,225,42,48,14,'#2e2822','Truth Kiosk Offices');
  addBox(65,175,48,38,11,'#1c3037','Clinic');addBox(150,125,55,34,15,'#1b2c32','Cold Storage');addBox(150,205,50,40,10,'#223238','Valve House');
  addBox(315,85,45,48,36,'#263947','High Glass 1');addBox(365,90,35,45,50,'#293d4b','High Glass 2');addBox(300,205,42,42,29,'#22333e','High Glass 3');addBox(360,215,38,42,41,'#2b3b45','High Glass 4');
  addBox(485,420,65,45,16,'#292a33','School Hall');addBox(555,455,48,62,25,'#262934','Archive School');addBox(465,535,45,38,20,'#252831','Dormitory');addBox(550,550,62,32,13,'#2d2b30','Gymnasium');
  const zones=[{x0:15,x1:220,z0:325,z1:445},{x0:270,x1:400,z0:325,z1:450},{x0:270,x1:400,z0:185,z1:280},{x0:435,x1:600,z0:335,z1:395},{x0:435,x1:600,z0:245,z1:280},{x0:15,x1:220,z0:15,z1:95}];
  zones.forEach((q,qi)=>{for(let x=q.x0;x<q.x1;x+=42)for(let z=q.z0;z<q.z1;z+=42){if(rnd()<.16)continue;const w=24+rnd()*10,d=24+rnd()*10,h=9+rnd()*30;addBox(x+rnd()*6,z+rnd()*6,w,d,h,['#24323a','#2a3539','#33332f','#26343c'][qi%4]);}});
  for(let i=0;i<38;i++)npcs.push({x:440+rnd()*130,z:120+rnd()*110,yaw:rnd()*Math.PI*2,speed:.6+rnd()*.7,color:['#a67b62','#8d6b55','#c3926a','#6f5447','#b78f72'][i%5],panic:rnd()});
  for(let i=0;i<18;i++)npcs.push({x:30+rnd()*175,z:105+rnd()*120,yaw:rnd()*Math.PI*2,speed:.4+rnd()*.6,color:['#a67b62','#8d6b55','#c3926a','#6f5447','#b78f72'][i%5],panic:0});
  for(let i=0;i<12;i++)traffic.push({axis:i%2?'z':'x',x:i%2?(i%4===1?246:424):rnd()*620,z:i%2?rnd()*620:(i%4===0?294:306),dir:i%3===0?-1:1,speed:8+rnd()*7,color:['#52646d','#7b5c49','#3d5059','#6e6750'][i%4]});
}
setupWorld();

function clamp(v,a=0,b=100){return Math.max(a,Math.min(b,v));}
function distance(a,b){return Math.hypot(a.x-b.x,a.z-b.z);}
function collides(x,z,r){if(x<r||z<r||x>WORLD.w-r||z>WORLD.d-r)return true;return collisionRects.some(o=>x+r>o.x&&x-r<o.x+o.w&&z+r>o.z&&z-r<o.z+o.d);}
function moveCircle(obj,nx,nz,r){if(!collides(nx,obj.z,r))obj.x=nx;if(!collides(obj.x,nz,r))obj.z=nz;}
function addLog(text){const box=$('#interactionLog'),p=document.createElement('p');p.textContent=text;box.prepend(p);while(box.children.length>5)box.removeChild(box.lastChild);}
function setMission(tag,title,text){$('#missionTag').textContent=tag;$('#missionTitle').textContent=title;$('#missionText').textContent=text;}
function setMeter(name,v){state[name]=clamp(v);const ids={stability:'stability',trust:'trust',leakage:'leak',agitation:'agitation'};const id=ids[name];$('#'+id+'Val').textContent=Math.round(state[name]);$('#'+id+'Bar').style.width=state[name]+'%';}
function updateMeters(){setMeter('stability',state.stability);setMeter('trust',state.trust);setMeter('leakage',state.leakage);setMeter('agitation',state.agitation);}
function beep(freq=440,d=.08,g=.025){if(!soundOn)return;if(!audio)audio=new (window.AudioContext||window.webkitAudioContext)();const o=audio.createOscillator(),gain=audio.createGain();o.type='triangle';o.frequency.value=freq;gain.gain.value=g;o.connect(gain);gain.connect(audio.destination);o.start();gain.gain.exponentialRampToValueAtTime(.0001,audio.currentTime+d);o.stop(audio.currentTime+d);}
function say(speaker,text,duration=5200){$('#radioSpeaker').textContent=speaker;$('#radioText').textContent=text;$('#radio').classList.remove('hidden');clearTimeout(radioTimer);radioTimer=setTimeout(()=>$('#radio').classList.add('hidden'),duration);beep(680,.05,.025);addLog(`${speaker}: ${text}`);}
function unlockFact(fact,text){const el=$(`.fact[data-fact="${fact}"]`);if(!el)return;el.classList.remove('locked');el.querySelector('span').textContent=text;}
function chooseTool(tool){state.selectedTool=tool;$$('.tool-belt button').forEach(b=>b.classList.toggle('selected',b.dataset.tool===tool));beep({praise:520,money:620,enemy:220,silence:390}[tool],.06,.02);}

function drawGround(){
  draw(GEO.cube,[310,-1.3,310],[310,1,310],'#102128');
  draw(GEO.cube,[92,-.1,42],[92,.08,42],'#12333f',[0,0,0],.25);
  for(let i=0;i<8;i++)draw(GEO.cube,[20+i*22,.15,77],[8,.18,35],'#2c3434');
  buildings.forEach(b=>{
    if(b.domeBase){draw(GEO.cylinder,[b.x,b.h/2,b.z],[b.w/2,b.h/2,b.d/2],b.color);draw(GEO.torus,[b.x,8,b.z],[16,16,16],'#8aa8a3',[Math.PI/2,0,0],.18,.45);return;}
    draw(GEO.cube,[b.x,b.h/2,b.z],[b.w/2,b.h/2,b.d/2],b.color,[0,0,0],b.line?.35:0);
    if(!b.road&&!b.line&&b.h>4){
      draw(GEO.cube,[b.x,b.h+.22,b.z],[b.w*.46,.22,b.d*.46],'#121d22');
      const rows=Math.min(5,Math.floor(b.h/6));for(let r=0;r<rows;r++){const y=4+r*5;if(y>b.h-2)break;draw(GEO.cube,[b.x,b.y,b.z+b.d/2+.03],[b.w*.27,.55,.05],'#d8b66a',[0,0,0],.45);}
    }
  });
  draw(GEO.cube,[111,4.5,475],[2,4.5,2],'#7d6339',[0,0,0],.15);draw(GEO.cube,[139,4.5,475],[2,4.5,2],'#7d6339',[0,0,0],.15);draw(GEO.cube,[125,8.6,475],[16,1,1.5],'#342a1d');
  draw(GEO.cylinder,[505,.12,168],[34,.12,34],'#332c23',[0,0,0],.05);
  for(let i=0;i<4;i++)draw(GEO.torus,[505,.3+i*.02,168],[10+i*7,.12,10+i*7],'#7b6743',[0,0,0],.1,.35);
}
function drawHuman(x,z,yaw,color='#9b765e',uniform='#33454e',scale=1){
  const base={x,z,yaw};draw(GEO.cylinder,local(base,0,1.45*scale,0),[.48*scale,.8*scale,.48*scale],uniform,[0,yaw,0]);draw(GEO.sphere,local(base,0,2.72*scale,0),[.48*scale,.48*scale,.48*scale],color);
  draw(GEO.cylinder,local(base,-.3,.48*scale,.05),[.14*scale,.55*scale,.14*scale],'#28363d');draw(GEO.cylinder,local(base,.3,.48*scale,.05),[.14*scale,.55*scale,.14*scale],'#28363d');
  draw(GEO.cylinder,local(base,-.67,1.55*scale,0),[.11*scale,.6*scale,.11*scale],color,[0,yaw,Math.PI/9]);draw(GEO.cylinder,local(base,.67,1.55*scale,0),[.11*scale,.6*scale,.11*scale],color,[0,yaw,-Math.PI/9]);
}
function drawPlayer(){if(player.inCar)return;drawHuman(player.x,player.z,player.yaw,'#8f654f','#a88243',1.05);}
function drawCarModel(obj,ambient=false){
  const b={x:obj.x,z:obj.z,yaw:obj.yaw};draw(GEO.cube,local(b,0,1.15,0),[2.8,.7,5.2],ambient?obj.color:'#a94139',[0,obj.yaw,0]);draw(GEO.cube,local(b,0,2.2,-.4),[2.2,.75,2.4],ambient?'#35454d':'#26363e',[0,obj.yaw,0],.1);
  draw(GEO.cube,local(b,0,1.3,-5.1),[1.5,.22,.15],'#e8c985',[0,obj.yaw,0],.8);draw(GEO.cube,local(b,0,1.3,5.1),[1.6,.22,.15],'#8f2424',[0,obj.yaw,0],.35);
  [[-2.8,-3.2],[2.8,-3.2],[-2.8,3.2],[2.8,3.2]].forEach(([x,z])=>draw(GEO.cylinder,local(b,x,.65,z),[.78,.35,.78],'#111519',[0,obj.yaw,Math.PI/2]));
  if(!ambient){draw(GEO.cube,local(b,0,2.7,-2.2),[1.8,.08,.8],state.siren?'#c85c50':'#22333a',[0,obj.yaw,0],state.siren?.8:0);draw(GEO.cube,local(b,0,2.85,-2.2),[.7,.08,.8],state.siren?'#6aa3c7':'#22333a',[0,obj.yaw,0],state.siren?.8:0);}
  if(state.cageAttached&&!ambient){
    draw(GEO.cube,local(b,0,3.2,3.1),[2.25,1.65,1.7],'#1d2c31',[0,obj.yaw,0],.05,.28);for(let x=-2;x<=2;x+=1)draw(GEO.cube,local(b,x,3.2,4.75),[.05,1.6,.05],'#d2a45f',[0,obj.yaw,0],.25);drawCreature(obj.x,obj.z,obj.yaw,true,local(b,0,3.25,3.2));
  }
}
function drawCreature(x=creature.x,z=creature.z,yaw=creature.yaw,mini=false,override=null){
  if(!creature.active&&!mini)return;const b={x:override?override[0]:x,z:override?override[2]:z,yaw};const y0=override?override[1]-1.4:0;const s=mini?.46:1;
  draw(GEO.sphere,local(b,0,y0+2.3*s,0),[2.4*s,1.35*s,1.25*s],'#4e5350',[0,yaw,0]);draw(GEO.sphere,local(b,0,y0+2.65*s,-2.1*s),[1.35*s,1.2*s,1.2*s],'#5c5f58',[0,yaw,0]);draw(GEO.sphere,local(b,0,y0+2.7*s,-2.18*s),[1.75*s,1.55*s,1.55*s],creature.attackFlash>0?'#8d423d':'#6c443c',[0,yaw,0],.08);
  [[-1.25,-.9],[1.25,-.9],[-1.25,1.1],[1.25,1.1]].forEach(([lx,lz])=>draw(GEO.cylinder,local(b,lx,y0+.85*s,lz),[.28*s,.9*s,.28*s],'#343b39',[0,yaw,0]));
  draw(GEO.cone,local(b,-.55,y0+4.35*s,-2.25*s),[.34*s,.9*s,.34*s],'#d2a45f',[0,yaw,0],.6);draw(GEO.cone,local(b,0,y0+4.58*s,-2.3*s),[.38*s,1.05*s,.38*s],'#d2a45f',[0,yaw,0],.75);draw(GEO.cone,local(b,.55,y0+4.35*s,-2.25*s),[.34*s,.9*s,.34*s],'#d2a45f',[0,yaw,0],.6);
  draw(GEO.sphere,local(b,-.43,y0+2.88*s,-3.18*s),[.13*s,.13*s,.13*s],'#d9c37c',[0,yaw,0],1);draw(GEO.sphere,local(b,.43,y0+2.88*s,-3.18*s),[.13*s,.13*s,.13*s],'#d9c37c',[0,yaw,0],1);
  if(!mini&&state.capturePulse>0){const a=.25+.35*Math.sin(state.capturePulse*6);for(let i=0;i<3;i++)draw(GEO.torus,[b.x,.5+i*1.65,b.z],[4.4-i*.45,4.4-i*.45,4.4-i*.45],'#d2a45f',[Math.PI/2,0,0],.7,a);}
}
function drawInteractable(o){
  if(o.special==='moth'){if(state.mothFound)return;draw(GEO.sphere,[o.x,1.7,o.z],[.22,.35,.22],'#7fb6ca',[0,0,0],.9);draw(GEO.sphere,[o.x-.35,1.8,o.z],[.42,.08,.58],'#75aabd',[0,0,.45],.6,.75);draw(GEO.sphere,[o.x+.35,1.8,o.z],[.42,.08,.58],'#75aabd',[0,0,-.45],.6,.75);return;}
  if(o.special==='numberer'){if(state.numbererFound)return;draw(GEO.cube,[o.x,1.3,o.z],[1.25,1.3,1.1],'#73746e');draw(GEO.sphere,[o.x,2.55,o.z-1.0],[.8,.6,.65],'#77766e');draw(GEO.cube,[o.x,1.45,o.z-1.15],[.85,.15,.15],'#d2a45f',[0,0,0],.5);return;}
  if(o.type==='npc'){drawHuman(o.x,o.z,0,o.id==='child'?'#8c604c':'#a2765e',o.id==='inventor'?'#5a3c47':'#36454e',o.id==='child'?.82:1);}
  if(o.type==='kiosk'){draw(GEO.cube,[o.x,1.6,o.z],[1.4,1.6,.8],'#263840');draw(GEO.cube,[o.x,2.1,o.z-.82],[1,.75,.06],'#8daaaa',[0,0,0],.45);}
  if(o.type==='sign'){draw(GEO.cylinder,[o.x,1.2,o.z],[.12,1.2,.12],'#8a7147');draw(GEO.cube,[o.x,2.7,o.z],[1.4,.7,.12],'#4c3c28',[0,0,0],.1);}
  if(!o.special&&!state.clues.has(o.fact))draw(GEO.sphere,[o.x,4.4,o.z],[.28,.28,.28],'#d2a45f',[0,0,0],1);
}
function drawNPCs(dt){npcs.forEach(n=>{const threat=distance(n,creature)<20&&state.agitation>55;if(threat){const a=Math.atan2(n.x-creature.x,n.z-creature.z);n.x+=Math.sin(a)*n.speed*dt*2;n.z+=Math.cos(a)*n.speed*dt*2;}else{n.x+=Math.sin(n.yaw)*n.speed*dt;n.z+=Math.cos(n.yaw)*n.speed*dt;if(Math.random()<.005)n.yaw+=(-1+Math.random()*2)*1.5;}if(collides(n.x,n.z,.5)){n.yaw+=Math.PI*.7;n.x+=Math.sin(n.yaw);n.z+=Math.cos(n.yaw);}drawHuman(n.x,n.z,n.yaw,n.color,'#35434a',.72);});}
function drawTraffic(dt){traffic.forEach(t=>{if(t.axis==='x'){t.x+=t.speed*t.dir*dt;if(t.x<-10)t.x=630;if(t.x>630)t.x=-10;drawCarModel({x:t.x,z:t.z,yaw:t.dir>0?Math.PI/2:-Math.PI/2,color:t.color},true);}else{t.z+=t.speed*t.dir*dt;if(t.z<-10)t.z=630;if(t.z>630)t.z=-10;drawCarModel({x:t.x,z:t.z,yaw:t.dir>0?0:Math.PI,color:t.color},true);}});}
function drawSkyObjects(time){
  for(let i=0;i<7;i++){const x=(time*2+i*93)%760-70,z=50+i*78;draw(GEO.sphere,[x,48+Math.sin(time*.2+i)*4,z],[18+i%3*5,4,8],'#6f8792',[0,0,0],.05,.2);}
}
