import { execFileSync } from 'node:child_process';
import { createHash } from 'node:crypto';
import { createWriteStream } from 'node:fs';
import { appendFile, mkdir, open, readFile, rename, rm, writeFile } from 'node:fs/promises';
import { dirname, resolve } from 'node:path';
import { Readable, Transform } from 'node:stream';
import { pipeline } from 'node:stream/promises';
import { setTimeout as delay } from 'node:timers/promises';
import { fileURLToPath, pathToFileURL } from 'node:url';

const API = 'https://build-automation.services.api.unity.com/v2';

export function loadConfig(env, { definitions, unityVersion, commit, projectId }) {
  const selection = env.BUILD_PLATFORMS || 'all';
  const targets = definitions.filter(target => selection === 'all' || target.key === selection);
  if (!targets.length) throw new Error(`Plataforma desconocida: ${selection}.`);
  const required = ['UNITY_ORG_ID', 'UNITY_SERVICE_ACCOUNT_KEY_ID', 'UNITY_SERVICE_ACCOUNT_SECRET', ...targets.map(target => target.variable)];
  const missing = required.filter(name => !env[name]?.trim());
  if (missing.length) throw new Error(`Configura en GitHub Actions: ${missing.join(', ')}. Las claves deben ser Secrets; los IDs, Variables.`);
  const selectedProjectId = env.UNITY_PROJECT_ID?.trim() || projectId?.trim();
  if (!selectedProjectId) throw new Error('Vincula el proyecto con Unity Cloud o configura UNITY_PROJECT_ID en GitHub Actions.');
  if (!/^[a-f0-9]{40}$/i.test(commit)) throw new Error('Se requiere el SHA completo del commit del checkout.');
  if (!/^\d+\.\d+\.\d+[abfp]\d+$/.test(unityVersion)) throw new Error('ProjectVersion.txt no contiene una versión válida de Unity.');
  const timeoutMinutes = Number(env.BUILD_TIMEOUT_MINUTES || 90);
  if (!Number.isInteger(timeoutMinutes) || timeoutMinutes < 1 || timeoutMinutes > 100) throw new Error('El tiempo máximo debe estar entre 1 y 100 minutos por build.');
  const configuredTargets = targets.map(target => ({ ...target, id: env[target.variable].trim() }));
  const invalidTarget = configuredTargets.find(target => /[\/\\:\s]/.test(target.id));
  if (invalidTarget) throw new Error(`${invalidTarget.variable}: pega solo el ID al final de /buildTarget/, no la URL completa.`);
  if (new Set(configuredTargets.map(target => target.id)).size !== configuredTargets.length) throw new Error('Cada plataforma necesita un build target distinto.');
  return {
    orgId: env.UNITY_ORG_ID.trim(),
    projectId: selectedProjectId,
    keyId: env.UNITY_SERVICE_ACCOUNT_KEY_ID.trim(),
    secret: env.UNITY_SERVICE_ACCOUNT_SECRET.trim(),
    repository: env.GITHUB_REPOSITORY || 'FranciscoPS/manners.exe',
    unityVersion,
    commit: commit.toLowerCase(),
    branch: env.BUILD_BRANCH || '',
    clean: env.BUILD_CLEAN === 'true',
    timeoutMs: timeoutMinutes * 60_000,
    pollMs: 15_000,
    targets: configuredTargets,
    outputDir: resolve('builds/cloud'),
    validateOnly: env.BUILD_VALIDATE_ONLY === 'true',
    summaryPath: env.GITHUB_STEP_SUMMARY,
  };
}

export function validateTarget(target, remote, config) {
  const problems = [];
  const settings = remote.settings || {};
  if (remote.buildtargetid !== target.id) problems.push('el ID devuelto no coincide');
  if (remote.enabled !== true) problems.push('el target está deshabilitado');
  if (!target.platforms.includes(remote.platform)) problems.push('la plataforma no coincide');
  if (settings.unityVersion !== config.unityVersion || settings.autoDetectUnityVersion === true || settings.fallbackPatchVersion === true) problems.push(`fija Unity ${config.unityVersion}, sin autodetección ni fallback de parche`);
  if (settings.autoBuild === true || settings.buildSchedule?.isEnabled === true) problems.push('desactiva Auto-build y los horarios; GitHub controla los disparos');
  if (!['', '.', './'].includes(settings.scm?.subdirectory || '')) problems.push('deja Project subfolder vacío; el proyecto está en la raíz del repositorio');
  const repository = (settings.scm?.url || '').replace(/^git@github\.com:/i, '').replace(/^ssh:\/\/git@github\.com\//i, '').replace(/^https?:\/\/github\.com\//i, '').replace(/\.git\/?$/i, '').replace(/\/$/, '');
  if (repository.toLowerCase() !== config.repository.toLowerCase()) problems.push('conecta el repositorio correcto en Source Control');
  if (problems.length) throw new Error(`${target.key}: ${problems.join('; ')}.`);
}

export function selectArchive(artifacts) {
  if (!Array.isArray(artifacts)) throw new Error('La lista de artefactos de Unity no es válida.');
  const files = artifacts.filter(artifact => artifact.primary === true || artifact.key === 'primary').flatMap(artifact => artifact.files || []).filter(file => /\.zip$/i.test(file.filename || ''));
  if (files.length !== 1) throw new Error('Unity debe entregar exactamente un ZIP como artefacto principal.');
  return files[0];
}

export async function downloadArchive(url, destination, { fetchImpl = fetch, signal, expectedSize, expectedMd5 } = {}) {
  const storageUrl = new URL(url);
  if (storageUrl.protocol !== 'https:' || storageUrl.username || storageUrl.password) throw new Error('Unity devolvió una URL de descarga no válida.');
  const response = await fetchImpl(storageUrl.href, { redirect: 'follow', signal: signal ? AbortSignal.any([signal, AbortSignal.timeout(1_800_000)]) : AbortSignal.timeout(1_800_000) });
  if (!response.ok || !response.body) throw new Error(`No se pudo descargar el ZIP: HTTP ${response.status}.`);
  await mkdir(dirname(destination), { recursive: true });
  const temporary = `${destination}.partial`;
  const sha256 = createHash('sha256');
  const md5 = createHash('md5');
  let bytes = 0;
  try {
    const digest = new Transform({
      transform(chunk, encoding, callback) {
        bytes += chunk.length;
        sha256.update(chunk);
        md5.update(chunk);
        callback(null, chunk);
      },
    });
    await pipeline(Readable.fromWeb(response.body), digest, createWriteStream(temporary), { signal });
    const handle = await open(temporary, 'r');
    const magic = Buffer.alloc(4);
    try { await handle.read(magic, 0, 4, 0); } finally { await handle.close(); }
    if (magic[0] !== 0x50 || magic[1] !== 0x4b || bytes < 22) throw new Error('El archivo descargado no es un ZIP válido.');
    if (Number(expectedSize) > 0 && bytes !== Number(expectedSize)) throw new Error('El tamaño del ZIP no coincide con el publicado por Unity.');
    if (expectedMd5 && md5.digest('hex') !== expectedMd5.toLowerCase()) throw new Error('El checksum del ZIP no coincide con el publicado por Unity.');
    await rename(temporary, destination);
    return { bytes, sha256: sha256.digest('hex') };
  } catch (error) {
    await rm(temporary, { force: true });
    throw error;
  }
}

export async function runBuilds(config, runtime = {}) {
  const fetchImpl = runtime.fetchImpl || fetch;
  const now = runtime.now || Date.now;
  const sleep = runtime.sleep || (ms => delay(ms, undefined, { signal: runtime.signal }));
  const logger = runtime.logger || console.log;
  const downloader = runtime.download || ((url, path, options) => downloadArchive(url, path, { fetchImpl, signal: runtime.signal, ...options }));
  const authorization = `Basic ${Buffer.from(`${config.keyId}:${config.secret}`).toString('base64')}`;
  const root = `/orgs/${encodeURIComponent(config.orgId)}/projects/${encodeURIComponent(config.projectId)}/buildtargets`;
  const active = new Map();
  const results = [];
  let stopped = false;
  let failure;

  async function request(path, { method = 'GET', body, allowDownload = false, cleanup = false } = {}) {
    for (let attempt = 0; attempt < 3; attempt++) {
      let response;
      try {
        const timeout = AbortSignal.timeout(30_000);
        response = await fetchImpl(`${API}${path}`, {
          method,
          headers: { Authorization: authorization, Accept: 'application/json', ...(body ? { 'Content-Type': 'application/json' } : {}) },
          body: body ? JSON.stringify(body) : undefined,
          redirect: 'manual',
          signal: runtime.signal && !cleanup ? AbortSignal.any([runtime.signal, timeout]) : timeout,
        });
      } catch {
        if (method === 'GET' && attempt < 2 && !runtime.signal?.aborted) { await sleep((attempt + 1) * 2_000); continue; }
        throw new Error(method === 'POST' ? 'No se pudo confirmar la solicitud de build. No se reintentó para evitar duplicados; revisa Build History en Unity.' : 'No se pudo contactar con la API de Unity.');
      }
      if (method === 'GET' && (response.status === 429 || response.status >= 500) && attempt < 2) {
        await response.body?.cancel();
        const retryAfter = Number(response.headers.get('retry-after'));
        await sleep(Number.isFinite(retryAfter) && retryAfter > 0 ? Math.min(retryAfter, 30) * 1_000 : (attempt + 1) * 2_000);
        continue;
      }
      if (!response.ok && !(allowDownload && response.status === 303)) {
        const hints = { 401: 'Comprueba las claves de la cuenta de servicio.', 403: 'Asigna el rol Automation User al proyecto.', 404: 'Comprueba Organization ID, Project ID y Build Target IDs.', 409: 'Ya existe una build pendiente. No se reutiliza ni se cancela una build ajena.' };
        throw new Error(`Unity API: HTTP ${response.status}. ${hints[response.status] || 'Revisa la configuración en Unity Cloud.'}`);
      }
      if (response.status === 204) return undefined;
      try { return await response.json(); } catch { throw new Error('Unity devolvió una respuesta JSON no válida.'); }
    }
  }

  async function cancelOwnBuilds() {
    await Promise.allSettled([...active.values()].map(async build => {
      try {
        await request(build.path, { method: 'DELETE', cleanup: true });
        active.delete(build.path);
        logger(`${build.key}: cancelación solicitada para build ${build.number}.`);
      } catch {
        logger(`Revisa Build History: no se pudo confirmar la cancelación de ${build.key} #${build.number}.`);
      }
    }));
  }

  const onAbort = () => { stopped = true; failure ||= new Error('La ejecución fue cancelada.'); };
  runtime.signal?.addEventListener('abort', onAbort, { once: true });
  try {
    for (const target of config.targets) {
      const remote = await request(`${root}/${encodeURIComponent(target.id)}`);
      validateTarget(target, remote, config);
      logger(`${target.key}: configuración verificada, Unity ${config.unityVersion}.`);
    }
    if (config.validateOnly) return { commit: config.commit, validated: config.targets.map(target => target.key), builds: [] };
    if (runtime.signal?.aborted) throw new Error('La ejecución fue cancelada.');
    await mkdir(config.outputDir, { recursive: true });
    let cursor = 0;
    async function worker() {
      while (!stopped && cursor < config.targets.length) {
        const target = config.targets[cursor++];
        const result = { target: target.key, targetId: target.id, status: 'requesting' };
        results.push(result);
        try {
          const targetPath = `${root}/${encodeURIComponent(target.id)}`;
          const startedAt = now();
          const requested = await request(`${targetPath}/builds`, { method: 'POST', body: { commit: config.commit, unityVersion: config.unityVersion, clean: config.clean, delay: 0, ...(config.branch ? { branch: config.branch } : {}) } });
          const record = Array.isArray(requested) && requested.length === 1 ? requested[0] : undefined;
          if (!record || record.buildtargetid !== target.id || !Number.isInteger(record.build) || record.build <= 0) throw new Error('La respuesta de Unity no identifica una única build del target solicitado; revisa Build History.');
          const buildPath = `${targetPath}/builds/${record.build}`;
          active.set(buildPath, { key: target.key, number: record.build, path: buildPath });
          result.build = record.build;
          if (record.requestedRevision && record.requestedRevision.toLowerCase() !== config.commit) throw new Error(`${target.key}: Unity no aceptó el commit solicitado.`);
          while (!stopped) {
            if (now() - startedAt >= config.timeoutMs) throw new Error(`${target.key}: se agotó el tiempo máximo de build.`);
            const build = await request(buildPath);
            if (stopped) throw new Error('Otra plataforma falló o la ejecución fue cancelada.');
            if (build.buildtargetid !== target.id || build.build !== record.build || !target.platforms.includes(build.platform)) throw new Error(`${target.key}: Unity devolvió una build diferente de la solicitada.`);
            result.status = build.buildStatus;
            logger(`${target.key} #${record.build}: ${build.buildStatus} (${Math.floor((now() - startedAt) / 60_000)} min).`);
            if (build.buildStatus === 'failure' || build.buildStatus === 'canceled') {
              active.delete(buildPath);
              throw new Error(`${target.key} #${record.build}: ${build.buildStatus}. Revisa los logs en Unity Build History.`);
            }
            if (build.buildStatus === 'success') {
              active.delete(buildPath);
              if (build.lastBuiltRevision?.toLowerCase() !== config.commit) throw new Error(`${target.key}: el commit realmente compilado no coincide; no se descargarán ni publicarán sus archivos.`);
              for (const builtVersion of [build.unityVersion, build.localUnityVersion].filter(Boolean)) {
                if (builtVersion !== config.unityVersion) throw new Error(`${target.key}: la versión de Unity compilada no coincide.`);
              }
              result.revision = build.lastBuiltRevision;
              const archive = selectArchive(await request(`${buildPath}/artifacts`));
              const signed = await request(`${buildPath}/download/${encodeURIComponent(archive.filename)}`, { allowDownload: true });
              if (stopped) throw new Error('Otra plataforma falló o la ejecución fue cancelada.');
              const url = typeof signed === 'string' ? signed : signed?.url;
              if (typeof url !== 'string') throw new Error('Unity no devolvió una URL para descargar el ZIP.');
              const destination = resolve(config.outputDir, `${target.artifactName}.zip`);
              const downloaded = await downloader(url, destination, { expectedSize: archive.size, expectedMd5: archive.md5sum });
              Object.assign(result, downloaded, { archive: `${target.artifactName}.zip` });
              break;
            }
            await sleep(config.pollMs);
          }
        } catch (error) {
          result.status = 'error';
          result.error = error.message;
          failure ||= error;
          stopped = true;
          await cancelOwnBuilds();
        }
      }
    }
    await Promise.allSettled(Array.from({ length: Math.min(2, config.targets.length) }, worker));
    if (failure) throw failure;
    return { commit: config.commit, builds: results };
  } catch (error) {
    failure ||= error;
    throw error;
  } finally {
    stopped = true;
    await cancelOwnBuilds();
    runtime.signal?.removeEventListener('abort', onAbort);
    if (!config.validateOnly) {
      await mkdir(config.outputDir, { recursive: true });
      await writeFile(resolve(config.outputDir, 'unity-build-results.json'), `${JSON.stringify({ commit: config.commit, unityVersion: config.unityVersion, ...(failure ? { error: failure.message } : {}), builds: results }, null, 2)}\n`);
    }
    if (config.summaryPath) {
      const rows = results.map(result => `| ${result.target} | ${result.build || '-'} | ${result.status} | ${result.archive || '-'} |`);
      await appendFile(config.summaryPath, `\nUnity Build Automation · ${config.unityVersion}\n\nCommit: \`${config.commit}\`\n\n${config.validateOnly && !failure ? 'Configuración verificada sin solicitar builds.\n' : '| Plataforma | Build | Estado | Archivo |\n|---|---:|---|---|\n' + rows.join('\n') + '\n'}${failure ? '\nLa validación o la build falló; consulta el error del paso.\n' : ''}`);
    }
  }
}

async function main() {
  const projectRoot = resolve(dirname(fileURLToPath(import.meta.url)), '../..');
  const definitions = JSON.parse(await readFile(resolve(projectRoot, '.github/unity-build-targets.json'), 'utf8'));
  const version = await readFile(resolve(projectRoot, 'ProjectSettings/ProjectVersion.txt'), 'utf8');
  const playerSettings = await readFile(resolve(projectRoot, 'ProjectSettings/ProjectSettings.asset'), 'utf8');
  const unityVersion = version.match(/^m_EditorVersion:\s*(\S+)/m)?.[1];
  const projectId = playerSettings.match(/^[ \t]*cloudProjectId:[ \t]*([^\r\n]*)/m)?.[1].trim();
  const commit = execFileSync('git', ['rev-parse', 'HEAD'], { cwd: projectRoot, encoding: 'utf8' }).trim();
  const config = loadConfig(process.env, { definitions, unityVersion, commit, projectId });
  config.outputDir = resolve(projectRoot, 'builds/cloud');
  const abort = new AbortController();
  const stop = () => abort.abort();
  process.once('SIGINT', stop);
  process.once('SIGTERM', stop);
  try { await runBuilds(config, { signal: abort.signal }); }
  finally { process.removeListener('SIGINT', stop); process.removeListener('SIGTERM', stop); }
}

if (process.argv[1] && import.meta.url === pathToFileURL(resolve(process.argv[1])).href) {
  main().catch(error => { console.error(error.message); process.exitCode = 1; });
}
