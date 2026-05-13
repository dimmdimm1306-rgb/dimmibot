const express = require('express');
const { OpenAI } = require('openai');
const fetch = require('node-fetch');
const path = require('path');
const fs = require('fs');

// Clear any existing OPENAI_API_KEY from environment
delete process.env.OPENAI_API_KEY;

const envPath = path.resolve(__dirname, '..', '.env');
console.log(`[DEBUG] Loading .env from: ${envPath}`);
console.log(`[DEBUG] .env file exists: ${fs.existsSync(envPath)}`);

// Force load .env with override
const dotenvResult = require('dotenv').config({ path: envPath, override: true });
if (dotenvResult.error) {
    console.error(`[ERROR] Failed to load .env:`, dotenvResult.error);
} else {
    console.log(`[DEBUG] .env loaded successfully`);
}

const app = express();
app.use(express.json());

// ── OpenRouter Client ────────────────────────────────────────────────
// OPENAI_API_KEY = key untuk OpenRouter (legacy nama, gak kita rename)
const openrouterKey = process.env.OPENAI_API_KEY;
const openaiDirectKey = process.env.OPENAI_DIRECT_KEY;

const mask = (k) => k ? `${k.substring(0, 8)}...${k.substring(k.length - 4)}` : 'NOT FOUND';
console.log(`[DEBUG] OPENROUTER  : ${mask(openrouterKey)}`);
console.log(`[DEBUG] OPENAI_DIRECT: ${mask(openaiDirectKey)}`);
console.log(`[DEBUG] API_PORT    : ${process.env.API_PORT}`);
console.log(`[DEBUG] DEFAULT_MODEL: ${process.env.DEFAULT_MODEL}`);

const openrouterClient = openrouterKey ? new OpenAI({
    apiKey: openrouterKey,
    baseURL: 'https://openrouter.ai/api/v1'
}) : null;

const openaiClient = openaiDirectKey ? new OpenAI({
    apiKey: openaiDirectKey,
    baseURL: 'https://api.openai.com/v1'
}) : null;

// Strip "openrouter/" prefix kalau ada — biar config GitHub bisa pakai prefix
// untuk readability tapi server kirim ke OpenRouter pake nama asli.
function normalizeModel(model) {
    if (model.toLowerCase().startsWith('openrouter/')) {
        return model.substring('openrouter/'.length);
    }
    return model;
}

// ── Fallback chain ───────────────────────────────────────────────────
// Diisi dari env FALLBACK_MODELS (comma-separated). Selalu coba model
// dari request lebih dulu, lalu walk through chain ini.
const FALLBACK_MODELS = (process.env.FALLBACK_MODELS || 'openrouter/owl-alpha,openrouter/poolside/laguna-m.1:free,openrouter/meta-llama/llama-3.3-70b-instruct:free')
    .split(',')
    .map(s => s.trim())
    .filter(Boolean);

console.log(`[DEBUG] FALLBACK_MODELS: ${FALLBACK_MODELS.join(', ')}`);

// ── Per-call function ────────────────────────────────────────────────
async function callModel(model, messages, temperature, max_tokens) {
    const realModel = normalizeModel(model);
    
    // Detect provider based on model name
    const isOpenAIModel = ['gpt-4o-mini', 'gpt-4o', 'gpt-4-turbo', 'gpt-3.5-turbo'].includes(realModel);
    
    if (isOpenAIModel) {
        if (!openaiClient) throw Object.assign(new Error('OpenAI key not set'), { status: 401 });
        
        const TIMEOUT_MS = 15000;
        const timeoutPromise = new Promise((_, reject) =>
            setTimeout(() => reject(new Error('timeout')), TIMEOUT_MS)
        );
        const promise = openaiClient.chat.completions.create({
            model: realModel,
            messages: messages,
            temperature: temperature,
            max_tokens: max_tokens
        });
        return await Promise.race([promise, timeoutPromise]);
    } else {
        // Use OpenRouter for all other models
        if (!openrouterClient) throw Object.assign(new Error('OPENROUTER key not set'), { status: 401 });
        
        const TIMEOUT_MS = 15000;
        const timeoutPromise = new Promise((_, reject) =>
            setTimeout(() => reject(new Error('timeout')), TIMEOUT_MS)
        );
        const promise = openrouterClient.chat.completions.create({
            model: realModel,
            messages: messages,
            temperature: temperature,
            max_tokens: max_tokens
        });
        return await Promise.race([promise, timeoutPromise]);
    }
}

// ── Endpoints ────────────────────────────────────────────────────────
app.get('/health', (req, res) => {
    res.json({
        status: 'ok',
        service: 'OpenClaw API Server',
        provider: 'openrouter',
        ready: !!openrouterClient
    });
});

app.get('/config', (req, res) => {
    res.json({
        baseUrl: process.env.PUBLIC_URL || 'http://localhost:8080/v1',
        models: FALLBACK_MODELS,
        model: FALLBACK_MODELS[0] || 'openrouter/owl-alpha', // backward compat
        apiKeyRequired: false,
        version: '3.0.0'
    });
});

app.post('/v1/chat/completions', async (req, res) => {
    try {
        const {
            messages,
            model,
            temperature = 0.7,
            max_tokens = 1000
        } = req.body;

        if (!messages || !Array.isArray(messages)) {
            return res.status(400).json({ error: 'Invalid request: messages array required' });
        }

        const requestedModel = (model && model.trim() !== '')
            ? model
            : (process.env.DEFAULT_MODEL || FALLBACK_MODELS[0]);

        // Build try-list: requested first, lalu fallback chain (dedup)
        const modelsToTry = [requestedModel, ...FALLBACK_MODELS.filter(m => m !== requestedModel)];

        console.log(`[API] Chat request | ${messages.length} msgs | requested: ${requestedModel}`);

        let lastError = null;
        for (const m of modelsToTry) {
            try {
                console.log(`[API] → Trying ${m}`);
                const completion = await callModel(m, messages, temperature, max_tokens);
                console.log(`[API] ✓ Success from ${m}`);
                return res.json(completion);
            } catch (err) {
                lastError = err;
                const reason = err.message === 'timeout' ? 'timeout'
                    : err.status === 429 ? 'rate-limit'
                    : err.status === 401 ? 'no-key'
                    : `err ${err.status || ''} ${(err.message || '').slice(0, 50)}`;
                console.log(`[API] ✗ ${m} failed: ${reason}`);
                continue;
            }
        }

        console.error('[API] All models failed');
        res.status(500).json({
            error: lastError?.message || 'All models failed',
            type: 'api_error'
        });

    } catch (error) {
        console.error('[API] Error:', error.message);
        res.status(500).json({ error: error.message, type: 'api_error' });
    }
});

// ── Admin: POST /admin/instructions ──────────────────────────────────
// Update field `aiInstructions` di cloudflare-config.json on GitHub.
// Body: { password: "<dimmi13 atau ADMIN_PASSWORD env>", instructions: "<isi>" }
// Server pakai GITHUB_PAT (di .env) untuk commit via GitHub Contents API.
app.post('/admin/instructions', async (req, res) => {
    try {
        const { password, instructions } = req.body || {};
        const expected = process.env.ADMIN_PASSWORD || 'dimmi13';
        if (!password || password !== expected) {
            return res.status(401).json({ error: 'unauthorized' });
        }
        if (typeof instructions !== 'string') {
            return res.status(400).json({ error: 'instructions must be string' });
        }

        const pat = process.env.GITHUB_PAT;
        if (!pat) {
            return res.status(500).json({
                error: 'GITHUB_PAT not configured on server',
                hint: 'Tambah GITHUB_PAT=ghp_xxx di OPENCLAW/.env lalu restart'
            });
        }

        const owner  = process.env.GITHUB_OWNER  || 'dimmdimm1306-rgb';
        const repo   = process.env.GITHUB_REPO   || 'dimmibot';
        const file   = process.env.GITHUB_FILE   || 'cloudflare-config.json';
        const branch = process.env.GITHUB_BRANCH || 'master';

        const apiBase = `https://api.github.com/repos/${owner}/${repo}/contents/${file}`;
        const headers = {
            'Authorization': `Bearer ${pat}`,
            'Accept': 'application/vnd.github+json',
            'User-Agent': 'OpenClaw-API',
            'X-GitHub-Api-Version': '2022-11-28'
        };

        // 1. GET current file (need SHA + content)
        const getRes = await fetch(`${apiBase}?ref=${branch}`, { headers });
        if (!getRes.ok) {
            const errText = await getRes.text();
            return res.status(502).json({
                error: `GitHub GET failed: ${getRes.status}`,
                detail: errText.slice(0, 300)
            });
        }
        const meta = await getRes.json();
        const currentJson = JSON.parse(Buffer.from(meta.content, 'base64').toString('utf-8'));

        // 2. Update field
        currentJson.aiInstructions = instructions;
        currentJson.lastInstructionsUpdate = new Date().toISOString();
        currentJson.version = String((parseInt(currentJson.version) || 0) + 1);

        const newContent = Buffer.from(JSON.stringify(currentJson, null, 2)).toString('base64');

        // 3. PUT new content
        const commitMsg = instructions
            ? `Admin AI instructions: ${instructions.slice(0, 60).replace(/\n/g, ' ')}`
            : 'Clear AI instructions';
        const putRes = await fetch(apiBase, {
            method: 'PUT',
            headers: { ...headers, 'Content-Type': 'application/json' },
            body: JSON.stringify({
                message: commitMsg,
                content: newContent,
                sha: meta.sha,
                branch: branch
            })
        });

        if (!putRes.ok) {
            const errText = await putRes.text();
            return res.status(502).json({
                error: `GitHub PUT failed: ${putRes.status}`,
                detail: errText.slice(0, 300)
            });
        }

        const putData = await putRes.json();
        const sha = putData.commit && putData.commit.sha ? putData.commit.sha.substring(0, 7) : '?';
        console.log(`[API] /admin/instructions OK — commit ${sha}, version=${currentJson.version}`);
        res.json({ success: true, commit: sha, version: currentJson.version });
    } catch (err) {
        console.error('[API] /admin/instructions error:', err);
        res.status(500).json({ error: err.message || 'internal error' });
    }
});

const PORT = process.env.API_PORT || 20128;
app.listen(PORT, '0.0.0.0', () => {
    console.log(`\n🚀 OpenClaw API Server running on http://0.0.0.0:${PORT}`);
    console.log(`📡 Endpoint: http://localhost:${PORT}/v1/chat/completions`);
    console.log(`💡 Use this as Base URL in StokBarangMAUI app\n`);
});
