'use strict';

const $ = s => document.querySelector(s);
const $$ = s => Array.from(document.querySelectorAll(s));
window.addEventListener('error', e => {
  const box = $('#runtimeError');
  box.classList.remove('hidden');
  box.textContent = `The Zoo encountered an error:\n${e.message}\n${e.filename || ''}:${e.lineno || ''}`;
});

// ---------------------------------------------------------------------------
// Minimal WebGL2 renderer. The world is genuinely 3D and uses no external
// engine or runtime dependency.
// ---------------------------------------------------------------------------
const canvas = $('#glCanvas');
const gl = canvas.getContext('webgl2', { antialias: true, alpha: false });
if (!gl) throw new Error('This browser does not support WebGL2.');

const VS = `#version 300 es
layout(location=0) in vec3 aPosition;
layout(location=1) in vec3 aNormal;
uniform mat4 uProjection;
uniform mat4 uView;
uniform mat4 uModel;
out vec3 vNormal;
out vec3 vWorld;
void main(){
  vec4 world = uModel * vec4(aPosition,1.0);
  vWorld = world.xyz;
  vNormal = normalize(mat3(uModel) * aNormal);
  gl_Position = uProjection * uView * world;
}`;
const FS = `#version 300 es
precision highp float;
in vec3 vNormal;
in vec3 vWorld;
uniform vec3 uColor;
uniform vec3 uLightDir;
uniform vec3 uCamera;
uniform vec3 uFogColor;
uniform float uFogNear;
uniform float uFogFar;
uniform float uEmissive;
uniform float uAlpha;
out vec4 outColor;
void main(){
  float diffuse = max(dot(normalize(vNormal), normalize(-uLightDir)), 0.0);
  float rim = pow(1.0-max(dot(normalize(vNormal),normalize(uCamera-vWorld)),0.0),2.0);
  vec3 lit = uColor * (0.42 + diffuse*0.58) + uColor*rim*0.12 + uColor*uEmissive;
  float d = distance(uCamera,vWorld);
  float fog = smoothstep(uFogNear,uFogFar,d);
  outColor = vec4(mix(lit,uFogColor,fog),uAlpha);
}`;

function compile(type, source){
  const shader = gl.createShader(type); gl.shaderSource(shader,source); gl.compileShader(shader);
  if(!gl.getShaderParameter(shader,gl.COMPILE_STATUS)) throw new Error(gl.getShaderInfoLog(shader));
  return shader;
}
const program = gl.createProgram();
gl.attachShader(program,compile(gl.VERTEX_SHADER,VS));
gl.attachShader(program,compile(gl.FRAGMENT_SHADER,FS));
gl.linkProgram(program);
if(!gl.getProgramParameter(program,gl.LINK_STATUS)) throw new Error(gl.getProgramInfoLog(program));
gl.useProgram(program);
const U = {};
['uProjection','uView','uModel','uColor','uLightDir','uCamera','uFogColor','uFogNear','uFogFar','uEmissive','uAlpha'].forEach(n=>U[n]=gl.getUniformLocation(program,n));

gl.enable(gl.DEPTH_TEST);
gl.enable(gl.CULL_FACE);
gl.cullFace(gl.BACK);
gl.enable(gl.BLEND);
gl.blendFunc(gl.SRC_ALPHA, gl.ONE_MINUS_SRC_ALPHA);

const M4 = {
  identity(){return new Float32Array([1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1]);},
  multiply(a,b){
    const o=new Float32Array(16);
    for(let c=0;c<4;c++) for(let r=0;r<4;r++) o[c*4+r]=a[0*4+r]*b[c*4+0]+a[1*4+r]*b[c*4+1]+a[2*4+r]*b[c*4+2]+a[3*4+r]*b[c*4+3];
    return o;
  },
  translation(x,y,z){const o=this.identity();o[12]=x;o[13]=y;o[14]=z;return o;},
  scale(x,y,z){const o=this.identity();o[0]=x;o[5]=y;o[10]=z;return o;},
  rotX(a){const c=Math.cos(a),s=Math.sin(a),o=this.identity();o[5]=c;o[6]=s;o[9]=-s;o[10]=c;return o;},
  rotY(a){const c=Math.cos(a),s=Math.sin(a),o=this.identity();o[0]=c;o[2]=-s;o[8]=s;o[10]=c;return o;},
  rotZ(a){const c=Math.cos(a),s=Math.sin(a),o=this.identity();o[0]=c;o[1]=s;o[4]=-s;o[5]=c;return o;},
  perspective(fov,aspect,near,far){const f=1/Math.tan(fov/2),nf=1/(near-far),o=new Float32Array(16);o[0]=f/aspect;o[5]=f;o[10]=(far+near)*nf;o[11]=-1;o[14]=2*far*near*nf;return o;},
  lookAt(eye,target,up=[0,1,0]){
    let zx=eye[0]-target[0],zy=eye[1]-target[1],zz=eye[2]-target[2];let l=Math.hypot(zx,zy,zz)||1;zx/=l;zy/=l;zz/=l;
    let xx=up[1]*zz-up[2]*zy,xy=up[2]*zx-up[0]*zz,xz=up[0]*zy-up[1]*zx;l=Math.hypot(xx,xy,xz)||1;xx/=l;xy/=l;xz/=l;
    const yx=zy*xz-zz*xy,yy=zz*xx-zx*xz,yz=zx*xy-zy*xx;
    const o=new Float32Array(16);o[0]=xx;o[1]=yx;o[2]=zx;o[3]=0;o[4]=xy;o[5]=yy;o[6]=zy;o[7]=0;o[8]=xz;o[9]=yz;o[10]=zz;o[11]=0;o[12]=-(xx*eye[0]+xy*eye[1]+xz*eye[2]);o[13]=-(yx*eye[0]+yy*eye[1]+yz*eye[2]);o[14]=-(zx*eye[0]+zy*eye[1]+zz*eye[2]);o[15]=1;return o;
  },
  compose(pos,scale=[1,1,1],rot=[0,0,0]){
    return this.multiply(this.translation(...pos),this.multiply(this.rotY(rot[1]),this.multiply(this.rotX(rot[0]),this.multiply(this.rotZ(rot[2]),this.scale(...scale)))));
  }
};

function mesh(vertices,normals,indices){
  const vao=gl.createVertexArray();gl.bindVertexArray(vao);
  const vb=gl.createBuffer();gl.bindBuffer(gl.ARRAY_BUFFER,vb);gl.bufferData(gl.ARRAY_BUFFER,new Float32Array(vertices),gl.STATIC_DRAW);gl.enableVertexAttribArray(0);gl.vertexAttribPointer(0,3,gl.FLOAT,false,0,0);
  const nb=gl.createBuffer();gl.bindBuffer(gl.ARRAY_BUFFER,nb);gl.bufferData(gl.ARRAY_BUFFER,new Float32Array(normals),gl.STATIC_DRAW);gl.enableVertexAttribArray(1);gl.vertexAttribPointer(1,3,gl.FLOAT,false,0,0);
  const ib=gl.createBuffer();gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER,ib);gl.bufferData(gl.ELEMENT_ARRAY_BUFFER,new Uint16Array(indices),gl.STATIC_DRAW);
  gl.bindVertexArray(null);return{vao,count:indices.length};
}
function cubeMesh(){
  const p=[-1,-1,1,1,-1,1,1,1,1,-1,1,1, 1,-1,-1,-1,-1,-1,-1,1,-1,1,1,-1, -1,1,1,1,1,1,1,1,-1,-1,1,-1, -1,-1,-1,1,-1,-1,1,-1,1,-1,-1,1, 1,-1,1,1,-1,-1,1,1,-1,1,1,1, -1,-1,-1,-1,-1,1,-1,1,1,-1,1,-1];
  const n=[0,0,1,0,0,1,0,0,1,0,0,1, 0,0,-1,0,0,-1,0,0,-1,0,0,-1, 0,1,0,0,1,0,0,1,0,0,1,0, 0,-1,0,0,-1,0,0,-1,0,0,-1,0, 1,0,0,1,0,0,1,0,0,1,0,0, -1,0,0,-1,0,0,-1,0,0,-1,0,0];
  const i=[];for(let f=0;f<6;f++){const b=f*4;i.push(b,b+1,b+2,b,b+2,b+3);}return mesh(p,n,i);
}
function sphereMesh(lat=8,lon=12){const p=[],n=[],i=[];for(let y=0;y<=lat;y++){const v=y/lat,th=v*Math.PI;for(let x=0;x<=lon;x++){const u=x/lon,ph=u*Math.PI*2,s=Math.sin(th);const nx=Math.cos(ph)*s,ny=Math.cos(th),nz=Math.sin(ph)*s;p.push(nx,ny,nz);n.push(nx,ny,nz);}}for(let y=0;y<lat;y++)for(let x=0;x<lon;x++){const a=y*(lon+1)+x,b=a+lon+1;i.push(a,b,a+1,b,b+1,a+1);}return mesh(p,n,i);}
function cylinderMesh(seg=12){const p=[],n=[],i=[];for(let y=0;y<2;y++){const py=y?1:-1;for(let s=0;s<=seg;s++){const a=s/seg*Math.PI*2,x=Math.cos(a),z=Math.sin(a);p.push(x,py,z);n.push(x,0,z);}}for(let s=0;s<seg;s++){const a=s,b=s+seg+1;i.push(a,b,a+1,b,b+1,a+1);}const top=p.length/3;p.push(0,1,0);n.push(0,1,0);const bot=p.length/3;p.push(0,-1,0);n.push(0,-1,0);for(let s=0;s<seg;s++){const a=seg+1+s,b=seg+1+s+1;i.push(top,a,b);i.push(bot,s+1,s);}return mesh(p,n,i);}
function coneMesh(seg=10){const p=[0,1,0],n=[0,1,0],i=[];for(let s=0;s<=seg;s++){const a=s/seg*Math.PI*2,x=Math.cos(a),z=Math.sin(a);p.push(x,-1,z);const l=Math.hypot(x,.7,z);n.push(x/l,.7/l,z/l);}for(let s=0;s<seg;s++)i.push(0,s+1,s+2);const c=p.length/3;p.push(0,-1,0);n.push(0,-1,0);for(let s=0;s<seg;s++)i.push(c,s+2,s+1);return mesh(p,n,i);}
function torusMesh(radSeg=14,tubeSeg=7){const p=[],n=[],i=[],R=1,r=.22;for(let a=0;a<=radSeg;a++){const u=a/radSeg*Math.PI*2,cu=Math.cos(u),su=Math.sin(u);for(let b=0;b<=tubeSeg;b++){const v=b/tubeSeg*Math.PI*2,cv=Math.cos(v),sv=Math.sin(v);p.push((R+r*cv)*cu,r*sv,(R+r*cv)*su);n.push(cv*cu,sv,cv*su);}}for(let a=0;a<radSeg;a++)for(let b=0;b<tubeSeg;b++){const q=a*(tubeSeg+1)+b,w=q+tubeSeg+1;i.push(q,w,q+1,w,w+1,q+1);}return mesh(p,n,i);}
const GEO={cube:cubeMesh(),sphere:sphereMesh(),cylinder:cylinderMesh(),cone:coneMesh(),torus:torusMesh()};
const colorCache=new Map();
function rgb(hex){if(colorCache.has(hex))return colorCache.get(hex);const h=hex.replace('#','');const v=h.length===3?h.split('').map(c=>c+c).join(''):h;const c=[parseInt(v.slice(0,2),16)/255,parseInt(v.slice(2,4),16)/255,parseInt(v.slice(4,6),16)/255];colorCache.set(hex,c);return c;}
let projection=M4.identity(),view=M4.identity(),cameraPos=[0,0,0];
function draw(meshObj,pos,scale,color,rot=[0,0,0],emissive=0,alpha=1){
  const model=M4.compose(pos,scale,rot);gl.uniformMatrix4fv(U.uModel,false,model);gl.uniform3fv(U.uColor,rgb(color));gl.uniform1f(U.uEmissive,emissive);gl.uniform1f(U.uAlpha,alpha);
  gl.depthMask(alpha>=.99);gl.bindVertexArray(meshObj.vao);gl.drawElements(gl.TRIANGLES,meshObj.count,gl.UNSIGNED_SHORT,0);gl.depthMask(true);
}
function local(base,lx,ly,lz){const s=Math.sin(base.yaw),c=Math.cos(base.yaw);return[base.x+lx*c+lz*s,ly,base.z-lx*s+lz*c];}
