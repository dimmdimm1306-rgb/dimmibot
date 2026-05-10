const https = require('https');

const OPENROUTER_KEY = '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';

const testModels = [
  'liquid/lfm-2.5-1.2b-instruct:free',
  'nvidia/nemotron-nano-9b-v2:free',
  'openai/gpt-oss-120b:free',
  'meta-llama/llama-3.3-70b-instruct:free',
  'qwen/qwen3-next-80b-a3b-instruct:free'
];

async function testModel(model) {
  return new Promise((resolve) => {
    const start = Date.now();
    const data = JSON.stringify({
      model: model,
      messages: [{ role: 'user', content: 'Say hi in 3 words' }],
      max_tokens: 20
    });

    const options = {
      hostname: 'openrouter.ai',
      port: 443,
      path: '/api/v1/chat/completions',
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${OPENROUTER_KEY}`,
        'Content-Type': 'application/json',
        'Content-Length': data.length,
        'HTTP-Referer': 'https://github.com/dimmdimm1306-rgb/dimmibot',
        'X-Title': 'StokBarangMAUI'
      },
      timeout: 30000
    };

    const req = https.request(options, (res) => {
      let body = '';
      res.on('data', (chunk) => body += chunk);
      res.on('end', () => {
        const elapsed = Date.now() - start;
        try {
          const json = JSON.parse(body);
          if (json.choices && json.choices[0]) {
            const reply = json.choices[0].message.content.substring(0, 50);
            console.log(`✓ ${elapsed}ms | ${model} → ${reply}`);
            resolve({ model, success: true, latency: elapsed, reply });
          } else if (json.error) {
            console.log(`✗ ${elapsed}ms | ${model} → ${json.error.message.substring(0, 80)}`);
            resolve({ model, success: false, latency: elapsed, error: json.error.message });
          } else {
            console.log(`✗ ${elapsed}ms | ${model} → Unknown response`);
            resolve({ model, success: false, latency: elapsed, error: 'Unknown response' });
          }
        } catch (e) {
          console.log(`✗ ${elapsed}ms | ${model} → Parse error: ${e.message}`);
          resolve({ model, success: false, latency: elapsed, error: e.message });
        }
      });
    });

    req.on('error', (e) => {
      const elapsed = Date.now() - start;
      console.log(`✗ ${elapsed}ms | ${model} → ${e.message}`);
      resolve({ model, success: false, latency: elapsed, error: e.message });
    });

    req.on('timeout', () => {
      req.destroy();
      const elapsed = Date.now() - start;
      console.log(`✗ ${elapsed}ms | ${model} → Timeout`);
      resolve({ model, success: false, latency: elapsed, error: 'Timeout' });
    });

    req.write(data);
    req.end();
  });
}

async function main() {
  console.log('Testing OpenRouter free models...\n');
  const results = [];
  
  for (const model of testModels) {
    const result = await testModel(model);
    results.push(result);
  }

  console.log('\n=== SUMMARY ===');
  const working = results.filter(r => r.success);
  console.log(`Working models: ${working.length}/${results.length}`);
  if (working.length > 0) {
    console.log('\nRecommended models (sorted by latency):');
    working.sort((a, b) => a.latency - b.latency);
    working.forEach(r => {
      console.log(`  ${r.model} (${r.latency}ms)`);
    });
  }
}

main();
