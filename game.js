(() => {
  const parts = ['game.part00.txt','game.part01.txt','game.part02.txt','game.part03.txt','game.part04.txt'];
  Promise.all(parts.map(path => fetch(path).then(response => {
    if (!response.ok) throw new Error(`Failed to load ${path}`);
    return response.text();
  }))).then(source => {
    const script = document.createElement('script');
    script.textContent = source.join('');
    document.body.appendChild(script);
  }).catch(error => {
    const panel = document.createElement('div');
    panel.style.cssText = 'position:fixed;inset:20px;z-index:999;background:#250d0d;color:white;padding:24px;font:16px monospace;overflow:auto';
    panel.textContent = 'The Zoo gates failed to open: ' + error.message;
    document.body.appendChild(panel);
  });
})();
