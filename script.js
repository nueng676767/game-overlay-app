/* ---------- shared simulated telemetry (demo only, honest per page copy) ---------- */
const STATS = [
  { key:"fps",  label:"FPS",  cls:"v-fps",  min:70,  max:165, val:144, step:6 },
  { key:"cpu",  label:"CPU",  cls:"v-cpu",  min:8,   max:90,  val:34,  step:5, suffix:"%" },
  { key:"gpu",  label:"GPU",  cls:"v-gpu",  min:10,  max:95,  val:58,  step:6, suffix:"%" },
  { key:"temp", label:"TEMP", cls:"v-temp", min:30,  max:70,  val:52,  step:1.4, suffix:"°C" },
  { key:"ram",  label:"RAM",  cls:"v-ram",  min:3,   max:14,  val:7.8, step:.3, decimals:1, suffix:"GB" },
];
const history = {};
STATS.forEach(s => history[s.key] = new Array(20).fill(s.val));

function step(){
  STATS.forEach(s=>{
    let v = s.val + (Math.random()-0.5)*s.step*2;
    if(v<s.min) v = s.min + Math.random()*s.step;
    if(v>s.max) v = s.max - Math.random()*s.step;
    s.val = v;
    const h = history[s.key]; h.push(v); if(h.length>20) h.shift();
  });
}
function fmt(s){ return s.val.toFixed(s.decimals||0); }

/* ---------- hero HUD ---------- */
function buildHud(){
  const mount = document.getElementById("heroHud");
  mount.innerHTML = "";
  STATS.forEach(s=>{
    const el = document.createElement("div");
    el.className = "hud-stat";
    el.dataset.key = s.key;
    el.innerHTML = `
      <div class="label">${s.label}</div>
      <div class="value ${s.cls}"><span class="num">${fmt(s)}</span>${s.suffix?`<sup>${s.suffix}</sup>`:""}</div>
      <canvas width="64" height="16"></canvas>`;
    mount.appendChild(el);
  });
}
function drawSpark(canvas, key, color){
  const ctx = canvas.getContext("2d");
  const w=canvas.width, h=canvas.height;
  ctx.clearRect(0,0,w,h);
  const data = history[key];
  const min=Math.min(...data), max=Math.max(...data), range=(max-min)||1;
  ctx.beginPath();
  data.forEach((v,i)=>{
    const x=(i/(data.length-1))*w, y=h-((v-min)/range)*h;
    i===0?ctx.moveTo(x,y):ctx.lineTo(x,y);
  });
  ctx.strokeStyle=color; ctx.lineWidth=1.4; ctx.stroke();
}
const lineColor = {fps:"#22d3ee",cpu:"#22c55e",gpu:"#f59e0b",temp:"#ef4444",ram:"#3b82f6"};

function refreshHud(){
  STATS.forEach(s=>{
    const el = document.querySelector(`.hud-stat[data-key="${s.key}"]`);
    if(!el) return;
    el.querySelector(".num").textContent = fmt(s);
    drawSpark(el.querySelector("canvas"), s.key, lineColor[s.key]);
  });
}

/* ---------- ticker ---------- */
function buildTicker(){
  const track = document.getElementById("tickerTrack");
  const renderSet = ()=> STATS.map(s=>
    `<span class="ticker-item">${s.label} <b>${fmt(s)}${s.suffix||""}</b></span>`
  ).join("");
  // duplicate content for a seamless scrolling loop
  track.innerHTML = renderSet() + renderSet() + renderSet() + renderSet();
}
function refreshTicker(){
  const spans = document.querySelectorAll(".ticker-item b");
  spans.forEach((el,i)=>{
    const s = STATS[i % STATS.length];
    el.textContent = fmt(s) + (s.suffix||"");
  });
}

let tickerX = 0;
function animateTicker(){
  const track = document.getElementById("tickerTrack");
  tickerX -= 0.6;
  if(Math.abs(tickerX) > track.scrollWidth/4) tickerX = 0;
  track.style.transform = `translateX(${tickerX}px)`;
  requestAnimationFrame(animateTicker);
}

/* ---------- boot ---------- */
buildHud();
buildTicker();
requestAnimationFrame(animateTicker);
setInterval(()=>{ step(); refreshHud(); refreshTicker(); }, 500);

/* ---------- cursor spotlight ---------- */
const spot = document.getElementById("spotlight");
window.addEventListener("pointermove", e=>{
  spot.style.setProperty("--sx", e.clientX+"px");
  spot.style.setProperty("--sy", e.clientY+"px");
});

/* ---------- smooth focus ring only for keyboard users ---------- */
document.body.addEventListener("mousedown", ()=>document.body.classList.add("using-mouse"));
document.body.addEventListener("keydown", e=>{ if(e.key==="Tab") document.body.classList.remove("using-mouse"); });
