const https = require('https');

// Load API key from .env file
require('dotenv').config({ path: require('path').join(__dirname, '..', '.env') });
const OPENAI_KEY = process.env.OPENAI_DIRECT_KEY;

async function testOpenAI() {
  return new Promise((resolve) => {
    const start = Date.now();
    const data = JSON.stringify({
      model: 'gpt-4o-mini',
      messages: [{ role: 'user', content: 'Say hi in 3 words' }],
      max_tokens: 20
    });

    const options = {
      hostname: 'api.openai.com',
      port: 443,
      path: '/v1/chat/completions',
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${OPENAI_KEY}`,
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
        try {
          const json = JSON.parse(body);
          if (json.choices && json.choices[0]) {
            const reply = json.choices[0].message.content;
            console.log(`✓ ${elapsed}ms | gpt-4o-mini → ${reply}`);
            console.log(`\nFull response:`, JSON.stringify(json, null, 2));
            resolve({ success: true, latency: elapsed, reply });
          } else if (json.error) {
            console.log(`✗ ${elapsed}ms | gpt-4o-mini → ${json.error.message}`);
            console.log(`\nError details:`, JSON.stringify(json.error, null, 2));
            resolve({ success: false, latency: elapsed, error: json.error.message });
          } else {
            console.log(`✗ ${elapsed}ms | gpt-4o-mini → Unknown response`);
            console.log(`\nResponse:`, body);
            resolve({ success: false, latency: elapsed, error: 'Unknown response' });
          }
        } catch (e) {
          console.log(`✗ ${elapsed}ms | gpt-4o-mini → Parse error: ${e.message}`);
          console.log(`\nRaw response:`, body);
          resolve({ success: false, latency: elapsed, error: e.message });
        }
      });
    });

    req.on('error', (e) => {
      const elapsed = Date.now() - start;
      console.log(`✗ ${elapsed}ms | gpt-4o-mini → ${e.message}`);
      resolve({ success: false, latency: elapsed, error: e.message });
    });

    req.on('timeout', () => {
      req.destroy();
      const elapsed = Date.now() - start;
      console.log(`✗ ${elapsed}ms | gpt-4o-mini → Timeout`);
      resolve({ success: false, latency: elapsed, error: 'Timeout' });
    });

    req.write(data);
    req.end();
  });
}

console.log('Testing OpenAI GPT-4o-mini...\n');
testOpenAI();
