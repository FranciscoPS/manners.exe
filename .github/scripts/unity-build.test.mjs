import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { mkdtemp, readFile, rm, stat } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { basename, dirname, join, resolve } from 'node:path';
import test from 'node:test';
import { downloadArchive, loadConfig, runBuilds, selectArchive, validateTarget } from './unity-build.mjs';

const definitions = JSON.parse(await readFile(new URL('../unity-build-targets.json', import.meta.url), 'utf8'));
const commit = 'a'.repeat(40);
const unityVersion = '6000.3.5f2';
const zip = Buffer.alloc(22);
zip.set([0x50, 0x4b, 0x05, 0x06]);
const md5 = createHash('md5').update(zip).digest('hex');
const sha256 = createHash('sha256').update(zip).digest('hex');

function environment(overrides = {}) {
  return {
    UNITY_ORG_ID: 'organization',
    UNITY_PROJECT_ID: 'project',
    UNITY_SERVICE_ACCOUNT_KEY_ID: 'test-key',
    UNITY_SERVICE_ACCOUNT_SECRET: 'test-secret',
    GITHUB_REPOSITORY: 'FranciscoPS/manners.exe',
    ...Object.fromEntries(definitions.map(target => [target.variable, `target-${target.key}`])),
    ...overrides,
  };
}

async function configuration(t, overrides = {}, env = {}, metadata = {}) {
  const outputDir = await mkdtemp(join(tmpdir(), 'manners-uba-test-'));
  t.after(() => {
    const resolvedOutputDir = resolve(outputDir);
    assert.equal(dirname(resolvedOutputDir), resolve(tmpdir()));
    assert.ok(basename(resolvedOutputDir).startsWith('manners-uba-test-'));
    return rm(resolvedOutputDir, { recursive: true, force: true });
  });
  return { ...loadConfig(environment(env), { definitions, unityVersion, commit, ...metadata }), outputDir, ...overrides };
}

function json(value, status = 200, headers = {}) {
  return new Response(JSON.stringify(value), { status, headers: { 'content-type': 'application/json', ...headers } });
}

function remoteTarget(target, config) {
  return {
    buildtargetid: target.id,
    enabled: true,
    platform: target.platforms[0],
    settings: { unityVersion, scm: { url: `https://github.com/${config.repository}.git` } },
  };
}

function opaqueTarget(target, config) {
  const remote = remoteTarget(target, config);
  remote.connectionId = 'opaque-connection';
  remote.settings.scm = { branch: 'master', oauth: { github: { token: 'synthetic-hidden-token' } } };
  return remote;
}

function buildRecord(target, number, overrides = {}) {
  return { buildtargetid: target.id, build: number, platform: target.platforms[0], buildStatus: 'success', lastBuiltRevision: commit, unityVersion, ...overrides };
}

function fakeApi(config, override = async () => undefined) {
  const calls = [];
  const fetchImpl = async (url, options = {}) => {
    const parsed = new URL(url);
    const method = options.method || 'GET';
    const call = { url: parsed.href, path: parsed.pathname, method, options };
    calls.push(call);
    const handled = await override(call, calls);
    if (handled !== undefined) return handled;
    if (parsed.hostname === 'storage.example') return new Response(zip);
    const match = parsed.pathname.match(/\/buildtargets\/([^/]+)(?:\/builds(?:\/(\d+)(?:\/(artifacts|download|failures)(?:\/(.+))?)?)?)?$/);
    assert.ok(match, `Unexpected API request ${method} ${url}`);
    const target = config.targets.find(item => item.id === decodeURIComponent(match[1]));
    assert.ok(target, `Unexpected target ${match[1]}`);
    const number = config.targets.indexOf(target) + 101;
    if (!parsed.pathname.includes('/builds')) return json(remoteTarget(target, config));
    if (method === 'POST') return json([buildRecord(target, number, { requestedRevision: commit, buildStatus: 'queued' })], 202);
    if (method === 'DELETE') return new Response(null, { status: 204 });
    if (match[3] === 'artifacts') return json([{ key: 'primary', files: [{ filename: `${target.key}.zip`, size: zip.length, md5sum: md5 }] }]);
    if (match[3] === 'download') return json({ url: `https://storage.example/${target.key}.zip?signature=temporary` }, 303);
    if (match[3] === 'failures') return json({ failures: [] });
    return json(buildRecord(target, number));
  };
  return { calls, fetchImpl, logger: () => {}, sleep: async () => {} };
}

test('rejects missing credentials and target IDs before requesting builds', () => {
  for (const name of ['UNITY_ORG_ID', 'UNITY_SERVICE_ACCOUNT_KEY_ID', 'UNITY_SERVICE_ACCOUNT_SECRET', 'UNITY_BUILD_TARGET_MAC']) {
    assert.throws(() => loadConfig(environment({ [name]: ' ' }), { definitions, unityVersion, commit }), error => error.message.includes(name));
  }
  assert.throws(() => loadConfig(environment({ UNITY_BUILD_TARGET_WEB: 'target-mac' }), { definitions, unityVersion, commit }), /distinto/);
  assert.throws(() => loadConfig(environment(), { definitions, unityVersion, commit: 'main' }), /SHA completo/);
});

test('dashboard URLs, paths and internal whitespace fail before requests and identify the target variable', async t => {
  const fixture = await configuration(t, { validateOnly: true });
  let requests = 0;
  const fetchImpl = async () => { requests++; throw new Error('No request should be made for an invalid ID'); };
  const invalidIds = [
    'https://cloud.unity.com/orgs/4673983555177/projects/7c68d06f-d4d8-4b8b-b758-e411552e8b95/cloud-build/setup/buildTarget/default-webgl?tab=settings',
    '/buildTarget/default-webgl',
    'buildTarget\\default-webgl',
    'default webgl',
    'default\twebgl',
  ];
  for (const target of definitions) {
    for (const id of invalidIds) {
      await assert.rejects(async () => {
        const config = { ...loadConfig(environment({ [target.variable]: id }), { definitions, unityVersion, commit }), outputDir: fixture.outputDir, validateOnly: true };
        await runBuilds(config, { fetchImpl, logger: () => {}, sleep: async () => {} });
      }, error => error.message.includes(target.variable) && error.message.includes('/buildTarget/') && /URL completa/i.test(error.message));
    }
  }
  assert.equal(requests, 0);
});

test('default target slugs with external whitespace are accepted and trimmed', () => {
  const slugs = { windows: 'default-windows-desktop-64-bit', mac: 'default-mac-desktop-universal', web: 'default-webgl' };
  const env = environment(Object.fromEntries(definitions.map(target => [target.variable, ` \t${slugs[target.key]}\n `])));
  const config = loadConfig(env, { definitions, unityVersion, commit });
  assert.deepEqual(config.targets.map(target => target.id), definitions.map(target => slugs[target.key]));
});

test('single-platform selection requires only its configured target', () => {
  const config = loadConfig(environment({ BUILD_PLATFORMS: 'web', UNITY_BUILD_TARGET_MAC: '', UNITY_BUILD_TARGET_WINDOWS: '' }), { definitions, unityVersion, commit });
  assert.deepEqual(config.targets.map(target => target.key), ['web']);
});

test('the removed Linux platform is rejected before requesting a build', () => {
  assert.throws(() => loadConfig(environment({ BUILD_PLATFORMS: 'linux' }), { definitions, unityVersion, commit }), /Plataforma desconocida: linux/);
});

test('routes requests to the project linked in Unity metadata when the environment ID is empty', async t => {
  const projectId = '7c68d06f-d4d8-4b8b-b758-e411552e8b95';
  const config = await configuration(t, { validateOnly: true }, { UNITY_PROJECT_ID: ' ' }, { projectId });
  assert.equal(config.projectId, projectId);
  const api = fakeApi(config);
  await runBuilds(config, api);
  assert.ok(api.calls.every(call => call.path.includes(`/projects/${projectId}/`)));
});

test('an explicit environment project ID takes priority over Unity metadata', async t => {
  const projectId = '9aa00dc7-5b49-4548-a8ed-7b7d7d278b98';
  const config = await configuration(t, { validateOnly: true }, { UNITY_PROJECT_ID: ` ${projectId} ` }, { projectId: '7c68d06f-d4d8-4b8b-b758-e411552e8b95' });
  assert.equal(config.projectId, projectId);
  const api = fakeApi(config);
  await runBuilds(config, api);
  assert.ok(api.calls.every(call => call.path.includes(`/projects/${projectId}/`)));
});

test('missing project IDs fail instead of silently routing to a historical hardcoded project', () => {
  for (const projectId of [undefined, '', ' ']) {
    assert.throws(() => loadConfig(environment({ UNITY_PROJECT_ID: '' }), { definitions, unityVersion, commit, projectId }), /UNITY_PROJECT_ID|cloudProjectId|proyecto/i);
  }
});

test('configured releases pin the checkout SHA and Unity version and download 303 JSON links without credentials', async t => {
  const config = await configuration(t, { branch: 'develop', clean: true });
  const api = fakeApi(config);
  const result = await runBuilds(config, api);
  assert.equal(result.commit, commit);
  assert.equal(result.builds.length, config.targets.length);
  const posts = api.calls.filter(call => call.method === 'POST');
  assert.equal(posts.length, config.targets.length);
  assert.deepEqual(api.calls.slice(0, config.targets.length).map(call => call.path.split('/').at(-1)), config.targets.map(target => target.id));
  for (const call of posts) {
    assert.deepEqual(JSON.parse(call.options.body), { commit, unityVersion, clean: true, delay: 0, branch: 'develop' });
    assert.equal(call.options.headers.Authorization, `Basic ${Buffer.from('test-key:test-secret').toString('base64')}`);
    assert.equal(call.options.redirect, 'manual');
  }
  const storage = api.calls.filter(call => new URL(call.url).hostname === 'storage.example');
  assert.equal(storage.length, config.targets.length);
  for (const call of storage) {
    assert.equal(call.options.headers, undefined);
    assert.equal(call.options.body, undefined);
  }
  for (const build of result.builds) {
    assert.equal(build.revision, commit);
    assert.equal(build.bytes, zip.length);
    assert.equal(build.sha256, sha256);
    assert.deepEqual(await readFile(join(config.outputDir, build.archive)), zip);
  }
  const report = JSON.parse(await readFile(join(config.outputDir, 'unity-build-results.json'), 'utf8'));
  assert.equal(report.builds.length, config.targets.length);
  assert.equal(report.unityVersion, unityVersion);
});

test('a release tag requests Windows, macOS and WebGL from its exact SHA without sending a branch override', async t => {
  const config = await configuration(t, {}, { BUILD_BRANCH: '', GITHUB_REF_TYPE: 'tag', GITHUB_REF: 'refs/tags/v0.2.0' });
  assert.equal(config.branch, '');
  assert.equal(config.validateOnly, false);
  assert.deepEqual(config.targets.map(target => target.key), ['windows', 'mac', 'web']);
  const api = fakeApi(config);
  const result = await runBuilds(config, { ...api, download: async () => ({ bytes: zip.length, sha256 }) });
  const posts = api.calls.filter(call => call.method === 'POST');
  assert.equal(posts.length, config.targets.length);
  for (const call of posts) {
    assert.deepEqual(JSON.parse(call.options.body), { commit, unityVersion, clean: false, delay: 0 });
  }
  assert.equal(result.commit, commit);
  assert.ok(result.builds.every(build => build.status === 'success' && build.revision === commit));
});

test('validation-only checks all targets without triggering or downloading builds', async t => {
  const config = await configuration(t, { validateOnly: true });
  const api = fakeApi(config);
  const result = await runBuilds(config, api);
  assert.deepEqual(result.validated, ['windows', 'mac', 'web']);
  assert.deepEqual(result.builds, []);
  assert.equal(api.calls.length, config.targets.length);
  assert.ok(api.calls.every(call => call.method === 'GET' && !call.path.includes('/builds')));
});

for (const [label, change] of [
  ['missing Unity version', remote => { delete remote.settings.unityVersion; }],
  ['wrong Unity patch', remote => { remote.settings.unityVersion = '6000.3.6f1'; }],
  ['missing platform', remote => { delete remote.platform; }],
  ['wrong platform', remote => { remote.platform = 'android'; }],
  ['automatic version detection', remote => { remote.settings.autoDetectUnityVersion = true; }],
  ['independent scheduled builds', remote => { remote.settings.buildSchedule = { isEnabled: true }; }],
  ['wrong project subfolder', remote => { remote.settings.scm.subdirectory = 'Assets'; }],
]) {
  test(`preflight rejects ${label} before any POST`, async t => {
    const config = await configuration(t);
    const invalid = config.targets.at(-1);
    const api = fakeApi(config, async call => {
      if (call.path.endsWith(`/${invalid.id}`)) {
        const remote = remoteTarget(invalid, config);
        change(remote);
        return json(remote);
      }
    });
    await assert.rejects(runBuilds(config, api));
    assert.equal(api.calls.filter(call => call.method === 'POST').length, 0);
  });
}

test('validates repository identity and accepts GitHub SSH URLs', async t => {
  const config = await configuration(t);
  const target = config.targets[0];
  const remote = remoteTarget(target, config);
  remote.settings.scm.url = `git@github.com:${config.repository}.git`;
  assert.doesNotThrow(() => validateTarget(target, remote, config));
  remote.settings.scm.url = 'https://github.com/someone/unrelated.git';
  assert.throws(() => validateTarget(target, remote, config), /repositorio correcto/);
});

for (const [label, repository] of [
  ['full_name', { full_name: 'FranciscoPS/manners.exe' }],
  ['clone_url', { clone_url: 'https://github.com/FranciscoPS/manners.exe.git' }],
]) {
  test(`accepts GitHub OAuth repository ${label} without a legacy SCM URL`, async t => {
    const config = await configuration(t, { validateOnly: true });
    const api = fakeApi(config, async call => {
      const target = config.targets.find(item => call.path.endsWith(`/${item.id}`));
      if (target) {
        const remote = remoteTarget(target, config);
        remote.settings.scm = { oauth: { github: { repository } } };
        return json(remote);
      }
    });
    const result = await runBuilds(config, api);
    assert.deepEqual(result.validated, config.targets.map(target => target.key));
    assert.equal(api.calls.length, config.targets.length);
    assert.ok(api.calls.every(call => call.method === 'GET'));
  });
}

test('inherits project-level GitHub OAuth identity and requests project settings only once', async t => {
  const config = await configuration(t, { validateOnly: true });
  const projectPath = `/v2/orgs/${config.orgId}/projects/${config.projectId}`;
  const api = fakeApi(config, async call => {
    if (call.path === projectPath) return json({ projectid: config.projectId, settings: { scm: { oauth: { github: { repository: { full_name: config.repository } } } } } });
    const target = config.targets.find(item => call.path.endsWith(`/${item.id}`));
    if (target) {
      const remote = remoteTarget(target, config);
      remote.settings.scm = { branch: 'master', subdirectory: '' };
      return json(remote);
    }
  });
  const result = await runBuilds(config, api);
  assert.deepEqual(result.validated, config.targets.map(target => target.key));
  assert.equal(api.calls.filter(call => call.path === projectPath).length, 1);
  assert.equal(api.calls.length, config.targets.length + 1);
  assert.equal(api.calls.filter(call => call.method !== 'GET').length, 0);
});

test('an explicit wrong target repository cannot be hidden by a correct project default', async t => {
  const config = await configuration(t);
  const remote = remoteTarget(config.targets[0], config);
  remote.settings.scm.url = 'https://github.com/someone/unrelated.git';
  assert.throws(() => validateTarget(config.targets[0], remote, config, { url: `https://github.com/${config.repository}.git` }), /repositorio correcto/);
});

test('conflicting explicit SCM and OAuth repository identities are rejected', async t => {
  const config = await configuration(t);
  const remote = remoteTarget(config.targets[0], config);
  remote.settings.scm.oauth = { github: { repository: { full_name: 'someone/unrelated' } } };
  assert.throws(() => validateTarget(config.targets[0], remote, config), /repositorio correcto/);
});

test('validation-only accepts opaque connections with a warning and makes no build or project requests', async t => {
  const config = await configuration(t, { validateOnly: true });
  const logs = [];
  const api = fakeApi(config, async call => {
    const target = config.targets.find(item => call.path.endsWith(`/${item.id}`));
    if (target) return json(opaqueTarget(target, config));
  });
  const result = await runBuilds(config, { ...api, logger: message => logs.push(message) });
  assert.deepEqual(result.validated, config.targets.map(target => target.key));
  assert.deepEqual(result.builds, []);
  assert.equal(result.warnings.length, config.targets.length);
  assert.equal(api.calls.length, config.targets.length);
  assert.ok(api.calls.every(call => call.method === 'GET' && call.path.includes('/buildtargets/')));
  assert.ok(logs.join('\n').includes(commit));
  for (const sensitive of ['synthetic-hidden-token', 'test-secret', Buffer.from('test-key:test-secret').toString('base64')]) assert.ok(!logs.join('\n').includes(sensitive));
  assert.deepEqual(validateTarget(config.targets[0], opaqueTarget(config.targets[0], config), config, { url: `https://github.com/${config.repository}.git` }), { repositoryVerified: false });
});

test('an opaque connection builds the exact SHA and records its warning with the verified artifact', async t => {
  const config = await configuration(t, {}, { BUILD_PLATFORMS: 'web' });
  config.summaryPath = join(config.outputDir, 'summary.md');
  const api = fakeApi(config, async call => {
    if (call.path.endsWith(`/${config.targets[0].id}`)) return json(opaqueTarget(config.targets[0], config));
  });
  const result = await runBuilds(config, api);
  assert.equal(result.warnings.length, 1);
  assert.equal(result.builds[0].revision, commit);
  assert.equal(result.builds[0].status, 'success');
  assert.equal(result.builds[0].sha256, sha256);
  assert.deepEqual(await readFile(join(config.outputDir, result.builds[0].archive)), zip);
  const report = JSON.parse(await readFile(join(config.outputDir, 'unity-build-results.json'), 'utf8'));
  assert.deepEqual(report.warnings, result.warnings);
  const summary = await readFile(config.summaryPath, 'utf8');
  assert.ok(summary.includes(result.warnings[0]));
  assert.ok(summary.includes(commit));
  assert.equal(api.calls.filter(call => call.method === 'POST').length, 1);
  assert.equal(api.calls.filter(call => call.path.endsWith('/artifacts')).length, 1);
  assert.equal(api.calls.filter(call => call.path === `/v2/orgs/${config.orgId}/projects/${config.projectId}`).length, 0);
});

test('opaque connection warnings never bypass requested or built SHA mismatches', async t => {
  for (const mismatch of ['requested', 'built']) {
    const config = await configuration(t, {}, { BUILD_PLATFORMS: 'web' });
    const api = fakeApi(config, async call => {
      if (call.path.endsWith(`/${config.targets[0].id}`)) return json(opaqueTarget(config.targets[0], config));
      if (mismatch === 'requested' && call.method === 'POST') return json([buildRecord(config.targets[0], 101, { requestedRevision: 'b'.repeat(40), buildStatus: 'queued' })], 202);
      if (mismatch === 'built' && call.method === 'GET' && /\/builds\/101$/.test(call.path)) return json(buildRecord(config.targets[0], 101, { lastBuiltRevision: 'b'.repeat(40) }));
    });
    await assert.rejects(runBuilds(config, api), /commit/);
    assert.equal(api.calls.filter(call => /\/artifacts|\/download\//.test(call.path)).length, 0);
    assert.equal(api.calls.filter(call => new URL(call.url).hostname === 'storage.example').length, 0);
    const report = JSON.parse(await readFile(join(config.outputDir, 'unity-build-results.json'), 'utf8'));
    assert.equal(report.warnings.length, 1);
  }
});

test('missing repository metadata without a connection remains a validation error', async t => {
  const config = await configuration(t);
  const remote = remoteTarget(config.targets[0], config);
  remote.settings.scm = { branch: 'master' };
  assert.throws(() => validateTarget(config.targets[0], remote, config), /repositorio seleccionado/);
});

test('accepts equivalent underscore Unity versions and inactive fallback patch settings', async t => {
  const config = await configuration(t);
  const remote = remoteTarget(config.targets[0], config);
  remote.settings.unityVersion = '6000_3_5f2';
  remote.settings.autoDetectUnityVersion = false;
  remote.settings.fallbackPatchVersion = true;
  assert.doesNotThrow(() => validateTarget(config.targets[0], remote, config));
});

test('a successful build with underscore version metadata downloads the artifact for the correct SHA', async t => {
  const config = await configuration(t, {}, { BUILD_PLATFORMS: 'web' });
  const api = fakeApi(config, async call => {
    if (call.method === 'GET' && /\/builds\/101$/.test(call.path)) {
      return json(buildRecord(config.targets[0], 101, { unityVersion: '6000_3_5f2', localUnityVersion: '6000_3_5f2' }));
    }
  });
  const result = await runBuilds(config, api);
  assert.equal(result.builds[0].revision, commit);
  assert.equal(result.builds[0].status, 'success');
  assert.equal(result.builds[0].sha256, sha256);
  assert.deepEqual(await readFile(join(config.outputDir, result.builds[0].archive)), zip);
  assert.equal(api.calls.filter(call => call.path.endsWith('/artifacts')).length, 1);
  assert.equal(api.calls.filter(call => new URL(call.url).hostname === 'storage.example').length, 1);
});

test('preflight reports every target problem before triggering builds without exposing repository credentials', async t => {
  const config = await configuration(t);
  const api = fakeApi(config, async call => {
    const target = config.targets.find(item => call.path.endsWith(`/${item.id}`));
    if (target) {
      const remote = remoteTarget(target, config);
      remote.settings.unityVersion = '6000.3.6f1';
      remote.settings.autoBuild = true;
      remote.settings.buildSchedule = { isEnabled: true };
      remote.settings.scm = {
        url: 'https://repository-user:synthetic-password@github.com/someone/unrelated.git',
        oauth: { github: { token: 'synthetic-oauth-token', repository: { full_name: 'someone/unrelated' } } },
      };
      return json(remote);
    }
  });
  await assert.rejects(runBuilds(config, api), error => {
    for (const target of config.targets) assert.ok(error.message.includes(`${target.key}:`));
    assert.match(error.message, /6000\.3\.6f1/);
    assert.match(error.message, /Auto-build/);
    assert.match(error.message, /Build schedule/);
    for (const sensitive of ['synthetic-password', 'synthetic-oauth-token', 'test-secret', 'https://', 'repository-user']) assert.ok(!error.message.includes(sensitive));
    return true;
  });
  assert.equal(api.calls.length, config.targets.length);
  assert.equal(api.calls.filter(call => call.method === 'POST').length, 0);
});

for (const [label, overrides] of [
  ['different built revision', { lastBuiltRevision: 'b'.repeat(40) }],
  ['missing built revision', { lastBuiltRevision: undefined }],
  ['different reported Unity version', { localUnityVersion: '6000.3.6f1' }],
  ['different build number', { build: 999 }],
]) {
  test(`${label} refuses artifacts`, async t => {
    const config = await configuration(t, {}, { BUILD_PLATFORMS: 'web' });
    const api = fakeApi(config, async call => {
      if (call.method === 'GET' && /\/builds\/101$/.test(call.path)) return json(buildRecord(config.targets[0], 101, overrides));
    });
    await assert.rejects(runBuilds(config, api));
    assert.equal(api.calls.filter(call => /\/artifacts|\/download\//.test(call.path)).length, 0);
    assert.equal(api.calls.filter(call => new URL(call.url).hostname === 'storage.example').length, 0);
  });
}

test('timeout cancels only the build created by this run', async t => {
  const config = await configuration(t, { timeoutMs: 100, pollMs: 100 }, { BUILD_PLATFORMS: 'web' });
  let clock = 0;
  const api = fakeApi(config, async call => {
    if (call.method === 'GET' && /\/builds\/101$/.test(call.path)) return json(buildRecord(config.targets[0], 101, { buildStatus: 'building' }));
  });
  await assert.rejects(runBuilds(config, { ...api, now: () => clock, sleep: async ms => { clock += ms; } }), /tiempo máximo/);
  assert.deepEqual(api.calls.filter(call => call.method === 'DELETE').map(call => call.path.split('/').at(-1)), ['101']);
  assert.equal(api.calls.filter(call => call.method === 'POST').length, 1);
});

test('abort cancels an owned build using a cleanup signal that is still usable', async t => {
  const config = await configuration(t, {}, { BUILD_PLATFORMS: 'web' });
  const abort = new AbortController();
  const api = fakeApi(config, async call => {
    if (call.method === 'GET' && /\/builds\/101$/.test(call.path)) {
      abort.abort();
      return json(buildRecord(config.targets[0], 101, { buildStatus: 'building' }));
    }
  });
  await assert.rejects(runBuilds(config, { ...api, signal: abort.signal }), /cancelada/);
  const cancellations = api.calls.filter(call => call.method === 'DELETE');
  assert.equal(cancellations.length, 1);
  assert.equal(cancellations[0].options.signal.aborted, false);
  assert.equal(api.calls.filter(call => /\/artifacts/.test(call.path)).length, 0);
});

test('GET retries transient HTTP and network errors, then triggers exactly once', async t => {
  const config = await configuration(t, {}, { BUILD_PLATFORMS: 'web' });
  let attempts = 0;
  const sleeps = [];
  const api = fakeApi(config, async call => {
    if (call.path.endsWith('/target-web')) {
      attempts++;
      if (attempts === 1) return json({ error: 'busy' }, 429, { 'retry-after': '100' });
      if (attempts === 2) throw new TypeError('connection interrupted');
    }
  });
  await runBuilds(config, { ...api, sleep: async ms => { sleeps.push(ms); } });
  assert.equal(attempts, 3);
  assert.deepEqual(sleeps, [30_000, 4_000]);
  assert.equal(api.calls.filter(call => call.method === 'POST').length, 1);
});

for (const [label, response] of [
  ['network failure', () => { throw new TypeError('response lost'); }],
  ['HTTP 503', () => json({ error: 'unavailable' }, 503)],
  ['HTTP 409 from an existing foreign build', () => json({ error: 'pending' }, 409)],
]) {
  test(`POST ${label} is never retried or used to cancel unknown builds`, async t => {
    const config = await configuration(t, {}, { BUILD_PLATFORMS: 'web' });
    const api = fakeApi(config, async call => { if (call.method === 'POST') return response(); });
    await assert.rejects(runBuilds(config, api));
    assert.equal(api.calls.filter(call => call.method === 'POST').length, 1);
    assert.equal(api.calls.filter(call => call.method === 'DELETE').length, 0);
  });
}

test('a peer failure blocks download after another successful status was already requested', async t => {
  const config = await configuration(t);
  config.targets = config.targets.slice(0, 2);
  let peerInFlight;
  let peerRequested;
  const requested = new Promise(resolve => { peerRequested = resolve; });
  const delayed = new Promise(resolve => { peerInFlight = resolve; });
  const api = fakeApi(config, async call => {
    if (call.method === 'GET' && /\/builds\/102$/.test(call.path)) {
      peerRequested();
      await delayed;
      return json(buildRecord(config.targets[1], 102));
    }
    if (call.method === 'GET' && /\/builds\/101$/.test(call.path)) {
      await requested;
      return json(buildRecord(config.targets[0], 101, { buildStatus: 'failure' }));
    }
    if (call.method === 'DELETE') {
      peerInFlight();
      return new Response(null, { status: 204 });
    }
  });
  await assert.rejects(runBuilds(config, api), /failure/);
  assert.equal(api.calls.filter(call => /\/artifacts|\/download\//.test(call.path)).length, 0);
  assert.deepEqual(api.calls.filter(call => call.method === 'DELETE').map(call => call.path.split('/').at(-1)), ['102']);
});

test('inline failure details prioritize matching stages and expose only bounded sanitized diagnostics', async t => {
  const config = await configuration(t, {}, { BUILD_PLATFORMS: 'web' });
  const encodedCredential = Buffer.from(`${config.keyId}:${config.secret}`).toString('base64');
  const pat = `github_pat_${'A'.repeat(60)}`;
  const details = [
    { displayName: 'Unrelated indicator excluded by limit', stageMatch: false, publicMessage: 'wrong-stage-sentinel' },
    { displayName: 'Git Checkout Error', stageMatch: true, stage: 'checkout', step: 'git-clone', publicMessage: `Git authentication failed\n${config.secret} ${config.keyId} Basic ${encodedCredential} ${pat} token=synthetic-assignment https://repo.example/private?secret=private-url-sentinel`, rawLog: 'raw-log-sentinel', token: 'raw-token-sentinel' },
    { displayName: 'Connection indicator', stageMatch: true, publicMessage: 'Repository unavailable' },
    { displayName: 'Checkout indicator', stageMatch: true, publicMessage: 'Unable to retrieve revision' },
  ];
  const logs = [];
  const api = fakeApi(config, async call => {
    if (call.method === 'GET' && /\/builds\/101$/.test(call.path)) return json(buildRecord(config.targets[0], 101, { buildStatus: 'failure', failureDetails: details }));
  });
  let error;
  try { await runBuilds(config, { ...api, logger: message => logs.push(message) }); } catch (caught) { error = caught; }
  assert.ok(error);
  assert.match(error.message, /web #101: failure/);
  assert.match(error.message, /Git Checkout Error/);
  assert.match(error.message, /Git authentication failed/);
  assert.ok(!error.message.includes('Unrelated indicator excluded by limit'));
  const report = JSON.parse(await readFile(join(config.outputDir, 'unity-build-results.json'), 'utf8'));
  assert.match(report.builds[0].error, /Git Checkout Error/);
  const surfaced = `${error.message}\n${logs.join('\n')}\n${JSON.stringify(report)}`;
  for (const sensitive of [config.secret, config.keyId, encodedCredential, pat, 'synthetic-assignment', 'private-url-sentinel', 'raw-log-sentinel', 'raw-token-sentinel', 'https://repo.example']) assert.ok(!surfaced.includes(sensitive), `Diagnostic exposed ${sensitive}`);
  assert.equal(api.calls.filter(call => call.path.endsWith('/failures')).length, 0);
  assert.equal(api.calls.filter(call => /\/artifacts|\/download\//.test(call.path)).length, 0);
});

test('fetches categorized failure details only after stopping peers and never launches the remaining platform', async t => {
  const config = await configuration(t);
  const api = fakeApi(config, async call => {
    if (call.method === 'GET' && /\/builds\/101$/.test(call.path)) return json(buildRecord(config.targets[0], 101, { buildStatus: 'building' }));
    if (call.method === 'GET' && /\/builds\/102$/.test(call.path)) return json(buildRecord(config.targets[1], 102, { buildStatus: 'failure' }));
    if (call.path.endsWith('/102/failures')) return json({ failures: [{ displayName: 'Git Checkout Error', stage: 'checkout', step: 'git-clone', publicMessage: 'Git authentication failed', stageMatch: true }] });
  });
  await assert.rejects(runBuilds(config, api), error => /mac #102: failure/.test(error.message) && /Git authentication failed/.test(error.message));
  const cancelIndex = api.calls.findIndex(call => call.method === 'DELETE' && call.path.endsWith('/101'));
  const diagnosticsIndex = api.calls.findIndex(call => call.path.endsWith('/102/failures'));
  assert.ok(cancelIndex >= 0 && diagnosticsIndex > cancelIndex);
  assert.equal(api.calls.filter(call => call.path.endsWith('/failures')).length, 1);
  assert.equal(api.calls.filter(call => call.method === 'POST').length, 2);
  assert.equal(api.calls.filter(call => /target-web\/builds$/.test(call.path)).length, 0);
  assert.equal(api.calls.filter(call => /\/artifacts|\/download\//.test(call.path)).length, 0);
});

test('unavailable, empty or malformed diagnostics preserve the original failure and peer cancellation without retries', async t => {
  for (const diagnosticResponse of [
    () => json({ error: 'forbidden' }, 403),
    () => json({ error: 'not found' }, 404),
    () => json({ error: 'unavailable' }, 503),
    () => { throw new TypeError('diagnostic network failure'); },
    () => new Response('{malformed', { status: 200 }),
    () => json({ failures: [] }),
  ]) {
    const config = await configuration(t);
    const api = fakeApi(config, async call => {
      if (call.method === 'GET' && /\/builds\/101$/.test(call.path)) return json(buildRecord(config.targets[0], 101, { buildStatus: 'building' }));
      if (call.method === 'GET' && /\/builds\/102$/.test(call.path)) return json(buildRecord(config.targets[1], 102, { buildStatus: 'failure' }));
      if (call.path.endsWith('/102/failures')) return diagnosticResponse();
    });
    await assert.rejects(runBuilds(config, api), error => /mac #102: failure/.test(error.message) && !/Unity API: HTTP|JSON no válida|contactar con la API/.test(error.message));
    assert.equal(api.calls.filter(call => call.path.endsWith('/failures')).length, 1);
    assert.deepEqual(api.calls.filter(call => call.method === 'DELETE').map(call => call.path.split('/').at(-1)), ['101']);
    assert.equal(api.calls.filter(call => call.method === 'POST').length, 2);
    assert.equal(api.calls.filter(call => /target-web\/builds$/.test(call.path)).length, 0);
    const report = JSON.parse(await readFile(join(config.outputDir, 'unity-build-results.json'), 'utf8'));
    assert.match(report.error, /mac #102: failure/);
  }
});

test('canceled builds do not fetch failure diagnostics or delete an already terminal build', async t => {
  const config = await configuration(t, {}, { BUILD_PLATFORMS: 'web' });
  const api = fakeApi(config, async call => {
    if (call.method === 'GET' && /\/builds\/101$/.test(call.path)) return json(buildRecord(config.targets[0], 101, { buildStatus: 'canceled', failureDetails: [{ displayName: 'Unused cancellation diagnostic' }] }));
  });
  await assert.rejects(runBuilds(config, api), /web #101: canceled/);
  assert.equal(api.calls.filter(call => call.path.endsWith('/failures')).length, 0);
  assert.equal(api.calls.filter(call => call.method === 'DELETE').length, 0);
  assert.equal(api.calls.filter(call => /\/artifacts|\/download\//.test(call.path)).length, 0);
});

test('oversized multiline failure messages remain bounded and omit raw fields', async t => {
  const config = await configuration(t, {}, { BUILD_PLATFORMS: 'web' });
  const api = fakeApi(config, async call => {
    if (call.method === 'GET' && /\/builds\/101$/.test(call.path)) return json(buildRecord(config.targets[0], 101, {
      buildStatus: 'failure',
      failureDetails: [{ displayName: `Configuration Error ${'D'.repeat(4_000)}`, stage: 'configure\nstage', step: 'configure\nstep', publicMessage: `Configuration invalid\n${'M'.repeat(15_000)}`, rawLog: 'never-print-raw-log' }],
    }));
  });
  await assert.rejects(runBuilds(config, api), error => {
    assert.match(error.message, /Configuration Error/);
    assert.ok(error.message.length < 1_500);
    assert.ok(!error.message.includes('configure\nstage'));
    assert.ok(!error.message.includes('configure\nstep'));
    assert.ok(!error.message.includes('never-print-raw-log'));
    return true;
  });
});

test('requires a unique primary ZIP rather than selecting a log or secondary artifact', () => {
  assert.equal(selectArchive([{ primary: true, files: [{ filename: 'build.log' }, { filename: 'release.zip' }] }, { key: 'secondary', files: [{ filename: 'debug.zip' }] }]).filename, 'release.zip');
  assert.throws(() => selectArchive([{ key: 'primary', files: [{ filename: 'a.zip' }, { filename: 'b.zip' }] }]), /exactamente un ZIP/);
  assert.throws(() => selectArchive([{ key: 'secondary', files: [{ filename: 'a.zip' }] }]), /exactamente un ZIP/);
});

for (const [label, data, expected] of [
  ['wrong size', zip, { expectedSize: 23, expectedMd5: md5 }],
  ['wrong checksum', zip, { expectedSize: 22, expectedMd5: '0'.repeat(32) }],
  ['HTML body instead of ZIP', Buffer.from('<html>storage error</html>'), {}],
]) {
  test(`download rejects ${label} and removes partial files`, async t => {
    const config = await configuration(t);
    const destination = join(config.outputDir, 'invalid.zip');
    await assert.rejects(downloadArchive('https://storage.example/release.zip', destination, { fetchImpl: async () => new Response(data), ...expected }));
    await assert.rejects(stat(destination), { code: 'ENOENT' });
    await assert.rejects(stat(`${destination}.partial`), { code: 'ENOENT' });
  });
}

test('download refuses HTTP or URL credentials before contacting storage', async t => {
  const config = await configuration(t);
  let requests = 0;
  for (const url of ['http://storage.example/a.zip', 'https://user:password@storage.example/a.zip']) {
    await assert.rejects(downloadArchive(url, join(config.outputDir, 'invalid.zip'), { fetchImpl: async () => { requests++; return new Response(zip); } }), /URL de descarga no válida/);
  }
  assert.equal(requests, 0);
});
