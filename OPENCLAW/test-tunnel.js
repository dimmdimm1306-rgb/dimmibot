const https = require('https');

const TUNNEL_URL = 'https://sao-phases-consultants-arising.trycloudflare.com';

async function testTunnel() {
  console.log(`Testing tunnel: ${TUNNEL_URL}\n`);
  
  // Test 1: Health check
  console.log('1. Testing /health endpoint...');
  await new Promise((resolve) => {
    const url = new URL(`${TUNNEL_URL}/health`);
    https.get({
      hostname: url.hostname,
      path: url.pathname,
      timeout: 10000
    }, (res) => {
      let body = '';
      res.on('data', (chunk) => body += chunk);
      res.on('end', () => {
        console.log(`   Status: ${res.statusCode}`);
        console.log(`   Response: ${body}\n`);
        resolve();
      });
    }).on('error', (e) => {
      console.log(`   Error: ${e.message}\n`);
      resolve();
    });
  });

  // Test 2: Config endpoint
  console.log('2. Testing /config endpoint...');
  await new Promise((resolve) => {
    const url = new URL(`${TUNNEL_URL}/config`);
    https.get({
      hostname: url.hostname,
      path: url.pathname,
      timeout: 10000
    }, (res) => {
      let body = '';
      res.on('data', (chunk) => body += chunk);
      res.on('end', () => {
        console.log(`   Status: ${res.statusCode}`);
        try {
          const json = JSON.parse(body);
          console.log(`   Config:`, JSON.stringify(json, null, 2));
        } catch (e) {
          console.log(`   Response: ${body}`);
        }
        console.log();
        resolve();
      });
    }).on('error', (e) => {
      console.log(`   Error: ${e.message}\n`);
      resolve();
    });
  });

  // Test 3: Chat completion with GPT-4o-mini
  console.log('3. Testing chat completion with GPT-4o-mini...');
  await new Promise((resolve) => {
    const start = Date.now();
    const data = JSON.stringify({
      model: 'gpt-4o-mini',
      messages: [{ role: 'user', content: 'Halo, siapa kamu?' }],
      max_tokens: 50
    });

    const url = new URL(`${TUNNEL_URL}/v1/chat/completions`);
    const options = {
      hostname: url.hostname,
      path: url.pathname,
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Content-Length': data.length
      },
      timeout: 30000
    };

    const req = https.request(options, (res) => {
      let body = '';
      res.on('data', (chunk) => body += chunk);
      res.on('end', () => {
        const elapsed = Date.now() - start;
        console.log(`   Status: ${res.statusCode}`);
        console.log(`   Latency: ${elapsed}ms`);
        try {
          const json = JSON.parse(body);
          if (json.choices && json.choices[0]) {
            console.log(`   Reply: ${json.choices[0].message.content}`);
            console.log(`   Model: ${json.model}`);
            console.log(`   ✓ SUCCESS - GPT-4o-mini working via tunnel!`);
          } else if (json.error) {
            console.log(`   Error: ${json.error.message || JSON.stringify(json.error)}`);
          } else {
            console.log(`   Response: ${body.substring(0, 200)}`);
          }
        } catch (e) {
          console.log(`   Parse error: ${e.message}`);
          console.log(`   Raw: ${body.substring(0, 200)}`);
        }
        resolve();
      });
    });

    req.on('error', (e) => {
      console.log(`   Error: ${e.message}`);
      resolve();
    });

    req.write(data);
    req.end();
  });
}

testTunnel();
